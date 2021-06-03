using System;
using UnityEngine;
using DistributedMatchEngine;
using System.Collections;
using System.Collections.Generic;
using static DistributedMatchEngine.ServerEdgeEvent.Types.ServerEventType;
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

    private void OnEnable()
    {
      startStreamingEvents += StartEdgeEvents;
      config = MobiledgeXIntegration.settings.edgeEventsConfig;
    }

    void StartEdgeEvents(MobiledgeXIntegration mxi)
    {
      integration = mxi;
      EdgeEventsConfig config = MobiledgeXIntegration.settings.edgeEventsConfig;
      
      if (config.newFindCloudletEventTriggers.Count == 0)
      {
        FindCloudletEvent findCloudletEvent = new FindCloudletEvent()
        {
          trigger = FindCloudletEventTrigger.Error
        };
        integration.NewFindCloudletHandler(EdgeEventsStatus.error, findCloudletEvent);
        Debug.LogError(EdgeEventsError.MissingFindCloudletTrigger.ToString());
      }
      if (integration.matchingEngine.sessionCookie == null)
      {
        FindCloudletEvent findCloudletEvent = new FindCloudletEvent()
        {
          trigger = FindCloudletEventTrigger.Error
        };
        integration.NewFindCloudletHandler(EdgeEventsStatus.error, findCloudletEvent);
        Debug.LogError(EdgeEventsError.MissingSessionCookie.ToString());
      }
      if (integration.matchingEngine.EdgeEventsConnection.edgeEventsCookie == null)
      {
        FindCloudletEvent findCloudletEvent = new FindCloudletEvent()
        {
          trigger = FindCloudletEventTrigger.Error
        };
        integration.NewFindCloudletHandler(EdgeEventsStatus.error, findCloudletEvent);
        Debug.LogError(EdgeEventsError.MissingSessionCookie.ToString());
      }
      
      integration.matchingEngine.EdgeEventsReceiver += HandleServerReceivedEvents;
      
      EdgeEventsConnection connection = integration.matchingEngine.EdgeEventsConnection;
      
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
          FindCloudletEvent findCloudletEvent = new FindCloudletEvent()
          {
            trigger = FindCloudletEventTrigger.Error
          };
          integration.NewFindCloudletHandler(EdgeEventsStatus.error, findCloudletEvent);
          Debug.LogError(EdgeEventsError.PortDoesNotExist.ToString());
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
          yield break;
        }
      }
      yield return new WaitForSecondsRealtime(config.locationConfig.updateIntervalSeconds);
      yield return StartCoroutine(LocationService.EnsureLocation());
      Loc location = LocationService.RetrieveLocation();
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

    async void HandleServerReceivedEvents(ServerEdgeEvent edgeEvent)
        {
          List<FindCloudletEventTrigger> fcTriggers = config.newFindCloudletEventTriggers;
          EdgeEventsConnection connection = integration.matchingEngine.EdgeEventsConnection;
          FindCloudletEvent findCloudletEvent = new FindCloudletEvent();
          if (edgeEvent.EventType == EventInitConnection)
          {
            Logger.Log("Received InitConnection Event");
          }
          switch (edgeEvent.EventType)
          {
            case EventAppinstHealth:
              Logger.Log("Received Event HealthCheck " + edgeEvent.HealthCheck.ToString());
              if (fcTriggers.Contains(FindCloudletEventTrigger.AppInstHealthChanged))
              {
                findCloudletEvent.trigger = FindCloudletEventTrigger.AppInstHealthChanged;
                if (edgeEvent.HealthCheck != HealthCheck.Ok)
                {
                  if (edgeEvent.NewCloudlet != null)
                  {
                    findCloudletEvent.newCloudlet = edgeEvent.NewCloudlet;
                    integration.NewFindCloudletHandler(EdgeEventsStatus.success, findCloudletEvent);
                  }
                  else
                  {
                    integration.NewFindCloudletHandler(EdgeEventsStatus.error, findCloudletEvent);
                    Debug.LogError(EdgeEventsError.AppInstanceDownButNoNewCloudlet.ToString());
                  }
                }
              }
              break;
            case EventCloudletMaintenance:
              Logger.Log("Received Event MaintenanceState " + edgeEvent.MaintenanceState.ToString());
              if (fcTriggers.Contains(FindCloudletEventTrigger.CloudletMaintenanceStateChanged))
              {
                findCloudletEvent.trigger = FindCloudletEventTrigger.CloudletMaintenanceStateChanged;
                if (edgeEvent.MaintenanceState != MaintenanceState.NormalOperation)
                {
                  if (edgeEvent.NewCloudlet != null)
                  {
                    findCloudletEvent.newCloudlet = edgeEvent.NewCloudlet;
                    integration.NewFindCloudletHandler(EdgeEventsStatus.success, findCloudletEvent);
                  }
                  else
                  {
                    integration.NewFindCloudletHandler(EdgeEventsStatus.error, findCloudletEvent);
                    Debug.LogError(EdgeEventsError.MaintenanceStateNotNormalButNoNewCloudlet.ToString());
                  }
                }
              }
              break;
            case EventCloudletState:
              Logger.Log("Received Event CloudletStateChanged " + edgeEvent.CloudletState.ToString());
              if (fcTriggers.Contains(FindCloudletEventTrigger.CloudletStateChanged))
              {
                findCloudletEvent.trigger = FindCloudletEventTrigger.CloudletStateChanged;
                if (edgeEvent.CloudletState != CloudletState.Ready)
                {
                  if (edgeEvent.NewCloudlet != null)
                  {
                    findCloudletEvent.newCloudlet = edgeEvent.NewCloudlet;
                    integration.NewFindCloudletHandler(EdgeEventsStatus.success, findCloudletEvent);
                  }
                  else
                  {
                    integration.NewFindCloudletHandler(EdgeEventsStatus.error, findCloudletEvent);
                    Debug.LogError(EdgeEventsError.CloudletStateNotReadyButNoNewCloudlet.ToString());
                  }
                }
              }
              break;
            case EventLatencyRequest:
              Logger.Log("Received EventLatencyRequest, Sending Latency Samples");
              string host = integration.GetHost();
              Loc location = LocationService.RetrieveLocation();
              if (hasTCPPorts)
              {
                await connection.TestConnectAndPostLatencyUpdate(host, (uint)config.latencyTestPort, location);
              }
              else
              {
                await connection.TestPingAndPostLatencyUpdate(host, location);
              }
              break;
            case EventLatencyProcessed:
              Logger.Log("Received EventLatencyProcessed, Comparing Latency to the LatencyThreshold from configs.");
              if (fcTriggers.Contains(FindCloudletEventTrigger.LatencyTooHigh))
                {
                  if (config.latencyThresholdTriggerMs.CompareTo(edgeEvent.Statistics.Avg) <= 0)
                  {
                    integration.UseFindCloudletPerformanceMode(true);
                    bool fcResult = await integration.FindCloudlet();
                    integration.UseFindCloudletPerformanceMode(false);
                    if (fcResult == true)// fixme check if automigration is true
                    {
                      findCloudletEvent.trigger = FindCloudletEventTrigger.LatencyTooHigh;
                      findCloudletEvent.newCloudlet = integration.FindCloudletReply;
                      integration.NewFindCloudletHandler(EdgeEventsStatus.success, findCloudletEvent);
                    }
                  }
                }
              break;
            case EventCloudletUpdate:
              Logger.Log("Received Cloudlet Update, New CloserCloudlet found,New Cloudlet GPS :" + CloudletLocation.GpsLocationFieldNumber);
              if (fcTriggers.Contains(FindCloudletEventTrigger.CloserCloudlet))
              {
                findCloudletEvent.trigger = FindCloudletEventTrigger.CloserCloudlet;
                findCloudletEvent.newCloudlet = edgeEvent.NewCloudlet;
                bool fcResult = await integration.FindCloudlet();
                integration.NewFindCloudletHandler(EdgeEventsStatus.success, findCloudletEvent);
              }
              break;
            case EventError:
              Logger.Log("Received EventError, :" + edgeEvent.ErrorMsg);
              FindCloudletEvent fcEvent = new FindCloudletEvent() { trigger = FindCloudletEventTrigger.Error };
              integration.NewFindCloudletHandler(EdgeEventsStatus.error, fcEvent);
              break;
            default:
            case EventUnknown:
              Logger.Log("Received Unknown Event");
              break;
          }
        }

    private void OnApplicationFocus(bool focus)
    {
      if (locationUpdatesRunning)
      {
        StartCoroutine(OnIntervalEdgeEventsLocation(integration.matchingEngine.EdgeEventsConnection));
      }
      if (latencyUpdatesRunning)
      {
        StartCoroutine(OnIntervalEdgeEventsLatency(integration.matchingEngine.EdgeEventsConnection, integration.GetHost()));
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
