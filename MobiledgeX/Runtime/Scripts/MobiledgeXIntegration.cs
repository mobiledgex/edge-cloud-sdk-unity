    /**
     * Copyright 2019 MobiledgeX, Inc. All rights and licenses reserved.
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

     namespace MobiledgeX{

   
     /// <summary>
     /// MobiledgeX Integration is am implementation of MobiledgeX MatchingEngine SDK
     /// </summary>
        public class MobiledgeXIntegration
    {
        /// <summary>
        /// Scriptable Object Holding MobiledgeX Settings (OrgName, AppName, AppVers)
        /// </summary>
        static MobiledgeXSettings settings = Resources.Load<MobiledgeXSettings>("MobiledgeXSettings");
        /// <summary>
        ///Additional application side to retrieve carrier information so that the SDK can provide
        ///Edge Cloudlet discovery.
        /// </summary>
        static PlatformIntegration pIntegration = new PlatformIntegration();
      /// <summary>
      /// Matching Engine SDK Instance
      /// </summary>
      public static MatchingEngine me = new MatchingEngine(pIntegration.CarrierInfo,pIntegration.NetInterface,pIntegration.UniqueID);
      /// <summary>
      /// Helper Performance Metric
      /// </summary>
      public static NetTest netTest= new NetTest(me);
      /// <summary>
      /// Orginization Name, The organization name stated on the MobiledgeX Console
      /// </summary>
      public static string orgName { get; set; }
      /// <summary>
      /// Application Name registered in your account on the MobiledgeX Console
      /// </summary>
      public static string appName { get; set; }
      /// <summary>
      /// Application version registered in your account on the MobiledgedX Console
      /// </summary>
      public static string appVers { get; set; }
      /// <summary>
      /// Set Authentication Token 
      /// </summary>
      public static string developerAuthToken { get; set; }
      /// <summary>
      /// // carrierName depends on the available subscriber SIM card and roaming carriers, and must be supplied by platform API.
      /// </summary>
      public static string carrierName { get; set; } = MatchingEngine.wifiCarrier;
      /// <summary>
      /// GSM Cell ID is a generally unique number used to identify each base transceiver station
      /// </summary>
      public static uint cellID { get; set; } = 0;
      public static string uniqueIDType { get; set; } = "";
      public static string uniqueID { get; set; } = "";
      public static Tag[] tags { get; set; } = new Tag[0];
      /// <summary>
      /// Constructor for MobiledgeX SDK
      /// </summary>
      public MobiledgeXIntegration()
      {
        // PlatformIntegration sets the platform specific way to get SIM carrier information, for Unity editor mode it will use Wifi
        pIntegration = new PlatformIntegration();

        // Platform integration needs to initialize first
        me = new MatchingEngine(pIntegration.CarrierInfo, pIntegration.NetInterface, pIntegration.UniqueID);

        // Optional NetTesting.
        netTest = new NetTest(me);
      }
      /// <summary>
      /// use wifi only option, might  be helpful in Editor Mode
      /// </summary>
      /// <param name="useWifi"></param>
      public static  void useWifiOnly(bool useWifi)
      {
        me.useOnlyWifi = useWifi;
      }
      /// <summary>
      /// GetCarrierName returns MccMnc code(string), MccMnc is a combination of MCC (Mobile Country Code) and MNC (Mobile Network Code)
      /// <para> <see cref="https://en.wikipedia.org/wiki/Mobile_country_code"/> </para>
      /// </summary>
      /// <returns> MccMnc code(string), MccMnc is a combination of MCC (Mobile Country Code) and MNC (Mobile Network Code)</returns>
      public static string GetCarrierName()
      {
        return me.carrierInfo.GetMccMnc();
      }
       /// <summary>
       /// GetLocationFromDevice , Uses Unity Location Services, Location is needed for Edge Cloudlet discovery.
       /// <para>If location is not supplied(ex. Editor mode) San Jose LatLon is supplied </para>
       /// <para>Remember to Supply Location Permission in the Player Settings</para> 
       /// </summary>
       /// <returns>Location Object(</returns>
       public static async Task<Loc> GetLocationFromDevice()
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
      /// <summary>
      /// Register client using (Org Name, App Name, App Vers, Optional args) to initiate the connection to Edge
      /// </summary>
      /// <returns>Register Client reply</returns>
      // These are just thin wrappers over the SDK to show how to use them:
      // Call once, or when the carrier changes. May throw DistributedMatchEngine.HttpException.
      public static async Task<bool> Register()
       {

        ConfigureMobiledgeXSettings();
        // If MEX is reachable on your SIM card
        string aCarrierName = GetCarrierName();
        string eCarrierName;
        if (me.useOnlyWifi)
        {
          eCarrierName = carrierName;
        }
        else
        {
          if (aCarrierName == null)
          {
            Debug.Log("Missing CarrierName for RegisterClient.");
            return false;
          }
          eCarrierName = aCarrierName;
        }

        RegisterClientRequest req = me.CreateRegisterClientRequest(eCarrierName, orgName, appName, appVers, developerAuthToken, cellID, uniqueIDType, uniqueID, tags);
                Debug.Log("CarrierName: " + req.carrier_name);
                Debug.Log("orgName: " + req.org_name);
                Debug.Log("AppName: " + req.app_name);
                Debug.Log("AppVers: " + req.app_vers);
                RegisterClientReply reply = await me.RegisterClient(req);
        return (reply.status == ReplyStatus.RS_SUCCESS);
      }
       /// <summary>
       /// Calls the api to find best cloudlet to connect to .
       /// <para> Cloudlet is a mobility-enhanced small-scale cloud datacenter </para>
       /// </summary>
       /// <returns></returns>
       public static async Task<FindCloudletReply> FindCloudlet()
      {
        ConfigureMobiledgeXSettings();
        // Location is ephemeral, so retrieve a new location from the platform. May return 0,0 which is
        // technically valid, though less likely real, as of writing.
        Loc loc = await GetLocationFromDevice();
        // If MEX is reachable on your SIM card.
        string aCarrierName = GetCarrierName();
        string eCarrierName;
        if (me.useOnlyWifi) // There's no host (PC, UnityEditor, etc.)
        {
          eCarrierName = carrierName;
        }
        else
        {
          if (aCarrierName == "" || aCarrierName == null)
          {
            Debug.Log("Missing CarrierName for FindCloudlet.");
            return null;
          }
          eCarrierName = aCarrierName;
        }
            FindCloudletRequest req = me.CreateFindCloudletRequest(eCarrierName, loc, orgName, appName, appVers, cellID, tags);

            FindCloudletReply reply = await me.FindCloudlet(req);
        return reply;
      }
      /// <summary>
      /// Currently Implemented for Devices with Deutsche Telekom Carrier Only
      /// <para>
      /// verifies the gps location against where in the cellular network the device is.
      /// </para>
      /// </summary>
      /// <returns></returns>
      public static async Task<bool> VerifyLocation()
      {
        ConfigureMobiledgeXSettings();
        Loc loc = await GetLocationFromDevice();
        // If MEX is reachable on your SIM card:
        string aCarrierName = GetCarrierName();
        string eCarrierName;
        if (me.useOnlyWifi) // There's no host (PC, UnityEditor, etc.)...
        {
          eCarrierName = carrierName;
        }
        else
        {
          eCarrierName = aCarrierName;
        }
        VerifyLocationRequest req = me.CreateVerifyLocationRequest(eCarrierName, loc, cellID, tags);
        VerifyLocationReply reply = await me.VerifyLocation(req);
        // The return is not binary, but one can decide the particular app's policy
        // on pass or failing the location check. Not being verified or the country
        // not matching at all is on such policy decision:
        // GPS and Tower Status:
        switch (reply.gps_location_status) {
          case VerifyLocationReply.GPSLocationStatus.LOC_ROAMING_COUNTRY_MISMATCH:
          case VerifyLocationReply.GPSLocationStatus.LOC_ERROR_UNAUTHORIZED:
          case VerifyLocationReply.GPSLocationStatus.LOC_ERROR_OTHER:
          case VerifyLocationReply.GPSLocationStatus.LOC_UNKNOWN:
            return false;
        }
        switch (reply.tower_status) {
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
      /// Gets the WebsocketConnection to the websocket server you uploaded to MobiledgeX 
      /// </summary>
      /// <param name="path"> the query parameters of the connection
      /// ex : ?roomid=test&amp;playerCharacter=2
      /// </param>
      /// <returns></returns>
      // Typical developer workflow to get connection to application backend
      public static async Task<ClientWebSocket> GetWebsocketConnection(string path)
      {
        Loc loc = await GetLocationFromDevice();
        string aCarrierName = GetCarrierName();
        string eCarrierName;
        if (me.useOnlyWifi)
        {
          eCarrierName = carrierName;
        }
        else
        {
          if (aCarrierName == null)
          {
            Debug.Log("Missing CarrierName for RegisterClient.");
            return null;
          }
          eCarrierName = aCarrierName;
        }
        FindCloudletReply findCloudletReply = await me.RegisterAndFindCloudlet(eCarrierName, orgName, appName, appVers, developerAuthToken, loc, cellID, uniqueIDType, uniqueID, tags);
        if (findCloudletReply == null)
        {
          Debug.Log("cannot find findCloudletReply");
        }
        Dictionary<int, AppPort> appPortsDict = me.GetTCPAppPorts(findCloudletReply);
        int public_port = findCloudletReply.ports[0].public_port; // We happen to know it's the first one.
        AppPort appPort = appPortsDict[public_port];
        return await me.GetWebsocketConnection(findCloudletReply, appPort, public_port, 5000, path);
      }
      /// <summary>
      /// Use MobiledgeXSetting Scriptable object to load orgName, appName, appVers 
      /// </summary>
      public static void ConfigureMobiledgeXSettings()
      {
            orgName = settings.orgName;
            appName = settings.appName;
            appVers = settings.appVers;
      }
      /// <summary>
      /// Gets the URI for your backend you uploaded to MobiledgeX 
      /// </summary>
      /// <returns>(string) uri </returns>
      public static async Task<string> GetRestURI()
        {
           ConfigureMobiledgeXSettings();
           Loc loc = await GetLocationFromDevice();
           string aCarrierName = GetCarrierName();
           string eCarrierName;
           if (me.useOnlyWifi)
           {
               eCarrierName = carrierName;
           }
           else
           {
               if (aCarrierName == null)
               {
                   Debug.Log("Missing CarrierName for RegisterClient.");
                   return null;
               }
               eCarrierName = aCarrierName;
           }
           FindCloudletReply findCloudletReply = await me.RegisterAndFindCloudlet(eCarrierName, orgName, appName, appVers, developerAuthToken, loc, cellID, uniqueIDType, uniqueID, tags);
           if (findCloudletReply == null)
           {
               Debug.Log("cannot find findCloudletReply");
           }
           Dictionary<int, AppPort> appPortsDict = me.GetTCPAppPorts(findCloudletReply);
           int public_port = findCloudletReply.ports[0].public_port; // We happen to know it's the first one.
           Debug.Log(findCloudletReply.fqdn + ":" + public_port);
           return findCloudletReply.fqdn + ":" + public_port;
            }
        }
    }
