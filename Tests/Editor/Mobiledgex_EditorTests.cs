using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.IO;
using System.Threading.Tasks;
using System;
using System.Net.Http;
using UnityEditor;
using DistributedMatchEngine;


namespace MobiledgeX
{
  public class MobiledgeX_EditorTests
  {
    #region Testing Setup & TearDown
    MobiledgeXSettings settings;
    MobiledgeXEditorWindow mobiledgeXEditor;
    MobiledgeXIntegration integration;

    [OneTimeSetUp]
    public void SetupEditorWindow()
    {
      settings = Resources.Load<MobiledgeXSettings>("MobiledgeXSettings");
      mobiledgeXEditor = MobiledgeXEditorWindow.CreateInstance<MobiledgeXEditorWindow>();
      integration = new MobiledgeXIntegration();
      mobiledgeXEditor.ShowPopup();
    }

    [OneTimeTearDown]
    public void Clean()
    {
      mobiledgeXEditor.Close();
    }

    #endregion

    #region HelperFunctions
    public async Task<bool> CheckCredentialsHelper()
    {

      bool check = await integration.Register();
      await Task.Delay(TimeSpan.FromMilliseconds(200));

      return check;
    }

    #endregion

    #region Editor Tests

    [Test]
    public void MovingFilesTest()
    {
      AssetDatabase.Refresh();
      Assert.True(File.Exists(Path.Combine(Application.dataPath, "Plugins/MobiledgeX/iOS/PlatformIntegration.m")));
      Assert.True(File.Exists(Path.Combine(Application.dataPath, "Plugins/MobiledgeX/link.xml")));
      Assert.True(File.Exists(Path.Combine(Application.dataPath, "Plugins/MobiledgeX/MatchingEngineSDKRestLibrary.dll")));
      Assert.True(File.Exists(Path.Combine(Application.dataPath, "Resources/MobiledgeXSettings.asset")));
    }


    [Test]
    [TestCase("MobiledgeX", "MobiledgeX SDK Demo", "2.0")]
    public void CheckCredentials(string orgName, string appName, string appVers)
    {
      settings.orgName = orgName;
      settings.appName = appName;
      settings.appVers = appVers;
      var task = Task.Run(async () =>
      {
        return await CheckCredentialsHelper();
      });

      Assert.True(task.Result);
    }


    [Test]
    [TestCase("WrongCredentials", "", "latest")]
    [TestCase("WrongCredentials", "WrongAppName", "2018-20-20")]
    public void ExpectedExceptionTest(string orgName, string appName, string appVers)
    {
      settings.orgName = orgName;
      settings.appName = appName;
      settings.appVers = appVers;
      try
      {
        CheckCredentials(orgName, appName, appVers);
        Assert.True(false);
      }
      catch (AggregateException e)
      {
        if (e.GetBaseException().GetType() == typeof(HttpException))
        {
          Assert.True(true);
        }
        else
        {
          if (e.GetBaseException().GetType() != typeof(HttpException))
          {
            Assert.True(false);
          }
        }
      }
    }
    #endregion
  }
}
