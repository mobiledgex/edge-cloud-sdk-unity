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

using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.IO;
using System.Threading.Tasks;
using System;
using DistributedMatchEngine;
using System.Text;

namespace MobiledgeX
{
    public class MobiledgeX_RuntimeTests
    {
        MobiledgeXIntegration mxi;
        #region Testing Setup

        [OneTimeSetUp]
        public void MobiledgeXEnvironmentSetup()
        {
            if (!File.Exists(Path.Combine(Application.dataPath, "Plugins/MobiledgeX/iOS/PlatformIntegration.m")) &&
            !File.Exists(Path.Combine(Application.dataPath, "Plugins/MobiledgeX/link.xml")) &&
            !File.Exists(Path.Combine(Application.dataPath, "Plugins/MobiledgeX/MatchingEngineSDKRestLibrary.dll")) &&
            !File.Exists(Path.Combine(Application.dataPath, "Resources/MobiledgeXSettings.asset")))
            {
                Assert.Fail("MobiledgeX Plugins are not loaded in the project, Can't preform tests");
            }
        }

        #endregion

        #region Run Time Tests

        [Test]
        [TestCase("MobiledgeX-Samples", "sdktest", "9.0")]
        public void RegisterClient(string orgName, string appName, string appVers)
        {
            MobiledgeXIntegration mxi = new MobiledgeXIntegration(new CarrierInfoClass(), null, new UniqueIDClass(), new TestDeviceInfo());
            using (mxi)
            {
                mxi.appName = appName;
                mxi.appVers = appVers;
                mxi.orgName = orgName;
                var task = Task.Run(async () =>
                {
                    return await RegisterHelper(mxi);
                });
                Assert.True(task.Result);
            }
        }

        [Test]
        [TestCase("MobiledgeX-Samples", "sdktest", "9.0")]
        public void FindCloudlet(string orgName, string appName, string appVers)
        {
            MobiledgeXIntegration mxi = new MobiledgeXIntegration(new CarrierInfoClass(), null, new UniqueIDClass(), new TestDeviceInfo());
            using (mxi)
            {
                mxi.appName = appName;
                mxi.appVers = appVers;
                mxi.orgName = orgName;
                var task = Task.Run(async () =>
                {
                    bool registered = await RegisterHelper(mxi);
                    Assert.True(registered, "Unable to register");
                    return await FindCloudletHelper(mxi);
                });
                Assert.True(task.Result);
            }
        }

        [Test]
        [TestCase("MobiledgeX-Samples", "sdktest", "9.0", "http", 8085)]
        [TestCase("MobiledgeX-Samples", "sdktest", "9.0", "https", 2015)]
        [TestCase("MobiledgeX-Samples", "sdktest", "9.0", "tcp", 2016)]
        [TestCase("MobiledgeX-Samples", "sdktest", "9.0", "ws", 3765)]
        [TestCase("MobiledgeX-Samples", "sdktest", "9.0", "udp", 2015)]
        public void GetUrl(string orgName, string appName, string appVers, string proto, int port)
        {
            MobiledgeXIntegration mxi = new MobiledgeXIntegration(new CarrierInfoClass(), null, new UniqueIDClass(), new TestDeviceInfo());
            using (mxi)
            {
                mxi.appName = appName;
                mxi.appVers = appVers;
                mxi.orgName = orgName;
                var task = Task.Run(async () =>
                {
                    return await GetUrlHelper(mxi, proto, port);
                });
                Debug.Log(task.Result);
                Assert.True(task.Result.Contains(proto));
                Assert.True(task.Result.Contains(port.ToString()));
            }
        }

        [Test]
        [TestCase("MobiledgeX-Samples", "sdktest", "9.0", "http", 8085)]
        [TestCase("MobiledgeX-Samples", "sdktest", "9.0", "https", 2015)]
        [TestCase("MobiledgeX-Samples", "sdktest", "9.0", "tcp", 2016)]
        [TestCase("MobiledgeX-Samples", "sdktest", "9.0", "ws", 3765)]
        [TestCase("MobiledgeX-Samples", "sdktest", "9.0", "udp", 2015)]
        public void GetHost(string orgName, string appName, string appVers, string proto, int port)
        {
            MobiledgeXIntegration mxi = new MobiledgeXIntegration(new CarrierInfoClass(), null, new UniqueIDClass(), new TestDeviceInfo());
            using (mxi)
            {
                mxi.appName = appName;
                mxi.appVers = appVers;
                mxi.orgName = orgName;
                var task = Task.Run(async () =>
                {
                    return await GetHostHelper(mxi, proto, port);
                });
                Debug.Log(task.Result);
                Assert.IsNotEmpty(task.Result);
            }
        }

        [Test]
        [TestCase("WrongCredentials", "WrongAppName", "WrongAppVersion")]
        public void RegisterClientFaliure(string orgName, string appName, string appVers)
        {
            MobiledgeXIntegration mxi = new MobiledgeXIntegration(new CarrierInfoClass(), null, new UniqueIDClass(), new TestDeviceInfo());
            using (mxi)
            {
                mxi.appName = appName;
                mxi.appVers = appVers;
                mxi.orgName = orgName;
                try
                {
                    var task = Task.Run(async () =>
                    {
                        await RegisterHelper(mxi);
                    });
                }
                catch (Exception e)
                {
                    if (e.GetBaseException().GetType() == typeof(HttpException))
                    {
                        Assert.True(true);
                    }
                    else
                    {
                        if (e.GetBaseException().GetType() != typeof(HttpException))
                        {
                            Assert.True(false);
                        }
                    }
                }
            }
        }

        [Test]
        [TestCase("MobiledgeX-Samples", "sdktest", "9.0", 104.1954, 35.8617)]
        public void FindCloudletFaliure(string orgName, string appName, string appVers, double latitude, double longitude)
        {
            MobiledgeXIntegration mxi = new MobiledgeXIntegration(new CarrierInfoClass(), null, new UniqueIDClass(), new TestDeviceInfo());
            using (mxi)
            {
                mxi.appName = appName;
                mxi.appVers = appVers;
                mxi.orgName = orgName;
                try
                {
                    var task = Task.Run(async () =>
                    {
                        mxi.useFallbackLocation = true;
                        mxi.SetFallbackLocation(longitude, latitude);
                        await RegisterHelper(mxi);
                        return await FindCloudletHelper(mxi);
                    });
                }
                catch (Exception e)
                {
                    if (e.GetBaseException().GetType() == typeof(FindCloudletException))
                    {
                        Assert.True(true);
                    }
                    else
                    {
                        if (e.GetBaseException().GetType() != typeof(HttpException))
                        {
                            Assert.True(false);
                        }
                    }
                }
            }
        }

        [Test]
        [TestCase("MobiledgeX-Samples", "sdktest", "9.0", "ws", 3765, 20000)]
        public void WebSocketTest(string orgName, string appName, string appVers, string proto, int port, int timeOutMs)
        {
            MobiledgeXIntegration mxi = new MobiledgeXIntegration(new CarrierInfoClass(), null, new UniqueIDClass(), new TestDeviceInfo());
            using (mxi)
            {
                mxi.appName = appName;
                mxi.appVers = appVers;
                mxi.orgName = orgName;
                var getUrlTask = Task.Run(async () =>
                {
                    return await GetUrlHelper(mxi, proto, port);
                });
                string url = getUrlTask.Result + "/ws";
                MobiledgeXWebSocketClient wsClient = new MobiledgeXWebSocketClient();
                var sendWSMessage = Task.Run(async () =>
                {
                    return await WebsocketMessageHelper(wsClient, url, "hello", timeOutMs);
                });
                Assert.True(sendWSMessage.Result == "olleh");
            }
        }


        [Test]
        [TestCase("MobiledgeX-Samples", "sdktest", "9.0", 20000)]
        public void UDPTest(string orgName, string appName, string appVers, int timeOutMs)
        {
            MobiledgeXIntegration mxi = new MobiledgeXIntegration(new CarrierInfoClass(), null, new UniqueIDClass(), new TestDeviceInfo());
            using (mxi)
            {
                mxi.appName = appName;
                mxi.appVers = appVers;
                mxi.orgName = orgName;
                var task = Task.Run(async () =>
                {
                    string hostName = await GetHostHelper(mxi, "udp");
                    MobiledgeXUDPClient udpClient = new MobiledgeXUDPClient(hostName, mxi.GetAppPort(LProto.L_PROTO_UDP).public_port);
                    return UDPMessageHelper(udpClient, "ping", timeOutMs);
                });
                Assert.True(task.Result == "pong");
            }
        }

        #endregion

        #region HelperFunctions

        public async Task<bool> RegisterHelper(MobiledgeXIntegration mxi)
        {
            bool check = await mxi.Register();
            await Task.Delay(TimeSpan.FromMilliseconds(1000));
            return check;
        }

        public async Task<bool> FindCloudletHelper(MobiledgeXIntegration mxi)
        {
            bool foundCloudlet = await mxi.FindCloudlet();
            await Task.Delay(TimeSpan.FromMilliseconds(1000));
            return foundCloudlet;
        }

        public async Task<string> GetUrlHelper(MobiledgeXIntegration mxi, string proto, int port = 0)
        {

            bool registerCheck = await RegisterHelper(mxi);
            await Task.Delay(TimeSpan.FromMilliseconds(1000));
            if (!registerCheck)
            {
                throw new RegisterClientException("RegisterClient Failed");
            }

            bool findCloudletCheck = await FindCloudletHelper(mxi);
            await Task.Delay(TimeSpan.FromMilliseconds(1000));
            if (!findCloudletCheck)
            {
                throw new FindCloudletException("FindCloudlet Failed");
            }

            AppPort appPort;
            switch(proto)
            {
                case "udp":
                    appPort = mxi.GetAppPort(LProto.L_PROTO_UDP, port);
                    break;
                case "ws":
                case "wss":
                case "http":
                case "https":
                default:
                    appPort = mxi.GetAppPort(LProto.L_PROTO_TCP, port);
                    break;
            }

            if (appPort == null)
            {
                throw new AppPortException("AppPort Not Found");
            }

            string url = mxi.GetUrl(proto, appPort);
            return url;
        }

        public async Task<string> GetHostHelper(MobiledgeXIntegration mxi, string proto, int port = 0)
        {

            bool registerCheck = await RegisterHelper(mxi);
            await Task.Delay(TimeSpan.FromMilliseconds(1000));
            if (!registerCheck)
            {
                throw new RegisterClientException("RegisterClient Failed");
            }
            bool findCloudletCheck = await FindCloudletHelper(mxi);
            await Task.Delay(TimeSpan.FromMilliseconds(1000));
            if (!findCloudletCheck)
            {
                throw new FindCloudletException("FindCloudlet Failed");
            }

            AppPort appPort;
            switch (proto)
            {
                case "udp":
                    appPort = mxi.GetAppPort(LProto.L_PROTO_UDP, port);
                    break;
                case "ws":
                case "wss":
                case "http":
                case "https":
                default:
                    appPort = mxi.GetAppPort(LProto.L_PROTO_TCP, port);
                    break;
            }
            return mxi.GetHost(appPort);
        }

        public async Task<string> WebsocketMessageHelper(MobiledgeXWebSocketClient mxiWS, string url, string message, int timeOutMs)
        {
            await mxiWS.Connect(new Uri(url));
            await Task.Delay(TimeSpan.FromMilliseconds(1000));
            mxiWS.Send(message);
            string output;
            System.Diagnostics.Stopwatch stopWatch = new System.Diagnostics.Stopwatch();
            stopWatch.Start();
            while (mxiWS.receiveQueue.Count == 0 && stopWatch.ElapsedMilliseconds < timeOutMs)
            {
                Debug.Log("Waiting for WebSocket Received messgae");
            }
            stopWatch = null;
            mxiWS.receiveQueue.TryDequeue(out output);
            return output;
        }

        public string UDPMessageHelper(MobiledgeXUDPClient mxiUDP, string message, int timeOutMs)
        {
            mxiUDP.Send(message);
            System.Diagnostics.Stopwatch stopWatch = new System.Diagnostics.Stopwatch();
            stopWatch.Start();
            while (mxiUDP.receiveQueue.Count == 0 && stopWatch.ElapsedMilliseconds < timeOutMs)
            {
                Debug.Log("Waiting for UDP Received messgae");
            }
            stopWatch = null;
            byte[] receivedBytes;
            mxiUDP.receiveQueue.TryDequeue(out receivedBytes);
            return Encoding.ASCII.GetString(receivedBytes);
        }

        #endregion
    }
}
