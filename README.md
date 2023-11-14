# cs2-WeaponPaints

[![ko-fi](https://ko-fi.com/img/githubbutton_sm.svg)](https://ko-fi.com/E1E2G0P2O) or [Donate on Steam](https://steamcommunity.com/tradeoffer/new/?partner=41515647&token=gW2W-nXE)

### Use this plugin at your own risk! Using this may lead to GSLT ban or something else Valve come with. [Valve Server guidelines](https://blog.counter-strike.net/index.php/server_guidelines/)

### Description
Unfinished, unoptimized and not fully functional ugly demo weapon paints plugin for [CSSharp](https://docs.cssharp.dev/).

### Features
- changes only paint, seed, wear on weapons
- mysql based
- data sync on player connect or playe
- Added command `!wp` to refresh skins (with cooldown in second can be configured)
- Added command `!ws` to show website

### CS2 server:
- compile and copy plugin to plugins. Info here [https://docs.cssharp.dev/guides/hello-world-plugin/](https://docs.cssharp.dev/guides/hello-world-plugin/)
- setup `addons/counterstrikesharp/configs/plugins/WeaponPaints/WeaponPaints.json` with database credentials
- in `addons/counterstrikesharp/configs/core.json` set **FollowCS2ServerGuidelines** to **false**

### Web install:
- copy website to web server
- import `database.sql` to mysql
- get steam api key [https://steamcommunity.com/dev/apikey](https://steamcommunity.com/dev/apikey)
- fill in database credentials and api key in `class/config.php`
- visit website and login via steam

### Preview
![preview](https://github.com/Nereziel/cs2-WeaponPaints/blob/main/website/preview.png?raw=true)
