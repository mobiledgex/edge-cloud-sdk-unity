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
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.IO;
using System.Text;
using System.Net.Http;

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
            sdkVersion = settings.sdkVersion;

            if (settings.authPublicKey.Length > 0)
            {
                developerAuthToken = settings.authPublicKey;
            }
        }

        /// <summary>
        /// Use for testing In UnityEditor, Won't work in Production
        /// </summary>
        /// <param name="useWifi"></param>
        public void UseWifiOnly(bool useWifi)
        {
            matchingEngine.useOnlyWifi = useWifi;
            Logger.Log("Setting useWifiOnly to " + useWifi);
        }

        /// <summary>
        /// Changes how FindCloudlet will find the "nearest" cloudlet
        /// Proximity Mode: Default. Gets the cloudlet that is nearest based on gps
        /// Performance Mode: Does latency test for all cloudlets and returns the fastest cloudlet. (takes longer to return)
        /// </summary>
        public void UseFindCloudletPerformanceMode(bool performanceMode)
        {
            mode = performanceMode ? FindCloudletMode.PERFORMANCE : FindCloudletMode.PROXIMITY;
            Logger.Log("Setting FindCloudlet mode to " + mode);
        }

        /// <summary>
        /// Fallback Location will be used if LocationServices is down or if running in UnityEditor
        /// </summary>
        public void SetFallbackLocation(double longitude, double latitude)
        {
            fallbackLocation.Longitude = longitude;
            fallbackLocation.Latitude = latitude;
        }

        public static async Task<LocationFromIPAddress> GetLocationFromIP()
        {
            HttpClient httpClient = new HttpClient();
            try
            {
                HttpResponseMessage response = await httpClient.GetAsync("https://freegeoip.app/json/").ConfigureAwait(false);
                string responseBodyStr = response.Content.ReadAsStringAsync().Result;
                LocationFromIPAddress location = Messaging<LocationFromIPAddress>.Deserialize(responseBodyStr);
                return location;
            }
            catch (Exception)
            {
                return new LocationFromIPAddress()
                {
                    latitude = 37.3382f,
                    longitude = 121.8863f
                };
            }
        }

        [DataContract]
        public class LocationFromIPAddress
        {
            [DataMember]
            public float longitude;
            [DataMember]
            public float latitude;
        }

        public static class Messaging<T>
        {
            public static T Deserialize(string jsonString)
            {
                MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(jsonString ?? ""));
                return Deserialize(ms);
            }

            public static T Deserialize(Stream stream)
            {
                DataContractJsonSerializer deserializer = new DataContractJsonSerializer(typeof(T));
                T t = (T)deserializer.ReadObject(stream);
                return t;
            }
        }
    }
}
