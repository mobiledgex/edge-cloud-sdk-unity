/**
 * Copyright 2018-2020 MobiledgeX, Inc. All rights and licenses reserved.
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

    // Unity Location Service, based on the documentation example:
    public class LocationService : MonoBehaviour
    {
        private static bool locationPermissionRejected = false;

        void Awake()
        {
            StartCoroutine(LocationServiceFlow());
        }

        public static IEnumerator InitalizeLocationService(int maxWait = 20, bool continuousLocationService = false)
        {
            // First, check if user has location service enabled
            if (!Input.location.isEnabledByUser)
            {

#if UNITY_IOS
                //if isEnabledByUser is false and you start location updates anyway,
                //the CoreLocation framework prompts the user with a confirmation panel
                //asking whether location services should be reenabled.
                //The user can enable or disable location services altogether from the Settings application
                //by toggling the switch in Settings>General>LocationServices.
                //https://docs.unity3d.com/ScriptReference/LocationService-isEnabledByUser.html
#elif UNITY_EDITOR
                Debug.LogWarning("MobiledgeX: Location Service disabled in UnityEditor");
#else
                throw new Exception("MobiledgeX: Location Service disabled by user."); // 
#endif
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
                throw new Exception("MobiledgeX: InitalizingLocationService coroutine Timed out");
            }

            // Connection has failed
            if (Input.location.status == LocationServiceStatus.Failed)
            {
                throw new Exception("MobiledgeX: Location Service is unable to determine device location");
            }
            else
            {
                if (Input.location.lastData.latitude == 0 && Input.location.lastData.longitude == 0)
                {
#if UNITY_EDITOR
                    Debug.LogWarning("MobiledgeX: Location Service disabled in UnityEditor");
#else
                    throw new Exception("MobiledgeX: Location Service disabled by user.");
#endif
                }
                // Access granted and location value could be retrieved
                Debug.Log("MobiledgeX: Location Service has location: " + Input.location.lastData.latitude + " " + Input.location.lastData.longitude + " " + Input.location.lastData.altitude + " " + Input.location.lastData.horizontalAccuracy + " " + Input.location.lastData.timestamp);
            }

            // Stop service if there is no need to query location updates continuously
            if (!continuousLocationService)
            {
                Input.location.Stop();
            }
        }

        /// <summary>
        /// EnsureLocation Confirm that user location is valid, user location is essential for MobiledgeX services
        /// If Location permission is denied by User an exception will be thrown (for Android) in LocationServiceFlow()
        ///                                                                      (for iOS) in InitalizeLocationService()
        /// </summary>
        public static IEnumerator EnsureLocation()
        {
#if UNITY_EDITOR
            Debug.LogWarning("MobiledgeX: LocationService is disabled in UnityEditor.");
            yield return null;
#endif
            if (Input.location.status == LocationServiceStatus.Initializing)
            {
                yield return new WaitUntil(() => (Input.location.status == LocationServiceStatus.Running));
            }
            else
            {
                yield return new WaitUntil(() => (Input.location.lastData.latitude != 0 && Input.location.lastData.longitude != 0) || locationPermissionRejected == true);
            }
        }

        public IEnumerator LocationServiceFlow()
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                if (!Permission.HasUserAuthorizedPermission(Permission.FineLocation))
                {
                    Permission.RequestUserPermission(Permission.FineLocation);
                    yield return new WaitForEndOfFrame(); // Application Out of focus , waiting for user decision on Location Permission
                    if (!Permission.HasUserAuthorizedPermission(Permission.FineLocation))
                    {
                        locationPermissionRejected = true;
                        throw new Exception("MobiledgeX: Location Permission denied by user!");
                    }
                    else
                    {
                        yield return StartCoroutine(InitalizeLocationService());
                    }
                }
                else
                {
                    yield return StartCoroutine(InitalizeLocationService());
                }
            }
            else //iOS - Permission Request are enabled once the application request location info
            {
                yield return StartCoroutine(InitalizeLocationService());
            }
        }

        // Retrieve the lastest location, without restarting locationService.
        public static Loc RetrieveLocation()
        {
            LocationInfo locationInfo = Input.location.lastData;
            Debug.Log("Location Info: [" + locationInfo.longitude + "," + locationInfo.latitude + "]");
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
                course = 0f,
                speed = 0f,
                timestamp = ts
            };

            return loc;
        }
    }
}
