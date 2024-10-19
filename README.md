# CS2 Weapon Paints

## Description
Unfinished, unoptimized and not fully functional ugly demo weapon paints plugin for **[CSSharp](https://docs.cssharp.dev/docs/guides/getting-started.html)**. 

## Created [Discord server](https://discord.gg/d9CvaYPSFe) where you can discuss about plugin.

### Consider to donate instead of buying from unknown sources.
[![ko-fi](https://ko-fi.com/img/githubbutton_sm.svg)](https://ko-fi.com/E1E2G0P2O) or [![Donate on Steam](https://github.com/Nereziel/cs2-WeaponPaints/assets/32937653/a0d53822-4ca7-4caf-83b4-e1a9b5f8c94e)](https://steamcommunity.com/tradeoffer/new/?partner=41515647&token=gW2W-nXE)

## Features
- Changes only paint, seed and wear on weapons, knives, gloves and agents
- MySQL based
- Data syncs on player connect
- Added command **`!wp`** to refresh skins ***(with cooldown in seconds can be configured)***
- Added command **`!ws`** to show website
- Added command **`!knife`** to show menu with knives
- Added command **`!gloves`** to show menu with gloves
- Added command **`!agents`** to show menu with agents
- Added command **`!pins`** to show menu with pins
- Added command **`!music`** to show menu with music
- Translations support, submit a PR if you want to share your translation

## ⚙️ Requirements
**Ensure all the following dependencies are installed before proceeding**
- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp)
- [PlayerSettings](https://github.com/NickFox007/PlayerSettingsCS2) - Required by MenuManagerCS2
- [AnyBaseLibCS2](https://github.com/NickFox007/AnyBaseLibCS2) - Required by PlayerSettings
- [MenuManagerCS2](https://github.com/NickFox007/MenuManagerCS2)
- MySQL database

## CS2 Server
- Have working CounterStrikeSharp (**with RUNTIME!**)
- Download from Release and copy plugin to plugins
- Run server with plugin, **it will generate config if installed correctly!**
- Edit `addons/counterstrikesharp/configs/`**`plugins/WeaponPaints/WeaponPaints.json`** include database credentials
- In `addons/counterstrikesharp/configs/`**`core.json`** set **FollowCS2ServerGuidelines** to **`false`**
- Copy from plugins folder gamedata file **`weaponpaints.json`** to folder **`addons/counterstrikesharp/gamedata/`**

## Plugin Configuration
<details>
  <summary>Click to expand</summary>
<code><pre>{
	"Version": 4, // Don't touch
	"DatabaseHost": "", // MySQL host
	"DatabasePort": 3306, // MySQL port
	"DatabaseUser": "", // MySQL username
	"DatabasePassword": "", // MySQL user password
	"DatabaseName": "", // MySQL database name
	"CmdRefreshCooldownSeconds": 60, // Cooldown time in refreshing skins (!wp command)
	"Prefix": "[WeaponPaints]", // Prefix every chat message
	"Website": "example.com/skins", // Website used in WebsiteMessageCommand (!ws command)
"Messages": {
	"WebsiteMessageCommand": "Visit {WEBSITE} where you can change skins.", // Information about website where player can change skins (!ws command) Set to empty to disable
	"SynchronizeMessageCommand": "Type !wp to synchronize chosen skins.", // Information about skins refreshing (!ws command) Set to empty to disable
	"KnifeMessageCommand": "Type !knife to open knife menu.", // Information about knife menu (!ws command) Set to empty to disable
	"CooldownRefreshCommand": "You can\u0027t refresh weapon paints right now.", // Cooldown information (!wp command) Set to empty to disable
	"SuccessRefreshCommand": "Refreshing weapon paints.", // Information about refreshing skins (!wp command) Set to empty to disable
	"ChosenKnifeMenu": "You have chosen {KNIFE} as your knife.", // Information about choosen knife (!knife command) Set to empty to disable
	"ChosenSkinMenu": "You have chosen {SKIN} as your skin.", // Information about choosen skin (!skins command) Set to empty to disable
	"ChosenKnifeMenuKill": "To correctly apply skin for knife, you need to type !kill.", // Information about suicide after knife selection (!knife command) Set to empty to disable
	"KnifeMenuTitle": "Knife Menu.",  // Menu title (!knife menu)
	"WeaponMenuTitle": "Weapon Menu.", // Menu title (!skins menu)
	"SkinMenuTitle": "Select skin for {WEAPON}" // Menu title (!skins menu, after weapon select)
},
"Additional": {
	"KnifeEnabled": true, // Enable or disable knife feature
	"SkinEnabled": true, // Enable or disable skin feature
	"CommandWpEnabled": true, // Enable or disable refreshing command
	"CommandKillEnabled": true, // Enable or disable kill command
	"CommandKnife": "knife", // Name of knife menu command, u can change to for e.g, knives
	"CommandSkin": "ws", // Name of skin information command, u can change to for e.g, skins
	"CommandSkinSelection": "skins", // Name of skins menu command, u can change to for e.g, weapons
	"CommandRefresh": "wp", // Name of skin refreshing command, u can change to for e.g, refreshskins
	"CommandKill": "kill", // Name of kill command, u can change to for e.g, suicide
	"GiveRandomKnife": false,  // Give random knife to players if they didn't choose
	"GiveRandomSkins": false  // Give random skins to players if they didn't choose
},
</pre></code>
</details>
    
## Web install
- Requires PHP >= 7.4 with curl and pdo_mysql ***(Tested on php ver **`8.2.3`** and nginx webserver)***
- **Before using website, make sure the plugin is correctly loaded in cs2 server!** Mysql tables are created by plugin not by website.
- Copy website to web server ***(Folder `img` not needed)***
- Get [Steam API Key](https://steamcommunity.com/dev/apikey)
- Fill in database credentials and api key in `class/config.php`
- Visit website and login via steam

## Web Features
- Basic website
- Steam login/logout
- Change knife, paint, seed and wear

## Troubleshooting
<details>
**Skins are not changing:**
Set FollowCSGOGuidelines to false in cssharp’s core.jcon config

**Database error table does not exists:**
Plugin is not loaded or configured with mysql credentials. Tables are auto-created by plugin.

</details>

### Use this plugin at your own risk! Using this may lead to GSLT ban or something else Valve come with. [Valve Server guidelines](https://blog.counter-strike.net/index.php/server_guidelines/)

## Preview
![preview](https://github.com/Nereziel/cs2-WeaponPaints/blob/main/website/preview.png?raw=true)
