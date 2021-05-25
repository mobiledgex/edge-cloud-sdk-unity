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
        //RegisterClientException is thrown if your app is not found or if your carrier is not registered on MobiledgeX yet
        catch (RegisterClientException rce)
        {
            Debug.Log("RegisterClientException: " + rce.Message + "Inner Exception: " + rce.InnerException);
            mxi.UseWifiOnly(true); // use location only to find the app instance
            await mxi.RegisterAndFindCloudlet();
        }
        //FindCloudletException is thrown if there is no app instance in the user region
        catch (FindCloudletException fce)
        {
            Debug.Log("FindCloudletException: " + fce.Message + "Inner Exception: " + fce.InnerException);
            // your fallback logic here
        }
        // LocationException is thrown if the app user rejected location permission
        catch (LocationException locException)
        {
            print("Location Exception: " + locException.Message);
            mxi.useFallbackLocation = true;
            mxi.SetFallbackLocation(-122.4194, 37.7749); //Example only (SF location),In Production you can optionally use:  MobiledgeXIntegration.LocationFromIPAddress location = await MobiledgeXIntegration.GetLocationFromIP();
            await mxi.RegisterAndFindCloudlet();
        }
        mxi.GetAppPort(LProto.L_PROTO_TCP); // or LProto.L_PROTO_UDP
        string url = mxi.GetUrl("http"); // or another L7 proto such as https, ws, wss, udp

        Debug.Log("url : " + url); // Once you have your edge server url you can start communicating with your Edge server deployed on MobiledgeX Console
        StartCoroutine(RestExample(url)); //using UnityWebRequest
        //await RestExampleHttpClient(url); // You can instead use HttpClient
    }

    IEnumerator RestExample(string url)
    {
        UnityWebRequest www = UnityWebRequest.Get(url);
        yield return www.SendWebRequest();

        if (www.error != null)
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