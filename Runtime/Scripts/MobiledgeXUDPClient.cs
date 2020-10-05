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
using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using System.Linq;
namespace MobiledgeX
{
    // MobiledgeXUDPClient is a UDP Client Implementation offered with MobiledgeX Unity Package
    // MobiledgeXUDPClient concurrency model supports the use of a single queue for
    // send, and another queue for recieve. MobiledgeXUDPClient here has 1 independent thread
    // per send or receive direction of communication.
    public class MobiledgeXUDPClient : IDisposable
    {
        private UdpClient udpClient;
        private string host;
        private int port;
        Thread receiveThread { get; set; }
        Thread sendThread { get; set; }
        static UTF8Encoding encoder;
        public ConcurrentQueue<byte[]> receiveQueue { get; }
        public BlockingCollection<ArraySegment<byte>> sendQueue { get; }
        public bool run = true;
        const int MAXPAYLOADSIZE = 508; // max payload size guaranteed to be deliverable (not guaranteed to be delivered)

        public MobiledgeXUDPClient(string hostName, int sendPort, int receivePort)
        {
            try
            {
                udpClient = new UdpClient(receivePort);
                udpClient.DontFragment = true;

            }
            catch (Exception e)
            {
                Debug.LogError("Failed to listen for UDP at port " + receivePort + ": " + e.Message);
                return;
            }

            host = Dns.GetHostAddresses(hostName)
                .First(ip => ip.AddressFamily == AddressFamily.InterNetwork
                || ip.AddressFamily == AddressFamily.InterNetworkV6).ToString();
            port = sendPort;
            encoder = new UTF8Encoding();
            receiveQueue = new ConcurrentQueue<byte[]>();
            receiveThread = new Thread(RunReceive);
            receiveThread.Start();
            sendQueue = new BlockingCollection<ArraySegment<byte>>();
            sendThread = new Thread(RunSend);
            sendThread.Start();
            Connect();
        }

        public void Connect()
        {
            run = true;
        }

        public void Send(string message)
        {

            byte[] buffer = encoder.GetBytes(message);
            if (buffer.Length > MAXPAYLOADSIZE)
            {
                Debug.LogError("Max UDP payload size is 508 bytes, trying slicing your message to suit the max payload size");
                return;
            }
            var sendBuf = new ArraySegment<byte>(buffer);
            sendQueue.Add(sendBuf);
        }

        public void Send(byte[] buffer)
        {
            if (buffer.Length > MAXPAYLOADSIZE)
            {
                Debug.LogError("Max UDP payload size is 508 bytes, trying slicing your buffer to suit the max payload size");
                return;
            }
            var sendBuf = new ArraySegment<byte>(buffer);
            sendQueue.Add(sendBuf);
        }

        public async void RunSend()
        {
            ArraySegment<byte> msg;
            Debug.Log("RunSend entered.");
            while (run)
            {
                while (!sendQueue.IsCompleted)
                {
                    msg = sendQueue.Take();
                    long count = sendQueue.Count;
                    Debug.Log("Dequeued this message to send: " + msg + ", queueSize: " + count);
                    IPEndPoint serverEndpoint = new IPEndPoint(IPAddress.Parse(host), port);
                    await udpClient.SendAsync(msg.Array, msg.Count, serverEndpoint);
                }
            }
        }

        public async void RunReceive()
        {
            while (run)
            {
                Debug.Log("Awaiting Receive...");
                UdpReceiveResult result = await udpClient.ReceiveAsync();
                if (result != null && result.Buffer.Length > 0)
                {
                    receiveQueue.Enqueue(result.Buffer);
                }
                else
                {
                    Task.Delay(50).Wait();
                }
            }
        }

        public void Dispose()
        {
            run = false;
            sendThread.Abort();
            receiveThread.Abort();
            udpClient.Close();
        }
    }
}