# Unity SDK

This document explains how to download the Matching Engine Unity SDK and integrate it into your applications

The MobiledgeX Client Library enables an application to register and then locate the nearest edge cloudlet backend server for use. The client library also allows verification of a device's location for all location-specific tasks. Because these APIs involve networking, most functions will run asynchronously, and in a background thread.

The Matching Engine Unity C# SDK provides everything required to create applications for Unity devices.


## Prerequisites  

* Unity 2018.2.x LTS or newer, along with selected platforms (iOS, Android) for your project
* .Net Standard 2.0
* A running AppInst deployed on your edge server

## Download the Unity SDK Package  

The fastest way to import the MobiledgeX Unity SDK into your project is by using the Package Manager. You can open it from *Window > Package Manager* in Unity. To add our MobiledgeX Package, select the **+** icon and click on **“Add package from git URL…”** 

![](https://developers.mobiledgex.com/assets/unity-sdk/add-git-url.png){.full}

Enter [https://github.com/mobiledgex/edge-cloud-sdk-unity.git](https://github.com/mobiledgex/edge-cloud-sdk-unity.git) in the text field, which will automatically start the process of importing the package into your application. 

Once that finishes, you will now see the MobiledgeX SDK within your Package Manager and the SDK will be available under the Packages tab of your Project. 

![](https://developers.mobiledgex.com/assets/unity-sdk/mobiledgex-package.png){.full}

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

Once that setup has been completed, you can very easily call all the necessary API requests to connect to a cloudlet with your application deployed. Here is some example code using the MobiledgeXIntegration class that comes with the package : 

```csharp
using MobiledgeX;

MobiledgeXIntegration integration = new MobiledgeXIntegration();
bool registered = await integration.Register(); //calls Register Client
DistributedMatchEngine.FindCloudletReply findCloudletReply = await integration.FindCloudlet(); //calls Find Cloudlet

MatchingEngine dme = integration.me;

Dictionary<int, AppPort> appPortsDict = dme.GetHTTPAppPorts(findCloudletReply);
int public_port = findCloudletReply.ports[0].public_port; // if you only have one port
AppPort appPort = appPortsDict[public_port];
HttpClient http = await dme.GetHTTPClient(findCloudletReply, appPort, public_port, 5000);
HttpResponseMessage message = await http.GetAsync("/"); //makes a get request
```

### Where to Go from Here  
* Click [here](https://api.mobiledgex.net/#section/Edge-SDK-Unity) to view and familiarize with the Unity C# SDK APIs to start your MobiledgeX integration.

* To learn how to use Docker to upload your application server, see this [tutorial](https://developers.mobiledgex.com/guides-and-tutorials/hello-world).

* For sample Unity code, please refer to our [Ping Pong tutorial](https://developers.mobiledgex.com/guides-and-tutorials/how-to-workshop-adding-mobiledgex-matchingengine-sdk-to-unity-ping-pong-demo-app).
