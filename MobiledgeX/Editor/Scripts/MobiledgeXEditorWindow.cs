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
        MobiledgeXIntegration integration;
        Texture2D MexLogo;
        string ProgressText;
        Vector2 scrollPos;
        GUIStyle headerStyle;
        GUIStyle LabelStyle;

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
        [MenuItem("MobiledgeX/Setup",priority =100)]
        public static void ShowWindow()
        {
            Type[] dockerNextTo = new Type[2] { typeof(SceneView),typeof(InspectorMode) };
            GetWindow<MobiledgeXEditorWindow>("MobiledgeX", dockerNextTo);
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
            if (!EditorPrefs.HasKey("PopUp"))
            {
                if (!EditorUtility.DisplayDialog("MobiledgeX",
            "Have you already created an Account?", "Yes", "No"))
                {
                    Application.OpenURL("https://console.mobiledgex.net/");
                };
                EditorPrefs.SetBool("PopUp", true);
            }
            AddMobiledgeXPlugins();
        }

        private void OnGUI()
        {
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
            MexLogo = Resources.Load("mobiledgexLogo") as Texture2D;
            headerStyle = new GUIStyle();

            if (Application.HasProLicense())
            {
                headerStyle.normal.background = MakeTex(20, 20, new Color(0.05f, 0.05f, 0.05f));
            }
            else
            {
                headerStyle.normal.background = MakeTex(20, 20, new Color(0.4f, 0.4f, 0.4f));
            }
            LabelStyle = new GUIStyle(GUI.skin.label);
            LabelStyle.normal.textColor = Color.white;
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
            GUI.DrawTexture(rect, MexLogo,ScaleMode.StretchToFill, true, 10.0F);
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// Draw the Setup Window.
        /// </summary>
        private async void SetupWindow()
        {
            EditorGUILayout.Space();
            MobiledgeXIntegration.devName = EditorGUILayout.TextField("Orginization Name", MobiledgeXIntegration.devName);
            MobiledgeXIntegration.appName = EditorGUILayout.TextField("App Name", MobiledgeXIntegration.appName);
            MobiledgeXIntegration.appVers = EditorGUILayout.TextField("App Version", MobiledgeXIntegration.appVers);
            EditorGUILayout.BeginVertical(headerStyle);
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Width(300), GUILayout.Height(100));
            GUILayout.Label(ProgressText, LabelStyle);
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
            if (GUILayout.Button("Setup"))
            {
                ProgressText = "";
            
                if (await CheckCredentials()) {
                    ProgressText += "\nConnected,You are all set! ";
                   
                }
                else
                {
                 ProgressText += "\nError Connecting, Check the console for more details! ";
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
            GUILayout.Label("To Create or Access your MobiledgeX Account", LabelStyle);
            if (GUILayout.Button("MobiledgeX Console"))
            {
                Application.OpenURL("https://console.mobiledgex.net/");
            }
            EditorGUILayout.Space();
            GUILayout.Label("For Getting Started Tutorials", LabelStyle);
            if (GUILayout.Button("MobiledgeX Guides and Tutorials"))
            {
                Application.OpenURL("https://developers.mobiledgex.com/guides-and-tutorials");
            }
            EditorGUILayout.Space();
            GUILayout.Label("For MobiledgeX API References", LabelStyle);
            if (GUILayout.Button("MobiledgeX API References"))
            {
                Application.OpenURL("https://api.mobiledgex.net/#section/Edge-SDK-Unity");
            }
            EditorGUILayout.Space();
            GUILayout.Label("For Issues and Contributions", LabelStyle);
            if (GUILayout.Button("Mobiledgex SDK &Samples Github"))
            {
                Application.OpenURL("https://github.com/mobiledgex/edge-cloud-sampleapps");
            }
            EditorGUILayout.Space();
            GUILayout.Label("Terms of Use", LabelStyle);
            if (GUILayout.Button("MobiledgeX Terms of Use"))
            {
                Application.OpenURL("https://mobiledgex.com/terms-of-use");
            }
            EditorGUILayout.Space();
            GUILayout.Label("Privacy Policy", LabelStyle);
            if (GUILayout.Button("MobiledgeX Privacy Policy"))
            {
                Application.OpenURL("https://www.mobiledgex.com/privacy-policy");
            }
            EditorGUILayout.EndVertical();
        }

        private void clog(string msgTitle,string msg,bool error=false)
        {
            if (!error)
            {
                UnityEngine.Debug.Log(msgTitle + "\n" + msg);
            }
            else
            {
                UnityEngine.Debug.LogErrorFormat(msgTitle + "\n" + msg);
            }
            
            ProgressText += "\n" + msgTitle;

        }

        /// <summary>
        /// Performs register client to the DME confirm wether ther user provided the right fields or not
        /// </summary>
        /// <returns>boolean value</returns>
        public async Task<bool> CheckCredentials()
        {
            integration = new MobiledgeXIntegration();
            integration.useWifiOnly(true);
            try
            {
                // Register and find cloudlet:
                clog("Registering to DME ...","");
                return await integration.Register();
            }
            catch (HttpException httpe) // HTTP status, and REST API call error codes.
            {
                // server error code, and human readable message:
                clog("RegisterClient Exception ", httpe.Message + ", HTTP StatusCode: " + httpe.HttpStatusCode + ", API ErrorCode: " + httpe.ErrorCode + "\nStack: " + httpe.StackTrace,true);
                return false;
            }
            catch (HttpRequestException httpre)
            {
                clog("RegisterClient HttpRequest Exception" ,httpre.Message + "\nStack Trace: " + httpre.StackTrace,true);
                return false;
            }
            catch (Exception e)
            {
                clog("Unexpected Exception " ,e.StackTrace,true);
                return false;
            }
        }

        /// <summary>
        /// Adds Mobiledgex Plugins to the Unity Project (SDK dll, IOS Plugin)
        /// </summary>
        private void AddMobiledgeXPlugins()
        {
            string pluginsFolderPath = @Application.dataPath + @"/Plugins";
            string MobiledgeXFolderPath = @pluginsFolderPath + @"/MobiledgeX";
            string dllPath = Path.GetFullPath("Packages/com.mobiledgex.mobiledgexsdk/Editor/Plugins/MatchingEngineSDKRestLibrary.dll");
            string iosPluginPath = Path.GetFullPath("Packages/com.mobiledgex.mobiledgexsdk/Editor/Plugins/PlatformIntegration.m");
            try
            {
                if (!Directory.Exists(@pluginsFolderPath))
                {
                    AssetDatabase.CreateFolder("Assets", "Plugins");
                }
                if (!Directory.Exists(@MobiledgeXFolderPath))
                {
                    AssetDatabase.CreateFolder("Assets/Plugins", "MobiledgeX");
                }
                if (!Directory.Exists(@MobiledgeXFolderPath))
                {
                    AssetDatabase.CreateFolder("Assets/Plugins", "MobiledgeX");
                }
                if (!File.Exists(Path.Combine(@MobiledgeXFolderPath , @"MatchingEngineSDKRestLibrary.dll")))
                {
                    FileUtil.MoveFileOrDirectory(dllPath, @MobiledgeXFolderPath + @"/MatchingEngineSDKRestLibrary.dll");

                }
                if (!File.Exists(Path.Combine(@MobiledgeXFolderPath , @"PlatformIntegration.m")))
                {
                    FileUtil.MoveFileOrDirectory(iosPluginPath, @MobiledgeXFolderPath + @"/PlatformIntegration.m");

                }
            }
            catch (Exception e)
            {
                Debug.Log(e);
            }
        }

        #endregion
    }
}