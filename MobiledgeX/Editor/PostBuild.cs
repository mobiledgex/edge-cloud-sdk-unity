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

using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
namespace Mobiledgex
{
    /// <summary>
    /// Post Build Adds Core Telephony Framework to the generated XCode Project, Make Sure Unity Ads Package is Updated or Removed
    /// </summary>
    public class PostBuild
    {

        [PostProcessBuildAttribute(1)]
        public static void OnPostProcessBuild(BuildTarget target, string path)
        {

            if (target == BuildTarget.iOS)
            {
                // Read.
                string projectPath = PBXProject.GetPBXProjectPath(path);
                PBXProject project = new PBXProject();
                project.ReadFromString(File.ReadAllText(projectPath));
#if UNITY_2018
                string targetName = PBXProject.GetUnityTargetName(); // note, not "project." ...
                string targetGUID = project.TargetGuidByName(targetName);
                AddFrameworks(project, targetGUID);
#else 
                string targetGUID = project.GetUnityMainTargetGuid();
                string unityFrameworkGUID = project.GetUnityFrameworkTargetGuid();
                AddFrameworks(project, targetGUID, unityFrameworkGUID);
#endif
                // Write.
                File.WriteAllText(projectPath, project.WriteToString());
            }
        }
#if UNITY_2018
        static void AddFrameworks(PBXProject project, string targetGUID)
        {
            // Frameworks (eppz! Photos, Google Analytics).
            // to add CoreTelephonyFramwork to the project
            project.AddFrameworkToProject(targetGUID, "CoreTelephony.framework", false);

            // Add `-ObjC` to "Other Linker Flags".
            project.AddBuildProperty(targetGUID, "OTHER_LDFLAGS", "-ObjC");
        }
#else

        static void AddFrameworks(PBXProject project, string targetGUID, string unityFrameworkGUID)

        {
            // to add CoreTelephonyFramwork to the project
            project.AddFrameworkToProject(targetGUID, "CoreTelephony.framework", false);
            // to add CoreTelephonyFramework to UnityFramework
            project.AddFrameworkToProject(unityFrameworkGUID, "CoreTelephony.framework", false);

            // Add `-ObjC` to "Other Linker Flags".
            project.AddBuildProperty(targetGUID, "OTHER_LDFLAGS", "-ObjC");
        }
#endif
    }
}
