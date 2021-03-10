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
using System.Net.Sockets;
using System.Collections.Generic;

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
        /// <summary>
        /// set to true to use Fallback Location instead of the device location in production, use SetFallbackLocation() to define the FallbackLocation
        /// set to true if you are using a device that doesn't provide location data such as magic leap
        /// Fallback location is used by default in Unity Editor
        /// </summary>
        public bool useFallbackLocation = false;

        /// <summary>
        /// You don't need this option in UnityEditor by default the region used will be the region selected in MobiledgeX Editor Window
        /// Set to true to use the Region selected in MobiledgeX Editor Window in production
        /// It's not recommended to use this option in production since the SDK will automatically select the best region.
        /// You can use this option for non-sim card devices such as Oculus or MagicLeap
        /// </summary>
        public bool useSelectedRegionInProduction = false;

        /// Call once, or when the carrier changes. May throw DistributedMatchEngine.HttpException.
        /// <summary>
        /// Wrapper for Register Client. First call to establish the connection with your backend(server) deployed on MobiledgeX
        /// </summary>
        /// <returns>bool Task</returns>
        public async Task<bool> Register(string dmeHost = null, uint dmePort = 0)
        {
            latestRegisterStatus = false;

            RegisterClientRequest req = matchingEngine.CreateRegisterClientRequest(orgName, appName, appVers, developerAuthToken.Length > 0 ? developerAuthToken : null);

            Logger.Log("OrgName: " + req.org_name);
            Logger.Log("AppName: " + req.app_name);
            Logger.Log("AppVers: " + req.app_vers);

            try
            {
                await UpdateLocationAndCarrierInfo();
            }
            catch (CarrierInfoException cie)
            {
                Debug.LogError("MobiledgeX: Register Exception: " + cie.Message);
                throw new RegisterClientException(cie.Message);
            }

            RegisterClientReply reply = null;
            try
            {
                if (dmeHost != null && dmePort != 0)
                {
                    Logger.Log("Doing Register Client with DME: " + dmeHost + ", p: " + dmePort + " with req: " + req);
                    reply = await matchingEngine.RegisterClient(dmeHost, dmePort, req);
                }
                else
                { 
                    if (!useSelectedRegionInProduction)
                    {
#if UNITY_EDITOR
                        Logger.Log("Doing Register Client with DME: " + region + ", p: " + MatchingEngine.defaultDmeRestPort + " with req: " + req);
                        Logger.LogWarning("Region Selection will work only in UnityEditor not on Mobile Devices");
                        reply = await matchingEngine.RegisterClient(region, MatchingEngine.defaultDmeRestPort, req);
#else
                        Logger.Log("Doing Register Client, with req: " + req);
                        reply = await matchingEngine.RegisterClient(req);
#endif
                    }
                    else
                    {
                        Logger.Log("MobiledgeX: Doing Register Client with DME: " + region + ", p: " + MatchingEngine.defaultDmeRestPort + " with req: " + req);
                        reply = await matchingEngine.RegisterClient(region, MatchingEngine.defaultDmeRestPort, req);
                    }
                }
            }
            catch (HttpException httpe)
            {
                Debug.LogError("MobiledgeX: RegisterClient HttpException: " + httpe.Message);
                throw new RegisterClientException("RegisterClient Exception: " + httpe.Message + ", HTTP StatusCode: " + httpe.HttpStatusCode + ", API ErrorCode: " + httpe.ErrorCode + "\nStack: " + httpe.StackTrace);
            }
            catch (Exception e)
            {
                throw new RegisterClientException("MobiledgeX: RegisterClient Exception Type: " + e.GetType() + ", Message: " + e.Message + ", InnerException : " + e.InnerException + "\nStack: " + e.StackTrace);
            }
            finally
            {
                if (reply == null)
                {
                    Debug.LogError("MobiledgeX: Register reply NULL!");
                    throw new RegisterClientException("RegisterClient returned null.");
                }
                if (reply.status != ReplyStatus.RS_SUCCESS)
                {
                    Debug.LogError("MobiledgeX: Register Failed: " + reply.status);
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
            if (!latestRegisterStatus)
            {
                throw new FindCloudletException("Last RegisterClient was unsuccessful. Call RegisterClient again before FindCloudlet");
            }

            try
            {
                if(fallbackLocation.Longitude == 0 && fallbackLocation.Latitude == 0)
                {
                    LocationFromIPAddress locationFromIP = await GetLocationFromIP();
                    fallbackLocation = new Location(locationFromIP.longitude, locationFromIP.latitude);
                }
                await UpdateLocationAndCarrierInfo();
            }
            catch (CarrierInfoException cie)
            {
                Debug.LogError("MobiledgeX: FindCloudlet CarrierInfoException: " + cie.Message);
                throw new FindCloudletException(cie.Message);
            }

            FindCloudletReply reply = null;
            try
            {
                if (location == null)
                {
                    throw new FindCloudletException("Location must not be null!");
                }
                Logger.Log("FindCloudlet Location: " + location.longitude + ", lat: " + location.latitude);
                FindCloudletRequest req = matchingEngine.CreateFindCloudletRequest(location, "");
                if (dmeHost != null && dmePort != 0)
                {
                    Logger.Log("Doing FindCloudlet with DME: " + dmeHost + ", p: " + dmePort + " with req: " + req);
                    reply = await matchingEngine.FindCloudlet(dmeHost, dmePort, req, mode);
                }
                else
                { 
                    if (!useSelectedRegionInProduction)
                    {
#if UNITY_EDITOR
                        Logger.Log("Doing FindCloudlet with DME: " + region + ", p: " + MatchingEngine.defaultDmeRestPort + " with req: " + req);
                        Logger.LogWarning("Region Selection will work only in UnityEditor not on Mobile Devices");
                        reply = await matchingEngine.FindCloudlet(region, MatchingEngine.defaultDmeRestPort, req);
#else
                        Logger.Log("Doing FindCloudlet, with req: " + req);
                        reply = await matchingEngine.FindCloudlet(req, mode);
#endif
                    }
                    else
                    {
                        Logger.Log("Doing FindCloudlet with DME: " + region + ", p: " + MatchingEngine.defaultDmeRestPort + " with req: " + req);
                        reply = await matchingEngine.FindCloudlet(region, MatchingEngine.defaultDmeRestPort, req, mode);
                    }
                }
            }
            catch (HttpException httpe)
            {
                throw new FindCloudletException("FindCloudlet Exception: " + httpe.Message + ", HTTP StatusCode: " + httpe.HttpStatusCode + ", API ErrorCode: " + httpe.ErrorCode + "\nStack: " + httpe.StackTrace);
            }
            catch (Exception e)
            {
                throw new FindCloudletException("FindCloudletException Type: " + e.GetType() + ", Message: " + e.Message + ", InnerException : " + e.InnerException + "\nStack: " + e.StackTrace);
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

            Logger.Log("FindCloudlet with DME result: " + reply.status);
            latestFindCloudletReply = reply;
            latestAppPortList = reply.ports;
            return reply.status == FindCloudletReply.FindStatus.FIND_FOUND;
        }

        /// <summary>
        /// Gets the location from the cellular device, Location is needed for Finding Cloudlet and Location Verification
        /// </summary>
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        private async Task UpdateLocationAndCarrierInfo()
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            UpdateLocationFromDevice();
#if UNITY_IOS
            bool isRoaming = await IsRoaming();
            if (isRoaming) {
                UseWifiOnly(true);
                Logger.Log("IOS Device is roaming. Unable to get current network information from IOS device. Switching to wifi mode");
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
            Logger.Log("Cannot Get location in Unity Editor. Returning fallback location. You can configure fallback location with SetFallbackLocation");
            location.longitude = fallbackLocation.Longitude;
            location.latitude = fallbackLocation.Latitude;
#else
            if (useFallbackLocation)
            {
                location.longitude = fallbackLocation.Longitude;
                location.latitude = fallbackLocation.Latitude;
                Logger.Log("Using FallbackLocation ["+location.latitude+", "+location.longitude+"]");
            }
            else
            {
                location = LocationService.RetrieveLocation();
                // 0f and 0f are hard zeros if no location service.
                if (location.longitude == 0f && location.latitude == 0f)
                {
                    Debug.LogError("LocationServices returned a location at (0,0)");
                }
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

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public async Task<Dictionary<string, string>> GetDeviceInfo()
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            var deviceInfo = matchingEngine.deviceInfo.GetDeviceInfo();
#if UNITY_IOS
            if (deviceInfo == null || deviceInfo.Count == 0) {
                return new Dictionary<string, string>();
            }
            UpdateLocationFromDevice();
            deviceInfo["NetworkCountryIso"] = await carrierInfoClass.ConvertGPSToISOCountryCode(location.longitude, location.latitude);
#endif
            return deviceInfo;
        }

        /// <summary>
        /// Checks whether the default netowrk data path Edge is Enabled on the device or not, Edge requires connections to run over cellular interface.
        /// This status is independent of the UseWiFiOnly setting.
        /// </summary>
        /// <returns>bool</returns>
        public bool IsNetworkDataPathEdgeEnabled() {
            string wifiIpV4 = null;
            string wifiIpV6 = null;

            if (!MatchingEngine.EnableEnhancedLocationServices)
            {
                Logger.LogWarning("MatchingEngine EnableEnhancedLocationServices is set to false.");
                return false;
            }

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
                Logger.Log("useWifiOnly must be false in production. useWifiOnly can be used only for testing");
                return true;
#else
                Logger.Log("useOnlyWifi must be false to enable edge connection");
                return false;
#endif
            }

            if (!MatchingEngine.EnableEnhancedLocationServices)
            {
                Logger.LogWarning("MatchingEngine EnableEnhancedLocationServices is set to false.");
                return false;
            }

            if (proto == GetConnectionProtocol.TCP || proto == GetConnectionProtocol.UDP)
            {
                if (!matchingEngine.netInterface.HasCellular())
                {
                    Logger.Log(proto + " connection requires a cellular interface to run connection over edge.");
                    return false;
                }
            }
            else
            {
                // Connections where we cannot bind to cellular interface default to wifi if wifi is up
                // We need to make sure wifi is off
                if (!matchingEngine.netInterface.HasCellular() || matchingEngine.netInterface.HasWifi())
                {
                    Logger.Log(proto + " connection requires the cellular interface to be up and the wifi interface to be off to run connection over edge.");
                    return false;
                }
            }

            // Check both IP stacks:
            string cellularIPAddressV6 = matchingEngine.netInterface.GetIPAddress(
                    matchingEngine.GetAvailableCellularName(matchingEngine.netInterface.GetNetworkInterfaceName()),
                    AddressFamily.InterNetworkV6);
            string cellularIPAddressV4 = matchingEngine.netInterface.GetIPAddress(
                    matchingEngine.GetAvailableCellularName(matchingEngine.netInterface.GetNetworkInterfaceName()),
                    AddressFamily.InterNetwork);
            if (cellularIPAddressV4 == null && cellularIPAddressV6 == null)
            {
                Logger.Log("Unable to find ip address for local cellular interface.");
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
