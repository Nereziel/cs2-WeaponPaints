<?php
require_once 'class/config.php';
require_once 'class/database.php';
require_once 'steamauth/steamauth.php';
require_once 'class/utils.php';

$db = new DataBase();
if (isset($_SESSION['steamid'])) {

	$steamid = $_SESSION['steamid'];
	
	// Fetch Steam user information
	require_once 'steamauth/userInfo.php';

	$weapons = UtilsClass::getWeaponsFromArray();
	$skins = UtilsClass::skinsFromJson();
    $querySelected = $db->select("
        SELECT `weapon_defindex`, MAX(`weapon_paint_id`) AS `weapon_paint_id`, MAX(`weapon_wear`) AS `weapon_wear`, MAX(`weapon_seed`) AS `weapon_seed`
        FROM `wp_player_skins`
        WHERE `steamid` = :steamid
        GROUP BY `weapon_defindex`, `steamid`
    ", ["steamid" => $steamid]);
	$selectedSkins = UtilsClass::getSelectedSkins($querySelected);
	$selectedKnife = $db->select("SELECT * FROM `wp_player_knife` WHERE `wp_player_knife`.`steamid` = :steamid LIMIT 1", ["steamid" => $steamid]);
	$knifes = UtilsClass::getKnifeTypes();

	if (isset($_POST['forma'])) {
		$ex = explode("-", $_POST['forma']);

		if ($ex[0] == "knife") {
			// Handle knife selection - use the knife key directly
			if (isset($knifes[$ex[1]])) {
				$knifeData = $knifes[$ex[1]];
				
				// Clear any existing knife skins first
				$db->query("DELETE FROM `wp_player_skins` WHERE `steamid` = :steamid AND `weapon_defindex` IN (500, 503, 505, 506, 507, 508, 509, 512, 514, 515, 516, 517, 518, 519, 520, 521, 522, 523, 525, 526)", ["steamid" => $steamid]);
				
				// Clear any existing basic knife selection first
				$db->query("DELETE FROM `wp_player_knife` WHERE `steamid` = :steamid", ["steamid" => $steamid]);
				
				// Set the new basic knife selection
				$db->query("INSERT INTO `wp_player_knife` (`steamid`, `knife`, `weapon_team`) VALUES(:steamid, :knife, 2)", ["steamid" => $steamid, "knife" => $knifeData['weapon_name']]);
				$db->query("INSERT INTO `wp_player_knife` (`steamid`, `knife`, `weapon_team`) VALUES(:steamid, :knife, 3)", ["steamid" => $steamid, "knife" => $knifeData['weapon_name']]);
			}
		} else {
			if (array_key_exists($ex[1], $skins[$ex[0]]) && isset($_POST['wear']) && $_POST['wear'] >= 0.00 && $_POST['wear'] <= 1.00 && isset($_POST['seed'])) {
				$wear = floatval($_POST['wear']);
				$seed = intval($_POST['seed']);
				
				// If this is a knife skin, clear basic knife selection AND other knife skins
				if (in_array($ex[0], [500, 503, 505, 506, 507, 508, 509, 512, 514, 515, 516, 517, 518, 519, 520, 521, 522, 523, 525, 526])) {
					// Clear basic knife selection
					$db->query("DELETE FROM `wp_player_knife` WHERE `steamid` = :steamid", ["steamid" => $steamid]);
					
					// Clear ALL other knife skins (to handle switching between knife types)
					$db->query("DELETE FROM `wp_player_skins` WHERE `steamid` = :steamid AND `weapon_defindex` IN (500, 503, 505, 506, 507, 508, 509, 512, 514, 515, 516, 517, 518, 519, 520, 521, 522, 523, 525, 526) AND `weapon_defindex` != :current_defindex", ["steamid" => $steamid, "current_defindex" => $ex[0]]);
				}
				
				if (array_key_exists($ex[0], $selectedSkins)) {
					$db->query("UPDATE wp_player_skins SET weapon_paint_id = :weapon_paint_id, weapon_wear = :weapon_wear, weapon_seed = :weapon_seed WHERE steamid = :steamid AND weapon_defindex = :weapon_defindex", ["steamid" => $steamid, "weapon_defindex" => $ex[0], "weapon_paint_id" => $ex[1], "weapon_wear" => $wear, "weapon_seed" => $seed]);
				} else {
					$db->query("INSERT INTO wp_player_skins (`steamid`, `weapon_defindex`, `weapon_paint_id`, `weapon_wear`, `weapon_seed`, `weapon_team`) VALUES (:steamid, :weapon_defindex, :weapon_paint_id, :weapon_wear, :weapon_seed, 2)", ["steamid" => $steamid, "weapon_defindex" => $ex[0], "weapon_paint_id" => $ex[1], "weapon_wear" => $wear, "weapon_seed" => $seed]);
					$db->query("INSERT INTO wp_player_skins (`steamid`, `weapon_defindex`, `weapon_paint_id`, `weapon_wear`, `weapon_seed`, `weapon_team`) VALUES (:steamid, :weapon_defindex, :weapon_paint_id, :weapon_wear, :weapon_seed, 3)", ["steamid" => $steamid, "weapon_defindex" => $ex[0], "weapon_paint_id" => $ex[1], "weapon_wear" => $wear, "weapon_seed" => $seed]);
				}
			}
		}
		header("Location: {$_SERVER['PHP_SELF']}");
	}

	// Organize weapons by categories
	$weaponCategories = [
		'Knives' => [],
		'Gloves' => [],
		'Rifles' => [7, 8, 10, 13, 16, 60, 39, 40, 38],
		'Pistols' => [1, 2, 3, 4, 30, 32, 36, 61, 63, 64],
		'SMGs' => [17, 19, 24, 26, 33, 34],
		'Shotguns' => [25, 27, 29, 35],
		'Snipers' => [9, 11, 38],
		'Machine Guns' => [14, 28],
		'Grenades' => [43, 44, 45, 46, 47, 48]
	];

	// Add knives to categories
	foreach ($knifes as $knifeKey => $knife) {
		if ($knifeKey != 0) {
			$weaponCategories['Knives'][$knifeKey] = $knife;
		}
	}
}
?>

<!DOCTYPE html>
<html lang="en" data-theme="dark">

<head>
	<meta charset="utf-8">
	<meta name="viewport" content="width=device-width, initial-scale=1">
	<link rel="stylesheet" href="style.css">
	<title>CS2 Weapon Paints</title>
</head>

<body>

	<?php if (!isset($_SESSION['steamid'])): ?>
		<div class="login-container">
			<div class="login-card">
				<h1>CS2 Weapon Paints</h1>
				<p>Connect your Steam account to customize your weapon loadout</p>
				<?php loginbutton("rectangle"); ?>
			</div>
		</div>
	<?php else: ?>
		<div class="app-container">
			<!-- Header -->
			<header class="app-header">
				<div class="header-left">
					<h1>CS2 Weapon Paints</h1>
				</div>
				<div class="header-right">
					<span class="user-info">Welcome, <?php echo $_SESSION['steam_personaname'] ?? 'Player'; ?></span>
					<a href="<?php echo $_SERVER['PHP_SELF']; ?>?logout" class="logout-btn">Logout</a>
				</div>
			</header>

			<div class="app-main">
				<!-- Sidebar -->
				<aside class="sidebar">
					<div class="sidebar-header">
						<h3>Weapons</h3>
						<div class="search-container">
							<input type="text" id="weaponSearch" placeholder="Search weapons..." onkeyup="searchWeapons(this.value)">
						</div>
					</div>
					<nav class="sidebar-nav">
						<?php foreach ($weaponCategories as $categoryName => $categoryWeapons): ?>
							<?php 
								// Count weapons in this category
								$weaponCount = 0;
								if ($categoryName == 'Knives') {
									$weaponCount = count($knifes) - 1; // Exclude default knife
								} else {
									foreach ($categoryWeapons as $weaponDefindex) {
										if (isset($weapons[$weaponDefindex])) {
											$weaponCount++;
										}
									}
								}
							?>
							<div class="nav-category">
								<div class="nav-item category-header" data-category="<?php echo strtolower($categoryName); ?>" onclick="toggleCategory('<?php echo strtolower($categoryName); ?>')">
									<span class="nav-icon">
										<?php echo $categoryName == 'Knives' ? '🗡️' : ($categoryName == 'Gloves' ? '🧤' : ($categoryName == 'Rifles' ? '🔫' : ($categoryName == 'Pistols' ? '🔫' : ($categoryName == 'SMGs' ? '🔫' : ($categoryName == 'Shotguns' ? '🔫' : ($categoryName == 'Snipers' ? '🎯' : ($categoryName == 'Machine Guns' ? '⚡' : '💣'))))))); ?>
									</span>
									<div class="nav-content">
										<span class="nav-text"><?php echo $categoryName; ?></span>
										<span class="nav-count"><?php echo $weaponCount; ?></span>
									</div>
									<span class="nav-arrow">▶</span>
								</div>
								
								<div class="weapon-list" data-category="<?php echo strtolower($categoryName); ?>">
									<?php if ($categoryName == 'Knives'): ?>
										<?php foreach ($knifes as $knifeKey => $knife): ?>
											<?php if ($knifeKey != 0): ?>
												<div class="weapon-container">
													<div class="weapon-item" onclick="toggleKnifeSkins(<?php echo $knifeKey; ?>)">
														<img src="<?php echo $knife['image_url']; ?>" alt="<?php echo $knife['paint_name']; ?>" class="weapon-icon">
														<span class="weapon-name"><?php echo $knife['paint_name']; ?></span>
														<span class="weapon-arrow">▶</span>
													</div>
													<div class="weapon-skins-grid" data-weapon="knife-<?php echo $knifeKey; ?>">
														<!-- Knife skins will be populated by JavaScript -->
													</div>
												</div>
											<?php endif; ?>
										<?php endforeach; ?>
									<?php else: ?>
										<?php foreach ($categoryWeapons as $weaponDefindex): ?>
											<?php if (isset($weapons[$weaponDefindex])): ?>
												<div class="weapon-container">
													<div class="weapon-item" onclick="toggleWeaponSkins(<?php echo $weaponDefindex; ?>)">
														<img src="<?php echo $weapons[$weaponDefindex]['image_url']; ?>" alt="<?php echo $weapons[$weaponDefindex]['paint_name']; ?>" class="weapon-icon">
														<span class="weapon-name"><?php echo ucfirst(strtolower(str_replace('weapon_', '', $weapons[$weaponDefindex]['weapon_name']))); ?></span>
														<span class="weapon-arrow">▶</span>
													</div>
													<div class="weapon-skins-grid" data-weapon="<?php echo $weaponDefindex; ?>">
														<!-- Skins will be populated by JavaScript -->
													</div>
												</div>
											<?php endif; ?>
										<?php endforeach; ?>
									<?php endif; ?>
								</div>
							</div>
						<?php endforeach; ?>
					</nav>
				</aside>

				<!-- Main Content -->
				<main class="main-content">
					<div class="loadout-header">
						<h2>Current Loadout</h2>
						<p>Hover over items to customize</p>
					</div>

					<div class="loadout-grid">
						<!-- Knife - Show the currently equipped knife (either basic knife or knife skin) -->
						<?php
						$displayKnife = null;
						$displayKnifeSkin = null;
						$knifeSource = '';
						
						// Debug: Show what's in selectedKnife
						// echo "<!-- Debug: selectedKnife = " . print_r($selectedKnife, true) . " -->";
						
						// Check if there's a knife skin equipped (from selectedSkins for knife defindexes)
						foreach ($selectedSkins as $defindex => $selectedSkin) {
							if (in_array($defindex, [500, 503, 505, 506, 507, 508, 509, 512, 514, 515, 516, 517, 518, 519, 520, 521, 522, 523, 525, 526])) {
								if (isset($skins[$defindex][$selectedSkin['weapon_paint_id']])) {
									$displayKnifeSkin = $skins[$defindex][$selectedSkin['weapon_paint_id']];
									$knifeSource = 'skin';
									break; // Use the first knife skin found
								}
							}
						}
						
						// If no knife skin, check for basic knife selection
						if (!$displayKnifeSkin && $selectedKnife != null && !empty($selectedKnife)) {
							foreach ($knifes as $knifeKey => $knife) {
								if ($selectedKnife[0]['knife'] == $knife['weapon_name']) {
									$displayKnife = $knife;
									$knifeSource = 'basic';
									break;
								}
							}
						}
						
						// If no knife selected at all, show default knife
						if (!$displayKnifeSkin && !$displayKnife && isset($knifes[0])) {
							$displayKnife = $knifes[0]; // Default knife
							$knifeSource = 'default';
						}
						?>
						
						<!-- Always show a knife (either selected or default) -->
						<?php if ($displayKnifeSkin || $displayKnife): ?>
							<div class="loadout-item" data-weapon-type="knife">
								<div class="item-image-container">
									<?php if ($knifeSource == 'skin'): ?>
										<img src="<?php echo $displayKnifeSkin['image_url']; ?>" alt="<?php echo $displayKnifeSkin['paint_name']; ?>" class="item-image">
									<?php else: ?>
										<img src="<?php echo $displayKnife['image_url']; ?>" alt="<?php echo $displayKnife['paint_name']; ?>" class="item-image">
									<?php endif; ?>
									<div class="item-overlay">
										<button class="customize-btn" onclick="openCustomizeModal('knife', 0)">Customize</button>
									</div>
								</div>
								<div class="item-info">
									<div class="item-category">Knife</div>
									<div class="item-name">
										<?php if ($knifeSource == 'skin'): ?>
											<?php echo $displayKnifeSkin['paint_name']; ?>
										<?php else: ?>
											<?php echo $displayKnife['paint_name']; ?>
										<?php endif; ?>
									</div>
								</div>
							</div>
						<?php endif; ?>

						<!-- Show all weapons (exclude knives) - either with custom skin or default -->
						<?php foreach ($weapons as $defindex => $weapon): ?>
							<?php 
							// Skip ALL knives - comprehensive check
							$isKnifeWeapon = in_array($defindex, [500, 503, 505, 506, 507, 508, 509, 512, 514, 515, 516, 517, 518, 519, 520, 521, 522, 523, 525, 526]);
							$isDefaultKnife = ($weapon['weapon_name'] == 'weapon_knife');
							$isKnifeVariant = (strpos($weapon['weapon_name'], 'knife') !== false);
							$isKnifePaint = (strpos($weapon['paint_name'], '★') !== false); // Knife skins have ★ in name
							
							// Skip ANY knife-related weapon
							if ($isKnifeWeapon || $isDefaultKnife || $isKnifeVariant || $isKnifePaint) {
								continue;
							}
							?>
								<?php 
								$hasCustomSkin = array_key_exists($defindex, $selectedSkins);
								$isEquipped = $hasCustomSkin ? 'true' : 'false';
								?>
								<div class="loadout-item" data-weapon-id="<?php echo $defindex; ?>" data-equipped="<?php echo $isEquipped; ?>">
									<div class="item-image-container">
										<?php if ($hasCustomSkin): ?>
											<!-- Show custom skin -->
											<img src="<?php echo $skins[$defindex][$selectedSkins[$defindex]['weapon_paint_id']]['image_url']; ?>" 
												 alt="<?php echo $skins[$defindex][$selectedSkins[$defindex]['weapon_paint_id']]['paint_name']; ?>" 
												 class="item-image">
											<div class="item-overlay">
												<button class="customize-btn" onclick="openCustomizeModal('weapon', <?php echo $defindex; ?>)">Customize</button>
											</div>
										<?php else: ?>
											<!-- Show default weapon -->
											<img src="<?php echo $weapon['image_url']; ?>" 
												 alt="<?php echo $weapon['paint_name']; ?>" 
												 class="item-image">
											<!-- No overlay button for default weapons -->
										<?php endif; ?>
									</div>
									<div class="item-info">
										<div class="item-category"><?php echo ucfirst(strtolower(str_replace('weapon_', '', $weapon['weapon_name']))); ?></div>
										<div class="item-name">
											<?php if ($hasCustomSkin): ?>
												<?php echo $skins[$defindex][$selectedSkins[$defindex]['weapon_paint_id']]['paint_name']; ?>
											<?php else: ?>
												<?php echo $weapon['paint_name']; ?>
											<?php endif; ?>
										</div>
									</div>
								</div>
						<?php endforeach; ?>
					</div>
				</main>
			</div>
			<!-- Footer -->
			<footer class="app-footer">
				<div class="footer-content">
					<p>Created with ❤️ by <strong><a target="_blank" href="https://github.com/BramSuurdje" rel="noopener noreferrer">Bram</a></strong></p>
				</div>
			</footer>
		</div>

		<!-- Customize Modal -->
		<div id="customizeModal" class="modal hidden">
			<div class="modal-content">
				<div class="modal-header">
					<h3 id="modalTitle">Customize Weapon</h3>
					<button class="close-btn" onclick="closeCustomizeModal()">&times;</button>
				</div>
				<div class="modal-body">
					<form id="customizeForm" method="POST">
						<input type="hidden" id="customizeWeaponId" name="forma" value="">
						
						<div class="customize-grid">
							<div class="customize-section">
								<label for="wearSelect">Wear Condition</label>
								<select id="wearSelect" name="wearSelect" onchange="updateWearValue(this.value)">
									<option value="0.00">Factory New</option>
									<option value="0.07">Minimal Wear</option>
									<option value="0.15">Field-Tested</option>
									<option value="0.38">Well-Worn</option>
									<option value="0.45">Battle-Scarred</option>
								</select>
							</div>

							<div class="customize-section">
								<label for="wearInput">Float Value</label>
								<input type="number" id="wearInput" name="wear" min="0" max="1" step="0.001" value="0.00">
							</div>

							<div class="customize-section">
								<label for="seedInput">Pattern Seed</label>
								<input type="number" id="seedInput" name="seed" min="0" max="1000" value="0">
							</div>
						</div>

						<div class="modal-footer">
							<button type="button" class="btn btn-secondary" onclick="closeCustomizeModal()">Cancel</button>
							<button type="submit" class="btn btn-primary">Apply Changes</button>
						</div>
					</form>
				</div>
			</div>
		</div>

		<script>
			// Store weapon and skin data for JavaScript
			const weaponsData = <?php echo json_encode($weapons); ?>;
			const skinsData = <?php echo json_encode($skins); ?>;
			const selectedSkinsData = <?php echo json_encode($selectedSkins); ?>;
			const knivesData = <?php echo json_encode($knifes); ?>;
			const weaponCategories = <?php echo json_encode($weaponCategories); ?>;



			function toggleCategory(category) {
				const categoryHeader = document.querySelector(`.category-header[data-category="${category}"]`);
				const weaponList = document.querySelector(`.weapon-list[data-category="${category}"]`);
				const arrow = categoryHeader.querySelector('.nav-arrow');
				
				// Toggle the weapon list
				if (weaponList.classList.contains('expanded')) {
					// Collapse
					weaponList.classList.remove('expanded');
					arrow.textContent = '▶';
					categoryHeader.classList.remove('active');
				} else {
					// Collapse all other categories first
					document.querySelectorAll('.weapon-list').forEach(list => {
						list.classList.remove('expanded');
					});
					document.querySelectorAll('.category-header').forEach(header => {
						header.classList.remove('active');
						header.querySelector('.nav-arrow').textContent = '▶';
					});
					
					// Expand this category
					weaponList.classList.add('expanded');
					arrow.textContent = '▼';
					categoryHeader.classList.add('active');
				}
			}



			function equipKnife(knifeId) {
				// Create form and submit
				const form = document.createElement('form');
				form.method = 'POST';
				form.style.display = 'none';

				const formaInput = document.createElement('input');
				formaInput.name = 'forma';
				formaInput.value = `knife-${knifeId}`;

				form.appendChild(formaInput);
				document.body.appendChild(form);
				form.submit();
			}

			function searchWeapons(query) {
				if (!query.trim()) {
					// Reset search - show all categories and hide all weapon lists
					document.querySelectorAll('.nav-category').forEach(category => {
						category.style.display = 'block';
					});
					document.querySelectorAll('.weapon-list').forEach(list => {
						list.classList.remove('expanded');
					});
					document.querySelectorAll('.category-header').forEach(header => {
						header.classList.remove('active');
						header.querySelector('.nav-arrow').textContent = '▶';
					});
					return;
				}

				const searchTerm = query.toLowerCase();

				// Search through categories and weapons
				document.querySelectorAll('.nav-category').forEach(category => {
					const categoryName = category.querySelector('.nav-text').textContent.toLowerCase();
					const weaponItems = category.querySelectorAll('.weapon-item');
					let categoryMatches = categoryName.includes(searchTerm);
					let hasMatchingWeapons = false;

					// Check if any weapons in this category match
					weaponItems.forEach(weaponItem => {
						const weaponName = weaponItem.querySelector('.weapon-name').textContent.toLowerCase();
						const matches = weaponName.includes(searchTerm);
						
						if (matches) {
							hasMatchingWeapons = true;
							weaponItem.style.display = 'flex';
						} else {
							weaponItem.style.display = 'none';
						}
					});

					// Show/hide category based on matches
					if (categoryMatches || hasMatchingWeapons) {
						category.style.display = 'block';
						if (hasMatchingWeapons) {
							// Expand the category to show matching weapons
							const weaponList = category.querySelector('.weapon-list');
							const header = category.querySelector('.category-header');
							weaponList.classList.add('expanded');
							header.classList.add('active');
							header.querySelector('.nav-arrow').textContent = '▼';
						}
					} else {
						category.style.display = 'none';
					}
				});
			}

			function toggleWeaponSkins(weaponId) {
				const weaponItem = event.target.closest('.weapon-item');
				const skinGrid = weaponItem.parentNode.querySelector('.weapon-skins-grid');

				if (!skinsData[weaponId]) return;

				// Toggle the weapon item and skin grid
				if (skinGrid.classList.contains('expanded')) {
					// Collapse
					skinGrid.classList.remove('expanded');
					weaponItem.classList.remove('expanded');
				} else {
					// Collapse all other weapon skin grids first
					document.querySelectorAll('.weapon-skins-grid').forEach(grid => {
						grid.classList.remove('expanded');
					});
					document.querySelectorAll('.weapon-item').forEach(item => {
						item.classList.remove('expanded');
					});
					
					// Expand this weapon's skin grid
					populateWeaponSkins(weaponId, skinGrid);
					skinGrid.classList.add('expanded');
					weaponItem.classList.add('expanded');
				}
			}

			function populateWeaponSkins(weaponId, skinGrid) {
				// Create skins container
				const skinsContainer = document.createElement('div');
				skinsContainer.className = 'skins-container';
				
				// Clear previous content
				skinGrid.innerHTML = '';
				
				// Populate skins in 3-column grid
				Object.entries(skinsData[weaponId]).forEach(([paintId, skin]) => {
					const skinOption = document.createElement('div');
					skinOption.className = 'skin-option';
					
					// Check if this skin is currently equipped
					if (selectedSkinsData[weaponId] && selectedSkinsData[weaponId].weapon_paint_id == paintId) {
						skinOption.classList.add('active');
					}
					
					skinOption.onclick = () => equipSkin(weaponId, paintId);
					
					skinOption.innerHTML = `
						<img src="${skin.image_url}" alt="${skin.paint_name}">
						<div class="skin-option-name">${skin.paint_name.replace(/.*\| /, '')}</div>
					`;
					
					skinsContainer.appendChild(skinOption);
				});
				
				skinGrid.appendChild(skinsContainer);
			}

			function toggleKnifeSkins(knifeType) {
				const weaponItem = event.target.closest('.weapon-item');
				const skinGrid = weaponItem.parentNode.querySelector('.weapon-skins-grid');

				// Toggle the weapon item and skin grid
				if (skinGrid.classList.contains('expanded')) {
					// Collapse
					skinGrid.classList.remove('expanded');
					weaponItem.classList.remove('expanded');
				} else {
					// Collapse all other weapon skin grids first
					document.querySelectorAll('.weapon-skins-grid').forEach(grid => {
						grid.classList.remove('expanded');
					});
					document.querySelectorAll('.weapon-item').forEach(item => {
						item.classList.remove('expanded');
					});
					
					// Expand this knife type's skin grid
					populateKnifeTypeSkins(knifeType, skinGrid);
					skinGrid.classList.add('expanded');
					weaponItem.classList.add('expanded');
				}
			}

			function populateKnifeTypeSkins(knifeType, skinGrid) {
				// Create skins container
				const skinsContainer = document.createElement('div');
				skinsContainer.className = 'skins-container';
				
				// Clear previous content
				skinGrid.innerHTML = '';
				
				// ALWAYS show the basic knife option first
				const knife = knivesData[knifeType];
				if (knife) {
					const basicKnifeOption = document.createElement('div');
					basicKnifeOption.className = 'skin-option';
					
					// Check if basic knife is currently selected (no knife skins equipped for this type)
					if (!selectedSkinsData[knifeType]) {
						basicKnifeOption.classList.add('active');
					}
					
					basicKnifeOption.onclick = () => equipKnife(knifeType);
					
					basicKnifeOption.innerHTML = `
						<img src="${knife.image_url}" alt="${knife.paint_name}">
						<div class="skin-option-name">Default</div>
					`;
					
					skinsContainer.appendChild(basicKnifeOption);
				}
				
				// Then show knife skins if available
				if (skinsData[knifeType]) {
					// This knife type has multiple skins
					Object.entries(skinsData[knifeType]).forEach(([paintId, skin]) => {
						// Skip the default skin (paint ID 0) since we already show it as "Default" option above
						if (paintId == '0') {
							return;
						}
						
						const skinOption = document.createElement('div');
						skinOption.className = 'skin-option';
						
						// Check if this skin is currently equipped
						if (selectedSkinsData[knifeType] && selectedSkinsData[knifeType].weapon_paint_id == paintId) {
							skinOption.classList.add('active');
						}
						
						skinOption.onclick = () => equipSkin(knifeType, paintId);
						
						skinOption.innerHTML = `
							<img src="${skin.image_url}" alt="${skin.paint_name}">
							<div class="skin-option-name">${skin.paint_name.replace(/.*\| /, '')}</div>
						`;
						
						skinsContainer.appendChild(skinOption);
					});
				}
				
				skinGrid.appendChild(skinsContainer);
			}

			function equipSkin(weaponId, paintId) {
				// Create form and submit
				const form = document.createElement('form');
				form.method = 'POST';
				form.style.display = 'none';

				const formaInput = document.createElement('input');
				formaInput.name = 'forma';
				formaInput.value = `${weaponId}-${paintId}`;

				const wearInput = document.createElement('input');
				wearInput.name = 'wear';
				wearInput.value = '0.00';

				const seedInput = document.createElement('input');
				seedInput.name = 'seed';
				seedInput.value = '0';

				form.appendChild(formaInput);
				form.appendChild(wearInput);
				form.appendChild(seedInput);
				document.body.appendChild(form);
				form.submit();
			}

			function openCustomizeModal(type, weaponId) {
				const modal = document.getElementById('customizeModal');
				const title = document.getElementById('modalTitle');
				const form = document.getElementById('customizeForm');
				const weaponIdInput = document.getElementById('customizeWeaponId');
				const wearSelect = document.getElementById('wearSelect');
				const wearInput = document.getElementById('wearInput');
				const seedInput = document.getElementById('seedInput');

				if (type === 'knife') {
					title.textContent = 'Customize Knife';
					weaponIdInput.value = 'knife-0';
				} else {
					title.textContent = `Customize ${weaponsData[weaponId].weapon_name.replace('weapon_', '').toUpperCase()}`;
					weaponIdInput.value = `${weaponId}-${selectedSkinsData[weaponId]?.weapon_paint_id || 0}`;
					
					// Set current values
					if (selectedSkinsData[weaponId]) {
						wearInput.value = selectedSkinsData[weaponId].weapon_wear;
						seedInput.value = selectedSkinsData[weaponId].weapon_seed;
						
						// Set wear select based on value
						const wear = parseFloat(selectedSkinsData[weaponId].weapon_wear);
						if (wear <= 0.00) wearSelect.value = "0.00";
						else if (wear <= 0.07) wearSelect.value = "0.07";
						else if (wear <= 0.15) wearSelect.value = "0.15";
						else if (wear <= 0.38) wearSelect.value = "0.38";
						else wearSelect.value = "0.45";
					}
				}

				modal.classList.remove('hidden');
			}

			function closeCustomizeModal() {
				document.getElementById('customizeModal').classList.add('hidden');
			}

			function updateWearValue(selectedValue) {
				document.getElementById('wearInput').value = selectedValue;
			}

			// Close modals when clicking outside
			document.addEventListener('click', function(e) {
				const modal = document.getElementById('customizeModal');
				
				if (e.target === modal) {
					closeCustomizeModal();
				}
			});
		</script>
	<?php endif; ?>

</body>
</html>
