using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace SimpleETL
{
    [Serializable]
    internal class EtlConfig
    {
        public string Name { get; set; }
        public string TableName { get; set; }
        public string FileName { get; set; }
        public string BeforeScript { get; set; }
        public string AfterScript { get; set; }
        public string FieldSeparator { get; set; }
        public int SkipLines { get; set; }
        public List<ColumnMap> ColumnsMap { get; set; }
        public List<RegexReplace> ColumnsTransform { get; set; }

        public EtlConfig()
        {
            ColumnsMap = new List<ColumnMap>();
            ColumnsTransform = new List<RegexReplace>();
          
        }

        public string ToJson()
        {
            return JsonConvert.SerializeObject(this);
        }
    }

    [Serializable]
    internal class ColumnMap
    {
        public int OriginColumn { get; set; }
        public string TargetColumn { get; set; }
    }
    [Serializable]
    internal class RegexReplace
    {
        public int OriginColumn { get; set; }
        public string RegexPattern { get; set; }
        public string ReplaceText { get; set; }

    }

}
