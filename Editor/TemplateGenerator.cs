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

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

namespace MobiledgeX
{
  public class TemplateGenerator : MonoBehaviour
  {
    [MenuItem("MobiledgeX/Create/GetUrlScript", false, 1)]
    static void CreateGetUrlScript()
    {
      string pathUnityPackage = Application.dataPath+"/Runtime/Scripts/ExampleRest.cs";//used if sdk is imported using unity package
      string pathGitUrl = "Packages/com.mobiledgex.sdk/Runtime/Scripts/ExampleRest.cs";//used if sdk is imported using git url
      if (!File.Exists(pathGitUrl))
      {
        Debug.LogError("Source file not found, Please file an issue to https://github.com/mobiledgex/edge-cloud-sdk-unity/issues/new?body=Reported%20on%20Unity" + Application.unityVersion);
        return;
      }
      string[] lines = File.ReadAllLines(pathGitUrl);
      lines[29] = "public class GetUrl : MonoBehaviour";
      string fileLocation = Application.dataPath + "/GetUrl.cs";
      if (File.Exists(fileLocation))
      {
        Debug.LogError($"{fileLocation} already exists");
        return;
      }
      using (StreamWriter outfile = new StreamWriter(fileLocation))
      {
        int counter = 0;
        while (counter < lines.Length)
        {
          outfile.WriteLine(lines[counter]);
          counter++;
        }
      }
      AssetDatabase.Refresh();
    }
  }
}
#endif
