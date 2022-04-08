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
  /// <summary>
  /// CarrierInfoException is thrown if there is an error in Roaming Detection.
  /// </summary>
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
  /// <summary>
  /// CarrierInfoClass is responsible for collecting data about the device and the telecom carrier.
  /// </summary>
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

    // Source: https://developer.android.com/reference/android/telephony/TelephonyManager
    enum NetworkDataType
    {
      NETWORK_TYPE_1xRTT = 7,
      NETWORK_TYPE_CDMA = 4,
      NETWORK_TYPE_EDGE = 2,
      NETWORK_TYPE_EHRPD = 14,
      NETWORK_TYPE_EVDO_0 = 5,
      NETWORK_TYPE_EVDO_A = 6,
      NETWORK_TYPE_EVDO_B = 12,
      NETWORK_TYPE_GPRS = 1,
      NETWORK_TYPE_GSM = 16,
      NETWORK_TYPE_HSDPA = 8,
      NETWORK_TYPE_HSPA = 10,
      NETWORK_TYPE_HSPAP = 15,
      NETWORK_TYPE_HSUPA = 9,
      NETWORK_TYPE_IDEN = 11,
      NETWORK_TYPE_IWLAN = 18,
      NETWORK_TYPE_LTE = 13,
      NETWORK_TYPE_NR = 20,
      NETWORK_TYPE_TD_SCDMA = 17,
      NETWORK_TYPE_UMTS = 3,
      NETWORK_TYPE_UNKNOWN = 0
    };

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

    /// <summary>
	  /// Gets the Android SDK Version (android.os.Build$VERSION)
	  /// </summary>
	  /// <returns>Android SDK Version (integer)</returns>
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

    /// <summary>
	  /// Obtains the TelephonyManager object from the device(Android Only).
	  /// https://developer.android.com/reference/android/telephony/TelephonyManager
	  /// </summary>
	  /// <returns> Telephony Manager(AndroidJavaObject)</returns>
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

      AndroidJavaObject telManager = PlatformIntegrationUtil.Call<AndroidJavaObject>(context, "getSystemService", new object[] { CONTEXT_TELEPHONY_SERVICE });

      sdkVersion = getAndroidSDKVers();

      if (sdkVersion < 24)
      {
        return telManager;
      }

      // Call SubscriptionManager to get a specific telManager:
      AndroidJavaClass subscriptionManagerCls = PlatformIntegrationUtil.GetAndroidJavaClass("android.telephony.SubscriptionManager");
      if (subscriptionManagerCls == null)
      {
        Logger.Log("Can't get Subscription Manager Class.");
        return null;
      }
      int subId = PlatformIntegrationUtil.CallStatic<int>(subscriptionManagerCls, "getDefaultDataSubscriptionId");
      int invalidSubId = PlatformIntegrationUtil.GetStatic<int>(subscriptionManagerCls, "INVALID_SUBSCRIPTION_ID");
      if (subId == invalidSubId)
      {
        Logger.Log("The Subscription ID is invalid: " + subId);
        return null;
      }
      object[] idParam = new object[1] { subId };
      telManager = PlatformIntegrationUtil.Call<AndroidJavaObject>(telManager, "createForSubscriptionId", idParam);

      return telManager;
    }

    /// <summary>
	  /// Gets the current carrier name from the device
	  /// (Android) Requires TelephonyManager
	  /// </summary>
	  /// <returns>Network Operator Name (string)</returns>
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

    /// <summary>
	  /// MCC-MNC (Mobile Country Code- Mobile Network Code)
	  /// Gets the MCC-MNC code from the device, used for selecting the regional DME 
	  /// </summary>
	  /// <returns>MCC-MNC code (string)</returns>
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

    private KeyValuePair<string, ulong> GetCidKeyValuePair(AndroidJavaObject cellInfo)
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

    /// <summary>
	  /// Obtains a list of Cellular Info from the device.
	  /// (Android)Requires Access to TelephonyManager and permission to access the user fine location.
	  /// https://developer.android.com/reference/android/telephony/CellInfo
	  /// </summary>
	  /// <returns>List of KeyValue Pairs <string, unsigned long></returns>
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
      KeyValuePair<string, ulong> empty = new KeyValuePair<string, ulong>(null, 0);

      for (int i = 0; i < length; i++)
      {
        AndroidJavaObject cellInfo = PlatformIntegrationUtil.Call<AndroidJavaObject>(cellInfoList, "get", new object[] { i });
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

    /// <summary>
	  /// Obtains CellIdentity from the device
	  /// </summary>
	  /// <returns>CellIdentity (unsinged long)</returns>
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

    /// <summary>
    /// Obtains the NetworkDataType (GPRS, LTE ... )
    /// (Android)Requires READ_PHONE_STATE permission
    /// </summary>
    /// <returns> NetworkDataType (string)</returns>
    public string GetDataNetworkPath()
    {
      AndroidJavaObject telManager = GetTelephonyManager();
      if (telManager == null)
      {
        return "";
      }
      const string readPhoneStatePermissionString = "android.permission.READ_PHONE_STATE";
      try
      {
        if (Permission.HasUserAuthorizedPermission(readPhoneStatePermissionString))
        {
          int nType = PlatformIntegrationUtil.Call<int>(telManager, "getDataNetworkType");
          NetworkDataType datatype = (NetworkDataType)nType;
          return datatype.ToString();
        }
        else
        {
          return "";
        }
      }
      catch (Exception e)
      {
        Logger.LogWarning("Exception retrieving properties: " + e.GetBaseException() + ", " + e.Message);
        return "";
      }
    }

    /// <summary>
	  /// Obtains the Signal Strength of the network
	  /// (Android) Requires TelephonyManager
	  /// </summary>
	  /// <returns>Signal Strength (unsigned long)</returns>
    public ulong GetSignalStrength()
    {
      AndroidJavaObject telManager = GetTelephonyManager();
      if (telManager == null)
      {
        return 0;
      }
      AndroidJavaObject signalStrength = telManager.Call<AndroidJavaObject>("getSignalStrength");
      if (signalStrength == null)
      {
        return 0;
      }
      ulong signalStrengthLevel = (ulong)signalStrength.Call<int>("getLevel");
      return signalStrengthLevel;
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

    /// <summary>
	  /// Gets the current carrier name from the device
	  /// </summary>
	  /// <returns>Network Operator Name (string)</returns>
    public string GetCurrentCarrierName()
    {
      string networkOperatorName = "";
      if (Application.platform == RuntimePlatform.IPhonePlayer)
      {
        networkOperatorName = _getCurrentCarrierName();
      }
      return networkOperatorName;
    }

    /// <summary>
	  /// MCC-MNC (Mobile Country Code- Mobile Network Code)
	  /// Gets the MCC-MNC code from the device used for selecting the regional DME 
	  /// </summary>
	  /// <returns>MCC-MNC code (string)</returns>
    public string GetMccMnc()
    {
      string mccmnc = null;
      if (Application.platform == RuntimePlatform.IPhonePlayer)
      {
        mccmnc = _getMccMnc();
      }
      return mccmnc;
    }

    /// <summary>
	  /// Obtains CellIdentity from the device
	  /// </summary>
	  /// <returns>CellIdentity (unsinged long)</returns>
    public ulong GetCellID()
    {
      int cellID = 0;
      if (Application.platform == RuntimePlatform.IPhonePlayer)
      {
        cellID = _getCellID();
      }
      return (ulong)cellID;
    }

    /// <summary>
    /// Obtains the NetworkDataType (GPRS, LTE ... )
    /// Requires READ_PHONE_STATE permission on Android Phones
    /// </summary>
    /// <returns> NetworkDataType (string)</returns>
    public string GetDataNetworkPath()
    {
      return "";
    }

    /// <summary>
	  /// Obtains the Signal Strength of the network
	  /// </summary>
	  /// <returns>Signal Strength (unsigned long)</returns>
    public ulong GetSignalStrength()
    {
      return 0;
    }

    /// <summary>
	  /// Returns wether the device is on the normal provider network or another network.
	  /// This method compares between the ISO Country code obtained from the DeviceLocation and from the DeviceCarrier
	  /// (Asynchronous method) (iOS only)
	  /// </summary>
	  /// <param name="longitude">(double)</param>
	  /// <param name="latitude">(double)</param>
	  /// <returns>boolean value</returns>
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

    /// <summary>
    /// Convert GPS (longitude, latitude) to ISOCountryCode
	  /// (Asynchronous method)(iOS only)
    /// </summary>
    /// <param name="longitude">(double)</param>
    /// <param name="latitude">(double)</param>
    /// <returns>ISOCountryCode (string)</returns>
    public async Task<string> ConvertGPSToISOCountryCode(double longitude, double latitude)
    {
      if (Application.platform == RuntimePlatform.IPhonePlayer)
      {
        _convertGPSToISOCountryCode(longitude, latitude);
        return await Task.Run(() =>
        {
          string isoCC = "";
          while (isoCC == "" || isoCC == null)
          {
            isoCC = GetISOCountryCodeFromGPS();
          }
          return isoCC;
        }).ConfigureAwait(false);
      }

      return null;
    }

    /// <summary>
	  /// Gets the ISO Country Code from the device location.
	  /// (iOS only)
	  /// </summary>
	  /// <returns>ISO Country Code (string)</returns>
    public string GetISOCountryCodeFromGPS()
    {
      string isoCC = null;
      if (Application.platform == RuntimePlatform.IPhonePlayer)
      {
        isoCC = _getISOCountryCodeFromGPS();
      }
      return isoCC;
    }

    /// <summary>
	  /// Gets the ISO Country Code from the device carrier.
	  /// (iOS only)
	  /// </summary>
	  /// <returns>ISO Country Code (string)</returns>
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

    /// <summary>
    /// Gets the current carrier name from the device
    /// </summary>
    /// <returns>Network Operator Name (string)</returns>
    public string GetCurrentCarrierName()
    {
      Logger.Log("GetCurrentCarrierName is NOT IMPLEMENTED");
      return null;
    }

    /// <summary>
	  /// MCC-MNC (Mobile Country Code- Mobile Network Code)
	  /// Gets the MCC-MNC code from the device used for selecting the regional DME 
	  /// </summary>
	  /// <returns>MCC-MNC code (string)</returns>
    public string GetMccMnc()
    {
      Logger.Log("GetMccMnc is NOT IMPLEMENTED");
      return null;
    }

    /// <summary>
	  /// Obtains CellIdentity from the device
	  /// </summary>
	  /// <returns>CellIdentity (unsinged long)</returns>
    public ulong GetCellID()
    {
      Logger.Log("GetCellID is NOT IMPLEMENTED");
      return 0;
    }

    /// <summary>
    /// Obtains the NetworkDataType (GPRS, LTE ... )
    /// Requires READ_PHONE_STATE permission on Android Phones
    /// </summary>
    /// <returns> NetworkDataType (string)</returns>
    public string GetDataNetworkPath()
    {
      Logger.Log("GetDataNetworkPath is NOT IMPLEMENTED");
      return "";
    }

    /// <summary>
	  /// Obtains the Signal Strength of the network
	  /// </summary>
	  /// <returns>Signal Strength (unsigned long)</returns>
    public ulong GetSignalStrength()
    {
      Logger.Log("GetSignalStrength is NOT IMPLEMENTED");
      return 0;
    }

#endif

  }

  /// <summary>
  /// Used for testing in UnityEditor (any target platform)
  /// </summary>
  public class TestCarrierInfoClass : CarrierInfo
  {
    /// <summary>
	  /// Gets the current carrier name from the device
	  /// </summary>
	  /// <returns>Network Operator Name (string)</returns>
    public string GetCurrentCarrierName()
    {
      return "";
    }

    /// <summary>
	  /// MCC-MNC (Mobile Country Code- Mobile Network Code)
	  /// Gets the MCC-MNC code from the device used for selecting the regional DME 
	  /// </summary>
	  /// <returns>MCC-MNC code (string)</returns>
    public string GetMccMnc()
    {
      return "";
    }

    /// <summary>
	  /// Obtains CellIdentity from the device
	  /// </summary>
	  /// <returns>CellIdentity (unsinged long)</returns>
    public ulong GetCellID()
    {
      return 0;
    }

    /// <summary>
    /// Obtains the NetworkDataType (GPRS, LTE ... )
    /// Requires READ_PHONE_STATE permission on Android Phones
    /// </summary>
    /// <returns> NetworkDataType (string)</returns>
    public string GetDataNetworkPath()
    {
      return "";
    }

    /// <summary>
	  /// Obtains the Signal Strength of the network
	  /// </summary>
	  /// <returns>Signal Strength (unsigned long)</returns>
    public ulong GetSignalStrength()
    {
      return 0;
    }
  }
}
