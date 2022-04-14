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
  /// MobiledgeXSettings defines the settings of the connection to MobiledgeX platform
  /// </summary>
  public class MobiledgeXSettings : ScriptableObject
  {
    /// <summary>
    /// MobiledgeX SDK Version
    /// </summary>
    [HideInInspector]
    public string sdkVersion;
    /// <summary>
    /// (Required)Organization name
    /// </summary>
    [Tooltip("(Required)Organization name")]
    public string orgName;
    /// <summary>
    /// (Required)Application name
    /// </summary>
    [Tooltip("Required)Application Name")]
    public string appName;
    /// <summary>
    /// (Required)Application version must match the image version sumbitted to MobiledgeX docker registry.
    /// </summary>
    [Tooltip("(Required)Application version must match the image version sumbitted to MobiledgeX docker registry.")]
    public string appVers;
    /// <summary>
    /// (Optional)Public key (string value) supplied by the developer for authentication 
    /// </summary>
    [Tooltip("(Optional)Public key (string value) supplied by the developer for authentication")]
    public string authPublicKey;
    /// <summary>
    /// The Regional DME, Select the region your app is deployed in.
    /// Mapped regions are the following: {EU, US,JP, Nearest} 
    /// </summary>
    [Tooltip("The Regional DME, Select the region your app is deployed in.\nMapped regions are the following: {EU, US,JP, Nearest}")]
    public string region;
    /// <summary>
    /// Defines the log level for the SDK, set the log level to ErrorsOnly in production.
    /// </summary>
    [Tooltip("Defines the log level for the SDK, set the log level to ErrorsOnly in production.")]
    public Logger.LogType logType = Logger.LogType.ErrorsAndWarnings;
  }
}
