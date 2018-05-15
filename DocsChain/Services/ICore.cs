using DocsChain.ModelsChain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DocsChain.Services
{
    public interface ICore
    {
        bool Bootstrap();
        bool Bootstrap(bool ResetChain);
        bool BootstrapNodes(IEnumerable<DataBlock> chainBlocks);
        bool CheckBlockIntegrity(DataBlock newBlock, DataBlock previousBlock);
        bool CheckFullChainIntegrity();
        NetworkNode GetMyChainCredentials();
        bool CreateBlockChain(string ChainFileName);
        DataBlock CreateNextBlock(byte[] dataToStore, string description);
        DataBlock StoreReceivedBlock(DataBlock newBlock, byte[] blockData);

        List<DataBlock> GetBlocksList();
        List<DataBlock> GetBlocksList(DateTime FromDate);
        List<DataBlock> GetBlocksList(DateTime FromDate, DateTime ToDate);
        Byte[] GetDataBlockBytes(int index);
        DataBlock GetLatestBlock();
        bool LoadBlockChain(string ChainFileName);
        bool StoreBlockBytesToDisk(Guid FileGuid, byte[] dataToStore);

        BlockChain GetFullChain();

        void AddTestDocuments(int DocsCount);
    }
}
