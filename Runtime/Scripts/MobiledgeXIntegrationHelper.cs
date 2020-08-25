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
        public async Task<bool> Register(string dmeHost = null, uint dmePort = 0)
        {
            latestRegisterStatus = false;

            RegisterClientRequest req = matchingEngine.CreateRegisterClientRequest(orgName, appName, appVers, developerAuthToken.Length > 0 ? developerAuthToken : null);

            Debug.Log("MobiledgeX: OrgName: " + req.org_name);
            Debug.Log("MobiledgeX: AppName: " + req.app_name);
            Debug.Log("MobiledgeX: AppVers: " + req.app_vers);

            try
            {
                await UpdateLocationAndCarrierInfo();
            }
            catch (CarrierInfoException cie)
            {
                Debug.Log("Register Exception: " + cie.Message);
                throw new RegisterClientException(cie.Message);
            }

            RegisterClientReply reply = null;
            try
            {
                if (dmeHost == null || dmePort == 0)
                {
                    Debug.Log("Doing Register Client, with req: " + req);
                    reply = await matchingEngine.RegisterClient(req);
                }
                else
                {
                    Debug.Log("Doing Register Client with DME: " + dmeHost + ", p: " + dmePort + " with req: " + req);
                    reply = await matchingEngine.RegisterClient(dmeHost, dmePort, req);
                }
            }
            catch (HttpException httpe)
            {
                Debug.Log("RegisterClient HttpException: " + httpe.Message);
                throw new RegisterClientException("RegisterClient Exception: " + httpe.Message + ", HTTP StatusCode: " + httpe.HttpStatusCode + ", API ErrorCode: " + httpe.ErrorCode + "\nStack: " + httpe.StackTrace);
            }
            catch (Exception e)
            {
                Debug.Log("RegisterClient Exception: " + e.Message);
                throw e;
            }
            finally
            {
                if (reply == null)
                {
                    Debug.Log("Register reply NULL!");
                    throw new RegisterClientException("RegisterClient returned null.");
                }
                if (reply.status != ReplyStatus.RS_SUCCESS)
                {
                    Debug.Log("Register Failed: " + reply.status);
                    throw new RegisterClientException("Bad RegisterClient. RegisterClient status is " + reply.status);
                }
            }

            latestRegisterStatus = true;
            return true;
        }

        /// <summary>
        /// Wrapper for FindCloudlet. Will find the "nearest" cloudlet hosting the application backend
        /// To use Performance mode. Call UseFindCloudletPerformanceMode(true)
        /// </summary>
        /// <returns>FindCloudletReply Task</returns>
        public async Task<bool> FindCloudlet(string dmeHost = null, uint dmePort = 0)
        {
            latestFindCloudletReply = null;
            Debug.Log("FindCloudlet to DME: " + dmeHost);
            if (!latestRegisterStatus)
            {
                throw new FindCloudletException("Last RegisterClient was unsuccessful. Call RegisterClient again before FindCloudlet");
            }

            try
            {
                await UpdateLocationAndCarrierInfo();
            }
            catch (CarrierInfoException cie)
            {
                Debug.Log("FindCloudlet CarrierInfoException: " + cie.Message);
                throw new FindCloudletException(cie.Message);
            }

            FindCloudletReply reply = null;
            try
            {
                if (location == null)
                {
                    throw new FindCloudletException("Location must not be null!");
                }
                Debug.Log("FindCloudlet Location: " + location.longitude + ", lat: " + location.latitude);
                FindCloudletRequest req = matchingEngine.CreateFindCloudletRequest(location, "");

                if (dmeHost == null || dmePort == 0)
                {
                    Debug.Log("Doing FindCloudlet, with req: " + req);
                    reply = await matchingEngine.FindCloudlet(req, mode);
                }
                else
                {
                    Debug.Log("Doing FindCloudlet with DME: " + dmeHost + ", p: " + dmePort + " with req: " + req);
                    reply = await matchingEngine.FindCloudlet(dmeHost, dmePort, req, mode);
                }
            }
            catch (HttpException httpe)
            {
                throw new FindCloudletException("FindCloudlet Exception: " + httpe.Message + ", HTTP StatusCode: " + httpe.HttpStatusCode + ", API ErrorCode: " + httpe.ErrorCode + "\nStack: " + httpe.StackTrace);
            }
            catch (Exception e)
            {
                throw new FindCloudletException(e.Message);
            }
            finally
            {
                if (reply == null)
                {
                    throw new FindCloudletException("FindCloudletReply returned null. Make Sure you created App Instances for your Application and they are deployed in the correct region.");
                }
                if (reply.status != FindCloudletReply.FindStatus.FIND_FOUND)
                {
                    throw new FindCloudletException("Unable to findCloudlet. Status is " + reply.status);
                }
            }

            Debug.Log("FindCloudlet with DME result: " + reply.status);
            latestFindCloudletReply = reply;
            latestAppPortList = reply.ports;
            return reply.status == FindCloudletReply.FindStatus.FIND_FOUND;
        }

        /// <summary>
        /// Gets the location from the cellular device, Location is needed for Finding Cloudlet and Location Verification
        /// </summary>
        private async Task UpdateLocationAndCarrierInfo()
        {
            UpdateLocationFromDevice();
#if UNITY_IOS
            bool isRoaming = await IsRoaming();
            if (isRoaming) {
                UseWifiOnly(true);
                Debug.Log("IOS Device is roaming. Unable to get current network information from IOS device. Switching to wifi mode");
            }
#endif
            UpdateCarrierName();
        }

        /// <summary>
        /// Updates the location from the cellular device, Location is needed for Finding Cloudlet and Location Verification
        /// </summary>
        private void UpdateLocationFromDevice()
        {
            // Location is ephemeral, so retrieve a new location from the platform. May return 0,0 which is
            // technically valid, though less likely real, as of writing.

#if UNITY_EDITOR
            Debug.Log("MobiledgeX: Cannot Get location in Unity Editor. Returning fallback location. Developer can configure fallback location with SetFallbackLocation");
            location.longitude = fallbackLocation.Longitude;
            location.latitude = fallbackLocation.Latitude;
#elif PLATFORM_LUMIN
            Debug.Log("MobiledgeX: Cannot Get location on Lumin Platform. Returning fallback location. Developer can configure fallback location with SetFallbackLocation");
            location.longitude = fallbackLocation.Longitude;
            location.latitude = fallbackLocation.Latitude;
#else
            location = LocationService.RetrieveLocation();
            // 0f and 0f are hard zeros if no location service.
            if (location.longitude == 0f && location.latitude == 0f)
            {
                Debug.LogError("LocationServices returned a location at (0,0)");
            }
#endif
    }

        /// <summary>
        /// Updates the carrier name to be used in FindCloudlet and VerifyLocation calls
        /// </summary>
        private void UpdateCarrierName()
        {
            carrierName = matchingEngine.GetCarrierName();
        }

#if UNITY_IOS
        /// <summary>
        /// Function for IOS that checks if device is in different country from carrier network
        /// </summary>
        /// <returns>bool</returns>
        public async Task<bool> IsRoaming()
        {
#if !UNITY_EDITOR
            // 0,0 is fine in Unity Editor
            if (location.longitude == 0 && location.latitude == 0)
            {
                Debug.LogError("Invalid location: (0,0). Please wait for valid location information before checking roaming status.");
                throw new CarrierInfoException("Invalid location: (0,0). Please wait for valid location information before checking roaming status.");
            }
#endif

            try
            {
                return await carrierInfoClass.IsRoaming(location.longitude, location.latitude);
            }
            catch (CarrierInfoException cie)
            {
                Debug.LogError("Unable to get Roaming status. CarrierInfoException: " + cie.Message + ". Assuming device is not roaming");
                return false;
            }
        }
#endif

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
