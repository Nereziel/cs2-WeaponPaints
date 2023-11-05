using CounterStrikeSharp.API.Modules.Utils;
using System.Reflection;
using System.Text.Json;

namespace WeaponPaints
{
    internal class Cfg
    {
        public static Config config = new();
        public void CheckConfig(string moduleDirectory)
        {
            string path = Path.Join(moduleDirectory, "config.json");

            if (!File.Exists(path))
            {
                CreateAndWriteFile(path);
            }

            using FileStream fs = new(path, FileMode.Open, FileAccess.Read);
            using StreamReader sr = new(fs);
            // Deserialize the JSON from the file and load the configuration.
            config = JsonSerializer.Deserialize<Config>(sr.ReadToEnd());
        }
        private static void CreateAndWriteFile(string path)
        {

            using (FileStream fs = File.Create(path))
            {
                // File is created, and fs will automatically be disposed when the using block exits.
            }

            Console.WriteLine($"File created: {File.Exists(path)}");

            config = new Config
            {
                DatabaseHost = "localhost",
                DatabasePort = 3306,
                DatabaseUser = "dbuser",
                DatabasePassword = "dbpassword",
                DatabaseName = "database"
            };

            // Serialize the config object to JSON and write it to the file.
            string jsonConfig = JsonSerializer.Serialize(config, new JsonSerializerOptions()
            {
                WriteIndented = true
            });
            File.WriteAllText(path, jsonConfig);
        }
    }
    internal class Config
    {
        public string? DatabaseHost { get; set; }
        public uint DatabasePort { get; set; }
        public string? DatabaseUser { get; set; }
        public string? DatabasePassword { get; set; }
        public string? DatabaseName { get; set; }
    }
}
