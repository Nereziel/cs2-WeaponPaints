CREATE TABLE IF NOT EXISTS `wp_players` (
    `user_id` INT UNSIGNED NOT NULL AUTO_INCREMENT,
    `steamid` BIGINT UNSIGNED NOT NULL,
    `created_at` TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `updated_at` TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (`user_id`),
    UNIQUE KEY `unique_steamid` (`steamid`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `wp_player_skins` (
    `user_id` INT UNSIGNED NOT NULL,
    `team` SMALLINT UNSIGNED NOT NULL,
    `weapon_defindex` SMALLINT UNSIGNED NOT NULL,
    `paint` SMALLINT UNSIGNED NOT NULL,
    `wear` FLOAT NOT NULL DEFAULT 0.001,
    `seed` SMALLINT UNSIGNED NOT NULL DEFAULT 0,
    `nametag` VARCHAR(20) DEFAULT NULL,
    `stattrack` INT UNSIGNED NOT NULL DEFAULT 0,
    `stattrack_enabled` SMALLINT NOT NULL DEFAULT 0,
    `quality` SMALLINT UNSIGNED NOT NULL DEFAULT 0,
    PRIMARY KEY (`user_id`,`team`,`weapon_defindex`),
    FOREIGN KEY (`user_id`) REFERENCES `wp_players`(`user_id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `wp_players_knife` (
    `user_id` INT UNSIGNED NOT NULL,
    `knife` VARCHAR(32) DEFAULT NULL,
    PRIMARY KEY (`user_id`),
    FOREIGN KEY (`user_id`) REFERENCES `wp_players`(`user_id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `wp_players_gloves` (
    `user_id` INT UNSIGNED NOT NULL,
    `weapon_defindex` SMALLINT UNSIGNED DEFAULT NULL,
    `team` SMALLINT UNSIGNED DEFAULT NULL,
    PRIMARY KEY (`user_id`,`team`),
    FOREIGN KEY (`user_id`) REFERENCES `wp_players`(`user_id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `wp_players_music` (
    `user_id` INT UNSIGNED NOT NULL,
    `music` SMALLINT UNSIGNED DEFAULT NULL,
    PRIMARY KEY (`user_id`),
    FOREIGN KEY (`user_id`) REFERENCES `wp_players`(`user_id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `wp_players_agents` (
    `user_id` INT UNSIGNED NOT NULL,
    `agent_ct` varchar(64) DEFAULT NULL,
    `agent_t` varchar(64) DEFAULT NULL,
    PRIMARY KEY (`user_id`),
    FOREIGN KEY (`user_id`) REFERENCES `wp_players`(`user_id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;