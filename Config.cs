using CounterStrikeSharp.API.Core;
using System.Text.Json.Serialization;

namespace WeaponPaints
{
    public class WeaponPaintsConfig : BasePluginConfig
    {
        public override int Version { get; set; } = 1;

        [JsonPropertyName("DatabaseHost")]
        public string DatabaseHost { get; set; } = "localhost";

        [JsonPropertyName("DatabasePort")]
        public int DatabasePort { get; set; } = 3306;

        [JsonPropertyName("DatabaseUser")]
        public string DatabaseUser { get; set; } = "dbuser";

        [JsonPropertyName("DatabasePassword")]
        public string DatabasePassword { get; set; } = "dbuserpw";

        [JsonPropertyName("DatabaseName")]
        public string DatabaseName { get; set; } = "dbname";

        [JsonPropertyName("CmdRefreshCooldownSeconds")]
        public int CmdRefreshCooldownSeconds { get; set; } = 60;

        [JsonPropertyName("WebSite")]
        public string WebSite { get; set; } = "http://wp.example.com";
        
    }
}
