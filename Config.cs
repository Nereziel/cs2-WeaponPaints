using CounterStrikeSharp.API.Core;
using System.Text.Json.Serialization;

namespace WeaponPaints
{
	public class Additional
	{
		[JsonPropertyName("SkinVisibilityFix")]
		public bool SkinVisibilityFix { get; set; } = true;

		[JsonPropertyName("KnifeEnabled")]
		public bool KnifeEnabled { get; set; } = true;

		[JsonPropertyName("SkinEnabled")]
		public bool SkinEnabled { get; set; } = true;

		[JsonPropertyName("CommandWpEnabled")]
		public bool CommandWpEnabled { get; set; } = true;

		[JsonPropertyName("CommandKillEnabled")]
		public bool CommandKillEnabled { get; set; } = true;

		[JsonPropertyName("CommandKnife")]
		public string CommandKnife { get; set; } = "knife";

		[JsonPropertyName("CommandSkin")]
		public string CommandSkin { get; set; } = "ws";

		[JsonPropertyName("CommandSkinSelection")]
		public string CommandSkinSelection { get; set; } = "skins";

		[JsonPropertyName("CommandRefresh")]
		public string CommandRefresh { get; set; } = "wp";

		[JsonPropertyName("CommandKill")]
		public string CommandKill { get; set; } = "kill";

		[JsonPropertyName("GiveRandomKnife")]
		public bool GiveRandomKnife { get; set; } = false;

		[JsonPropertyName("GiveRandomSkin")]
		public bool GiveRandomSkin { get; set; } = false;

		[JsonPropertyName("GiveKnifeAfterRemove")]
		public bool GiveKnifeAfterRemove { get; set; } = false;

		[JsonPropertyName("ShowSkinImage")]
		public bool ShowSkinImage { get; set; } = true;
	}

	public class WeaponPaintsConfig : BasePluginConfig
	{
		public override int Version { get; set; } = 4;

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

		[JsonPropertyName("GlobalShare")]
		public bool GlobalShare { get; set; } = false;

		[JsonPropertyName("CmdRefreshCooldownSeconds")]
		public int CmdRefreshCooldownSeconds { get; set; } = 60;

		[JsonPropertyName("Prefix")]
		public string Prefix { get; set; } = "[WeaponPaints]";

		[JsonPropertyName("Website")]
		public string Website { get; set; } = "example.com/skins";

		[JsonPropertyName("Additional")]
		public Additional Additional { get; set; } = new Additional();
	}
}