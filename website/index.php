<?php
require_once 'class/config.php';
require_once 'class/database.php';
require_once 'steamauth/steamauth.php';
# You would uncomment the line beneath to make it refresh the data every time the page is loaded
// unset($_SESSION['steam_uptodate']);

$db = new DataBase();

//$steamid = $_GET['steamid'];
if(isset($_SESSION['steamid']))
{
	include ('steamauth/userInfo.php');
	$steamid = $steamprofile['steamid'];
}


if(isset($_POST['forma'])) {
	$ex = explode("-", $_POST['forma']);
	
	$query2 = $db->select("SELECT * FROM wp_weapons_paints WHERE weapon_defindex = :weapon_defindex AND paint = :paint", ["weapon_defindex" => $ex[0], "paint" => $ex[1]]);

	if($query2) {
		$check = $db->select("SELECT * FROM wp_weapons_paints LEFT JOIN wp_player_skins ON wp_player_skins.weapon_paint_id = wp_weapons_paints.paint WHERE wp_weapons_paints.weapon_defindex = :weapon_defindex AND wp_player_skins.steamid = :steamid", ["weapon_defindex" => $ex[0], "steamid" => $steamid]);
		if($check) {
			$db->query("UPDATE wp_player_skins SET weapon_paint_id = :weapon_paint_id WHERE steamid = :steamid AND weapon_defindex = :weapon_defindex", ["steamid" => $steamid, "weapon_defindex" => $ex[0], "weapon_paint_id" => $query2[0]['paint']]);
		} else {
			$db->query("INSERT INTO wp_player_skins (`steamid`, `weapon_defindex`, `weapon_paint_id`) VALUES (:steamid, :weapon_defindex, :weapon_paint_id)", ["steamid" => $steamid, "weapon_defindex" => $ex[0],"weapon_paint_id" => $query2[0]["paint"]]);
		}
	}
}

?>

<!DOCTYPE html>
<html lang="en">
  <head>
    <meta charset="utf-8">
	<link href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.2/dist/css/bootstrap.min.css" rel="stylesheet" integrity="sha384-T3c6CoIi6uLrA9TneNEoa7RxnatzjcDSCmG1MXxSR1GAsXEV/Dwwykc2MPK8M2HN" crossorigin="anonymous">
	<script src="https://cdn.jsdelivr.net/npm/bootstrap@5.3.2/dist/js/bootstrap.bundle.min.js" integrity="sha384-C6RzsynM9kWDrMNeT87bh95OGNyZPhcTNXj1NW7RuBCsyN/o0jlpcV8Qyq46cDfL" crossorigin="anonymous"></script>
	<link rel="stylesheet" href="style.css">
	<title>CS2 Simple Weapon Paints</title>
</head>
<body>


<?php
if(!isset($_SESSION['steamid']))
{
	echo "<div class='bg-primary'><h2>To choose weapon paints loadout, you need to ";
	loginbutton("rectangle");
	echo "</h2></div>";
}
else
{
	echo "<div class='bg-primary'>Your current weapon skin loadout<form action='' method='get'><button class='btn btn-secondary' name='logout' type='submit'>Logout</button></form></div>";
	echo "<div class='card-group'>";
	$query = $db->select("SELECT * FROM wp_weapons_paints GROUP BY weapon_defindex ORDER BY weapon_defindex");
	foreach($query as $key) { ?>
    <div class="col-sm-2">
        <div class="card text-center mb-3">
			<div class="card-body">
				<?php
				if($query3 = $db->select("SELECT * FROM wp_weapons_paints LEFT JOIN wp_player_skins ON wp_player_skins.weapon_paint_id = wp_weapons_paints.paint WHERE wp_player_skins.steamid = :steamid AND wp_weapons_paints.weapon_defindex = :weapon_defindex", ["steamid" => $steamid, "weapon_defindex" => $key['weapon_defindex']]))
				{
					echo "<div class='card-header'>";
					echo"<h5 class='card-title item-name'>{$query3[0]["paint_name"]}</h5>";
					echo "</div>";
					echo "<img src='{$query3[0]["image"]}' class='skin-image' >";
				}
				else
				{
					echo "<div class='card-header'>";
					echo"<h5 class='card-title item-name'>{$key["paint_name"]}</h5>";
					echo "</div>";
					echo"<img src='{$key["image"]}' class='skin-image'>";
				}
				?>
			</div>
			<div class="card-footer">
				<form action="" method="POST">
					<select name="forma" class="form-control select" onchange="this.form.submit()" class="SelectWeapon">
					<option>Select skin</option>
					<?php
					$list = $db->select("SELECT * FROM wp_weapons_paints WHERE weapon_defindex = \"{$key["weapon_defindex"]}\"");
					foreach($list as $list){
						echo "<option value=\"{$list['weapon_defindex']}-{$list['paint']}\">{$list['paint_name']}</option>";
					}
					?>
					</select>
				</form>
			</div>
        </div>
    </div>
	<?php } ?>
	</div>
<?php } ?>


</body>
</html>