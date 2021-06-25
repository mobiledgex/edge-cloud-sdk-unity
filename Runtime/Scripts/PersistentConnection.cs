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
using System.Threading.Tasks;
using static DistributedMatchEngine.ServerEdgeEvent.Types;
using static DistributedMatchEngine.PerformanceMetrics.NetTest;
using DistributedMatchEngine.PerformanceMetrics;
namespace MobiledgeX
{
  [RequireComponent(typeof(LocationService))]
  [AddComponentMenu("MobiledgeX/PersistentConnection")]
  public class PersistentConnection : MonoBehaviour
  {
    internal Action<MobiledgeXIntegration> startStreamingEvents;
    MobiledgeXIntegration integration;
    bool locationUpdatesRunning;
    bool latencyUpdatesRunning;
    EdgeEventsConfig config;
    bool hasTCPPorts;
    int latencyUpdatesCounter;
    int locationUpdatesCounter;
    Dictionary<ServerEventType, FindCloudletEventTrigger> event_trigger_dict;
    FindCloudletReply currentFindCloudlet;
    public Loc location;
    public bool useMobiledgexLocationServices = true;

    #region MonoBehaviour Callbacks

    private void Awake()
    {
      event_trigger_dict = new Dictionary<ServerEventType, FindCloudletEventTrigger>();
      event_trigger_dict.Add(ServerEventType.EventAppinstHealth, FindCloudletEventTrigger.AppInstHealthChanged);
      event_trigger_dict.Add(ServerEventType.EventCloudletState, FindCloudletEventTrigger.CloudletStateChanged);
      event_trigger_dict.Add(ServerEventType.EventCloudletUpdate, FindCloudletEventTrigger.CloserCloudlet);
      event_trigger_dict.Add(ServerEventType.EventCloudletMaintenance, FindCloudletEventTrigger.CloudletMaintenanceStateChanged);
      event_trigger_dict.Add(ServerEventType.EventLatencyProcessed, FindCloudletEventTrigger.LatencyTooHigh);
      event_trigger_dict.Add(ServerEventType.EventLatencyRequest, FindCloudletEventTrigger.LatencyTooHigh);
      event_trigger_dict.Add(ServerEventType.EventError, FindCloudletEventTrigger.Error);
      //EVENT_INIT_CONNECTION and EVENT_UNKNOWN will be logged
    }

    IEnumerator Start()
    {
#if !UNITY_EDITOR
      yield return StartCoroutine(UpdateLocation());
#endif
      yield break;
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
      if (config.newFindCloudletEventTriggers.Count == 1) //No FindCloudletTrigger except Error (Added By Default)
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
      //latency config validation
      if (config.newFindCloudletEventTriggers.Contains(FindCloudletEventTrigger.LatencyTooHigh))
      {
        if (config.latencyConfig.updatePattern == UpdatePattern.OnInterval)
        {
          if (config.latencyConfig.maxNumberOfUpdates < 0)
          {
            PropagateError(FindCloudletEventTrigger.Error, "latencyConfig.maxNumberOfUpdates must greater >= 0");
            return false;
          }
          if (config.latencyConfig.updateIntervalSeconds < 0)
          {
            PropagateError(FindCloudletEventTrigger.Error, "latencyConfig.updateIntervalSeconds must greater >= 0");
            return false;
          }
        }
      }
      //location config validation
      if (config.newFindCloudletEventTriggers.Contains(FindCloudletEventTrigger.CloserCloudlet))
      {

        if (config.locationConfig.updatePattern == UpdatePattern.OnInterval)
        {
          if (config.locationConfig.maxNumberOfUpdates < 0)
          {
            PropagateError(FindCloudletEventTrigger.Error, "locationConfig.maxNumberOfUpdates must >= 0");
            return false;
          }
          if (config.locationConfig.updateIntervalSeconds < 0)
          {
            PropagateError(FindCloudletEventTrigger.Error, "locationConfig.updateIntervalSeconds must greater >= 0");
            return false;
          }
        }
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


    public void StartEdgeEvents(MobiledgeXIntegration mxi)
    {
      integration = mxi;
      config = MobiledgeXIntegration.settings.edgeEventsConfig;
      currentFindCloudlet = integration.latestFindCloudletReply;
      //Log summary of EdgeEvents
      string configSummary = "EdgeEvents Config Summary:\n LatencyTestPort: " + config.latencyTestPort;
      configSummary += "\n EdgeEvents Config: LatencyThresholdTriggerMs: " + config.latencyThresholdTriggerMs;
      configSummary += "\n EdgeEvents Config: LatencyUpdatePattern: " + config.latencyConfig.updatePattern;
      configSummary += "\n EdgeEvents Config: LatencyMaxNoUpdates: " + config.latencyConfig.maxNumberOfUpdates;
      configSummary += "\n EdgeEvents Config: LatencyUpdateIntervalSeconds: " + config.latencyConfig.updateIntervalSeconds;
      configSummary += "\n EdgeEvents Config: LocationUpdatePattern: " + config.locationConfig.updatePattern;
      configSummary += "\n EdgeEvents Config: LocationMaxNoUpdates: " + config.locationConfig.maxNumberOfUpdates;
      configSummary += "\n EdgeEvents Config: LocationUpdateIntervalSeconds: " + config.locationConfig.updateIntervalSeconds;
      configSummary += "\n NewFindCloudletEventTriggers: ";
      foreach (FindCloudletEventTrigger trigger in config.newFindCloudletEventTriggers)
      {
        configSummary += "\n  Trigger : " + trigger.ToString();
      }
      Logger.Log(configSummary);

      integration.matchingEngine.EdgeEventsReceiver += HandleReceivedEvents;
      EdgeEventsConnection connection = integration.matchingEngine.EdgeEventsConnection;
      if (!ValidateConfigs())
      {
        return;
      }
      AppPort appPort = integration.GetAppPort(LProto.Tcp, config.latencyTestPort);
      if (appPort != null)
      {
        hasTCPPorts = true;
        if (config.latencyTestPort == 0)
        {
          config.latencyTestPort = appPort.PublicPort;
        }
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
      yield return StartCoroutine(UpdateLocation());
      Logger.Log("EdgeEvents Posting location update, Location to send [" + location.Latitude + ", " + location.Longitude + "]");
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
      yield return StartCoroutine(UpdateLocation());
      if (hasTCPPorts)
      {
        connection.TestConnectAndPostLatencyUpdate(host, (uint)config.latencyTestPort, location).ConfigureAwait(false);
      }
      else
      {
        connection.TestPingAndPostLatencyUpdate(host, location).ConfigureAwait(false);
      }
      Logger.Log("EdgeEvents Posting latency update, Location to send [" + location.Latitude + ", " + location.Longitude + "]");
      latencyUpdatesCounter++;
      yield return StartCoroutine(OnIntervalEdgeEventsLatency(connection, host));
    }

    void HandleReceivedEvents(ServerEdgeEvent edgeEvent)
    {
      Logger.Log("<color=green>Received event type: " + edgeEvent.EventType + "</color>");
      FindCloudletEventTrigger trigger = event_trigger_dict[edgeEvent.EventType];
      if (config.newFindCloudletEventTriggers.Contains(trigger))
      {
        switch (trigger)
        {
          case FindCloudletEventTrigger.Error:
            PropagateError(FindCloudletEventTrigger.Error, edgeEvent.ErrorMsg);
            return;
          case FindCloudletEventTrigger.LatencyTooHigh:
            if (edgeEvent.EventType == ServerEventType.EventLatencyRequest)
            {
              StartCoroutine(RespondToLatencyRequest());
              return;
            }
            ProcessLatency(edgeEvent.Statistics);
            return;
          default:
            if (edgeEvent.NewCloudlet != null)
            {
              PropagateSuccess(trigger, edgeEvent.NewCloudlet);
            }
            return;
        }
      }
    }

    IEnumerator RespondToLatencyRequest()
    {
      string host = integration.GetHost();
      yield return StartCoroutine(UpdateLocation());
      if (hasTCPPorts)
      {
        Logger.Log("Sending Latency Samples (TCP) as a response to LatencyRequest from the server");
        integration.matchingEngine.EdgeEventsConnection.TestConnectAndPostLatencyUpdate(host, (uint)config.latencyTestPort, location).ConfigureAwait(false);
      }
      else
      {
        Logger.Log("Sending Latency Samples (UDP) as a response to LatencyRequest from the server");
        integration.matchingEngine.EdgeEventsConnection.TestPingAndPostLatencyUpdate(host, location).ConfigureAwait(false);
      }
    }

    async void ProcessLatency(Statistics stats)
    {
      if (stats.Avg < config.latencyThresholdTriggerMs)
      {
        FindCloudletMode previousMode = integration.Mode;
        integration.UseFindCloudletPerformanceMode(true);
        bool fcResult = await integration.FindCloudlet();
        if (fcResult)
        {
          if (!integration.latestFindCloudletReply.Equals(currentFindCloudlet))
          {
            CompareLatencies(stats);
            integration.mode = previousMode;
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
        appPort = integration.GetAppPort(LProto.Tcp, config.latencyTestPort);
        site = new Site { host = appPort.FqdnPrefix + currentFindCloudlet.Fqdn, port = appPort.PublicPort, testType = TestType.CONNECT };
      }
      else
      {
        appPort = integration.GetAppPort(LProto.Udp);
        site = new Site { host = appPort.FqdnPrefix + currentFindCloudlet.Fqdn, port = appPort.PublicPort, testType = TestType.PING };
      }
      await netTest.TestSite(site);
      double normalizedLatency = receivedStats.Avg - (receivedStats.Avg * config.performanceSwitchMargin);
      if (site.average < normalizedLatency)
      {
        PropagateSuccess(FindCloudletEventTrigger.LatencyTooHigh, currentFindCloudlet);
      }
      else
      {
        //switch back to the previous cloudlet
        integration.latestFindCloudletReply = currentFindCloudlet;
        PropagateError(FindCloudletEventTrigger.LatencyTooHigh,
          "Latency threshold exceeded, but no other cloudlets have a meaningful improvement in latency");
      }
    }

    async Task StopEdgeEvents()
    {
      StopAllCoroutines();
      if (integration != null)
      {
        await integration.matchingEngine.EdgeEventsConnection.SendTerminate();
      }
      latencyUpdatesCounter = 0;
      latencyUpdatesRunning = false;
      locationUpdatesCounter = 0;
      locationUpdatesRunning = false;
      location = null;
    }

    async void PropagateSuccess(FindCloudletEventTrigger trigger, FindCloudletReply newCloudlet)
    {
      Logger.Log("Received NewCloudlet from the server triggered by" + trigger);
      FindCloudletEvent findCloudletEvent = new FindCloudletEvent();
      findCloudletEvent.trigger = trigger;
      findCloudletEvent.newCloudlet = newCloudlet;
      EdgeEventsStatus eventStatus = new EdgeEventsStatus(Status.success);
      try
      {
        integration.NewFindCloudletHandler(eventStatus, findCloudletEvent);
      }
      catch (NullReferenceException)
      {
        Debug.LogError("Missing NewCloudletHandler, " +
          "Listen to EdgeEvents by using \"mobiledgexIntegration.NewCloudletHandler += HandleNewFindCloudlet;\"");
      }
      if (config.autoMigration)
      {
        await StopEdgeEvents();
        integration.persistentConnection.startStreamingEvents(integration);
      }
      else
      {
        //Migration occurs and autoMigration is set to false
        await StopEdgeEvents();
      }
    }

    async void PropagateError(FindCloudletEventTrigger trigger, string error_msg)
    {

      FindCloudletEvent findCloudletEvent = new FindCloudletEvent();
      findCloudletEvent.trigger = trigger;
      Logger.Log(error_msg);
      EdgeEventsStatus eventStatus = new EdgeEventsStatus(Status.error, error_msg);
      await StopEdgeEvents();
      try
      {
        integration.NewFindCloudletHandler(eventStatus, findCloudletEvent);
      }
      catch (NullReferenceException)
      {
        Debug.LogError("Missing NewCloudletHandler, " +
          "Listen to EdgeEventsHandler by using \"mobiledgexIntegration.NewCloudletHandler += HandleNewFindCloudlet;\"");
        return;
      }
    }

    IEnumerator UpdateLocation()
    {
      if (!useMobiledgexLocationServices)
      {
        if (location == null)
        {
          PropagateError(FindCloudletEventTrigger.Error, "PersistentConnection.location is empty, Either use MobiledgeXLocationServices or Set PersistentConnection.location to a value. ");
        }
        yield break;
      }
      else
      {
        yield return StartCoroutine(LocationService.EnsureLocation());
        try
        {
          location = LocationService.RetrieveLocation();//throws location exception if location is not retrieved
        }
        catch(LocationException loc)
        {
          PropagateError(FindCloudletEventTrigger.Error, "Error retrieving location " + loc.Message);
        }
        yield break;
      }
    }
    #endregion
  }
}
