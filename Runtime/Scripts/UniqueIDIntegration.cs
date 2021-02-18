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

using System;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using System.Runtime.InteropServices; //for importing IOS functions
using DistributedMatchEngine;

namespace MobiledgeX
{
  public static class HexUtil
  {
    static public string HexStringSha512(string data)
    {
      SHA512 shaM = new SHA512Managed();
      var hashedBytes = shaM.ComputeHash(Encoding.ASCII.GetBytes(data));
      StringBuilder sb = new StringBuilder();
      foreach (byte b in hashedBytes)
      {
        sb.AppendFormat("{0:x2}", b);
      }
      return sb.ToString();
    }
  }

  public class UniqueIDClass : UniqueID
  {

#if UNITY_ANDROID

    public string GetUniqueIDType()
    {
      return SystemInfo.deviceModel;
    }

    public String GetUniqueID()
    {
      AndroidJavaClass unityPlayer = PlatformIntegrationUtil.GetAndroidJavaClass("com.unity3d.player.UnityPlayer");
      if (unityPlayer == null)
      {
        Logger.Log("Can't get UnityPlayer");
        return null;
      }

      AndroidJavaObject activity = PlatformIntegrationUtil.GetStatic<AndroidJavaObject>(unityPlayer, "currentActivity");
      if (activity == null)
      {
        Logger.Log("Can't find an activity!");
        return null;
      }

      AndroidJavaObject context = PlatformIntegrationUtil.Call<AndroidJavaObject>(activity, "getApplicationContext");
      if (context == null)
      {
        Logger.Log("Can't find an app context!");
        return null;
      }

      AndroidJavaObject contentResolver = PlatformIntegrationUtil.Call<AndroidJavaObject>(context, "getContentResolver");
      if (contentResolver == null)
      {
        Logger.Log("Can't get content resolver from context");
        return null;
      }

      AndroidJavaClass secureClass = PlatformIntegrationUtil.GetAndroidJavaClass("android.provider.Settings$Secure");
      if (secureClass == null)
      {
        Logger.Log("Can't get secure class");
        return null;
      }

      AndroidJavaObject androidID = PlatformIntegrationUtil.GetStatic<AndroidJavaObject>(secureClass, "ANDROID_ID");
      if (androidID == null)
      {
        Logger.Log("Cant get Android ID static string");
        return null;
      }

      object[] parameters = new object[2];
      parameters[0] = contentResolver;
      parameters[1] = androidID;

      string aid = PlatformIntegrationUtil.CallStatic<string>(secureClass, "getString", parameters);

      if (aid != null) {
        string hashedAdId = HexUtil.HexStringSha512(aid);
        return hashedAdId;
      }
      return aid;
    }

#elif UNITY_IOS

    [DllImport("__Internal")]
    private static extern string _getUniqueIDType();

    [DllImport("__Internal")]
    private static extern string _getUniqueID();

    public string GetUniqueIDType()
    {
      string uniqueIDType = null;
      if (Application.platform == RuntimePlatform.IPhonePlayer)
      {
        uniqueIDType = _getUniqueIDType();
      }
      return uniqueIDType;
    }

    public string GetUniqueID()
    {
      // Permission Guard:
      if (!MatchingEngine.EnableEnhancedLocationServices)
      {
        return null;
      }

      // Directly retrieve on IOS. The Unity UI Thread Agent isn't needed.
      string adId = UnityEngine.iOS.Device.advertisingIdentifier;
      if (adId != null) {
        string hashedAdId = HexUtil.HexStringSha512(adId);
        return hashedAdId;
      }
      return adId;
    }
#else

    public string GetUniqueIDType()
    {
      Logger.Log("GetUniqueIDType is NOT IMPLEMENTED");
      return null;
    }
    public string GetUniqueID()
    {
      Logger.Log("GetUniqueID is NOT IMPLEMENTED");
      return null;
    }
#endif
  }

  // Used for testing in UnityEditor (any target platform)
  public class TestUniqueIDClass : UniqueID
  {
    public string GetUniqueIDType()
    {
      return SystemInfo.deviceModel;
    }
    public string GetUniqueID()
    {
      return HexUtil.HexStringSha512(SystemInfo.deviceUniqueIdentifier);
    }
  }
}
