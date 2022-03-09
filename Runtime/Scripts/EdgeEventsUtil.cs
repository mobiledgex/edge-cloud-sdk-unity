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
 * limitations undser the License.
 */

using UnityEngine;
using DistributedMatchEngine;
using System.Threading;
using System;
using System.Collections;
using System.Threading.Tasks;
using static MobiledgeX.EdgeEventsError;
using static MobiledgeX.FindCloudletEventTrigger;

namespace MobiledgeX
{
  public class ConnectionDetails
  {
    public MobiledgeXIntegration mobiledgexManager;
    public MatchingEngine matchingEngine;
    public string dmeHostOverride;
    public uint dmePortOverride;
    public bool hasTCPPort;
    public string appHost;
    public int latencyTestPort;

    public ConnectionDetails(MobiledgeXIntegration mobiledgexManager, string hostOverride = "", uint portOverride = 0)
    {
      this.mobiledgexManager = mobiledgexManager;
      if (mobiledgexManager.latestFindCloudletReply == null)
      {
        Debug.LogError("Please call register and find cloudlet using your mobiledgexManager before instantiating a ConnectionsDetails object");
        throw new FindCloudletException("Latest FindCloudlet Reply is null");
      }
      if (hostOverride == "" || hostOverride == null)
      {
#if UNITY_EDITOR
        dmeHostOverride = mobiledgexManager.region;
#endif
        if (mobiledgexManager.useSelectedRegionInProduction == true)
        {
          dmeHostOverride = mobiledgexManager.region;
        }
      }
      else
      {
        dmeHostOverride = hostOverride;
      }
      if (portOverride == 0)
      {
#if UNITY_EDITOR
        dmePortOverride = MatchingEngine.defaultDmeGrpcPort;
#endif
        if (mobiledgexManager.useSelectedRegionInProduction == true)
        {
          dmePortOverride = MatchingEngine.defaultDmeGrpcPort;
        }
      }
      else
      {
        dmePortOverride = portOverride;
      }
      matchingEngine = this.mobiledgexManager.matchingEngine;
    }

    /// <summary>
    /// Update appHost (application host) instance variable in ConnectionDetails
    /// </summary>
    /// <param name="latencyPort"> TCP LatencyTestPort </param>
    public void SetAppHost(int latencyPort)
    {
      mobiledgexManager.GetAppPort(LProto.Tcp, latencyPort);
      appHost = mobiledgexManager.GetHost();
    }

    public override string ToString()
    {
      string summary = "dmeHostOverride: " + dmeHostOverride;
      summary += ", dmePortOverride: " + dmePortOverride;
      summary += ", app Host: " + appHost;
      summary += ", latencyTestPort: " + latencyTestPort;
      summary += ", mobiledgexManager: " + mobiledgexManager;
      summary += ", hasTCPPort: " + hasTCPPort;
      return summary;
    }
  }

  public class UpdatesMonitor
  {
    public static int latencyUpdatesCounter = 0;
    public static int locationUpdatesCounter = 0;
    public static UpdatesStatus latencyUpdatesStatus = UpdatesStatus.Ready;
    public static UpdatesStatus locationUpdatesStatus = UpdatesStatus.Ready;
    public static UpdatesStatus latencyProcessingStatus = UpdatesStatus.Ready;
    public static bool edgeEventConnectionInitiated;

    public UpdatesMonitor()
    {
      latencyProcessingStatus = UpdatesStatus.Ready;
      latencyUpdatesStatus = UpdatesStatus.Ready;
      locationUpdatesStatus = UpdatesStatus.Ready;
    }

    public void Reset()
    {
      edgeEventConnectionInitiated = false;
      latencyUpdatesCounter = 0;
      locationUpdatesCounter = 0;
      latencyUpdatesStatus = UpdatesStatus.Ready;
      locationUpdatesStatus = UpdatesStatus.Ready;
      latencyProcessingStatus = UpdatesStatus.Ready;
    }

    public void StopUpdates()
    {
      latencyUpdatesStatus = UpdatesStatus.Stopped;
      locationUpdatesStatus = UpdatesStatus.Stopped;
      latencyProcessingStatus = UpdatesStatus.Stopped;
    }

    public void PauseUpdates()
    {
      if (latencyUpdatesStatus == UpdatesStatus.Running)
      {
        latencyUpdatesStatus = UpdatesStatus.Paused;
      }
      if (locationUpdatesStatus == UpdatesStatus.Running)
      {
        locationUpdatesStatus = UpdatesStatus.Paused;
      }
      if (latencyProcessingStatus == UpdatesStatus.Running)
      {
        latencyProcessingStatus = UpdatesStatus.Paused;
      }
    }

    public void ResumeUpdates()
    {
      if (latencyProcessingStatus == UpdatesStatus.Paused)
      {
        latencyProcessingStatus = UpdatesStatus.Ready;
      }
    }
    public void StartLocationUpdates(UpdateConfig locationUpdateConfig)
    {
      locationUpdatesCounter = SetNumberOfUpdates(locationUpdateConfig, locationUpdatesStatus, locationUpdatesCounter);
      Logger.Log("Setting Number of Location Updates to " + locationUpdatesCounter);
      if (locationUpdatesCounter > 0)
      {
        locationUpdatesStatus = UpdatesStatus.Start;
      }
    }
    public void StartLatencyUpdates(UpdateConfig latencyUpdateConfig)
    {
      latencyUpdatesCounter = SetNumberOfUpdates(latencyUpdateConfig, latencyUpdatesStatus, latencyUpdatesCounter);
      Logger.Log("Setting Number of Latency Updates to " + latencyUpdatesCounter);
      if (latencyUpdatesCounter > 0)
      {
        latencyUpdatesStatus = UpdatesStatus.Start;
      }
    }

    public int SetNumberOfUpdates(UpdateConfig updateConfig, UpdatesStatus updatesStatus, int currentCounterVal)
    {
      if (updateConfig.updatePattern == UpdatePattern.OnTrigger)
      {
        return 0;
      }
      if (updatesStatus == UpdatesStatus.Stopped || updatesStatus == UpdatesStatus.Completed)
      {
        return 0;
      }
      if (updatesStatus == UpdatesStatus.Paused)
      {
        return currentCounterVal;
      }
      if (updateConfig.updatePattern == UpdatePattern.OnStart)
      {
        return 1;
      }
      //OnInterval (Fresh Start)
      if (updateConfig.maxNumberOfUpdates == 0)
      {
        return int.MaxValue;
      }
      else
      {
        return updateConfig.maxNumberOfUpdates;
      }
    }

    public override string ToString()
    {
      string summary = "UpdatesMonitor Status:" +
        "\nedgeEventConnectionInitiated = " + edgeEventConnectionInitiated +
        "\nlatencyUpdatesStatus = " + latencyUpdatesStatus +
        "\nlocationUpdatesStatus = " + locationUpdatesStatus +
        "\nlatencyProcessingStatus = " + latencyProcessingStatus +
        "\nlatencyUpdatesCounter = " + latencyUpdatesCounter +
        "\nlocationUpdatesCounter = " + locationUpdatesCounter;
      return summary;
    }
  }

  public enum UpdatesStatus
  {
    Ready,
    Start,
    Running,
    Paused,
    Stopped,
    Completed
  }

  public class FCPerformanceThreadManager
  {
    string hostOverride;
    uint portOverride;
    int latencyTestPort;
    MatchingEngine matchingEngine;
    EdgeEventsManager.FCPerformanceCallback callback;
    Loc location;
    Thread FCPerformanceThread;

    internal FCPerformanceThreadManager(MatchingEngine matchingEngine, Loc location, string hostOverride, uint portOverride,
     EdgeEventsManager.FCPerformanceCallback callbackDelegate, int latencyTestPort)
    {
      this.hostOverride = hostOverride;
      this.portOverride = portOverride;
      this.matchingEngine = matchingEngine;
      this.location = location;
      this.latencyTestPort = latencyTestPort;
      callback = callbackDelegate;
    }

    internal void RunFCPerformance()
    {
      FCPerformanceThread = new Thread(new ThreadStart(RunFCPerformanceHelper));
      try
      {
        FCPerformanceThread.Name = "FCPerformanceThread";
        FCPerformanceThread.IsBackground = true;
        FCPerformanceThread.Priority = System.Threading.ThreadPriority.Lowest;
        FCPerformanceThread.Start();
        Debug.Log("Waiting for FindCloudlet Performance Mode");
        FCPerformanceThread.Join();
      }
      catch (ThreadInterruptedException tie)
      {
        callback(null);
        FCPerformanceThread.Abort();
        Logger.Log("FCPerformance Thread interrupted " + tie.Message + ", InnerExcetion: " + tie.InnerException + ", Stack: " + tie.StackTrace);
        return;
      }
    }

    internal void Interrupt()
    {
      if (FCPerformanceThread != null && FCPerformanceThread.IsAlive)
      {
        FCPerformanceThread.Interrupt();
      }
      FCPerformanceThread = null;
      hostOverride = null;
      portOverride = 0;
      location = null;
      callback = null;
    }

    internal async void RunFCPerformanceHelper()
    {
      FindCloudletReply fcReply = null;
      FindCloudletRequest fcReq = matchingEngine.CreateFindCloudletRequest(location, "");
      Logger.Log("FindCloudletPerformanceMode Request: " + fcReq.ToString());
      try
      {
        Logger.Log("FindCloudletPerformanceMode HostOverride: " + hostOverride);
        Logger.Log("FindCloudletPerformanceMode portOverride: " + portOverride);
        Logger.Log("FindCloudletPerformanceMode LatencyTestPort: " + latencyTestPort);
        fcReply = await matchingEngine.FindCloudletPerformanceMode(hostOverride, portOverride, fcReq, testPort: latencyTestPort);
      }
      catch (Exception fce)
      {
        Debug.LogError("FindCloudletException: " + fce.Message + "Inner Exception: " + fce.InnerException + ", Stack: " + fce.StackTrace);
        callback(fcReply);
        Thread.CurrentThread.Abort();
      }
      Logger.Log("FindCloudletPerformance done, fcReply: " + fcReply.ToString());
      if (callback != null)
      {
        callback(fcReply);
      }
    }
  }

  public class EdgeEventsUtil
  {
    /// <summary>
    /// Helper function for acquiring DeviceDynamicInfo in a Multithreading environment to avoid stale references on Android Devices
    /// </summary>
    /// <param name="MobiledgeXIntegration"></param>
    /// <returns>DeviceInfoDynamic Object</returns>
    public static DeviceInfoDynamic GetDynamicInfo(MobiledgeXIntegration mobiledgeXManager, EdgeEventsConfig config = null)
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
          DeviceInfoDynamic result = mobiledgeXManager.matchingEngine.deviceInfo.GetDeviceInfoDynamic();
          AndroidJNI.DetachCurrentThread();
          return result;
        }).Result;
      }
      else
      {
        deviceInfoDynamic = mobiledgeXManager.matchingEngine.deviceInfo.GetDeviceInfoDynamic();
      }
      if (config.useAnyCarrier)
      {
        deviceInfoDynamic.CarrierName = "";
      }
      return deviceInfoDynamic;
    }

    /// <summary>
    /// A coroutine responsible for responding to latency request received from MobiledgeX DME
    /// </summary>
    /// <param name="connectionDetails"> ConnectionDetails object </param>
    /// <param name="location"> Loc object </param>
    /// <param name="latencyPort"> the port used for latency testing </param>
    public static IEnumerator RespondToLatencyRequest(ConnectionDetails connectionDetails, Loc location, int latencyPort = 0)
    {
      string host = connectionDetails.appHost;
      if (location == null)
      {
        throw new NullReferenceException("location argument is null, Please Supply the location, Location is used in the LatencySamples Response.");
      }
      if (connectionDetails.matchingEngine.EdgeEventsConnection == null)
      {
        throw new NullReferenceException("edgeEventsConnection is null, Make sure you have a running edge events connection or that you are passing the correct arguments.");
      }
      DeviceInfoDynamic deviceInfoDynamic = EdgeEventsManager.GetDeviceDynamicInfo(connectionDetails.matchingEngine);
      AppPort appPort = GetLatencyTestPort(connectionDetails.mobiledgexManager, latencyPort);
      if (appPort == null)
      {
        Debug.LogError("Latency Port doesn't exist");
        yield break;
      }

      if (appPort.Proto == LProto.Tcp)
      {
        Logger.Log("Sending Latency Samples (TCP) as a response to LatencyRequest from the server");
        connectionDetails.matchingEngine.EdgeEventsConnection.TestConnectAndPostLatencyUpdate(host, (uint)latencyPort, location, deviceInfoDynamic: deviceInfoDynamic).ConfigureAwait(false);
      }
      else
      {
        Logger.Log("Sending Latency Samples (UDP) as a response to LatencyRequest from the server");
        connectionDetails.matchingEngine.EdgeEventsConnection.TestPingAndPostLatencyUpdate(host, location, deviceInfoDynamic: deviceInfoDynamic).ConfigureAwait(false);
      }
    }

    /// <summary>
    /// Validates EdgeEvents Config, sessionCookie and edgeEventsCookie
    /// </summary>
    /// <param name="config"> EdgeEventsConfig object </param>
    /// <param name="connectionDetails"> ConnectionDetails object </param>
    /// <returns></returns>
    public static EdgeEventsError ValidateConfigs(EdgeEventsConfig config, ConnectionDetails connectionDetails)
    {
      if (config.newFindCloudletEventTriggers.Count == 1) //No FindCloudletTrigger except Error (Added By Default)
      {
        Debug.LogError("Missing FindCloudlets Triggers");
        return InvalidEdgeEventsSetup;
      }

      if (config.latencyThresholdTriggerMs <= 0)
      {
        Debug.LogError("LatencyThresholdTriggerMs must be > 0");
        return InvalidLatencyThreshold;
      }

      if (config.performanceSwitchMargin > 1 || config.performanceSwitchMargin < 0)
      {
        Debug.LogError("performanceSwitchMargin must between (0 to 1.0f)");
        return InvalidPerformanceSwitchMargin;
      }
      //latency config validation
      if (config.newFindCloudletEventTriggers.Contains(LatencyTooHigh))
      {
        if (config.latencyConfig.updatePattern == UpdatePattern.OnInterval)
        {
          if (config.latencyConfig.maxNumberOfUpdates < 0)
          {
            Debug.LogError("latencyConfig.maxNumberOfUpdates must be >= 0");
            return InvalidEdgeEventsSetup;
          }
          if (config.latencyConfig.updateIntervalSeconds <= 0)
          {
            Debug.LogError("latencyConfig.updateIntervalSeconds must be > 0");
            return InvalidUpdateInterval;
          }
        }
      }
      //location config validation
      if (config.newFindCloudletEventTriggers.Contains(CloserCloudlet))
      {
        if (config.locationConfig.updatePattern == UpdatePattern.OnInterval)
        {
          if (config.locationConfig.maxNumberOfUpdates < 0)
          {
            Debug.LogError("Config.maxNumberOfUpdates must be >= 0");
            return InvalidEdgeEventsSetup;
          }
          if (config.locationConfig.updateIntervalSeconds <= 0)
          {
            Debug.LogError("locationConfig.updateIntervalSeconds must be > 0");
            return InvalidUpdateInterval;
          }
        }
      }

      if (connectionDetails.matchingEngine.sessionCookie == null || connectionDetails.matchingEngine.sessionCookie == "")
      {
        Debug.LogError("Missing SessionCookie");
        return MissingSessionCookie;
      }
      if (connectionDetails.matchingEngine.edgeEventsCookie == null || connectionDetails.matchingEngine.edgeEventsCookie == "")
      {
        Debug.LogError("Missing EdgeEvents Cookie");
        return MissingEdgeEventsCookie;
      }
      return EdgeEventsError.None;
    }

    /// <summary>
    /// Gets the latency test port using latencyPort (int) and the proto (protocol)
    /// </summary>
    /// <param name="mobiledgexManager"></param>
    /// <param name="latencyPortNumber"></param>
    /// <param name="proto"></param>
    /// <returns></returns>
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

  }
}
