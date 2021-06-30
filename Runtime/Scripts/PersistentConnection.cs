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
    FindCloudletReply currentFindCloudlet;
    bool stopEdgeEventsRequested;
    enum LatencyProcessingStatus
    {
      Ready,
      Start,
      InProgress
    }
    LatencyProcessingStatus processingStatus;
    Statistics latestServerStats;

    /// <summary>
    /// Location sent to the DME Server, used only if persistentConnection.useMobiledgexLocationServices = false
    /// </summary>
    public Loc location;

    /// <summary>
    /// Set to false if you have your own Location handler. If useMobiledgexLocationServices = false, you must set persistentConnection.location to a value.
    /// </summary>
    [Tooltip("Set to false if you have your own Location handler.If useMobiledgexLocationServices = false, you must set persistentConnection.location to a value.")]
    public bool useMobiledgexLocationServices = true;

    internal string hostOverride;
    internal uint portOverride;


    #region MonoBehaviour Callbacks

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

    private void Update()
    {
      if (stopEdgeEventsRequested)
      {
        stopEdgeEventsRequested = false;
        StopAllCoroutines();
      }

      if (processingStatus == LatencyProcessingStatus.Start)
      {
        processingStatus = LatencyProcessingStatus.InProgress;
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        ProcessLatency(latestServerStats);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
      }
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
      if (integration.matchingEngine.edgeEventsCookie == null)
      {
        PropagateError(FindCloudletEventTrigger.Error, "Missing EdgeEvents Cookie");
        return false;
      }
      return true;
    }


    public async void StartEdgeEvents(MobiledgeXIntegration mxi)
    {
      stopEdgeEventsRequested = false;
      processingStatus = LatencyProcessingStatus.Ready;
      integration = mxi;
      if (integration.matchingEngine.EdgeEventsConnection == null)
      {
        Debug.Log("EdgeEventsConnection is null");
        return;
      }
      DeviceInfoIntegration deviceInfo = new DeviceInfoIntegration();
      DeviceStaticInfo deviceStaticInfo = deviceInfo.GetDeviceStaticInfo();
      DeviceDynamicInfo deviceDynamicInfo = deviceInfo.GetDeviceDynamicInfo();
      integration.matchingEngine.EdgeEventsConnection.Open(deviceStaticInfo, deviceDynamicInfo);
      config = MobiledgeXIntegration.settings.edgeEventsConfig;
      currentFindCloudlet = integration.latestFindCloudletReply;
      //Log summary of EdgeEvents
      string configSummary = "EdgeEvents Config Summary:\n Latency Test Port: " + config.latencyTestPort;
      configSummary += "\n EdgeEvents Config: Latency Threshold Trigger (Milliseconds): " + config.latencyThresholdTriggerMs;
      configSummary += "\n EdgeEvents Config: Latency Update Pattern: " + config.latencyConfig.updatePattern;
      configSummary += "\n EdgeEvents Config: Latency Max. Number of Updates: " + config.latencyConfig.maxNumberOfUpdates;
      configSummary += "\n EdgeEvents Config: Latency Update Interval (Seconds): " + config.latencyConfig.updateIntervalSeconds;
      configSummary += "\n EdgeEvents Config: Location Update Pattern: " + config.locationConfig.updatePattern;
      configSummary += "\n EdgeEvents Config: Location Max. Number of Updates: " + config.locationConfig.maxNumberOfUpdates;
      configSummary += "\n EdgeEvents Config: Location Update Interval (Seconds): " + config.locationConfig.updateIntervalSeconds;
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
      await StartEdgeEventsLatency(connection, host);
      await StartEdgeEventsLocation(connection);
    }

    async Task StartEdgeEventsLocation(EdgeEventsConnection connection)
    {
      await connection.PostLocationUpdate(location).ConfigureAwait(false);
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

    async Task StartEdgeEventsLatency(EdgeEventsConnection connection, string host)
    {
      Logger.Log("EdgeEvents Posting latency update," +
        "Host : " + host +
        ", Location to send [" + location.Latitude + ", " + location.Longitude + "]");
      bool requestSent;
      if (hasTCPPorts)
      {
        requestSent = await connection.TestConnectAndPostLatencyUpdate(host, (uint)config.latencyTestPort, location).ConfigureAwait(false);
        Logger.Log("TestConnectAndPostLatencyUpdate : " + requestSent);
      }
      else
      {
        requestSent = await connection.TestPingAndPostLatencyUpdate(host, location).ConfigureAwait(false);
        Logger.Log("TestPingAndPostLatencyUpdate : " + requestSent);
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
      Logger.Log("EdgeEvents Posting location update, Location to send [" + location.Latitude + ", " + location.Longitude+"]");
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
      Logger.Log("EdgeEvents Posting latency update," +
        "Host : " + host +
        ", Location to send [" + location.Latitude + ", " + location.Longitude + "]");
      latencyUpdatesCounter++;
      yield return StartCoroutine(OnIntervalEdgeEventsLatency(connection, host));
    }

    void HandleReceivedEvents(ServerEdgeEvent edgeEvent)
    {
      Logger.Log("Received event type: " + edgeEvent.EventType);
      List<FindCloudletEventTrigger> fcTriggers = config.newFindCloudletEventTriggers;
      switch (edgeEvent.EventType)
      {
        case ServerEventType.EventInitConnection:
          Logger.Log("Successfully initiated persistent edge event connection");
          return;
        case ServerEventType.EventLatencyRequest:
          if (fcTriggers.Contains(FindCloudletEventTrigger.LatencyTooHigh))
          {
            StartCoroutine(RespondToLatencyRequest());
          }
          return;
        case ServerEventType.EventLatencyProcessed:
          if (fcTriggers.Contains(FindCloudletEventTrigger.LatencyTooHigh))
          {
            if (processingStatus == LatencyProcessingStatus.Ready)
            {
              latestServerStats = edgeEvent.Statistics;
              processingStatus = LatencyProcessingStatus.Start;
            }
            else
            {
              Logger.Log("Latency processing skipped, still processing the previous Server Edge Event");
            }
          }
          return;
        case ServerEventType.EventAppinstHealth:
          if (fcTriggers.Contains(FindCloudletEventTrigger.AppInstHealthChanged))
          {
            if (edgeEvent.NewCloudlet == null || edgeEvent.ErrorMsg != "")
            {
              PropagateError(FindCloudletEventTrigger.AppInstHealthChanged, "Received EventAppinstHealth Event but NewCloudlet is null, " + edgeEvent.ErrorMsg);
              return;
            }
            PropagateSuccess(FindCloudletEventTrigger.AppInstHealthChanged, edgeEvent.NewCloudlet);
          }
          return;
        case ServerEventType.EventCloudletMaintenance:
          if (fcTriggers.Contains(FindCloudletEventTrigger.CloudletMaintenanceStateChanged))
          {
            if (edgeEvent.NewCloudlet == null || edgeEvent.ErrorMsg != "")
            {
              PropagateError(FindCloudletEventTrigger.CloudletMaintenanceStateChanged, "Received CloudletStateChanged Event but NewCloudlet is null, " + edgeEvent.ErrorMsg);
              return;
            }
            PropagateSuccess(FindCloudletEventTrigger.CloudletMaintenanceStateChanged, edgeEvent.NewCloudlet);
          }
          return;
        case ServerEventType.EventCloudletState:
          if (fcTriggers.Contains(FindCloudletEventTrigger.CloudletStateChanged))
          {
            if (edgeEvent.NewCloudlet == null || edgeEvent.ErrorMsg != "")
            {
              PropagateError(FindCloudletEventTrigger.CloudletStateChanged, "Received CloudletStateChanged Event but NewCloudlet is null, " + edgeEvent.ErrorMsg);
              return;
            }
            PropagateSuccess(FindCloudletEventTrigger.CloudletStateChanged, edgeEvent.NewCloudlet);
          }
          return;
        case ServerEventType.EventCloudletUpdate:
          if (fcTriggers.Contains(FindCloudletEventTrigger.CloserCloudlet))
          {
            if (edgeEvent.NewCloudlet == null || edgeEvent.ErrorMsg != "")
            {
              PropagateError(FindCloudletEventTrigger.CloserCloudlet, "Received CloserCloudlet Event but NewCloudlet is null, " + edgeEvent.ErrorMsg);
              return;
            }
            PropagateSuccess(FindCloudletEventTrigger.CloserCloudlet, edgeEvent.NewCloudlet);
          }
          return;
        case ServerEventType.EventError:
          Logger.Log("Received EventError from server, " + edgeEvent.ToString());
          PropagateError(FindCloudletEventTrigger.Error, edgeEvent.ErrorMsg);
          return;
        default:
          Logger.Log("Received Unknown event, "+edgeEvent.ToString());
          return;
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

    async Task ProcessLatency(Statistics stats)
    {
      Logger.Log("Processing Latency Received from Server");
      if (stats.Avg > config.latencyThresholdTriggerMs)
      {
        Logger.Log("Current Latency (" + stats.Avg + ") > LatencyThreshold (" + config.latencyThresholdTriggerMs + ")");
        Logger.Log("Performing FindCloudlet PerformanceMode");
        MobiledgeXIntegration tempMxi = new MobiledgeXIntegration();
        bool regResult = await tempMxi.Register(hostOverride, (uint)portOverride);
        if (!regResult)
        {
          PropagateError(FindCloudletEventTrigger.LatencyTooHigh, "FindCloudletPerformanceMode failed (RegisterClient failed)");
          return;
        }
        tempMxi.UseFindCloudletPerformanceMode(true);
        bool fcSuccess = await tempMxi.FindCloudlet(hostOverride, (uint)portOverride);
        if (!fcSuccess)
        {
          PropagateError(FindCloudletEventTrigger.LatencyTooHigh, "FindCloudletPerformanceMode failed");
          return;
        }
        currentFindCloudlet = tempMxi.latestFindCloudletReply;
        tempMxi.Dispose();
        if (!integration.latestFindCloudletReply.Equals(currentFindCloudlet))
        {
          PropagateError(FindCloudletEventTrigger.LatencyTooHigh,
            "New Cloudlet obtained from FindCloudletPerformanceMode is the same as old cloudlet");
          return;
        }
        else
        {
         await CompareLatencies(stats);
        }
      }
      else
      {
        Logger.Log("Current Latency (" + stats.Avg + ") < LatencyThreshold (" + config.latencyThresholdTriggerMs + ")");
      }
      processingStatus = LatencyProcessingStatus.Ready;
      Logger.Log("Finished processing Latency");
      return;
    }

    async Task CompareLatencies(Statistics receivedStats)
    {
      Logger.Log("Comparing Latencies");
      AppPort appPort;
      Site site;
      var netTest = new NetTest(integration.matchingEngine);
      if (hasTCPPorts)
      {
        appPort = integration.GetAppPort(LProto.Tcp, config.latencyTestPort);
        Logger.Log("Performing Connect Test");
        site = new Site { host = appPort.FqdnPrefix + currentFindCloudlet.Fqdn, port = appPort.PublicPort, testType = TestType.CONNECT };
      }
      else
      {
        appPort = integration.GetAppPort(LProto.Udp);
        Logger.Log("Performing Ping Test");
        site = new Site { host = appPort.FqdnPrefix + currentFindCloudlet.Fqdn, port = appPort.PublicPort, testType = TestType.PING };
      }
      await netTest.TestSite(site);
      double normalizedLatency = receivedStats.Avg - (receivedStats.Avg * config.performanceSwitchMargin);
      if (site.average < normalizedLatency)
      {
        Logger.Log("NewCloudlet Obtained from LatencyTest have better Latency, Connecting to NewCloudlet");
        PropagateSuccess(FindCloudletEventTrigger.LatencyTooHigh, currentFindCloudlet);
      }
      else
      {
        //switch back to the previous cloudlet
        integration.latestFindCloudletReply = currentFindCloudlet;
        PropagateError(FindCloudletEventTrigger.LatencyTooHigh,
          "Latency threshold exceeded, but no other cloudlets have a meaningful improvement in latency");
      }
      return;
    }

    /// <summary>
    /// Terminates EdgeEvents Connection
    /// If you didn't set AutoMigration to true, You need to call StopEdgeEvents() to close the connection before migrating to a new app instance.
    /// </summary>
    public void StopEdgeEvents()
    {
      Logger.Log("Stopping EdgeEvents");
      stopEdgeEventsRequested = true;
      if (integration != null)
      {
        integration.matchingEngine.EdgeEventsConnection.Close();
      }
      latencyUpdatesCounter = 0;
      latencyUpdatesRunning = false;
      locationUpdatesCounter = 0;
      locationUpdatesRunning = false;
      location = null;
    }

    void PropagateSuccess(FindCloudletEventTrigger trigger, FindCloudletReply newCloudlet)
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
        StopEdgeEvents();
        integration.persistentConnection.startStreamingEvents(integration);
      }
      else
      {
        Logger.Log("Migration occurs and autoMigration is set to false, Call StopEdgeEvents() to terminate the current EdgeEventsConnection.");
      }
    }

    void PropagateError(FindCloudletEventTrigger trigger, string error_msg)
    {
      FindCloudletEvent findCloudletEvent = new FindCloudletEvent();
      findCloudletEvent.trigger = trigger;
      Debug.LogError(error_msg);
      EdgeEventsStatus eventStatus = new EdgeEventsStatus(Status.error, error_msg);
      StopEdgeEvents();
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
