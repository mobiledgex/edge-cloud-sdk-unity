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


        public void Start()
        {

        }

        public void Update()
        {
        }

        public static async Task<LocationInfo> UpdateLocation()
        {
            // Main thread check, so, submit to main thread.

            // First, check if user has location service enabled
            if (!Input.location.isEnabledByUser)
            {
                // For UNITY_ANDROID, you will need to create an app specific UI to request access to sensitive permissions.
#if UNITY_ANDROID
      print("Location Services Disabled");
      // Per documentation, on iOS, CoreLocation asks the user for permission.
      return Input.location.lastData;
#elif UNITY_IOS
                print("CoreLocation.");
#else
      print("Location Services Disabled, cannot get location.");
      return Input.location.lastData;
#endif
            }

            // Start service before querying location
            Input.location.Start();

            // Wait until service initializes
            int maxWait = 10;
            int start = maxWait;
            while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0)
            {
                await Task.Delay(TimeSpan.FromSeconds(1));
                maxWait--;
            }

            print("Location Services waited " + (start - maxWait));
            // Service didn't initialize in time.
            if (maxWait < 1)
            {
                print("Timed out");
                throw new LocationTimeoutException("Location Services not returning results!");
            }

            // Connection has failed
            if (Input.location.status == LocationServiceStatus.Failed)
            {
                print("Unable to determine device location");
                throw new LocationException("Location Services can't find location!");
            }
            else
            {
                // Access granted and location value could be retrieved
                print("Location Services= lat: " + Input.location.lastData.latitude + ", long: " + Input.location.lastData.longitude);
                print("Location: " + Input.location.lastData.latitude + " " + Input.location.lastData.longitude + " " + Input.location.lastData.altitude + " " + Input.location.lastData.horizontalAccuracy + " " + Input.location.lastData.timestamp);
            }

            // Stop service if there is no need to query location updates continuously
            Input.location.Stop();

            LocationInfo info = Input.location.lastData;

            return info;
        }

        // Return a previously resolved location. Does not start location services.
        public static DistributedMatchEngine.Loc GetLastLocation()
        {
            return ConvertUnityLocationToDMELoc(Input.location.lastData);
        }

        // Retrieve the lastest location.
        public static async Task<DistributedMatchEngine.Loc> RetrieveLocation()
        {
            LocationInfo locationInfo = await UpdateLocation();
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
