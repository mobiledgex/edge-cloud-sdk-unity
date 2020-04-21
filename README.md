<p align="center">
  <img src="https://developers.mobiledgex.com/site/themes/developers/img/logo.svg?v=1582820109" alt="Logo" width="100" height="100">

  <h3 align="center">MobiledgeX Unity SDK</h3>

  <p align="center">
  Add Edge Computing to your Unity Games and Applications.
    <br />
    <a href="https://developers.mobiledgex.com/sdk-libraries/unity-sdk"><strong>Explore the docs »</strong></a>
    <br />
    <br />
    <a href="https://github.com/mobiledgex/edge-cloud-sampleapps">View Edge Sample Apps</a>
    ·
    <a href="https://github.com/mobiledgex/edge-cloud-sdk-unity/issues/new">Report Bug</a>
    ·
    <a href="https://github.com/mobiledgex/edge-cloud-sdk-unity/issues/new">Request Feature</a>
  </p>
</p>



<!-- TABLE OF CONTENTS -->
## Table of Contents

* [Getting Started](#getting-started)
  * [Prerequisites](#prerequisites)
  * [Installation](#installation)
* [Usage](#usage)
* [Roadmap](#roadmap)
* [Contributing](#contributing)
* [License](#license)
* [Contact](#contact)
* [Acknowledgements](#acknowledgements)


<!-- GETTING STARTED -->
## Getting Started
MobiledgeX Inc. is building a marketplace of edge resources and services that will connect developers with the world’s largest mobile networks to power the next generation of applications and devices. MobiledgeX is an edge computing company founded by Deutsche Telekom AG and headquartered in San Francisco, California.

### Prerequisites

This SDK works on Unity Editor 2018.4.19f1 (LTS) and higher.


### Installation

1. Create an Account on MobiledgeX Console [https://console.mobiledgex.net](https://console.mobiledgex.net)
2. Upload your server/backend to MobiledgeX Console using(github actions/ MobiledgeX Console) 
3. If you have Unity 2019 or higher use Unity Package Manager, Add Package using git url 

4. If you have Unity 2018, Please clone the repo and add it to your Unity Project


5. Editor Step
(Enter Organization Name, Application Name, Application Version)

6. MobiledgeX Setting
(Click on Mobiledgex Settings to Highlight Mobiledgex Settings Object)

##### Known Issues : Make sure to Update Unity Ads Package to (3.4.4) or Remove it.
##### Location Premissions is needed to connect to the closest Edge Cloudlet according to your location 

<!-- USAGE EXAMPLES -->
## Usage

```
using MobiledgeX;
using System.Threading.Tasks;
```


For Restful Connection

` string url = await MobiledgeXIntegration.GetRestURI();`


For Websocket Connection

```
    // Create an instance of MobiledgeXSocketClient
    MobiledgeXSocketClient wsClient;
    // Start a websocket using an instance of MobiledgeXSocketClient
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
    
    // Check wsClient 
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
```


_For more examples, please refer to the [Edge Sample Apps](https://github.com/mobiledgex/edge-cloud-sampleapps/)_



<!-- ROADMAP -->
## Roadmap

Add or See the [open issues](https://github.com/mobiledgex/edge-cloud-sdk-unity/issues) for a list of proposed features (and known issues).



<!-- CONTRIBUTING -->
## Contributing

Contributions are what make the open source community such an amazing place to be learn, inspire, and create. Any contributions you make are **greatly appreciated**.

1. Fork the Project
2. Create your Feature Branch (`git checkout -b feature/AmazingFeature`)
3. Commit your Changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the Branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request



<!-- LICENSE -->
## License




<!-- CONTACT -->
## Contact





<!-- ACKNOWLEDGEMENTS -->
## Acknowledgements
