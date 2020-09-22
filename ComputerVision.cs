
using UnityEngine;
using System;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Linq;
using MobiledgeX;
using DistributedMatchEngine;
using System.Collections;
using UnityEngine.Networking;
using System.Text;
using System.Runtime.Serialization;
using System.IO;
using System.Runtime.Serialization.Json;

[RequireComponent(typeof(MobiledgeX.LocationService))]
[AddComponentMenu("MobiledgeX/ComputerVision")]
public class ComputerVision : MonoBehaviour
{
    public ConnectionMode connectionMode;
    public ServiceMode serviceMode = ServiceMode.ObjectDetection;

    [Tooltip("The image will be resized before sending it to the server respecting the image aspect ratio. Image Shrinking Ratio is proportional to Detection Quality &" +
        "inversely proportional to the Full Detection Process Latency. Image Shrinking Ratio is between (0.1 to 0.4)")]
    [Range(0.1f, 0.4f)]
    public float imageShrinkingRatio = 0.15f;

    [Header("Face Detection")]
    [Tooltip("Texture to be added on top of detected faces")]
    public Texture faceRectTexture;

    [Header("Object Detection")]
    [Tooltip("For Object Detection, Texture to be added on top of detected objects with Confidence level higher than the confidence Threshold ")]
    public Texture highConfidenceTexture;

    [Tooltip("For Object Detection, Texture to be added on top of detected objects with Confidence level lower than the confidence Threshold ")]
    public Texture lowConfidenceTexture;

    public Font objectClassFont;
    [Tooltip("Wether to show the confidence level next to the detected object or not, ex.(car 100%)")]
    public bool showConfidenceLevel = true;

    [Tooltip("For object detection, the font of the object class font")]
    public int objectClassFontSize = 30;

    [Tooltip("For object detection, color of the object class font if the confidence level is above or equal the confidence threshold")]
    public Color highConfidenceFontColor = Color.green;

    [Tooltip("For object detection, color of the object class font if the confidence level is below the confidence threshold")]
    public Color lowConfidenceFontColor = Color.red;

    public int confidenceThreshold = 1;

    public enum ConnectionMode
    {
        Rest,
        WebSocket
    }

    public enum ServiceMode
    {
        FaceDetection,
        ObjectDetection
    }

    MobiledgeXWebSocketClient client;
    MobiledgeXIntegration integration;
    /// <summary>
    /// Hide Detection rects before taking a screenshot
    /// </summary>
    static bool showGUI;
    int[][] faceDetectionRects;
    static @Object[] objectsDetected;


    bool serviceAlreadyStarted
    {
        get
        {
            switch (serviceMode)
            {
                case ServiceMode.ObjectDetection:
                    return objectsDetected == null ? false : true;
                case ServiceMode.FaceDetection:
                    return faceDetectionRects == null ? false : true;
                default:
                    return false;
            }
        }
    }

    /// <summary>
    /// For more info about the computer vision server and the url suffix check MobiledgeX docs
    /// for Rest https://developers.mobiledgex.com/guides-and-tutorials/computer-vision/how-to-computer-vision-api
    /// for WebSockets https://mobiledgex.com/blog/2020/04/22/computer-vision-websocket-support
    /// </summary>
    string urlSuffix
    {
        get
        {
            switch (serviceMode)
            {
                case ServiceMode.ObjectDetection:
                    return "/object/detect/";
                case ServiceMode.FaceDetection:
                    return "/detector/detect/";
                default:
                    return "";
            }
        }
    }

    /// <summary>
    /// The complete url retreived from FindCloudlet+ url suffix
    /// </summary>
    static string url;
    static bool urlFound; // trigger indicating RegisterAndFindCloudlet, GetAppPort() & GetUrl() occured
    bool webRequestsLock = false; // to control the flow of sending web requests
    bool wsStarted; // trigger indicating wether websocket connection have been intialized or not



    #region Monobehaviour callbacks
    IEnumerator Start()
    {
#if UNITY_EDITOR
        GetEdgeConnection();
        yield break;
#endif
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            // Handle failure due no internet connection
            throw new Exception("No Internet Connection");
        }
        else 
        {
            // MobiledgeX uses location for finding the closest cloudlet to the user
            yield return StartCoroutine(MobiledgeX.LocationService.EnsureLocation());
            GetEdgeConnection();
        }
    }

    void OnGUI()
    {
        // show Detection Rects after the screen shot have been taken and the server have responded
        if (showGUI)
        {
            DrawRectangles();
        }
    }

    private void Update()
    {
        if (client == null)
        {
            return;
        }
        var cqueue = client.receiveQueue;
        string msg;
        while (cqueue.TryPeek(out msg))
        {
            cqueue.TryDequeue(out msg);
            HandleServerRespone(msg);
        }

    }
    #endregion

    async Task GetEdgeConnection()
    {
        integration = new MobiledgeXIntegration();
        integration.UseWifiOnly(true);
        
        try
        {
            bool cloudletFound = await integration.RegisterAndFindCloudlet("eu-mexdemo.dme.mobiledgex.net", 38001);
            if (cloudletFound)
            {
                integration.GetAppPort(LProto.L_PROTO_HTTP);
                SetConnection();
                StartCoroutine(ImageSenderFlow());
            }
        }

        catch (Exception e) // In case we don't support the detected carrierName  (In the generated dme)
        {
            Debug.LogError(e + "Error finding Connecting to Edge.");
            throw e;
        }

    }

    public string UriBasedOnConnectionMode()
    {
        AppPort appPort;
        string url;
        switch (connectionMode)
        {
            case ConnectionMode.WebSocket:
                appPort = integration.GetAppPort(LProto.L_PROTO_TCP);
                url = integration.GetUrl("wss");
                return url;
            case ConnectionMode.Rest:
                appPort = integration.GetAppPort(LProto.L_PROTO_TCP);
                url = integration.GetUrl("https");
                return url;
            default:
                return "";
        }
    }

    public async void StartWebSocket(string url)
    {
        Uri uri = new Uri(url);
        client = new MobiledgeXWebSocketClient();
        if (client.isOpen())
        {

            client.Dispose();
            client = new MobiledgeXWebSocketClient();
        }
        await client.Connect(uri);
        wsStarted = true;
    }

    public void SendtoServer(byte[] imgBinary)
    {
        client.Send(imgBinary);
    }

    public IEnumerator SendImageToServer(byte[] imgBinary, string url)
    {
        webRequestsLock = true;
        WWWForm form = new WWWForm();
        form.AddBinaryData("image", imgBinary);
        UnityWebRequest www = UnityWebRequest.Post(url, form);
        www.timeout = 5; // Timeout in seconds
        yield return www.SendWebRequest();

        // isHttpError True on response codes greater than or equal to 400.
        // isNetworkError True on failure to resolve a DNS entry
        if (www.isNetworkError || www.isHttpError)
        {
            Debug.Log("Error sending Image to the server");
            Debug.Log(www.error);
            if (www.responseCode == 503)
            {
                Debug.Log("Training data update in progress, Sending another request in 2 seconds.");
                yield return new WaitForSeconds(2);
                StartCoroutine(SendImageToServer(imgBinary, url));
                yield break;
            }
            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                Debug.LogError("Error. Your are not connected to the Internet.");
            }
            else
            {
                yield return new WaitForEndOfFrame();
                StartCoroutine(SendImageToServer(imgBinary, url));
                yield break;
            }
        }
        else
        {
            HandleServerRespone(www.downloadHandler.text);
        }
    }

    public void SetConnection()
    {
        string uri = UriBasedOnConnectionMode();
        switch (connectionMode)
        {
            case ConnectionMode.WebSocket:
                url = uri+"/ws" + urlSuffix;
                StartWebSocket(url);
                urlFound = true;
                break;
            case ConnectionMode.Rest:
                url = uri + urlSuffix;
                urlFound = true;
                break;
            default:
                throw new Exception("Connection mode is not configured, Select REST or WebSocket.");
        }
    }

    /// <summary>`
    /// ImageSenderFlow Flow : Hide the GUI > CaptureScreenShot
    ///  > ShowGUI > Shrink Image >
    ///  Based on Connection Mode > WebSocket Case > Add image binary to the socket queue > OnReceive > Handle Server Response
    ///                           >  Rest Case > Send Image to Server > Handle Server Response > Repeat
    /// </summary>
    IEnumerator ImageSenderFlow()
    {
        showGUI = false;
        yield return new WaitForEndOfFrame();
        int width = Screen.width;
        int height = Screen.height;
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGB24, true);
        texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        showGUI = serviceAlreadyStarted;
        texture.Apply();
        byte[] imgBinary = ShrinkAndEncode(source: texture);
        Destroy(texture);
        while (!urlFound)
        {
            yield return null;
        }
        switch (connectionMode)
        {
            case ConnectionMode.Rest:
                while (webRequestsLock)
                {
                    yield return null;
                }
                StartCoroutine(SendImageToServer(imgBinary, url));
                break;
            case ConnectionMode.WebSocket:
                while (!wsStarted)
                {
                    yield return null;
                }
                SendtoServer(imgBinary);
                break;
        }
    }

    public void HandleServerRespone(string response)
    {
        switch (serviceMode)
        {
            case ServiceMode.FaceDetection:
                FaceDetectionResponse faceDetectionResponse = Messaging<FaceDetectionResponse>.Deserialize(response);
                showGUI = faceDetectionResponse.success;
                if (faceDetectionResponse.success)
                {
                    faceDetectionRects = faceDetectionResponse.rects;
                }
                else
                {
                    faceDetectionRects = null;
                }
                break;

            case ServiceMode.ObjectDetection:
                ObjectDetectionResponse objectDetectionResponse = Messaging<ObjectDetectionResponse>.Deserialize(response);
                showGUI = objectDetectionResponse.success;
                if (objectDetectionResponse.success)
                {
                    objectsDetected = objectDetectionResponse.objects;
                }
                else
                {
                    objectsDetected = null;
                }
                break;
        }
        if (connectionMode == ConnectionMode.Rest)
        {
            webRequestsLock = false;
            StartCoroutine(ImageSenderFlow());
        }
        else // websocket
        {
            StartCoroutine(ImageSenderFlow());
        }
    }

    void DrawRectangles()
    {
        float height = 0;
        float width = 0;
        GUIStyle TextStyle = new GUIStyle();
        switch (serviceMode)
        {
            case ServiceMode.FaceDetection:
                for (int i = 0; i < faceDetectionRects.Length; i++)
                {
                    height = imageShrinkingRatio * (faceDetectionRects[i][3] - faceDetectionRects[i][1]);
                    width = imageShrinkingRatio * (faceDetectionRects[i][2] - faceDetectionRects[i][0]);
                    if (faceRectTexture)
                        GUI.DrawTexture(new Rect(faceDetectionRects[i][0] * imageShrinkingRatio, faceDetectionRects[i][1] * imageShrinkingRatio, width, height), faceRectTexture, ScaleMode.ScaleToFit, true, width / height);
                }
                break;

            case ServiceMode.ObjectDetection:
                foreach (@Object obj in objectsDetected)
                {
                    height =  (obj.rect[3] - obj.rect[1])/ imageShrinkingRatio;
                    width =  (obj.rect[2] - obj.rect[0]) / imageShrinkingRatio;
                    if ((obj.confidence >= confidenceThreshold / 100 && highConfidenceTexture) || (obj.confidence < confidenceThreshold / 100 && lowConfidenceTexture))
                        GUI.DrawTexture(new Rect((obj.rect[0] / imageShrinkingRatio), (obj.rect[1] / imageShrinkingRatio), width, height), obj.confidence >= confidenceThreshold / 100 ? highConfidenceTexture : lowConfidenceTexture, ScaleMode.StretchToFill, true, width / height);
                    
                    TextStyle.font = objectClassFont;
                    TextStyle.normal.textColor = Color.white;
                    TextStyle.fontSize = objectClassFontSize;
                    string objClass = obj.@class + (showConfidenceLevel == true ? " " + obj.confidence * 100 + "%" : "");
                    if (objectClassFont)
                        GUI.Label(new Rect((obj.rect[0] / imageShrinkingRatio) + (width / 2) - (25), (obj.rect[1] / imageShrinkingRatio) + 10, 50, 50), new GUIContent(objClass), TextStyle);
                }
                break;
        }
    }

    /// <summary>
    /// Shrinks the screen shot taken to the supplied TargetWidth, then encode the new texture a JPG format and returns the binary array
    /// Shrinking happens by organizing the pixels of the source img into the new scaled texture using the normalized UV map
    /// </summary>
    /// <param name="source">Screen Shot Texture</param>
    /// <param name="targetWidth"></param>
    /// <returns> shrank image binary </returns>
    byte[] ShrinkAndEncode(Texture2D source)
    {
        int targetHeight = Mathf.FloorToInt(source.height * imageShrinkingRatio);
        int targetWidth = Mathf.FloorToInt(source.width * imageShrinkingRatio);
        Texture2D scaledTex = new Texture2D(targetWidth, targetHeight, source.format, true);
        Color[] pixelsColorArray = scaledTex.GetPixels(0);
        float xRatio = ((float)1 / source.width) * ((float)source.width / targetWidth);
        float yRatio = ((float)1 / source.height) * ((float)source.height / targetHeight);
        for (int px = 0; px < pixelsColorArray.Length; px++)
        {
            pixelsColorArray[px] = source.GetPixelBilinear(xRatio * ((float)px % targetWidth), yRatio * ((float)Mathf.Floor(px / targetWidth)));
        }
        scaledTex.SetPixels(pixelsColorArray, 0);
        scaledTex.Apply();
        byte[] bytes = scaledTex.EncodeToJPG();
        Destroy(scaledTex);
        return bytes;
    }
}

[DataContract]
public class FaceDetectionResponse
{
    [DataMember]
    public bool success;
    [DataMember]
    public float server_processing_time;
    [DataMember]
    public int[][] rects;
}

[DataContract]
public class ObjectDetectionResponse
{
    [DataMember]
    public bool success;
    [DataMember]
    public float server_processing_time;
    [DataMember]
    public bool gpu_support;
    [DataMember]
    public @Object[] objects;
}

[DataContract]
public class @Object
{
    [DataMember]
    public int[] rect;
    [DataMember]
    public string @class;
    [DataMember]
    public float confidence;
}

[DataContract]
public class MessageWrapper
{
    [DataMember]
    public string type = "utf8";
    [DataMember]
    public string utf8Data;
    public static MessageWrapper WrapTextMessage(string jsonStr)
    {
        var wrapper = new MessageWrapper();
        wrapper.utf8Data = jsonStr;
        return wrapper;
    }
    public static MessageWrapper UnWrapMessage(string wrappedJsonStr)
    {
        var wrapper = Messaging<MessageWrapper>.Deserialize(wrappedJsonStr);
        return wrapper;
    }
}

public static class Messaging<T>
{
    public static string StreamToString(Stream s)
    {
        s.Position = 0;
        StreamReader reader = new StreamReader(s);
        string jsonStr = reader.ReadToEnd();
        return jsonStr;
    }
    public static string Serialize(T t)
    {
        DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(T));
        MemoryStream ms = new MemoryStream();
        serializer.WriteObject(ms, t);
        string jsonStr = StreamToString(ms);
        return jsonStr;
    }
    public static T Deserialize(string jsonString)
    {
        MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(jsonString ?? ""));
        return Deserialize(ms);
    }
    public static T Deserialize(Stream stream)
    {
        DataContractJsonSerializer deserializer = new DataContractJsonSerializer(typeof(T));
        T t = (T)deserializer.ReadObject(stream);
        return t;
    }
}
