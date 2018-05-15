using DocsChain.ModelsChain;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DocsChain.Services
{
    public class MainLoop : IHostedService
    {
        private readonly ILogger<NetworkManager> _logger;
        private readonly ICore _chainService;
        private readonly INetworkManager _network;
        private readonly IConfiguration _configuration;

        public MainLoop(ILogger<NetworkManager> logger,
            ICore core, 
            INetworkManager networkManager, 
            IConfiguration configuration
            ) {
            _logger = logger;
            _chainService = core;
            _network = networkManager;
            _configuration = configuration;
            _logger.LogInformation("MainLoop Constructed");
            
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("MainLoop Started");
            var bootUrl = _configuration["boot"];
            if (!String.IsNullOrWhiteSpace(bootUrl))
            {
                var myNode = _chainService.GetMyChainCredentials();
                //Fill my node from configuration
                myNode.Endpoint = _configuration["NodeIdentification:IPEndpoint"];
                IEnumerable<NetworkNode> remoteNodes = _network.CallWelcomeNode(bootUrl, myNode).Result;
                //Add remote credentials to my list
                foreach (var node in remoteNodes)
                {
                    _network.AddNode(node);
                }
                NetworkNode bootNode = new NetworkNode() {
                    AccessKey="",
                    Description="",
                    Endpoint= bootUrl
                };
                
                //Get Chain
                IEnumerable<DataBlock> chainBlocks = _network.CallGetNodesList(bootNode).Result;
                //Replace my Chain Nodes
                _chainService.BootstrapNodes(chainBlocks);
                _logger.LogInformation($"Rebuilding local blockchain");
                //Download Chain to local folder
                foreach (var block in chainBlocks)
                {
                    _logger.LogInformation($"Downloading DocsChain Desc {block.Description}");
                    _logger.LogInformation($"Downloading DocsChain Node {block.Index}");
                    //Download Data from the remote server
                    var bytesToStore = _network.CallGetDataBlockBytes(block.Index, bootNode).Result;
                    _logger.LogInformation($"{bytesToStore.Length / 1024} KBytes downloaded");
                    _chainService.StoreBlockBytesToDisk(block.Guid, bytesToStore);
                }
                //Validate The chain
                _logger.LogInformation("Validating downloaded DocsChain...");
                _chainService.CheckFullChainIntegrity();
            }

            var createtestchain = _configuration["create-test-chain"];
            if (!String.IsNullOrWhiteSpace(createtestchain))
            {
                _chainService.AddTestDocuments(3);              
            }


            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("MainLoop Ended");
           
            return Task.CompletedTask;
        }

        protected async Task ExecuteAsync(CancellationToken stoppingToken) {
            _logger.LogInformation("Execute Async");
          do
            {
                _logger.LogInformation("Loop");

                await Task.Delay(5000, stoppingToken); //5 seconds delay
            }
            while (!stoppingToken.IsCancellationRequested);
        }

        
    }
}
