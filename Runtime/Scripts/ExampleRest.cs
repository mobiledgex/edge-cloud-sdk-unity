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

using System.Collections;
using UnityEngine;
using UnityEngine.Networking; // for Unity Web Request

using MobiledgeX;
using DistributedMatchEngine;
using System.Threading.Tasks;
using System.Net.Http;
using System;
using System.Collections.Generic;

[RequireComponent(typeof(MobiledgeX.LocationService))]
    public class ExampleRest : MonoBehaviour
    {
        MobiledgeXIntegration mxi;

        IEnumerator Start()
        {
            yield return StartCoroutine(MobiledgeX.LocationService.EnsureLocation());
            GetEdgeConnection();
        }

        async void GetEdgeConnection()
        {
            mxi = new MobiledgeXIntegration();
            try
            {
                await mxi.RegisterAndFindCloudlet();
            }           
            catch (DmeDnsException dde)
            {
                Debug.Log("Dme dns Exception: " + dde.Message);
                mxi.UseWifiOnly(true);
                await mxi.RegisterAndFindCloudlet();
            }
            // LocationException is thrown if app user rejected location permission
            catch (LocationException locException)
            {
                print("Location Exception: " + locException.Message);
                mxi.useFallbackLocation = true;
                mxi.SetFallbackLocation(-122.4194, 37.7749); //Example only (SF location), You can use:  MobiledgeXIntegration.LocationFromIPAddress location = await MobiledgeXIntegration.GetLocationFromIP();
                await mxi.RegisterAndFindCloudlet();
            }
            mxi.GetAppPort(LProto.L_PROTO_TCP);
            string url = mxi.GetUrl("http");
            Debug.Log("Rest URL is : " + url); // Once you have your edge server url you can start communicating with your Edge server deployed on MobiledgeX Console
            StartCoroutine(RestExample(url)); //using UnityWebRequest
            //await RestExampleHttpClient(url); // You can instead use HttpClient
        }

        IEnumerator RestExample(string url)
        {
            UnityWebRequest www = UnityWebRequest.Get(url);
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.DataProcessingError || www.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.Log(www.error);
            }
            else
            {
                // Show results as text
                Debug.Log(www.downloadHandler.text);

                // Or retrieve results as binary data
                byte[] results = www.downloadHandler.data;
            }
        }

        async Task<HttpResponseMessage> RestExampleHttpClient(string url)
        {
            HttpClient httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri(url);
            return await httpClient.GetAsync("/"); //makes a get request
        }
    }

