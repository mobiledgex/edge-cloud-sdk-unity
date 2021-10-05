/**
 * Copyright 2018-2021 MobiledgeX, Inc. All rights and licenses reserved.
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

using UnityEngine;
using MobiledgeX;
using DistributedMatchEngine;

/// <summary>
/// This class is an example of using MobiledgeX SDK on NonCellular Device (Devices with no sim card)
/// The class also provides a location override through SetFallbackLocation method
/// </summary>
[RequireComponent(typeof(EdgeEventsManager))]
public class ExampleNonCellular : MonoBehaviour
{
  MobiledgeXIntegration mxi;
  MobiledgeXIntegration.LocationFromIPAddress location;
  async void Start()
  {
    CarrierInfo carrierInfo = new TestCarrierInfoClass();
    DeviceInfoApp deviceInfo = new DeviceInfoIntegration(carrierInfo);
    mxi = new MobiledgeXIntegration(FindObjectOfType<EdgeEventsManager>(), carrierInfo: carrierInfo, deviceInfo: deviceInfo);
    location = await MobiledgeXIntegration.GetLocationFromIP();
    mxi.SetFallbackLocation(location.longitude, location.latitude);
    mxi.useFallbackLocation = true;
    mxi.edgeEventsManager.location = new Loc() { Latitude = location.latitude, Longitude = location.longitude };
    mxi.NewFindCloudletHandler += HandleFindCloudlet;
    try
    {
      await mxi.RegisterAndFindCloudlet();
    }
    //FindCloudletException is thrown if there is no app instance in the user region
    catch (FindCloudletException fce)
    {
      Debug.Log("FindCloudletException: " + fce.Message + "Inner Exception: " + fce.InnerException);
      // your fallback logic here
    }
    mxi.GetAppPort(LProto.Tcp); // or LProto.L_PROTO_UDP
    string url = mxi.GetUrl("http"); // or another L7 proto such as https, ws, wss, udp

    Debug.Log("url : " + url); // Once you have your edge server url you can start communicating with your Edge server deployed on MobiledgeX Console
  }

  private void HandleFindCloudlet(EdgeEventsStatus edgeEventstatus, FindCloudletEvent fcEvent)
  {
    print("NewFindCloudlet triggered status is " + edgeEventstatus.status + ", Trigger" + fcEvent.trigger);
    if (fcEvent.newCloudlet != null)
    {
      print("New Cloudlet FQDN: " + fcEvent.newCloudlet.Fqdn);
    }
    if (edgeEventstatus.status == Status.error)
    {
      print("Error received: " + edgeEventstatus.error);
    }
  }


  private void OnDestroy()
  {
    mxi.NewFindCloudletHandler -= HandleFindCloudlet;
    mxi.matchingEngine.Dispose();
  }
}
