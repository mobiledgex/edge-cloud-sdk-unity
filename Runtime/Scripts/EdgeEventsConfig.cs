﻿/**
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
using System.Collections.Generic;

namespace MobiledgeX
{
  [Serializable]
  public class EdgeEventsConfig
  {
    /// <summary>
    /// Latency threshold (In Milliseconds) as the limit to automatically migrate to a new cloudlet
    /// </summary>
    [Tooltip("Latency threshold (In Milliseconds) as the limit to automatically migrate to a new cloudlet")]
    public double latencyThresholdTriggerMs;
    /// <summary>
    /// Port information for latency testing
    /// <para>Use 0 to select a random port</para>
    /// </summary>
    [Tooltip("Port information for latency testing" +
        "\nUse 0 to select a random port")]
    public int latencyTestPort;
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
    /// List of triggers that application that will trigger a new find cloudlet.
    /// </summary>
    [Tooltip("List of triggers that application that will trigger a new find cloudlet.")]
    public List<FindCloudletEventTrigger> newFindCloudletEventTriggers;

    /// <summary>
    /// Allow MobiledgeX EdgeEvents to automatically connect to the new Cloudlet received from the DME server
    /// </summary>
    [Tooltip("Allow MobiledgeX EdgeEvents to automatically connect to the new Cloudlet received from the DME server.")]
    public bool autoMigration;
    /// <summary>
    /// Average performance must be by better by this latency margin (0 to 1.0f) before notifying of switch.
    /// </summary>
    [Tooltip("Average performance must be by better by this latency margin (0 to 1.0f) before notifying of switch.")]
    public float performanceSwitchMargin = 0.05f;
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
    /// Maximum number of updates through out the App lifetime.
    /// <para>Works only if the UpdatePattern is set to <b>OnInterval</b></para>
    /// <para>Set to -1 for updates to run till the EdgeEvents connection is closed</para>
    /// </summary>
    [Tooltip("Maximum number of updates through out the App lifetime." +
        "\nWorks only if the UpdatePattern is set to OnInterval" +
        "\nSet to 0 for updates to run till the EdgeEvents connection is closed")]
    public int maxNumberOfUpdates;
  }

  /// <summary>
  /// FindCloudletEvent received from the server
  /// </summary>
  public class FindCloudletEvent
  {
    public FindCloudletReply newCloudlet;
    public FindCloudletEventTrigger trigger;
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

  /// <summary>
  /// Represents the status of handling server received event
  /// </summary>
  public class EdgeEventsStatus
  {
    public Status status;
    /// <summary>
    /// empty if the status = success
    /// </summary>
    public string error_msg;

    public EdgeEventsStatus(Status status, string error_msg = "")
    {
      this.status = status;
      this.error_msg = error_msg;
    }
  }

  public enum Status
  {
    success,
    error
  }
}
