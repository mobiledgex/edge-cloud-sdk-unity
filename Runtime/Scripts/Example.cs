using UnityEngine;
using MobiledgeX;
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
        MobiledgeXIntegration mobiledgeXIntegration = new MobiledgeXIntegration();

#if UNITY_EDITOR
        mobiledgeXIntegration.useWifiOnly(true);
#endif

        wsClient = new MobiledgeXSocketClient(mobiledgeXIntegration);
        if (wsClient.isOpen())
        {
            wsClient.Dispose();
            wsClient = new MobiledgeXSocketClient(mobiledgeXIntegration);
        }
       await wsClient.Connect("?roomid=testing&pName=Ahmed&pCharacter=2");
      
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
        MobiledgeXIntegration integration = new MobiledgeXIntegration();

#if UNITY_EDITOR
        integration.useWifiOnly(true);
#endif

        string uri =  await integration.GetURI();
        Debug.Log("uri is " + uri);
        RestURIText.text = uri; 
    }
 
    #endregion

    // test case
    private async void OnEnable()
    {
        await RestExample(); 
    }
}
