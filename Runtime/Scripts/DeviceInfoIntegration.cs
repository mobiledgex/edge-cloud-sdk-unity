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

using System.Collections.Generic;
using DistributedMatchEngine;
using UnityEngine;

namespace MobiledgeX
{
  public class DeviceInfoIntegration : DeviceInfo
  {
    CarrierInfo overrideCarrierInfo;

    public DeviceInfoIntegration(CarrierInfo overrideCarrierInfo = null)
    {
      this.overrideCarrierInfo = overrideCarrierInfo;
    }

    public DeviceInfoIntegration()
    {
    }

    public Dictionary<string, string> GetDeviceInfo()
    {
      CarrierInfoClass carrierInfo = (CarrierInfoClass)overrideCarrierInfo;
      if (carrierInfo == null)
      {
        carrierInfo = new CarrierInfoClass();
      }
      Dictionary<string, string> deviceInfo = new Dictionary<string, string>();
      deviceInfo["DataNetworkPath"] = carrierInfo.GetDataNetworkPath();
      deviceInfo["CarrierName"] = carrierInfo.GetCurrentCarrierName();
      deviceInfo["SignalStrength"] = carrierInfo.GetSignalStrength().ToString();
      deviceInfo["DeviceOS"] = SystemInfo.operatingSystem;
      deviceInfo["DeviceModel"] = SystemInfo.deviceModel;
      return deviceInfo;
    }
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
