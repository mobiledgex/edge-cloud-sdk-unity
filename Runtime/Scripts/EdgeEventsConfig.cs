using System;
using UnityEngine;
using DistributedMatchEngine;
using System.Collections.Generic;

namespace MobiledgeX
{
    [Serializable]
    public struct EdgeEventsConfig
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
        public ClientEventsConfig latencyConfig;
        /// <summary>
        /// Config for location updates
        /// </summary>
        [Tooltip("Config for location updates")]
        public ClientEventsConfig locationConfig;

        /// <summary>
        /// List of triggers that application that will trigger a new find cloudlet.
        /// </summary>
        [Tooltip("List of triggers that application that will trigger a new find cloudlet.")]
        public List<FindCloudletEventTrigger> newFindCloudletEventTriggers;
    }

    [Serializable]
    public struct ClientEventsConfig
    {
        /// <summary>
        /// UpdatePattern for sending client events
        /// <para><b>OnInterval</b> update every updateInterval seconds </para>
        /// <para><b>OnStart</b> only update once the connections starts</para>
        /// <para><b>OnTrigger</b> the application is responsible for sending the events </para>
        /// </summary>
        [Tooltip(" UpdatePattern for sending client events" +
            "\nOnInterval: update every updateInterval seconds" +
            "\nOnStart: only update once the connections starts" +
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
            "\nSet to -1 for updates to run till the EdgeEvents connection is closed")]
        public int maxNumberOfUpdates;
    }

    /// <summary>
    /// UpdatePattern for sending client events
    /// </summary>
    public enum UpdatePattern
    {
        OnInterval,
        OnTrigger,
        OnStart
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
        CloserCloudlet 
    }
}
