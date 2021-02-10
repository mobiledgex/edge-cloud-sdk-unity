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

using DistributedMatchEngine;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Net;
using UnityEngine;

namespace MobiledgeX
{
  // A generic network interface for most systems, with an interface names parameter.
  // The following is to allow Get{TCP, TLS, UDP}Connection APIs to return the configured
  // edge network path to your MobiledgeX AppInsts. Other connections will use the system
  // default network route. (NetInterfaceClass is used for MacOS and Linux)
  public class NetInterfaceClass : NetInterface
  {
    NetworkInterfaceName networkInterfaceName;

    public NetInterfaceClass(NetworkInterfaceName networkInterfaceName)
    {
      SetNetworkInterfaceName(networkInterfaceName);
    }

    public NetworkInterfaceName GetNetworkInterfaceName()
    {
      return networkInterfaceName;
    }

    public void SetNetworkInterfaceName(NetworkInterfaceName networkInterfaceName)
    {
      this.networkInterfaceName = networkInterfaceName;
    }

    private NetworkInterface[] GetInterfaces()
    {
      return NetworkInterface.GetAllNetworkInterfaces();
    }

    private IPEndPoint GetDefaultLocalEndPoint()
    {
      IPEndPoint defaultEndPoint = null;
      using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
      {
        // Should not actually connect if UDP (connectionless)
        try
        {
          socket.Connect("65.52.63.145", 38002); // if using wifi.dme.mobiledgex.net, this causes an host not found error some devices.
          defaultEndPoint = socket.LocalEndPoint as IPEndPoint;
        }
        catch (SocketException se)
        {
          Logger.Log("Exception trying to test endpoint: " + se.Message);
        }
      }
      return defaultEndPoint;
    }

    // In this implementation, it also checks if it's on the default network route.
    public string GetIPAddress(string sourceNetInterfaceName, AddressFamily addressfamily = AddressFamily.InterNetwork)
    {
      if (!NetworkInterface.GetIsNetworkAvailable())
      {
        return null;
      }

      NetworkInterface[] netInterfaces = GetInterfaces();
      IPEndPoint defaultLocalEndPoint = GetDefaultLocalEndPoint();

      string ipAddress = null;
      string ipAddressV4;
      string ipAddressV6;

      foreach (NetworkInterface iface in netInterfaces)
      {
        ipAddressV4 = null;
        ipAddressV6 = null;
        if (iface.Name.Equals(sourceNetInterfaceName))
        {
          IPInterfaceProperties ipifaceProperties = iface.GetIPProperties();
          foreach (UnicastIPAddressInformation ip in ipifaceProperties.UnicastAddresses)
          {
            string potentialIP = ip.Address.ToString();
            if (potentialIP.Equals(defaultLocalEndPoint.Address.ToString())) {
              // This interface is on the default network route.
              if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
              {
                ipAddressV4 = potentialIP;
              }
              if (ip.Address.AddressFamily == AddressFamily.InterNetworkV6)
              {
                ipAddressV6 = potentialIP;
              }
            }
          }

          if (addressfamily == AddressFamily.InterNetworkV6)
          {
            ipAddress = ipAddressV6;
          }
          else if (addressfamily == AddressFamily.InterNetwork)
          {
            ipAddress = ipAddressV4;
          }
        }
      }

      return ipAddress;
    }

    public bool HasCellular()
    {
      NetworkInterface[] netInterfaces = GetInterfaces();

      foreach (NetworkInterface iface in netInterfaces)
      {
        string iName = iface.Name;
        if (networkInterfaceName.CELLULAR.IsMatch(iName))
        {
          if (GetIPAddress(iName, AddressFamily.InterNetworkV6) != null ||
              GetIPAddress(iName, AddressFamily.InterNetwork) != null)
          {
            return true;
          }
        }
      }
      return false;
    }


    public bool HasWifi()
    {
      NetworkInterface[] netInterfaces = GetInterfaces();

      foreach (NetworkInterface iface in netInterfaces)
      {
        string iName = iface.Name;
        if (networkInterfaceName.WIFI.IsMatch(iName))
        {
          if (GetIPAddress(iName, AddressFamily.InterNetworkV6) != null ||
              GetIPAddress(iName, AddressFamily.InterNetwork) != null)
          {
            return true;
          }
        }
      }
      return false;
    }
  }
}
