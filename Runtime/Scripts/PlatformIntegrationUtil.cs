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

namespace MobiledgeX
{
  public class PlatformIntegrationUtil
  {

#if UNITY_ANDROID

    // empty parameters for JNI calls
    private static object[] emptyObjectArr = new object[0];

    public static AndroidJavaClass GetAndroidJavaClass(string pkg)
    {
      try
      {
        return new AndroidJavaClass(pkg);
      }
      catch (Exception e)
      {
        Logger.Log("Could not get AndroidJavaClass " + pkg + ". Exception is: " + e.Message);
        return null;
      }
    }

    public static AndroidJavaObject GetAndroidJavaObject(string pkg)
    {
      try
      {
        return new AndroidJavaObject(pkg);
      }
      catch (Exception e)
      {
        Logger.Log("Could not get AndroidJavaObject " + pkg + ". Exception is: " + e.Message);
        return null;
      }
    }

    public static string GetSimpleName(AndroidJavaObject obj)
    {
      try
      {
        return obj.Call<AndroidJavaObject>("getClass", emptyObjectArr).Call<string>("getSimpleName", emptyObjectArr);
      }
      catch (Exception e)
      {
        Logger.Log("Could not getSimpleName. Exception is " + e.Message);
        return "";
      }
    }
   
    // Generic functions that get static variables, call static functions, and call object functions

    public static T GetStatic<T>(AndroidJavaClass c, string member)
    {
      try
      {
        return c.GetStatic<T>(member);
      }
      catch (Exception e)
      {
        Logger.Log("Could not GetStatic " + typeof(T) + ". Exception: " + e.Message);
        return default(T);
      }
    }

    public static T CallStatic<T>(AndroidJavaClass c, string method, object[] param = null)
    {
      if (param == null)
      {
        param = emptyObjectArr;
      }

      try
      {
        return c.CallStatic<T>(method, param);
      }
      catch (Exception e)
      {
        Logger.Log("Could not CallStatic " + typeof(T) + ". Exception: " + e.Message);
        return default(T);
      }
    }

    public static T Call<T>(AndroidJavaObject obj, string method, object[] param = null)
    {
      if (param == null)
      {
        param = emptyObjectArr;
      }

      try
      {
        return obj.Call<T>(method, param);
      }
      catch (Exception e)
      {
        Logger.Log("Could not Call " + typeof(T) + " method " + method + ". Exception: " + e.Message);
        return default(T);
      }
    }

#endif
  }
}
