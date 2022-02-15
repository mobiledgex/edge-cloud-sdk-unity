using UnityEngine;
using UnityEditor;
using System.Net;
using System;
using System.IO;

namespace MobiledgeX
{
  [ExecuteInEditMode]
  public class InstallMobiledgeX : EditorWindow
  {
    GUIStyle headerStyle;
    GUIStyle labelStyle;
    GUIStyle textAreaStyle;
    GUIStyle buttonStyle;
    Texture2D mexLogo;
    const string grpcPluginsUrl = "https://packages.grpc.io/archive/2019/11/6950e15882f28e43685e948a7e5227bfcef398cd-6d642d6c-a6fc-4897-a612-62b0a3c9026b/csharp/grpc_unity_package.2.26.0-dev.zip";


    #region EditorWindow Callbacks

    private void Awake()
    {
      if (EditorPrefs.HasKey("MobiledgeXDependenciesDownloaded") && !CheckForGRPCPluginsInProject(Directory.GetDirectories(Application.dataPath)))
      {
        EditorPrefs.SetBool("MobiledgeXDependenciesDownloaded", false);
        EditorPrefs.SetBool("MobiledgeXDependenciesInstalled", false);
      }

      if (EditorPrefs.HasKey("MobiledgeXGRPCSDKInstalled") && !CheckIfMobiledgeXSDKInstalled())
      {
        EditorPrefs.SetBool("MobiledgeXGRPCSDKInstalled", false);
      }
    }

    private void OnGUI()
    {
      Init();
      DrawLogo();
      InstallMobiledgeXWindow();
    }
    private void OnInspectorUpdate()
    {
      Repaint();
    }

    #endregion

    #region Helper Functions

    [MenuItem("MobiledgeX/Install", false, 0)]
    public static void ShowWindow()
    {
      InstallMobiledgeX window = (InstallMobiledgeX)GetWindow(typeof(InstallMobiledgeX), true, "MobiledgeX");
      window.Show();
    }

    void Init()
    {
      mexLogo = Resources.Load("mobiledgexLogo") as Texture2D;
      headerStyle = new GUIStyle();
      headerStyle.normal.background = MakeTex(20, 20, new Color(0.4f, 0.4f, 0.4f));
      buttonStyle = new GUIStyle(GUI.skin.button);
      buttonStyle.normal.background = MakeTex(20, 20, new Color(0.4f, 0.4f, 0.4f));
      buttonStyle.normal.textColor = Color.white;
      textAreaStyle = new GUIStyle(GUI.skin.textArea);
      textAreaStyle.richText = true;
      labelStyle = new GUIStyle(GUI.skin.label);
      labelStyle.normal.textColor = Color.white;
      labelStyle.richText = true;
    }

    /// <summary>
    /// Draw the InstallMobiledgeX Window
    /// </summary>
    private void InstallMobiledgeXWindow()
    {
      GUIStyle completedStyle = new GUIStyle();
      completedStyle.normal.textColor = Color.green;
      EditorGUILayout.BeginVertical(headerStyle);
      GUILayout.TextArea("In order for installation process to work, please ensure the following:" +
        "\n1.If you have any Plugins folder, make sure they are at the Assets Folder." +
        "\n2.No previous MobiledgeX SDK exists in the project." +
        "\n3.Don't Change location of files or directories till the installation is completed.");

      EditorGUILayout.Space();
      if (EditorPrefs.HasKey("MobiledgeXDependenciesDownloaded") && EditorPrefs.GetBool("MobiledgeXDependenciesDownloaded"))
      {
        GUILayout.Label("<b>Step 1:</b> Download MobiledgeX SDK Dependencies", completedStyle);
        GUILayout.Label("Completed");
      }
      else
      {
        GUILayout.Label("<b>Step 1:</b> Download MobiledgeX SDK Dependencies", labelStyle);
        if (GUILayout.Button("Download Plugins", buttonStyle))
        {
          DownloadGRPCPlugins();
        }
      }

      EditorGUILayout.Space();
      if (EditorPrefs.HasKey("MobiledgeXDependenciesInstalled") && EditorPrefs.GetBool("MobiledgeXDependenciesInstalled"))
      {
        GUILayout.Label("<b>Step 2:</b> Unzip dependencies file", completedStyle);
        GUILayout.Label("Completed");
      }
      else
      {
        GUILayout.Label("<b>Step 2:</b>  Unzip dependencies file", labelStyle);
        GUILayout.TextArea("1.Double Click the <b>\"grpc_unity_package.2.26.0-dev.zip\"</b> in your Assets Folder.\n\n2.Once the file is unzipped delete the zip file.\n\n3.Click <b>Check for Completetion</b>.", textAreaStyle);
        if (GUILayout.Button("Check for Completetion", buttonStyle))
        {
          if (CheckForGRPCPluginsInProject(Directory.GetDirectories(Application.dataPath)))
          {
            EditorPrefs.SetBool("MobiledgeXDependenciesInstalled", true);
          }
          else
          {
            Debug.LogError("MobiledgeX SDK Dependencies are not found");
          }
        }
      }
      EditorGUILayout.Space();
      if (EditorPrefs.HasKey("MobiledgeXGRPCSDKInstalled") && EditorPrefs.GetBool("MobiledgeXGRPCSDKInstalled"))
      {
        GUILayout.Label("<b>Step3:</b> Install MobiledgeX to Project", completedStyle);
        GUILayout.Label("Completed");
      }
      else
      {
        GUILayout.Label("<b>Step3:</b> Install MobiledgeX to Project", labelStyle);
        if (GUILayout.Button("Install MobiledgeX", buttonStyle))
        {
          AssetDatabase.ImportPackage(Application.dataPath + "/MobiledgeX/MobiledgeXGRPC.unitypackage", true);
          AssetDatabase.Refresh();
          CheckIfMobiledgeXSDKInstalled();
        }
      }
      EditorGUILayout.Space();
      if (Application.platform == RuntimePlatform.OSXEditor)
      {
        GUILayout.Label("<b>Step4:</b> Trust MobiledgeX SDK Dependecies", labelStyle);
        GUILayout.TextArea("1.From MobiledgeX Menu Click <b>Setup</b> (It will fail)." +
          "\n\n2.Allow the dependencies in your Mac <b>(SystemPreferences/Security&Privacy/General)</b>." +
          "\n\n3.From MobiledgeX Menu Click <b>Setup</b> again (It should succeed this time).", textAreaStyle);
        EditorGUILayout.Space();
      }
      EditorGUILayout.Space();
      GUILayout.Label("Need Help:", labelStyle);
      if (GUILayout.Button("Reach out on Discord", buttonStyle))
      {
        Application.OpenURL("https://discord.gg/CHCWfgrxh6");
      }
      EditorGUILayout.Space();
      EditorGUILayout.EndVertical();
    }

    private void DownloadGRPCPlugins()
    {
      DownloadFile(grpcPluginsUrl, Path.Combine(Application.dataPath, "grpc_unity_package.2.26.0-dev.zip"));
    }

    void DownloadFile(string fileUrl, string filePath)
    {
      using (WebClient wc = new WebClient())
      {
        wc.DownloadProgressChanged += wc_DownloadProgressChanged;
        wc.DownloadFileAsync(new Uri(fileUrl), filePath);
      }
    }

    void wc_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
    {
      if (e.ProgressPercentage < 100)
        EditorUtility.DisplayProgressBar("Downloading MobiledgeX Dependencies", "Download in progress ...", e.ProgressPercentage);
      else
      {
        if (e.ProgressPercentage == 100)
        {
          EditorUtility.ClearProgressBar();
          AssetDatabase.Refresh();
          EditorPrefs.SetBool("MobiledgeXDependenciesDownloaded", true);
        }
      }
    }

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

    private bool CheckIfMobiledgeXSDKInstalled()
    {
      string MobiledgeXSDKMainPlugin = Application.dataPath + "/MobiledgeX/MobiledgeXGRPC/Runtime/Plugins/MobiledgeX.MatchingEngineGrpc.dll";
      if (File.Exists(MobiledgeXSDKMainPlugin))
      {
        EditorPrefs.SetBool("MobiledgeXGRPCSDKInstalled", true);
        return true;
      }
      return false;     
    }
    private bool CheckForGRPCPluginsInProject(string[] assetsDirectories)
    {
      string pluginDirectory = "";
      foreach (string directoryName in assetsDirectories)
      {
        if (directoryName.Contains("Plugins"))
        {
          string[] directoryChildren = Directory.GetDirectories(directoryName);
          Array.Sort(directoryChildren);
          if (Array.BinarySearch(directoryChildren, directoryName + "/Google.Protobuf") >= 0)
          {
            pluginDirectory = directoryName;
          }
        }
      }
      if (pluginDirectory == "")
      {
        return false;
      }
      string[] pluginDirectoryChildren = Directory.GetDirectories(pluginDirectory);
      Array.Sort(pluginDirectoryChildren);
      if (Array.BinarySearch(pluginDirectoryChildren, pluginDirectory + "/Google.Protobuf") < 0)
      {
        return false;
      }
      if (Array.BinarySearch(pluginDirectoryChildren, pluginDirectory + "/Grpc.Core") < 0)
      {
        return false;
      }
      if (Array.BinarySearch(pluginDirectoryChildren, pluginDirectory + "/Grpc.Core.Api") < 0)
      {
        return false;
      }
      if (Array.BinarySearch(pluginDirectoryChildren, pluginDirectory + "/System.Buffers") < 0)
      {
        return false;
      }
      if (Array.BinarySearch(pluginDirectoryChildren, pluginDirectory + "/System.Memory") < 0)
      {
        return false;
      }
      if (Array.BinarySearch(pluginDirectoryChildren, pluginDirectory + "/System.Runtime.CompilerServices.Unsafe") < 0)
      {
        return false;
      }
      return true;
    }
  }
  #endregion
}