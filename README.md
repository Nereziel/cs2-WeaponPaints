# cs2-WeaponPaints

### Description
Unfinished, unoptimized and not fully functional ugly demo weapon paints plugin for [CSSharp](https://docs.cssharp.dev/).
There will be a lot of frequent changes which may break functionality or compatibility. You have been warned!

## Created [Discord server](https://discord.gg/mwEQppJ5AT) where you can discus about plugin.

### Consider to donate instead of buying from unknown sources.
[![ko-fi](https://ko-fi.com/img/githubbutton_sm.svg)](https://ko-fi.com/E1E2G0P2O) or [Donate on Steam](https://steamcommunity.com/tradeoffer/new/?partner=41515647&token=gW2W-nXE)

### Features
- changes only paint, seed and wear on weapons and knives
- mysql based
- data sync on player connect
- Added command `!wp` to refresh skins (with cooldown in second can be configured)
- Added command `!ws` to show website
- Added command `!knife` to show menu with knives
- Knife change is now limited to have these cvars empty `mp_t_default_melee ""` and `mp_ct_default_melee ""`

### CS2 server:
- compile and copy plugin to plugins. Info here [https://docs.cssharp.dev/guides/hello-world-plugin/](https://docs.cssharp.dev/guides/hello-world-plugin/)
- setup `addons/counterstrikesharp/configs/plugins/WeaponPaints/WeaponPaints.json` with database credentials
- in `addons/counterstrikesharp/configs/core.json` set **FollowCS2ServerGuidelines** to **false**

### Web install:
- requires PHP min v7.3 (tested on php ver `8.2.3` and nginx webserver)
- copy website to web server (img folder not needed)
- import `database.sql` to mysql
- get steam api key [https://steamcommunity.com/dev/apikey](https://steamcommunity.com/dev/apikey)
- fill in database credentials and api key in `class/config.php`
- visit website and login via steam

### Use this plugin at your own risk! Using this may lead to GSLT ban or something else Valve come with. [Valve Server guidelines](https://blog.counter-strike.net/index.php/server_guidelines/)

### Preview
![preview](https://github.com/Nereziel/cs2-WeaponPaints/blob/main/website/preview.png?raw=true)
