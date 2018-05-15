using DocsChain.ModelsChain;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace DocsChain.Services
{
    public class WebApi
    {
        private WebApi(string value) { Value = value; }

        public string Value { get; set; }

        public static WebApi GetNodesList { get { return new WebApi("/chain/GetNodesList"); } }
        public static WebApi WelcomeNode { get { return new WebApi("/chain/WelcomeNode"); } }
        public static WebApi NetworkUpdate { get { return new WebApi("/chain/NetworkNodesUpdate"); } }
        public static WebApi GetDataBlockBytes { get { return new WebApi("/chain/GetDataBlockBytes/"); } }
        public static WebApi AddChainBlock { get { return new WebApi("/chain/AddChainBlock"); } }
        

    }


    public class NetworkManager : INetworkManager
    {
        private readonly ILogger<NetworkManager> _logger;
        private readonly IConfiguration _configuration;


        private readonly string NodesFileName = "DataFiles\\DocsChainNodes.dc";

        private List<NetworkNode> NetworkNodes;




        public NetworkManager(
            ILogger<NetworkManager> logger,
            IConfiguration Configuration
            )
        {
            _logger = logger;
            _configuration = Configuration;
            NodesFileName = $"{_configuration["Storage:ConfigFolder"]}\\{_configuration["Storage:NetworkNodesFilename"]}";
            _logger.LogWarning("Initiating Network Manager");

            this.LoadNodes();
        }

        #region Utilities

        public NetworkNode GetNodeByUrl(string url) {
            return NetworkNodes.First(x=>x.Endpoint==url);
        }

        public IEnumerable<NetworkNode> GetAllNetworkNodes()
        {
            return NetworkNodes;
        }

        public bool LoadNodes()
        {
            //Try to load Nodes from disk
            if (File.Exists(NodesFileName))
            {
                _logger.LogInformation($"Loading network nodes from file {NodesFileName}:");
                var theNodesFile = File.ReadAllBytes(NodesFileName);
                NetworkNodes = (List<NetworkNode>)Utility.ByteArrayToObject(theNodesFile);
                int index = 1;
                foreach (var node in NetworkNodes)
                {
                    _logger.LogInformation($"----------Node #{index} -------------------------");
                    _logger.LogInformation($"Access Key:{node.AccessKey}");
                    _logger.LogInformation($"Description:{node.Description}");
                    _logger.LogInformation($"Endpoint:{node.Endpoint}");
                    _logger.LogInformation("--------------------------------------------------");
                    index++;
                }
            }
            else
            {
                _logger.LogWarning("No nodes founds on file, adding myself to nodes");
                NetworkNode myselfNode = new NetworkNode();

                myselfNode.Description = _configuration["NodeIdentification:Name"];
                myselfNode.Endpoint = _configuration["NodeIdentification:IPEndpoint"];
                AddNode(myselfNode);
            }

            return true;
        }
                
        //Node is added only when a response from a remote party is detected
        public bool AddNode(NetworkNode networkNode)
        {
            _logger.LogInformation($"Request to Add {networkNode.Endpoint}");
            if (NetworkNodes == null) NetworkNodes = new List<NetworkNode>();

            //Don't add myself to the list or I'll loop, skip existing items
            if (NetworkNodes.FirstOrDefault(x => x.Endpoint == networkNode.Endpoint) == null)
            {
                _logger.LogInformation($"adding New Node {networkNode.Endpoint}:{networkNode.AccessKey}");
                NetworkNodes.Add(networkNode);
                this.DumpNodesToDisk();
            }
            else
            {
                _logger.LogWarning("Node yet present");
            }


            return true;

        }


        private void DumpNodesToDisk()
        {
            //Write down to file
            var bytesToWrite = Utility.ObjectToByteArray(NetworkNodes);
            File.WriteAllBytesAsync(NodesFileName, bytesToWrite).Wait();
        }

        #endregion

        #region RemoteCalls

        public async Task<IEnumerable<NetworkNode>> CallWelcomeNode(string remoteUrl, NetworkNode node)
        {
            HttpClient client = new HttpClient();
            IEnumerable<NetworkNode> remoteNode = null;
            client.BaseAddress = new Uri(remoteUrl); //"http://localhost:64195/"
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            try
            {
                HttpResponseMessage response = await client.PostAsync(WebApi.WelcomeNode.Value, new StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(node), System.Text.Encoding.UTF8, "application/json"));
                response.EnsureSuccessStatusCode();

                if (response.IsSuccessStatusCode)
                {
                    var responseString = await response.Content.ReadAsStringAsync();
                    //Transform JSON to object
                    remoteNode = JsonConvert.DeserializeObject<IEnumerable<NetworkNode>>(responseString);
                }
            }
            catch (Exception e)
            {
                _logger.LogCritical(e, "Cannot call remote Node");
            }
            return remoteNode;

        }

        public async Task<bool> CallAddChainBlock(string remoteUrl, DataBlock block) {
            HttpClient client = new HttpClient();
            bool? remoteResponse = null;
            client.BaseAddress = new Uri(remoteUrl); //"http://localhost:64195/"
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            try
            {
                HttpResponseMessage response = await client.PostAsync(WebApi.AddChainBlock.Value, new StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(block), System.Text.Encoding.UTF8, "application/json"));
                response.EnsureSuccessStatusCode();

                if (response.IsSuccessStatusCode)
                {
                    var responseString = await response.Content.ReadAsStringAsync();
                    //Transform JSON to object
                    remoteResponse = JsonConvert.DeserializeObject<Boolean>(responseString);
                }
            }
            catch (Exception e)
            {
                _logger.LogCritical(e, "Cannot call remote Node");
            }
            

            return Convert.ToBoolean(remoteResponse);
        }

        public async Task<IEnumerable<DataBlock>> CallGetNodesList(NetworkNode node)
        {
            HttpClient client = new HttpClient();
            IEnumerable<DataBlock> blocks = null;
            client.BaseAddress = new Uri(node.Endpoint); //"http://localhost:64195/"
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            try
            {
                HttpResponseMessage response = await client.GetAsync(WebApi.GetNodesList.Value);
                if (response.IsSuccessStatusCode)
                {
                    var responseString = await response.Content.ReadAsStringAsync();
                    //Transform JSON to object
                    blocks = JsonConvert.DeserializeObject<IEnumerable<DataBlock>>(responseString);
                }
            }
            catch (Exception e)
            {
                _logger.LogCritical(e, "Cannot call remote Node");
            }
            return blocks;

        }

        public async void BroadcastNewBlock(DataBlock newBlock) {
            foreach (var node in NetworkNodes.Where(x => x.Endpoint != _configuration["NodeIdentification:IPEndpoint"]))
            {
                await CallAddChainBlock(node.Endpoint, newBlock);
            }
        }


        public async Task<byte[]> GetDataBlockFromRandomNode(int Id) {
            Random rnd = new Random();
            var nodesWithoutMe = NetworkNodes.Where(x => x.Endpoint != _configuration["NodeIdentification:IPEndpoint"]);
                     
            while (true) {
                var randomIndex = rnd.Next(nodesWithoutMe.Count());
                var randomNetworkNode = nodesWithoutMe.OrderBy(item => rnd.Next()).ToList().First();
                var bytes = await CallGetDataBlockBytes(Id, randomNetworkNode);
                if (bytes.Length > 0) {
                    return bytes;
                }
                Thread.Sleep(1000);
            }
        }

        public async Task<byte[]> CallGetDataBlockBytes(int Id, NetworkNode node)
        {
            HttpClient client = new HttpClient();
            byte[] blockBytes = null;
            client.BaseAddress = new Uri(node.Endpoint); //"http://localhost:64195/"
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            try
            {
                var url = WebApi.GetDataBlockBytes.Value + $"{Id}";
                HttpResponseMessage response = await client.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var responseString = await response.Content.ReadAsStringAsync();
                    //Transform JSON to object
                    blockBytes = JsonConvert.DeserializeObject<byte[]>(responseString);
                }
            }
            catch (Exception e)
            {
                _logger.LogCritical(e, "Cannot call remote Node");
            }
            return blockBytes;
        }

        public async Task<bool> CallNetworkNodesUpdate()
        {
            HttpClient client = new HttpClient();
            foreach (var node in NetworkNodes.Where(x=>x.Endpoint != _configuration["NodeIdentification:IPEndpoint"]))
            {
                _logger.LogInformation($"Sending node list update to {node.Endpoint}");
                client.BaseAddress = new Uri(node.Endpoint); //"http://localhost:64195/"
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                try
                {
                    HttpResponseMessage response = await client.PostAsync(WebApi.NetworkUpdate.Value, new StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(NetworkNodes), System.Text.Encoding.UTF8, "application/json"));
                    response.EnsureSuccessStatusCode();

                    if (response.IsSuccessStatusCode)
                    {
                        var responseString = await response.Content.ReadAsStringAsync();
                        //Transform JSON to object
                        var callResponse = JsonConvert.DeserializeObject<bool>(responseString);
                        _logger.LogInformation($"Node answer is Received={callResponse}");
                    }
                }
                catch (Exception e)
                {
                    _logger.LogCritical(e, $"Cannot call remote Node {node.Endpoint}");
                }

            }
            return true;
        }

        #endregion


    }
}
