# Unity SDK

This document explains how to download the Matching Engine Unity SDK and integrate it into your applications

The MobiledgeX Client Library enables an application to register and then locate the nearest edge cloudlet backend server for use. The client library also allows verification of a device's location for all location-specific tasks. Because these APIs involve networking, most functions will run asynchronously, and in a background thread.

The Matching Engine Unity C# SDK provides everything required to create applications for Unity devices.

## Prerequisites  

* Unity 2019.2 or newer, along with selected platforms (iOS, Android) for your project
* A running AppInst deployed on your edge server
* Git installed

## Download the Unity SDK Package  

### 2019.3.x and above

The fastest way to import the MobiledgeX Unity SDK into your project is by using the Package Manager. You can open it from *Window > Package Manager* in Unity. To add our MobiledgeX Package, select the **+** icon and click on **“Add package from git URL…”** 

![](https://developers.mobiledgex.com/assets/unity-sdk/add-git-url.png)

Enter [https://github.com/mobiledgex/edge-cloud-sdk-unity.git](https://github.com/mobiledgex/edge-cloud-sdk-unity.git) in the text field, which will automatically start the process of importing the package into your application. 

Once that finishes, you will now see the MobiledgeX SDK within your Package Manager and the SDK will be available under the Packages tab of your Project. 

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

Once you have successfully imported the Unity package, you will see a new tab as part of the Unity menu labeled **MobiledgeX**

![](https://developers.mobiledgex.com/assets/unity-sdk/mobiledgex-menu.png)

Click on **Setup**, which will open up a new Unity window that will ask for your application's
* organization name
* app name
* app version number 

![](https://developers.mobiledgex.com/assets/unity-sdk/mobiledgex-unity-window.png)

After you input your application credentials, you can click the setup button, which will communicate with the DME to verify that your application definition exists on the MobiledgeX console. If successful, your project will be set up with the correct plugins and resources necessary to use our APIs. You can verify these files were generated correctly by looking in the Plugins and Resources folders of your project. 

![](https://developers.mobiledgex.com/assets/unity-sdk/generated-plugins.png)

![](https://developers.mobiledgex.com/assets/unity-sdk/generated-resources.png)

![](https://developers.mobiledgex.com/assets/unity-sdk/mobiledgex-settings.png)

**Important**: Make sure your Resources/MobiledgeXSettings.asset file has the correct information for your application. 

Once that setup has been completed, you can very easily call all the necessary API requests to connect to a cloudlet with your application deployed. Here is some example code using the MobiledgeXIntegration class that comes with the package 


**Getting Edge Connection Url**



```csharp
using MobiledgeX;
using DistributedMatchEngine;
using UnityEngine;
using System.Collections;

[RequireComponent(typeof(MobiledgeX.LocationService))]
public class NetworkManager : MonoBehaviour
{ 
    IEnumerator Start()
    {
        yield return StartCoroutine(MobiledgeX.LocationService.EnsureLocation()); // Location is needed to connect you to the closet edge
        GetEdgeConnection();
    }
     async void GetEdgeConnection()
    {
        MobiledgeXIntegration mxi = new MobiledgeXIntegration();
        try
        {
            await mxi.RegisterAndFindCloudlet();
        }
        catch(DmeDnsException)
        {
            mxi.UseWifiOnly(true); // if you carrier is not supported yet, WifiOnly will connect you to the closet MobiledgeX Public Cloud
            await mxi.RegisterAndFindCloudlet();
        }

        mxi.GetAppPort(LProto.L_PROTO_HTTP); // Get the port of the desired protocol
        string url = mxi.GetUrl("http"); // Get the url of the desired protocol
    }
    
}
```


MobiledgeX requires Location and Mobile phone carrier information (specifically MccMnc code)


If you carrier is not supported yet by MobiledgeX it will throw a DmeDnsException and it will connect you to the closet MobiledgeX Public Cloud 



**In UnityEditor** 



(Location is not used) there is fallback Location in [RunTime/Scripts/MobiledgeXIntegration.cs](https://github.com/mobiledgex/edge-cloud-sdk-unity/blob/master/Runtime/Scripts/MobiledgeXIntegration.cs) , You change it to your desired test location.

Since there is no phone carrier information in UnityEditor, by default you will be using WifiOnly mode which will connect you to the closet MobiledgeX Public Cloud to the fallback location.





**Communicating with your Edge Server using REST**


For full example code, Please check [RunTime/Scripts/ExampleRest.cs](https://github.com/mobiledgex/edge-cloud-sdk-unity/blob/master/Runtime/Scripts/ExampleRest.cs)

```csharp
 async void GetEdgeConnection()
    {
        MobiledgeXIntegration mxi = new MobiledgeXIntegration();
        await mxi.RegisterAndFindCloudlet();
        
        mxi.GetAppPort(LProto.L_PROTO_HTTP); // Get the port of the desired protocol
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
        return await httpClient.GetAsync("/"); //makes a get request
    }
    
```


**Communicating with your Edge Server using WebSockets**



 MobiledgeXWebSocketClient is a WebSocket Implementation offered with MobiledgeX Unity Package,
 Built in WebSockets concurrency model supports the use a single queue for
 send, and another queue for recieve.
 
 
 MobiledgeXWebSocketClient has 1 independent thread
 per send or receive direction of communication.
 
 For Using MobiledgeXWebSocket:
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



## Platform Specific

### Android

The minimum API we support for Android is API Version 24. In your player settings, make sure to set the minimum API to 24, otherwise you will be unable to build your project. 

![](https://developers.mobiledgex.com/assets/unity-sdk/android_version_error.png)

## Known Issues

If you recieve the following error and cannot compile your Unity project, restart Unity.

![](https://developers.mobiledgex.com/assets/unity-sdk/metadata_error.png)


### Where to Go from Here  
* Click [here](https://api.mobiledgex.net/#section/Edge-SDK-Unity) to view and familiarize with the Unity C# SDK APIs to start your MobiledgeX integration.

* To learn how to use Docker to upload your application server, see this [tutorial](https://developers.mobiledgex.com/guides-and-tutorials/hello-world).

* For sample Unity code, please refer to our [Ping Pong tutorial](https://developers.mobiledgex.com/guides-and-tutorials/how-to-workshop-adding-mobiledgex-matchingengine-sdk-to-unity-ping-pong-demo-app).
