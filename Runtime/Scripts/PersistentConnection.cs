using System;
using UnityEngine;
using DistributedMatchEngine;
using System.Collections;
using DistributedMatchEngine.PerformanceMetrics;
using System.Collections.Generic;
using static DistributedMatchEngine.ServerEdgeEvent.Types.ServerEventType;
using static DistributedMatchEngine.PerformanceMetrics.NetTest;

namespace MobiledgeX
{
  [RequireComponent(typeof(LocationService))]
  [AddComponentMenu("MobiledgeX/PersistentConnection")]
  public class PersistentConnection : MonoBehaviour
  {
    public Action<MobiledgeXIntegration> startStreamingEvents;

    MobiledgeXIntegration integration;
    bool edgeEventsRunning;
    List<FindCloudletEventTrigger> fcTriggers;
    int locationUpdateInterval;
    int locationMaxNumberOfUpdates;
    int locationUpdatesCounter;
    int latencyUpdateInterval;
    int latencyMaxNumberOfUpdates;
    int latencyUpdatesCounter;
    uint latencyPort;
    Loc location;
    bool haveTCPPorts;

    private void OnEnable()
    {
      startStreamingEvents += RunEdgeEvents;
    }

    void RunEdgeEvents(MobiledgeXIntegration mxi)
    {
      edgeEventsRunning = true;
      integration = mxi;
      fcTriggers = MobiledgeXIntegration.settings.edgeEventsConfig.newFindCloudletEventTriggers;
      integration.matchingEngine.EdgeEventsReceiver += HandleServerReceivedEvents;
      locationMaxNumberOfUpdates = MobiledgeXIntegration.settings.edgeEventsConfig.locationConfig.maxNumberOfUpdates;
      locationUpdateInterval = MobiledgeXIntegration.settings.edgeEventsConfig.locationConfig.updateIntervalSeconds;
      latencyMaxNumberOfUpdates = MobiledgeXIntegration.settings.edgeEventsConfig.locationConfig.maxNumberOfUpdates;
      latencyUpdateInterval = MobiledgeXIntegration.settings.edgeEventsConfig.locationConfig.updateIntervalSeconds;
      latencyPort = (uint)MobiledgeXIntegration.settings.edgeEventsConfig.latencyTestPort;
      EdgeEventsConnection connection = integration.matchingEngine.EdgeEventsConnection;
      location = LocationService.RetrieveLocation();
      AppPort appPort = integration.GetAppPort(LProto.Tcp);
      if (appPort != null)
        haveTCPPorts = true;
      else
        haveTCPPorts = false;
      string host = integration.GetHost();
      switch (MobiledgeXIntegration.settings.edgeEventsConfig.locationConfig.updatePattern)
      {
        case UpdatePattern.OnStart:
          connection.PostLocationUpdate(location);
          edgeEventsRunning = false;
          break;
        case UpdatePattern.OnInterval:
          edgeEventsRunning = true;
          //Location is already acquired, send the first PostLocationUpdate
          connection.PostLocationUpdate(location);
          StartCoroutine(OnIntervalEdgeEventsLocation());
          break;
      }

      switch (MobiledgeXIntegration.settings.edgeEventsConfig.latencyConfig.updatePattern)
      {
        case UpdatePattern.OnStart:
          if (haveTCPPorts)
          {
            connection.TestConnectAndPostLatencyUpdate(host, latencyPort, location).ConfigureAwait(false);
          }
          else
          {
            connection.TestPingAndPostLatencyUpdate(host, location).ConfigureAwait(false);
          }
          break;
        case UpdatePattern.OnInterval:
          edgeEventsRunning = true;
          //Location is already acquired, send the first PostLatencyUpdate
          if (haveTCPPorts)
          {
            connection.TestConnectAndPostLatencyUpdate(host, latencyPort, location).ConfigureAwait(false);
          }
          else
          {
            connection.TestPingAndPostLatencyUpdate(host, location).ConfigureAwait(false);
          }
          StartCoroutine(OnIntervalEdgeEventsLatency());
          break;
      }
    }

    IEnumerator OnIntervalEdgeEventsLocation()
    {
      if (locationMaxNumberOfUpdates > 0)
      {
        if (locationUpdatesCounter >= locationMaxNumberOfUpdates)
        {
          edgeEventsRunning = false;
          yield break;
        }
      }
      yield return new WaitForSecondsRealtime(locationUpdateInterval);
      yield return StartCoroutine(LocationService.EnsureLocation());
      Loc location = LocationService.RetrieveLocation();
      integration.matchingEngine.EdgeEventsConnection.PostLocationUpdate(location);
      locationUpdatesCounter++;
      yield return StartCoroutine(OnIntervalEdgeEventsLocation());
    }

    IEnumerator OnIntervalEdgeEventsLatency()
    {
      if (latencyMaxNumberOfUpdates > 0)
      {
        if (latencyUpdatesCounter >= latencyMaxNumberOfUpdates)
        {
          edgeEventsRunning = false;
          yield break;
        }
      }
      yield return new WaitForSecondsRealtime(latencyUpdateInterval);
      yield return StartCoroutine(LocationService.EnsureLocation());
      if (haveTCPPorts)
      {
        integration.matchingEngine.EdgeEventsConnection
             .TestConnectAndPostLatencyUpdate
             (integration.GetHost(), latencyPort, location).ConfigureAwait(false);
      }
      else
      {
        integration.matchingEngine.EdgeEventsConnection
          .TestPingAndPostLatencyUpdate
          (integration.GetHost(), location).ConfigureAwait(false);
      }
      latencyUpdatesCounter++;
      yield return StartCoroutine(OnIntervalEdgeEventsLatency());
    }

    async void HandleServerReceivedEvents(ServerEdgeEvent edgeEvent)
    {
      EdgeEventsConfig config = MobiledgeXIntegration.settings.edgeEventsConfig;
      if (edgeEvent.EventType == EventInitConnection)
      {
        Logger.Log("Received InitConnection Event");
      }

      switch (edgeEvent.EventType)
      {
        case EventAppinstHealth:
          if (fcTriggers.Contains(FindCloudletEventTrigger.AppInstHealthChanged))
          {
            if (edgeEvent.HealthCheck != HealthCheck.Ok)
            {
              Logger.Log("Received Event HealthCheck " + edgeEvent.HealthCheck.ToString());
              integration.HandleConnectionUpgrade
                 (FindCloudletEventTrigger.AppInstHealthChanged, EventAppinstHealth);
            }
          }
          break;
        case EventCloudletMaintenance:
          if (fcTriggers.Contains(FindCloudletEventTrigger.CloudletMaintenanceStateChanged))
          {
            if (edgeEvent.MaintenanceState != MaintenanceState.NormalOperation)
            {
              Logger.Log("Received Event CloudletMaintenanceStateChanged " + edgeEvent.MaintenanceState.ToString());
              integration.HandleConnectionUpgrade
                  (FindCloudletEventTrigger.CloudletMaintenanceStateChanged, EventCloudletMaintenance);
            }
          }
          break;
        case EventCloudletState:
          if (fcTriggers.Contains(FindCloudletEventTrigger.CloudletStateChanged))
          {
            if (edgeEvent.CloudletState != CloudletState.Ready)
            {
              Logger.Log("Received Event CloudletStateChanged " + edgeEvent.CloudletState.ToString());
              integration.HandleConnectionUpgrade(FindCloudletEventTrigger.CloudletStateChanged, EventCloudletState);
            }
          }
          break;
        case EventLatencyRequest:
          Logger.Log("Received EventLatencyRequest, Sending Latency Samples");
          NetTest latencyTester = new NetTest(integration.matchingEngine);
          TestType testType = TestType.CONNECT;
          if (!haveTCPPorts)
          {
            testType = TestType.PING;
          }
          Site site1 = new Site { host = integration.GetHost(), port = (int)latencyPort, testType = testType };
          latencyTester.sites.Enqueue(site1);
          await latencyTester.RunNetTest(5);
          foreach (var site in latencyTester.sites)
          {
            await integration.matchingEngine.EdgeEventsConnection.PostLatencyUpdate(site, LocationService.RetrieveLocation());
          }
          break;
        case EventLatencyProcessed:
          if (fcTriggers.Contains(FindCloudletEventTrigger.LatencyTooHigh))
          {
            Logger.Log("Received EventLatencyProcessed, Comparing Latency to the LatencyThreshold from configs.");
            if (config.latencyThresholdTriggerMs.CompareTo(edgeEvent.Statistics.Avg) <= 0)
            {
              integration.HandleConnectionUpgrade(FindCloudletEventTrigger.LatencyTooHigh, EventLatencyProcessed);
            }
          }
          break;
        case EventCloudletUpdate:
          if (fcTriggers.Contains(FindCloudletEventTrigger.CloserCloudlet))
          {
            Logger.Log("New CloserCloudlet found,New Cloudlet GPS :" + CloudletLocation.GpsLocationFieldNumber);
            integration.HandleConnectionUpgrade(FindCloudletEventTrigger.CloserCloudlet, EventCloudletUpdate);
          }
          break;
        default:
        case EventUnknown:
          Logger.Log("Received Unknown Event");
          break;
          //fixme missing ServerEdgeEvent.Types.ServerEventType.EventError from C# DLL
      }
    }

    private void OnApplicationFocus(bool focus)
    {
      if (edgeEventsRunning)
      {
        RunEdgeEvents(integration);// resume edge events streaming
      }
    }

    private void OnApplicationPause(bool pause)
    {
      StopAllCoroutines();// stop edge events streaming
    }

    private void OnApplicationQuit()
    {
      StopAllCoroutines();
    }

    private void OnDestroy()
    {
      StopAllCoroutines();
    }
  }
}
