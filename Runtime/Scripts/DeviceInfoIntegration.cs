using System;
using System.Collections.Generic;
using DistributedMatchEngine;
using MobiledgeX;
using UnityEngine;

public class DeviceInfoIntegration
{
  string TAG = "DeviceInfoIntegration";

  public DeviceInfoIntegration()
  {
  }

  // Placeholder, if available, just use the Unity version.

#if UNITY_ANDROID
  Dictionary<string, string> GetDeviceDetails()
  {
    CarrierInfoClass carrierInfo = new CarrierInfoClass();
    AndroidJavaObject telephonyManager = carrierInfo.GetTelephonyManager();
    Dictionary<string, string> map;

    if (telephonyManager == null)
    {
      return null;
    }
    map = new Dictionary<string, string>();

    AndroidJavaClass versionClass = new AndroidJavaClass("android.os.Build$VERSION");
    int sdk_int = PlatformIntegrationUtil.GetStatic<int>(versionClass, "SDK_INT");
    map["Build.VERSION.SDK_INT"] = sdk_int.ToString();

    try
    {
      String ver = PlatformIntegrationUtil.Call<string>(telephonyManager, "getDeviceSoftwareVersion");
      map["DeviceSoftwareVersion"] = ver.ToString();
    }
    catch (Exception e)
    {
      Debug.LogFormat("[{0}] {1}", TAG, "");
      // Ignoring and continue.
    }

    AndroidJavaClass versionCodesClass = new AndroidJavaClass("android.os.Build$VERSION_CODES");
    int versionCode = PlatformIntegrationUtil.CallStatic<int>(versionCodesClass, "Q");
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

    string phoneType = PlatformIntegrationUtil.Call<string>(telephonyManager, "getPhoneType");
    if (phoneType != null)
    {
      map["PhoneType"] = phoneType;
    }

    // Default one.
    String simOperatorName = PlatformIntegrationUtil.Call<string>(telephonyManager, "getSimOperatorName");
    if (simOperatorName != null)
    {
      map["SimOperator"] = simOperatorName;
    }

    // Default one.
    String networkOperator = PlatformIntegrationUtil.Call<string>(telephonyManager, "getNetworkOperatorName");
    if (networkOperator != null)
    {
      map["getNetworkOperatorName"] = networkOperator;
    }

    return map;
  }
#elif UNITY_IOS
  Dictionary<string, string> GetDeviceDetails()
  {
    Debug.LogFormat("[{0}] DeviceInfo not implemented!", TAG);
  }
#endif

  // Used for DeviceInfo in UnityEditor (any target platform)
  public class TestDeviceInfoClass : DeviceInfo
  {
    public string GetUniqueIDType()
    {
      return "";
    }
    public string GetUniqueID()
    {
      return "";
    }
  }

}
