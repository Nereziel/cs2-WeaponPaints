<?php
class UtilsClass
{
    // Knife defindexes as constants for better maintainability
    private const KNIFE_DEFINDEXES = [
        500, 503, 505, 506, 507, 508, 509, 512, 514, 515, 516, 517, 518, 519, 520, 521, 522, 523, 525, 526
    ];

    // Knife mapping for better maintainability
    private const KNIFE_MAPPING = [
        500 => 'weapon_bayonet',
        503 => 'weapon_knife_css',
        505 => 'weapon_knife_flip',
        506 => 'weapon_knife_gut',
        507 => 'weapon_knife_karambit',
        508 => 'weapon_knife_m9_bayonet',
        509 => 'weapon_knife_tactical',
        512 => 'weapon_knife_falchion',
        514 => 'weapon_knife_survival_bowie',
        515 => 'weapon_knife_butterfly',
        516 => 'weapon_knife_push',
        517 => 'weapon_knife_cord',
        518 => 'weapon_knife_canis',
        519 => 'weapon_knife_ursus',
        520 => 'weapon_knife_gypsy_jackknife',
        521 => 'weapon_knife_outdoor',
        522 => 'weapon_knife_stiletto',
        523 => 'weapon_knife_widowmaker',
        525 => 'weapon_knife_skeleton',
        526 => 'weapon_knife_css'
    ];

    // Weapon categories for better organization
    private const WEAPON_CATEGORIES = [
        'Rifles' => [7, 8, 10, 13, 16, 60, 39, 40, 38],
        'Pistols' => [1, 2, 3, 4, 30, 32, 36, 61, 63, 64],
        'SMGs' => [17, 19, 24, 26, 33, 34],
        'Shotguns' => [25, 27, 29, 35],
        'Snipers' => [9, 11, 38],
        'Machine Guns' => [14, 28],
        'Grenades' => [43, 44, 45, 46, 47, 48]
    ];

    private static $skinCache = null;
    private static $weaponCache = null;
    private static $knifeCache = null;

    public static function getKnifeDefindexes(): array
    {
        return self::KNIFE_DEFINDEXES;
    }

    public static function getKnifeMapping(): array
    {
        return self::KNIFE_MAPPING;
    }

    public static function getWeaponCategories(): array
    {
        return self::WEAPON_CATEGORIES;
    }

    public static function skinsFromJson(): array
    {
        if (self::$skinCache !== null) {
            return self::$skinCache;
        }

        $skins = [];
        $jsonFile = __DIR__ . "/../data/" . SKIN_LANGUAGE . ".json";
        
        if (!file_exists($jsonFile)) {
            return [];
        }

        $json = json_decode(file_get_contents($jsonFile), true);
        if (!$json) {
            return [];
        }

        foreach ($json as $skin) {
            $defindex = (int) $skin['weapon_defindex'];
            $paintId = (int) $skin['paint'];
            
            $skins[$defindex][$paintId] = [
                'weapon_name' => $skin['weapon_name'],
                'paint_name' => $skin['paint_name'],
                'image_url' => $skin['image'],
            ];
        }

        self::$skinCache = $skins;
        return $skins;
    }

    public static function getWeaponsFromArray(): array
    {
        if (self::$weaponCache !== null) {
            return self::$weaponCache;
        }

        $weapons = [];
        $skins = self::skinsFromJson();

        foreach ($skins as $defindex => $skinList) {
            if (!isset($weapons[$defindex]) && isset($skinList[0])) {
                $weapons[$defindex] = [
                    'weapon_name' => $skinList[0]['weapon_name'],
                    'paint_name' => $skinList[0]['paint_name'],
                    'image_url' => $skinList[0]['image_url'],
                ];
            }
        }

        self::$weaponCache = $weapons;
        return $weapons;
    }

    public static function getKnifeTypes(): array
    {
        if (self::$knifeCache !== null) {
            return self::$knifeCache;
        }

        $knifes = [];
        $weapons = self::getWeaponsFromArray();

        foreach (self::KNIFE_DEFINDEXES as $defindex) {
            if (isset($weapons[$defindex])) {
                $weapon = $weapons[$defindex];
                $knifes[$defindex] = [
                    'weapon_name' => $weapon['weapon_name'],
                    'paint_name' => rtrim(explode("|", $weapon['paint_name'])[0]),
                    'image_url' => $weapon['image_url'],
                ];
            }
        }

        // Add default knife
        $knifes[0] = [
            'weapon_name' => "weapon_knife",
            'paint_name' => "Default knife",
            'image_url' => "https://raw.githubusercontent.com/Nereziel/cs2-WeaponPaints/main/website/img/skins/weapon_knife.png",
        ];

        ksort($knifes);
        self::$knifeCache = $knifes;
        return $knifes;
    }

    public static function getSelectedSkins(array $queryResult): array
    {
        $selected = [];

        foreach ($queryResult as $weapon) {
            $selected[$weapon['weapon_defindex']] = [
                'weapon_paint_id' => $weapon['weapon_paint_id'],
                'weapon_seed' => $weapon['weapon_seed'],
                'weapon_wear' => $weapon['weapon_wear'],
            ];
        }

        return $selected;
    }

    public static function isKnifeDefindex(int $defindex): bool
    {
        return in_array($defindex, self::KNIFE_DEFINDEXES);
    }

    public static function isKnifeWeapon(array $weapon): bool
    {
        return $weapon['weapon_name'] === 'weapon_knife' || 
               strpos($weapon['weapon_name'], 'knife') !== false ||
               strpos($weapon['paint_name'], '★') !== false;
    }

    public static function clearCache(): void
    {
        self::$skinCache = null;
        self::$weaponCache = null;
        self::$knifeCache = null;
    }
}
