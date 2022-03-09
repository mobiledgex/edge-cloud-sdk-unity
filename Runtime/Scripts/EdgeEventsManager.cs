/**
 * Copyright 2018-2022 MobiledgeX, Inc. All rights and licenses reserved.
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
using static MobiledgeX.EdgeEventsError;
using static MobiledgeX.FindCloudletEventTrigger;
using DistributedMatchEngine.PerformanceMetrics;
using System.Threading;

namespace MobiledgeX
{
  [DisallowMultipleComponent]
  [AddComponentMenu("MobiledgeX/EdgeEventsManager")]
  public class EdgeEventsManager : MonoBehaviour
  {
    /// <summary>
    /// Location sent to the DME Server, used only if edgeEventsManager.useMobiledgexLocationServices = false
    /// </summary>
    public Loc location;

    /// <summary>
    /// Set to false if you have your own Location handler. If useMobiledgexLocationServices = false, you must set edgeEventsManager.location to a value.
    /// </summary>
    [Tooltip("Set to false if you have your own Location handler.If useMobiledgexLocationServices = false, you must set edgeEventsManager.location to a value.")]
    public bool useMobiledgexLocationServices = true;

    internal delegate void FCPerformanceCallback(FindCloudletReply findCloudletReply);
    internal Action<ConnectionDetails> startStreamingEvents;
    private ConnectionDetails connectionDetails;
    private EdgeEventsConfig config;
    private static FindCloudletReply FCPerformanceReply;
    private FCPerformanceThreadManager fcThreadManager;//FindCloudletPerformanceMode Thread Manager
    private Statistics latestServerStats;
    internal static UpdatesMonitor updatesMonitor;
    private static CancellationTokenSource stopUpdatesSource;
    internal MobiledgeX.LocationService locationService;
    private static int uiThread;
    private static bool IsMainThread
    {
      get { return Thread.CurrentThread.ManagedThreadId == uiThread; }
    }

    #region MonoBehaviour Callbacks

    private void OnEnable()
    {
      uiThread = Thread.CurrentThread.ManagedThreadId;
      startStreamingEvents += StartEdgeEvents;
      updatesMonitor = new UpdatesMonitor();
      if (useMobiledgexLocationServices)
      {
        locationService = FindObjectOfType<MobiledgeX.LocationService>();
        config = MobiledgeXIntegration.settings.edgeEventsConfig;
        if (locationService == null)
        {
          throw new EdgeEventsException("EdgeEventsManager.useMobiledgexLocationServices is set to true but no Active LocationService component in the scene.");
        }
        if (config.locationConfig.updatePattern == UpdatePattern.OnTrigger
        && config.latencyConfig.updatePattern == UpdatePattern.OnTrigger)
        {
          MobiledgeX.LocationService.EnsureLocation(obtainLocationOnce: true);
        }
        else
        {
          StartCoroutine(MobiledgeX.LocationService.EnsureLocation(obtainLocationOnce: false));
        }
      }
    }

    private void OnApplicationQuit()
    {
      if (fcThreadManager != null)
      {
        fcThreadManager.Interrupt();
      }
      if (updatesMonitor != null)
      {
        updatesMonitor.StopUpdates();
      }
    }

    private void OnDestroy()
    {
      if (fcThreadManager != null)
      {
        fcThreadManager.Interrupt();
      }
      if (updatesMonitor != null)
      {
        updatesMonitor.StopUpdates();
      }
      if (locationService != null)
      {
        locationService.StopLocationUpdates();
      }
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
          if (config == null || connectionDetails == null)
          {
            Logger.Log("Application paused but EdgeEvents didn't start, yet");
            return;
          }
          connectionDetails.matchingEngine.EdgeEventsConnection.PauseSendingUpdates();
          updatesMonitor.PauseUpdates();
          stopUpdatesSource.Cancel();
        }
        else
        {
          if (config == null || connectionDetails == null)
          {
            Logger.Log("Application resumed but EdgeEvents didn't start, yet");
            return;
          }
          connectionDetails.matchingEngine.EdgeEventsConnection.ResumeSendingUpdates();
          updatesMonitor.ResumeUpdates();
          EdgeEventsUpdatesDispatcher(connectionDetails, config);
        }
      }
    }

    #endregion

    #region EdgeEventsManager Functions

    /// <summary>
    /// Starts EdgeEvents based on the MobiledgeXSettings.edgeEventsConfig, can be called manually or using EdgeEventsManager Component
    /// StartEdgeEvents can not be called before RegisterAndFindCloudlet
    /// </summary>
    /// <param name="mobiledgeXConnectionDetails"> ConnectionDetails object holding the current state of MatchingEngine </param>
    public void StartEdgeEvents(ConnectionDetails mobiledgeXConnectionDetails)
    {
      Logger.Log("Starting EdgeEvents");
      config = MobiledgeXIntegration.settings.edgeEventsConfig;
      if (config == null)
      {
        throw new EdgeEventsException("MobiledgeX EdgeEventsConfig is Null");
      }
      Logger.Log(config.ToString());
      updatesMonitor.Reset();
      connectionDetails = mobiledgeXConnectionDetails;
      EdgeEventsError validationError = EdgeEventsUtil.ValidateConfigs(config, connectionDetails);
      if (validationError != EdgeEventsError.None)
      {
        PropagateError(Error, validationError, "EdgeEvents Setup error, check logs for more details.");
        return;
      }
      if (connectionDetails.matchingEngine.EdgeEventsConnection == null)
      {
        Debug.LogError("MobiledgeX: EdgeEventsConnection is null");
        return;
      }
      if (IsMainThread) // Input.location can be accessed only from UI Thread
      {
        StartCoroutine(locationService.UpdateEdgeEventsManagerLocation(Math.Min(config.locationConfig.updateIntervalSeconds, config.latencyConfig.updateIntervalSeconds)));
      }
      connectionDetails.matchingEngine.EdgeEventsReceiver += HandleReceivedEvents;
      DeviceInfoDynamic deviceInfoDynamic = GetDeviceDynamicInfo(connectionDetails.matchingEngine);
      DeviceInfoStatic deviceInfoStatic = GetDeviceStaticInfo(connectionDetails.matchingEngine);
      bool connectionOpened = connectionDetails.matchingEngine.EdgeEventsConnection.Open(deviceInfoDynamic: deviceInfoDynamic, deviceInfoStatic: deviceInfoStatic);
      if (!connectionOpened)
      {
        Debug.LogError("Failed to OpenEdgeEventsConnection, StoppingEdgeEvents updates");
        return;
      }
      AppPort appPort = EdgeEventsUtil.GetLatencyTestPort(connectionDetails.mobiledgexManager, config.latencyTestPort);
      if (appPort == null)
      {
        PropagateError(Error, PortDoesNotExist, "Port doesn't exist, make sure config.latencyTestPort is available in your app instance.");
        return;
      }
      connectionDetails.latencyTestPort = appPort.PublicPort;
      connectionDetails.SetAppHost(appPort.PublicPort);
      if (appPort.Proto == LProto.Tcp)
      {
        connectionDetails.hasTCPPort = true;
      }
      Logger.Log("ConnectionDetails: " + connectionDetails.ToString());
    }

    // <summary>
    // Terminates EdgeEvents Connection
    // If you didn't set AutoMigration to true, You need to call StopEdgeEvents() to close the connection before migrating to a new app instance.
    // </summary>
    public void StopEdgeEvents()
    {
      Logger.Log("Stopping EdgeEvents");
      connectionDetails.matchingEngine.EdgeEventsConnection.PauseSendingUpdates();
      if (stopUpdatesSource != null)
      {
        stopUpdatesSource.Cancel();
      }
      updatesMonitor.StopUpdates();

      if (connectionDetails != null)
      {
        connectionDetails.matchingEngine.EdgeEventsReceiver -= HandleReceivedEvents;
      }
      connectionDetails.matchingEngine.EdgeEventsConnection.Close();
    }

    private void EdgeEventsUpdatesDispatcher(ConnectionDetails connectionDetails, EdgeEventsConfig config)
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
            Logger.Log("Stopped sending LatencySamples, current latency updates status is " + UpdatesMonitor.latencyUpdatesStatus);
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
            Logger.Log("Stopped sending LocationSamples, LocationUpdates status is " + UpdatesMonitor.locationUpdatesStatus);
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

    private async Task LatencyUpdatesLoop(ConnectionDetails connectionDetails, int latencyUpdatesCounter, int updateIntervalSeconds, CancellationToken token)
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
        deviceInfoDynamic = GetDeviceDynamicInfo(connectionDetails.matchingEngine);
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

    private async Task LocationUpdatesLoop(ConnectionDetails connectionDetails, int locationUpdatesCounter, int updateIntervalSeconds, CancellationToken token)
    {
      TimeSpan updateTimeSpan = new TimeSpan(0, 0, updateIntervalSeconds);
      DeviceInfoDynamic deviceInfoDynamic;
      System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
      while (locationUpdatesCounter > 0)
      {
        stopwatch.Start();
        deviceInfoDynamic = GetDeviceDynamicInfo(connectionDetails.matchingEngine);
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

    private async static Task<bool> SendLocationSamples(EdgeEventsConnection connection, DeviceInfoDynamic deviceInfoDynamic, Loc location)
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

    private async static Task<bool> SendLatencySamples(EdgeEventsConnection connection, string host, uint latencyTestPort, LProto proto, DeviceInfoDynamic deviceInfoDynamic, Loc location)
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

    private void HandleReceivedEvents(ServerEdgeEvent edgeEvent)
    {
      Logger.Log("Received event: " + edgeEvent.EventType
        + "\nStatistics: " + edgeEvent.Statistics
        + "\nHealthCheck: " + edgeEvent.HealthCheck
        + "\nMaintenanceState: " + edgeEvent.MaintenanceState
        + "\nNewCloudlet: " + edgeEvent.NewCloudlet
        + "\nCloudletState: " + edgeEvent.CloudletState
        + "\nErrorMsg: " + edgeEvent.ErrorMsg);
      string errorMsg = "";
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
          EdgeEventsUpdatesDispatcher(connectionDetails, config);
          return;
        case ServerEventType.EventLatencyRequest:
          if (fcTriggers.Contains(LatencyTooHigh))
          {
            Logger.Log("Received EventLatencyRequest from Server");
            StartCoroutine(EdgeEventsUtil.RespondToLatencyRequest(connectionDetails, location, config.latencyTestPort));
          }
          return;
        case ServerEventType.EventLatencyProcessed:
          if (fcTriggers.Contains(LatencyTooHigh))
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
          if (fcTriggers.Contains(AppInstHealthChanged))
          {
            if (edgeEvent.NewCloudlet == null || edgeEvent.ErrorMsg != "")
            {
              errorMsg = $"Received EventAppinstHealth Event but NewCloudlet is null, {edgeEvent.ErrorMsg}";
              PropagateError(AppInstHealthChanged, EventTriggeredButFindCloudletError, errorMsg);
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
              PropagateSuccess(AppInstHealthChanged, edgeEvent.NewCloudlet);
            }
          }
          return;
        case ServerEventType.EventCloudletMaintenance:
          if (fcTriggers.Contains(CloudletMaintenanceStateChanged))
          {
            if (edgeEvent.NewCloudlet == null || edgeEvent.ErrorMsg != "")
            {
              errorMsg = $"Received CloudletStateChanged Event but NewCloudlet is null, {edgeEvent.ErrorMsg}";
              PropagateError(CloudletMaintenanceStateChanged, EventTriggeredButFindCloudletError, errorMsg);
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
              PropagateSuccess(CloudletMaintenanceStateChanged, edgeEvent.NewCloudlet);
            }

          }
          return;
        case ServerEventType.EventCloudletState:
          if (fcTriggers.Contains(CloudletStateChanged))
          {
            if (edgeEvent.NewCloudlet == null || edgeEvent.ErrorMsg != "")
            {
              errorMsg = $"Received CloudletStateChanged Event but NewCloudlet is null, {edgeEvent.ErrorMsg}";
              PropagateError(CloudletStateChanged, EventTriggeredButFindCloudletError, errorMsg);
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
              PropagateSuccess(CloudletStateChanged, edgeEvent.NewCloudlet);
            }
          }
          return;
        case ServerEventType.EventCloudletUpdate:
          if (fcTriggers.Contains(CloserCloudlet))
          {
            if (edgeEvent.NewCloudlet == null || edgeEvent.ErrorMsg != "" || edgeEvent.NewCloudlet.Fqdn == connectionDetails.mobiledgexManager.latestFindCloudletReply.Fqdn)
            {
              errorMsg = $"Received CloserCloudlet Event but NewCloudlet is null or the same, {edgeEvent.ErrorMsg}";
              PropagateError(CloserCloudlet, EventTriggeredButCurrentCloudletIsBest, errorMsg);
            }
            else
            {
              PropagateSuccess(CloserCloudlet, edgeEvent.NewCloudlet);
            }
          }
          return;
        case ServerEventType.EventError:
          errorMsg = $"Received EventError from server, Error :{edgeEvent.ErrorMsg}";
          PropagateError(Error, EventError, errorMsg);
          return;
        default:
          Logger.LogWarning($"Received Unknown event, event type: {edgeEvent.EventType}");
          return;
      }
    }

    //Compares between the FindCloudletPerformance Cloudlet and the Current Cloudlet(last stats received from the server)
    private async Task CompareLatencies(FindCloudletReply fcPerformanceReply, Statistics receivedStats)
    {
      string errorMsg = "";
      if (FCPerformanceReply == null)
      {
        errorMsg = "Error In FindCloudlet Perfromance Mode, FindCloudlet Reply is null";
        PropagateError(LatencyTooHigh, EventTriggeredButFindCloudletError, errorMsg);
        return;
      }

      if (FCPerformanceReply.Status == FindCloudletReply.Types.FindStatus.FindNotfound || FCPerformanceReply.Status == FindCloudletReply.Types.FindStatus.FindUnknown)
      {
        errorMsg = $"Error In FindCloudlet Perfromance Mode, FindCloudlet Status is {FCPerformanceReply.Status}";
        PropagateError(LatencyTooHigh, EventTriggeredButFindCloudletError, errorMsg);
        return;
      }

      if (connectionDetails.mobiledgexManager.latestFindCloudletReply.Fqdn.Equals(FCPerformanceReply.Fqdn))
      {
        errorMsg = "New Cloudlet obtained from FindCloudletPerformanceMode is the same as old cloudlet";
        PropagateError(LatencyTooHigh, EventTriggeredButCurrentCloudletIsBest, errorMsg);
        return;
      }

      Logger.Log("Comparing Latencies");
      AppPort appPort = EdgeEventsUtil.GetLatencyTestPort(connectionDetails.mobiledgexManager, config.latencyTestPort);
      Site site;
      var netTest = new NetTest(connectionDetails.matchingEngine);

      if (connectionDetails.hasTCPPort)
      {
        Logger.Log("Performing Connect Test");
        site = new Site { host = appPort.FqdnPrefix + fcPerformanceReply.Fqdn, port = connectionDetails.latencyTestPort, testType = TestType.CONNECT };
      }
      else
      {
        Logger.Log("Performing Ping Test");
        site = new Site { host = appPort.FqdnPrefix + fcPerformanceReply.Fqdn, port = connectionDetails.latencyTestPort, testType = TestType.PING };
      }
      await netTest.TestSite(site);
      double normalizedLatency = receivedStats.Avg - (receivedStats.Avg * config.performanceSwitchMargin);
      if (site.average < normalizedLatency)
      {
        Logger.Log("NewCloudlet Obtained from LatencyTest have better Latency, Connecting to NewCloudlet");
        PropagateSuccess(LatencyTooHigh, fcPerformanceReply);
      }
      else
      {
        errorMsg = "Latency threshold exceeded, but no other cloudlets have a meaningful improvement in latency";
        PropagateError(LatencyTooHigh, EventTriggeredButCurrentCloudletIsBest, errorMsg);
      }
      return;
    }

    private void PropagateSuccess(FindCloudletEventTrigger trigger, FindCloudletReply newCloudlet)
    {
      Logger.Log($"Received NewCloudlet by trigger: {trigger.ToString()},NewCloudlet: {newCloudlet.ToString()}");
      connectionDetails.mobiledgexManager.OnConnectionUpgrade(newCloudlet);

      if (config.autoMigration)
      {
        connectionDetails.mobiledgexManager.latestFindCloudletReply = newCloudlet;
        StopEdgeEvents();
        Logger.Log($"Restart EdgeEventsConnection, newCloudlet.EdgeEventsCookie: {newCloudlet.EdgeEventsCookie}, dmeHostOverride: {connectionDetails.dmeHostOverride},dmePortOverride: {connectionDetails.dmePortOverride}");
        connectionDetails.matchingEngine.RestartEdgeEventsConnection(newCloudlet, connectionDetails.dmeHostOverride, connectionDetails.dmePortOverride);
        StartEdgeEvents(connectionDetails);
      }
      else
      {
        StopEdgeEvents();
        if (locationService != null)
        {
          locationService.StopLocationUpdates();
        }
        Logger.Log("Migration occured and autoMigration is set to false, you can manually call StartEdgeEvents method.");
      }
    }

    private void PropagateError(FindCloudletEventTrigger trigger, EdgeEventsError error, string logMsg)
    {
      Debug.LogError(logMsg);
      switch (error)
      {
        case EventTriggeredButFindCloudletError:
          connectionDetails.mobiledgexManager.OnConnectionFailure(trigger.ToString() + "\n" + error.ToString());
          break;
        case InvalidEdgeEventsSetup:
          throw new EdgeEventsException(InvalidEdgeEventsSetup.ToString());
        case InvalidLatencyThreshold:
          throw new EdgeEventsException(InvalidLatencyThreshold.ToString());
        case InvalidPerformanceSwitchMargin:
          throw new EdgeEventsException(InvalidPerformanceSwitchMargin.ToString());
        case InvalidUpdateInterval:
          throw new EdgeEventsException(InvalidUpdateInterval.ToString());
        case MissingEdgeEventsCookie:
          throw new EdgeEventsException(MissingEdgeEventsCookie.ToString());
        case MissingSessionCookie:
          throw new EdgeEventsException(MissingSessionCookie.ToString());
        case EventTriggeredButCurrentCloudletIsBest:
          Logger.LogWarning("EventTriggeredButCurrentCloudletIsBest ");
          break;
        case UnableToGetLastLocation:
          Logger.LogWarning("UnableToGetLastLocation ");
          break;
      }
    }

    private void ProcessLatency(Statistics stats)
    {
      Logger.Log("Processing Latency Received from Server");
      if (stats.Avg > config.latencyThresholdTriggerMs)
      {
        Logger.Log($"Current Latency ({stats.Avg}) > LatencyThreshold ({config.latencyThresholdTriggerMs})");
        Logger.Log("Performing FindCloudlet PerformanceMode");
        FCPerformanceThreadManager fcThreadObj = new FCPerformanceThreadManager
          (matchingEngine: connectionDetails.matchingEngine, location: location, hostOverride: connectionDetails.dmeHostOverride, portOverride: connectionDetails.dmePortOverride, callbackDelegate: FCCallback, latencyTestPort: config.latencyTestPort);
        fcThreadObj.RunFCPerformance();
      }
      else
      {
        Logger.Log($"Current Latency ({stats.Avg}) < LatencyThreshold ({config.latencyThresholdTriggerMs})");
        UpdatesMonitor.latencyUpdatesStatus = UpdatesStatus.Ready;
      }
      return;
    }

    internal static DeviceInfoDynamic GetDeviceDynamicInfo(MatchingEngine matchingEngine, EdgeEventsConfig config = null)
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

    internal static DeviceInfoStatic GetDeviceStaticInfo(MatchingEngine matchingEngine)
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
