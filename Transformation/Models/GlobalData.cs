using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;

namespace TransformationCore.Models
{
    public class GlobalData
    {
        public GlobalData(IReadOnlyDictionary<string, object> data) 
        {
            CacheDataSet = new DataSet();
            Data = data;
        }

        public GlobalData()
        {
            CacheDataSet = new DataSet();
            Data = new ReadOnlyDictionary<string,object>(new Dictionary<string, object>());
        }

        public DataSet CacheDataSet { get; private set; }
        public IReadOnlyDictionary<string, object> Data { get; private set; }
    }
}
