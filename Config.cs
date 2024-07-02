using CounterStrikeSharp.API.Core;
using System.Text.Json.Serialization;

namespace WeaponPaints
{
	public class DatabaseCredentials
	{
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
	}

	public class Additional
	{
		[JsonPropertyName("KnifeEnabled")]
		public bool KnifeEnabled { get; set; } = true;

		[JsonPropertyName("GloveEnabled")]
		public bool GloveEnabled { get; set; } = true;

		[JsonPropertyName("MusicEnabled")]
		public bool MusicEnabled { get; set; } = true;

		[JsonPropertyName("AgentEnabled")]
		public bool AgentEnabled { get; set; } = true;

		[JsonPropertyName("SkinEnabled")]
		public bool SkinEnabled { get; set; } = true;

        [JsonPropertyName("NameTagEnabled")]
        public bool NameTagEnabled { get; set; } = true;

		[JsonPropertyName("PinEnabled")]
		public bool PinEnabled { get; set; } = true;

		[JsonPropertyName("CommandsKnife")]
		public List<string> CommandsKnife { get; set; } = ["knife", "knives"];

		[JsonPropertyName("CommandsMusic")]
		public List<string> CommandsMusic { get; set; } = ["music", "musickits", "mkit"];

		[JsonPropertyName("CommandsGlove")]
		public List<string> CommandsGlove { get; set; } = ["gloves", "glove"];

		[JsonPropertyName("CommandsAgent")]
		public List<string> CommandsAgent { get; set; } = ["agents", "agent"];

		[JsonPropertyName("CommandsInfo")]
		public List<string> CommandsInfo { get; set; } = ["ws", "skininfo"];

		[JsonPropertyName("CommandsSkinSelection")]
		public List<string> CommandsSkinSelection { get; set; } = ["skins", "skin"];

		[JsonPropertyName("CommandsRefresh")]
		public List<string> CommandsRefresh { get; set; } = ["wp", "refreshskins"];

		[JsonPropertyName("CommandsKill")]
		public List<string> CommandsKill { get; set; } = ["kill", "suicide"];

		[JsonPropertyName("GiveRandomKnife")]
		public bool GiveRandomKnife { get; set; } = false;

		[JsonPropertyName("GiveRandomSkin")]
		public bool GiveRandomSkin { get; set; } = false;

		[JsonPropertyName("ShowSkinImage")]
		public bool ShowSkinImage { get; set; } = true;
		
		[JsonPropertyName("UseHtmlMenu")]
		public bool UseHtmlMenu { get; set; } = true;
		
		[JsonPropertyName("ExpireOlderThan")]
		public int ExpireOlderThan { get; set; } = 90;
	}

	public class WeaponPaintsConfig : BasePluginConfig
	{
		public override int Version { get; set; } = 7;
		
		[JsonPropertyName("DatabaseCredentials")]
		public DatabaseCredentials DatabaseCredentials { get; set; } = new();
		
		[JsonPropertyName("CmdRefreshCooldownSeconds")]
		public int CmdRefreshCooldownSeconds { get; set; } = 60;

		[JsonPropertyName("Prefix")]
		public string Prefix { get; set; } = "[WeaponPaints]";

		[JsonPropertyName("Website")]
		public string Website { get; set; } = "example.com/skins";

		[JsonPropertyName("Additional")]
		public Additional Additional { get; set; } = new();
	}
}