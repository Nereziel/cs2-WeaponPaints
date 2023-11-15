<?php
class UtilsClass
{
    public static function skinsFromJson(): array
    {
        $skins = [];
        $json = json_decode(file_get_contents(__DIR__ . "/../data/skins.json"), true);

        foreach ($json as $skin) {
            $skins[(int)$skin['weapon_defindex']][(int)$skin['paint']] = [
                'paint_name' => $skin['paint_name'],
                'image_url' => $skin['image'],
            ];
        }

        return $skins;
    }

    public static function getWeaponsFromArray()
    {
        $weapons = [];
        $temp = self::skinsFromJson();

        foreach ($temp as $key => $value)
        {
            if (key_exists($key, $weapons)) continue;

            $weapons[$key] = [
                'paint_name' => $value[0]['paint_name'],
                'image_url' => $value[0]['image_url'],
            ];
        }

        return $weapons;
    }

    public static function getSelectedSkins(array $temp)
    {
        $selected = [];

        foreach ($temp as $weapon)
        {
            $selected[$weapon['weapon_defindex']] = $weapon['weapon_paint_id'];
        }

        return $selected;
    }
}