using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Config_Generator.Models
{
    internal class EtlConfig
    {
        public string Name { get; set; }
        public string TableName { get; set; }
        public string FileName { get; set; }
        public string WatchFolder { get; set; }
        public string BeforeScript { get; set; }
        public string AfterScript { get; set; }
        public int SkipLines { get; set; }
        public ObservableCollection<ColumnMap> ColumnsMap { get; set; }
        public List<RegexReplace> ColumnTransform { get; set; }
    }

    internal class ColumnMap
    {
        public int OriginColumn { get; set; }
        public string TargetColumn { get; set; }
    }
    internal class RegexReplace
    {
        public int OriginColumn { get; set; }
        public string RegexPattern { get; set; }
        public string ReplaceText { get; set; }

    }

}
