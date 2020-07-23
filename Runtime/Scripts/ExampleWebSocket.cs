
using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

using MobiledgeX;
using DistributedMatchEngine;


    [RequireComponent(typeof(MobiledgeX.LocationService))]
    public class ExampleWebSocket : MonoBehaviour
    {
        MobiledgeXWebSocketClient wsClient;
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
            catch (DmeDnsException)
            {
                mxi.UseWifiOnly(true);
                await mxi.RegisterAndFindCloudlet();
            }
            
            mxi.GetAppPort(LProto.L_PROTO_TCP);
            string url = mxi.GetUrl("ws");
            Debug.Log("WebSocket URL is : " + url);
            await StartWebSocket(url);
            //wsClient.Send("WebSocketMsg");// You can send  Text or Binary messages to the WebSocket Server 
        }


        async Task StartWebSocket(string url)
        {
            wsClient = new MobiledgeXWebSocketClient();
            if (wsClient.isOpen())
            {
                wsClient.Dispose();
                wsClient = new MobiledgeXWebSocketClient();
            }

            Uri uri = new Uri(url);
            await wsClient.Connect(uri);
        }



        // Dequeue WebSocket Messages every frame (if there is any)
        private void Update()
        {
            if (wsClient == null)
            {
                return;
            }
            var cqueue = wsClient.receiveQueue;
            string msg;
            while (cqueue.TryPeek(out msg))
            {
                cqueue.TryDequeue(out msg);
                Debug.Log("WebSocket Received messgae : " + msg);
            }
        }

    }

