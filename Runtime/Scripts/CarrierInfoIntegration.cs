/**
 * Copyright 2019-2021 MobiledgeX, Inc. All rights and licenses reserved.
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
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Android;
using DistributedMatchEngine;
using System.Runtime.InteropServices; //for importing IOS functionS

namespace MobiledgeX
{
  public class CarrierInfoException : Exception
  {
    public CarrierInfoException(string message)
    : base(message)
    {
    }

    public CarrierInfoException(string message, Exception innerException)
    : base(message, innerException)
    {
    }
  }

  public class CarrierInfoClass : CarrierInfo
  {
#pragma warning disable 0649
#if UNITY_ANDROID // PC android target builds to through here as well.

    int sdkVersion;

    AndroidJavaObject cellInfoLte;
    AndroidJavaObject cellInfoGsm;
    AndroidJavaObject cellInfoWcdma;
    AndroidJavaObject cellInfoCdma;
    AndroidJavaObject cellInfoTdscdma;
    AndroidJavaObject cellInfoNr;

    string cellInfoLteString;
    string cellInfoGsmString;
    string cellInfoWcdmaString;
    string cellInfoCdmaString;
    string cellInfoTdscdmaString;
    string cellInfoNrString;

    public CarrierInfoClass()
    {
      sdkVersion = getAndroidSDKVers();
      if (sdkVersion < 0)
      {
        Logger.Log("Could not get valid sdkVersion: " + sdkVersion);
        return;
      }

      /*
       * The following code is commented out to prevent Android JNI blacklist crashes.
       * As of Android API 28, CellInfo interfaces and class reflection through JNI are not allowed.
       * (https://developer.android.com/distribute/best-practices/develop/restrictions-non-sdk-interfaces)
       * The following code can be used with older Android API versions.
       */

      /*if (sdkVersion >= 17) {
        cellInfoLte = PlatformIntegrationUtil.GetAndroidJavaObject("android.telephony.CellInfoLte");
        cellInfoLteString = cellInfoLte != null ? PlatformIntegrationUtil.GetSimpleName(cellInfoLte) : "";

        cellInfoGsm = PlatformIntegrationUtil.GetAndroidJavaObject("android.telephony.CellInfoGsm");
        cellInfoGsmString = cellInfoGsm != null ? PlatformIntegrationUtil.GetSimpleName(cellInfoGsm) : "";

        cellInfoCdma = PlatformIntegrationUtil.GetAndroidJavaObject("android.telephony.CellInfoCdma");
        cellInfoCdmaString = cellInfoCdma != null ? PlatformIntegrationUtil.GetSimpleName(cellInfoCdma) : "";
      }

      if (sdkVersion >= 18) {
        cellInfoWcdma = PlatformIntegrationUtil.GetAndroidJavaObject("android.telephony.CellInfoWcdma");
        cellInfoWcdmaString = cellInfoWcdma != null ? PlatformIntegrationUtil.GetSimpleName(cellInfoWcdma) : "";
      }
      
      if (sdkVersion >= 28)
      {
        cellInfoTdscdma = PlatformIntegrationUtil.GetAndroidJavaObject("android.telephony.CellInfoTdscdma");
        cellInfoTdscdmaString = cellInfoTdscdma != null ? PlatformIntegrationUtil.GetSimpleName(cellInfoTdscdma) : "";
      }
      if (sdkVersion >= 29)
      {
        cellInfoNr = PlatformIntegrationUtil.GetAndroidJavaObject("android.telephony.CellInfoNr");
        cellInfoNrString = cellInfoNr != null ? PlatformIntegrationUtil.GetSimpleName(cellInfoNr) : "";
      }*/
    }

    public int getAndroidSDKVers()
    {
      AndroidJavaClass version = PlatformIntegrationUtil.GetAndroidJavaClass("android.os.Build$VERSION");
      if (version == null)
      {
        Logger.Log("Unable to get Build Version");
        return 0;
      }
      return PlatformIntegrationUtil.GetStatic<int>(version, "SDK_INT");
    }

    public AndroidJavaObject GetTelephonyManager()
    {
      AndroidJavaClass unityPlayer = PlatformIntegrationUtil.GetAndroidJavaClass("com.unity3d.player.UnityPlayer");
      if (unityPlayer == null)
      {
        Logger.Log("Unable to get UnityPlayer");
        return null;
      }
      AndroidJavaObject activity = PlatformIntegrationUtil.GetStatic<AndroidJavaObject>(unityPlayer, "currentActivity");
      if (activity == null)
      {
        Logger.Log("Can't find an activity!");
        return null;
      }

      AndroidJavaObject context = PlatformIntegrationUtil.Call<AndroidJavaObject>(activity, "getApplicationContext");
      if (context == null)
      {
        Logger.Log("Can't find an app context!");
        return null;
      }

      // Context.TELEPHONY_SERVICE:
      string CONTEXT_TELEPHONY_SERVICE = context.GetStatic<string>("TELEPHONY_SERVICE");
      if (CONTEXT_TELEPHONY_SERVICE == null)
      {
        Logger.Log("Can't get Context Telephony Service");
        return null;
      }

      AndroidJavaObject telManager = PlatformIntegrationUtil.Call<AndroidJavaObject>(context, "getSystemService", new object[] {CONTEXT_TELEPHONY_SERVICE});

      sdkVersion = getAndroidSDKVers();

      if (sdkVersion < 24)
      {
        return telManager;
      }

      // Call SubscriptionManager to get a specific telManager:
      AndroidJavaClass subscriptionManagerCls = PlatformIntegrationUtil.GetAndroidJavaClass("android.telephony.SubscriptionManager");
      if (subscriptionManagerCls == null) {
        Logger.Log("Can't get Subscription Manager Class.");
        return null;
      }
      int subId = PlatformIntegrationUtil.CallStatic<int>(subscriptionManagerCls, "getDefaultDataSubscriptionId");
      int invalidSubId = PlatformIntegrationUtil.GetStatic<int>(subscriptionManagerCls, "INVALID_SUBSCRIPTION_ID");
      if (subId == invalidSubId) {
        Logger.Log("The Subscription ID is invalid: " + subId);
        return null;
      }
      object[] idParam = new object[1] { subId };
      telManager = PlatformIntegrationUtil.Call<AndroidJavaObject>(telManager, "createForSubscriptionId", idParam);

      return telManager;
    }

    public string GetCurrentCarrierName()
    {
      string networkOperatorName = "";
      Logger.Log("Device platform: " + Application.platform);
      if (Application.platform != RuntimePlatform.Android)
      {
        Logger.Log("Not on android device.");
        return "";
      }

      AndroidJavaObject telManager = GetTelephonyManager();
      if (telManager == null)
      {
        Logger.Log("Can't get telephony manager!");
        return "";
      }

      networkOperatorName = PlatformIntegrationUtil.Call<string>(telManager, "getNetworkOperatorName");
      if (networkOperatorName == null)
      {
        Logger.Log("Network Operator Name is not found on the device");
        networkOperatorName = "";
      }

      return networkOperatorName;
    }

    public string GetMccMnc()
    {
      string mccmnc = null;
      if (Application.platform != RuntimePlatform.Android)
      {
        Logger.Log("Not on android device.");
        return null;
      }

      AndroidJavaObject telManager = GetTelephonyManager();
      if (telManager == null)
      {
        Logger.Log("Can't get telephony manager!");
        return null;
      }

      mccmnc = PlatformIntegrationUtil.Call<string>(telManager, "getNetworkOperator");
      if (mccmnc == null || mccmnc == "")
      {
        return null;
      }

      if (mccmnc.Length < 5)
      {
        return null;
      }

      return mccmnc;
    }

    KeyValuePair<string, ulong> GetCidKeyValuePair(AndroidJavaObject cellInfo)
    {
      KeyValuePair<string, ulong> pair = new KeyValuePair<string, ulong>(null, 0);

      string simpleName = PlatformIntegrationUtil.GetSimpleName(cellInfo);
      AndroidJavaObject cellIdentity = PlatformIntegrationUtil.Call<AndroidJavaObject>(cellInfo, "getCellIdentity");
      if (cellIdentity == null)
      {
        Logger.Log("Unable to get cellIdentity");
        return pair;
      }

      if (simpleName.Equals(cellInfoTdscdmaString))
      {
        int cid = PlatformIntegrationUtil.Call<int>(cellIdentity, "getCid");
        if (cid > 0)
        {
          pair = new KeyValuePair<string, ulong>(simpleName, (ulong)cid);
        }
      }
      else if (simpleName.Equals(cellInfoNrString))
      {
        long nci = PlatformIntegrationUtil.Call<long>(cellIdentity, "getNci");
        if (nci > 0)
        {
          pair = new KeyValuePair<string, ulong>(simpleName, (ulong)nci);
        }
      }
      else if (simpleName.Equals(cellInfoLteString))
      {
        int ci = PlatformIntegrationUtil.Call<int>(cellIdentity, "getCi");
        if (ci > 0)
        {
          pair = new KeyValuePair<string, ulong>(simpleName, (ulong)ci);
        }
      }
      else if (simpleName.Equals(cellInfoGsmString))
      {
        int cid = PlatformIntegrationUtil.Call<int>(cellIdentity, "getCid");
        if (cid > 0)
        {
          pair = new KeyValuePair<string, ulong>(simpleName, (ulong)cid);
        }
      }
      else if (simpleName.Equals(cellInfoWcdmaString))
      {
        int cid = PlatformIntegrationUtil.Call<int>(cellIdentity, "getCid");
        if (cid > 0)
        { 
          pair = new KeyValuePair<string, ulong>(simpleName, (ulong)cid);
        }
      }
      else if (simpleName.Equals(cellInfoCdmaString))
      {
        int baseStationId = PlatformIntegrationUtil.Call<int>(cellIdentity, "getBaseStationId");
        if (baseStationId > 0)
        { 
          pair = new KeyValuePair<string, ulong>(simpleName, (ulong)baseStationId);
        }
      }
      else
      {
        Logger.Log("Object is not an instance of a CellInfo class");
      }

      return pair;

    }

    public List<KeyValuePair<String, ulong>> GetCellInfoList()
    {
      if (Application.platform != RuntimePlatform.Android)
      {
        Logger.Log("Not on android device.");
        return null;
      }

      AndroidJavaObject telManager = GetTelephonyManager();
      if (telManager == null)
      {
        Logger.Log("Can't get telephony manager!");
        return null;
      }

      if (!Permission.HasUserAuthorizedPermission(Permission.FineLocation))
      {
        Permission.RequestUserPermission(Permission.FineLocation);
      }

      AndroidJavaObject cellInfoList = PlatformIntegrationUtil.Call<AndroidJavaObject>(telManager, "getAllCellInfo");
      if (cellInfoList == null)
      {
        Logger.Log("Can't get list of cellInfo objects.");
        return null;
      }

      int length = PlatformIntegrationUtil.Call<int>(cellInfoList, "size");
      if (length <= 0)
      {
        Logger.Log("Unable to get valid length for cellInfoList");
        return null;
      }

      List<KeyValuePair<String, ulong>> cellIDList = new List<KeyValuePair<string, ulong>>();
      // KeyValuePair to compare to in case GetCidKeyValuePair returns nothing
      KeyValuePair<string,ulong> empty = new KeyValuePair<string, ulong>(null, 0);

      for (int i = 0; i < length; i++)
      {
        AndroidJavaObject cellInfo = PlatformIntegrationUtil.Call<AndroidJavaObject>(cellInfoList, "get", new object[] {i});
        if (cellInfo == null) continue;
        
        bool isRegistered = PlatformIntegrationUtil.Call<bool>(cellInfo, "isRegistered");
        if (isRegistered)
        {
          KeyValuePair<string, ulong> pair = GetCidKeyValuePair(cellInfo);
          if (!pair.Equals(empty))
          {
            cellIDList.Add(pair);
          }
        }
      }

      return cellIDList;
    }

    public ulong GetCellID()
    {
      /*
       * The following code is commented out to prevent Android JNI blacklist crashes.
       * As of Android API 28, CellInfo interfaces and class reflection through JNI are not allowed.
       * (https://developer.android.com/distribute/best-practices/develop/restrictions-non-sdk-interfaces)
       * The following code can be used with older Android API versions.
       */
       
      /*ulong cellID = 0;

      List<KeyValuePair<String, ulong>> cellInfoList = GetCellInfoList();

      if (cellInfoList == null || cellInfoList.Count == 0)
      {
        Logger.Log("no cellID");
        return cellID;
      }

      KeyValuePair<String, ulong> pair = cellInfoList[0]; // grab first value

      return pair.Value;*/
      return 0;
    }

#elif UNITY_IOS

    // Sets iOS platform specific internal callbacks (reference counted objects), etc.
    [DllImport("__Internal")]
    private static extern string _ensureMatchingEnginePlatformIntegration();

    [DllImport("__Internal")]
    private static extern string _getCurrentCarrierName();

    [DllImport("__Internal")]
    private static extern string _getMccMnc();

    [DllImport("__Internal")]
    private static extern int _getCellID();

    [DllImport("__Internal")]
    private static extern string _getISOCountryCodeFromGPS();

    [DllImport("__Internal")]
    private static extern void _convertGPSToISOCountryCode(double longitude, double latitude);

    [DllImport("__Internal")]
    private static extern string _getISOCountryCodeFromCarrier();

    public string GetCurrentCarrierName()
    {
      string networkOperatorName = "";
      if (Application.platform == RuntimePlatform.IPhonePlayer)
      {
        networkOperatorName = _getCurrentCarrierName();
      }
      return networkOperatorName;
    }

    public string GetMccMnc()
    {
      string mccmnc = null;
      if (Application.platform == RuntimePlatform.IPhonePlayer)
      {
        mccmnc = _getMccMnc();
      }
      return mccmnc;
    }

    public ulong GetCellID()
    {
      int cellID = 0;
      if (Application.platform == RuntimePlatform.IPhonePlayer)
      {
        cellID = _getCellID();
      }
      return (ulong)cellID;
    }

    public async Task<bool> IsRoaming(double longitude, double latitude)
    {
      if (Application.platform == RuntimePlatform.IPhonePlayer)
      {  
        Task<string> task = ConvertGPSToISOCountryCode(longitude, latitude);
        string isoCCFromGPS = null;
        if (await Task.WhenAny(task, Task.Delay(5000)) == task)
        {
          isoCCFromGPS = await task; 
        }
        else
        {
          // Timeout
          throw new CarrierInfoException("Timeout: unable to get ISO country code from gps");
        }
        if (isoCCFromGPS == null)
        {
          Debug.LogError("Unable to get ISO country code from gps");
          throw new CarrierInfoException("Unable to get ISO country code from gps");
        }
        Logger.Log("ISO country code from gps location is " + isoCCFromGPS);

        string isoCCFromCarrier = GetISOCountryCodeFromCarrier();
        if (isoCCFromCarrier == null)
        {
          Debug.LogError("Unable to get ISO country code from carrier");
          throw new CarrierInfoException("Unable to get ISO country code from carrier");
        }
        Logger.Log("ISO country code from carrier is " + isoCCFromCarrier);

        return isoCCFromGPS != isoCCFromCarrier;
      }

      // If in UnityEditor, return not roaming
      return false;
    }

    public async Task<string> ConvertGPSToISOCountryCode(double longitude, double latitude)
    {
      if (Application.platform == RuntimePlatform.IPhonePlayer)
      {
        _convertGPSToISOCountryCode(longitude, latitude);
        return await Task.Run(() => {
          string isoCC = "";
          while(isoCC == "" || isoCC == null)
          {
            isoCC = GetISOCountryCodeFromGPS();
          }
          return isoCC;
         }).ConfigureAwait(false);
      }

      return null;
    }  

    public string GetISOCountryCodeFromGPS()
    {
      string isoCC = null;
      if (Application.platform == RuntimePlatform.IPhonePlayer)
      {
        isoCC = _getISOCountryCodeFromGPS();
      }
      return isoCC;
    }

    public string GetISOCountryCodeFromCarrier()
    {
      string isoCC = null;
      if (Application.platform == RuntimePlatform.IPhonePlayer)
      {
        isoCC = _getISOCountryCodeFromCarrier();
      }
      return isoCC;
    }

#else

    // Implement CarrierInfo
    public string GetCurrentCarrierName()
    {
      Logger.Log("GetCurrentCarrierName is NOT IMPLEMENTED");
      return null;
    }

    public string GetMccMnc()
    {
      Logger.Log("GetMccMnc is NOT IMPLEMENTED");
      return null;
    }

    public ulong GetCellID()
    {
      Logger.Log("GetCellID is NOT IMPLEMENTED");
      return 0;
    }

#endif
  }

  // Used for testing in UnityEditor (any target platform)
  public class TestCarrierInfoClass : CarrierInfo
  {
    // Implement CarrierInfo
    public string GetCurrentCarrierName()
    {
      return "";
    }

    public string GetMccMnc()
    {
      return "";
    }

    public ulong GetCellID()
    {
      return 0;
    }
  }
}
