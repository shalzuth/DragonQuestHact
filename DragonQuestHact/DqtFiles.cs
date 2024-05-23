using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace DragonQuestHact
{
    public static class DqtFiles
    {
        public static Dictionary<String, JsonArray> Caches = new Dictionary<String, JsonArray>();
        static DqtFiles()
        {
        }
        static String basePath;
        public static async Task LoadFile(String fileName)
        {
            if (Caches.ContainsKey(fileName.Replace("data/", "").Replace("en/", "").Replace("map/", ""))) return;
            if (Environment.MachineName == "DESKTOP-L4O7MO1")
            {
                var json = File.ReadAllText(@"C:\Users\shalzuth\Documents\GitHub\dqt-dump\" + fileName.Replace("/", "\\") + ".json");
                json = json.Replace("\n", "");
                var obj = JsonNode.Parse(json);
                Caches.Add(fileName.Replace("data/", "").Replace("en/", "").Replace("map/", ""), obj.AsArray());
            }
            else
            {
                string url = "https://raw.githubusercontent.com/shalzuth/dqt-dump/master/" + fileName + ".json";

                using (var client = new System.Net.Http.HttpClient())
                {
                    using (var response = await client.GetAsync(url))
                    {
                        using (var content = response.Content)
                        {
                            var json = await content.ReadAsStringAsync();
                            json = json.Replace("\n", "");
                            var obj = JsonNode.Parse(json).AsArray();
                            Caches.Add(fileName.Replace("data/", "").Replace("en/", "").Replace("map/", ""), obj);
                        }
                    }
                }
            }
        }
        public static async Task Init(String dir = "")
        {
            var files = new List<String> {
                "data/stages",
            };
            basePath = dir + @"\wwwroot\resource\";
            foreach (var file in files)
            {
                if (String.IsNullOrEmpty(dir))
                {
                    await LoadFile(file);
                }
                //else
                //    LoadFile(file);
            }
        }
    }
}
