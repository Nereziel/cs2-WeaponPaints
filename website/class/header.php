<?php
ini_set('display_errors', 1);
ini_set('display_startup_errors', 1);
error_reporting(E_ALL);

// Set security headers to enhance security
// header("X-Frame-Options: SAMEORIGIN");
// header("X-XSS-Protection: 1; mode=block");
// header("X-Content-Type-Options: nosniff");
// header("Referrer-Policy: no-referrer-when-downgrade");
// header("Content-Security-Policy: default-src 'self'; script-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net https://code.jquery.com; style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net; img-src 'self' data: https://cdn.jsdelivr.net https://steamcommunity-a.akamaihd.net https://raw.githubusercontent.com;");


// Include necessary classes and files
require 'class/config.php';
require 'class/database.php';
require 'steamauth/steamauth.php';
require 'class/utils.php';

// Create a database instance
$db = new DataBase();

// Check if the user is logged in
if (isset($_SESSION['steamid'])) {
    // Insert or update user's Steam ID in the database
    $steamid = $_SESSION['steamid'];
    $db->query("INSERT INTO `wp_users` (`steamid`) VALUES ('{$steamid}') ON DUPLICATE KEY UPDATE `updated_at` = CURRENT_TIMESTAMP");

    // Get user's database index
    $userInfoQuery = $db->select("SELECT `id` FROM `wp_users` WHERE `steamid` = :steamid", ["steamid" => $steamid]);
    $_SESSION['userDbIndex'] = $userDbIndex = (int)$userInfoQuery[0]['id'];

    // Get weapons and skins information
    $weapons = UtilsClass::getWeaponsFromArray();
    $skins = UtilsClass::skinsFromJson();
    $gloves = UtilsClass::glovesFromJson();

    // Retrieve user's selected skins and knife
    $querySelected = $db->select("SELECT `weapon`, `paint`, `wear`, `seed`, `nametag` FROM `wp_users_items` WHERE `user_id` = :user_id", ["user_id" => $userDbIndex]);
    $selectedSkins = UtilsClass::getSelectedSkins($querySelected);
    $selectedKnifeResult = $db->select("SELECT `knife` FROM `wp_users_knife` WHERE `user_id` = :user_id", ["user_id" => $userDbIndex]);
    $selectedGlovesResult = $db->select("SELECT `weapon_defindex` FROM `wp_users_gloves` WHERE `user_id` = :user_id", ["user_id" => $userDbIndex]);
    $selectedGloves = !empty($selectedGlovesResult) ? $selectedGlovesResult[0] : $gloves[0][0];

    // Determine user's selected knife or set default knife
    if (!empty($selectedKnifeResult)) {
        $selectedKnife = $selectedKnifeResult[0]['knife'];
    } else {
        $selectedKnife = "weapon_knife";
    }
    $knifes = UtilsClass::getKnifeTypes();

    // Handle form submission
    if (isset($_POST['forma'])) {
        $ex = explode("-", $_POST['forma']);

        // Handle knife selection
        if ($ex[0] == "knife") {
            $db->query("INSERT INTO `wp_users_knife` (`user_id`, `knife`) VALUES(:user_id, :knife) ON DUPLICATE KEY UPDATE `knife` = :knife", ["user_id" => $userDbIndex, "knife" => $knifes[$ex[1]]['weapon_name']]);
        } else {
            // Handle skin selection
            if (array_key_exists($ex[1], $skins[$ex[0]]) && isset($_POST['wear']) && $_POST['wear'] >= 0.00 && $_POST['wear'] <= 1.00 && isset($_POST['seed'])) {
                $wear = floatval($_POST['wear']); // wear
                $seed = intval($_POST['seed']); // seed

                // Check if the skin is already selected and update or insert accordingly
                if (array_key_exists($ex[0], $selectedSkins)) {
                    $db->query("UPDATE wp_users_items SET paint = :weapon_paint_id, wear = :weapon_wear, seed = :weapon_seed WHERE user_id = :user_id AND weapon = :weapon_defindex", ["user_id" => $userDbIndex, "weapon_defindex" => $ex[0], "weapon_paint_id" => $ex[1], "weapon_wear" => $wear, "weapon_seed" => $seed]);
                } else {
                    $db->query("INSERT INTO wp_users_items (`user_id`, `weapon`, `paint`, `wear`, `seed`) VALUES (:user_id, :weapon_defindex, :weapon_paint_id, :weapon_wear, :weapon_seed)", ["user_id" => $userDbIndex, "weapon_defindex" => $ex[0], "weapon_paint_id" => $ex[1], "weapon_wear" => $wear, "weapon_seed" => $seed]);
                }
            }
        }
        // Redirect to the same page after form submission
        header("Location: {$_SERVER['PHP_SELF']}");
    }
}
?>
