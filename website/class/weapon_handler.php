<?php
require_once 'utils.php';
require_once 'database.php';

class WeaponHandler
{
    private $db;
    private $steamid;

    public function __construct($steamid)
    {
        $this->db = new DataBase();
        $this->steamid = $steamid;
    }

    public function handleWeaponUpdate($postData): bool
    {
        if (!isset($postData['forma'])) {
            return false;
        }

        $formaParts = explode("-", $postData['forma']);
        
        if ($formaParts[0] === "knife") {
            return $this->handleKnifeSelection($formaParts[1]);
        } else {
            return $this->handleWeaponSkin($formaParts, $postData);
        }
    }

    private function handleKnifeSelection($knifeId): bool
    {
        $knifes = UtilsClass::getKnifeTypes();
        
        if (!isset($knifes[$knifeId])) {
            return false;
        }

        $knifeData = $knifes[$knifeId];
        
        // Clear existing knife data
        $this->clearKnifeData();
        
        // Set new knife selection (insert for both teams separately)
        $this->db->query(
            "INSERT INTO `wp_player_knife` (`steamid`, `knife`, `weapon_team`) VALUES (:steamid, :knife, 2)",
            ["steamid" => $this->steamid, "knife" => $knifeData['weapon_name']]
        );
        
        $this->db->query(
            "INSERT INTO `wp_player_knife` (`steamid`, `knife`, `weapon_team`) VALUES (:steamid, :knife, 3)",
            ["steamid" => $this->steamid, "knife" => $knifeData['weapon_name']]
        );

        return true;
    }

    private function handleWeaponSkin($formaParts, $postData): bool
    {
        $defindex = $formaParts[0];
        $paintId = $formaParts[1];
        
        $skins = UtilsClass::skinsFromJson();
        
        if (!isset($skins[$defindex][$paintId]) || 
            !isset($postData['wear']) || 
            !isset($postData['seed'])) {
            return false;
        }

        $wear = $this->validateWear($postData['wear']);
        $seed = $this->validateSeed($postData['seed']);
        
        if ($wear === false || $seed === false) {
            return false;
        }

        // Handle knife skins
        if (UtilsClass::isKnifeDefindex($defindex)) {
            $this->handleKnifeSkin($defindex, $paintId, $wear, $seed);
        } else {
            $this->handleRegularWeaponSkin($defindex, $paintId, $wear, $seed);
        }

        return true;
    }

    private function handleKnifeSkin($defindex, $paintId, $wear, $seed): void
    {
        $knifeMapping = UtilsClass::getKnifeMapping();
        
        // Clear existing knife data
        $this->clearKnifeData();
        
        // Clear other knife skins
        $knifeDefindexes = UtilsClass::getKnifeDefindexes();
        foreach ($knifeDefindexes as $knifeDefindex) {
            if ($knifeDefindex != $defindex) {
                $this->db->query(
                    "DELETE FROM `wp_player_skins` WHERE `steamid` = :steamid AND `weapon_defindex` = :weapon_defindex",
                    ["steamid" => $this->steamid, "weapon_defindex" => $knifeDefindex]
                );
            }
        }
        
        // Set knife type in wp_player_knife table
        if (isset($knifeMapping[$defindex])) {
            $this->db->query(
                "INSERT INTO `wp_player_knife` (`steamid`, `knife`, `weapon_team`) VALUES (:steamid, :knife, 2)",
                ["steamid" => $this->steamid, "knife" => $knifeMapping[$defindex]]
            );
            
            $this->db->query(
                "INSERT INTO `wp_player_knife` (`steamid`, `knife`, `weapon_team`) VALUES (:steamid, :knife, 3)",
                ["steamid" => $this->steamid, "knife" => $knifeMapping[$defindex]]
            );
        }
        
        // Set knife skin
        $this->upsertWeaponSkin($defindex, $paintId, $wear, $seed);
    }

    private function handleRegularWeaponSkin($defindex, $paintId, $wear, $seed): void
    {
        $this->upsertWeaponSkin($defindex, $paintId, $wear, $seed);
    }

    private function upsertWeaponSkin($defindex, $paintId, $wear, $seed): void
    {
        $selectedSkins = $this->getSelectedSkins();
        
        if (array_key_exists($defindex, $selectedSkins)) {
            // Update existing
            $this->db->query(
                "UPDATE wp_player_skins SET weapon_paint_id = :weapon_paint_id, weapon_wear = :weapon_wear, weapon_seed = :weapon_seed WHERE steamid = :steamid AND weapon_defindex = :weapon_defindex",
                [
                    "weapon_paint_id" => $paintId,
                    "weapon_wear" => $wear,
                    "weapon_seed" => $seed,
                    "steamid" => $this->steamid,
                    "weapon_defindex" => $defindex
                ]
            );
        } else {
            // Insert new for both teams
            $this->db->query(
                "INSERT INTO wp_player_skins (`steamid`, `weapon_defindex`, `weapon_paint_id`, `weapon_wear`, `weapon_seed`, `weapon_team`) VALUES (:steamid, :weapon_defindex, :weapon_paint_id, :weapon_wear, :weapon_seed, 2)",
                [
                    "steamid" => $this->steamid,
                    "weapon_defindex" => $defindex,
                    "weapon_paint_id" => $paintId,
                    "weapon_wear" => $wear,
                    "weapon_seed" => $seed
                ]
            );
            
            $this->db->query(
                "INSERT INTO wp_player_skins (`steamid`, `weapon_defindex`, `weapon_paint_id`, `weapon_wear`, `weapon_seed`, `weapon_team`) VALUES (:steamid, :weapon_defindex, :weapon_paint_id, :weapon_wear, :weapon_seed, 3)",
                [
                    "steamid" => $this->steamid,
                    "weapon_defindex" => $defindex,
                    "weapon_paint_id" => $paintId,
                    "weapon_wear" => $wear,
                    "weapon_seed" => $seed
                ]
            );
        }
    }

    private function clearKnifeData(): void
    {
        $knifeDefindexes = UtilsClass::getKnifeDefindexes();
        
        // Clear knife skins
        foreach ($knifeDefindexes as $knifeDefindex) {
            $this->db->query(
                "DELETE FROM `wp_player_skins` WHERE `steamid` = :steamid AND `weapon_defindex` = :weapon_defindex",
                ["steamid" => $this->steamid, "weapon_defindex" => $knifeDefindex]
            );
        }
        
        // Clear basic knife selection
        $this->db->query(
            "DELETE FROM `wp_player_knife` WHERE `steamid` = :steamid",
            ["steamid" => $this->steamid]
        );
    }

    private function validateWear($wear)
    {
        $wear = floatval($wear);
        return ($wear >= 0.00 && $wear <= 1.00) ? $wear : false;
    }

    private function validateSeed($seed)
    {
        $seed = intval($seed);
        return ($seed >= 0) ? $seed : false;
    }

    public function getSelectedSkins(): array
    {
        $query = $this->db->select(
            "SELECT `weapon_defindex`, MAX(`weapon_paint_id`) AS `weapon_paint_id`, MAX(`weapon_wear`) AS `weapon_wear`, MAX(`weapon_seed`) AS `weapon_seed`
            FROM `wp_player_skins`
            WHERE `steamid` = :steamid
            GROUP BY `weapon_defindex`, `steamid`",
            ["steamid" => $this->steamid]
        );
        
        return UtilsClass::getSelectedSkins($query ?: []);
    }

    public function getSelectedKnife(): array
    {
        return $this->db->select(
            "SELECT * FROM `wp_player_knife` WHERE `steamid` = :steamid LIMIT 1",
            ["steamid" => $this->steamid]
        ) ?: [];
    }

    public function getLoadoutData(): array
    {
        $weapons = UtilsClass::getWeaponsFromArray();
        $knifes = UtilsClass::getKnifeTypes();
        $selectedSkins = $this->getSelectedSkins();
        $selectedKnife = $this->getSelectedKnife();

        return [
            'weapons' => $weapons,
            'knifes' => $knifes,
            'selectedSkins' => $selectedSkins,
            'selectedKnife' => $selectedKnife,
            'displayKnife' => $this->getDisplayKnife($selectedSkins, $selectedKnife, $knifes)
        ];
    }

    private function getDisplayKnife($selectedSkins, $selectedKnife, $knifes): array
    {
        $skins = UtilsClass::skinsFromJson();
        
        // Check for knife skin first
        foreach ($selectedSkins as $defindex => $selectedSkin) {
            if (UtilsClass::isKnifeDefindex($defindex) && isset($skins[$defindex][$selectedSkin['weapon_paint_id']])) {
                return [
                    'data' => $skins[$defindex][$selectedSkin['weapon_paint_id']],
                    'source' => 'skin'
                ];
            }
        }
        
        // Check for basic knife selection
        if (!empty($selectedKnife)) {
            foreach ($knifes as $knife) {
                if ($selectedKnife[0]['knife'] === $knife['weapon_name']) {
                    return [
                        'data' => $knife,
                        'source' => 'basic'
                    ];
                }
            }
        }
        
        // Default knife
        return [
            'data' => $knifes[0] ?? null,
            'source' => 'default'
        ];
    }

    public function getOrganizedWeapons(): array
    {
        $weapons = UtilsClass::getWeaponsFromArray();
        $knifes = UtilsClass::getKnifeTypes();
        $categories = UtilsClass::getWeaponCategories();
        
        $organized = [
            'Knives' => [],
            'Gloves' => []
        ];

        // Add weapon categories
        foreach ($categories as $categoryName => $weaponIds) {
            $organized[$categoryName] = [];
            foreach ($weaponIds as $weaponId) {
                if (isset($weapons[$weaponId])) {
                    $organized[$categoryName][$weaponId] = $weapons[$weaponId];
                }
            }
        }

        // Add knives (exclude default)
        foreach ($knifes as $knifeId => $knife) {
            if ($knifeId !== 0) {
                $organized['Knives'][$knifeId] = $knife;
            }
        }

        // Remove empty categories
        return array_filter($organized, function($category) {
            return !empty($category);
        });
    }
} 