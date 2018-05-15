using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using DocsChain.ModelsChain;
using DocsChain.ModelsWeb;
using DocsChain.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DocsChain.Controllers
{
    
    public class WebAppController : Controller
    {
        private readonly ILogger<ChainController> _logger;
        private readonly ICore _chainService;
        private readonly INetworkManager _networkManager;
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _accessor;

        public WebAppController(
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

        public IActionResult Index()
        {
            //Retrieve current BlockChain
            var blockList = _chainService.GetBlocksList();

            return View(blockList);
        }

        public IActionResult System()
        {
            return View();
        }

        public IActionResult NodeStatus()
        {
            NodeStatusViewModel model = new NodeStatusViewModel();
             model.Nodes = _networkManager.GetAllNetworkNodes().ToList();
            model.Chain = _chainService.GetFullChain();
            return View(model);
        }

        public IActionResult CreateTestChain()
        {
            _chainService.AddTestDocuments(3);
            return RedirectToAction("Index");
        }


        public IActionResult ResetChain()
        {
            _chainService.Bootstrap(true);
            return RedirectToAction("Index");
        }

        public IActionResult BootFromUrl(string bootUrl) {

            bootUrl = $"http://{bootUrl}/";
            if (!String.IsNullOrWhiteSpace(bootUrl))
            {
                var myNode = _chainService.GetMyChainCredentials();
                //Fill my node from configuration
                myNode.Endpoint = _configuration["NodeIdentification:IPEndpoint"];
                IEnumerable<NetworkNode> remoteNodes = _networkManager.CallWelcomeNode(bootUrl, myNode).Result;
                //Add remote credentials to my list
                foreach (var node in remoteNodes)
                {
                    _networkManager.AddNode(node);
                }
                NetworkNode bootNode = new NetworkNode()
                {
                    AccessKey = "",
                    Description = "",
                    Endpoint = bootUrl
                };

                //Get Chain
                IEnumerable<DataBlock> chainBlocks = _networkManager.CallGetNodesList(bootNode).Result;
                //Replace my Chain Nodes
                _chainService.BootstrapNodes(chainBlocks);
                _logger.LogInformation($"Rebuilding local blockchain");
                //Download Chain to local folder
                foreach (var block in chainBlocks)
                {
                    _logger.LogInformation($"Downloading DocsChain Desc {block.Description}");
                    _logger.LogInformation($"Downloading DocsChain Node {block.Index}");
                    //Download Data from the remote server
                    var bytesToStore = _networkManager.CallGetDataBlockBytes(block.Index, bootNode).Result;
                    _logger.LogInformation($"{bytesToStore.Length / 1024} KBytes downloaded");
                    _chainService.StoreBlockBytesToDisk(block.Guid, bytesToStore);
                }
                //Validate The chain
                _logger.LogInformation("Validating downloaded DocsChain...");
                _chainService.CheckFullChainIntegrity();
            }
            return RedirectToAction("Index");

        }

        [HttpPost]
        public async Task<IActionResult> Upload(IFormFile formFile)
        {
            var fileName = WebUtility.HtmlEncode(Path.GetFileName(formFile.FileName));
            
            var memoryStream = new MemoryStream();
            formFile.CopyTo(memoryStream);
            memoryStream.Seek(0, SeekOrigin.Begin);
            var binaryReader = new BinaryReader(memoryStream);
            var bytes = binaryReader.ReadBytes((int)memoryStream.Length);
            var newBlock = _chainService.CreateNextBlock(bytes, fileName);
            _networkManager.BroadcastNewBlock(newBlock);
            return RedirectToAction("Index");
        }

        private string GetContentType(string path)
        {
            var types = GetMimeTypes();
            var ext = Path.GetExtension(path).ToLowerInvariant();
            return types[ext];
        }

        private Dictionary<string, string> GetMimeTypes()
        {
            return new Dictionary<string, string>
            {
                {".txt", "text/plain"},
                {".pdf", "application/pdf"},
                {".doc", "application/vnd.ms-word"},
                {".docx", "application/vnd.ms-word"},
                {".xls", "application/vnd.ms-excel"},
                {".xlsx", "application/vnd.openxmlformatsofficedocument.spreadsheetml.sheet"},
                {".png", "image/png"},
                {".jpg", "image/jpeg"},
                {".jpeg", "image/jpeg"},
                {".gif", "image/gif"},
                {".csv", "text/csv"}
            };
        }

        [HttpGet]
        public async Task<IActionResult> Download(int Id)
        {
            var nodeBytes = _chainService.GetDataBlockBytes(Id);
            var block = _chainService.GetFullChain().NodesList.Where(x => x.Index == Id).First();
            return File(nodeBytes, GetContentType(block.Description), block.Description);
        }

        [HttpGet]
        public string GetNodeAsBase64(int Id)
        {
            var nodeBytes = _chainService.GetDataBlockBytes(Id);

            return Convert.ToBase64String(nodeBytes);
        }
    }
}