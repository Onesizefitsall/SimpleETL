using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace SimpleETL
{
    class Program
    {
        private static int batchSize = 1000000;
        static void Main(string[] args)
        {
            if (args.Any() && args[0] == "sample")
            {
                EtlConfig config = new EtlConfig();
                config.Name = "Name";
                config.AfterScript = "Select * from test";
                config.BeforeScript = "Select * from test";
                config.FileName = "teste_[0-9]{3}.csv";
                config.TableName = "Log";
                config.SkipLines = 3;
                config.ColumnsMap.Add(new ColumnMap() { OriginColumn = 0, TargetColumn = "ID" });
                config.ColumnsMap.Add(new ColumnMap() { OriginColumn = 1, TargetColumn = "Name" });
                config.ColumnsTransform.Add(new RegexReplace() { OriginColumn = 1, RegexPattern = "^VQ", ReplaceText = "Vazio" });
                config.ColumnsTransform.Add(new RegexReplace() { OriginColumn = 1, RegexPattern = "^AA.{5}", ReplaceText = "BB" });

                List<EtlConfig> testConfig = new List<EtlConfig>();
                testConfig.Add(config);
                testConfig.Add(config);
                testConfig.Add(config);
                File.WriteAllText("ConfigSample.json", JsonConvert.SerializeObject(testConfig, Formatting.Indented), Encoding.UTF8);
                Console.WriteLine("Sample Config Saved");

            }
            FileSystemWatcher fsw = new FileSystemWatcher(Properties.Settings.Default.WatchFolder);

            fsw.Created += Fsw_Created;
            //fsw.Changed += Fsw_Created;
            fsw.EnableRaisingEvents = true;
            Console.ReadKey();

        }

        private static void Fsw_Created(object sender, FileSystemEventArgs e)
        {
            var configs = JsonConvert.DeserializeObject<List<EtlConfig>>(File.ReadAllText("Config.json"));
            foreach (var config in configs)
            {
                if (Regex.IsMatch(e.Name, config.FileName))
                    ExecuteEtl(config, e.FullPath);
            }
        }

        private static void ExecuteEtl(EtlConfig config, string file)
        {
            int count = 0;
            //Inicio, leitura do arquivo
            using (var fileStream = File.OpenRead(file))
            using (var reader = new StreamReader(fileStream))
            using (var conn = new SqlConnection(Properties.Settings.Default.ConnectionString))
            {
                //Skip Header Lines
                DataTable tbl = new DataTable();
                for (int i = 0; i < config.SkipLines; i++)
                {
                    if (!reader.EndOfStream)
                        reader.ReadLine();

                }
                conn.Open();
                bool runFileRead;
                if (!string.IsNullOrEmpty(config.BeforeScript))
                {
                    SqlCommand beforeCommand = conn.CreateCommand();
                    beforeCommand.CommandText = config.BeforeScript;
                    beforeCommand.ExecuteNonQuery();
                }

                SqlBulkCopy sbc = new SqlBulkCopy(conn);
                sbc.NotifyAfter = 1000;
                sbc.SqlRowsCopied += (sender, args) => { Console.WriteLine(args.RowsCopied); };
                sbc.DestinationTableName = config.TableName;
                foreach (var columnMap in config.ColumnsMap)
                    sbc.ColumnMappings.Add(columnMap.OriginColumn, columnMap.TargetColumn);


                do
                {
                    var currentLine = reader.ReadLine();
                    bool addToTable = currentLine != null;

                    count++;

                    if (addToTable)
                    {
                        var currentFields = currentLine.Split(new[] { config.FieldSeparator }, StringSplitOptions.None);
                        //Add Columnsm should run the first time only
                        if (tbl.Columns.Count == 0)
                            for (int i = 0; i < sbc.ColumnMappings.Count; i++)
                                tbl.Columns.Add(i.ToString());


                        //Run regex replaces
                        if (config.ColumnsTransform.Any())
                            for (var i = 0; i < currentFields.Length; i++)
                                if (config.ColumnsTransform.Any(w => w.OriginColumn == i))
                                {
                                    var transform = config.ColumnsTransform.First(w => w.OriginColumn == i);
                                    currentFields[i] = Regex.Replace(currentFields[i], transform.RegexPattern, transform.ReplaceText);

                                }

                       
                                                                     


                        tbl.Rows.Add(currentFields);
                    }

                    if (count != batchSize && !reader.EndOfStream)
                        continue;

                    //Carregando um batch
                    sbc.WriteToServer(tbl);
                    tbl.Rows.Clear();
                    count = 0;

                } while (!reader.EndOfStream);

                if (!string.IsNullOrEmpty(config.AfterScript))
                {
                    SqlCommand afterCommand = conn.CreateCommand();
                    afterCommand.CommandText = config.AfterScript;
                    afterCommand.ExecuteNonQuery();
                }
            }


        }
    }
}
