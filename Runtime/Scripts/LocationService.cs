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

using UnityEngine;

using System;
using System.Threading.Tasks;
using DistributedMatchEngine;
using UnityEngine.Android;
using System.Diagnostics;
using System.Collections;

namespace MobiledgeX
{

  // Unity Location Service, based on the documentation example:
  public class LocationService : MonoBehaviour
  {
    // LocationService enabled continuously?

    public static IEnumerator InitalizeLocationService(int maxWait = 20, bool continuousLocationService = true)
    {
      // First, check if user has location service enabled
      if (!Input.location.isEnabledByUser)
      {
        UnityEngine.Debug.Log("Location Service disabled by user.");
        yield break;
      }

      // Start service before querying location
      Input.location.Start();

      // Wait until service initializes
      while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0)
      {
        yield return new WaitForSeconds(1);
        maxWait--;
      }

      // Service didn't initialize in 20 seconds
      if (maxWait < 1)
      {
        print("Timed out");
        yield break;
      }

      // Connection has failed
      if (Input.location.status == LocationServiceStatus.Failed)
      {
        print("Unable to determine device location");
        yield break;
      }
      else
      {
        // Access granted and location value could be retrieved
        print("Location Service has location: " + Input.location.lastData.latitude + " " + Input.location.lastData.longitude + " " + Input.location.lastData.altitude + " " + Input.location.lastData.horizontalAccuracy + " " + Input.location.lastData.timestamp);
      }

      // Stop service if there is no need to query location updates continuously
      if (!continuousLocationService)
      {
        Input.location.Stop();
      }
    }

    public IEnumerator Start()
    {
      yield return StartCoroutine(InitalizeLocationService());
    }

    public void Update()
    {

    }

    public static void ensurePermissions()
    {
#if PLATFORM_ANDROID
      if (!Permission.HasUserAuthorizedPermission(Permission.FineLocation))
      {
        Permission.RequestUserPermission(Permission.FineLocation);
      }
#endif
    }

    public void OnApplicationFocus(bool hasFocus)
    {
      if (hasFocus)
      {
        ensurePermissions();
      }
    }

    // Retrieve the lastest location, without restarting locationService.
    public static Loc RetrieveLocation()
    {
      if (Input.location.status != LocationServiceStatus.Running)
      {
        UnityEngine.Debug.Log("Warning: Location Status must be in running state to get a valid location! Current Status: " + Input.location.status);
      }
      LocationInfo locationInfo = Input.location.lastData;
      UnityEngine.Debug.Log("Location Info: [" + locationInfo.longitude + "," + locationInfo.latitude + "]");
      return ConvertUnityLocationToDMELoc(locationInfo);
    }

    public static DistributedMatchEngine.Timestamp ConvertTimestamp(double timeInSeconds)
    {
      DistributedMatchEngine.Timestamp ts;

      int nanos;
      long sec = (long)(timeInSeconds); // Truncate.
      double remainder = timeInSeconds - (double)sec;

      nanos = (int)(remainder * 1e9);
      ts = new DistributedMatchEngine.Timestamp { seconds = sec.ToString(), nanos = nanos };
      return ts;
    }

    public static DistributedMatchEngine.Loc ConvertUnityLocationToDMELoc(UnityEngine.LocationInfo info)
    {
      DistributedMatchEngine.Timestamp ts = ConvertTimestamp(info.timestamp);

      Loc loc = new Loc
      {
        latitude = info.latitude,
        longitude = info.longitude,
        horizontal_accuracy = info.horizontalAccuracy,
        vertical_accuracy = info.verticalAccuracy,
        altitude = info.altitude,
        course = 0f,
        speed = 0f,
        timestamp = ts
      };

      return loc;
    }

  }
}
