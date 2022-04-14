/**
 * Copyright 2018-2022 MobiledgeX, Inc. All rights and licenses reserved.
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

namespace MobiledgeX
{
  /// <summary>
  /// Internal class for MobiledgeX SDK logs
  /// </summary>
  public class Logger
  {
    /// <summary>
    /// Defines the log level for the SDK, Its advised to set the log level to ErrorsOnly in a production environment.
    /// </summary>
    public enum LogType { All, ErrorsAndWarnings, ErrorsOnly };

    internal static void LogWarning(string message)
    {
      if (MobiledgeXIntegration.settings.logType == LogType.All || MobiledgeXIntegration.settings.logType == LogType.ErrorsAndWarnings)
      {
        Debug.LogWarning("MobiledgeX: " + message);
      }
    }

    internal static void Log(string message)
    {
      if (MobiledgeXIntegration.settings.logType == LogType.All)
      {
        Debug.Log("MobiledgeX: " + message);
      }
    }
  }
}
