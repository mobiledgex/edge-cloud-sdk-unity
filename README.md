![GitHub package.json version](https://img.shields.io/github/package-json/v/mobiledgex/edge-cloud-sdk-unity?style=plastic)
<a href="https://twitter.com/intent/follow?screen_name=mobiledgex">
<img alt="Twitter Follow" src="https://img.shields.io/twitter/follow/mobiledgex?style=social">
</a>
 <a href="https://discord.gg/k22WcfMFZ3">
<img src="https://img.shields.io/discord/779074183551385620?logo=discord" alt="chat on Discord">
</a>

# Unity SDK

This document explains how to download the Matching Engine Unity SDK and integrate it into your applications

The MobiledgeX Client Library enables an application to register and then locate the nearest edge cloudlet backend server for use. The client library also allows verification of a device's location for all location-specific tasks. Because these APIs involve networking, most functions will run asynchronously, and in a background thread.

The Matching Engine Unity C# SDK provides everything required to create applications for Unity devices.

## Prerequisites  

* Unity 2019.2 or newer, along with selected platforms (iOS, Android) for your project
* The SDK is compatible with (IL2CPP & .NET 2.0) , (IL2CPP & .NET 4.x) , (Mono & .NET 2.0) **but not compatible with (Mono  & .NET 4.x)**
* A running AppInst deployed on your edge server
* Git installed

## Download the Unity SDK Package  

### 2019.3.x and above

The fastest way to import the MobiledgeX Unity SDK into your project is by using the Package Manager. You can open it from *Window > Package Manager* in Unity. To add our MobiledgeX Package, select the **+** icon and click on **“Add package from git URL…”** 

![](https://developers.mobiledgex.com/assets/unity-sdk/add-git-url.png)

Enter [https://github.com/mobiledgex/edge-cloud-sdk-unity.git](https://github.com/mobiledgex/edge-cloud-sdk-unity.git) in the text field, which will automatically start the process of importing the package into your application. 

Once that completes, you will see the MobiledgeX SDK within your Package Manager and the SDK will be available under the Packages tab of your Project. 

![](https://developers.mobiledgex.com/assets/unity-sdk/mobiledgex-package.png)

### 2019.2.x

In order to import the MobiledgeX package into your project, you will need to edit the **manifest.json file**. This file is located at ***Unity_Project_Path/Packages/manifest.json***. When opened, the file will be in this format : 

```
{
  "dependencies": {
    "com.unity.*": "*.*.*",
    .
    .
    .
   }
}
```

Under dependencies, add the following : ```"com.mobiledgex.sdk": "https://github.com/mobiledgex/edge-cloud-sdk-unity.git"```
When you do, your manifest.json file should look like this (**minor** : do **NOT** include the comma if you add the mobiledgex line to the end of the dependency list):
```
{
  "dependencies": {
    "com.mobiledgex.sdk": "https://github.com/mobiledgex/edge-cloud-sdk-unity.git",
    "com.unity.*": "*.*.*",
    .
    .
    .
   }
}
```
After you finish editing and save the file, you can now click into the Unity editor and it will automatically begin the process of importing the package. 

## Using the MobiledgeX SDK

### Setup


Once you have successfully imported the Unity package, you will see a new tab as part of the Unity menu labeled **MobiledgeX**

![](https://developers.mobiledgex.com/assets/unity-sdk/mobiledgex-menu.png)

Click **Setup**, which will open a new Unity window asking you for your application's
* organization name
* app name
* app version number 

![](https://developers.mobiledgex.com/assets/unity-sdk/mobiledgex-unity-window.png)

After you provide your application credentials, click the setup button, which will communicate with the DME to verify that your application definition exists on the MobiledgeX console. If successful, your project will be set up with the correct plugins and resources necessary to use our APIs. You can verify if these files were generated correctly by looking in the Plugins and Resources folders of your project. 

![](https://developers.mobiledgex.com/assets/unity-sdk/generated-plugins.png)

![](https://developers.mobiledgex.com/assets/unity-sdk/generated-resources.png)

![](https://developers.mobiledgex.com/assets/unity-sdk/mobiledgex-settings.png)

**Important**: Make sure your Resources/MobiledgeXSettings.asset file has the correct information for your application. 


### Example Usage:
Once that setup has been completed, you can very easily call all the necessary API requests to connect to a cloudlet with your application deployed. Here is some example code using the MobiledgeXIntegration class that comes with the package 


**Getting Edge Connection Url**

MobiledgeX SDK uses the device Location and [the device's MCC-MNC ID (if available)](https://developers.mobiledgex.com/sdks/overview#distributed-matching-engine) to connect you to the closest Edge cloudlet where your application instance is deployed.

If your carrier is not supported yet by MobiledgeX the SDK will throw a RegisterClient Exception. You can catch this exception and instead use WifiOnly(true) to connect to [the wifi dme](https://developers.mobiledgex.com/sdks/overview#distributed-matching-engine) which will connect you to the closest [regional DME](https://developers.mobiledgex.com/sdks/overview#distributed-matching-engine).

```csharp
using MobiledgeX;
using DistributedMatchEngine;
using UnityEngine;
using System.Collections;

[RequireComponent(typeof(MobiledgeX.LocationService))]
public class YourClassName : MonoBehaviour
{ 
    IEnumerator Start()
    {
        yield return StartCoroutine(MobiledgeX.LocationService.EnsureLocation()); // Location is needed to connect you to the closet edge
        GetEdgeConnection();
    }
    
    async void GetEdgeConnection()
    {
        MobiledgeXIntegration mxi = new MobiledgeXIntegration();
        // you can use new MobiledgeXIntegration("orgName","appName","appVers");
        try
        {
            await mxi.RegisterAndFindCloudlet();
        }
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
        
        mxi.GetAppPort(LProto.L_PROTO_TCP); // Get the port of the desired protocol
        string url = mxi.GetUrl("http"); // Get the url of the desired protocol
    }
    
}
```

If your device doesn't have MCC-MNC ID (no sim card - for ex. Oculus device), Please use UseWifiOnly before RegisterAndFindCloudlet.

```csharp
use mxi.UseWifiOnly(true); 
await mxi.RegisterAndFindCloudlet();
```





**In UnityEditor** 

While developing in Unity Editor (Location is not used), The fallback location by default is San Jose, CA.

If you wish to change the fallback Location, use SetFallbackLocation() before you call RegisterAndFindCloudlet().

```csharp
 mxi.SetFallbackLocation(testLongtiude, testLatitude); 
 await mxi.RegisterAndFindCloudlet();
```

By default in Unity Editor you will connect with the Wifi DME, which is specified using the TestCarrierInfoClass in the CarrierInfoIntegration script.




**Communicating with your Edge Server using REST**


For full example code, Please check [RunTime/Scripts/ExampleRest.cs](https://github.com/mobiledgex/edge-cloud-sdk-unity/blob/master/Runtime/Scripts/ExampleRest.cs)

```csharp
 async void GetEdgeConnection()
    {
        MobiledgeXIntegration mxi = new MobiledgeXIntegration();
        await mxi.RegisterAndFindCloudlet();
        
        mxi.GetAppPort(LProto.L_PROTO_TCP); // Get the port of the desired protocol
        string url = mxi.GetUrl("http"); // Get the url of the desired protocol
        StartCoroutine(RestExample(url)); // using UnityWebRequest
        RestExampleHttpClient(url); // using HttpClient
        
    }
 // using UnityWebRequest
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
    
    // using HttpClient
    async Task<HttpResponseMessage> RestExampleHttpClient(string url)
    {
        HttpClient httpClient = new HttpClient();
        httpClient.BaseAddress = new Uri(url);
        return await httpClient.GetAsync("?q=x"); //makes a get request, "?q=x" is a parameter example 
    }
    
```


**Communicating with your Edge Server using WebSockets**

MobiledgeX Unity Package comes with  WebSocket Implementation (MobiledgeXWebSocketClient).
 
 For Using MobiledgeXWebSocketClient:
 1. Start the WebSocket
 2. Handle received messages from your Edge server.
 3. Send messages. (Text or Binary)
 
 For full example code, Please check [RunTime/Scripts/ExampleWebSocket.cs](https://github.com/mobiledgex/edge-cloud-sdk-unity/blob/master/Runtime/Scripts/ExampleWebSocket.cs)
 
 
 
 ```csharp
     async void GetEdgeConnection()
        {
            mxi = new MobiledgeXIntegration();
            await mxi.RegisterAndFindCloudlet();
            mxi.GetAppPort(LProto.L_PROTO_TCP);
            string url = mxi.GetUrl("ws");
            await StartWebSocket(url);
            wsClient.Send("WebSocketMsg");// You can send  Text or Binary messages to the WebSocket Server 
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
     
     // Handle received messages from your Edge server
     // Using MonoBehaviour callback Update to dequeue Received WebSocket Messages every frame (if there is any)
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
 
 ```
 **Communicating with your Edge Server using UDP**

 MobiledgeX Unity Package comes with  UDP Client Implementation (MobiledgeXUDPClient).
  
  For Using MobiledgeXUDPClient :
  1. Start the UDP Connection
  2. Handle received messages from your Edge server.
  3. Send messages. (Text or Binary)
  
  For full example code, Please check [RunTime/Scripts/ExampleUDP.cs](https://github.com/mobiledgex/edge-cloud-sdk-unity/blob/master/Runtime/Scripts/ExampleUDP.cs)
  
  
  
  ```csharp
      async void GetEdgeConnection()
         {
             mxi = new MobiledgeXIntegration();
             await mxi.RegisterAndFindCloudlet();
             // udpSendPort is the udp port exposed on your EdgeServer
             int udpSendPort = mxi.GetAppPort(LProto.L_PROTO_UDP).public_port;
             udpHost = mxi.GetHost();
             SendUDPMessage("Hi, From client to server", udpHost, udpSendPort);
         }
         
      void SendUDPMessage(string message, string udpHost, int udpSendPort)
         {
             udpClient = new MobiledgeXUDPClient(udpHost, udpSendPort);
             udpClient.Send(message);
                 
             //You can send binary also
             //byte[] messageBinary = Encoding.ASCII.GetBytes(message);
             //udpClient.Send(messageBinary);
         }
      
      // Handle received messages from your Edge server
      // Using MonoBehaviour callback Update to dequeue Received UDP Messages every frame (if there is any)
      void Update()
          {
              if (udpClient == null)
              {
                  return;
              }
              //udp receive queue
              byte[] udpMsg;
              var udp_queue = udpClient.receiveQueue;
              while (udp_queue.TryPeek(out udpMsg))
              {
                  udp_queue.TryDequeue(out udpMsg);
                  string udpReceivedMsg = Encoding.UTF8.GetString(udpMsg);
                  print("Received UDP Message : " + udpReceivedMsg);
              }
          }
  ```

## Location
MobiledgeX SDK uses a combination of device Location  & MCC-MNC code to connect you to the closet Edge data center where your backend is deployed.

The SDK comes with an easy to integrate Location Service Solution (LocationService.cs) that asks for the user permission and access the user GPS location, LocationService.cs must be added to the Scene in order for the SDK to automatically ask for Location Permission and use the user location.

You can find LocationService in the Unity Editor Inspector.
Select AddComponent then select (MobiledgeX/LocationService)
![](https://developers.mobiledgex.com/assets/unity-sdk/mobiledgex-unity-location-service.png)

If the user rejects Location permission, Location Exception will be thrown. Check ExampleRest.cs for handling location exception example.

Different way to get the device's location :

```csharp
   MobiledgeXIntegration mxi = new MobiledgeXIntegration();
   mxi.SetFallbackLocation(longtiude, latitude);
   // Fallback location is used by default in Unity Editor
   // To enable location overloading outside the Editor
   mxi.useFallbackLocation = true; 
```
## Platform Specific

### Android

The minimum API we support for Android is API Version 24. In your player settings, make sure to set the minimum API to 24, otherwise you will be unable to build your project. 

![](https://developers.mobiledgex.com/assets/unity-sdk/android_version_error.png)

## Known Issues

If you recieve the following error and cannot compile your Unity project, restart Unity.

![](https://developers.mobiledgex.com/assets/unity-sdk/metadata_error.png)


### Where to Go from Here  
* Click [here](https://mobiledgex.github.io/unity-samples/) to view and familiarize with the Unity C# SDK APIs to start your MobiledgeX integration.

* To learn how to use Docker to upload your application server, see this [tutorial](https://developers.mobiledgex.com/deployments/application-deployment-guides/hello-world).

* For sample Unity code, please refer to our [Ping Pong tutorial](https://developers.mobiledgex.com/sdks/unity-sdk/unity-sdk-sample).
