﻿using System.Collections;
using UnityEngine;
using MobiledgeX;
using System.Text;
using DistributedMatchEngine;

[RequireComponent(typeof(MobiledgeX.LocationService))]
public class ExampleUDP : MonoBehaviour
{
    MobiledgeXIntegration mxi;
    MobiledgeXUDPClient udpClient;
    string udpHost; // the UDP url for your app instance
    int udpSendPort; // the public UDP port of your app instance

    IEnumerator Start()
    {
        yield return StartCoroutine(MobiledgeX.LocationService.EnsureLocation());
        GetEdgeConnection();
    }

    async void GetEdgeConnection()
    {
        mxi = new MobiledgeXIntegration(FindObjectOfType<EdgeEventsManager>());
        mxi.NewFindCloudletHandler += HandleFindCloudlet;
        await mxi.RegisterAndFindCloudlet();
        udpSendPort = mxi.GetAppPort(LProto.Udp).PublicPort;
        udpHost = mxi.GetHost();
        Debug.Log("UDP HOST : " + udpHost);
        Debug.Log("UDP PORT : " + udpSendPort);
        SendUDPMessage("Hi, From a UDP Client to the UDP Server");
    }

    void SendUDPMessage(string message)
    {
        udpClient = new MobiledgeXUDPClient(udpHost, udpSendPort);
        udpClient.Send(message);
      
        //You can send binary also
        //byte[] messageBinary = Encoding.ASCII.GetBytes(message);
        //udpClient.Send(messageBinary);
    }
    
    private void HandleFindCloudlet(EdgeEventsStatus edgeEventstatus, FindCloudletEvent fcEvent)
    {
       print("NewFindCloudlet triggered status is " + edgeEventstatus.status + ", Trigger" + fcEvent.trigger);
        if (fcEvent.newCloudlet != null)
        {
            print("New Cloudlet FQDN: " + fcEvent.newCloudlet.Fqdn);
        }
        if (edgeEventstatus.status == Status.error)
        {
            print("Error received: " + edgeEventstatus.error);
        }
    }
    void Update()
    {
        if (udpClient == null)
        {
            return;
        }
        //udp receive queue
        byte[] udpMsg;
        var udp_queue = udpClient.receiveQueue;
        while (udp_queue.TryPeek(out udpMsg))
        {
            udp_queue.TryDequeue(out udpMsg);
            string udpReceivedMsg = Encoding.UTF8.GetString(udpMsg);
            print("Received UDP Message : " + udpReceivedMsg);
        }
    }

    private void OnDestroy()
    {
        mxi.Dispose();
    }
}
