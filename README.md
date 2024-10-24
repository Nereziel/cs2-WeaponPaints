```

	 	     __    __                               ___      _       _       
		    / / /\ \ \___  __ _ _ __   ___  _ __   / _ \__ _(_)_ __ | |_ ___ 
		    \ \/  \/ / _ \/ _` | '_ \ / _ \| '_ \ / /_)/ _` | | '_ \| __/ __|
		     \  /\  /  __/ (_| | |_) | (_) | | | / ___/ (_| | | | | | |_\__ \
		      \/  \/ \___|\__,_| .__/ \___/|_| |_\/    \__,_|_|_| |_|\__|___/
        		 	       |_|
                                     
```

<p align="center">
    <a href="https://github.com/Nereziel/cs2-WeaponPaints/releases">📖 Releases</a> •
    <a href="https://discord.gg/d9CvaYPSFe">💬 Discord</a>
    <br /><br />
</p>
<hr />

## 📝 Description
Unfinished, unoptimized and not fully functional ugly demo weapon paints plugin for **[CSSharp](https://docs.cssharp.dev/docs/guides/getting-started.html)**. 


### 💸 Consider to donate instead of buying from unknown sources.
[![ko-fi](https://ko-fi.com/img/githubbutton_sm.svg)](https://ko-fi.com/E1E2G0P2O) or [![Donate on Steam](https://github.com/Nereziel/cs2-WeaponPaints/assets/32937653/a0d53822-4ca7-4caf-83b4-e1a9b5f8c94e)](https://steamcommunity.com/tradeoffer/new/?partner=41515647&token=gW2W-nXE)

## ✨ Features
- Changes only paint, seed and wear on weapons, knives, gloves and agents
- MySQL based
- Data syncs on player connect
- Added command **`!wp`** to refresh skins ***(with cooldown in seconds can be configured)***
- Added command **`!ws`** to show website
- Added command **`!knife`** to show menu with knives
- Added command **`!stattrak`** to enable stattrak on weapon
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

## 🗄️ CS2 Server
- Have working CounterStrikeSharp (**with RUNTIME!**)
- Download from Release and copy plugin to plugins
- Run server with plugin, **it will generate config if installed correctly!**
- Edit `addons/counterstrikesharp/configs/`**`plugins/WeaponPaints/WeaponPaints.json`** include database credentials
- In `addons/counterstrikesharp/configs/`**`core.json`** set **FollowCS2ServerGuidelines** to **`false`**
- Copy from plugins folder gamedata file **`weaponpaints.json`** to folder **`addons/counterstrikesharp/gamedata/`**

## 🛠️ Plugin Configuration
<details>
  <summary>Click to expand</summary>
<code><pre>{
  "ConfigVersion": 10, // Don't touch
  "SkinsLanguage": "en", // Language
  "DatabaseHost": "", // MySQL host
  "DatabasePort": 3306, // MySQL Port
  "DatabaseUser": "", // MySQL Username
  "DatabasePassword": "", // MySQL User password
  "DatabaseName": "", // MySQL Database name
  "CmdRefreshCooldownSeconds": 3, // Cooldown time in refreshing skins (!wp command)
  "Website": "example.com/skins", // Website used in WebsiteMessageCommand (!ws command)
  "Additional": {
    "KnifeEnabled": true, // If knives are enabled
    "GloveEnabled": true, // If gloves are enabled
    "MusicEnabled": true, // If music kits are enabled
    "AgentEnabled": true, // If agents are enabled
    "SkinEnabled": true, // If skins are enabled
    "PinsEnabled": true, // If pins are enabled
    "CommandWpEnabled": true, // If command !wp is enabled
    "CommandKillEnabled": true, // If command !kill is enabled
    "CommandKnife": [ // Command for knives
      "knife"
    ],
    "CommandMusic": [ // Command for music kits
      "music"
    ],
    "CommandPin": [  // Command for pins
      "pin",
      "pins",
      "coin",
      "coins"
    ],
    "CommandGlove": [  // Command for gloves
      "gloves"
    ],
    "CommandAgent": [ // Command for agents
      "agents"
    ],
    "CommandStattrak": [  // Command for stattrak
      "stattrak",
      "st"
    ],
    "CommandSkin": [  // Command for skins
      "ws"
    ],
    "CommandSkinSelection": [  // Command for skin selection
      "skins"
    ],
    "CommandRefresh": [  // Command for refreshing your skins
      "wp"
    ],
    "CommandKill": [  // Command for death
      "kill"
    ],
    "GiveRandomKnife": false, // If it should give you Random Knife
    "GiveRandomSkin": false, // If it should give you Random Skin
    "ShowSkinImage": true // When you select a skin if it should show skins image
  },
  "MenuType": "selectable" // Menu type commands. Can be: selectable, dynamic, center, chat, console
}
</pre></code>
</details>
    
## 🖥️ Web install
- Requires PHP >= 7.4 with curl and pdo_mysql ***(Tested on php ver **`8.2.3`** and nginx webserver)***
- **Before using website, make sure the plugin is correctly loaded in cs2 server!** Mysql tables are created by plugin not by website.
- Copy website to web server ***(Folder `img` not needed)***
- Get [Steam API Key](https://steamcommunity.com/dev/apikey)
- Fill in database credentials and api key in `class/config.php`
- Visit website and login via steam

## 🧩 Web Features
> [!WARNING]
> We recommend you to use any third-party website for WeaponPaints. Website by us doesn't get updated!
- Basic website
- Steam login/logout
- Change knife, paint, seed and wear

## 🌐 Third-party websites
 - **[CSS-Bans](https://github.com/counterstrikesharp-panel/css-bans)**
 - **[CS2-WeaponPaints-Website](https://github.com/LielXD/CS2-WeaponPaints-Website)**
 - **[cs2-WeaponPaints-website](https://github.com/L1teD/cs2-WeaponPaints-website)** > This webiste is different from the one above!
 - **[CS2-WeaponPaints-Web](https://github.com/rogeraabbccdd/CS2-WeaponPaints-Web)**
## 🤔 Troubleshooting
<details>
**Skins are not changing:**
Set FollowCSGOGuidelines to false in cssharp’s core.jcon config

**Database error table does not exists:**
Plugin is not loaded or configured with mysql credentials. Tables are auto-created by plugin.

</details>

### Use this plugin at your own risk! Using this may lead to GSLT ban or something else Valve come with. [Valve Server guidelines](https://blog.counter-strike.net/index.php/server_guidelines/)

## Website Preview
![preview](https://github.com/Nereziel/cs2-WeaponPaints/blob/main/website/preview.png?raw=true)
