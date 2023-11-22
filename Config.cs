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
		[JsonPropertyName("ChosenSkinMenu")]
		public string ChosenSkinMenu { get; set; } = "You have chosen {SKIN} as your skin.";
		[JsonPropertyName("ChosenKnifeMenuKill")]
		public string ChosenKnifeMenuKill { get; set; } = "To correctly apply skin for knife, you need to type !kill.";
		[JsonPropertyName("KnifeMenuTitle")]
		public string KnifeMenuTitle { get; set; } = "Knife Menu.";
		[JsonPropertyName("WeaponMenuTitle")]
		public string WeaponMenuTitle { get; set; } = "Weapon Menu.";
		[JsonPropertyName("SkinMenuTitle")]
		public string SkinMenuTitle { get; set; } = "Select skin for {WEAPON}";
	}

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

		[JsonPropertyName("Messages")]
		public Messages Messages { get; set; } = new Messages();

		[JsonPropertyName("Additional")]
		public Additional Additional { get; set; } = new Additional();
	}

}
