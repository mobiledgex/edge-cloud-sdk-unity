/**
 * Copyright 2018-2022 MobiledgeX, Inc. All rights and licenses reserved.
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

using System.Collections;
using UnityEngine;
using MobiledgeX;
using DistributedMatchEngine;
using System.Threading.Tasks;

[RequireComponent(typeof(MobiledgeX.LocationService))]
public class ExampleQoS : MonoBehaviour
{
  MobiledgeXIntegration mxi;

  IEnumerator Start()
  {
    yield return StartCoroutine(MobiledgeX.LocationService.EnsureLocation());
    GetEdgeConnection();

  }

  async void GetEdgeConnection()
  {
    mxi = new MobiledgeXIntegration();
    try
    {
      await mxi.RegisterAndFindCloudlet();
    }
    catch (RegisterClientException rce)
    {
      Debug.Log("RegisterClientException: " + rce.Message + "Inner Exception: " + rce.InnerException);
      mxi.UseWifiOnly(true);
      await mxi.RegisterAndFindCloudlet();
    }
    catch (FindCloudletException fce)
    {
      Debug.Log("FindCloudletException: " + fce.Message + "Inner Exception: " + fce.InnerException);
    }
    mxi.GetAppPort(LProto.Tcp);
    string url = mxi.GetUrl("http");
    Debug.Log("url : " + url);


    //Creates QoS Session
    await CreateQoSSession();
  }

  private async Task<bool> CreateQoSSession()
  {
    QosPrioritySessionCreateRequest qoSRequest = mxi.matchingEngine.CreateQosPriorityCreateRequest(QosSessionProfile.LowLatency, mxi.FindCloudletReply, "5000", "3000");
    QosPrioritySessionCreateReply qoSReply = await mxi.matchingEngine.CreateQOSPrioritySession(qoSRequest);
    Debug.Log(qoSReply.ToString());
    if (qoSReply.http_status == 200 || qoSReply.http_status == 201) return true;
    else return false;
  }

  private async Task<bool> DeleteQoSSession()
  {
    QosPrioritySessionDeleteRequest deleteRequest = mxi.matchingEngine.CreateQosPriorityDeleteRequest(QosSessionProfile.LowLatency);
    QosPrioritySessionDeleteReply deleteReply = await mxi.matchingEngine.DeleteQOSPrioritySession(deleteRequest);
    Debug.Log(deleteReply.ToString());
    if (deleteReply.status == DeleteStatus.Deleted) return true;
    else return false;
  }

  void OnDestroy()
  {
    mxi.Dispose();
  }
}
