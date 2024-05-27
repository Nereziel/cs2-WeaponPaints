PRAGMA foreign_keys = ON;

CREATE TABLE IF NOT EXISTS `wp_players` (
                                            `user_id` INTEGER PRIMARY KEY AUTOINCREMENT,
                                            `steamid` INTEGER NOT NULL,
                                            `created_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                                            `updated_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                                            UNIQUE(`steamid`)
    );

CREATE TABLE IF NOT EXISTS `wp_player_skins` (
                                                 `user_id` INTEGER NOT NULL,
                                                 `team` INTEGER NOT NULL,
                                                 `weapon_defindex` INTEGER NOT NULL,
                                                 `paint` INTEGER NOT NULL,
                                                 `wear` REAL NOT NULL DEFAULT 0.001,
                                                 `seed` INTEGER NOT NULL DEFAULT 0,
                                                 `nametag` TEXT DEFAULT NULL,
                                                 `stattrack` INTEGER NOT NULL DEFAULT 0,
                                                 `stattrack_enabled` INTEGER NOT NULL DEFAULT 0,
                                                 `quality` INTEGER NOT NULL DEFAULT 0,
                                                 PRIMARY KEY (`user_id`,`team`,`weapon_defindex`),
    FOREIGN KEY (`user_id`) REFERENCES `wp_players`(`user_id`) ON DELETE CASCADE
    );

CREATE TABLE IF NOT EXISTS `wp_players_knife` (
                                                  `user_id` INTEGER NOT NULL,
                                                  `knife` TEXT DEFAULT NULL,
                                                  PRIMARY KEY (`user_id`),
    FOREIGN KEY (`user_id`) REFERENCES `wp_players`(`user_id`) ON DELETE CASCADE
    );

CREATE TABLE IF NOT EXISTS `wp_players_gloves` (
                                                   `user_id` INTEGER NOT NULL,
                                                   `weapon_defindex` INTEGER DEFAULT NULL,
                                                   `team` INTEGER DEFAULT NULL,
                                                   PRIMARY KEY (`user_id`,`team`),
    FOREIGN KEY (`user_id`) REFERENCES `wp_players`(`user_id`) ON DELETE CASCADE
    );

CREATE TABLE IF NOT EXISTS `wp_players_music` (
                                                  `user_id` INTEGER NOT NULL,
                                                  `music` INTEGER DEFAULT NULL,
                                                  PRIMARY KEY (`user_id`),
    FOREIGN KEY (`user_id`) REFERENCES `wp_players`(`user_id`) ON DELETE CASCADE
    );

CREATE TABLE IF NOT EXISTS `wp_players_agents` (
                                                   `user_id` INTEGER NOT NULL,
                                                   `agent_ct` TEXT DEFAULT NULL,
                                                   `agent_t` TEXT DEFAULT NULL,
                                                   PRIMARY KEY (`user_id`),
    FOREIGN KEY (`user_id`) REFERENCES `wp_players`(`user_id`) ON DELETE CASCADE
    );