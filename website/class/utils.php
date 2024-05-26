<?php
/**
 * Class UtilsClass
 *
 * Provides utility methods for handling skin and weapon data.
 */
class UtilsClass
{
    /**
     * Retrieve skins data from the JSON file.
     *
     * @return array An associative array containing skin data.
     */
    public static function skinsFromJson()
    {
        $skins = array();
        $jsonFilePath = __DIR__ . "/../data/skins.json";

        if (file_exists($jsonFilePath) && is_readable($jsonFilePath)) {
            $json = json_decode(file_get_contents($jsonFilePath), true);

            foreach ($json as $skin) {
                $skins[(int) $skin['weapon_defindex']][(int) $skin['paint']] = array(
                    'weapon_name' => $skin['weapon_name'],
                    'paint_name' => $skin['paint_name'],
                    'image_url' => $skin['image'],
                );
            }
        } else {
            // Handle file not found or unreadable error
            // You can throw an exception or log an error message
        }

        return $skins;
    }

    /**
     * Retrieve weapons data from the skin data array.
     *
     * @return array An associative array containing weapon data.
     */
    public static function getWeaponsFromArray()
    {
        $weapons = array();
        $skinsData = self::skinsFromJson();

        foreach ($skinsData as $key => $value) {
            $weapons[$key] = array(
                'weapon_name' => $value[0]['weapon_name'],
                'paint_name' => $value[0]['paint_name'],
                'image_url' => $value[0]['image_url'],
            );
        }

        return $weapons;
    }

    /**
     * Retrieve knife types from the weapon data array.
     *
     * @return array An associative array containing knife types data.
     */
    public static function getKnifeTypes()
    {
        $knifes = array();
        $weaponsData = self::getWeaponsFromArray();

        $allowedKnifeKeys = array(
            500, 503, 505, 506, 507, 508, 509, 512, 514, 515,
            516, 517, 518, 519, 520, 521, 522, 523, 525
        );

        foreach ($weaponsData as $key => $weapon) {
            if (in_array($key, $allowedKnifeKeys)) {
                $knifes[$key] = array(
                    'weapon_name' => $weapon['weapon_name'],
                    'paint_name' => rtrim(explode("|", $weapon['paint_name'])[0]),
                    'image_url' => $weapon['image_url'],
                );
            }
        }

        // Add default knife
        $knifes[0] = array(
            'weapon_name' => "weapon_knife",
            'paint_name' => "Default knife",
            'image_url' => "https://raw.githubusercontent.com/Nereziel/cs2-WeaponPaints/main/website/img/skins/weapon_knife.png",
        );

        ksort($knifes);
        return $knifes;
    }

    /**
     * Retrieve selected skins data from the database result.
     *
     * @param array $temp An array containing the selected skins data.
     * @return array An associative array containing selected skins data.
     */
    public static function getSelectedSkins($temp)
    {
        $selected = array();

        foreach ($temp as $weapon) {
            $selected[$weapon['weapon']] =  array(
                'weapon_paint_id' => $weapon['paint'],
                'weapon_seed' => $weapon['seed'],
                'weapon_wear' => $weapon['wear'],
            );
        }

        return $selected;
    }
}
