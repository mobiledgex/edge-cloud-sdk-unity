using System.Collections;
using UnityEngine;
using UnityEngine.Networking; // for Unity Web Request

using MobiledgeX;
using DistributedMatchEngine;
using System.Threading.Tasks;
using System.Net.Http;
using System;

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

            if (www.isHttpError || www.isNetworkError)
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

