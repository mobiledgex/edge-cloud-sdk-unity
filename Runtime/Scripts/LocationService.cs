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

namespace MobiledgeX
{
    public class LocationException : Exception
{
  public LocationException(string message) : base(message)
  {
  }
}

public class LocationTimeoutException : Exception
{
  public LocationTimeoutException(string message) : base(message)
  {
  }
}

// Unity Location Service, based on the documentation example:
public class LocationService : MonoBehaviour
{
  static LocationInfo locationInfo;
  Stopwatch stopWatch = new Stopwatch();
  TimeSpan interval = new TimeSpan(0, 0, 10);
  bool enabledByUser = false;

  // LocationService enabled continuously?
  bool locationServiceEnabled { get; set; } = true;

  public void Start()
  {
    enabledByUser = Input.location.isEnabledByUser;
    if (!enabledByUser)
    {
      return;
    }
    Input.location.Start();
  }

  public void Update()
  {
    enabledByUser = Input.location.isEnabledByUser;
    if (!enabledByUser)
    {
      return;
    }

    switch (Input.location.status)
    {
      case LocationServiceStatus.Failed:
        break;
      case LocationServiceStatus.Initializing:
        if (stopWatch.Elapsed > interval)
        {
          // Failed.
          stopWatch.Stop();
        }
        break;
      case LocationServiceStatus.Running:
        locationInfo = Input.location.lastData;
        if (!locationServiceEnabled)
        {
          Input.location.Stop();
        }
        break;
      case LocationServiceStatus.Stopped:
        stopWatch.Reset();
        stopWatch.Start();
        Input.location.Start();
        break;
    }
  }


  public static LocationInfo UpdateLocation()
  {
    Input.location.Start();
    return locationInfo;
  }

  // Retrieve the lastest location, without restarting locationService.
  public static Loc RetrieveLocation()
  {
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
