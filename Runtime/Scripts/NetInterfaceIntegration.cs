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

using UnityEngine;
using DistributedMatchEngine;
using System.Net.NetworkInformation;
using System.Net.Sockets;

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
      Debug.Log("Looking for: " + sourceNetInterfaceName);

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

    public bool HasCellular()
    {
      NetworkInterface[] netInterfaces = GetInterfaces();
      bool foundByIp = false;

      foreach (NetworkInterface iface in netInterfaces)
      {
        foreach (string cellularName in networkInterfaceName.CELLULAR)
        {
          if (iface.Name.Equals(cellularName))
          {
            // unreliable.
            if (iface.OperationalStatus == OperationalStatus.Up)
            {
              return true;
            }
          }

          // First one with both IPv4 and IPv6 is a heuristic without NetTest. OperationStatus seems inaccurate or "unknown".
          if (GetIPAddress(cellularName, AddressFamily.InterNetwork) != null &&
              GetIPAddress(cellularName, AddressFamily.InterNetworkV6) != null)
          {
            foundByIp = true;
          }
          else if (GetIPAddress(cellularName, AddressFamily.InterNetworkV6) != null)
          {
            // No-op. Every interface has IpV6.
          }
          else if (GetIPAddress(cellularName, AddressFamily.InterNetwork) != null)
          {
            foundByIp = true;
          }
          if (foundByIp)
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
      bool foundByIp = false;

      foreach (NetworkInterface iface in netInterfaces)
      {
        foreach (string wifiName in networkInterfaceName.WIFI)
        {
          // unreliable.
          if (iface.Name.Equals(wifiName) && iface.OperationalStatus == OperationalStatus.Up)
          {
            return true;
          }

          // First one with both IPv4 and IPv6 is a heuristic without NetTest. OperationStatus seems inaccurate or "unknown".
          if (GetIPAddress(wifiName, AddressFamily.InterNetwork) != null &&
              GetIPAddress(wifiName, AddressFamily.InterNetworkV6) != null)
          {
            foundByIp = true;
          }
          else if (GetIPAddress(wifiName, AddressFamily.InterNetworkV6) != null)
          {
            // No-op. Every interface has IpV6.
          }
          else if (GetIPAddress(wifiName, AddressFamily.InterNetwork) != null)
          {
            foundByIp = true;
          }
          if (foundByIp)
          {
            return true;
          }
        }
      }
      return false;
    }
  }
}
