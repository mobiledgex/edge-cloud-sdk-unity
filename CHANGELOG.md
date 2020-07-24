# Changelog
All notable changes to this package will be documented in this file.


## [2.1.2] - 2020-07-21

### Fix & Improvements.
- Added a Remove Function to the SDK Menu to make it easy to uninstall our SDK and upgrade to a newer version.
- For iOS Builds, added a fallback on to the wifi dme when on a roaming network. This is because iOS does not provide information about the roaming carrier network.
- Added an EnsureLocation function to LocationServices. If you are using Location Services, please wait on this function for your app to get valid GPS location data from your device. 
- Renamed MobiledgeXSocketClient to MobiledgeXWebsocketClient 
- MobiledgeXWebsocketClient now sends Binary & Text

### MobiledgeX Settings 
- Added an optional authentication token field to the MobiledgeXSettings asset. 
