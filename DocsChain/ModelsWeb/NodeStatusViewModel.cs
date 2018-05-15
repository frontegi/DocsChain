using DocsChain.ModelsChain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DocsChain.ModelsWeb
{
    public class NodeStatusViewModel
    {
        public List<NetworkNode> Nodes { get; set; }
        public BlockChain Chain { get; set; }
    }
}
