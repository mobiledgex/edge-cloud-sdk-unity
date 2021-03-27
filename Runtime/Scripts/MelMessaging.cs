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
using UnityEngine;
using MobiledgeX;
using DistributedMatchEngine.Mel;

public class MelMessaging : MelMessagingInterface
{

#if UNITY_ANDROID
  AndroidJavaObject getActivity()
  {
    AndroidJavaClass unityPlayer = PlatformIntegrationUtil.GetAndroidJavaClass("com.unity3d.player.UnityPlayer");
    if (unityPlayer == null)
    {
      return null;
    }
    AndroidJavaObject activity = PlatformIntegrationUtil.GetStatic<AndroidJavaObject>(unityPlayer, "currentActivity");
    if (activity == null)
    {
      return null;
    }
    return activity;
  }

  AndroidJavaClass getMel()
  {
    AndroidJavaClass MelCls = PlatformIntegrationUtil.GetAndroidJavaClass("com.mobiledgex.mel.MelMessaging");
    return MelCls;
  }

  public MelMessaging(string appName)
  {
    // MobiledgeX Vender Interface. Only for Android.
    AndroidJavaClass MelCls = getMel();

    // Fire off intents to update MEL state.
    var activity = getActivity();
    if (activity == null)
    {
      return;
    }
    MelCls.CallStatic("sendForMelStatus", activity, appName);
  }

  public bool IsMelEnabled()
  {
    AndroidJavaClass MelCls = getMel();
    if (MelCls == null)
    {
      return false;
    }
    bool enabled = PlatformIntegrationUtil.CallStatic<Boolean>(MelCls, "isMelEnabled");
    return enabled;
  }

  public string GetMelVersion()
  {
    AndroidJavaClass MelCls = getMel();
    if (MelCls == null)
    {
      return "";
    }
    return PlatformIntegrationUtil.CallStatic<string>(MelCls, "getMelVersion");
  }

  public string GetUid()
  {
    AndroidJavaClass MelCls = getMel();
    if (MelCls == null)
    {
      return "";
    }
    string uid = PlatformIntegrationUtil.CallStatic<string>(MelCls, "getUid");
    return uid;
  }

  public string SetToken(string token, string app_name)
  {
    AndroidJavaClass MelCls = getMel();
    if (MelCls == null)
    {
      return "";
    }
    object[] pa = new object[3] { getActivity(), token, app_name };
    string sent_token = PlatformIntegrationUtil.CallStatic<string>(MelCls, "sendSetToken", pa);
    return sent_token;
  }

  // MelMessaging related:
  public string GetManufacturer()
  {
     AndroidJavaClass BuildCls = PlatformIntegrationUtil.GetAndroidJavaClass("android.os.Build");
     string manufacturer = PlatformIntegrationUtil.GetStatic<string>(BuildCls, "MANUFACTURER");
     return manufacturer;
  }
#elif UNITY_IOS
  public MelMessaging(string app_name) { }
  public bool IsMelEnabled() { return false; }
  public string GetMelVersion() { return ""; }
  public string SetToken(string token, string app_name) { return ""; }
  public string GetUid() { return ""; }
  public string GetManufacturer() { return "Apple"; }
#else
  public MelMessaging(string app_name) { }
  public bool IsMelEnabled() { return false; }
  public string GetMelVersion() { return ""; }
  public string SetToken(string token, string app_name) { return ""; }
  public string GetUid() { return ""; }
  public string GetManufacturer() { return ""; }
#endif
}
