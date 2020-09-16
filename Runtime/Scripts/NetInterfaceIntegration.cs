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

    public string GetIPAddress(string sourceNetInterfaceName, AddressFamily addressfamily = AddressFamily.InterNetwork)
    {
      if (!NetworkInterface.GetIsNetworkAvailable())
      {
        return null;
      }

      NetworkInterface[] netInterfaces = GetInterfaces();

      string ipAddress = null;
      string ipAddressV4 = null;
      string ipAddressV6 = null;

      foreach (NetworkInterface iface in netInterfaces)
      {
        if (iface.Name.Equals(sourceNetInterfaceName))
        {
          IPInterfaceProperties ipifaceProperties = iface.GetIPProperties();
          foreach (UnicastIPAddressInformation ip in ipifaceProperties.UnicastAddresses)
          {
            if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
            {
              ipAddressV4 = ip.Address.ToString();
            }
            if (ip.Address.AddressFamily == AddressFamily.InterNetworkV6)
            {
              ipAddressV6 = ip.Address.ToString();
            }
          }

          if (addressfamily == AddressFamily.InterNetworkV6)
          {
            return ipAddressV6;
          }

          if (addressfamily == AddressFamily.InterNetwork)
          {
            return ipAddressV4;
          }
        }
      }
      return ipAddress;
    }

    private bool TestSameDefaultRoute(string iName)
    {
      using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
      {
        // Should not actually connect if UDP (connectionless)
        try
        {
          socket.Connect("65.52.63.145", 38002); // if using wifi.dme.mobiledgex.net, this causes an host not found error some devices.
          IPEndPoint defaultEndPoint = socket.LocalEndPoint as IPEndPoint;

          if (defaultEndPoint != null)
          {
            // Is default endpoint that of this interface?
            string ipv6 = GetIPAddress(iName, AddressFamily.InterNetworkV6);
            if (ipv6 != null && ipv6.Equals(defaultEndPoint.Address.ToString()))
            {
              return true;
            }
            string ipv4 = GetIPAddress(iName, AddressFamily.InterNetwork);
            if (ipv4 != null && ipv4.Equals(defaultEndPoint.Address.ToString()))
            {
              return true;
            }
          }
        }
        catch (SocketException se)
        {
          UnityEngine.Debug.Log("Exception trying to test endpoint: " + se.Message);
        }
      }
      return false;
    }

    public bool HasCellular()
    {
      NetworkInterface[] netInterfaces = GetInterfaces();

      foreach (NetworkInterface iface in netInterfaces)
      {
        string iName = iface.Name;
        if (networkInterfaceName.CELLULAR.IsMatch(iName))
        {
          if (TestSameDefaultRoute(iName))
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
          if (TestSameDefaultRoute(iName))
          {
            return true;
          }
        }
      }
      return false;
    }
  }
}
