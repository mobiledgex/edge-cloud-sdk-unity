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
using System.Collections.Generic;

namespace MobiledgeX
{
  [Serializable]
  public class EdgeEventsConfig
  {
    /// <summary>
    /// Latency threshold (In Milliseconds) is the threshold at which MobiledgeX SDK will start to look for a cloudlet with better latency
    /// </summary>
    [Tooltip("Latency threshold (In Milliseconds) is the threshold at which MobiledgeX SDK will start to look for a cloudlet with better latency")]
    public double latencyThresholdTriggerMs;
    /// <summary>
    /// Port information for latency testing
    /// <para>Use 0 to select a random port</para>
    /// </summary>
    [Tooltip("Port information for latency testing" +
        "\nUse 0 to select a random port")]
    public int latencyTestPort;
    /// <summary>
    /// List of triggers that will trigger a new find cloudlet.
    /// </summary>
    [Tooltip("List of triggers that will trigger a new find cloudlet.")]
    public List<FindCloudletEventTrigger> newFindCloudletEventTriggers;
    /// <summary>
    /// Allow MobiledgeX EdgeEvents to automatically stop the current EdgeEvents connection and start a new EdgeEvents connection to receive events from the new cloudlet
    /// </summary>
    [Tooltip("Allow MobiledgeX EdgeEvents to automatically stop the current EdgeEvents connection and start a new EdgeEvents connection to receive events from the new cloudlet")]
    public bool autoMigration;
    /// <summary>
    /// Average performance must be by better by this latency margin (0 to 1.0f) before notifying of switch.
    /// </summary>
    [Tooltip("Average performance must be by better by this latency margin (0 to 1.0f) before notifying of switch.")]
    public float performanceSwitchMargin = 0.05f;
    /// <summary>
    /// Config for latency updates
    /// </summary>
    [Tooltip("Config for latency updates")]
    public UpdateConfig latencyConfig;
    /// <summary>
    /// Config for location updates
    /// </summary>
    [Tooltip("Config for location updates")]
    public UpdateConfig locationConfig;

    /// <summary>
    /// Set to true to switch to any better cloudlet even if it's with a different carrier other than the device carrier.
    /// </summary>
    [Tooltip("Set to true to switch to any better cloudlet even if it's with a different carrier other than the device carrier")]
    public bool useAnyCarrier;
    //DefaultConfig
    public EdgeEventsConfig()
    {
      latencyThresholdTriggerMs = 50;
      latencyTestPort = 0;
      newFindCloudletEventTriggers = new List<FindCloudletEventTrigger>() {
        FindCloudletEventTrigger.AppInstHealthChanged,
        FindCloudletEventTrigger.LatencyTooHigh,
        FindCloudletEventTrigger.CloudletStateChanged,
        FindCloudletEventTrigger.CloudletMaintenanceStateChanged,
        FindCloudletEventTrigger.CloserCloudlet,
        FindCloudletEventTrigger.Error,
      };
      autoMigration = true;
      performanceSwitchMargin = 0.05f;
      latencyConfig = new UpdateConfig();
      locationConfig = new UpdateConfig();
      useAnyCarrier = true;
    }

    public override string ToString()
    {
      string configSummary = "EdgeEvents Config Summary:";
      configSummary += "\nLatency Test Port: " + latencyTestPort;
      configSummary += "\nLatency Threshold Trigger (Milliseconds): " + latencyThresholdTriggerMs;
      configSummary += "\nLatency Update Pattern: " + latencyConfig.updatePattern;
      configSummary += "\nLatency Max. Number of Updates: " + latencyConfig.maxNumberOfUpdates;
      configSummary += "\nLatency Update Interval (Seconds): " + latencyConfig.updateIntervalSeconds;
      configSummary += "\nLocation Update Pattern: " + locationConfig.updatePattern;
      configSummary += "\nLocation Max. Number of Updates: " + locationConfig.maxNumberOfUpdates;
      configSummary += "\nLocation Update Interval (Seconds): " + locationConfig.updateIntervalSeconds;
      configSummary += "\nAutoMigration : " + autoMigration;
      configSummary += "\nPerformanceMarginSwitch : " + performanceSwitchMargin;
      configSummary += "\nNewFindCloudletEventTriggers: ";
      foreach (FindCloudletEventTrigger trigger in newFindCloudletEventTriggers)
      {
        configSummary += "\nTrigger : " + trigger.ToString();
      }
      return configSummary;
    }
  }


  [Serializable]
  public class UpdateConfig
  {
    /// <summary>
    /// UpdatePattern for sending client events
    /// <para><b>OnInterval</b> update every updateInterval seconds </para>
    /// <para><b>OnStart</b> only update once the connection starts</para>
    /// <para><b>OnTrigger</b> the application is responsible for sending the events </para>
    /// </summary>
    [Tooltip(" UpdatePattern for sending client events" +
        "\nOnInterval: update every updateInterval seconds" +
        "\nOnStart: only update once the connection starts" +
        "\nOnTrigger: the application is responsible for sending the events ")]
    public UpdatePattern updatePattern;
    /// <summary>
    /// Update interval in seconds
    /// <para>Works only if the UpdatePattern is set to <b>OnInterval</b></para>
    /// </summary>
    [Tooltip("Update interval in seconds" +
         "\nWorks only if the UpdatePattern is set to OnInterval")]
    public int updateIntervalSeconds;
    /// <summary>
    /// Maximum number of updates throughout the App lifetime.
    /// <para>Works only if the UpdatePattern is set to <b>OnInterval</b></para>
    /// <para>Set to 0 for updates to run till the EdgeEvents connection is closed</para>
    /// </summary>
    [Tooltip("Maximum number of updates through out the App lifetime." +
        "\nWorks only if the UpdatePattern is set to OnInterval" +
        "\nSet to 0 for updates to run till the EdgeEvents connection is closed")]
    public int maxNumberOfUpdates;
    //DefaultUpdateConfig
    public UpdateConfig()
    {
      updatePattern = UpdatePattern.OnInterval;
      updateIntervalSeconds = 30;
      maxNumberOfUpdates = 0;
    }
  }

  /// <summary>
  /// UpdatePattern for sending client events
  /// </summary>
  public enum UpdatePattern
  {
    OnInterval = 0,
    OnStart = 1,
    OnTrigger = 2,
  }

  /// <summary>
  /// Triggers that will trigger a new find cloudlet.
  /// </summary>
  [Serializable]
  public enum FindCloudletEventTrigger
  {
    AppInstHealthChanged,
    CloudletStateChanged,
    CloudletMaintenanceStateChanged,
    LatencyTooHigh,
    CloserCloudlet,
    Error
  }

  public enum EdgeEventsError
  {
    MissingSessionCookie,
    MissingEdgeEventsCookie,
    UnableToGetLastLocation,
    InvalidEdgeEventsSetup,
    InvalidLatencyThreshold,
    InvalidPerformanceSwitchMargin,
    InvalidUpdateInterval,
    PortDoesNotExist,
    EventTriggeredButCurrentCloudletIsBest,
    EventTriggeredButFindCloudletError,
    EventError,
    None
  }

  public class EdgeEventsException : Exception
  {
    public EdgeEventsException()
    {
    }

    public EdgeEventsException(string message)
    : base(message)
    {
    }

    public EdgeEventsException(string message, Exception inner)
    : base(message, inner)
    {
    }
  }
}
