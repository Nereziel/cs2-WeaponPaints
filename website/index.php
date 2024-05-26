<?php
require 'class/header.php';
?>
<!DOCTYPE html>
<html lang="en" <?php if (WEB_STYLE_DARK) echo 'data-bs-theme="dark"' ?>>
<head>
    <meta charset="utf-8">
    <link href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.2/dist/css/bootstrap.min.css" rel="stylesheet" integrity="sha384-T3c6CoIi6uLrA9TneNEoa7RxnatzjcDSCmG1MXxSR1GAsXEV/Dwwykc2MPK8M2HN" crossorigin="anonymous">
    <script src="https://code.jquery.com/jquery-3.6.4.min.js"></script>
    <script src="https://cdn.jsdelivr.net/npm/bootstrap@5.3.2/dist/js/bootstrap.bundle.min.js" integrity="sha384-C6RzsynM9kWDrMNeT87bh95OGNyZPhcTNXj1NW7RuBCsyN/o0jlpcV8Qyq46cDfL" crossorigin="anonymous"></script>
    <script src="https://cdn.jsdelivr.net/npm/bootstrap@4.6.0/dist/js/bootstrap.min.js"></script>
    <link rel="stylesheet" href="style.css">
    <title>CS2 Simple Weapon Paints</title>
</head>
<body>
    <?php if (!isset($_SESSION['steamid'])) : ?>
        <div class='bg-primary'><h2>To choose weapon paints loadout, you need to <?php loginbutton("rectangle"); ?></h2></div>
    <?php else : ?>
        <div class='bg-primary'><h2>Your current weapon skin loadout <a class='btn btn-danger' href='<?php echo $_SERVER['PHP_SELF']; ?>?logout'>Logout</a></h2> </div>
        <div class='card-group mt-2'>
            <!-- Display user's selected knife -->
            <?php require_once 'view/display_knife.php'; ?>
            <!-- Display user's selected skins for different weapons -->
            <?php require_once 'view/display_weapons.php'; ?>
        </div>
    <?php endif; ?>
    <!-- Footer section -->
    <div class="container">
        <footer class="d-flex flex-wrap justify-content-between align-items-center py-3 my-4 border-top">
            <div class="col-md-4 d-flex align-items-center">
                <span class="mb-3 mb-md-0 text-body-secondary">© 2024 <a href="https://github.com/Nereziel/cs2-WeaponPaints">Nereziel/cs2-WeaponPaints</a></span>
            </div>
        </footer>
    </div>
</body>
</html>
