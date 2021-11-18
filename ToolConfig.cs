using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace ReeleaseEx
{
    public class ToolConfig
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("dir_path_from")]
        public string DirectoryPathFrom { get; set; }

        [JsonProperty("added_files")]
        public List<string> AddedFiles { get; set; }

        [JsonProperty("zip_file_name")]
        public string ZipName { get; set; }

        [JsonProperty("zip_file_params")]
        public string ZipNameParams { get; set; }

        [JsonProperty("dir_path_to")]
        public string DirectoryPathTo { get; set; }

        public static ToolConfig Empty => new ToolConfig(string.Empty, string.Empty, new List<string>(), string.Empty, string.Empty, string.Empty);

        public ToolConfig(string name, string dirPathFrom, List<string> addedFiles, string zipName, string zipNameParams, string dirPathTo)
        {
            Name = name;
            DirectoryPathFrom = dirPathFrom;
            AddedFiles = addedFiles;
            ZipName = zipName;
            ZipNameParams = zipNameParams;
            DirectoryPathTo = dirPathTo;
        }
    }
}
