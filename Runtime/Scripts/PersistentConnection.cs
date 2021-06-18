/**
 * Copyright 2018-2021 MobiledgeX, Inc. All rights and licenses reserved.
 * MobiledgeX, Inc. 156 2nd Street #408, San Francisco, CA 94105
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using UnityEngine;
using DistributedMatchEngine;
using System.Collections;
using System.Collections.Generic;
using static DistributedMatchEngine.ServerEdgeEvent.Types;
using static DistributedMatchEngine.PerformanceMetrics.NetTest;
using DistributedMatchEngine.PerformanceMetrics;
namespace MobiledgeX
{
  [RequireComponent(typeof(LocationService))]
  [AddComponentMenu("MobiledgeX/PersistentConnection")]
  public class PersistentConnection : MonoBehaviour
  {
    public Action<MobiledgeXIntegration> startStreamingEvents;
    MobiledgeXIntegration integration;
    bool locationUpdatesRunning;
    bool latencyUpdatesRunning;
    EdgeEventsConfig config;
    Loc location;
    bool hasTCPPorts;
    int latencyUpdatesCounter;
    int locationUpdatesCounter;
    Dictionary<ServerEventType, FindCloudletEventTrigger> event_trigger_dict;
    #region MonoBehaviour Callbacks

    private void Awake()
    {
      event_trigger_dict = new Dictionary<ServerEventType, FindCloudletEventTrigger>();
      event_trigger_dict.Add(ServerEventType.EventAppinstHealth, FindCloudletEventTrigger.AppInstHealthChanged);
      event_trigger_dict.Add(ServerEventType.EventCloudletState, FindCloudletEventTrigger.CloudletStateChanged);
      event_trigger_dict.Add(ServerEventType.EventCloudletUpdate, FindCloudletEventTrigger.CloserCloudlet);
      event_trigger_dict.Add(ServerEventType.EventCloudletMaintenance, FindCloudletEventTrigger.CloudletMaintenanceStateChanged);
      event_trigger_dict.Add(ServerEventType.EventLatencyProcessed, FindCloudletEventTrigger.LatencyTooHigh);
      event_trigger_dict.Add(ServerEventType.EventError, FindCloudletEventTrigger.Error);
      //EVENT_INIT_CONNECTION and EVENT_UNKNOWN will be logged
      //EVENT_LATENCY_REQUEST will be processed
    }

    private void OnEnable()
    {
      startStreamingEvents += StartEdgeEvents;
      config = MobiledgeXIntegration.settings.edgeEventsConfig;
    }

    private void OnApplicationPause(bool pause)
    {
      if (pause)
      {
        StopAllCoroutines();// stop edge events streaming
      }
      else
      {
        // resume edge events streaming
        if (locationUpdatesRunning)
        {
          StartCoroutine(OnIntervalEdgeEventsLocation(integration.matchingEngine.EdgeEventsConnection));
        }
        if (latencyUpdatesRunning)
        {
          StartCoroutine(OnIntervalEdgeEventsLatency(integration.matchingEngine.EdgeEventsConnection, integration.GetHost()));
        }
      }
    }

    private void OnApplicationQuit()
    {
      StopAllCoroutines();// stop edge events streaming
    }

    private void OnDestroy()
    {
      StopAllCoroutines();// stop edge events streaming
    }
    #endregion

    #region PersistentConnection Functions

    bool ValidateConfigs()
    {
      if (config.newFindCloudletEventTriggers.Count == 1)//No FindCloudletTrigger except Error (Added By Default)
      {
        PropagateError(FindCloudletEventTrigger.Error, "Missing FindCloudlets Triggers");
        return false;
      }

      if (config.latencyThresholdTriggerMs <= 0)
      {
        PropagateError(FindCloudletEventTrigger.Error, "latencyThresholdTriggerMs must be greater than 0");
        return false;
      }

      if (config.performanceSwitchMargin > 1 || config.performanceSwitchMargin < 0)
      {
        PropagateError(FindCloudletEventTrigger.Error, "performanceSwitchMargin must between (0 to 1.0f)");
        return false;
      }

      if (integration.matchingEngine.sessionCookie == null)
      {
        PropagateError(FindCloudletEventTrigger.Error, "Missing SessionCookie");
        return false;
      }
      if (integration.matchingEngine.EdgeEventsConnection.edgeEventsCookie == null)
      {
        PropagateError(FindCloudletEventTrigger.Error, "Missing EdgeEvents Cookie");
        return false;
      }

      return true;
    }


    void StartEdgeEvents(MobiledgeXIntegration mxi)
    {
      integration = mxi;
      config = MobiledgeXIntegration.settings.edgeEventsConfig;
      //Log summary of EdgeEvents
      Logger.Log("EdgeEvents Config: LatencyTestPort: " + config.latencyTestPort);
      Logger.Log("EdgeEvents Config: NewFindCloudletEventTriggers: ");
      foreach (FindCloudletEventTrigger trigger in config.newFindCloudletEventTriggers)
      {
        Logger.Log("Trigger : " + trigger.ToString());
      }
      Logger.Log("EdgeEvents Config: LatencyThresholdTriggerMs: " + config.latencyThresholdTriggerMs);
      Logger.Log("EdgeEvents Config: LatencyUpdatePattern: " + config.latencyConfig.updatePattern);
      Logger.Log("EdgeEvents Config: LatencyMaxNoUpdates: " + config.latencyConfig.maxNumberOfUpdates);
      Logger.Log("EdgeEvents Config: LatencyUpdateIntervalSeconds: " + config.latencyConfig.updateIntervalSeconds);
      Logger.Log("EdgeEvents Config: LocationUpdatePattern: " + config.locationConfig.updatePattern);
      Logger.Log("EdgeEvents Config: LocationMaxNoUpdates: " + config.locationConfig.maxNumberOfUpdates);
      Logger.Log("EdgeEvents Config: LocationUpdateIntervalSeconds: " + config.locationConfig.updateIntervalSeconds);

      integration.matchingEngine.EdgeEventsReceiver += HandleReceivedEvents;
      EdgeEventsConnection connection = integration.matchingEngine.EdgeEventsConnection;
      bool valid = ValidateConfigs();
      if (!valid)
      {
        return;
      }
      location = LocationService.RetrieveLocation();
      AppPort appPort = integration.GetAppPort(LProto.Tcp, config.latencyTestPort == 0 ? 0 : config.latencyTestPort);
      if (appPort != null)
      {
        hasTCPPorts = true;
      }
      else
      {
        if (config.latencyTestPort != 0)
        {
          PropagateError(FindCloudletEventTrigger.Error, "Test port doesn't exist");
          return;
        }
        hasTCPPorts = false;
      }
      string host = integration.GetHost();
      StartEdgeEventsLatency(connection, host);
      StartEdgeEventsLocation(connection);
    }

    void StartEdgeEventsLocation(EdgeEventsConnection connection)
    {
      connection.PostLocationUpdate(location);
      switch (config.locationConfig.updatePattern)
      {
        case UpdatePattern.OnStart:
          locationUpdatesRunning = false;
          break;
        case UpdatePattern.OnInterval:
          locationUpdatesRunning = true;
          StartCoroutine(OnIntervalEdgeEventsLocation(connection));
          break;
      }
    }

    void StartEdgeEventsLatency(EdgeEventsConnection connection, string host)
    {
      if (hasTCPPorts)
      {
        connection.TestConnectAndPostLatencyUpdate(host, (uint)config.latencyTestPort, location).ConfigureAwait(false);
      }
      else
      {
        connection.TestPingAndPostLatencyUpdate(host, location).ConfigureAwait(false);
      }
      switch (config.latencyConfig.updatePattern)
      {
        case UpdatePattern.OnStart:
          latencyUpdatesRunning = false;
          break;
        case UpdatePattern.OnInterval:
          latencyUpdatesRunning = true;
          StartCoroutine(OnIntervalEdgeEventsLatency(connection, host));
          break;
      }
    }

    IEnumerator OnIntervalEdgeEventsLocation(EdgeEventsConnection connection)
    {
      if (config.locationConfig.maxNumberOfUpdates > 0)
      {
        if (locationUpdatesCounter >= config.locationConfig.maxNumberOfUpdates)
        {
          locationUpdatesRunning = false;
          Logger.Log("Stopping Location Updates according to configs, No. LocationUpdates: " + locationUpdatesCounter);
          locationUpdatesCounter = 0;
          yield break;
        }
      }
      yield return new WaitForSecondsRealtime(config.locationConfig.updateIntervalSeconds);
      yield return StartCoroutine(LocationService.EnsureLocation());
      Loc location = LocationService.RetrieveLocation();
      Logger.Log("EdgeEvents Posting location update");
      connection.PostLocationUpdate(location);
      locationUpdatesCounter++;
      yield return StartCoroutine(OnIntervalEdgeEventsLocation(connection));
    }

    IEnumerator OnIntervalEdgeEventsLatency(EdgeEventsConnection connection, string host)
    {
      if (config.latencyConfig.maxNumberOfUpdates > 0)
      {
        if (latencyUpdatesCounter >= config.latencyConfig.maxNumberOfUpdates)
        {
          latencyUpdatesRunning = false;
          Logger.Log("Stopping Latency Updates according to configs, No. LatencyUpdates: " + latencyUpdatesCounter);
          latencyUpdatesCounter = 0;
          yield break;
        }
      }
      yield return new WaitForSecondsRealtime(config.latencyConfig.updateIntervalSeconds);
      yield return StartCoroutine(LocationService.EnsureLocation());
      if (hasTCPPorts)
      {
        connection.TestConnectAndPostLatencyUpdate(host, (uint)config.latencyTestPort, location).ConfigureAwait(false);
      }
      else
      {
        integration.matchingEngine.EdgeEventsConnection
          .TestPingAndPostLatencyUpdate
          (integration.GetHost(), location).ConfigureAwait(false);
      }
      latencyUpdatesCounter++;
      yield return StartCoroutine(OnIntervalEdgeEventsLatency(connection, host));
    }

    async void HandleReceivedEvents(ServerEdgeEvent edgeEvent)
    {
      Logger.Log("Received event type: " + edgeEvent.EventType);
      FindCloudletEvent findCloudletEvent = new FindCloudletEvent();
      FindCloudletEventTrigger trigger = event_trigger_dict[edgeEvent.EventType];
      EdgeEventsStatus eventStatus;
      if (config.newFindCloudletEventTriggers.Contains(trigger))
      {
        switch (trigger)
        {
          case FindCloudletEventTrigger.Error:
            PropagateError(FindCloudletEventTrigger.Error, edgeEvent.ErrorMsg);
            return;
          case FindCloudletEventTrigger.LatencyTooHigh:
            ProcessLatency(edgeEvent.Statistics);
            break;
          default:
            if (edgeEvent.NewCloudlet != null)
            {
              integration.latestFindCloudletReply = edgeEvent.NewCloudlet;
              findCloudletEvent.trigger = trigger;
              findCloudletEvent.newCloudlet = edgeEvent.NewCloudlet;
              Logger.Log("Received NewCloudlet from the server triggered by" + event_trigger_dict[edgeEvent.EventType]);
              eventStatus = new EdgeEventsStatus(Status.success);
              integration.NewFindCloudletHandler(eventStatus, findCloudletEvent);
              if (config.autoMigration)
              {
                await integration.matchingEngine.EdgeEventsConnection.SendTerminate();
                CleanUp();
                integration.persistentConnection.startStreamingEvents(integration);
              }
            }
            return;
        }
      }
      else
      {
        if (edgeEvent.EventType == ServerEventType.EventLatencyRequest)
        {
          string host = integration.GetHost();
          Loc location = LocationService.RetrieveLocation();
          if (hasTCPPorts)
          {
            await integration.matchingEngine.EdgeEventsConnection.TestConnectAndPostLatencyUpdate(host, (uint)config.latencyTestPort, location);
          }
          else
          {
            await integration.matchingEngine.EdgeEventsConnection.TestPingAndPostLatencyUpdate(host, location);
          }
        }
      }
    }

    async void ProcessLatency(Statistics stats)
    {
      FindCloudletReply currentCloudlet = integration.latestFindCloudletReply;
      if (stats.Avg < config.latencyThresholdTriggerMs)
      {
        integration.UseFindCloudletPerformanceMode(true);
        bool fcResult = await integration.FindCloudlet();
        if (fcResult)
        {
          if (!integration.latestFindCloudletReply.Equals(currentCloudlet))
          {
            CompareLatencies(stats);
          }
          else
          {
            PropagateError(FindCloudletEventTrigger.LatencyTooHigh,
              "New Cloudlet obtained from FindCloudletPerformanceMode is the same as old cloudlet");
          }
        }
        else
        {
          PropagateError(FindCloudletEventTrigger.LatencyTooHigh, "FindCloudletPerformanceMode failed");
        }
      }
    }

    async void CompareLatencies(Statistics receivedStats)
    {
      AppPort appPort;
      Site site;
      var netTest = new NetTest(integration.matchingEngine);
      if (hasTCPPorts)
      {
        appPort = integration.GetAppPort(LProto.Tcp, config.latencyTestPort == 0 ? 0 : config.latencyTestPort);
        site = new Site { host = appPort.FqdnPrefix + integration.latestFindCloudletReply.Fqdn, port = appPort.PublicPort, testType = TestType.CONNECT };
      }
      else
      {
        appPort = integration.GetAppPort(LProto.Udp);
        site = new Site { host = appPort.FqdnPrefix + integration.latestFindCloudletReply.Fqdn, port = appPort.PublicPort, testType = TestType.PING };
      }
      await netTest.TestSite(site);
      double normalizedLatency = receivedStats.Avg - (receivedStats.Avg * config.performanceSwitchMargin);
      if (site.average < normalizedLatency)
      {
        FindCloudletEvent findCloudletEvent = new FindCloudletEvent();
        findCloudletEvent.trigger = FindCloudletEventTrigger.LatencyTooHigh;
        findCloudletEvent.newCloudlet = integration.latestFindCloudletReply;
        EdgeEventsStatus eventStatus = new EdgeEventsStatus(Status.success);
        integration.NewFindCloudletHandler(eventStatus, findCloudletEvent);
      }
      else
      {
        PropagateError(FindCloudletEventTrigger.LatencyTooHigh,
          "Cloudlet obtained from FindCloudletPerformanceMode have higher latency than the previous cloudlet");
      }
    }

    void CleanUp()
    {
      latencyUpdatesCounter = 0;
      latencyUpdatesRunning = false;
      locationUpdatesCounter = 0;
      locationUpdatesRunning = false;
      location = null;
    }

    void PropagateError(FindCloudletEventTrigger trigger, string error_msg)
    {
      FindCloudletEvent findCloudletEvent = new FindCloudletEvent();
      findCloudletEvent.trigger = trigger;
      Logger.Log(error_msg);
      EdgeEventsStatus eventStatus = new EdgeEventsStatus(Status.error, error_msg);
      integration.NewFindCloudletHandler(eventStatus, findCloudletEvent);
    }
    #endregion
  }
}
