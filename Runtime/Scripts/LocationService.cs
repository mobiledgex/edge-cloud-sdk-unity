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
using DistributedMatchEngine;
using UnityEngine.Android;
using System.Collections;
using System;

namespace MobiledgeX
{
    // Unity Location Service, based on the documentation example
    [AddComponentMenu("MobiledgeX/LocationService")]
    public class LocationService : MonoBehaviour
    {
        /// <summary>
        /// After initializing location services, this is the maximum wait time (in seconds) to acquire location info
        /// if the value selected is zero or less the default value (20 seconds) will be used.
        /// </summary>
        [Tooltip("After initializing location services, this is the maximum wait time (in seconds) to acquire location info" +
            "if the value selected is zero or less the default value (20 seconds) will be used.")]
        public int timeOut = 20;

        /// <summary>
        /// EnsureLocation Confirm that user location is valid, user location is essential for MobiledgeX services
        /// If Location permission is denied by User an exception will be thrown once RetrieveLocation() is called
        /// </summary>
        public static IEnumerator EnsureLocation()
        {
            if (!SystemInfo.supportsLocationService)
            {
                Logger.Log("Your Device doesn't support LocationService");
                yield break;
            }

#if UNITY_EDITOR
            Logger.Log("LocationService is not supported in UNITY_EDITOR");
            yield break;
#else

            int timeOutValue = FindObjectOfType<LocationService>().timeOut;
            int maxWait = timeOutValue > 0 ? timeOutValue : 20;
            if (Application.platform == RuntimePlatform.IPhonePlayer)
            {
                Input.location.Start();
                yield return new WaitForEndOfFrame();
                while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0)
                {
                    yield return new WaitForSeconds(1);
                    Logger.Log("Initializing Location Service, Exiting in " + maxWait);
                    maxWait--;
                }

                // Service didn't initialize in before the timeOut threshold
                if (maxWait < 1)
                {
                    Logger.Log("Initializing Location Service Timed Out");
                    yield break;//Exception will be thrown from RetrieveLocation()
                }
                else
                {
                    if (Input.location.status == LocationServiceStatus.Running)
                    {
                        Logger.Log("Location Service succeded, Stopping LocationService, data saved to Input.location.lastData");
                        Input.location.Stop();
                    }
                    yield break;
                }
            }

            if (Application.platform == RuntimePlatform.Android)
            {
                if (Input.location.isEnabledByUser)
                {
                    Input.location.Start();
                    while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0)
                    {
                        yield return new WaitForSeconds(1);
                        Logger.Log("Initializing Location Service, Exiting in " + maxWait);
                        maxWait--;
                    }

                    // Service didn't initialize in before the timeOut threshold
                    if (maxWait < 1)
                    {
                        Logger.Log("Initializing Location Service Timed Out");
                        Input.location.Stop();
                        yield break;//Exception will be thrown from RetrieveLocation()
                    }
                    if (Input.location.status == LocationServiceStatus.Running)
                    {
                        Logger.Log("Location Service succeded, Stopping LocationService, data saved to Input.location.lastData");
                        Input.location.Stop();
                    }
                    yield break;
                }
                else
                {
                    Logger.Log("Location permission is not allowed by user yet.");
                    Permission.RequestUserPermission(Permission.FineLocation);
                    yield return new WaitForEndOfFrame(); // Application Out of focus , waiting for user decision on Location Permission
                    if (!Permission.HasUserAuthorizedPermission(Permission.FineLocation))
                    {
                        Logger.Log("User rejected location permission.");
                        yield break;//Exception will be thrown from RetrieveLocation()
                    }
                    else
                    {
                        Logger.Log("User accepted location permission.");
                        Input.location.Start();
                        while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0)
                        {
                            yield return new WaitForSeconds(1);
                            Logger.Log("Initializing Location Service, Exiting in " + maxWait);
                            maxWait--;
                        }
                        // Service didn't initialize in before the timeOut threshold
                        if (maxWait < 1)
                        {
                            Logger.Log("Initializing Location Service Timed Out");
                            Input.location.Stop();
                            yield break;//Exception will be thrown from RetrieveLocation()
                        }
                        else
                        {
                            if (Input.location.status == LocationServiceStatus.Running)
                            {
                                Logger.Log("Location Service succeded, Stopping LocationService, data saved to Input.location.lastData");
                                Input.location.Stop();
                            }
                            yield break;
                        }
                    }
                }
            }
#endif
        }

        // Retrieve the lastest location, without restarting locationService.
        public static Loc RetrieveLocation()
        {
            LocationInfo locationInfo = Input.location.lastData;
            if (locationInfo.Equals(default(LocationInfo)) || !Input.location.isEnabledByUser || Input.location.status == LocationServiceStatus.Failed)
            {
                throw new LocationException("MobiledgeX: Location Service disabled by user.");
            }
            Logger.Log("Location Info: [" + locationInfo.longitude + "," + locationInfo.latitude + "]");
            return ConvertUnityLocationToDMELoc(locationInfo);
        }

        public static Timestamp ConvertTimestamp(double timeInSeconds)
        {
            Timestamp ts;
            int nanos;
            long sec = (long)(timeInSeconds); // Truncate.
            double remainder = timeInSeconds - (double)sec;
            nanos = (int)(remainder * 1e9);
            ts = new Timestamp { seconds = sec.ToString(), nanos = nanos };
            return ts;
        }

        public static Loc ConvertUnityLocationToDMELoc(LocationInfo info)
        {
            Timestamp ts = ConvertTimestamp(info.timestamp);
            Loc loc = new Loc
            {
                latitude = info.latitude,
                longitude = info.longitude,
                horizontal_accuracy = info.horizontalAccuracy,
                vertical_accuracy = info.verticalAccuracy,
                altitude = info.altitude,
                course = 0,
                speed = 0,
                timestamp = ts
            };
            return loc;
        }
    }

    public class LocationException : Exception
    {
        public LocationException()
        {
        }

        public LocationException(string message) : base(message)
        {
        }
    }
}
