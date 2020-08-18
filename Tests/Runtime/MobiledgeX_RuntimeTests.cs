using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.IO;
using System.Threading.Tasks;
using System;
using System.Net.WebSockets;
using DistributedMatchEngine;
using System.Threading;
using System.Collections.Concurrent;
using System.Text;
namespace MobiledgeX
{
    public class MobiledgeX_RuntimeTests
    {

        #region Testing Setup & TearDown
        MobiledgeXSettings settings;
        MobiledgeXIntegration integration;
        Loc testLocation;

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
            else
            {
                integration = new MobiledgeXIntegration();
                testLocation = new Loc
                {
                    longitude = -121.8863286,
                    latitude = 37.3382082
                };
            }
        }

        [OneTimeTearDown]
        public void Clean()
        {
            integration = null;
            MobiledgeXIntegration.orgName = "";
            MobiledgeXIntegration.appName = "";
            MobiledgeXIntegration.appVers = "";
        }

        #endregion

        #region Run Time Tests

        [Test]
        [TestCase("MobiledgeX", "MobiledgeX SDK Demo", "2.0")]
        public void RegisterClient(string orgName, string appName, string appVers)
        {
            MobiledgeXIntegration.orgName = orgName;
            MobiledgeXIntegration.appName = appName;
            MobiledgeXIntegration.appVers = appVers;
            var task = Task.Run(async () =>
            {
                return await RegisterHelper();
            });

            Debug.Log("result of registerClient is " + task.Result);
            Assert.True(task.Result);
        }

        [Test]
        [TestCase("MobiledgeX", "MobiledgeX SDK Demo", "2.0")]
        public void FindCloudlet(string orgName, string appName, string appVers)
        {
            MobiledgeXIntegration.orgName = orgName;
            MobiledgeXIntegration.appName = appName;
            MobiledgeXIntegration.appVers = appVers;
            var task = Task.Run(async () =>
            {
                bool registered = await RegisterHelper();
                Assert.True(registered, "Unable to register");
                return await FindCloudletHelper();
            });

            Debug.Log("result of findCloudlet is " + task.Result);
            Assert.True(task.Result);
        }

        [Test]
        [TestCase("MobiledgeX", "MobiledgeX SDK Demo", "2.0")]
        public void GetRestUrl(string orgName, string appName, string appVers)
        {
            MobiledgeXIntegration.orgName = orgName;
            MobiledgeXIntegration.appName = appName;
            MobiledgeXIntegration.appVers = appVers;
            var task = Task.Run(async () =>
            {
                return await GetUrlHelper(orgName, appName, appVers, "http");
            });

            Assert.AreEqual(task.Result, "http://mobiledgexsdkdemo-tcp.mobiledgexmobiledgexsdkdemo20.sdkdemo-app-cluster.us-los-angeles.gcp.mobiledgex.net:8008"); 
        }

        [Test]
        [TestCase("MobiledgeX", "MobiledgeX SDK Demo", "2.0")]
        public void GetWSUrl(string orgName, string appName, string appVers)
        {
            MobiledgeXIntegration.orgName = orgName;
            MobiledgeXIntegration.appName = appName;
            MobiledgeXIntegration.appVers = appVers;
            var task = Task.Run(async () =>
            {
                return await GetUrlHelper(orgName, appName, appVers, "ws");
            });

            Assert.AreEqual(task.Result, "ws://mobiledgexsdkdemo-tcp.mobiledgexmobiledgexsdkdemo20.sdkdemo-app-cluster.us-los-angeles.gcp.mobiledgex.net:8008");
        }

        [Test]
        [TestCase("WrongCredentials", "WrongAppName", "WrongAppVersion")]
        public void GetRestURIException(string orgName, string appName, string appVers)
        {
            MobiledgeXIntegration.orgName = orgName;
            MobiledgeXIntegration.appName = appName;
            MobiledgeXIntegration.appVers = appVers;
            try
            {
                GetRestUrl(orgName, appName, appVers);
                Assert.True(false);
            }
            catch (AggregateException e)
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
        [TestCase("MobiledgeX", "PingPong", "2.0")]
        public void WebSocketTest(string orgName, string appName, string appVers)
        {
            MobiledgeXIntegration.orgName = orgName;
            MobiledgeXIntegration.appName = appName;
            MobiledgeXIntegration.appVers = appVers;
            var task = Task.Run(async () =>
            {
                return await WebSocketTestHelper(orgName, appName, appVers);
            });
            // websocket reply messsage printed in console (Register event and Notification event are being emitted on the server)
            Debug.Log(task.Result);
            Assert.IsNotEmpty(task.Result.ToString());
        }

        [Test]
        [TestCase("WrongCredentials", "WrongAppName", "WrongAppVersion")]
        public void WebSocketTestExpectedException(string orgName, string appName, string appVers)
        {
            MobiledgeXIntegration.orgName = orgName;
            MobiledgeXIntegration.appName = appName;
            MobiledgeXIntegration.appVers = appVers;
            try
            {
                WebSocketTest(orgName, appName, appVers);
                Assert.True(false);
            }
            catch (AggregateException e)
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
        #endregion

        #region HelperFunctions

        public async Task<bool> RegisterHelper()
        {
            bool check = await integration.Register();
            await Task.Delay(TimeSpan.FromMilliseconds(200));
            return check;
        }

        public async Task<bool> FindCloudletHelper()
        {
            bool foundCloudlet = await integration.FindCloudlet();
            await Task.Delay(TimeSpan.FromMilliseconds(200));
            return foundCloudlet;
        }

        public async Task<string> GetUrlHelper(string orgName, string appName, string appVers, string proto)
        {

            FindCloudletReply reply = await integration.matchingEngine.RegisterAndFindCloudlet(orgName, appName, appVers, testLocation, "");
            await Task.Delay(TimeSpan.FromMilliseconds(200));
            DistributedMatchEngine.AppPort appPort;

            switch (proto)
	    {
                case "http":
                     appPort = integration.GetAppPort(DistributedMatchEngine.LProto.L_PROTO_HTTP);
                    break;
                case "ws":
                     appPort = integration.GetAppPort(DistributedMatchEngine.LProto.L_PROTO_TCP);
                    break;
                default:
                    appPort = integration.GetAppPort(DistributedMatchEngine.LProto.L_PROTO_HTTP);
                    break;
            }
            string url = integration.GetUrl(proto, appPort);
            return url;
        }

        public async Task<string> WebSocketTestHelper(string orgName, string appName, string appVers)
        {
            FindCloudletReply reply = await integration.matchingEngine.RegisterAndFindCloudlet(orgName, appName, appVers, testLocation);
            await Task.Delay(TimeSpan.FromMilliseconds(200));
	    Dictionary<int, AppPort> appPortsDict = integration.matchingEngine.GetTCPAppPorts(reply);
	    int public_port = reply.ports[0].public_port;
	    AppPort appPort = appPortsDict[public_port];
	    ClientWebSocket ws = await integration.matchingEngine.GetWebsocketConnection(reply, appPort, public_port, 5000, "?roomid=testing");
	    await Task.Delay(TimeSpan.FromMilliseconds(200));
            byte[] buf = new byte[4 * 1024];
            ArraySegment<byte> arrayBuf = new ArraySegment<byte>(buf);
            var ms = new MemoryStream();
            WebSocketReceiveResult chunkResult = null;
            chunkResult = await ws.ReceiveAsync(arrayBuf, CancellationToken.None);
            await Task.Delay(TimeSpan.FromMilliseconds(200));
            ms.Write(arrayBuf.Array, arrayBuf.Offset, chunkResult.Count);
            ms.Seek(0, SeekOrigin.Begin);
            string resultString = "";
            if (chunkResult.MessageType == WebSocketMessageType.Text)
            {
                resultString = StreamToString(ms, Encoding.UTF8);
            }
            return resultString;
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

        #endregion
    }
}
