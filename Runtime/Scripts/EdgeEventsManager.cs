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
using System.Threading;

namespace MobiledgeX
{
  [AddComponentMenu("MobiledgeX/EdgeEventsManager")]
  public class EdgeEventsManager : MonoBehaviour
  {
    internal Action<ConnectionDetails> startStreamingEvents;
    ConnectionDetails managerConnectionDetails;
    EdgeEventsConfig config;
    static FindCloudletReply FCPerformanceReply;
    public delegate void FCPerformanceCallback(FindCloudletReply findCloudletReply);
    FCPerformanceThreadManager fcThreadManager;//FindCloudletPerformanceMode Thread Manager
    Statistics latestServerStats;
    internal static UpdatesMonitor updatesMonitor;
    static CancellationTokenSource stopUpdatesSource;
    /// <summary>
    /// Location sent to the DME Server, used only if edgeEventsManager.useMobiledgexLocationServices = false
    /// </summary>
    public Loc location;

    /// <summary>
    /// Set to false if you have your own Location handler. If useMobiledgexLocationServices = false, you must set edgeEventsManager.location to a value.
    /// </summary>
    [Tooltip("Set to false if you have your own Location handler.If useMobiledgexLocationServices = false, you must set edgeEventsManager.location to a value.")]
    public bool useMobiledgexLocationServices = true;

    #region MonoBehaviour Callbacks

    private void OnEnable()
    {
      startStreamingEvents += StartEdgeEvents;
      updatesMonitor = new UpdatesMonitor();
    }
    private void OnApplicationQuit()
    {
      if (fcThreadManager != null)
      {
        fcThreadManager.Interrupt();
      }
      updatesMonitor.StopUpdates();
    }

    private void OnDestroy()
    {
      if (fcThreadManager != null)
      {
        fcThreadManager.Interrupt();
      }
      updatesMonitor.StopUpdates();
    }

    private void Update()
    {
      if (UpdatesMonitor.latencyProcessingStatus == UpdatesStatus.Start)
      {
        UpdatesMonitor.latencyProcessingStatus = UpdatesStatus.Running;
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        ProcessLatency(latestServerStats);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
      }
      if (UpdatesMonitor.latencyProcessingStatus == UpdatesStatus.Completed)
      {
        UpdatesMonitor.latencyProcessingStatus = UpdatesStatus.Ready;
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        CompareLatencies(FCPerformanceReply, latestServerStats);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
      }
    }

    private void OnApplicationPause(bool pause)
    {
      if (Application.platform == RuntimePlatform.Android)
      {
        if (pause)
        {
          if (config == null || managerConnectionDetails == null)
          {
            Logger.Log("Application paused but EdgeEvents didn't start, yet");
            return;
          }
          managerConnectionDetails.matchingEngine.EdgeEventsConnection.PauseSendingUpdates();
          updatesMonitor.PauseUpdates();
          stopUpdatesSource.Cancel();
        }
        else
        {
          if (config == null || managerConnectionDetails == null)
          {
            Logger.Log("Application resumed but EdgeEvents didn't start, yet");
            return;
          }
          managerConnectionDetails.matchingEngine.EdgeEventsConnection.ResumeSendingUpdates();
          updatesMonitor.ResumeUpdates();
          EdgeEventsUpdatesDispatcher(managerConnectionDetails, config);
        }
      }
    }
    #endregion

    #region EdgeEventsManager Functions

    public static EdgeEventsError ValidateConfigs(EdgeEventsConfig config, ConnectionDetails connectionDetails)
    {
      if (config.newFindCloudletEventTriggers.Count == 1) //No FindCloudletTrigger except Error (Added By Default)
      {
        Debug.LogError("Missing FindCloudlets Triggers");
        return EdgeEventsError.InvalidEdgeEventsSetup;
      }

      if (config.latencyThresholdTriggerMs <= 0)
      {
        Debug.LogError("LatencyThresholdTriggerMs must be > 0");
        return EdgeEventsError.InvalidLatencyThreshold;
      }

      if (config.performanceSwitchMargin > 1 || config.performanceSwitchMargin < 0)
      {
        Debug.LogError("performanceSwitchMargin must between (0 to 1.0f)");
        return EdgeEventsError.InvalidPerformanceSwitchMargin;
      }
      //latency config validation
      if (config.newFindCloudletEventTriggers.Contains(FindCloudletEventTrigger.LatencyTooHigh))
      {
        if (config.latencyConfig.updatePattern == UpdatePattern.OnInterval)
        {
          if (config.latencyConfig.maxNumberOfUpdates < 0)
          {
            Debug.LogError("latencyConfig.maxNumberOfUpdates must be >= 0");
            return EdgeEventsError.InvalidEdgeEventsSetup;
          }
          if (config.latencyConfig.updateIntervalSeconds <= 0)
          {
            Debug.LogError("latencyConfig.updateIntervalSeconds must be > 0");
            return EdgeEventsError.InvalidUpdateInterval;
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
            Debug.LogError("Config.maxNumberOfUpdates must be >= 0");
            return EdgeEventsError.InvalidEdgeEventsSetup;
          }
          if (config.locationConfig.updateIntervalSeconds <= 0)
          {
            Debug.LogError("locationConfig.updateIntervalSeconds must be > 0");
            return EdgeEventsError.InvalidUpdateInterval;
          }
        }
      }

      if (connectionDetails.matchingEngine.sessionCookie == null || connectionDetails.matchingEngine.sessionCookie == "")
      {
        Debug.LogError("Missing SessionCookie");
        return EdgeEventsError.MissingSessionCookie;
      }
      if (connectionDetails.matchingEngine.edgeEventsCookie == null || connectionDetails.matchingEngine.edgeEventsCookie == "")
      {
        Debug.LogError("Missing EdgeEvents Cookie");
        return EdgeEventsError.MissingEdgeEventsCookie;
      }
      return EdgeEventsError.None;
    }

    public void StartEdgeEvents(ConnectionDetails connectionDetails)
    {
      Logger.Log("Starting EdgeEvents");
      config = MobiledgeXIntegration.settings.edgeEventsConfig;
      Logger.Log(config.ToString());
      updatesMonitor.Reset();
      managerConnectionDetails = connectionDetails;
      EdgeEventsError validationError = ValidateConfigs(config, managerConnectionDetails);
      if (validationError != EdgeEventsError.None)
      {
        PropagateError(FindCloudletEventTrigger.Error, validationError);
        return;
      }
      if (managerConnectionDetails.matchingEngine.EdgeEventsConnection == null)
      {
        Debug.LogError("MobiledgeX: EdgeEventsConnection is null");
        return;
      }
      managerConnectionDetails.matchingEngine.EdgeEventsReceiver += HandleReceivedEvents;
      DeviceInfoDynamic deviceInfoDynamic = GetDynamicInfo(managerConnectionDetails.matchingEngine);
      DeviceInfoStatic deviceInfoStatic = GetStaticInfo(managerConnectionDetails.matchingEngine);
      bool connectionOpened = managerConnectionDetails.matchingEngine.EdgeEventsConnection.Open(deviceInfoDynamic: deviceInfoDynamic, deviceInfoStatic: deviceInfoStatic);
      if (!connectionOpened)
      {
        Debug.LogError("Failed to OpenEdgeEventsConnection, StoppingEdgeEvents updates");
        return;
      }
      AppPort appPort = GetLatencyTestPort(managerConnectionDetails.mobiledgexManager, config.latencyTestPort);
      if (appPort == null)
      {
        PropagateError(FindCloudletEventTrigger.Error, EdgeEventsError.PortDoesNotExist);
        return;
      }
      managerConnectionDetails.latencyTestPort = appPort.PublicPort;
      managerConnectionDetails.SetAppHost(appPort.PublicPort);
      if (appPort.Proto == LProto.Tcp)
      {
        managerConnectionDetails.hasTCPPort = true;
      }
      Logger.Log("ConnectionDetails: " + managerConnectionDetails.ToString());
    }

    public void EdgeEventsUpdatesDispatcher(ConnectionDetails connectionDetails, EdgeEventsConfig config)
    {
      stopUpdatesSource = new CancellationTokenSource();

      //Latency Updates
      if (config.newFindCloudletEventTriggers.Contains(FindCloudletEventTrigger.LatencyTooHigh))
      {
        updatesMonitor.StartLatencyUpdates(config.latencyConfig);
        if (UpdatesMonitor.latencyUpdatesStatus == UpdatesStatus.Start)
        {
          UpdatesMonitor.latencyUpdatesStatus = UpdatesStatus.Running;
          Logger.Log("Sending LatencySamples every " + config.latencyConfig.updateIntervalSeconds + "second, remaining no. of updates " + UpdatesMonitor.latencyUpdatesCounter);
          Task.Run(async () =>
          {
            await LatencyUpdatesLoop(connectionDetails, UpdatesMonitor.latencyUpdatesCounter, config.latencyConfig.updateIntervalSeconds, stopUpdatesSource.Token);
            Logger.Log("Stopped sending LatencySamples, current latency updates status is " + UpdatesMonitor.latencyUpdatesStatus
              + ", no. of samples sent since first starting edge events : "
              + ((config.latencyConfig.maxNumberOfUpdates == 0 ? int.MaxValue : config.latencyConfig.maxNumberOfUpdates) - UpdatesMonitor.latencyUpdatesCounter));
          });
        }
        else
        {
          Logger.Log("Not Starting LatencyUpdatesLoop, LatencyUpdates status is " + UpdatesMonitor.latencyUpdatesStatus);
        }
      }
      else
      {
        Logger.Log("LatencyTooHigh is not a trigger, skipping latency updates");
      }

      //Location Updates
      if (config.newFindCloudletEventTriggers.Contains(FindCloudletEventTrigger.CloserCloudlet))
      {
        updatesMonitor.StartLocationUpdates(config.locationConfig);
        if (UpdatesMonitor.locationUpdatesStatus == UpdatesStatus.Start)
        {
          UpdatesMonitor.locationUpdatesStatus = UpdatesStatus.Running;
          Logger.Log("Sending LocationSamples every " + config.locationConfig.updateIntervalSeconds + " second");
          Task.Run(async () =>
          {
            await LocationUpdatesLoop(connectionDetails, UpdatesMonitor.locationUpdatesCounter, config.locationConfig.updateIntervalSeconds, stopUpdatesSource.Token);
            Logger.Log("Stopped sending LocationSamples, LocationUpdates status is " + UpdatesMonitor.locationUpdatesStatus
              + ", no. of samples sent since first starting edge events : "
              + ((config.locationConfig.maxNumberOfUpdates == 0 ? int.MaxValue : config.locationConfig.maxNumberOfUpdates) - UpdatesMonitor.locationUpdatesCounter));
          });
        }
        else
        {
          Logger.Log("Not Starting  LocationUpdatesLoop , LocationUpdates Status is " + UpdatesMonitor.locationUpdatesStatus);
        }
      }
      else
      {
        Logger.Log("CloserCloudlet is not a trigger, skipping location updates");
      }
    }

    public async Task LatencyUpdatesLoop(ConnectionDetails connectionDetails, int latencyUpdatesCounter, int updateIntervalSeconds, CancellationToken token)
    {
      TimeSpan updateTimeSpan = new TimeSpan(0, 0, updateIntervalSeconds);
      DeviceInfoDynamic deviceInfoDynamic;
      LProto proto;
      if (connectionDetails.hasTCPPort)
      {
        proto = LProto.Tcp;
      }
      else
      {
        proto = LProto.Udp;
      }
      System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
      while (latencyUpdatesCounter > 0)
      {
        stopwatch.Start();
        deviceInfoDynamic = GetDynamicInfo(connectionDetails.matchingEngine);
        await SendLatencySamples(connectionDetails.matchingEngine.EdgeEventsConnection, connectionDetails.appHost, (uint)connectionDetails.latencyTestPort, proto, deviceInfoDynamic, location).ConfigureAwait(false);
        latencyUpdatesCounter--;
        while (stopwatch.Elapsed < updateTimeSpan)
        {
          if (token.IsCancellationRequested)
          {
            return;
          }
          //else continue
        }
        stopwatch.Reset();
      }
      UpdatesMonitor.latencyUpdatesCounter = latencyUpdatesCounter;
    }


    public async Task LocationUpdatesLoop(ConnectionDetails connectionDetails, int locationUpdatesCounter, int updateIntervalSeconds, CancellationToken token)
    {
      TimeSpan updateTimeSpan = new TimeSpan(0, 0, updateIntervalSeconds);
      DeviceInfoDynamic deviceInfoDynamic;
      System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
      while (locationUpdatesCounter > 0)
      {
        stopwatch.Start();
        deviceInfoDynamic = GetDynamicInfo(connectionDetails.matchingEngine);
        await SendLocationSamples(connectionDetails.matchingEngine.EdgeEventsConnection, deviceInfoDynamic, location).ConfigureAwait(false);
        locationUpdatesCounter--;
        while (stopwatch.Elapsed < updateTimeSpan)
        {
          if (token.IsCancellationRequested)
          {
            return;
          }
          //else continue
        }
        stopwatch.Reset();
      }
      UpdatesMonitor.locationUpdatesCounter = locationUpdatesCounter;
    }


    async static Task<bool> SendLocationSamples(EdgeEventsConnection connection, DeviceInfoDynamic deviceInfoDynamic, Loc location)
    {
      if (location == null)
      {
        Debug.LogError("Mobiledgex: Location is not Configured, skipping");
        return false;
      }

      if (deviceInfoDynamic == null)
      {
        Debug.LogError("Mobiledgex: deviceInfoDynamic is not Configured, skipping");
        return false;
      }
      Logger.Log("Posting location update, " +
        "Location : [" + location.Latitude + ", " + location.Longitude + "]" +
        ", DeviceDynamicInfo: CarrierName: " + deviceInfoDynamic.CarrierName +
        " DataNetworkType: " + deviceInfoDynamic.DataNetworkType +
        " SignalStrength: " + deviceInfoDynamic.SignalStrength);
      await connection.PostLocationUpdate(location, deviceInfoDynamic).ConfigureAwait(false);
      return true;
    }

    public async static Task<bool> SendLatencySamples(EdgeEventsConnection connection, string host, uint latencyTestPort, LProto proto, DeviceInfoDynamic deviceInfoDynamic, Loc location)
    {
      if (host == "" || host == null)
      {
        Debug.LogError("Mobiledgex: Application Test Port is not Configured, skipping");
        return false;
      }
      if (latencyTestPort == 0)
      {
        Debug.LogError("Mobiledgex: Application Test Port is not Configured, skipping");
        return false;
      }

      if (location == null)
      {
        Debug.LogError("Mobiledgex: Location is not Configured, skipping");
        return false;
      }

      if (deviceInfoDynamic == null)
      {
        Debug.LogError("Mobiledgex: deviceInfoDynamic is not Configured, skipping");
        return false;
      }
      Logger.Log("Posting latency update. " +
       "Location : [" + location.Latitude + ", " + location.Longitude + "]" +
       ", DeviceDynamicInfo: CarrierName: " + deviceInfoDynamic.CarrierName +
       " DataNetworkType: " + deviceInfoDynamic.DataNetworkType +
       " SignalStrength: " + deviceInfoDynamic.SignalStrength +
       ", Host: " + host +
       ", Port: " + latencyTestPort +
       ", Proto: " + proto.ToString());
      switch (proto)
      {
        case LProto.Tcp:
          await connection.TestConnectAndPostLatencyUpdate(
           host: host,
           port: latencyTestPort,
           location: location,
           numSamples: 1,
           deviceInfoDynamic: deviceInfoDynamic
           ).ConfigureAwait(false);
          return true;
        case LProto.Udp:
          await connection.TestPingAndPostLatencyUpdate(
             host: host,
             location: location,
             numSamples: 1,
             deviceInfoDynamic: deviceInfoDynamic
             ).ConfigureAwait(false);
          return true;
        default:
          return false;//Unknown Proto
      }
    }

    public static AppPort GetLatencyTestPort(MobiledgeXIntegration mobiledgexManager, int latencyPortNumber = 0, LProto proto = LProto.Tcp)
    {
      AppPort appPort;
      if (mobiledgexManager.latestFindCloudletReply == null)
      {
        return null;
      }
      try
      {
        appPort = mobiledgexManager.GetAppPort(proto, latencyPortNumber);
      }
      catch (AppPortException)
      {
        appPort = null;
      }

      if (appPort == null)
      {
        if (proto == LProto.Tcp)
        {
          Logger.LogWarning("No TCP ports exists on your App, It's recommended to use TCP ports for latency testing as Connect Tests is more reliable than Ping Tests.");
          return GetLatencyTestPort(mobiledgexManager, latencyPortNumber, LProto.Udp);// try UDP proto
        }
        else
        {
          return null;
        }
      }
      if (appPort == null && proto == LProto.Udp)
      {
        Debug.LogError("Test port doesn't exist");
      }
      return appPort;
    }

    // <summary>
    // Terminates EdgeEvents Connection
    // If you didn't set AutoMigration to true, You need to call StopEdgeEvents() to close the connection before migrating to a new app instance.
    // </summary>
    public void StopEdgeEvents()
    {
      Logger.Log("Stopping EdgeEvents");
      managerConnectionDetails.matchingEngine.EdgeEventsConnection.PauseSendingUpdates();
      if (stopUpdatesSource != null)
      {
        stopUpdatesSource.Cancel();
      }
      updatesMonitor.StopUpdates();

      if (managerConnectionDetails != null)
      {
        managerConnectionDetails.matchingEngine.EdgeEventsReceiver -= HandleReceivedEvents;
      }
      managerConnectionDetails.matchingEngine.EdgeEventsConnection.Close();
    }

    void HandleReceivedEvents(ServerEdgeEvent edgeEvent)
    {
      Debug.Log("Received event: " + edgeEvent.EventType
        + "\nStatistics: " + edgeEvent.Statistics
        + "\nHealthCheck: " + edgeEvent.HealthCheck
        + "\nMaintenanceState: " + edgeEvent.MaintenanceState
        + "\nNewCloudlet: " + edgeEvent.NewCloudlet
        + "\nCloudletState: " + edgeEvent.CloudletState
        + "\nErrorMsg: " + edgeEvent.ErrorMsg);
      if (config == null)
      {
        Debug.LogError("MobiledgeX: Couldn't locate EdgeEvents Config");
        return;
      }
      List<FindCloudletEventTrigger> fcTriggers = config.newFindCloudletEventTriggers;
      switch (edgeEvent.EventType)
      {
        case ServerEventType.EventInitConnection:
          Logger.Log("Successfully initiated EdgeEventConnection");
          UpdatesMonitor.edgeEventConnectionInitiated = true;
          EdgeEventsUpdatesDispatcher(managerConnectionDetails, config);
          return;
        case ServerEventType.EventLatencyRequest:
          if (fcTriggers.Contains(FindCloudletEventTrigger.LatencyTooHigh))
          {
            Logger.Log("Received EventLatencyRequest from Server");
            StartCoroutine(EdgeEventsUtil.RespondToLatencyRequest(managerConnectionDetails, location, config.latencyTestPort));
          }
          return;
        case ServerEventType.EventLatencyProcessed:
          if (fcTriggers.Contains(FindCloudletEventTrigger.LatencyTooHigh))
          {
            if (UpdatesMonitor.latencyProcessingStatus == UpdatesStatus.Ready)
            {
              latestServerStats = edgeEvent.Statistics;
              UpdatesMonitor.latencyProcessingStatus = UpdatesStatus.Start;
            }
            else
            {
              Logger.Log("Latency processing skipped, still processing the previous ServerEdgeEvent");
            }
          }
          return;
        case ServerEventType.EventAppinstHealth:
          if (fcTriggers.Contains(FindCloudletEventTrigger.AppInstHealthChanged))
          {
            if (edgeEvent.NewCloudlet == null || edgeEvent.ErrorMsg != "")
            {
              Debug.LogError("Received EventAppinstHealth Event but NewCloudlet is null, " + edgeEvent.ErrorMsg);
              PropagateError(FindCloudletEventTrigger.AppInstHealthChanged, EdgeEventsError.EventTriggeredButFindCloudletError);
              if (edgeEvent.HealthCheck == HealthCheck.FailRootlbOffline
                || edgeEvent.HealthCheck == HealthCheck.FailServerFail
                || edgeEvent.HealthCheck == HealthCheck.CloudletOffline
                || edgeEvent.HealthCheck == HealthCheck.Unknown)
              {
                UpdatesMonitor.latencyUpdatesStatus = UpdatesStatus.Stopped;
              }
            }
            else
            {
              PropagateSuccess(FindCloudletEventTrigger.AppInstHealthChanged, edgeEvent.NewCloudlet);
            }
          }
          return;
        case ServerEventType.EventCloudletMaintenance:
          if (fcTriggers.Contains(FindCloudletEventTrigger.CloudletMaintenanceStateChanged))
          {
            if (edgeEvent.NewCloudlet == null || edgeEvent.ErrorMsg != "")
            {
              Debug.LogError("Received CloudletStateChanged Event but NewCloudlet is null, " + edgeEvent.ErrorMsg);
              PropagateError(FindCloudletEventTrigger.CloudletMaintenanceStateChanged, EdgeEventsError.EventTriggeredButFindCloudletError);
              if (edgeEvent.MaintenanceState == MaintenanceState.MaintenanceStart
                  || edgeEvent.MaintenanceState == MaintenanceState.CrmUnderMaintenance
                  || edgeEvent.MaintenanceState == MaintenanceState.FailoverError
                  || edgeEvent.MaintenanceState == MaintenanceState.UnderMaintenance)
              {
                UpdatesMonitor.latencyUpdatesStatus = UpdatesStatus.Stopped;
              }

            }
            else
            {
              PropagateSuccess(FindCloudletEventTrigger.CloudletMaintenanceStateChanged, edgeEvent.NewCloudlet);
            }

          }
          return;
        case ServerEventType.EventCloudletState:
          if (fcTriggers.Contains(FindCloudletEventTrigger.CloudletStateChanged))
          {
            if (edgeEvent.NewCloudlet == null || edgeEvent.ErrorMsg != "")
            {
              Debug.LogError("Received CloudletStateChanged Event but NewCloudlet is null, " + edgeEvent.ErrorMsg);
              PropagateError(FindCloudletEventTrigger.CloudletStateChanged, EdgeEventsError.EventTriggeredButFindCloudletError);
              if (edgeEvent.CloudletState == CloudletState.Errors
                  || edgeEvent.CloudletState == CloudletState.NotPresent
                  || edgeEvent.CloudletState == CloudletState.Offline
                  || edgeEvent.CloudletState == CloudletState.Unknown)
              {
                UpdatesMonitor.latencyUpdatesStatus = UpdatesStatus.Stopped;
              }
            }
            else
            {
              PropagateSuccess(FindCloudletEventTrigger.CloudletStateChanged, edgeEvent.NewCloudlet);
            }
          }
          return;
        case ServerEventType.EventCloudletUpdate:
          if (fcTriggers.Contains(FindCloudletEventTrigger.CloserCloudlet))
          {
            if (edgeEvent.NewCloudlet == null || edgeEvent.ErrorMsg != "" || edgeEvent.NewCloudlet.Fqdn == managerConnectionDetails.mobiledgexManager.latestFindCloudletReply.Fqdn)
            {
              Debug.LogError("Received CloserCloudlet Event but NewCloudlet is null or the same, " + edgeEvent.ErrorMsg);
              PropagateError(FindCloudletEventTrigger.CloserCloudlet, EdgeEventsError.EventTriggeredButCurrentCloudletIsBest);
            }
            else
            {
              PropagateSuccess(FindCloudletEventTrigger.CloserCloudlet, edgeEvent.NewCloudlet);
            }
          }
          return;
        case ServerEventType.EventError:
          Logger.Log("Received EventError from server, Error : " + edgeEvent.ErrorMsg);
          PropagateError(FindCloudletEventTrigger.Error, EdgeEventsError.EventError);
          return;
        default:
          Logger.Log("Received Unknown event, event type: " + edgeEvent.EventType);
          return;
      }
    }

    //Compares between the FindCloudletPerformance Cloudlet and the Current Cloudlet(last stats received from the server)
    async Task CompareLatencies(FindCloudletReply fcPerformanceReply, Statistics receivedStats)
    {
      if (FCPerformanceReply == null)
      {
        Debug.LogError("Error In FindCloudlet Perfromance Mode, FindCloudlet Reply is null");
        PropagateError(FindCloudletEventTrigger.LatencyTooHigh, EdgeEventsError.EventTriggeredButFindCloudletError);
        return;
      }

      if (FCPerformanceReply.Status == FindCloudletReply.Types.FindStatus.FindNotfound || FCPerformanceReply.Status == FindCloudletReply.Types.FindStatus.FindUnknown)
      {
        Debug.LogError("Error In FindCloudlet Perfromance Mode, FindCloudlet Status is " + FCPerformanceReply.Status);
        PropagateError(FindCloudletEventTrigger.LatencyTooHigh, EdgeEventsError.EventTriggeredButFindCloudletError);
        return;
      }

      if (managerConnectionDetails.mobiledgexManager.latestFindCloudletReply.Fqdn.Equals(FCPerformanceReply.Fqdn))
      {
        Debug.LogError("New Cloudlet obtained from FindCloudletPerformanceMode is the same as old cloudlet");
        PropagateError(FindCloudletEventTrigger.LatencyTooHigh, EdgeEventsError.EventTriggeredButCurrentCloudletIsBest);
        return;
      }

      Logger.Log("Comparing Latencies");
      AppPort appPort = GetLatencyTestPort(managerConnectionDetails.mobiledgexManager, config.latencyTestPort);
      Site site;
      var netTest = new NetTest(managerConnectionDetails.matchingEngine);

      if (managerConnectionDetails.hasTCPPort)
      {
        Logger.Log("Performing Connect Test");
        site = new Site { host = appPort.FqdnPrefix + fcPerformanceReply.Fqdn, port = managerConnectionDetails.latencyTestPort, testType = TestType.CONNECT };
      }
      else
      {
        Logger.Log("Performing Ping Test");
        site = new Site { host = appPort.FqdnPrefix + fcPerformanceReply.Fqdn, port = managerConnectionDetails.latencyTestPort, testType = TestType.PING };
      }
      await netTest.TestSite(site);
      double normalizedLatency = receivedStats.Avg - (receivedStats.Avg * config.performanceSwitchMargin);
      if (site.average < normalizedLatency)
      {
        Logger.Log("NewCloudlet Obtained from LatencyTest have better Latency, Connecting to NewCloudlet");
        PropagateSuccess(FindCloudletEventTrigger.LatencyTooHigh, fcPerformanceReply);
      }
      else
      {
        Debug.LogError("Latency threshold exceeded, but no other cloudlets have a meaningful improvement in latency");
        PropagateError(FindCloudletEventTrigger.LatencyTooHigh, EdgeEventsError.EventTriggeredButCurrentCloudletIsBest);
      }
      return;
    }

    void PropagateSuccess(FindCloudletEventTrigger trigger, FindCloudletReply newCloudlet)
    {
      Logger.Log("Received NewCloudlet by trigger: " + trigger.ToString() + ", NewCloudlet" + newCloudlet.ToString());
      FindCloudletEvent findCloudletEvent = new FindCloudletEvent();
      findCloudletEvent.trigger = trigger;
      findCloudletEvent.newCloudlet = newCloudlet;
      EdgeEventsStatus eventStatus = new EdgeEventsStatus(Status.success);
      try
      {
        managerConnectionDetails.mobiledgexManager.NewFindCloudletHandler(eventStatus, findCloudletEvent);
      }
      catch (NullReferenceException)
      {
        Debug.LogError("Missing NewCloudletHandler, " +
          "Listen to EdgeEvents by using \"mobiledgexIntegration.NewCloudletHandler += HandleNewFindCloudlet;\"");
      }
      if (config.autoMigration)
      {
        managerConnectionDetails.mobiledgexManager.latestFindCloudletReply = newCloudlet;
        StopEdgeEvents();
        Logger.Log("Restart EdgeEventsConnection, newCloudlet.EdgeEventsCookie: " + newCloudlet.EdgeEventsCookie
          + ", dmeHostOverride: " + managerConnectionDetails.dmeHostOverride
          + ", dmePortOverride: " + managerConnectionDetails.dmePortOverride);
        managerConnectionDetails.matchingEngine.RestartEdgeEventsConnection(newCloudlet, managerConnectionDetails.dmeHostOverride, managerConnectionDetails.dmePortOverride);
        StartEdgeEvents(managerConnectionDetails);
      }
      else
      {
        StopEdgeEvents();
        Logger.Log("Migration occured and autoMigration is set to false, you can manually call StartEdgeEvents method.");
      }
    }

    void PropagateError(FindCloudletEventTrigger trigger, EdgeEventsError error)
    {
      FindCloudletEvent findCloudletEvent = new FindCloudletEvent();
      findCloudletEvent.trigger = trigger;
      Debug.LogError(error);
      EdgeEventsStatus eventStatus = new EdgeEventsStatus(Status.error, error);
      try
      {
        managerConnectionDetails.mobiledgexManager.NewFindCloudletHandler(eventStatus, findCloudletEvent);
      }
      catch (NullReferenceException)
      {
        Debug.LogError("Missing NewCloudletHandler, " +
          "Listen to EdgeEventsHandler by using \"mobiledgexIntegration.NewCloudletHandler += HandleNewFindCloudlet;\"");
        return;
      }
    }

    void ProcessLatency(Statistics stats)
    {
      Logger.Log("Processing Latency Received from Server");
      if (stats.Avg > config.latencyThresholdTriggerMs)
      {
        Logger.Log("Current Latency (" + stats.Avg + ") > LatencyThreshold (" + config.latencyThresholdTriggerMs + ")");
        Logger.Log("Performing FindCloudlet PerformanceMode");
        FCPerformanceThreadManager fcThreadObj = new FCPerformanceThreadManager
          (matchingEngine: managerConnectionDetails.matchingEngine, location: location, hostOverride: managerConnectionDetails.dmeHostOverride, portOverride: managerConnectionDetails.dmePortOverride, callbackDelegate: FCCallback, latencyTestPort: config.latencyTestPort);
        fcThreadObj.RunFCPerformance();
      }
      else
      {
        Logger.Log("Current Latency (" + stats.Avg + ") < LatencyThreshold (" + config.latencyThresholdTriggerMs + ")");
        UpdatesMonitor.latencyUpdatesStatus = UpdatesStatus.Ready;
      }
      return;
    }

    /// <summary>
    /// Helper function for acquiring DeviceDynamicInfo in Multithreading environment to avoid stale references on Android Devices
    /// </summary>
    /// <param name="matchingEngine"></param>
    /// <returns>DeviceInfoDynamic Object</returns>
    public static DeviceInfoDynamic GetDynamicInfo(MatchingEngine matchingEngine, EdgeEventsConfig config = null)
    {
      if (config == null)
      {
        config = MobiledgeXIntegration.settings.edgeEventsConfig;
        if (config == null)
        {
          throw new NullReferenceException("Error finding EdgeEventsConfig, couldn't load MobiledgeX Settings");
        }
      }

      DeviceInfoDynamic deviceInfoDynamic;
      if (Application.platform == RuntimePlatform.Android)
      {
        deviceInfoDynamic = Task.Run(() =>
        {
          AndroidJNI.AttachCurrentThread();
          DeviceInfoDynamic result = matchingEngine.deviceInfo.GetDeviceInfoDynamic();
          AndroidJNI.DetachCurrentThread();
          return result;
        }).Result;
      }
      else
      {
        deviceInfoDynamic = matchingEngine.deviceInfo.GetDeviceInfoDynamic();
      }
      if (config.useAnyCarrier)
      {
        deviceInfoDynamic.CarrierName = "";
      }
      return deviceInfoDynamic;
    }

    public static DeviceInfoStatic GetStaticInfo(MatchingEngine matchingEngine)
    {
      DeviceInfoStatic deviceInfoStatic;
      if (Application.platform == RuntimePlatform.Android)
      {
        deviceInfoStatic = Task.Run(() =>
        {
          AndroidJNI.AttachCurrentThread();
          DeviceInfoStatic result = matchingEngine.GetDeviceInfoStatic();
          AndroidJNI.DetachCurrentThread();
          return result;
        }).Result;
      }
      else
      {
        deviceInfoStatic = matchingEngine.GetDeviceInfoStatic();
      }
      return deviceInfoStatic;
    }

    // This callback is emitted from a ThreadPoolWorker
    private void FCCallback(FindCloudletReply findCloudletReply)
    {
      UpdatesMonitor.latencyProcessingStatus = UpdatesStatus.Completed;
      FCPerformanceReply = findCloudletReply;
      if (fcThreadManager != null)
      {
        fcThreadManager.Interrupt();
      }
    }
  }
  #endregion
}
