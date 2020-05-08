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
using System.Collections.Generic;
using UnityEngine;
using DistributedMatchEngine;
using System.Threading.Tasks;
using DistributedMatchEngine.PerformanceMetrics;
using System.Net.WebSockets;
using System.Net.Http;
/*
 * MobiledgeX MatchingEngine SDK integration has an additional application side
 * "PlatformIntegration.cs/m" file for Android, IOS, or other platform integration
 * with Unity.
 * 
 * This is necessary to retrieve carrier information so that the SDK can provide
 * Edge Cloudlet discovery.
 */
namespace MobiledgeX
{
    public class MobiledgeXIntegration
    {

        /// <summary>
        /// Scriptable Object Holding MobiledgeX Settings (OrgName, AppName, AppVers)
        /// </summary>
        static MobiledgeXSettings settings = Resources.Load<MobiledgeXSettings>("MobiledgeXSettings");
        PlatformIntegration pIntegration;
        public MatchingEngine me;
        public NetTest netTest;
        /*
         * These are "carrier independent" settings for demo use:
         */
        public string carrierName { get; set; } = ""; // carrierName depends on the available subscriber SIM card and roaming carriers, and must be supplied by platform API.
        public static string orgName { get; set; } = ""; // Organization name
        public static string appName { get; set; } = ""; // Your appName, if you have created this in the MobiledgeX console.
        public static string appVers { get; set; } = ""; // Your app version uploaded to the docker registry.
        public static int tcpPort { get; set; } = 0; // Your exposed tcp port mapped at MobiledgeX Console.
        public static int udpPort { get; set; } = 0; // Your exposed udp port mapped at MobiledgeX Console.
        public string developerAuthToken { get; set; } = ""; // This is an opaque string value supplied by the developer.
        public uint cellID { get; set; } = 0;
        public string uniqueIDType { get; set; } = "";
        public string uniqueID { get; set; } = "";
        public Tag[] tags { get; set; } = new Tag[0];
        Loc location = new Loc();

        public MobiledgeXIntegration()
        {
            // Set the platform specific way to get SIM carrier information.
            pIntegration = new PlatformIntegration();

            // Platform integration needs to initialize first:
            me = new MatchingEngine(pIntegration.CarrierInfo, pIntegration.NetInterface, pIntegration.UniqueID);

            // Optional NetTesting.
            netTest = new NetTest(me);
        }

        public void useWifiOnly(bool useWifi)
        {
            me.useOnlyWifi = useWifi;
        }

        public string GetCarrierName()
        {
            return me.carrierInfo.GetMccMnc();
        }

        public async Task<Loc> GetLocationFromDevice()
        {
            
            // Location is ephemeral, so retrieve a new location from the platform. May return 0,0 which is
            // technically valid, though less likely real, as of writing.
            Loc loc = await LocationService.RetrieveLocation();

            // If in UnityEditor, 0f and 0f are hard zeros as there is no location service.
            if (loc.longitude == 0f && loc.latitude == 0f)
            {
                // Likely not in the ocean. We'll chose something for demo FindCloudlet purposes:
                loc.longitude = -121.8863286;
                loc.latitude = 37.3382082;
            }
            return loc;
        }

        // These are just thin wrappers over the SDK to show how to use them:
        // Call once, or when the carrier changes. May throw DistributedMatchEngine.HttpException.
        public async Task<bool> Register()
        {
            await ConfigureMobiledgeXSettings(LocationNeeded:false);

            RegisterClientRequest req = me.CreateRegisterClientRequest(orgName, appName, appVers);
            Debug.Log("OrgName: " + req.org_name);
            Debug.Log("AppName: " + req.app_name);
            Debug.Log("AppVers: " + req.app_vers);
            RegisterClientReply reply = await me.RegisterClient(req);

            return (reply.status == ReplyStatus.RS_SUCCESS);
        }

        public async Task<FindCloudletReply> FindCloudlet()
        {
            await ConfigureMobiledgeXSettings();
            FindCloudletRequest req = me.CreateFindCloudletRequest(location, carrierName);
            FindCloudletReply reply = await me.FindCloudlet(req);

            return reply;
        }

        public async Task<bool> VerifyLocation()
        {
            await ConfigureMobiledgeXSettings();
            VerifyLocationRequest req = me.CreateVerifyLocationRequest(location, carrierName);
            VerifyLocationReply reply = await me.VerifyLocation(req);

            // The return is not binary, but one can decide the particular app's policy
            // on pass or failing the location check. Not being verified or the country
            // not matching at all is on such policy decision:
            
            // GPS and Tower Status:
            switch (reply.gps_location_status)
            {
                case VerifyLocationReply.GPSLocationStatus.LOC_ROAMING_COUNTRY_MISMATCH:
                case VerifyLocationReply.GPSLocationStatus.LOC_ERROR_UNAUTHORIZED:
                case VerifyLocationReply.GPSLocationStatus.LOC_ERROR_OTHER:
                case VerifyLocationReply.GPSLocationStatus.LOC_UNKNOWN:
                    return false;
            }

            switch (reply.tower_status)
            {
                case VerifyLocationReply.TowerStatus.NOT_CONNECTED_TO_SPECIFIED_TOWER:
                case VerifyLocationReply.TowerStatus.TOWER_UNKNOWN:
                    return false;
            }

            // Distance? A negative value means no verification was done.
            if (reply.gps_location_accuracy_km < 0f)
            {
                return false;
            }

            // A per app policy decision might be 0.5 km, or 25km, or 100km:
            if (reply.gps_location_accuracy_km < 100f)
            {
                return true;
            }

            // Too far for this app.
            return false;
        }

        // Typical developer workflow to get connection to application backend
        public async Task<ClientWebSocket> GetWebsocketConnection(string path)
        {
            await ConfigureMobiledgeXSettings();

            FindCloudletReply findCloudletReply = await me.RegisterAndFindCloudlet(orgName, appName, appVers, location, carrierName);
            if (findCloudletReply == null)
            {
                Debug.Log("cannot find findCloudletReply");
            }

            Dictionary<int, AppPort> appPortsDict = me.GetTCPAppPorts(findCloudletReply);
            int public_port = findCloudletReply.ports[0].public_port; // We happen to know it's the first one.
            AppPort appPort = appPortsDict[public_port];
            return await me.GetWebsocketConnection(findCloudletReply, appPort, public_port, tcpPort, path);
        }

        public async Task<String> GetURI()
        {
            await ConfigureMobiledgeXSettings();
            string host="";
            string port="";
            NetTest.Site site;

            Debug.Log("Calling DME to register client...");
            bool registered = false;
            registered = await Register();

            if (!registered)
            {
                Debug.Log("Exceptions, or app not found. Not Registered!");
                return null;
            }
            else
            {
                FindCloudletReply reply;
                Debug.Log("Finding Cloudlet...");
                reply = await FindCloudlet();


                // Handle reply status:
                bool found = false;
                if (reply == null)
                {
                    Debug.Log("FindCloudlet call failed.");
                    return "";
                }

                switch (reply.status)
                {
                    case FindCloudletReply.FindStatus.FIND_UNKNOWN:
                        Debug.Log("FindCloudlet status unknown. No edge cloudlets.");
                        break;
                    case FindCloudletReply.FindStatus.FIND_NOTFOUND:
                        Debug.Log("FindCloudlet Found no edge cloudlets in range.");
                        break;
                    case FindCloudletReply.FindStatus.FIND_FOUND:
                        found = true;
                        break;

                }

                if (found)
                {
                    // Edge cloudlets found!
                    Debug.Log("Edge cloudlets found!");

                    // Where is this app specific edge enabled cloud server:
                    Debug.Log("GPS location: longitude: " + reply.cloudlet_location.longitude + ", latitude: " + reply.cloudlet_location.latitude);

                    // Where is the URI for this app specific edge enabled cloud server:
                    Debug.Log("fqdn: " + reply.fqdn);
                    // AppPorts?
                    Debug.Log("On ports: ");

                    foreach (AppPort ap in reply.ports)
                    {
                        Debug.Log("Port: proto: " + ap.proto + ", prefix: " +
                            ap.fqdn_prefix + ", path_prefix: " + ap.path_prefix + ", port: " +
                            ap.public_port);

                        // We're looking for one of the TCP app ports:
                        if (ap.proto == LProto.L_PROTO_TCP)
                        {
                            // Add to test targets.
                            if (ap.path_prefix == "")
                            {
                                site = new NetTest.Site
                                {
                                    host = ap.fqdn_prefix + reply.fqdn,
                                    port = ap.public_port
                                };
                                site.testType = NetTest.TestType.CONNECT;
                            }
                            else
                            {
                                site = new NetTest.Site
                                {
                                    L7Path = ap.fqdn_prefix + reply.fqdn + ":" + ap.public_port + ap.path_prefix
                                };
                                site.testType = NetTest.TestType.CONNECT;
                            }
                        }
                    }
                    netTest.doTest(true);
                    host = reply.ports[0].fqdn_prefix + reply.fqdn;
                    port = reply.ports[0].public_port + reply.ports[0].path_prefix;
                }
            }
            return "https://" + host + ":" + port;
        }

        /// <summary>
        /// Wether Edge is Enabled on the device or not, Edge requires connections to run over cellular interface
        /// </summary>
        /// <param name="proto">GetConnectionProtocols (TCP, UDP, HTTP, WebSocket) </param>
        /// <returns></returns>
        public bool IsEdgeEnabled()
        {
            if (me.useOnlyWifi)
          {
#if UNITY_EDITOR
                Debug.LogWarning("MobiledgeX: Make sure useWifiOnly is not in production level Only for testing");
                return true;
#else
                throw new Exception("Device is not edge enabled. Please switch to cellular connection or use server in public cloud");
                return false;
                
#endif
            }
            if (!me.netInterface.HasCellular())
            {
#if UNITY_EDITOR
                Debug.LogWarning("MobiledgeX: For Testing in UnityEditor use integration.useWifiOnly(true) ,\n In Production EdgeConnection requires the cellular interface to be up and the wifi interface to be off to run connection over edge.");
#else
                Debug.LogError("MobiledgeX: Connection requires the cellular interface to be up and the wifi interface to be off to run connection over edge.");
                return false;
#endif
            }

            string cellularIPAddress = me.netInterface.GetIPAddress(me.netInterface.GetNetworkInterfaceName().CELLULAR);
            if (cellularIPAddress == null)
            {
                Debug.LogError("MobiledgeX: Unable to find ip address for local cellular interface.");
                return false;
            }
        
            return true;
        }

        /// <summary>
        /// Enum for holding GetConnection Protocols (TCP, UDP, HTTP, Websocket)
        /// </summary>
        public enum GetConnectionProtocols
        {
          TCP,
          UDP,
          HTTP,
          Websocket
        }

        /// <summary>
        /// Use MobiledgeXSetting Scriptable object to load orgName, appName, appVers 
        /// </summary>
        public async Task ConfigureMobiledgeXSettings(bool LocationNeeded = true)
        {
            // Setting Application Definitions
            orgName = settings.orgName;
            appName = settings.appName;
            appVers = settings.appVers;
            tcpPort = settings.TCP_Port;
            udpPort = settings.UDP_Port;
            // Checking if edge is enabled on the device used or not
            if (!IsEdgeEnabled())
            {
                return;
            }
            // Getting location from the device, needed for FindCloudlet and VerifyLocation
            // Location is ephemeral, so retrieve a new location from the platform. May return 0,0 which is
            // technically valid, though less likely real, as of writing.

            if (LocationNeeded)
            {
                location = await GetLocationFromDevice();
            }

            // Getting Carrier Name
            if (me.useOnlyWifi)
            {
                carrierName = MatchingEngine.wifiCarrier;
            }
            else
            {
                string CarrierNameFromDevice = GetCarrierName();
                if (CarrierNameFromDevice == null)
                {
                    Debug.LogError("Missing CarrierName for RegisterClient.");
                    return;

                }
                carrierName = CarrierNameFromDevice;
            }

        }
    }
}