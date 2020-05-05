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
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using DistributedMatchEngine;

// We need this one for importing our IOS functions
using System.Runtime.InteropServices;

namespace MobiledgeX
{
  public class PlatformIntegration
  {
    public NetworkInterfaceName NetworkInterfaceName { get; }

    // These will be passed into the constructor for MatchingEngine
    public CarrierInfo CarrierInfo { get; }
    public NetInterface NetInterface { get; }
    public UniqueID UniqueID { get; }

    public PlatformIntegration()
    {
            // Target Device Network Interfaces. This is not known until compile time:
#if UNITY_ANDROID
      NetworkInterfaceName = new AndroidNetworkInterfaceName();
                 // Editor or Player network management (overrides target device platform):
      switch (Application.platform)
      {
        case RuntimePlatform.OSXPlayer: case RuntimePlatform.OSXEditor:
          NetworkInterfaceName = new MacNetworkInterfaceName();
          CarrierInfo = new TestCarrierInfoClass();
          UniqueID = new TestUniqueIDClass();
          break;
        default:
          CarrierInfo = new CarrierInfoClass();
          UniqueID = new UniqueIDClass();
          break;
      }
#elif UNITY_IOS
            NetworkInterfaceName = new IOSNetworkInterfaceName();
#else
      Debug.Log("Unknown or unsupported platform. Please create WiFi and Cellular interface name Object for your platform");
#endif
      // Editor or Player network management (overrides target device platform):
      switch (Application.platform)
      {
        case RuntimePlatform.OSXPlayer: case RuntimePlatform.OSXEditor:
          NetworkInterfaceName = new MacNetworkInterfaceName();
          CarrierInfo = new TestCarrierInfoClass();
          UniqueID = new TestUniqueIDClass();
          break;
        default:
          CarrierInfo = new CarrierInfoClass();
          UniqueID = new UniqueIDClass();
          break;
      }
      NetInterface = new NetInterfaceClass(NetworkInterfaceName);
    }
  }
}
