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

using UnityEngine;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.WebSockets;
using System.Linq;
using DistributedMatchEngine; //MobiledgeX MatchingEngine
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
    public partial class MobiledgeXIntegration : IDisposable
    {
        public static string sdkVersion { get; set; }
        
        /// <summary>
        /// Scriptable Object Holding MobiledgeX Settings (OrgName, AppName, AppVers)
        /// </summary>
        public static MobiledgeXSettings settings = Resources.Load<MobiledgeXSettings>("MobiledgeXSettings");

        /// <summary>
        /// MatchingEngine objects
        /// </summary>
        PlatformIntegration pIntegration;
        public MatchingEngine matchingEngine;

        /// <summary>
        /// MatchingEngine API parameters
        /// </summary>
        public string carrierName { get; set; } = ""; // carrierName depends on the available subscriber SIM card and roaming carriers, and must be supplied by platform API.
        public string orgName { get; set; } = ""; // Organization name
        public string appName { get; set; } = ""; // Your appName, if you have created this in the MobiledgeX console.
        public string appVers { get; set; } = ""; // Your app version uploaded to the docker registry.
        public string developerAuthToken { get; set; } = ""; // This is an opaque string value supplied by the developer.
        public uint cellID { get; set; } = 0;
        public string uniqueIDType { get; set; } = "";
        public string uniqueID { get; set; } = "";
        public Loc location { get; set; } = new Loc();

        /// <summary>
        /// Public MatchingEngine Reply/ State properties
        /// </summary>
        public bool RegisterStatus { get { return latestRegisterStatus; } } // Whether the most recent registerClient call was successful
        public FindCloudletReply FindCloudletReply { get { return latestFindCloudletReply; } } // Stored to be used in GetUrl, GetHost, GetPort, Get[]Connection
        public bool VerifyLocationStatus { get { return latestVerifyLocationStatus; } } // Whether the most recent verifyLocation call was successful
        public FindCloudletMode Mode { get { return mode; } } // FindCloudlet mode
        public AppPort AppPort { get { return latestAppPort; } }
        public AppPort[] AppPortList { get { return latestAppPortList; } }

        /// <summary>
        /// MatchingEngine Reply/ State variables (for internal use)
        /// </summary>
        bool latestRegisterStatus = false; // Whether the most recent registerClient call was successful
        FindCloudletReply latestFindCloudletReply = null; // Stored to be used in GetUrl, GetHost, GetPort, Get[]Connection
        bool latestVerifyLocationStatus = false; // Whether the most recent verifyLocation call was successful
        FindCloudletMode mode = FindCloudletMode.PROXIMITY; // FindCloudlet mode
        AppPort latestAppPort = null;
        AppPort[] latestAppPortList = null;
        Location fallbackLocation = new Location(0,0);
        CarrierInfoClass carrierInfoClass = new CarrierInfoClass(); // used for IsRoaming check
        MelMessaging melMessaging;

        string region
        {
            get
            {
                switch (settings.region)
                {
                    case "EU":
                        return EU_DME;
                    case "JP":
                        return JP_DME;
                    case "US":
                        return US_DME;
                    case "Nearest":
                    default:
                        return WIFI_DME;
                }
            }
        }

        const string WIFI_DME = "wifi.dme.mobiledgex.net";
        const string EU_DME = "eu-mexdemo.dme.mobiledgex.net";
        const string US_DME = "us-mexdemo.dme.mobiledgex.net";
        const string JP_DME = "jp-mexdemo.dme.mobiledgex.net";

        /// <summary>
        /// Constructor for MobiledgeXIntegration. This class has functions that wrap DistributedMatchEngine functions for ease of use
        /// </summary>
        public MobiledgeXIntegration(CarrierInfo carrierInfo = null, NetInterface netInterface = null, UniqueID uniqueId = null, DeviceInfo deviceInfo = null)
        {
            ConfigureMobiledgeXSettings();
            // Set the platform specific way to get SIM carrier information.
            pIntegration = new PlatformIntegration();

            // Optionally override each interface:
            matchingEngine = new MatchingEngine(
              carrierInfo == null ? pIntegration.CarrierInfo : carrierInfo,
              netInterface == null ? pIntegration.NetInterface : netInterface,
              uniqueId == null ? pIntegration.UniqueID : uniqueId,
              deviceInfo == null ? pIntegration.DeviceInfo : deviceInfo);

            melMessaging = new MelMessaging(appName);
            matchingEngine.SetMelMessaging(melMessaging);
        }
        
        /// <summary>
        /// Constructor for MobiledgeXIntegration. This class has functions that wrap DistributedMatchEngine functions for ease of use
        /// </summary>
        public MobiledgeXIntegration(string orgName, string appName , string appVers , string developerAuthToken = "")
        {
            this.orgName = orgName;
            this.appVers = appVers;
            this.appName = appName;
            this.developerAuthToken = developerAuthToken;

            // Set the platform specific way to get SIM carrier information.
            pIntegration = new PlatformIntegration();

            matchingEngine = new MatchingEngine(pIntegration.CarrierInfo, pIntegration.NetInterface, pIntegration.UniqueID, pIntegration.DeviceInfo);

            melMessaging = new MelMessaging(appName);
            matchingEngine.SetMelMessaging(melMessaging);
        }

        /// <summary>
        /// Wrapper for RegisterAndFindCloudlet. Returns false if either Register or FindCloudlet fails.
        /// RegisterClientException and FindCloudletException will give more details on reason for failure
        /// </summary>
        /// <returns>bool Task</returns>
        public async Task<bool> RegisterAndFindCloudlet(string dmeHost = null, uint dmePort = 0)
        {
            bool registered = await Register(dmeHost, dmePort);
            if (!registered)
            {
                Debug.LogError("Register Failed!");
                return false;
            }
            Logger.Log("Register OK!");
            bool found = await FindCloudlet(dmeHost, dmePort);
            if (!found)
            {
                Debug.LogError("FindCloudlet Failed!");
                return false;
            }
            return true;
        }

        /// <summary>
        /// Wrapper for VerifyLocation. Verification of location based on the device location and the cell tower location
        /// </summary>
        /// <returns>bool Task</returns>
        public async Task<bool> VerifyLocation(string dmeHost = null, uint dmePort = 0)
        {
            latestVerifyLocationStatus = false;

            if (!latestRegisterStatus)
            {
                Debug.LogError("MobiledgeX: Last RegisterClient was unsuccessful. Call RegisterClient again before VerifyLocation");
                return false;
            }

            await UpdateLocationAndCarrierInfo();

            VerifyLocationRequest req = matchingEngine.CreateVerifyLocationRequest(location, carrierName);
            VerifyLocationReply reply;
            if (dmeHost == null || dmePort == 0)
            {
                reply = await matchingEngine.VerifyLocation(req);
            }
            else
            {
                reply = await matchingEngine.VerifyLocation(dmeHost, dmePort, req);
            }

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
            if (reply.gps_location_accuracy_km < 0)
            {
                return false;
            }

            // A per app policy decision might be 0.5 km, or 25km, or 100km:
            if (reply.gps_location_accuracy_km < 100)
            {
                return true;
            }
            // Too far for this app.
            return false;
        }

        /// <summary>
        /// Wrapper for Get[]AppPorts. This will return the AppPort object mapped to the given port.
        /// If no port is given, this will return the first AppPort in the dictionary
        /// </summary>
        /// <param name="proto">LProto protocol (L_PROTO_TCP, L_PROTO_UDP, or L_PROTO_HTTP)</param>
        /// <param name="port">port for developer specific backend service</param>
        /// <returns>AppPort</returns>
        public AppPort GetAppPort(LProto proto, int port = 0)
        {
            if (latestFindCloudletReply == null)
            {
                Debug.LogError("MobiledgeX: Last FindCloudlet returned null. Call FindCloudlet again before GetAppPort");
                throw new AppPortException("Last FindCloudlet returned null. Call FindCloudlet again before GetAppPort");
            }

            Dictionary<int, AppPort> appPortsDict = new Dictionary<int, AppPort>();

            switch (proto)
            {
                case LProto.L_PROTO_TCP:
                    appPortsDict = matchingEngine.GetTCPAppPorts(latestFindCloudletReply);
                    break;
                case LProto.L_PROTO_UDP:
                    appPortsDict = matchingEngine.GetUDPAppPorts(latestFindCloudletReply);
                    break;
                default:
                    throw new AppPortException(proto + " is not supported");
            }

            if (appPortsDict.Keys.Count < 1)
            {
                Debug.LogError("MobiledgeX: Please make sure you defined the desired Ports in your Application Port Mapping Section on MobiledgeX Console.");
                throw new AppPortException("No AppPorts available for your Application");
            }

            if (port == 0)
            {
                Logger.Log("No port specified. Grabbing first AppPort in dictionary");
                port = appPortsDict.OrderBy(kvp => kvp.Key).First().Key;
            }

            try
            {
                AppPort appPort = appPortsDict[port];
                latestAppPort = appPort;
                return appPort;
            }
            catch (KeyNotFoundException)
            {
                Debug.LogError("MobiledgeX: Port supplied is not mapped for your Application, Make sure the desired port are defined in your Application Port Mapping Section on MobiledgeX Console.");
                throw new AppPortException(proto + " " + port + " is not defined for your Application");
            }
        }

        /// <summary>
        /// Wrapper for CreateUrl. Returns the L7 url for application backend
        /// </summary>
        /// <param name="appPort">AppPort (from GetAppPort)</param>
        /// <param name="l7Proto">Layer 7 communication protocol (eg. http, https, ws, wss)</param>
        /// <param name="port">port for developer specific backend service</param>
        /// <param name="path">optional path to append to end of url</param>
        /// <returns>string</returns>
        public string GetUrl(string l7Proto, AppPort appPort = null, int port = 0, string path = "")
        {
            if (latestFindCloudletReply == null)
            {
                throw new GetConnectionException("Last FindCloudlet returned null. Call FindCloudlet again before GetAppPort");
            }

            if (appPort == null)
            {
                if (latestAppPort == null)
                {
                    Debug.LogError("MobiledgeX: Unable to find AppPort. Supply an AppPort or call GetAppPort before calling GetUrl");
                    throw new GetConnectionException("Unable to find AppPort. Supply an AppPort or call GetAppPort before calling GetUrl");
                }
                appPort = latestAppPort;
            }

            return matchingEngine.CreateUrl(latestFindCloudletReply, appPort, l7Proto, port, path);
        }

        /// <summary>
        /// Wrapper for GetPort. Returns the port of specified service in the application backend (use with GetHost)
        /// </summary>
        /// <param name="appPort">AppPort (from GetAppPort)</param>
        /// <param name="port">port for developer specific backend service</param>
        /// <returns>string</returns>
        public string GetHost(AppPort appPort = null)
        {
            if (latestFindCloudletReply == null)
            {
                throw new GetConnectionException("Last FindCloudlet returned null. Call FindCloudlet again before GetAppPort");
            }

            if (appPort == null)
            {
                if (latestAppPort == null)
                {
                    Debug.LogError("MobiledgeX: Unable to find AppPort. Call GetAppPort before calling GetHost");
                    throw new GetConnectionException("Unable to find AppPort. Call GetAppPort before calling GetHost");
                }
                appPort = latestAppPort;
            }

            return matchingEngine.GetHost(latestFindCloudletReply, appPort);
        }

        /// <summary>
        /// Wrapper for GetHost. Returns the host of the application backend (use with GetPort)
        /// </summary>
        /// <param name="appPort">AppPort (from GetAppPort)</param>
        /// <returns>int</returns>
        public int GetPort(AppPort appPort = null, int port = 0)
        {
            if (appPort == null)
            {
                if (latestAppPort == null)
                {
                    Debug.LogError("MobiledgeX: Unable to find AppPort. Call GetAppPort before calling GetPort");
                    throw new GetConnectionException("Unable to find AppPort. Call GetAppPort before calling GetPort");
                }
                appPort = latestAppPort;
            }

            return matchingEngine.GetPort(appPort, port);
        }

        /// <summary>
        /// Wrapper for GetWebsocketConnection
        /// </summary>
        /// <param name="path">string path for ex. roomId  </param>
        /// <param name="port">Integer TCP port </param>
        /// <returns>ClientWebSocket Task</returns>
        public async Task<ClientWebSocket> GetWebsocketConnection(AppPort appPort = null, int port = 0, string path = "")
        {
            if (!IsEdgeEnabled(GetConnectionProtocol.Websocket))
            {
                throw new GetConnectionException("Device is not edge enabled. Please switch to cellular connection or use server in public cloud");
            }

            if (latestFindCloudletReply == null)
            {
                Debug.LogError("MobiledgeX: Last FindCloudlet returned null. Call FindCloudlet again before GetAppPort");
                throw new GetConnectionException("Last RegisterClient was unsuccessful. Call RegisterClient again before FindCloudlet");
            }

            if (appPort == null)
            {
                if (latestAppPort == null)
                {
                    Debug.LogError("MobiledgeX: Unable to find AppPort. Call GetAppPort before calling GetWebsocketConnection");
                    throw new GetConnectionException("Unable to find AppPort. Call GetAppPort before calling GetWebsocketConnection");
                }
                appPort = latestAppPort;
            }

            return await matchingEngine.GetWebsocketConnection(latestFindCloudletReply, appPort, port, 5000, path);
        }

        public void Dispose() {
            if (matchingEngine != null)
            {
                matchingEngine.Dispose();
                matchingEngine = null;
            }
        }
    }
}
