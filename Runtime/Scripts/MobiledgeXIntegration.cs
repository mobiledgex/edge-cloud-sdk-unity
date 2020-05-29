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
using System.Net.WebSockets;
using System.Net.Http;
using System.Linq;
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
        public string carrierName { get; set; } = ""; // carrierName depends on the available subscriber SIM card and roaming carriers, and must be supplied by platform API.
        public static string orgName { get; set; } = ""; // Organization name
        public static string appName { get; set; } = ""; // Your appName, if you have created this in the MobiledgeX console.
        public static string appVers { get; set; } = ""; // Your app version uploaded to the docker registry.
        public static int tcpPort { get; set; } = 0; // Your exposed TCP port mapped at MobiledgeX Console.
        public static int udpPort { get; set; } = 0; // Your exposed UDP port mapped at MobiledgeX Console.
        public string developerAuthToken { get; set; } = ""; // This is an opaque string value supplied by the developer.
        public uint cellID { get; set; } = 0;
        public string uniqueIDType { get; set; } = "";
        public string uniqueID { get; set; } = "";
        public Tag[] tags { get; set; } = new Tag[0];
        Loc location = new Loc();

        public MobiledgeXIntegration()
        {
            ConfigureMobiledgeXSettings();
            // Set the platform specific way to get SIM carrier information.
            pIntegration = new PlatformIntegration();
            // Platform integration needs to initialize first:
            me = new MatchingEngine(pIntegration.CarrierInfo, pIntegration.NetInterface, pIntegration.UniqueID);
            me.SetMelMessaging(new MelMessaging(appName));
        }

        /// <summary>
        /// Use for testing In UnityEditor, Won't work in Production
        /// </summary>
        /// <param name="useWifi"></param>
        public void useWifiOnly(bool useWifi)
        {
            me.useOnlyWifi = useWifi;
        }

        /// <summary>
        /// Returns the MccMnc (Mobile Country Code Mobile Network Code)
        /// </summary>
        /// <returns></returns>
        public void UpdateCarrierName()
        {
            carrierName = me.GetCarrierName();
            Debug.Log("carriername is " + carrierName);
        }

        /// <summary>
        /// Gets the location from the cellular device, Location is needed for Finding Cloudlet and Location Verification
        /// </summary>
        /// <returns></returns>
        public Loc GetLocationFromDevice()
        {
            // Location is ephemeral, so retrieve a new location from the platform. May return 0,0 which is
            // technically valid, though less likely real, as of writing.
            Loc loc = LocationService.RetrieveLocation();
            // If in UnityEditor, 0f and 0f are hard zeros as there is no location service.
            if (loc.longitude == 0f && loc.latitude == 0f)
            {
                // Likely not in the ocean. We'll chose something for demo FindCloudlet purposes:
                loc.longitude = -121.8863286;
                loc.latitude = 37.3382082;
            }
            return loc;
        }

        // Call once, or when the carrier changes. May throw DistributedMatchEngine.HttpException.
        /// <summary>
        /// Register Client is used to establish the connection with your backend(server) deployed on MobiledgeX
        /// </summary>
        /// <returns>Boolean Value</returns>
        public async Task<bool> Register()
        {
            RegisterClientRequest req = me.CreateRegisterClientRequest(orgName, appName, appVers, developerAuthToken.Length > 0 ? developerAuthToken : null);

            Debug.Log("OrgName: " + req.org_name);
            Debug.Log("AppName: " + req.app_name);
            Debug.Log("AppVers: " + req.app_vers);

            RegisterClientReply reply = await me.RegisterClient(req);
            return (reply.status == ReplyStatus.RS_SUCCESS);
        }

        public async Task<FindCloudletReply> FindCloudlet()
        {
            location = GetLocationFromDevice();
            UpdateCarrierName();

            FindCloudletRequest req = me.CreateFindCloudletRequest(location, carrierName);
            FindCloudletReply reply = await me.FindCloudlet(req);
            return reply;
        }

        /// <summary>
        /// Gets WebsocketConnection Optional Params (string path for ex. roomId and/or specific TCP port)
        /// </summary>
        /// <param name="path">string path for ex. roomId  </param>
        /// <param name="port">Integer TCP port </param>
        /// <returns>ClientWebSocket Connection</returns>
        public async Task<ClientWebSocket> GetWebsocketConnection(string path = "", int port = 0)
        {
            if (!IsEdgeEnabled(GetConnectionProtocols.Websocket))
            {
                throw new Exception("Device is not edge enabled. Please switch to cellular connection or use server in public cloud");
            }

            location = GetLocationFromDevice();
            UpdateCarrierName();

            if (port == 0)
            {
                port = tcpPort;
            }

            FindCloudletReply findCloudletReply = await me.RegisterAndFindCloudlet(orgName, appName, appVers, location, carrierName, developerAuthToken.Length > 0 ? developerAuthToken : null);
            if (findCloudletReply == null)
            {
                Debug.LogError("MobiledgeX: Couldn't Find findCloudletReply, Make Sure you created App Instances for your Application and they are deployed in the correct region.");
                throw new FindCloudletException("No findCloudletReply");
            }

            Dictionary<int, AppPort> appPortsDict = me.GetTCPAppPorts(findCloudletReply);
            if (appPortsDict.Keys.Count < 1)
            {
                Debug.LogError("MobiledgeX: Please make sure you defined the desired TCP Ports in your Application Port Mapping Section on MobiledgeX Console.");
                throw new FindCloudletException("No TCP ports available on your Application");
            }

            if (port == 0)
            {
                port = appPortsDict.OrderBy(kvp => kvp.Key).First().Key;
            }

            try
            {
                AppPort appPort = appPortsDict[port];
                return await me.GetWebsocketConnection(findCloudletReply, appPort, port, 5000, path);
            }
            catch (KeyNotFoundException)
            {
                Debug.LogError("MobiledgeX: Port supplied is not mapped to your Application, Make sure the desired port is defined in your Application Port Mapping Section on MobiledgeX Console.");
                throw new GetConnectionException("TCP " + port + " is not defined on your Application Port Mapping section");
            }
        }

        /// <summary>
        /// Returns the URI for your backend (server) deployed on MobiledgeX
        /// </summary>
        /// <param name="protocol">(LProto) Protocol (TCP, UDP)</param>
        /// <param name="port">(Integer) Desired Port </param>
        /// <returns></returns>
        public async Task<String> GetURI(LProto protocol = LProto.L_PROTO_TCP, int port = 0)
        {
            location = GetLocationFromDevice();
            UpdateCarrierName();

            string uri = "";
            string host = "";

            if (port == 0)
            {
                port = tcpPort;
            }

            Debug.Log("Calling DME to register client...");
            bool registered = false;
            registered = await Register();

            if (!registered)
            {
                Debug.LogError("MobiledgeX: Make sure your credentials in MobiledgeX Settings are the same as on MobiledgeX Console.\n check the exceptions for more info.");
                throw new RegisterClientException("RegisterClient failed");
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
                    Debug.LogError("MobiledgeX: FindCloudlet call failed.\n Make sure to register client is called before findCloudlet");
                    throw new FindCloudletException("FindCloudlet call failed.");
                }

                switch (reply.status)
                {
                    case FindCloudletReply.FindStatus.FIND_UNKNOWN:
                        Debug.LogError("MobiledgeX: FindCloudlet status unknown. No edge cloudlets.");
                        throw new FindCloudletException("FindCloudlet Reply is Unkown.");

                    case FindCloudletReply.FindStatus.FIND_NOTFOUND:
                        Debug.LogError("MobiledgeX: FindCloudlet Found no edge cloudlets in range.\n make sure you deployed app instances for your application.");
                        throw new FindCloudletException("FindCloudlet Found no edge cloudlets in range..");

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
                    Dictionary<int, AppPort> appPortsDict = me.GetAppPortsByProtocol(reply, protocol);
                    if (appPortsDict.Keys.Count < 1)
                    {
                        Debug.LogError("MobiledgeX: Please make sure you the desired " + protocol + " Ports is defined in your Application Port Mapping Section on MobiledgeX Console.");
                        throw new GetConnectionException("No ports mapped to " + protocol + " protocol.");
                    }
                    if (port == 0)
                    {
                        port = appPortsDict.OrderBy(kvp => kvp.Key).First().Key;
                    }
                    try
                    {
                        host = appPortsDict[port].fqdn_prefix + reply.fqdn;
                        port = appPortsDict[port].public_port;
                        uri = host + ":" + port + appPortsDict[port].path_prefix;
                    }
                    catch (KeyNotFoundException)
                    {
                        Debug.LogError("MobiledgeX: Port supplied is not mapped to your Application, Make sure the desired port is defined in  your Application Port Mapping Section on MobiledgeX Console.");
                        throw new GetConnectionException(protocol + " " + port + " is not defined on your Application Port Mapping section.");
                    }
                }
            }
            return uri;
        }

        /// Verification of Location based on the device location and the cell tower location
        /// </summary>
        /// <returns></returns>
        public async Task<bool> VerifyLocation()
        {
            location = GetLocationFromDevice();
            UpdateCarrierName();

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

        /// <summary>
        /// Whether Edge is Enabled on the device or not, Edge requires connections to run over cellular interface
        /// </summary>
		/// <param name="proto">GetConnectionProtocol (TCP, UDP, HTTP, Websocket)</param>
        /// <returns> boolean value </returns>
        public  bool IsEdgeEnabled(GetConnectionProtocols proto)
        {
            if (me.useOnlyWifi)
            {
#if UNITY_EDITOR
                Debug.Log("MobiledgeX: useWifiOnly must be false in production. useWifiOnly can be used only for testing");
                return true;
#else
                Debug.Log("useOnlyWifi must be false to enable edge connection");
                return false;
#endif
            }

            if (proto == GetConnectionProtocols.TCP || proto == GetConnectionProtocols.UDP)
            {
                if (!me.netInterface.HasCellular())
                {
                    Debug.Log(proto + " connection requires a cellular interface to run connection over edge.");
                    return false;
                }
            }
            else
            {
                // Connections where we cannot bind to cellular interface default to wifi if wifi is up
                // We need to make sure wifi is off
                if (!me.netInterface.HasCellular() || me.netInterface.HasWifi())
                {
                    Debug.Log(proto + " connection requires the cellular interface to be up and the wifi interface to be off to run connection over edge.");
                    return false;
                }
            }
    
            string cellularIPAddress = me.netInterface.GetIPAddress(me.netInterface.GetNetworkInterfaceName().CELLULAR);
            if (cellularIPAddress == null)
            {
                Debug.Log("Unable to find ip address for local cellular interface.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Communication Protocols supported by MobiledgeX GetConnection
        /// </summary>
        public enum GetConnectionProtocols
        {
            TCP,
            UDP,
            HTTP,
            Websocket
        }

        /// <summary>
        /// Uses MobiledgeXSetting Scriptable object to load orgName, appName, appVers 
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
    }
}