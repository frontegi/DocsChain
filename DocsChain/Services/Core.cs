using DocsChain.ModelsChain;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace DocsChain.Services
{
    public class Core : ICore
    {
        private readonly ILogger<Core> _logger;
        private readonly IConfiguration _configuration;

        private readonly string ConfigDirectory = "DataFiles";
        private readonly string DataFileFolder = "";
        private readonly string ChainFileName = $"DataFiles\\DocsChain.dc";
        private const string GENESIS_BLOCK_DATA = "Genesis Block";

        private BlockChain MyChain = new BlockChain();

        

        public Core(ILogger<Core> logger,
            IConfiguration Configuration
            )
        {
            _configuration = Configuration;
            _logger = logger;

            //Load Configuration
            ConfigDirectory = _configuration["Storage:ConfigFolder"];
            ChainFileName = $"{_configuration["Storage:ConfigFolder"]}\\{_configuration["Storage:BlochChainFileName"]}";
            DataFileFolder = _configuration["Storage:DataFileFolder"];

            if (MyChain.NodesList == null)
            {
                Bootstrap();
                CheckFullChainIntegrity();
            }
        }

        public bool Bootstrap()
        {
            return Bootstrap(false);
        }


        public bool Bootstrap(bool ResetChain)
        {
            if (ResetChain) {  
                Directory.Delete(_configuration["Storage:DataFileFolder"], true);             

                //Delete ChainFile
                File.Delete(ChainFileName);
            }

            //Try to load chain from disk
            if (File.Exists(ChainFileName))
            {
                _logger.LogInformation($"Chain exists, Load from file {ChainFileName}");
                this.LoadBlockChain(ChainFileName);
            }
            else
            {
                _logger.LogWarning("Chain doesn't exist, creating Genesis Block. World is starting now. Big Bang.");
                this.CreateBlockChain(ChainFileName);
            }
            return true;
        }

        public void AddTestDocuments(int DocsCount) {
            byte[] fileToStore = null;
            for (int i = 0; i < DocsCount; i++)
            {
                switch (i % 3)
                {
                    case 0:
                        fileToStore = File.ReadAllBytes("SamplesPDF\\sample1.pdf");
                        break;
                    case 1:
                        fileToStore = File.ReadAllBytes("SamplesPDF\\sample2.pdf");
                        break;
                    case 2:
                        fileToStore = File.ReadAllBytes("SamplesPDF\\sample3.pdf");
                        break;
                }
                CreateNextBlock(fileToStore, $"sample_{i}.pdf");
                _logger.LogTrace($"cycle {i}");
            }

        }

        public BlockChain GetFullChain() {
            return MyChain;
        }

        public bool BootstrapNodes(IEnumerable<DataBlock> chainBlocks)
        {
            MyChain.NodesList = chainBlocks.ToList();
            this.DumpChainToDisk();
            return true;
        }


        public bool LoadBlockChain(string ChainFileName)
        {
            //Load the chain and validate all blocks
            var theChainFile = File.ReadAllBytes(ChainFileName);
            MyChain = (BlockChain)Utility.ByteArrayToObject(theChainFile);
            return true;
        }

        public DataBlock GetLatestBlock()
        {
            return MyChain.NodesList.OrderByDescending(x => x.Timestamp).First();
        }

        public List<DataBlock> GetBlocksList()
        {
            return MyChain.NodesList;
        }

        public byte[] LoadBlockBytesFromDisk(Guid FileGuid)
        {
            var filePath = $"{_configuration["Storage:DataFileFolder"]}\\{FileGuid}";
            return File.ReadAllBytes(filePath);
        }

        public bool StoreBlockBytesToDisk(Guid FileGuid, byte[] dataToStore)
        {
            var filePath = $"{_configuration["Storage:DataFileFolder"]}\\{FileGuid}";
            Directory.CreateDirectory($"{_configuration["Storage:DataFileFolder"]}");
            File.WriteAllBytes(filePath, dataToStore);
            return true;
        }

        public List<DataBlock> GetBlocksList(DateTime FromDate)
        {
            List<DataBlock> dataBlockList = new List<DataBlock>();
            return dataBlockList;
        }

        public List<DataBlock> GetBlocksList(DateTime FromDate, DateTime ToDate)
        {
            List<DataBlock> dataBlockList = new List<DataBlock>();
            return dataBlockList;
        }

        public DataBlock StoreReceivedBlock(DataBlock newBlock, byte[] blockData)
        {
            _logger.LogInformation("Adding new Chain Block");
          
            //Dump DataBlock to Disk with GUID filename
            StoreBlockBytesToDisk(newBlock.Guid, blockData);

            _logger.LogInformation($"Block Index {newBlock.Index}");
            _logger.LogInformation($"Block Description {newBlock.Description}");
            _logger.LogInformation($"Block Data To Store Size {blockData.Length / 1024} KBytes");
            _logger.LogInformation($"Calculating new Hash");          
            _logger.LogInformation($"Hash Block {newBlock.Hash}");

            MyChain.NodesList.Add(newBlock);
            _logger.LogInformation($"Block Added, stored on disk at {newBlock.Guid}");
            this.DumpChainToDisk();
            return newBlock;
        }

        public DataBlock CreateNextBlock(byte[] dataToStore, string description)
        {
            _logger.LogInformation("Creating a new Chain Block");

            var previousBlock = GetLatestBlock();
            DataBlock newBlock = new DataBlock();

            newBlock.Index = previousBlock.Index + 1;
            newBlock.Description = description;
            newBlock.PreviousHash = previousBlock.Hash;
            newBlock.Salt = this.GenerateSalt();
            newBlock.DataSize = dataToStore.Length;
            //Dump DataBlock to Disk with GUID filename
            StoreBlockBytesToDisk(newBlock.Guid, dataToStore);

            _logger.LogInformation($"Block Index {newBlock.Index}");
            _logger.LogInformation($"Block Description {newBlock.Description}");
            _logger.LogInformation($"Block Data To Store Size {dataToStore.Length / 1024} KBytes");
            _logger.LogInformation($"Calculating new Hash");
            newBlock.Hash = this.CalculateBlockHash(
                    newBlock.Index,
                    newBlock.Description,
                    newBlock.PreviousHash,
                    newBlock.Timestamp,
                    newBlock.Salt,
                    newBlock.Guid);
            _logger.LogInformation($"Hash Calculated {newBlock.Hash}");

            MyChain.NodesList.Add(newBlock);
            _logger.LogInformation($"Block Added, stored on disk at {newBlock.Guid}");
            this.DumpChainToDisk();
            return newBlock;
        }

        public Byte[] GetDataBlockBytes(int index)
        {
            var node = MyChain.NodesList.First(x => x.Index == index);
            var bytes = LoadBlockBytesFromDisk(node.Guid);
            return bytes;
        }

        public NetworkNode GetMyChainCredentials()
        {
            NetworkNode node = new NetworkNode();
            node.Description = _configuration["NodeIdentification:Name"];
            node.AccessKey = MyChain.UniqueId.ToString();
            return node;
        }


        public bool CreateBlockChain(string ChainFileName)
        {
            try
            {
                //Create the Chain with genesis block
                MyChain.ChainDescription = "DocsChain 0.1 POC by Giulio Fronterotta";
                MyChain.GenesisDate = DateTime.Now;
                MyChain.UniqueId = Guid.NewGuid();
                MyChain.NodesList = new System.Collections.Generic.List<DataBlock>();

                //Now create Genesis Node


                DataBlock GenesisBlock = new DataBlock();
                GenesisBlock.Index = 0;
                GenesisBlock.PreviousHash = "0";
                GenesisBlock.Timestamp = DateTime.Now;
                GenesisBlock.Salt = this.GenerateSalt();
                GenesisBlock.Description = "Genesis Block";
                StoreBlockBytesToDisk(GenesisBlock.Guid, Encoding.ASCII.GetBytes(GENESIS_BLOCK_DATA));


                GenesisBlock.Hash = this.CalculateBlockHash(
                    GenesisBlock.Index,
                    GenesisBlock.Description,
                    GenesisBlock.PreviousHash,
                    GenesisBlock.Timestamp,
                    GenesisBlock.Salt,
                    GenesisBlock.Guid);

                MyChain.NodesList.Add(GenesisBlock);
                this.DumpChainToDisk();
                return true;
            }
            catch (Exception)
            {
                return false;
            }



        }

        private void DumpChainToDisk()
        {
            if (!Directory.Exists(ConfigDirectory)) Directory.CreateDirectory(ConfigDirectory);

            _logger.LogInformation($"Chain Dump to Disk requested...");


            //Write down to file
            var bytesToWrite = Utility.ObjectToByteArray(MyChain);

            File.WriteAllBytesAsync(ChainFileName, bytesToWrite).Wait();
            _logger.LogInformation($"Dump Done");
            _logger.LogInformation($"Chain Nodes {MyChain.NodesList.Count}");
            _logger.LogInformation($"Chain Size {Convert.ToDouble(bytesToWrite.Length) / 1024000} MBs");
        }

        private string GenerateSalt()
        {
            byte[] randomBytes = new byte[128 / 8];
            using (var generator = RandomNumberGenerator.Create())
            {
                generator.GetBytes(randomBytes);
                return Convert.ToBase64String(randomBytes);
            }
        }

        private string CalculateBlockHash(DataBlock block)
        {

            return CalculateBlockHash(block.Index, block.Description, block.PreviousHash, block.Timestamp, block.Salt, block.Guid);
        }

        private string CalculateBlockHash(int index, string description, string previousHash, DateTime timestamp, string salt, Guid Guid)
        {
            _logger.LogTrace("Calculating Block Hash");
            byte[] indexBytes = Encoding.ASCII.GetBytes(index.ToString("D20"));
            byte[] descriptionBytes = Encoding.ASCII.GetBytes(description);
            byte[] previousHashBytes = Encoding.ASCII.GetBytes(previousHash);
            byte[] timestampBytes = Encoding.ASCII.GetBytes(timestamp.ToString("yyyyMMddHHmmssf"));
            byte[] saltBytes = Encoding.ASCII.GetBytes(salt);
            byte[] data = LoadBlockBytesFromDisk(Guid);


            byte[] theBlock = new byte[indexBytes.Length + descriptionBytes.Length + previousHashBytes.Length + timestampBytes.Length + saltBytes.Length + data.Length];
            _logger.LogTrace("Copying bytes for Hashing");
            System.Buffer.BlockCopy(indexBytes, 0, theBlock, 0, indexBytes.Length);
            System.Buffer.BlockCopy(descriptionBytes, 0, theBlock, indexBytes.Length, descriptionBytes.Length);
            System.Buffer.BlockCopy(previousHashBytes, 0, theBlock, indexBytes.Length + descriptionBytes.Length, previousHashBytes.Length);
            System.Buffer.BlockCopy(timestampBytes, 0, theBlock, indexBytes.Length + descriptionBytes.Length + previousHashBytes.Length, timestampBytes.Length);
            System.Buffer.BlockCopy(saltBytes, 0, theBlock, indexBytes.Length + descriptionBytes.Length + previousHashBytes.Length + timestampBytes.Length, saltBytes.Length);
            System.Buffer.BlockCopy(data, 0, theBlock, indexBytes.Length + descriptionBytes.Length + previousHashBytes.Length + timestampBytes.Length + saltBytes.Length, data.Length);
            _logger.LogTrace($"Copying bytes terminated - Calculate Hash for block {index} - desc {description} - salt {salt}");

            //Calculate Hash
            using (var sha512 = SHA512.Create())
            {
                // Send a sample text to hash.
                var hashedBytes = sha512.ComputeHash(theBlock);

                // Get the hashed string.
                var hash = BitConverter.ToString(hashedBytes).Replace("-", "").ToLower();
                _logger.LogTrace($"Magic SHA512 Hash is -> [{hash}]");
                // Print the string. 
                return hash;
            }
        }

        public bool CheckBlockIntegrity(DataBlock newBlock, DataBlock previousBlock)
        {
            _logger.LogInformation("Blocks integrity check initiated");
            if (previousBlock.Index + 1 != newBlock.Index)
            {
                _logger.LogError($"new Block received has invalid index. Expected {previousBlock.Index + 1}, received {newBlock.Index}");
                return false;
            }
            else if (previousBlock.Hash != newBlock.PreviousHash)
            {
                _logger.LogError($"new Block 'previous hash' is different from the latest block 'previous hash' field. \r\nExpected {previousBlock.PreviousHash}\r\n received {newBlock.PreviousHash}");
                return false;
            }
            else if (CalculateBlockHash(newBlock) != newBlock.Hash)
            {
                _logger.LogError("Recalculated Block Hash is different from the received one.");
                return false;
            }
            _logger.LogInformation($"Integrity check passed  for block {newBlock.Index}:[{newBlock.Description}] against previous Block {previousBlock.Index}:[{previousBlock.Description}]");
            return true;
        }

        public bool CheckFullChainIntegrity()
        {
            _logger.LogWarning("Start of Full Chain integrity Check");

            DataBlock previousBlock = null;
            foreach (var block in MyChain.NodesList.OrderBy(x => x.Index))
            {
                if (block.Index == 0)
                {
                    previousBlock = block;
                    //Skip Genesis Block
                }
                else
                {
                    //Load node Data for checking                    
                    if (CheckBlockIntegrity(block, previousBlock))
                    {
                        //Empty block to avoid data write on chain directly                       
                        previousBlock = block;
                        _logger.LogInformation($"Block {block.Index} verified");
                    }
                    else
                    {
                        _logger.LogCritical($"Integrity Broken at node {block.Index}");
                        return false;
                    }
                }
            }
            _logger.LogInformation("Full chain integrity verified");
            return true;
        }
    }
}
