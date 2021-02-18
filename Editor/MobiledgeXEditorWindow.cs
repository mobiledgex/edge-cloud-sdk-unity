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

using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using DistributedMatchEngine;
using UnityEditor.PackageManager;
using EnhancementManager;

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
        GUIStyle sdkVersionStyle;
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

        private string sdkVersion;
        private int selectedRegionIndex = 0;
        private List<string> regionOptions = new List<string>(5) { "Nearest", "EU", "JP", "US" };

        #endregion

        #region  Mobiledgex ToolBar Menu items

        [MenuItem("MobiledgeX/Setup", false, 0)]
        public static void ShowWindow()
        {
            MobiledgeXEditorWindow window = (MobiledgeXEditorWindow)EditorWindow.GetWindow(typeof(MobiledgeXEditorWindow), false, "MobiledgeX");
            window.Show();
        }

        [MenuItem("MobiledgeX/Settings", false, 0)]
        public static void ShowSettings()
        {
            settings = (MobiledgeXSettings)Resources.Load("MobiledgeXSettings", typeof(MobiledgeXSettings));
            Selection.objects = new UnityEngine.Object[] { settings };
            EditorGUIUtility.PingObject(settings);
        }

        [MenuItem("MobiledgeX/Docs/SDK Documentation", false, 20)]
        public static void OpenDocumentationURL()
        {
            Application.OpenURL("https://mobiledgex.github.io/unity-samples/index.html");
        }

        [MenuItem("MobiledgeX/Examples/EdgeMultiplay", false, 20)]
        public static void ImportEdgeMultiplayExample()
        {
            DownloadFile("https://github.com/mobiledgex/edge-multiplay-unity-client/raw/main/EdgeMultiplay.unitypackage",
                Path.Combine(Application.dataPath,"EdgeMultiplay.unitypackage"));
            Enhancement.EdgeMultiplayImported(getId());
        }

        [MenuItem("MobiledgeX/Examples/Computer Vision", false, 20)]
        public static void ImportComputerVisionExample()
        {
            string sdkPath = Path.GetFullPath("Packages/com.mobiledgex.sdk");
            AssetDatabase.ImportPackage(Path.Combine(sdkPath, "Resources/Examples/ComputerVision.unitypackage"), true);
            Enhancement.CVImported(getId());
        }

        [MenuItem("MobiledgeX/Join the Community", false, 20)]
        public static void JoinTheCommunity()
        {
            Application.OpenURL("https://discord.gg/k22WcfMFZ3");
        }

        [MenuItem("MobiledgeX/Remove MobiledgeX", false, 40)]
        public static void RemoveMobiledgeX()
        {
            if (EditorUtility.DisplayDialog("MobiledgeX", "Choosing Remove will delete MobiledgeX package and close Unity Editor", "Remove", "Cancel"))
            {
                Enhancement.SDKRemoved(getId());
                if (Directory.Exists(Path.Combine("Assets", "Plugins/MobiledgeX")))
                {
                    Directory.Delete(Path.Combine("Assets", "Plugins/MobiledgeX"), true);
                    File.Delete(Path.Combine("Assets", "Plugins/MobiledgeX") + ".meta");
                }
                EditorPrefs.DeleteKey("mobiledgex-user");
                AssetDatabase.Refresh();
                Client.Remove("com.mobiledgex.sdk");
                EditorApplication.Exit(0);
            }
        }

        #endregion


        #region EditorWindow callbacks

        private void Awake()
        {
            settings = (MobiledgeXSettings)Resources.Load("MobiledgeXSettings", typeof(MobiledgeXSettings));
            settings.sdkVersion = GetSDKVersion();
            sdkVersion = settings.sdkVersion;
            if (PlayerSettings.iOS.locationUsageDescription.Length < 1)
            {
                SetUpLocationSettings();
            }
            if (!editorPopUp && settings.orgName.Length < 1)
            {
                if (!EditorUtility.DisplayDialog("MobiledgeX",
                "Do you have MobiledgeX Account?", "Yes/Will create later", "I want to create one"))
                {
                    if (EditorUtility.DisplayDialog("MobiledgeX",
                "How would you like to connect with us?", "Discord", "Schedule an Meeting"))
                    {
                        Application.OpenURL("https://discord.gg/k22WcfMFZ3");
                    }
                    else
                    {
                        Application.OpenURL("https://developers.mobiledgex.com/getting-started");
                    }
                }
                else
                {
                    Enhancement.SDKInstalled(getId(), Application.unityVersion);
                    editorPopUp = true;
                }
            }
        }

        void OnGUI()
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
            GUILayout.Label(sdkVersion, sdkVersionStyle);
        }

        void OnInspectorUpdate()
        {
            Repaint();
        }

        #endregion 


        #region Private Helper Functions

        string GetSDKVersion()
        {
            TextAsset asset = (TextAsset)AssetDatabase.LoadAssetAtPath("Packages/com.mobiledgex.sdk/package.json", typeof(TextAsset));
            string sdkVersion = JsonUtility.FromJson<PackageDetails>(asset.text).version;
            return "v" + sdkVersion;
        }

        /// <summary>
        /// Load Resources to be used in OnGUI
        /// </summary>
        private void Init()
        {
            settings = Resources.Load<MobiledgeXSettings>("MobiledgeXSettings");
            mexLogo = Resources.Load("mobiledgexLogo") as Texture2D;
            headerStyle = new GUIStyle();
            headerStyle.normal.background = MakeTex(20, 20, new Color(0.4f, 0.4f, 0.4f));
            labelStyle = new GUIStyle(GUI.skin.label);
            labelStyle.normal.textColor = Color.white;
            sdkVersionStyle = new GUIStyle(GUI.skin.label);
            sdkVersionStyle.alignment = TextAnchor.UpperRight;
        }
        /// <summary>
        /// Draws MobiledgeX Logo.
        /// </summary>
        private void DrawLogo()
        {
            EditorGUILayout.BeginHorizontal();
            Rect reservedRect = GUILayoutUtility.GetRect(240, 40);
            Rect LogoRect = new Rect(0, 0, 150, 25);
            Rect LogoLayout = new Rect((reservedRect.width / 2) - (LogoRect.width / 2), (reservedRect.height / 2) - (LogoRect.height / 2), LogoRect.width, LogoRect.height);
            GUI.DrawTexture(LogoLayout, mexLogo, ScaleMode.ScaleToFit, true, 6);
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// Draw the Setup Window.
        /// </summary>
        private async void SetupWindow()
        {

            EditorGUILayout.Space();
            if (settings.appName == "")
            {
                settings.appName = Application.productName;
            }
            settings.orgName = EditorGUILayout.TextField("Organization Name", settings.orgName);
            settings.appName = EditorGUILayout.TextField("App Name", settings.appName);
            settings.appVers = EditorGUILayout.TextField("App Version", settings.appVers);
            if (settings.region.Length > 0)
            {
                try
                {
                    selectedRegionIndex = regionOptions.FindIndex(region => region == settings.region);
                    if (selectedRegionIndex == -1)
                    {
                        selectedRegionIndex = 0;
                    }
                }
                catch (ArgumentNullException)
                {
                    selectedRegionIndex = 0;
                }
                catch (ArgumentOutOfRangeException)
                {
                    selectedRegionIndex = 0;
                }
            }
            EditorGUI.BeginChangeCheck();
            selectedRegionIndex = EditorGUILayout.Popup("Region (Editor Only)", selectedRegionIndex, regionOptions.ToArray());
            EditorGUI.EndChangeCheck();
            settings.region = regionOptions[selectedRegionIndex];
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
                MobiledgeXIntegration.developerAuthToken = settings.authPublicKey;
                progressText = "";
                if (await CheckCredentials())
                {
                    Enhancement.SetupStep(getId());
                    progressText += "\nConnected !\nSee App Information in MobiledgeXSettings!";
                    ShowSettings();
                    EditorUtility.SetDirty(settings);
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
            GUILayout.Label("Reporting Issues and Bugs", labelStyle);
            if (GUILayout.Button("Report a Bug"))
            {
                Application.OpenURL("https://github.com/mobiledgex/edge-cloud-sdk-unity/issues/new?body=Reported%20on%20Unity"+Application.unityVersion+"%0DSDK%20"+sdkVersion);
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
                Debug.Log("MobiledgeX: " + msgTitle + "\n" + msg);
            }
            else
            {
                Debug.LogErrorFormat("MobiledgeX: " + msgTitle + "\n" + msg);
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
            bool checkResult = false;
            integration.UseWifiOnly(true);
            try
            {
                // Register and find cloudlet:
                clog("Registering to DME ...", "");
                checkResult = await integration.Register();
                bool foundCloudlet = await integration.FindCloudlet();

                if (!foundCloudlet)
                {
                    Debug.LogError("MobiledgeX: Couldn't Find findCloudletReply, Make Sure you created App Instances for your Application and they are deployed in the correct region.");
                    throw new FindCloudletException("No findCloudletReply");
                }
                return checkResult;
            }
            catch (HttpRequestException httpre)
            {
                clog("MobiledgeX: RegisterClient HttpRequest Exception", httpre.Message + "\nStack Trace: " + httpre.StackTrace, true);
                return false;
            }
            catch (RegisterClientException rce)
            {
                clog("MobiledgeX: RegisterClientException", rce.Message, true);
                return false;
            }
            catch (FindCloudletException fce)
            {
                clog("MobiledgeX: Couldn't Find findCloudletReply, Make Sure you created App Instances for your Application and they are deployed in the correct region.",
                fce.Message + "\nStack Trace: " + fce.StackTrace, true);
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
            string melAARPath = Path.GetFullPath("Packages/com.mobiledgex.sdk/Runtime/Plugins/Android/mel.aar");
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
                MoveFile(@linkXMLPath, Path.Combine(@mobiledgeXFolderPath, @"link.xml"), true);
                if (!Directory.Exists(Path.Combine(@mobiledgeXFolderPath, @"Resources")))
                {
                    AssetDatabase.CreateFolder("Assets/Plugins/MobiledgeX", "Resources");
                }
                MoveFile(@settingPath, Path.Combine(@mobiledgeXFolderPath, @"Resources/MobiledgeXSettings.asset"), true);
                MoveFile(@sdkPath, Path.Combine(@mobiledgeXFolderPath, @"MatchingEngineSDKRestLibrary.dll"), true);
                if (!Directory.Exists(Path.Combine(@mobiledgeXFolderPath, @"iOS")))
                {
                    AssetDatabase.CreateFolder("Assets/Plugins/MobiledgeX", "iOS");
                }
                MoveFile(@iosPluginPath, Path.Combine(@mobiledgeXFolderPath, @"iOS/PlatformIntegration.m"), true);
                if (!Directory.Exists(Path.Combine(@mobiledgeXFolderPath, @"Android")))
                {
                    AssetDatabase.CreateFolder("Assets/Plugins/MobiledgeX", "Android");
                }
                MoveFile(melAARPath, Path.Combine(@mobiledgeXFolderPath, @"Android/mel.aar"), true);
                AssetDatabase.Refresh();
            }
            catch (Exception e)
            {
                Debug.LogError("MobiledgeX: " + e);
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

        static string getId()
        {
            string id = EditorPrefs.GetString("mobiledgex-user", Guid.NewGuid().ToString());
            EditorPrefs.SetString("mobiledgex-user", id);
            return id;
        }

        static void DownloadFile(string fileUrl, string filePath)
        {
            using (WebClient wc = new WebClient())
            {
                wc.DownloadProgressChanged += wc_DownloadProgressChanged;
                wc.DownloadFileAsync(new Uri(fileUrl), filePath);
                wc.DownloadDataCompleted += DownloadDataCompleted;
            }
        }

       static void wc_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            if (e.ProgressPercentage < 100)
                EditorUtility.DisplayProgressBar("Downloading", "Download in progress ...", e.ProgressPercentage);
            else
                EditorUtility.ClearProgressBar();
        }

        static void DownloadDataCompleted (object sender, DownloadDataCompletedEventArgs e)
        {
            AssetDatabase.ImportPackage(Application.dataPath + "/EdgeMultiplay.unitypackage", true);
        }

        #endregion
    }
}

//for JSON Utility
public class PackageDetails
{
    public string version;

}
