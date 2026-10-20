using MTFO.Ext.PartialData.DataBlockTypes;
using System.Collections.Generic;
using System.Text.Json;

namespace MTFO.Ext.PartialData
{
    internal class PartialDataCache
    {
        public string Name
        {
            get
            {
                return DataBlockType.GetShortName();
            }
        }

        public IDataBlockType DataBlockType { get; private set; }
        public Queue<JsonElement> JsonsToRead { get; private set; } = new Queue<JsonElement>();

        private PartialDataCache()
        {
        }

        public PartialDataCache(IDataBlockType dbTypeCache)
        {
            DataBlockType = dbTypeCache;
        }
    }
}