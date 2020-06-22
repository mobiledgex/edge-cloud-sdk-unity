using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using DistributedMatchEngine;
using System.Linq;
using System.Collections.Generic;
using UnityEditor.PackageManager.Requests;
using UnityEditor.PackageManager;

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
        static RemoveRequest Request; // used for removing MobiledgeX Package from the Unity project

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

        [MenuItem("MobiledgeX/Setup")]
        public static void ShowWindow()
        {
            MobiledgeXEditorWindow window = (MobiledgeXEditorWindow)EditorWindow.GetWindow(typeof(MobiledgeXEditorWindow));
            window.Show();
        }

        [MenuItem("MobiledgeX/Settings")]
        public static void ShowSettings()
        {
            settings = (MobiledgeXSettings)Resources.Load("MobiledgeXSettings", typeof(MobiledgeXSettings));
            Selection.objects = new UnityEngine.Object[] { settings };
            EditorGUIUtility.PingObject(settings);
        }

        [MenuItem("MobiledgeX/API References")]
        public static void OpenAPIReferencesURL()
        {
            Application.OpenURL("https://api.mobiledgex.net/#section/Edge-SDK-Unity");
        }

        [MenuItem("MobiledgeX/Documentation")]
        public static void OpenDocumentationURL()
        {
            Application.OpenURL("https://developers.mobiledgex.com/sdk-libraries/unity-sdk");
        }

        [MenuItem("MobiledgeX/Remove MobiledgeX")]
        public static void RemoveMobiledgeX()
        {
             if (EditorUtility.DisplayDialog("MobiledgeX",
                "Choosing Remove will delete MobiledgeX package and close Unity Editor", "Remove", "Cancel"))
                {
                    if(Directory.Exists(Path.Combine("Assets", "Plugins/MobiledgeX"))){
                         Directory.Delete(Path.Combine("Assets", "Plugins/MobiledgeX"), true);
                         File.Delete(Path.Combine("Assets", "Plugins/MobiledgeX")+".meta");
                    }
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
            if (PlayerSettings.iOS.locationUsageDescription.Length < 1)
            {
                SetUpLocationSettings();
            }
            if (!editorPopUp && settings.orgName.Length < 1)
            {
                if (!EditorUtility.DisplayDialog("MobiledgeX",
                "Have you already created an Account?", "Yes", "No"))
                {
                    Application.OpenURL("https://console.mobiledgex.net/");
                }
                else
                {
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
        }

        #endregion


        #region Private Helper Functions

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
            rect.width = 180;
            rect.height = 30;
            GUI.DrawTexture(rect, mexLogo, ScaleMode.StretchToFill, true, 10.0F);
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
                // MobiledgeXIntegration.tcpPort = (int)settings.TCP_Port;
                // MobiledgeXIntegration.udpPort = (int)settings.UDP_Port;
                progressText = "";
                if (await CheckCredentials())
                {
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
                /*AppPort[] appPortList = integration.AppPortList;
                if (appPortList != null && appPortList.Length > 0)
                {
                    // mappedPorts size is presisted to since mappedPorts is exposed in the Inspector(used in OnValidation in MobiledgeXSettings)
                    settings.mappedPortsSize = appPortList.Length;
                    foreach (AppPort appPort in appPortList)
                    {
                        Port port = new Port(appPort);
                        // check if port have already being added ,(In EditorWindow)  if Setup is pressed before
                        // Port.ToString() returns "tlsProtocolPortNumber" > ex "SecureTCP6000" ,ex "UDP3000"
                        if (!settings.mappedPorts.Any(mappedPort => mappedPort.ToString() == port.ToString()))
                        {
                            settings.mappedPorts.Add(port);
                        }
                    }
                    // overwrites the TCPPorts enum or  UDPPorts enum
                    // TCPPorts,UDPPorts scripts once the package is imported
                    // Once the credential check passes enums are being created
                    // enum values ex (TCP5000 = 5000) the integer value used in MobiledgeXIntegration with an integer cast
                    CreateEnum("TCPPorts", Protocol.TCP);
                    CreateEnum("UDPPorts", Protocol.UDP);
                }
                else
                {
                    Debug.LogError("No Mapped Ports for your application backend");
                }*/
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
                if (!Directory.Exists(Path.Combine(@mobiledgeXFolderPath, @"Resources"))){

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

        /// <summary>
        /// Creates Enum for Mapped Ports 
        /// </summary>
        /// <param name="enumName">will be the name of the file</param>
        /// <param name="protocol">TCP, UDP</param>
        /// Protocol enum exists in MobiledgeXSettings
        /*public static void CreateEnum(string enumName, Protocol protocol)
        {
            List<string> enumEntries = new List<string>(settings.mappedPorts.Count);
            foreach (Port port in settings.mappedPorts)
            {
                if (port.ToString().Contains(protocol.ToString()))
                {
                    enumEntries.Add(port.ToString());
                }
            }
            string filePathAndName = "Packages/com.mobiledgex.sdk/RunTime/Scripts/" + enumName + ".cs";
            using (StreamWriter streamWriter = new StreamWriter(filePathAndName))
            {
                streamWriter.WriteLine("namespace MobiledgeX{ \t ");
                streamWriter.WriteLine("public enum " + enumName);
                streamWriter.WriteLine("{ \t");
                for (int i = 0; i < enumEntries.Count; i++)
                {
                    string portName = enumEntries[i];
                    int portNumber;
                    int.TryParse(new String(enumEntries[i].Where(Char.IsDigit).ToArray()), out portNumber);
                    streamWriter.WriteLine("\t" + portName + "=" + portNumber + ",");
                }
                streamWriter.WriteLine("}}");
            }
            // refresh assets to update MobiledgeX Assembly definition using the default ImportAssetOptions.Default
            AssetDatabase.Refresh();
        }*/
        #endregion
    }
}
