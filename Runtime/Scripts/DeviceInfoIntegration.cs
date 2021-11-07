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

using DistributedMatchEngine;
using UnityEngine;
using System.Runtime.InteropServices;

namespace MobiledgeX
{
  public class DeviceInfoIntegration : DeviceInfoApp
  {
    CarrierInfo overrideCarrierInfo;
    public DeviceInfoIntegration(CarrierInfo carrierInfo = null)
    {
      overrideCarrierInfo = carrierInfo;
    }

    public DeviceInfoDynamic GetDeviceInfoDynamic()
    {
      DeviceInfoDynamic deviceInfoDynamic = new DeviceInfoDynamic();
      CarrierInfo carrierInfo;
      if (overrideCarrierInfo != null)
      {
        carrierInfo = overrideCarrierInfo;
      }
      else
      {
        carrierInfo = new CarrierInfoClass();
      }
      deviceInfoDynamic.CarrierName = carrierInfo.GetCurrentCarrierName();
      deviceInfoDynamic.SignalStrength = carrierInfo.GetSignalStrength();
      deviceInfoDynamic.DataNetworkType = carrierInfo.GetDataNetworkType();
      return deviceInfoDynamic;
    }

#if UNITY_ANDROID
    public DeviceInfoStatic GetDeviceInfoStatic()
    {
      string deviceModel = "Android";
      string deviceOS = "Android";
      if (overrideCarrierInfo == null)
      {
        CarrierInfoClass carrierInfo = new CarrierInfoClass();
        deviceModel += carrierInfo.GetManufacturer();
        deviceOS += carrierInfo.getAndroidSDKVers(); 
      }
      DeviceInfoStatic deviceInfoStatic = new DeviceInfoStatic()
      {
        DeviceModel = deviceModel,
        DeviceOs = deviceOS
      };
      return deviceInfoStatic;
    }

    public bool IsPingSupported()
    {
      return true;
    }

#elif UNITY_IOS

    [DllImport("__Internal")]
    private static extern string _getDeviceModel();

    [DllImport("__Internal")]
    private static extern string _getOperatingSystem();

    public DeviceInfoStatic GetDeviceInfoStatic()
    {
      string deviceModel = _getDeviceModel();
      string deviceOS = _getOperatingSystem();

      DeviceInfoStatic deviceInfoStatic = new DeviceInfoStatic()
      {
        DeviceModel = deviceModel,
        DeviceOs = deviceOS
      };
      return deviceInfoStatic;
    }

    public bool IsPingSupported()
    {
      return false;
    }

#else // Unsupported platform.
    public DeviceInfoStatic GetDeviceInfoStatic()
    {
      DeviceInfoStatic deviceInfoStatic = new DeviceInfoStatic()
      {
        DeviceModel = "UnityUnsupportedDeviceModel",
        DeviceOs = "UnityUnsupportedDeviceOS"
      };
      return deviceInfoStatic;
    }
    public bool IsPingSupported()
    {
      return true;
    }
#endif
  }

  // Used for DeviceInfo in UnityEditor (any target platform)
  public class TestDeviceInfo : DeviceInfoApp
  {
    public DeviceInfoDynamic GetDeviceInfoDynamic()
    {
      Logger.Log("DeviceInfoDynamic not implemented!");
      return null;
    }

    public DeviceInfoStatic GetDeviceInfoStatic()
    {
      Logger.Log("DeviceInfoStatic not implemented!");
      return null;
    }

    public bool IsPingSupported()
    {
      return true;
    }
  }
}
