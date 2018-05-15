using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DocsChain.ModelsChain;
using DocsChain.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DocsChain.Controllers
{
    //[Route("api/[controller]")]
    //example http://localhost:5000/chain/GetNodesList
    // or  http://localhost:5000/chain/GetDataBlock/11

    [Route("[controller]")]
    public class ChainController : Controller
    {
        private readonly ILogger<ChainController> _logger;
        private readonly ICore _chainService;
        private readonly INetworkManager _networkManager;
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _accessor;

        public ChainController(
            ILogger<ChainController> logger, 
            ICore core, 
            INetworkManager networkManager, 
            IConfiguration configuration,
            IHttpContextAccessor accessor
            )
        {
            _logger = logger;
            _chainService = core;
            _networkManager = networkManager;
            _configuration = configuration;
            _accessor = accessor;
        }


        /// <summary>
        /// Receive a new node and send back my description as welcome.
        /// Add Node to the list of known nodes
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        [HttpPost("WelcomeNode")]
        public IEnumerable<NetworkNode> WelcomeNode([FromBody]NetworkNode receivedNode)
        {
            var nodesList = _networkManager.GetAllNetworkNodes();
            //Add received node to my personal peer list
            _networkManager.AddNode(receivedNode);
            //Broadcast my new list to known nodes except the one that just called me
            _networkManager.CallNetworkNodesUpdate();
            //return my additional Info
            return nodesList;

        }

        [HttpPost("NetworkNodesUpdate")]
        public bool NetworkNodesUpdate([FromBody] IEnumerable<NetworkNode> networkNodes)
        {
            var connection = _accessor.HttpContext.Connection;
            _logger.LogWarning($"Received network nodes update from {connection.RemoteIpAddress}:{connection.RemotePort}");            
            foreach (var node in networkNodes)
            {
                _networkManager.AddNode(node);
            }
            return true;
        }

        // GET api/values
        [HttpGet("GetNodesList")]
        public IEnumerable<DataBlock> GetNodesList()
        {
            var blockList = _chainService.GetBlocksList();
            return blockList;
        }

        // GET api/values
        [HttpPost("AddChainBlock")]
        public async Task<bool> AddChainBlock([FromBody] DataBlock block)
        {
            _logger.LogInformation("Received new Data Block");
            //pick another node for download       
            if (block.Index == 0) throw new Exception("Block Index 0 is invalid");

            var bytes = await _networkManager.GetDataBlockFromRandomNode(block.Index);
            //Add Block to Chain
            _chainService.StoreReceivedBlock(block,bytes);
            //Verify Chain Integrity
            _chainService.CheckFullChainIntegrity();            
            return true;
        }

        [HttpGet("GetDataBlockBytes/{Id}")]
        public Byte[] GetDataBlockBytes(int Id)
        {
            return _chainService.GetDataBlockBytes(Id); ;
        }
      
    }
}