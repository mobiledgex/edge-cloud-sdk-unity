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

//
//  PlatformIntegration.m
//

#import <Foundation/Foundation.h>
#import <CoreTelephony/CTTelephonyNetworkInfo.h>
#import <CoreTelephony/CTCarrier.h>
#import <UIKit/UIKit.h>
#import <CoreLocation/CoreLocation.h>

#import <ifaddrs.h>
#import <sys/socket.h>
#import <netdb.h>
#import <sys/types.h>

// The subscriber callback is set, and notifies new subscriber info.
// Simple state object.
@interface NetworkState : NSObject
@property CTTelephonyNetworkInfo *networkInfo;
@property NSDictionary<NSString *,CTCarrier *>* ctCarriers;
@property CTCarrier* lastCarrier;
@end
@implementation NetworkState
@end

NetworkState *networkState = NULL;
NSString* isoCountryCode = NULL;

void _ensureMatchingEnginePlatformIntegration() {
    if (networkState == NULL)
    {
        networkState = [[NetworkState alloc] init];
        networkState.networkInfo = [[CTTelephonyNetworkInfo alloc] init];
        // Give it an initial value, if any.
        if (@available(iOS 12.1, *))
        {
            networkState.ctCarriers = [networkState.networkInfo serviceSubscriberCellularProviders];
        }
        else {
            networkState.ctCarriers = NULL;
        }
        networkState.lastCarrier = [networkState.networkInfo subscriberCellularProvider];

        if (@available(iOS 12.1, *))
        {
            networkState.networkInfo.serviceSubscriberCellularProvidersDidUpdateNotifier = ^(NSString *name) {
                networkState.ctCarriers = [networkState.networkInfo serviceSubscriberCellularProviders];
                if (networkState.ctCarriers != NULL)
                {
                    networkState.lastCarrier = networkState.ctCarriers[name];
                }
            };
        }
    }
}

char* convertToCStr(const char* str) {
    if (str == NULL) {
        return (char*)NULL;
    }

    char* out = (char*)malloc(strlen(str) + 1);
    strcpy(out, str);
    return out;
}

char* _getCurrentCarrierName()
{
    _ensureMatchingEnginePlatformIntegration();
    NSString* nsstr = @"";

    if (@available(iOS 12.1, *))
    {
        nsstr = [networkState.lastCarrier carrierName];
    }
    else
    {
        CTTelephonyNetworkInfo *netinfo = [[CTTelephonyNetworkInfo alloc] init];
        CTCarrier *carrier = [netinfo subscriberCellularProvider]; // s for dual SIM?
        NSLog(@"Carrier Name: %@", [carrier carrierName]);
        // Ref counted.

        nsstr = [carrier carrierName];
    }

    NSLog(@"Mobile CarrierName: %@", nsstr);
    return convertToCStr([nsstr UTF8String]);
}

// Atomically retrieves the last subscriber network carrier's MCCMNC as a "mccmnc" concatenated
// string combination.
char* _getMccMnc(NSString* name)
{
    _ensureMatchingEnginePlatformIntegration();
    NSMutableString* mccmnc = [NSMutableString stringWithString:@""];
    NSString* mcc;
    NSString* mnc;

    if (@available(iOS 12.1, *))
    {
        if (networkState.lastCarrier == NULL)
        {
            networkState.lastCarrier = [networkState.networkInfo subscriberCellularProvider];
        }
        mcc = [networkState.lastCarrier mobileCountryCode];
        mnc = [networkState.lastCarrier mobileNetworkCode];
    }
    else
    {
        CTTelephonyNetworkInfo *netinfo = [[CTTelephonyNetworkInfo alloc] init];
        CTCarrier *carrier = [netinfo subscriberCellularProvider];

        mcc = [carrier mobileCountryCode];
        mnc = [carrier mobileNetworkCode];
    }

    if (mcc == NULL || mnc == NULL)
    {
        return convertToCStr([@"" UTF8String]);
    }

    [mccmnc appendString: mcc];
    [mccmnc appendString: mnc];

    NSLog(@"Mobile Country Code and Mobile Network Code: %@", mccmnc);
    return convertToCStr([mccmnc UTF8String]);
}

// Gets the local IP address with specified network interface
// TODO: SUPPORT IPV6 (also in iOS)
char* _getIPAddress(char* netInterfaceType)
{
    _ensureMatchingEnginePlatformIntegration();
    char* ipAddress = (char*)malloc(sizeof(char)*INET6_ADDRSTRLEN);
    struct ifaddrs* interfaces = malloc(sizeof(struct ifaddrs));
    
    if (getifaddrs(&interfaces) == -1)
    {
        return convertToCStr([@"" UTF8String]);
    }
    struct ifaddrs* runner = interfaces;
    while (runner != NULL)
    {
        char* name = runner->ifa_name;
        if (strcmp(name, netInterfaceType) == 0)
        {
            struct sockaddr* sockaddr = runner->ifa_addr;
            char address[INET_ADDRSTRLEN]; // max: length of ipv4 address
            if (sockaddr->sa_family == AF_INET && getnameinfo(sockaddr, sockaddr->sa_len, address, sizeof(address), NULL, 0, NI_NUMERICHOST) == 0)
            {
                strcpy(ipAddress, address);
                break;
            }
        }
        runner = runner->ifa_next;
    }
    freeifaddrs(interfaces);
    return ipAddress;
}

bool _isWifi()
{
    struct ifaddrs* interfaces = NULL;
    
    if (getifaddrs(&interfaces) == -1)
    {
        return false;
    }
    
    struct ifaddrs* runner = interfaces;
    
    while (runner != NULL)
    {
        sa_family_t family = runner->ifa_addr->sa_family;
        if (family == AF_INET)
        {
            char* name = runner->ifa_name;
            NSString* interfaceName = [NSString stringWithFormat:@"%s", name];
            if ([interfaceName isEqualToString:@"en0"])
            {
                return true;
            }
        }
        runner = runner->ifa_next;
    }
    return false;
}

bool _isCellular()
{
    struct ifaddrs* interfaces = NULL;
    
    if (getifaddrs(&interfaces) == -1)
    {
        return false;
    }
    
    struct ifaddrs* runner = interfaces;
    
    while (runner != NULL)
    {
        sa_family_t family = runner->ifa_addr->sa_family;
        if (family == AF_INET)
        {
            char* name = runner->ifa_name;
            NSString* interfaceName = [NSString stringWithFormat:@"%s", name];
            if ([interfaceName isEqualToString:@"pdp_ip0"])
            {
                return true;
            }
        }
        runner = runner->ifa_next;
    }
    return false;
}

unsigned int _getCellID()
{
    return 0;
}

char* _getUniqueID()
{
    UIDevice* device = UIDevice.currentDevice;
    NSUUID *uuid = device.identifierForVendor;
    return convertToCStr([uuid.UUIDString UTF8String]);
}

char* _getUniqueIDType()
{
    UIDevice* device = UIDevice.currentDevice;
    NSString *aid = device.model;
    return convertToCStr([aid UTF8String]);
}

void _convertGPSToISOCountryCode(double longitude, double latitude)
{
    // reset isoCountryCode, so we don't get previous country code
    isoCountryCode = NULL;
    CLLocation *location = [[CLLocation alloc] initWithLatitude:latitude longitude:longitude];
    CLGeocoder *geocoder = [[CLGeocoder alloc] init];
    [geocoder reverseGeocodeLocation:location completionHandler:^(NSArray *placemarks, NSError
    *error)
     {
         if(placemarks && placemarks.count > 0)
         {
             CLPlacemark *placemark= [placemarks objectAtIndex:0];
             isoCountryCode = [placemark ISOcountryCode];
         }
     }];
}

char* _getISOCountryCodeFromGPS()
{
    NSString* capitalizedISOCC = [isoCountryCode uppercaseString];
    return convertToCStr([capitalizedISOCC UTF8String]);
}

char* _getISOCountryCodeFromCarrier()
{
    _ensureMatchingEnginePlatformIntegration();
    CTCarrier *carrier;

    if (@available(iOS 12.1, *))
    {
        carrier = networkState.lastCarrier;
    }
    else
    {
        CTTelephonyNetworkInfo *netinfo = [[CTTelephonyNetworkInfo alloc] init];
        carrier = [netinfo subscriberCellularProvider];
    }
    NSString* capitalizedISOCC = [carrier.isoCountryCode uppercaseString];
    return convertToCStr([capitalizedISOCC UTF8String]);
}

char* _getManufacturerCode() {
    return convertToCStr([@"Apple" UTF8String]);
}

char* _getDeviceSoftwareVersion() {
    UIDevice* device = UIDevice.currentDevice;
    return convertToCStr([device.systemVersion UTF8String]);
}

char* _getDeviceModel() {
    UIDevice* device = UIDevice.currentDevice;
    return convertToCStr([device.model UTF8String]);
}

char* _getOperatingSystem() {
    UIDevice* device = UIDevice.currentDevice;
    return convertToCStr([device.systemName UTF8String]);
}
