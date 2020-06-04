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
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using UnityEngine;
using DistributedMatchEngine;

namespace MobiledgeX
{
  // C#'s built in WebSockets concurrency model supports the use a single queue for
  // send, and another queue for recieve. MobiledgeXSokcetClient here has 1 independent thread
  // per send or receive direction of communication.
  public class MobiledgeXSocketClient : IDisposable
  {
    // Life of MobiledgeXSocketClient:
    private static string proto = "ws";
    private static string host = "localhost";
    private static int port = 3000;
    private static string server = proto + "://" + host + ":" + port;
    public Uri uri = new Uri(server);
    private ClientWebSocket ws = new ClientWebSocket();
    static UTF8Encoding encoder; // For websocket text message encoding.
    const UInt64 MAXREADSIZE = 1 * 1024 * 1024;
    public ConcurrentQueue<String> receiveQueue { get; }
    public BlockingCollection<ArraySegment<byte>> sendQueue { get; }
    Thread receiveThread { get; set; }
    Thread sendThread { get; set; }
    private bool run = true;
    MobiledgeXIntegration integration;

    // For testing
    public MobiledgeXSocketClient()
    {
      encoder = new UTF8Encoding();
      ws = new ClientWebSocket();
    
      receiveQueue = new ConcurrentQueue<string>();
      receiveThread = new Thread(RunReceive);
      receiveThread.Start();
    
      sendQueue = new BlockingCollection<ArraySegment<byte>>();
      sendThread = new Thread(RunSend);
      sendThread.Start();
    }

    public MobiledgeXSocketClient(MobiledgeXIntegration integration)
    {
        encoder = new UTF8Encoding();
        ws = new ClientWebSocket();
        this.integration = integration;
        receiveQueue = new ConcurrentQueue<string>();
        receiveThread = new Thread(RunReceive);
        receiveThread.Start();
        sendQueue = new BlockingCollection<ArraySegment<byte>>();
        sendThread = new Thread(RunSend);
        sendThread.Start();
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

    private async Task<ClientWebSocket> MobiledgeXGetWebsocketConnection(string path)
    {
      // RegisterAndFindCloudlet
      bool registeredAndFoundCloudlet;
      try
      {
        registeredAndFoundCloudlet = await integration.RegisterAndFindCloudlet();  
      }
      catch (RegisterClientException rce)
	  {
        Debug.LogError("RegisterClientException: " + rce.Message + ". Make sure OrgName, AppName, and AppVers are correct.");
        return null;
	  }
      catch (FindCloudletException fce)
	  {
        Debug.LogError("FindCloudletException: " + fce.Message + ". Make sure you have an app instance deployed to your region and carrier network");
        return null;
	  }
      catch (DmeDnsException dde)
	  {
        Debug.LogError("Unable to connect to DME to make RegisterClient call. Exception: " + dde.Message + ". Make sure MobiledgeX supports your carrier.");
        return null;
	  }
      catch (NotImplementedException nie)
	  {
        Debug.LogError("NotImplementedException: " + nie.Message); // This should not occur, since the constructor supplies the Integration classes
        return null;
	  }
      if (!registeredAndFoundCloudlet)
	  {
        Debug.LogError("Unable to register and find cloudlet");
        return null;
	  }

      // GetAppPort
      AppPort appPort;
      try {
        appPort = integration.GetAppPort(LProto.L_PROTO_TCP);
      }
      catch (AppPortException ape)
	  {
        Debug.LogError("Unabled to get AppPort. AppPortException: " + ape.Message);
        return null;
	  }
      if (appPort == null)
	  {
        Debug.LogError("GetAppPort returned null");
        return null;
	  }

      // GetWebSocket
      ClientWebSocket clientWebSocket;
      try
	  {
        clientWebSocket = await integration.GetWebsocketConnection(path: path);
	  }
      catch (GetConnectionException gce)
	  {
        Debug.LogError("Unable to GetWebsocketConnection. GetConnectionException is " + gce.Message);
        return null;
	  }
      return clientWebSocket;
	}

    public async Task Connect(string path)
    {
      ClientWebSocket c = await MobiledgeXGetWebsocketConnection(path);
      if (c == null)
	  {
        Debug.LogError("Unable to get MobiledgeXSocketClient. Try connecting again");
        return;
	  }
      ws = c;
      run = true;
      Debug.Log("websocket state is " + ws.State);
    }

    public async Task Connect(Uri uri)
    {
      Debug.Log("Connecting to: " + uri);
      await ws.ConnectAsync(uri, CancellationToken.None);
      while (ws.State == WebSocketState.Connecting)
      {
        Debug.Log("Waiting to connect...");
        Task.Delay(50).Wait();
      }
      Debug.Log("Connect status: " + ws.State);
      run = true;
    }

    public void Send(string message)
    {
      byte[] buffer = encoder.GetBytes(message);
      //Debug.Log("Message to queue for send: " + buffer.Length + ", message: " + message);
      var sendBuf = new ArraySegment<byte>(buffer);

      sendQueue.Add(sendBuf);
    }

    public async void RunSend()
    {
      ArraySegment<byte> msg;
      Debug.Log("RunSend entered.");
      while (run)
      {
        while(!sendQueue.IsCompleted)
        {
          msg = sendQueue.Take();
          long count = sendQueue.Count;
          //Debug.Log("Dequeued this message to send: " + msg + ", queueSize: " + count);
          await ws.SendAsync(msg, WebSocketMessageType.Text, true /* is last part of message */, CancellationToken.None);
        }
      }
    }

    // This belongs in a background thread posting queued results for the UI thread to pick up.
    public async Task<string> Receive(UInt64 maxSize = MAXREADSIZE)
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
          chunkResult = await ws.ReceiveAsync(arrayBuf, CancellationToken.None);
          ms.Write(arrayBuf.Array, arrayBuf.Offset, chunkResult.Count);
          //Debug.Log("Size of Chunk message: " + chunkResult.Count);
          if ((UInt64)(chunkResult.Count) > MAXREADSIZE)
          {
            Console.Error.WriteLine("Warning: Message is bigger than expected!");
          }
        } while (!chunkResult.EndOfMessage);
        ms.Seek(0, SeekOrigin.Begin);

        // Looking for UTF-8 JSON type messages.
        if (chunkResult.MessageType == WebSocketMessageType.Text)
        {
          return StreamToString(ms, Encoding.UTF8);
        }

      }

      return "";
    }

    public async void RunReceive()
    {
      Debug.Log("WebSocket Message Receiver looping.");
      string result;
      while (run)
      {
        //Debug.Log("Awaiting Receive...");
        result = await Receive();
        if (result != null && result.Length > 0)
        {
          //Debug.Log("Received: " + result);
          receiveQueue.Enqueue(result);
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
      ws.Abort();
      CancellationTokenSource tokenSource = new CancellationTokenSource();
      CancellationToken token = tokenSource.Token;
      ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Dispose", token).ConfigureAwait(false).GetAwaiter().GetResult();
      ws = null;
    }
  }
}