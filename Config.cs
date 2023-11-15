using CounterStrikeSharp.API.Core;
using System.Text.Json.Serialization;

namespace WeaponPaints
{
    public class Messages
    {
        [JsonPropertyName("WebsiteMessageCommand")]
        public string WebsiteMessageCommand { get; set; } = "Visit {WEBSITE} where you can change skins.";
        [JsonPropertyName("SynchronizeMessageCommand")]
        public string SynchronizeMessageCommand { get; set; } = "Type !wp to synchronize chosen skins.";
        [JsonPropertyName("KnifeMessageCommand")]
        public string KnifeMessageCommand { get; set; } = "Type !knife to open knife menu.";
        [JsonPropertyName("CooldownRefreshCommand")]
        public string CooldownRefreshCommand { get; set; } = "You can't refresh weapon paints right now.";
        [JsonPropertyName("SuccessRefreshCommand")]
        public string SuccessRefreshCommand { get; set; } = "Refreshing weapon paints.";
        [JsonPropertyName("ChosenKnifeMenu")]
        public string ChosenKnifeMenu { get; set; } = "You have chosen {KNIFE} as your knife.";
        [JsonPropertyName("KnifeMenuTitle")]
        public string KnifeMenuTitle { get; set; } = "Knife Menu.";
    }

    public class WeaponPaintsConfig : BasePluginConfig
    {
        public override int Version { get; set; } = 2;

        [JsonPropertyName("DatabaseHost")]
        public string DatabaseHost { get; set; } = "";

        [JsonPropertyName("DatabasePort")]
        public int DatabasePort { get; set; } = 3306;

        [JsonPropertyName("DatabaseUser")]
        public string DatabaseUser { get; set; } = "";

        [JsonPropertyName("DatabasePassword")]
        public string DatabasePassword { get; set; } = "";

        [JsonPropertyName("DatabaseName")]
        public string DatabaseName { get; set; } = "";

        [JsonPropertyName("CmdRefreshCooldownSeconds")]
        public int CmdRefreshCooldownSeconds { get; set; } = 60;

        [JsonPropertyName("Prefix")]
        public string Prefix { get; set; } = "[WeaponPaints]";

        [JsonPropertyName("Website")]
        public string Website { get; set; } = "example.com/skins";

        [JsonPropertyName("Messages")]
        public Messages Messages { get; set; } = new Messages();
    }
    
}
