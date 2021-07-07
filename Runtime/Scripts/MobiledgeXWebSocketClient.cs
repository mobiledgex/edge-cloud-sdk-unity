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
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using UnityEngine;


namespace MobiledgeX
{
    // MobiledgeXWebSocketClient is a WebSocket Implementation offered with MobiledgeX Unity Package
    // To see examples of using MobiledgeXWebSocketClient check MobiledgeX unity sample apps
    // at ("https://github.com/mobiledgex/edge-cloud-sampleapps/tree/master/unity")
    // C#'s built in WebSockets concurrency model supports the use a single queue for
    // send, and another queue for recieve. MobiledgeXWebSocketClient here has 1 independent thread
    // per send or receive direction of communication.
    public class MobiledgeXWebSocketClient : IDisposable
    {
        // Life of MobiledgeXSocketClient:
        private ClientWebSocket ws = new ClientWebSocket();
        static UTF8Encoding encoder; // For websocket text message encoding.
        const ulong MAXREADSIZE = 1 * 1024 * 1024;
        public ConcurrentQueue<string> receiveQueue { get; }
        public ConcurrentQueue<byte[]> receiveQueueBinary { get; }
        public BlockingCollection<ArraySegment<byte>> sendQueue { get; }
        public BlockingCollection<ArraySegment<byte>> sendQueueBinary { get; }
        Thread receiveThread { get; set; }
        Thread sendThread { get; set; }
        Thread sendThreadBinary { get; set; }
        private bool run = true;
        public CancellationTokenSource tokenSource { get; set; }

        public MobiledgeXWebSocketClient()
        {
            tokenSource = new CancellationTokenSource();
            encoder = new UTF8Encoding();
            ws = new ClientWebSocket();
            receiveQueue = new ConcurrentQueue<string>();
            receiveQueueBinary = new ConcurrentQueue<byte[]>();
            receiveThread = new Thread(RunReceive);
            receiveThread.Start();
            sendQueue = new BlockingCollection<ArraySegment<byte>>();
            sendQueueBinary = new BlockingCollection<ArraySegment<byte>>();
            sendThread = new Thread(RunSend);
            sendThread.Start();
            sendThreadBinary = new Thread(RunSendBinary);
            sendThreadBinary.Start();
        }

        public bool isConnecting()
        {
            if (ws == null)
            {
                ws = new ClientWebSocket();
            }
            return ws.State == WebSocketState.Connecting;
        }

        public bool isOpen()
        {
            return ws.State == WebSocketState.Open;
        }

        public async Task Connect(Uri uri)
        {
            Logger.Log("WebSocket Connecting to: " + uri);
            await ws.ConnectAsync(uri, tokenSource.Token);
            while (ws.State == WebSocketState.Connecting)
            {
                Logger.Log("WebSocket Waiting to connect...");
                Task.Delay(50).Wait();
            }
            Logger.Log("WebSocket Connect status: " + ws.State);
            run = true;
        }

        /// <summary>
        /// For Sending Text Messages to the server (ex. JSON)
        /// </summary>
        /// <param name="message"></param>
        public void Send(string message)
        {
            byte[] buffer = encoder.GetBytes(message);
            Logger.Log("WebSocket Message to queue for send: " + buffer.Length + ", message: " + message);
            var sendBuf = new ArraySegment<byte>(buffer);
            sendQueue.Add(sendBuf);
        }

        /// <summary>
        /// For Sending Binary to the server
        /// </summary>
        /// <param name="binary"></param>
        public void Send(byte[] binary)
        {
            var sendBuf = new ArraySegment<byte>(binary);
            sendQueueBinary.Add(sendBuf);
        }

        /// <summary>
        /// RunSend is used in sendThread
        /// </summary>
        public async void RunSend()
        {
            ArraySegment<byte> msg;
            Logger.Log("WebSocket RunSend entered.");
            while (run)
            {
                while (!sendQueue.IsCompleted)
                {
                    msg = sendQueue.Take();
                    long count = sendQueue.Count;
                    Logger.Log("WebSocket Client Dequeued this message to send: " + msg + ", queueSize: " + count);
                    await ws.SendAsync(msg, WebSocketMessageType.Text, true, tokenSource.Token);
                }
            }
        }

        /// <summary>
        /// RunSendBinary is used in sendThreadBinary
        /// </summary>
        public async void RunSendBinary()
        {
            ArraySegment<byte> msg;
            Logger.Log("WebSocket RunSend entered.");
            while (run)
            {
                while (!sendQueueBinary.IsCompleted)
                {
                    msg = sendQueueBinary.Take();
                    long count = sendQueueBinary.Count;
                    Logger.Log("WebSocket Client Dequeued this message to send: " + msg + ", queueSize: " + count);
                    await ws.SendAsync(msg, WebSocketMessageType.Binary, true, tokenSource.Token);
                }
            }
        }

        //This belongs in a background thread posting queued results for the UI thread to pick up.
        public async Task<Dictionary<WebSocketMessageType, MemoryStream>> Receive(ulong maxSize = MAXREADSIZE)
        {
            // A read buffer, and a memory stream to stuff unknown number of chunks into:
            byte[] buf = new byte[4 * 1024];
            var ms = new MemoryStream();
            ArraySegment<byte> arrayBuf = new ArraySegment<byte>(buf);
            WebSocketReceiveResult chunkResult = null;
            if (ws.State == WebSocketState.Open)
            {
                do
                {
                    chunkResult = await ws.ReceiveAsync(arrayBuf, tokenSource.Token);
                    ms.Write(arrayBuf.Array, arrayBuf.Offset, chunkResult.Count);
                    Logger.Log("Size of WebSocket Chunk message: " + chunkResult.Count);
                    if ((UInt64)(chunkResult.Count) > MAXREADSIZE)
                    {
                        Debug.LogError("MobiledgeX: WebSocket Message is bigger than expected!");
                    }
                } while (!chunkResult.EndOfMessage);
                
                ms.Seek(0, SeekOrigin.Begin);
                return new Dictionary<WebSocketMessageType, MemoryStream>(){ [chunkResult.MessageType]= ms };
            }

            return null;
        }

        /// <summary>
        /// RunReceive is used in receive thread
        /// RunReceive receives websocket messages asynchronously and add it to either receiveQueue or receiveQueueBinary ...
        /// dependent on the MessageType
        /// </summary>
        public async void RunReceive()
        {
            Logger.Log("WebSocket Message Receiver looping.");
            Dictionary<WebSocketMessageType, MemoryStream> response = new Dictionary<WebSocketMessageType, MemoryStream>(1);
            while (run)
            {
                response = await Receive();
                if (response != null && response.Keys.Count > 0)
                {
                    if (response.Keys.First() == WebSocketMessageType.Text)
                    {
                        string result = StreamToString(response.Values.First(), Encoding.UTF8);
                        if (result != null && result.Length > 0)
                        {
                            Logger.Log("WebSocket Message Received: " + result);
                            receiveQueue.Enqueue(result);
                        }
                    }
                    if (response.Keys.First() == WebSocketMessageType.Binary)
                    {
                        byte[] result = response.Values.First().GetBuffer();
                        if (result != null && result.Length > 0)
                        {
                            Logger.Log("WebSocket Message Received: " + result);
                            receiveQueueBinary.Enqueue(result);
                        }
                    }
                }
                else
                {
                    Task.Delay(50).Wait();
                }
            }
        }

        static string StreamToString(MemoryStream ms, Encoding encoding)
        {
            string readString = "";
            if (encoding == Encoding.UTF8)
            {
                using (var reader = new StreamReader(ms, encoding))
                {
                    readString = reader.ReadToEnd();
                }
            }
            return readString;
        }

        public void Dispose()
        {
            run = false;
            if(ws != null)
            {
                ws.Abort();
                ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Dispose", tokenSource.Token).ConfigureAwait(false).GetAwaiter().GetResult();
                ws = null;
            }
        }
    }
}
