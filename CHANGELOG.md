# Changelog
All notable changes to this package will be documented in this file.

## [3.0.0] - 2021-08-31

### Fix & Improvements.
- Call MobiledgeXIntegration.Dispose() to avoid memory leakage, see the examples for more details.
- LocationService updated with more robust behavior on Android and iOS.
- Added Support for non-cellular devices, see ExampleNonCellular.cs.

## [3.0.0] - 2021-08-31 (gRPC) 

### New
- New SDK version that leverages gRPC bidirectional streaming for sending and receiving EdgeEvents to ensure  you are always connected to the best server.
- Add EdgeEventsManager component to any active game object in your scene to ensure  you are always connected to the best server.
- MobiledgeXIntegration constructors now have an EdgeEventsManager parameter.
- MobiledgeXIntegration now have a new NewFindCloudletHandler action, Add your HandleFindCloudlet function to NewFindCloudletHandler, see the examples for more details.
- EdgeEvents works only on Android and iOS, gRPC bidirectional streaming is not supported in UnityEditor yet.

### Fix & Improvements.
- Call MobiledgeXIntegration.Dispose() to avoid memory leakage, see the examples for more details.
- LocationService updated with more robust behavior on Android and iOS.
### MobiledgeX Settings
- Edge Events configuration, your edge events config. will be used by EdgeEventsManager.cs to ensure you are always connected to the best server.


## [2.4.1] - 2021-03-02

### Fix & Improvements.
- Location Exception added, Location Exception is thrown if the user rejected location permission.
- Added support for Oculus Devices.
- New MobiledgeXIntegration Constructor added using your MobiledgeX App Definitions, you can connect to multiple apps from a single Unity project.
- Local Network permission on iOS is removed by default, this might degrade some edge features, to fix this set matchingEngine.EnableEnhancedLocationServices to true.
### MobiledgeX Settings
- log type is added to MobiledgeX Settings to switch between development and production.

## [2.4.0] - 2020-12-10

### Fix & Improvements.
- UDP Client added to the SDK (MobiledgeXUDPClient.cs), and  ExampleUDP.cs as an example of using  MobiledgeXUDPClient.
- In UnityEditor if fallback location is not defined, the location from the IP address will be used.
- Multiplayer Sample using Websockets & UDP (EdgeMultiplay), to be found under MobiledgeX Menu (MobiledgeX/Examples).
- MobiledgeX Editor Window (KR region removed - Links updated).
- ComputerVision Example Updated (CV Instances in EU & US - Readme added - LProto.HTTP removed).
- SDK Readme updated (Location section - How to communicate with your Edge server using MobiledgeXUDPClient).
- DeviceInfoIntegration added as part of the PlatformIntegration constructor.
- LProto.HTTP removed, use LProto.TCP instead.
- Join The Community added to MobiledgeX Menu links to "MobiledgeX Community" Discord server.
### MobiledgeX Settings
- region is added to MobiledgeXSettings.

## [2.1.3] - 2020-09-16

### Fix & Improvements.
- SDK Version is available in MobiledgeX Editor Window.
- MobiledgeX logo added to (LocationService.cs, ExampleRest.cs,ExampleWebSocket.cs)
- GetAppPort (LProto.UDP) fixed, returns the correct mapped UDP Port.
- You can use fallback location in production, if your device doesn't support Location Services use mobiledgeXIntegration.useFallBackLocation = true.
- ComputerVision Example added to the SDK.
- Optional region selection to connect to an app instance in a specific region (works in Unity Editor Only).


## [2.1.2] - 2020-07-21

### Fix & Improvements.
- Added a Remove Function to the SDK Menu to make it easy to uninstall our SDK and upgrade to a newer version.
- For iOS Builds, added a fallback on to the wifi dme when on a roaming network. This is because iOS does not provide information about the roaming carrier network.
- Added an EnsureLocation function to LocationServices. If you are using Location Services, please wait on this function for your app to get valid GPS location data from your device. 
- Renamed MobiledgeXSocketClient to MobiledgeXWebsocketClient 
- MobiledgeXWebsocketClient now sends Binary & Text
### MobiledgeX Settings 
- Added an optional authentication token field to the MobiledgeXSettings asset. 
