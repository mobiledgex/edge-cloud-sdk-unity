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
using System.Net.Sockets;

/*
* Helper functions, private functions, and exceptions used to implement MobiledgeXIntegration wrapper functions
*/

namespace MobiledgeX
{
    public class AppPortException : Exception
    {
        public AppPortException(string message)
        : base(message)
        {
        }

        public AppPortException(string message, Exception innerException)
        : base(message, innerException)
        {
        }
    }

    public struct Location
    {
        public Location(double longitude, double latitude)
        {
            Longitude = longitude;
            Latitude = latitude;
        }

        public double Longitude { get; set; }
        public double Latitude { get; set; }
    }

    public partial class MobiledgeXIntegration
    {
        /// Call once, or when the carrier changes. May throw DistributedMatchEngine.HttpException.
        /// <summary>
        /// Wrapper for Register Client. First call to establish the connection with your backend(server) deployed on MobiledgeX
        /// </summary>
        /// <returns>bool Task</returns>
        public async Task<bool> Register()
        {
            latestRegisterStatus = false;

            RegisterClientRequest req = matchingEngine.CreateRegisterClientRequest(orgName, appName, appVers, developerAuthToken.Length > 0 ? developerAuthToken : null);

            Debug.Log("MobiledgeX: OrgName: " + req.org_name);
            Debug.Log("MobiledgeX: AppName: " + req.app_name);
            Debug.Log("MobiledgeX: AppVers: " + req.app_vers);

            RegisterClientReply reply;
            try
            {
                reply = await matchingEngine.RegisterClient(req);
            }
            catch (HttpException httpe)
            {
                throw new RegisterClientException("RegisterClient Exception: " + httpe.Message + ", HTTP StatusCode: " + httpe.HttpStatusCode + ", API ErrorCode: " + httpe.ErrorCode + "\nStack: " + httpe.StackTrace);
            }

            if (reply == null)
            {
                throw new RegisterClientException("RegisterClient returned null.");
            }
            if (reply.status != ReplyStatus.RS_SUCCESS)
            {
                throw new RegisterClientException("Bad RegisterClient. RegisterClient status is " + reply.status);
            }

            latestRegisterStatus = true;
            return true;
        }

        /// <summary>
        /// Wrapper for FindCloudlet. Will find the "nearest" cloudlet hosting the application backend
        /// To use Performance mode. Call UseFindCloudletPerformanceMode(true)
        /// </summary>
        /// <returns>FindCloudletReply Task</returns>
        public async Task<bool> FindCloudlet()
        {
            latestFindCloudletReply = null;

            if (!latestRegisterStatus)
            {
                Debug.LogError("MobiledgeX: Last RegisterClient was unsuccessful. FindCloudlet requires a succesful RegisterClient");
                throw new FindCloudletException("Last RegisterClient was unsuccessful. Call RegisterClient again before FindCloudlet");
            }

            location = GetLocationFromDevice();
            UpdateCarrierName();

            FindCloudletRequest req = matchingEngine.CreateFindCloudletRequest(location, carrierName);
            FindCloudletReply reply;
            try
            {
                reply = await matchingEngine.FindCloudlet(req, mode);
            }
            catch (HttpException httpe)
            {
                throw new FindCloudletException("FindCloudlet Exception: " + httpe.Message + ", HTTP StatusCode: " + httpe.HttpStatusCode + ", API ErrorCode: " + httpe.ErrorCode + "\nStack: " + httpe.StackTrace);
            }


            if (reply == null)
            {
                throw new FindCloudletException("FindCloudletReply returned null. Make Sure you created App Instances for your Application and they are deployed in the correct region.");
            }
            if (reply.status != FindCloudletReply.FindStatus.FIND_FOUND)
            {
                throw new FindCloudletException("Unable to findCloudlet. Status is " + reply.status);
            }

            latestFindCloudletReply = reply;
            latestAppPortList = reply.ports;
            return true;
        }

        /// <summary>
        /// Gets the location from the cellular device, Location is needed for Finding Cloudlet and Location Verification
        /// </summary>
        /// <returns>Loc</returns>
        private Loc GetLocationFromDevice()
        {
            // Location is ephemeral, so retrieve a new location from the platform. May return 0,0 which is
            // technically valid, though less likely real, as of writing.
            Loc loc = new Loc();

#if UNITY_EDITOR
            Debug.Log("MobiledgeX: Cannot Get location in Unity Editor. Returning fallback location. Developer can configure fallback location with SetFallbackLocation");
            loc.longitude = fallbackLocation.Longitude;
            loc.latitude = fallbackLocation.Latitude;
            return loc;
#else
            loc = LocationService.RetrieveLocation();
            // 0f and 0f are hard zeros if no location service.
            if (loc.longitude == 0f && loc.latitude == 0f)
            {
                Debug.LogError("LocationServices returned a location at (0,0)");
            }
            return loc;               
#endif        
        }

        /// <summary>
        /// Updates the carrier name to be used in FindCloudlet and VerifyLocation calls
        /// </summary>
        private void UpdateCarrierName()
        {
            carrierName = matchingEngine.GetCarrierName();
        }

        /// <summary>
        /// Checks whether the default netowrk data path Edge is Enabled on the device or not, Edge requires connections to run over cellular interface.
        /// This status is independent of the UseWiFiOnly setting.
        /// </summary>
        /// <returns>bool</returns>
        public bool IsNetworkDataPathEdgeEnabled() {
            string wifiIpV4 = null;
            string wifiIpV6 = null;

            if (matchingEngine.netInterface.HasWifi())
            {
                string wifi = matchingEngine.GetAvailableWiFiName(matchingEngine.netInterface.GetNetworkInterfaceName());
                wifiIpV6 = matchingEngine.netInterface.GetIPAddress(wifi, AddressFamily.InterNetwork);
                wifiIpV4 = matchingEngine.netInterface.GetIPAddress(wifi, AddressFamily.InterNetworkV6);
            }

            // HasCellular() is best effort due to variable network names and queriable status.
            if (matchingEngine.netInterface.HasCellular() && wifiIpV4 == null && wifiIpV6 == null) {
                return true;
            }
            return false;
        }

#if UNITY_IOS
        /// <summary>
        /// Function for IOS that checks if device is in different country from carrier network
        /// </summary>
        /// <returns>bool</returns>
        public async Task<bool> IsRoaming()
        {
            location = GetLocationFromDevice();
            return await carrierInfoClass.IsRoaming(location.longitude, location.latitude);
        }
#endif

        /// <summary>
        /// Checks whether Edge is Enabled on the device or not, Edge requires connections to run over cellular interface
        /// </summary>
        /// <param name="proto">GetConnectionProtocol (TCP, UDP, HTTP, Websocket)</param>
        /// <returns>bool</returns>
        private bool IsEdgeEnabled(GetConnectionProtocol proto)
        {
            if (matchingEngine.useOnlyWifi)
            {
#if UNITY_EDITOR
                Debug.Log("MobiledgeX: useWifiOnly must be false in production. useWifiOnly can be used only for testing");
                return true;
#else
                Debug.Log("MobiledgeX: useOnlyWifi must be false to enable edge connection");
                return false;
#endif
            }

            if (proto == GetConnectionProtocol.TCP || proto == GetConnectionProtocol.UDP)
            {
                if (!matchingEngine.netInterface.HasCellular())
                {
                    Debug.Log(proto + " connection requires a cellular interface to run connection over edge.");
                    return false;
                }
            }
            else
            {
                // Connections where we cannot bind to cellular interface default to wifi if wifi is up
                // We need to make sure wifi is off
                if (!matchingEngine.netInterface.HasCellular() || matchingEngine.netInterface.HasWifi())
                {
                    Debug.Log("MobiledgeX: " + proto + " connection requires the cellular interface to be up and the wifi interface to be off to run connection over edge.");
                    return false;
                }
            }

            string cellularIPAddress = matchingEngine.netInterface.GetIPAddress(
                    matchingEngine.GetAvailableCellularName(matchingEngine.netInterface.GetNetworkInterfaceName()));
            if (cellularIPAddress == null)
            {
                Debug.Log("MobiledgeX: Unable to find ip address for local cellular interface.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Communication Protocols supported by MobiledgeX GetConnection
        /// </summary>
        private enum GetConnectionProtocol
        {
            TCP,
            UDP,
            HTTP,
            Websocket
        }
    }
}
