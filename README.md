# cs2-WeaponPaints

Unfinished, unoptimized and not fully functional ugly demo weapon paints plugin for [CSSharp](https://docs.cssharp.dev/).

### Features
- changes only paint on weapons
- mysql base
- data sync on player connect or player can type !wp to refresh skins (command has hardcoded 2 minute cooldown for now)

### CS2 server:
- Compile and copy plugin to plugins. Info here [https://docs.cssharp.dev/guides/hello-world-plugin/](https://docs.cssharp.dev/guides/hello-world-plugin/)
- setup config.json with database credentials

### Web install:
- copy website to web server
- import database.sql to mysql
- get steam api key [https://steamcommunity.com/dev/apikey](https://steamcommunity.com/dev/apikey)
- setup class/config.php
- visit website and login via steam

### Preview
![preview](https://github.com/Nereziel/cs2-WeaponPaints/blob/main/website/preview.png?raw=true)
