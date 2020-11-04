using System;
using System.Collections.Generic;
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

    // Placeholder, if available, just use the Unity version.

#if UNITY_ANDROID
    public Dictionary<string, string> GetDeviceInfo()
    {
      CarrierInfoClass carrierInfo = new CarrierInfoClass();
      AndroidJavaObject telephonyManager = carrierInfo.GetTelephonyManager();
      Dictionary<string, string> map;

      if (telephonyManager == null)
      {
        Debug.Log("No TelephonyManager!");
        return null;
      }
      map = new Dictionary<string, string>();

      AndroidJavaClass versionClass = new AndroidJavaClass("android.os.Build$VERSION");
      int sdk_int = PlatformIntegrationUtil.GetStatic<int>(versionClass, "SDK_INT");
      map["Build.VERSION.SDK_INT"] = sdk_int.ToString();

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
        Debug.Log("Exception retrieving properties: " + e.GetBaseException() + ", " + e.Message);
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
        Debug.Log("Exception retrieving properties: " + e.GetBaseException() + ", " + e.Message);
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
  private static extern Dictionary<string, string> _getDeviceInfo();

  public Dictionary<string, string> GetDeviceInfo()
  {
    Dictionary<string, string> deviceInfo = new Dictionary<string, string>();
    if (Application.platform == RuntimePlatform.IPhonePlayer)
    {
      deviceInfo = _getDeviceInfo());
    }
    return deviceInfo;
  }
#else // Unsupported platform.
  public Dictionary<string, string> GetDeviceInfo()
  {
    Debug.LogFormat("DeviceInfo not implemented!");
    return null;
  }
#endif
  }

  // Used for DeviceInfo in UnityEditor (any target platform)
  public class TestDeviceInfo : DeviceInfoIntegration
  {
    public Dictionary<string, string> GetDeviceInfo()
    {
      return null;
    }
  }
}
