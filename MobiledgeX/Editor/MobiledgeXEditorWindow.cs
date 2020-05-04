using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using DistributedMatchEngine;

namespace MobiledgeX
  {
      [ExecuteInEditMode]
      public class MobiledgeXEditorWindow : EditorWindow
      {
      

          #region Private Variables
      
          Texture2D mexLogo;
          string progressText;
          Vector2 scrollPos;
          GUIStyle headerStyle;
          GUIStyle labelStyle;
          static MobiledgeXSettings settings;
          static bool editorPopUp;
          
          /// <summary>
          /// The titles of the tabs in Mobiledgex window.
          /// </summary>
          private readonly string[] tabTitles = { "Setup", "Documentation", "License" };

          /// <summary>
          /// The currently selected tab in the Mobiledgex window.
          /// </summary>
          private int currentTab;

          /// <summary>
          /// Changing the background color
          /// </summary>
          private Texture2D MakeTex(int width, int height, Color col)
          {
              Color[] pix = new Color[width * height];

              for (int i = 0; i < pix.Length; i++)
                  pix[i] = col;

              Texture2D result = new Texture2D(width, height);
              result.SetPixels(pix);
              result.Apply();

              return result;
          }

          #endregion

          #region  Mobiledgex ToolBar Menu items

          [MenuItem("MobiledgeX/Setup", priority = 100)]
          public static void ShowWindow()
          {
              Type[] dockerNextTo = new Type[2] { typeof(SceneView), typeof(InspectorMode) };
          GetWindow<MobiledgeXEditorWindow>("MobiledgeX", dockerNextTo);
          }

          [MenuItem("MobiledgeX/Settings", priority = 100)]
          public static void ShowSettings()
          {
              settings = (MobiledgeXSettings)Resources.Load("MobiledgeXSettings", typeof(MobiledgeXSettings));
              Selection.objects = new UnityEngine.Object[] { settings };
              EditorGUIUtility.PingObject(settings);
          }

          [MenuItem("MobiledgeX/API References")]
          public static void OpenAPIReferencesURL()
          {
              Application.OpenURL("https://swagger.mobiledgex.net/client-test/#section/Edge-SDK-Unity");
          }

          [MenuItem("MobiledgeX/Documentation")]
          public static void OpenDocumentationURL()
          {
              Application.OpenURL("https://developers.mobiledgex.com/sdk-libraries/unity-sdk");
          }

          #endregion


          #region EditorWindow callbacks

          private void Awake()
          {
          
              if (PlayerSettings.iOS.locationUsageDescription.Length<1)
              {
                  SetUpLocationSettings();
              }

          }
          private void OnGUI()
          {
              AssetDatabase.Refresh();
              if (!editorPopUp)
              {
                  if (!EditorUtility.DisplayDialog("MobiledgeX",
              "Have you already created an Account?", "Yes", "No"))
                  {
                      Application.OpenURL("https://console.mobiledgex.net/");
                  }else{
                    editorPopUp = true;
                  }
                  
              }
              Init();
              DrawLogo();
              int selectedTab = GUILayout.Toolbar(currentTab, tabTitles);
              if (selectedTab != currentTab)
              {
                  currentTab = 0;
              }
              currentTab = selectedTab;
              switch (currentTab)
              {
                  default:
                  case 0:
                      SetupWindow();
                      break;
                  case 1:
                      DocumentationWindow();
                      break;
                  case 2:
                      LicenseWindow();
                      break;
              }
          }

              #endregion


              #region Private Helper Functions

              /// <summary>
              /// Load Resources to be used in OnGUI
              /// </summary>
              private void Init()
          {
              mexLogo = Resources.Load("mobiledgexLogo") as Texture2D;
              headerStyle = new GUIStyle();
              if (Application.HasProLicense())
              {
                  headerStyle.normal.background = MakeTex(20, 20, new Color(0.05f, 0.05f, 0.05f));
              }
              else
              {
                  headerStyle.normal.background = MakeTex(20, 20, new Color(0.4f, 0.4f, 0.4f));
              }
              labelStyle = new GUIStyle(GUI.skin.label);
              labelStyle.normal.textColor = Color.white;
          }

          /// <summary>
          /// Draws MobiledgeX Logo.
          /// </summary>
          private void DrawLogo()
          {
              EditorGUILayout.BeginHorizontal();
              Rect rect = GUILayoutUtility.GetRect(200, 50);
              int padding = EditorStyles.label.padding.vertical;
              rect.x = 100;
              rect.y += padding;
              rect.width = 200;
              rect.height = 50;
              GUI.DrawTexture(rect, mexLogo, ScaleMode.StretchToFill, true, 10.0F);
              EditorGUILayout.EndHorizontal();
          }

          /// <summary>
          /// Draw the Setup Window.
          /// </summary>
          private async void SetupWindow()
          {
              settings = Resources.Load<MobiledgeXSettings>("MobiledgeXSettings");
              EditorGUILayout.Space();
              settings.orgName = EditorGUILayout.TextField("Orginization Name", settings.orgName);
              settings.appName = EditorGUILayout.TextField("App Name", settings.appName);
              settings.appVers = EditorGUILayout.TextField("App Version", settings.appVers);
              EditorGUILayout.BeginVertical(headerStyle);
              scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Width(300), GUILayout.Height(100));
              GUILayout.Label(progressText, labelStyle);
              EditorGUILayout.EndScrollView();
              EditorGUILayout.EndVertical();
              if (GUILayout.Button("Setup"))
              {
                  MobiledgeXIntegration.orgName = settings.orgName;
                  MobiledgeXIntegration.appName = settings.appName;
                  MobiledgeXIntegration.appVers = settings.appVers;
                  progressText = "";
                  if (await CheckCredentials())
                  {
                      progressText += "\nConnected,You are all set! ";
                      AddMobiledgeXPlugins();
                  }
                  else
                  {
                      progressText += "\nError Connecting, Check the console for more details! ";
                  }
              }
          }

          /// <summary>
          /// Draw the License Window
          /// </summary>
          private void LicenseWindow()
          {
              EditorGUILayout.BeginHorizontal();
              string licenseText = "Copyright 2020 MobiledgeX, Inc.All rights and licenses reserved.\n MobiledgeX, Inc. 156 2nd Street #408, San Francisco, CA 94105" +
                  "Licensed under the Apache License, Version 2.0 (the \"License\") \n you may not use this file except in compliance with the License.\n You may obtain a copy of the License at" +
                  "\n \n  http://www.apache.org/licenses/LICENSE-2.0  \n  \n Unless required by applicable law or agreed to in writing, software \n distributed under the License is distributed on an \"AS IS- BASIS\" \n" +
                  "WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. \n See the License for the specific language governing permissions and\n limitations under the License.";
              GUILayout.TextArea(licenseText);
              EditorGUILayout.EndHorizontal();
          }

          /// <summary>
          /// Draw the Documentation Window
          /// </summary>
          private void DocumentationWindow()
          {
              EditorGUILayout.BeginVertical(headerStyle);
              EditorGUILayout.Space();
              GUILayout.Label("To Create or Access your MobiledgeX Account", labelStyle);
              if (GUILayout.Button("MobiledgeX Console"))
              {
                  Application.OpenURL("https://console.mobiledgex.net/");
              }
              EditorGUILayout.Space();
              GUILayout.Label("For Getting Started Tutorials", labelStyle);
              if (GUILayout.Button("MobiledgeX Guides and Tutorials"))
              {
                  Application.OpenURL("https://developers.mobiledgex.com/guides-and-tutorials");
              }
              EditorGUILayout.Space();
              GUILayout.Label("For MobiledgeX API References", labelStyle);
              if (GUILayout.Button("MobiledgeX API References"))
              {
                  Application.OpenURL("https://api.mobiledgex.net/#section/Edge-SDK-Unity");
              }
              EditorGUILayout.Space();
              GUILayout.Label("For Issues and Contributions", labelStyle);
              if (GUILayout.Button("Mobiledgex SDK &Samples Github"))
              {
                  Application.OpenURL("https://github.com/mobiledgex/edge-cloud-sampleapps");
              }
              EditorGUILayout.Space();
              GUILayout.Label("Terms of Use", labelStyle);
              if (GUILayout.Button("MobiledgeX Terms of Use"))
              {
                  Application.OpenURL("https://mobiledgex.com/terms-of-use");
              }
              EditorGUILayout.Space();
              GUILayout.Label("Privacy Policy", labelStyle);
              if (GUILayout.Button("MobiledgeX Privacy Policy"))
              {
                  Application.OpenURL("https://www.mobiledgex.com/privacy-policy");
              }
              EditorGUILayout.EndVertical();
          }

          private void clog(string msgTitle, string msg, bool error = false)
          {
              if (!error)
              {
                  Debug.Log(msgTitle + "\n" + msg);
              }
              else
              {
                  Debug.LogErrorFormat(msgTitle + "\n" + msg);
              }

              progressText += "\n" + msgTitle;

          }

          /// <summary>
          /// Performs register client to the DME confirm wether ther user provided the right fields or not
          /// </summary>
          /// <returns>boolean value</returns>
          public async Task<bool> CheckCredentials()
          {
              MobiledgeXIntegration integration = new MobiledgeXIntegration();
              integration.useWifiOnly(true);
              try
              {
                  // Register and find cloudlet:
                  clog("Registering to DME ...", "");
                  return await integration.Register();
              }
              catch (HttpException httpe) // HTTP status, and REST API call error codes.
              {
                  // server error code, and human readable message:
                  clog("RegisterClient Exception ", httpe.Message + ", HTTP StatusCode: " + httpe.HttpStatusCode + ", API ErrorCode: " + httpe.ErrorCode + "\nStack: " + httpe.StackTrace, true);
                  return false;
              }
              catch (HttpRequestException httpre)
              {
                  clog("RegisterClient HttpRequest Exception", httpre.Message + "\nStack Trace: " + httpre.StackTrace, true);
                  return false;
              }
              catch (Exception e)
              {
                  clog("Unexpected Exception ", e.StackTrace, true);
                  return false;
              }
          }

          /// <summary>
          /// Adds Mobiledgex Plugins to the Unity Project (SDK dll, IOS Plugin, link.xml and MobiledgeXSettings)
          /// </summary>
           void AddMobiledgeXPlugins()
          {

              string unityPluginsFolderPath = Path.Combine(@Application.dataPath, @"Plugins");
              string resourcesFolderPath = Path.Combine(@Application.dataPath, @"Resources");
              string mobiledgeXFolderPath = Path.Combine(@unityPluginsFolderPath, @"MobiledgeX");
              string sdkPath = Path.GetFullPath("Packages/com.mobiledgex.sdk/Runtime/Plugins/MatchingEngineSDKRestLibrary.dll");
              string iosPluginPath = Path.GetFullPath("Packages/com.mobiledgex.sdk/Runtime/Plugins/iOS/PlatformIntegration.m");
              string linkXMLPath = Path.GetFullPath("Packages/com.mobiledgex.sdk/link.xml");
              string settingPath = Path.GetFullPath("Packages/com.mobiledgex.sdk/Resources/MobiledgeXSettings.asset");
              try
                  {
                      if (!Directory.Exists(@unityPluginsFolderPath))
                      {
                          AssetDatabase.CreateFolder("Assets", "Plugins");
                      }
                      if (!Directory.Exists(@mobiledgeXFolderPath))
                      {
                          AssetDatabase.CreateFolder("Assets/Plugins", "MobiledgeX");
                      }
                      MoveFile(@sdkPath, Path.Combine(@mobiledgeXFolderPath, @"MatchingEngineSDKRestLibrary.dll"), true);
                      if (!Directory.Exists(Path.Combine(@mobiledgeXFolderPath, @"iOS")))
                      {
                          AssetDatabase.CreateFolder("Assets/Plugins/MobiledgeX", "iOS");
                      }
                      MoveFile(@iosPluginPath, Path.Combine(@mobiledgeXFolderPath, @"iOS/PlatformIntegration.m"), true);
                      MoveFile(@linkXMLPath, Path.Combine(@mobiledgeXFolderPath, @"link.xml"), true);
                      if (!Directory.Exists(Path.Combine("Assets", @"Resources")))
                      {
                          AssetDatabase.CreateFolder("Assets", "Resources");
                      }
                      MoveFile(@settingPath, Path.Combine(@resourcesFolderPath, @"MobiledgeXSettings.asset"), true);
                      AssetDatabase.Refresh();
                  }
                  catch (Exception e)
                  {
                      Debug.LogError(e);
                      Debug.Log("MobiledgeX: Please Follow these steps \n 1.remove the package from the Pacakge Manager. \n 2.Delete This folder Assets/MobiledgeX \n 3.Use the Package Manager to download Again.");
                  }
          }
          void MoveFile(string srcPath, string targetPath, bool MoveMetaFiles)
          {
              if (!File.Exists(Path.Combine(targetPath)) && File.Exists(srcPath))
              {

              FileUtil.MoveFileOrDirectory(srcPath, targetPath);
              if (MoveMetaFiles)
              {
                  string srcMetaPath = srcPath + ".meta";
                  string targetMetaPath = targetPath + ".meta";
                  FileUtil.MoveFileOrDirectory(srcMetaPath, targetMetaPath);
              }
             }

          }

          void SetUpLocationSettings()
          {
              PlayerSettings.iOS.locationUsageDescription = "Geo-Location is used by MobiledgeX SDK to locate the closest edge cloudlet server and (where supported) for carrier enhanced Verify Location services.";
          }

          #endregion
      }
  }
