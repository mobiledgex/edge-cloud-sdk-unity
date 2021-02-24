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
            MobiledgeXIntegration mix = new MobiledgeXIntegration(new CarrierInfoClass(),null,new UniqueIDClass(), new TestDeviceInfo());
            mix.appName = appName;
            mix.appVers = appVers;
            mix.orgName = orgName;
            var task = Task.Run(async () =>
            {
                return await RegisterHelper(mix);
            });

            Debug.Log("result of registerClient is " + task.Result);
            Assert.True(task.Result);
        }

        [Test]
        [TestCase("MobiledgeX-Samples", "sdktest", "9.0")]
        public void FindCloudlet(string orgName, string appName, string appVers)
        {
            MobiledgeXIntegration mix = new MobiledgeXIntegration(new CarrierInfoClass(), null, new UniqueIDClass(), new TestDeviceInfo());
            mix.appName = appName;
            mix.appVers = appVers;
            mix.orgName = orgName;
            var task = Task.Run(async () =>
            {
                bool registered = await RegisterHelper(mix);
                Assert.True(registered, "Unable to register");
                return await FindCloudletHelper(mix);
            });

            Debug.Log("result of findCloudlet is " + task.Result);
            Assert.True(task.Result);
        }

        [Test]
        [TestCase("MobiledgeX-Samples", "sdktest", "9.0", "http", 8085)]
        [TestCase("MobiledgeX-Samples", "sdktest", "9.0", "https", 2015)]
        [TestCase("MobiledgeX-Samples", "sdktest", "9.0", "tcp", 2016)]
        [TestCase("MobiledgeX-Samples", "sdktest", "9.0", "ws", 3765)]
        [TestCase("MobiledgeX-Samples", "sdktest", "9.0", "udp", 2015)]
        public void GetUrl(string orgName, string appName, string appVers, string proto, int port)
        {
            MobiledgeXIntegration mix = new MobiledgeXIntegration(new CarrierInfoClass(), null, new UniqueIDClass(), new TestDeviceInfo());
            mix.appName = appName;
            mix.appVers = appVers;
            mix.orgName = orgName;
            var task = Task.Run(async () =>
            {
                return await GetUrlHelper(mix, proto, port);
            });
            Debug.Log(task.Result);
            Assert.IsNotEmpty(task.Result);
        }

        [Test]
        [TestCase("MobiledgeX-Samples", "sdktest", "9.0", "http", 8085)]
        [TestCase("MobiledgeX-Samples", "sdktest", "9.0", "https", 2015)]
        [TestCase("MobiledgeX-Samples", "sdktest", "9.0", "tcp", 2016)]
        [TestCase("MobiledgeX-Samples", "sdktest", "9.0", "ws", 3765)]
        [TestCase("MobiledgeX-Samples", "sdktest", "9.0", "udp", 2015)]
        public void GetHost(string orgName, string appName, string appVers, string proto, int port)
        {
            MobiledgeXIntegration mix = new MobiledgeXIntegration(new CarrierInfoClass(), null, new UniqueIDClass(), new TestDeviceInfo());
            mix.appName = appName;
            mix.appVers = appVers;
            mix.orgName = orgName;
            var task = Task.Run(async () =>
            {
                Debug.Log("In get host" + proto);
                return await GetHostHelper(mix, proto, port);
            });
            Debug.Log(task.Result);
            Assert.IsNotEmpty(task.Result);
        }

        [Test]
        [TestCase("WrongCredentials", "WrongAppName", "WrongAppVersion")]
        public void RegisterClientFaliure(string orgName, string appName, string appVers)
        {
            MobiledgeXIntegration mix = new MobiledgeXIntegration(new CarrierInfoClass(), null, new UniqueIDClass(), new TestDeviceInfo());
            mix.appName = appName;
            mix.appVers = appVers;
            mix.orgName = orgName;
            try
            {
                var task = Task.Run(async () =>
                {
                     await RegisterHelper(mix);
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

        [Test]
        [TestCase("MobiledgeX-Samples", "sdktest", "9.0", -44949, 29393)]
        public void FindCloudletFaliure(string orgName, string appName, string appVers , double longtiude, double latitude)
        {
            MobiledgeXIntegration mix = new MobiledgeXIntegration(new CarrierInfoClass(), null, new UniqueIDClass(), new TestDeviceInfo());
            mix.appName = appName;
            mix.appVers = appVers;
            mix.orgName = orgName;
            try
            {
                var task = Task.Run(async () =>
                {
                    mix.useFallbackLocation = true;
                    mix.SetFallbackLocation(longtiude, latitude);
                    await RegisterHelper(mix);
                    return  await FindCloudletHelper(mix);
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

        [Test]
        [TestCase("MobiledgeX-Samples", "sdktest", "9.0","ws", 3765, 20000)]
        public void WebSocketTest(string orgName, string appName, string appVers, string proto, int port, int timeOutMs)
        {
            MobiledgeXIntegration mix = new MobiledgeXIntegration(new CarrierInfoClass(), null, new UniqueIDClass(), new TestDeviceInfo());
            mix.appName = appName;
            mix.appVers = appVers;
            mix.orgName = orgName;
            var getUrlTask = Task.Run(async () =>
            {
               return await GetUrlHelper(mix, proto, port);
            });
            string url = getUrlTask.Result+ "/ws";
            MobiledgeXWebSocketClient wsClient = new MobiledgeXWebSocketClient();
            var sendWSMessage = Task.Run(async () =>
            {
                return await WebsocketMessageHelper(wsClient, url, "hello", timeOutMs);
            });
            Assert.True(sendWSMessage.Result== "olleh");
        }


        [Test]
        [TestCase("MobiledgeX-Samples", "sdktest", "9.0", 30000)]
        public void UDPTest(string orgName, string appName, string appVers, int timeOutMs)
        {
            MobiledgeXIntegration mix = new MobiledgeXIntegration(new CarrierInfoClass(), null, new UniqueIDClass(), new TestDeviceInfo());
            mix.appName = appName;
            mix.appVers = appVers;
            mix.orgName = orgName;
            var task = Task.Run(async () =>
            {
                string hostName= await GetHostHelper(mix, "udp");
                Debug.Log("hosdasdsa" + hostName);
                MobiledgeXUDPClient udpClient = new MobiledgeXUDPClient(hostName, mix.GetAppPort(LProto.L_PROTO_UDP).public_port);
                return UDPMessageHelper(udpClient, "ping", 20000);
            });
            Assert.True(task.Result == "pong");
        }

        #endregion

        #region HelperFunctions

        public async Task<bool> RegisterHelper(MobiledgeXIntegration mix)
        {
            bool check = await mix.Register();
            await Task.Delay(TimeSpan.FromMilliseconds(1000));
            return check;
        }

        public async Task<bool> FindCloudletHelper(MobiledgeXIntegration mix)
        {
            bool foundCloudlet = await mix.FindCloudlet();
            await Task.Delay(TimeSpan.FromMilliseconds(1000));
            return foundCloudlet;
        }

        public async Task<string> GetUrlHelper(MobiledgeXIntegration mix, string proto , int port = 0)
        {

            bool registerCheck = await RegisterHelper(mix);
            await Task.Delay(TimeSpan.FromMilliseconds(1000));
            if (!registerCheck)
            {
                throw new RegisterClientException("RegisterClient Failed");
            }

            bool findCloudletCheck = await FindCloudletHelper(mix);
            await Task.Delay(TimeSpan.FromMilliseconds(1000));
            if (!findCloudletCheck)
            {
                throw new FindCloudletException("FindCloudlet Failed");
            }

            AppPort appPort = proto switch
            {
                "udp" => mix.GetAppPort(LProto.L_PROTO_UDP, port),
                _ => mix.GetAppPort(LProto.L_PROTO_TCP, port),
            };

            if (appPort == null)
            {
                throw new AppPortException("AppPort Not Found");
            }

            string url = mix.GetUrl(proto, appPort);
            return url;
        }

        public async Task<string> GetHostHelper(MobiledgeXIntegration mix, string proto, int port = 0)
        {

            bool registerCheck = await RegisterHelper(mix);
            await Task.Delay(TimeSpan.FromMilliseconds(1000));
            if (!registerCheck)
            {
                throw new RegisterClientException("RegisterClient Failed");
            }
            bool findCloudletCheck = await FindCloudletHelper(mix);
            await Task.Delay(TimeSpan.FromMilliseconds(1000));
            if (!findCloudletCheck)
            {
                throw new FindCloudletException("FindCloudlet Failed");
            }

            AppPort appPort = proto switch
            {
                "udp" => mix.GetAppPort(LProto.L_PROTO_UDP, port),
                 _ => mix.GetAppPort(LProto.L_PROTO_TCP, port),
            };
            return mix.GetHost(appPort);
        }

        public async Task<string> WebsocketMessageHelper(MobiledgeXWebSocketClient mixWS, string url, string message, int timeOutMs)
        {
            await mixWS.Connect(new Uri(url));
            await Task.Delay(TimeSpan.FromMilliseconds(1000));
            mixWS.Send(message);
            string output;
            System.Diagnostics.Stopwatch stopWatch = new System.Diagnostics.Stopwatch();
            stopWatch.Start();
            while (mixWS.receiveQueue.Count == 0 && stopWatch.ElapsedMilliseconds < timeOutMs)
            {
                Debug.Log("Waiting for WebSocket Received messgae");
            }
            stopWatch = null;
            mixWS.receiveQueue.TryDequeue(out output);
            return output;
        }

        public string UDPMessageHelper(MobiledgeXUDPClient mixUDP, string message, int timeOutMs)
        {
            mixUDP.Send(message);
            System.Diagnostics.Stopwatch stopWatch = new System.Diagnostics.Stopwatch();
            stopWatch.Start();
            while (mixUDP.receiveQueue.Count == 0 && stopWatch.ElapsedMilliseconds < timeOutMs)
            {
                Debug.Log("Waiting for UDP Received messgae");
            }
            stopWatch = null;
            byte[] receivedBytes;
            mixUDP.receiveQueue.TryDequeue(out receivedBytes);
            return Encoding.ASCII.GetString(receivedBytes);
        }

        #endregion
    }
}
