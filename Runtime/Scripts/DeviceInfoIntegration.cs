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
using System.Runtime.InteropServices;
using DistributedMatchEngine;
using UnityEngine;
using UnityEngine.Android;

namespace MobiledgeX
{
  public class DeviceInfoIntegration : DeviceInfoApp
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

#if UNITY_ANDROID
    public DeviceDynamicInfo GetDeviceDynamicInfo()
    {
      DeviceDynamicInfo deviceInfoDynamic = new DeviceDynamicInfo();
      CarrierInfoClass carrierInfo = new CarrierInfoClass();
      if (UnityEngine.XR.XRSettings.loadedDeviceName.Contains("oculus"))
      {
        return deviceInfoDynamic;
      }
      AndroidJavaObject telephonyManager = carrierInfo.GetTelephonyManager();
      if (telephonyManager == null)
      {
        Logger.Log("No TelephonyManager!");
        return deviceInfoDynamic;
      }
      int signalStrengthLevel = carrierInfo.GetSignalStrength();//Doesn't require SignalStrength
      if (signalStrengthLevel >= 0)
      {
        deviceInfoDynamic.SignalStrength = (ulong)signalStrengthLevel;
      }
      string simOperatorName = PlatformIntegrationUtil.Call<string>(telephonyManager, "getSimOperatorName");
      if (simOperatorName != null)
      {
        deviceInfoDynamic.CarrierName = simOperatorName;
      }
      const string readPhoneStatePermissionString = "android.permission.READ_PHONE_STATE";
      try
      {
        if (Permission.HasUserAuthorizedPermission(readPhoneStatePermissionString))
        {
          int nType = PlatformIntegrationUtil.Call<int>(telephonyManager, "getDataNetworkType");
          NetworkDataType datatype = (NetworkDataType)nType;
          deviceInfoDynamic.DataNetworkType = datatype.ToString();
        }
      }
      catch (Exception e)
      {
        Logger.LogWarning("Exception retrieving properties: " + e.GetBaseException() + ", " + e.Message);
      }
      return deviceInfoDynamic;
    }

    public DeviceStaticInfo GetDeviceStaticInfo()
    {
      DeviceStaticInfo deviceInfoStatic = new DeviceStaticInfo()
      {
        DeviceModel = SystemInfo.deviceModel,
        DeviceOs = SystemInfo.operatingSystem
      };
      return deviceInfoStatic;
    }

    public bool IsUdpPingSupported()
    {
      return true;
    }

#elif UNITY_IOS
    
    public DeviceDynamicInfo GetDeviceDynamicInfo()
    {
      CarrierInfoClass carrierInfo = new CarrierInfoClass();
      DeviceDynamicInfo deviceInfoDynamic = new DeviceDynamicInfo()
      {
        CarrierName = carrierInfo.GetCurrentCarrierName()
      };
      //TODO Cellular Signal Strength for iOS if possible 
      return deviceInfoDynamic;
    }

    public DeviceStaticInfo GetDeviceStaticInfo()
    {
      DeviceStaticInfo deviceInfoStatic = new DeviceStaticInfo()
      {
        DeviceModel = SystemInfo.deviceModel,
        DeviceOs = SystemInfo.operatingSystem
      };
      return deviceInfoStatic;
    }

    public bool IsUdpPingSupported()
    {
      return false;
    }

#else // Unsupported platform.
    public DeviceDynamicInfo GetDeviceDynamicInfo()
    {
      Logger.Log("DeviceDynamicInfo not implemented!");
      return null;
    }

    public DeviceStaticInfo GetDeviceStaticInfo()
    {
      DeviceStaticInfo deviceInfoStatic = new DeviceStaticInfo()
      {
        DeviceModel = SystemInfo.deviceModel,
        DeviceOs = SystemInfo.operatingSystem
      };
      return deviceInfoStatic;
    }
    public bool IsUdpPingSupported()
    {
      return true;
    }
#endif
  }

  // Used for DeviceInfo in UnityEditor (any target platform)
  public class TestDeviceInfo : DeviceInfoApp
  {
    public DeviceDynamicInfo GetDeviceDynamicInfo()
    {
      Logger.Log("DeviceDynamicInfo not implemented!");
      return null;
    }

    public DeviceStaticInfo GetDeviceStaticInfo()
    {
      Logger.Log("DeviceStaticInfo not implemented!");
      return null;
    }

    public bool IsUdpPingSupported()
    {
      return true;
    }
  }
}
