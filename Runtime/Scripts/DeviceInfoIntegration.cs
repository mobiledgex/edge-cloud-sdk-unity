/*
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
using System.Runtime.InteropServices;
using DistributedMatchEngine;
using MobiledgeX;
using UnityEngine;
using UnityEngine.Android;

namespace MobiledgeX
{
  public class DeviceInfoIntegration : DeviceInfo
  {
    public DeviceInfoIntegration()
    {
    }

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

    // Placeholder, if available, just use the Unity version
#if UNITY_ANDROID
    public Dictionary<string, string> GetDeviceInfo()
    {
      CarrierInfoClass carrierInfo = new CarrierInfoClass();
      Dictionary<string, string> map = new Dictionary<string, string>();
      int sdk_int = carrierInfo.getAndroidSDKVers();
      map["Build.VERSION.SDK_INT"] = sdk_int.ToString();
      if (UnityEngine.XR.XRSettings.loadedDeviceName.Contains("oculus"))
      {
          return map;
      }
      AndroidJavaObject telephonyManager = carrierInfo.GetTelephonyManager();
      if (telephonyManager == null)
      {
          Logger.Log("No TelephonyManager!");
          return map;
      }
      const string readPhoneStatePermissionString = "android.permission.READ_PHONE_STATE";
      try
      {
        if (Permission.HasUserAuthorizedPermission(readPhoneStatePermissionString))
        {
          string ver = PlatformIntegrationUtil.Call<string>(telephonyManager, "getDeviceSoftwareVersion");
          if (ver != null)
          {
            map["DeviceSoftwareVersion"] = ver.ToString();
          }
        }
      }
      catch (Exception e)
      {
        Logger.LogWarning("Exception retrieving properties: " + e.GetBaseException() + ", " + e.Message);
      }

      try
      {
        if (Permission.HasUserAuthorizedPermission(readPhoneStatePermissionString))
        {
          int nType = PlatformIntegrationUtil.Call<int>(telephonyManager, "getDataNetworkType");
          NetworkDataType datatype = (NetworkDataType)nType;
          map["DataNetworkType"] = datatype.ToString();
        }
      }
      catch (Exception e)
      {
        Logger.LogWarning("Exception retrieving properties: " + e.GetBaseException() + ", " + e.Message);
      }

      AndroidJavaClass versionCodesClass = new AndroidJavaClass("android.os.Build$VERSION_CODES");
      int versionCode = PlatformIntegrationUtil.GetStatic<int>(versionCodesClass, "Q");
      if (sdk_int > versionCode)
      {
        string mc = PlatformIntegrationUtil.Call<string>(telephonyManager, "getManufacturerCode");
        if (mc != null)
        {
          map["ManufacturerCode"] = mc;
        }
      }

      string niso = PlatformIntegrationUtil.Call<string>(telephonyManager, "getNetworkCountryIso");
      if (niso != null)
      {
        map["NetworkCountryIso"] = niso;
      }

      string siso = PlatformIntegrationUtil.Call<string>(telephonyManager, "getSimCountryIso");
      if (siso != null)
      {
        map["SimCountryCodeIso"] = siso;
      }

      int phoneType = PlatformIntegrationUtil.Call<int>(telephonyManager, "getPhoneType");
      map["PhoneType"] = phoneType.ToString();

      // Default one.
      string simOperatorName = PlatformIntegrationUtil.Call<string>(telephonyManager, "getSimOperatorName");
      if (simOperatorName != null)
      {
        map["SimOperatorName"] = simOperatorName;
      }

      // Default one.
      string networkOperator = PlatformIntegrationUtil.Call<string>(telephonyManager, "getNetworkOperatorName");
      if (networkOperator != null)
      {
        map["NetworkOperatorName"] = networkOperator;
      }

      return map;
    }
#elif UNITY_IOS
    [DllImport("__Internal")]
    private static extern string _getManufacturerCode();
    
    [DllImport("__Internal")]
    private static extern string _getDeviceSoftwareVersion();
    
    [DllImport("__Internal")]
    private static extern string _getDeviceModel();
    
    [DllImport("__Internal")]
    private static extern string _getOperatingSystem();

    public Dictionary<string, string> GetDeviceInfo()
    {
      Dictionary<string, string> deviceInfo = new Dictionary<string, string>();
      if (Application.platform == RuntimePlatform.IPhonePlayer)
      {
        // Fill in device system info
        deviceInfo["ManufacturerCode"] = _getManufacturerCode();
        deviceInfo["DeviceSoftwareVersion"] = _getDeviceSoftwareVersion();
        deviceInfo["DeviceModel"] = _getDeviceModel();
        deviceInfo["OperatingSystem"] = _getOperatingSystem();
        // Fill in carrier/ISO info
        CarrierInfoClass carrierInfo = new CarrierInfoClass();
        deviceInfo["SimOperatorName"] = carrierInfo.GetCurrentCarrierName();
        deviceInfo["SimCountryCodeIso"] = carrierInfo.GetISOCountryCodeFromCarrier();
      }
      return deviceInfo;
    }
#else // Unsupported platform.
    public Dictionary<string, string> GetDeviceInfo()
    {
      Logger.Log("DeviceInfo not implemented!");
      return null;
    }
#endif
  }

  // Used for DeviceInfo in UnityEditor (any target platform)
  public class TestDeviceInfo : DeviceInfo
  {
      public Dictionary<string, string> GetDeviceInfo()
      {
          Logger.Log("DeviceInfo not implemented!");
          return null;
      }
  }
}
