using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DocsChain.ModelsChain
{
    [Serializable]
    public class DataBlock
    {
        public DataBlock()
        {
            Timestamp = DateTime.Now;
            Guid = Guid.NewGuid();
        }
        public int Index { get; set; }
        public Guid Guid { get; set; }
        public string Description { get; set; }
        public DateTime Timestamp { get; set; }
        public string Hash { get; set; }
        public string PreviousHash { get; set; }
        public string Salt { get; set; }

        //Attributes, not part of block hash
        public int DataSize { get; set; }
    }
}
