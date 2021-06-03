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
        public UpdateConfig latencyConfig;
        /// <summary>
        /// Config for location updates
        /// </summary>
        [Tooltip("Config for location updates")]
        public UpdateConfig locationConfig;

        /// <summary>
        /// List of triggers that will trigger a new find cloudlet.
        /// </summary>
        [Tooltip("List of triggers that will trigger a new find cloudlet.")]
        public List<FindCloudletEventTrigger> newFindCloudletEventTriggers;
    }

    [Serializable]
    public struct UpdateConfig
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

    //TODO add comments
    public struct FindCloudletEvent
    {
        public FindCloudletReply newCloudlet;
        public FindCloudletEventTrigger trigger;
    }

    /// <summary>
    /// UpdatePattern for sending client events
    /// </summary>
    public enum UpdatePattern
    {
        OnInterval=0,
        OnStart=1,
        OnTrigger=2,
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

    //TODO add comments
    public enum EdgeEventsStatus
    {
        success,
        error
    }

    //TODO add comments
    public enum EdgeEventsError
    {
       MissingSessionCookie,
       MissingEdgeEventsCookie,
       MissingEdgeEventsConfig,
       MissingFindCloudletTrigger,
       MissingNewFindCloudletHandler,
       PortDoesNotExist,
       AppInstanceDownButNoNewCloudlet,
       CloudletStateNotReadyButNoNewCloudlet,
       MaintenanceStateNotNormalButNoNewCloudlet
    }
}
