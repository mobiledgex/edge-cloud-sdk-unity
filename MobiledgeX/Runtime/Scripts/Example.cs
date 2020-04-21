using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using MobiledgeX;
using System.Threading.Tasks;

public class Example : MonoBehaviour
{
    #region Websocket Example using MobiledgeX
    MobiledgeXSocketClient wsClient;
    async void StartWebSocket()
    {
        wsClient = new MobiledgeXSocketClient();
        if (wsClient.isOpen())
        {
            wsClient.Dispose();
            wsClient = new MobiledgeXSocketClient();
        }
       await wsClient.Connect("?roomid=testt");
        wsClient.Send("msg");
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
            print(msg);
        }
    }
    #endregion

    #region Rest Example using MobiledgeX
    async Task RestExample()
    {
    string url = await MobiledgeXIntegration.GetRestURI();
     //WWWForm form = new WWWForm();
     //form.AddField("myField", "myData");
     //StartCoroutine(SendPostRequest(url,form));
    }
    IEnumerator SendPostRequest(string url, WWWForm form)
    {
        using (UnityWebRequest www = UnityWebRequest.Post(url, form))
        {
            yield return www.SendWebRequest();

            if (www.isNetworkError || www.isHttpError)
            {
                Debug.Log(www.error);
            }
            else
            {
                Debug.Log(www.downloadHandler.text);
                Debug.Log("Form upload complete!");
            }
        }
    }
    #endregion

    // test case
    private async void OnEnable()
    {
        await RestExample(); 
    }
}
