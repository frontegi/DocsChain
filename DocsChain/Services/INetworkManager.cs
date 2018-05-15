using DocsChain.ModelsChain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DocsChain.Services
{
    public interface INetworkManager
    {
        bool LoadNodes();
        bool AddNode(NetworkNode networkNode);
        NetworkNode GetNodeByUrl(string url);
        IEnumerable<NetworkNode> GetAllNetworkNodes();
        Task<IEnumerable<NetworkNode>> CallWelcomeNode(string remoteUrl, NetworkNode node);
        Task<IEnumerable<DataBlock>> CallGetNodesList(NetworkNode node);
        Task<byte[]> CallGetDataBlockBytes(int Id, NetworkNode node);
        Task<bool> CallNetworkNodesUpdate();
        Task<byte[]> GetDataBlockFromRandomNode(int Id);
        void BroadcastNewBlock(DataBlock newBlock);
    }
}
