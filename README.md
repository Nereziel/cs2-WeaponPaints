# CS2 WeaponPaints

WeaponPaints is a [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp) plugin for Counter-Strike 2 that allows players to use custom weapon skins, knives, gloves, agents, music kits and pins.

Player selections are stored in MySQL and loaded automatically when the player connects.

The repository also includes a basic PHP web interface for selecting weapon skins and knives.

> [!WARNING]
> Using cosmetic-changing plugins may conflict with Valve's server guidelines.
>
> This plugin requires `FollowCS2ServerGuidelines` to be set to `false` in CounterStrikeSharp.
>
> **Use at your own risk.**

## Features

### Weapon skins

WeaponPaints supports:

- paint kits
- seed
- wear
- name tags
- StatTrak
- StatTrak kill counter
- up to 5 sticker slots
- one keychain slot

The plugin reads these properties from the `wp_player_skins` database table and applies them to weapons.

The in-game `!skins` menu only selects the paint kit. A skin selected through the in-game menu is stored with:

```text
wear = 0.01
seed = 0
```

Wear and seed can be changed through the included web interface or directly through a compatible database/web integration.

There are currently no built-in in-game menus for editing name tags, stickers or keychains.

### Other cosmetics

The plugin also supports:

- knife selection
- glove selection
- agent selection
- music kit selection
- pin / coin selection
- separate T / CT selections where supported
- configurable item/menu images
- configurable menu implementation
- configurable command aliases
- localization
- configurable item-data language
- optional random weapon skins
- MySQL persistence
- automatic player-data loading on connect

### StatTrak

StatTrak can be toggled for the currently held customized weapon with:

```text
!stattrak
```

or:

```text
!st
```

When StatTrak is enabled, kills made with that weapon increase its StatTrak counter.

The current StatTrak state and counter are synchronized back to the database when the player disconnects.

---

# Requirements

Install the following before WeaponPaints:

- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp) **with runtime**
- [MenuManagerCS2](https://github.com/NickFox007/MenuManagerCS2)
- [PlayerSettingsCS2](https://github.com/NickFox007/PlayerSettingsCS2)
- [AnyBaseLibCS2](https://github.com/NickFox007/AnyBaseLibCS2)
- MySQL / MariaDB-compatible database

WeaponPaints currently declares:

```text
Minimum CounterStrikeSharp API: 338
```

The current project references:

```text
CounterStrikeSharp.API 1.0.376
```

---

# Installation

## 1. Install dependencies

Install CounterStrikeSharp with its runtime and the MenuManager dependencies listed above.

Make sure CounterStrikeSharp is working before installing WeaponPaints.

## 2. Download WeaponPaints

Download the latest release:

[Latest WeaponPaints release](https://github.com/Nereziel/cs2-WeaponPaints/releases/latest)

Use:

```text
WeaponPaints.zip
```

Extract it directly into:

```text
game/csgo/
```

The release archive is generated with this layout:

```text
game/csgo/
└── addons/
    └── counterstrikesharp/
        ├── plugins/
        │   └── WeaponPaints/
        │       ├── WeaponPaints.dll
        │       ├── data/
        │       ├── lang/
        │       └── ...
        │
        └── gamedata/
            └── weaponpaints.json
```

Do not copy only `WeaponPaints.dll`.

The gamedata file is required.

## 3. Start the server

Start the server once so CounterStrikeSharp creates the plugin configuration.

The configuration is located at:

```text
addons/counterstrikesharp/configs/plugins/WeaponPaints/WeaponPaints.json
```

WeaponPaints requires valid database settings. If `DatabaseHost`, `DatabaseName` or `DatabaseUser` are empty, the plugin throws a configuration error.

## 4. Configure MySQL

Set your database credentials in:

```text
addons/counterstrikesharp/configs/plugins/WeaponPaints/WeaponPaints.json
```

Example:

```json
{
  "ConfigVersion": 10,
  "SkinsLanguage": "en",
  "DatabaseHost": "127.0.0.1",
  "DatabasePort": 3306,
  "DatabaseUser": "weaponpaints",
  "DatabasePassword": "your-password",
  "DatabaseName": "weaponpaints",
  "CmdRefreshCooldownSeconds": 3,
  "Website": "example.com/skins",
  "Additional": {
    "KnifeEnabled": true,
    "GloveEnabled": true,
    "MusicEnabled": true,
    "AgentEnabled": true,
    "SkinEnabled": true,
    "PinsEnabled": true,
    "CommandWpEnabled": true,
    "CommandKillEnabled": true,
    "CommandKnife": [
      "knife"
    ],
    "CommandMusic": [
      "music"
    ],
    "CommandPin": [
      "pin",
      "pins",
      "coin",
      "coins"
    ],
    "CommandGlove": [
      "gloves"
    ],
    "CommandAgent": [
      "agents"
    ],
    "CommandStattrak": [
      "stattrak",
      "st"
    ],
    "CommandSkin": [
      "ws"
    ],
    "CommandSkinSelection": [
      "skins"
    ],
    "CommandRefresh": [
      "wp"
    ],
    "CommandKill": [
      "kill"
    ],
    "GiveRandomKnife": false,
    "GiveRandomSkin": false,
    "ShowSkinImage": true
  },
  "MenuType": "selectable"
}
```

> [!NOTE]
> `GiveRandomKnife` still exists in the current configuration model, but the current `main` implementation does not use this option.

## 5. Disable CounterStrikeSharp server-guideline protection

Edit:

```text
addons/counterstrikesharp/configs/core.json
```

Set:

```json
"FollowCS2ServerGuidelines": false
```

Restart the server after changing this option.

---

# Configuration

## General

| Setting | Default | Description |
|---|---:|---|
| `ConfigVersion` | `10` | Current WeaponPaints configuration version. |
| `SkinsLanguage` | `"en"` | Language suffix used when loading item JSON files. |
| `DatabaseHost` | `""` | MySQL host. |
| `DatabasePort` | `3306` | MySQL port. |
| `DatabaseUser` | `""` | MySQL username. |
| `DatabasePassword` | `""` | MySQL password. |
| `DatabaseName` | `""` | MySQL database name. |
| `CmdRefreshCooldownSeconds` | `3` | Shared cooldown used by refresh and cosmetic menu commands. |
| `Website` | `"example.com/skins"` | Website printed by the `!ws` command. |
| `MenuType` | `"selectable"` | MenuManager menu implementation to use. |

## Feature settings

| Setting | Default | Description |
|---|---:|---|
| `KnifeEnabled` | `true` | Enables knife support and the knife menu. |
| `GloveEnabled` | `true` | Enables glove support and the glove menu. |
| `MusicEnabled` | `true` | Enables music kits and the music menu. |
| `AgentEnabled` | `true` | Enables custom agents and the agent menu. |
| `SkinEnabled` | `true` | Enables weapon skin handling and the skin menu. |
| `PinsEnabled` | `true` | Enables pins / coins and the pin menu. |
| `CommandWpEnabled` | `true` | Enables the player refresh command and server `wp_refresh` processing. |
| `CommandKillEnabled` | `true` | Registers the configured suicide command. |
| `GiveRandomSkin` | `false` | Applies a random paint kit when a weapon has no selected custom paint. |
| `GiveRandomKnife` | `false` | Present in the current config model but currently unused by the implementation. |
| `ShowSkinImage` | `true` | Shows the selected item's image using center HTML for approximately 2 seconds. |

## Command aliases

Command settings are arrays.

This means multiple aliases can be assigned to the same feature.

Example:

```json
"CommandKnife": [
  "knife",
  "knives",
  "nuz"
]
```

CounterStrikeSharp registers these as:

```text
css_knife
css_knives
css_nuz
```

Players can normally use them in chat as:

```text
!knife
!knives
!nuz
```

---

# Menu Types

The current implementation supports:

| Value | MenuManager type |
|---|---|
| `selectable` | Default selectable menu |
| `dynamic` | Button menu |
| `center` | Center menu |
| `chat` | Chat menu |
| `console` | Console menu |

Example:

```json
"MenuType": "center"
```

If an unknown value is specified, WeaponPaints falls back to the normal MenuManager menu.

---

# Player Commands

Default command aliases:

| Command | Function |
|---|---|
| `!skins` | Opens the weapon skin selection menu. |
| `!knife` | Opens the knife selection menu. |
| `!gloves` | Opens the glove selection menu. |
| `!agents` | Opens the agent selection menu. |
| `!music` | Opens the music kit selection menu. |
| `!pin` | Opens the pin / coin selection menu. |
| `!pins` | Alias for the pin menu. |
| `!coin` | Alias for the pin menu. |
| `!coins` | Alias for the pin menu. |
| `!stattrak` | Toggles StatTrak on the currently held customized weapon. |
| `!st` | Alias for `!stattrak`. |
| `!wp` | Reloads player data from the database and refreshes cosmetics. |
| `!ws` | Prints configured WeaponPaints information including the configured website. |
| `!kill` | Kills the player's current character. |

The aliases can be changed in `WeaponPaints.json`.

## Command cooldown

`CmdRefreshCooldownSeconds` is used by:

- `!wp`
- knife menu
- skin menu
- glove menu
- agent menu
- music menu
- pin menu

The default is:

```json
"CmdRefreshCooldownSeconds": 3
```

`!ws`, `!stattrak` and `!kill` do not use this menu/refresh cooldown.

Commands that modify or refresh cosmetics are also blocked while `_gBCommandsAllowed` is disabled between round end and the next round start where applicable.

---

# Server Console Command

WeaponPaints registers:

```text
wp_refresh <steamid64|all>
```

This command is server-console only.

Calling it from a player context is ignored.

Refresh all connected non-bot players:

```text
wp_refresh all
```

Refresh a specific connected player:

```text
wp_refresh 76561198012345678
```

The command reloads the player's database data and reapplies:

- weapon skins
- gloves
- agents
- music kit
- pin

---

# Database

WeaponPaints automatically creates its database tables.

You do not need to manually import a database schema before running the plugin.

The following tables are created:

```text
wp_player_skins
wp_player_knife
wp_player_gloves
wp_player_agents
wp_player_music
wp_player_pins
```

## `wp_player_skins`

Stores weapon cosmetic information.

Current columns include:

```text
steamid
weapon_team
weapon_defindex
weapon_paint_id
weapon_wear
weapon_seed
weapon_nametag
weapon_stattrak
weapon_stattrak_count
weapon_sticker_0
weapon_sticker_1
weapon_sticker_2
weapon_sticker_3
weapon_sticker_4
weapon_keychain
```

The unique key is:

```text
steamid + weapon_team + weapon_defindex
```

### Sticker format

Each sticker is stored as:

```text
id;schema;x;y;wear;scale;rotation
```

WeaponPaints currently supports five sticker columns:

```text
weapon_sticker_0
weapon_sticker_1
weapon_sticker_2
weapon_sticker_3
weapon_sticker_4
```

### Keychain format

The keychain is stored as:

```text
id;x;y;z;seed
```

## `wp_player_knife`

Stores:

```text
steamid
weapon_team
knife
```

Unique key:

```text
steamid + weapon_team
```

## `wp_player_gloves`

Stores:

```text
steamid
weapon_team
weapon_defindex
```

Unique key:

```text
steamid + weapon_team
```

Glove paint information is handled through the weapon-paint data loaded into WeaponPaints.

## `wp_player_agents`

Stores:

```text
steamid
agent_ct
agent_t
```

Unique key:

```text
steamid
```

## `wp_player_music`

Stores:

```text
steamid
weapon_team
music_id
```

Unique key:

```text
steamid + weapon_team
```

## `wp_player_pins`

Stores:

```text
steamid
weapon_team
id
```

Unique key:

```text
steamid + weapon_team
```

---

# Player Data Loading

When a player fully connects, WeaponPaints loads enabled cosmetic data from MySQL.

Depending on configuration, this includes:

```text
Knife
Gloves
Agents
Music
Weapon paints
Pins
```

The data is kept in memory while the player is connected.

On spawn, WeaponPaints reapplies the player's:

```text
Music kit
Agent
Gloves
Pin
```

Weapon skins are applied when the relevant weapon entity is created/given.

---

# Skin Data

WeaponPaints loads cosmetic definitions from JSON files in the plugin's `data` directory.

The files are selected using:

```json
"SkinsLanguage": "en"
```

For English this results in:

```text
data/skins_en.json
data/gloves_en.json
data/agents_en.json
data/music_en.json
data/collectibles_en.json
```

Changing `SkinsLanguage` changes the suffix WeaponPaints attempts to load.

For example:

```json
"SkinsLanguage": "de"
```

causes WeaponPaints to look for:

```text
skins_de.json
gloves_de.json
agents_de.json
music_de.json
collectibles_de.json
```

The corresponding files must exist.

---

# Localization

Plugin messages use CounterStrikeSharp localization.

Language files are stored in:

```text
lang/
```

Translations can be contributed through pull requests.

---

# Item Images

When:

```json
"ShowSkinImage": true
```

WeaponPaints retrieves the image URL stored in the item JSON entry after a selection and temporarily displays it using CounterStrikeSharp center HTML.

This is currently used for selections such as:

```text
Skins
Gloves
Agents
Music kits
Pins
```

The temporary image is removed after approximately 2 seconds.

---

# Random Skins

When:

```json
"GiveRandomSkin": true
```

and the player does not have a custom paint selected for the weapon, WeaponPaints selects a random paint kit available for that weapon definition.

The random skin is applied with:

```text
seed = 0
wear = 0.01
```

`GiveRandomKnife` exists in the current configuration class but is not currently referenced by the plugin implementation.

---

# StatTrak

The default aliases are:

```text
!stattrak
!st
```

The command operates on the currently held weapon.

It only works when WeaponPaints already has a customized paint entry for that weapon.

When enabled:

- the item is given StatTrak item quality
- the current StatTrak count is applied
- kills with the weapon increment the count

StatTrak information is stored in:

```text
weapon_stattrak
weapon_stattrak_count
```

WeaponPaints synchronizes current StatTrak values to MySQL when the player disconnects.

---

# Web Interface

The repository contains a basic PHP web interface in:

```text
website/
```

The release workflow publishes it separately as:

```text
WeaponPaints-Website.zip
```

## Requirements

The existing bundled web interface requires a PHP-capable web server with:

```text
PHP >= 7.4
curl
pdo_mysql
```

The original project documentation notes testing with PHP `8.2.3` and nginx.

## Installation

Download:

```text
WeaponPaints-Website.zip
```

from the release page and extract it to your web root.

Configure:

```text
class/config.php
```

Current configuration variables are:

```php
define('SKIN_LANGUAGE', 'skins_en');

define('DB_HOST', 'localhost');
define('DB_PORT', '3306');
define('DB_NAME', '');
define('DB_USER', '');
define('DB_PASS', '');

define('WEB_STYLE_DARK', true);

define('STEAM_API_KEY', '');
define('STEAM_DOMAIN_NAME', '');
define('STEAM_LOGOUT_PAGE', '');
define('STEAM_LOGIN_PAGE', '');
```

Get a Steam Web API key from:

https://steamcommunity.com/dev/apikey

The website must use the same database as the CS2 plugin.

> [!IMPORTANT]
> The web interface does not create the WeaponPaints tables.
>
> Run and configure the CS2 plugin first so the database schema exists.

## Current web features

The bundled website currently implements:

- Steam login/logout
- knife selection
- weapon skin selection
- weapon skin preview images
- wear configuration
- seed configuration
- optional dark theme

Knife selection is written for both T and CT.

When a new weapon skin is selected through the website, it is also written for both T and CT.

The bundled website does not currently provide the same selection UI for gloves, agents, music kits, pins, stickers, keychains or name tags.

---

# Building From Source

The current project targets:

```text
net10.0
```

Install the .NET 10 SDK and clone the repository:

```bash
git clone https://github.com/Nereziel/cs2-WeaponPaints.git
cd cs2-WeaponPaints
```

Restore packages:

```bash
dotnet restore
```

Build:

```bash
dotnet build WeaponPaints.csproj -c Release
```

Current NuGet dependencies are:

```text
CounterStrikeSharp.API 1.0.376
Dapper 2.1.72
MySqlConnector 2.5.0
Newtonsoft.Json 13.0.5-beta1
```

The project also references:

```text
3rd_party/MenuManagerApi.dll
```

> [!NOTE]
> The current `.csproj` targets `.NET 10`, while the repository's current GitHub Actions workflow still explicitly requests `.NET 8.0.x`.
>
> For a manual build, follow the target framework declared by `WeaponPaints.csproj`.

---

# Updating

Download the newest:

```text
WeaponPaints.zip
```

from:

https://github.com/Nereziel/cs2-WeaponPaints/releases/latest

Extract it again into:

```text
game/csgo/
```

The release contains both:

```text
addons/counterstrikesharp/plugins/WeaponPaints/
```

and:

```text
addons/counterstrikesharp/gamedata/weaponpaints.json
```

Update both the plugin and gamedata together.

Your persistent player cosmetic data is stored in MySQL.

It is still recommended to back up:

```text
WeaponPaints.json
```

and your database before updating.

---

# Troubleshooting

## Plugin fails during configuration

WeaponPaints requires these fields:

```json
"DatabaseHost": "...",
"DatabaseUser": "...",
"DatabaseName": "..."
```

If any of these are empty, initialization fails with a database-configuration error.

Also verify `DatabasePassword` and `DatabasePort` for your MySQL server.

## Missing gamedata

WeaponPaints requires:

```text
addons/counterstrikesharp/gamedata/weaponpaints.json
```

If the file does not exist, the plugin refuses to initialize.

Extract the complete release ZIP into:

```text
game/csgo/
```

instead of copying only the DLL.

## Database tables do not exist

WeaponPaints creates the tables automatically.

If they are missing, verify that:

```text
WeaponPaints loaded successfully
Database credentials are correct
The database user can create tables
```

## Skins are not changing

Verify:

```text
addons/counterstrikesharp/configs/core.json
```

contains:

```json
"FollowCS2ServerGuidelines": false
```

Then restart the server.

## Menus are not working

Verify that all menu dependencies are installed:

```text
MenuManagerCS2
PlayerSettingsCS2
AnyBaseLibCS2
```

WeaponPaints obtains the MenuManager API during `OnAllPluginsLoaded`. If the required menu plugins cannot be loaded, WeaponPaints logs:

```text
Error while loading required plugins
```

and plugin initialization fails.

## Menu type does not work

Try the default:

```json
"MenuType": "selectable"
```

Supported values are:

```text
selectable
dynamic
center
chat
console
```

## `!wp` does nothing

The refresh command requires:

```json
"CommandWpEnabled": true
```

and:

```json
"SkinEnabled": true
```

It is also unavailable while cosmetic commands are disabled at round end.

## Commands are on cooldown

The default cooldown is:

```json
"CmdRefreshCooldownSeconds": 3
```

Change it in `WeaponPaints.json` if required.

---

# Version

Current source version:

```text
3.3a
```

WeaponPaints checks the repository's `VERSION` file at runtime and logs whether the installed plugin is up to date, outdated or probably a development version.

---

# Community

Discord:

https://discord.gg/d9CvaYPSFe

GitHub Issues:

https://github.com/Nereziel/cs2-WeaponPaints/issues

GitHub Discussions:

https://github.com/Nereziel/cs2-WeaponPaints/discussions

---

# Support Development

Consider supporting the project instead of purchasing unofficial copies from unknown sources.

Ko-fi:

https://ko-fi.com/E1E2G0P2O

Steam trade offer:

https://steamcommunity.com/tradeoffer/new/?partner=41515647&token=gW2W-nXE

---

# Preview

![WeaponPaints preview](https://github.com/Nereziel/cs2-WeaponPaints/blob/main/website/preview.png?raw=true)

---

# Disclaimer

WeaponPaints is not affiliated with or endorsed by Valve Corporation.

Using server-side cosmetic modification plugins may conflict with Valve server policies and may carry account, token or server-related risks.

Valve server guidelines:

https://blog.counter-strike.net/index.php/server_guidelines/

**Use WeaponPaints at your own risk.**

---

# License

WeaponPaints is licensed under the [GNU General Public License v3.0](LICENSE).
