# Changelog
All notable changes to this package will be documented in this file.

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
