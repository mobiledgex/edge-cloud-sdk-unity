/**
* Copyright 2018-2020 MobiledgeX, Inc. All rights and licenses reserved.
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
using System.Threading.Tasks;

/*
* Utility functions to help developer configure their MobiledgeXIntegration object
*/

namespace MobiledgeX
{
    public partial class MobiledgeXIntegration
    {
        /// <summary>
        /// Uses MobiledgeXSetting Scriptable object to load and save orgName, appName, appVers 
        /// </summary>
        public void ConfigureMobiledgeXSettings()
        {
            // Setting Application Definitions
            orgName = settings.orgName;
            appName = settings.appName;
            appVers = settings.appVers;

            if (settings.authPublicKey.Length > 0)
            {
                developerAuthToken = settings.authPublicKey;
            }

            tcpPort = (int)settings.TCP_Port;
            udpPort = (int)settings.UDP_Port;
        }

        /// <summary>
        /// Use for testing In UnityEditor, Won't work in Production
        /// </summary>
        /// <param name="useWifi"></param>
        public void UseWifiOnly(bool useWifi)
        {
            matchingEngine.useOnlyWifi = useWifi;
            Debug.Log("Setting useWifiOnly to " + useWifi);
        }

        /// <summary>
        /// Changes how FindCloudlet will find the "nearest" cloudlet
        /// Proximity Mode: Default. Gets the cloudlet that is nearest based on gps
        /// Performance Mode: Does latency test for all cloudlets and returns the fastest cloudlet. (takes longer to return)
        /// </summary>
        public void UseFindCloudletPerformanceMode(bool performanceMode)
        {
            mode = performanceMode ? FindCloudletMode.PERFORMANCE : FindCloudletMode.PROXIMITY;
            Debug.Log("Setting FindCloudlet mode to " + mode);
        }
    }
}
