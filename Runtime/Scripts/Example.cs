using System;
using UnityEngine;
using MobiledgeX;
using DistributedMatchEngine;
using System.Threading.Tasks;
using UnityEngine.UI;

public class Example : MonoBehaviour
{

    #region Websocket Example using MobiledgeX
    MobiledgeXSocketClient wsClient;

    public Text RestURIText;
    public Text WebSocketText;
    public Button StartWebSocketButton;

    private void Start()
    {
        StartWebSocketButton.onClick.AddListener(StartWebSocket);
    }

    async void StartWebSocket()
    {
        MobiledgeXIntegration mxi = new MobiledgeXIntegration();

#if UNITY_EDITOR
        mxi.UseWifiOnly(true);
#endif

        wsClient = new MobiledgeXSocketClient(mxi);
        if (wsClient.isOpen())
        {
            wsClient.Dispose();
            wsClient = new MobiledgeXSocketClient(mxi);
        }

        String url = await MobiledgeXIntegrationWorkflow(mxi, "ws");
        Debug.Log("GetWebsocket url is " + url);

        Uri uri = new Uri(url);
        await wsClient.Connect(uri);
    }

    // Update is called evey frame
    private void Update()
    {
        if (wsClient == null)
        {
            return;
        }
        var cqueue = wsClient.receiveQueue;
        string msg;
        while (cqueue.TryPeek(out msg))
        {
            cqueue.TryDequeue(out msg);
            WebSocketText.text += msg;
            print(msg);
        }
    }
    #endregion

    #region Rest Example using MobiledgeX
    async Task RestExample()
    {
        MobiledgeXIntegration mxi = new MobiledgeXIntegration();
        CarrierInfoClass ci = new CarrierInfoClass();
        await ci.IsRoaming(-121.243, 37.443);

#if UNITY_EDITOR
        mxi.UseWifiOnly(true);
#endif
        String url = await MobiledgeXIntegrationWorkflow(mxi, "http");

        Debug.Log("RestExample url is " + url);
    }
    #endregion

    // test case
    private async void OnEnable()
    {
        await RestExample();
    }

    private async Task<String> MobiledgeXIntegrationWorkflow(MobiledgeXIntegration integration, String proto)
	{
        // RegisterAndFindCloudlet
        bool registeredAndFoundCloudlet;
        try
        {
            registeredAndFoundCloudlet = await integration.RegisterAndFindCloudlet();
        }
        catch (RegisterClientException rce)
        {
            Debug.LogError("RegisterClientException: " + rce.Message + ". Make sure OrgName, AppName, and AppVers are correct.");
            return null;
        }
        catch (FindCloudletException fce)
        {
            Debug.LogError("FindCloudletException: " + fce.Message + ". Make sure you have an app instance deployed to your region and carrier network");
            return null;
        }
        catch (DmeDnsException dde)
        {
            Debug.LogError("Unable to connect to DME to make RegisterClient call. Exception: " + dde.Message + ". Make sure MobiledgeX supports your carrier.");
            return null;
        }
        catch (NotImplementedException nie)
        {
            Debug.LogError("NotImplementedException: " + nie.Message); // This should not occur, since the constructor supplies the Integration classes
            return null;
        }
        if (!registeredAndFoundCloudlet)
        {
            Debug.LogError("Unable to register and find cloudlet");
            return null;
        }

        // GetAppPort
        AppPort appPort;
        try
        {
            appPort = integration.GetAppPort(LProto.L_PROTO_TCP);
        }
        catch (AppPortException ape)
        {
            Debug.LogError("Unabled to get AppPort. AppPortException: " + ape.Message);
            return null;
        }
        if (appPort == null)
        {
            Debug.LogError("GetAppPort returned null");
            return null;
        }

        // GetUrl
        string url;
        try
        {
            url = integration.GetUrl("http");
        }
        catch (GetConnectionException gce)
        {
            Debug.Log("Unabled to get url. GetConnectionException " + gce.Message);
            return null;
        }

        return url;
	}
}
