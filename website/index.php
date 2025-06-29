<?php
require_once 'class/config.php';
require_once 'class/database.php';
require_once 'steamauth/steamauth.php';
require_once 'class/utils.php';

$db = new DataBase();
if (isset($_SESSION['steamid'])) {

	$steamid = $_SESSION['steamid'];

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
			$db->query("INSERT INTO `wp_player_knife` (`steamid`, `knife`, `weapon_team`) VALUES(:steamid, :knife, 2) ON DUPLICATE KEY UPDATE `knife` = :knife", ["steamid" => $steamid, "knife" => $knifes[$ex[1]]['weapon_name']]);
			$db->query("INSERT INTO `wp_player_knife` (`steamid`, `knife`, `weapon_team`) VALUES(:steamid, :knife, 3) ON DUPLICATE KEY UPDATE `knife` = :knife", ["steamid" => $steamid, "knife" => $knifes[$ex[1]]['weapon_name']]);
		} else {
			if (array_key_exists($ex[1], $skins[$ex[0]]) && isset($_POST['wear']) && $_POST['wear'] >= 0.00 && $_POST['wear'] <= 1.00 && isset($_POST['seed'])) {
				$wear = floatval($_POST['wear']);
				$seed = intval($_POST['seed']);
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
							<div class="nav-item" data-category="<?php echo strtolower($categoryName); ?>" onclick="toggleCategory('<?php echo strtolower($categoryName); ?>')">
								<span class="nav-icon">
									<?php echo $categoryName == 'Knives' ? '🗡️' : ($categoryName == 'Gloves' ? '🧤' : ($categoryName == 'Rifles' ? '🔫' : ($categoryName == 'Pistols' ? '🔫' : ($categoryName == 'SMGs' ? '🔫' : ($categoryName == 'Shotguns' ? '🔫' : ($categoryName == 'Snipers' ? '🎯' : ($categoryName == 'Machine Guns' ? '⚡' : '💣'))))))); ?>
								</span>
								<div class="nav-content">
									<span class="nav-text"><?php echo $categoryName; ?></span>
									<span class="nav-count"><?php echo $weaponCount; ?></span>
								</div>
								<span class="nav-arrow">▶</span>
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
						<!-- Knife -->
						<div class="loadout-item" data-weapon-type="knife">
							<?php
							$actualKnife = $knifes[0];
							if ($selectedKnife != null) {
								foreach ($knifes as $knife) {
									if ($selectedKnife[0]['knife'] == $knife['weapon_name']) {
										$actualKnife = $knife;
										break;
									}
								}
							}
							?>
							<div class="item-image-container">
								<img src="<?php echo $actualKnife['image_url']; ?>" alt="<?php echo $actualKnife['paint_name']; ?>" class="item-image">
								<div class="item-overlay">
									<button class="customize-btn" onclick="openCustomizeModal('knife', 0)">Customize</button>
								</div>
							</div>
							<div class="item-info">
								<div class="item-category">Knife</div>
								<div class="item-name"><?php echo $actualKnife['paint_name']; ?></div>
							</div>
						</div>

						<!-- Only show equipped weapons -->
						<?php foreach ($selectedSkins as $defindex => $selectedSkin): ?>
							<?php if (isset($weapons[$defindex])): ?>
								<div class="loadout-item" data-weapon-id="<?php echo $defindex; ?>" data-equipped="true">
									<div class="item-image-container">
										<img src="<?php echo $skins[$defindex][$selectedSkin['weapon_paint_id']]['image_url']; ?>" 
											 alt="<?php echo $skins[$defindex][$selectedSkin['weapon_paint_id']]['paint_name']; ?>" 
											 class="item-image">
										<div class="item-overlay">
											<button class="customize-btn" onclick="openCustomizeModal('weapon', <?php echo $defindex; ?>)">Customize</button>
										</div>
									</div>
									<div class="item-info">
										<div class="item-category"><?php echo ucfirst(strtolower(str_replace('weapon_', '', $weapons[$defindex]['weapon_name']))); ?></div>
										<div class="item-name"><?php echo $skins[$defindex][$selectedSkin['weapon_paint_id']]['paint_name']; ?></div>
									</div>
								</div>
							<?php endif; ?>
						<?php endforeach; ?>

						<!-- Show message if no weapons equipped -->
						<?php if (empty($selectedSkins)): ?>
							<div class="empty-loadout">
								<div class="empty-message">
									<h3>No weapons equipped</h3>
									<p>Browse weapons below to equip skins</p>
								</div>
							</div>
						<?php endif; ?>
					</div>

					<!-- Weapon Browser Section -->
					<div class="weapon-browser">
						<div class="browser-header">
							<h2>Weapon Browser</h2>
							<p>Click on any weapon to equip a skin</p>
						</div>
						<div class="browser-grid" id="weaponBrowser">
							<?php foreach ($weapons as $defindex => $weapon): ?>
								<div class="browser-item" data-weapon-id="<?php echo $defindex; ?>" onclick="showWeaponSkins(<?php echo $defindex; ?>)">
									<div class="browser-image-container">
										<img src="<?php echo $weapon['image_url']; ?>" alt="<?php echo $weapon['paint_name']; ?>" class="browser-image">
										<div class="browser-overlay">
											<span class="equip-text">Equip Skin</span>
										</div>
									</div>
									<div class="browser-info">
										<div class="browser-category"><?php echo ucfirst(strtolower(str_replace('weapon_', '', $weapon['weapon_name']))); ?></div>
										<div class="browser-name"><?php echo $weapon['paint_name']; ?></div>
									</div>
								</div>
							<?php endforeach; ?>
						</div>
					</div>
				</main>
			</div>
		</div>

		<!-- Skin Selection Overlay -->
		<div id="skinSelectionOverlay" class="overlay hidden">
			<div class="overlay-content">
				<div class="overlay-header">
					<h3 id="overlayTitle">Select Skin</h3>
					<button class="close-btn" onclick="closeSkinSelection()">&times;</button>
				</div>
				<div class="skin-grid" id="skinGrid">
					<!-- Skins will be populated here by JavaScript -->
				</div>
			</div>
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

			let currentFilter = 'all';

			function toggleCategory(category) {
				const navItem = document.querySelector(`[data-category="${category}"]`);
				const arrow = navItem.querySelector('.nav-arrow');
				
				// Check if already active
				if (navItem.classList.contains('active')) {
					// Deactivate and show all
					arrow.textContent = '▶';
					hideCategoryWeapons(category);
					showAllWeapons();
					currentFilter = 'all';
				} else {
					// Activate and filter
					arrow.textContent = '▼';
					showCategoryWeapons(category);
					filterWeaponsByCategory(category);
					currentFilter = category;
				}
			}

			function showCategoryWeapons(category) {
				// Clear all active states
				document.querySelectorAll('.nav-item').forEach(item => {
					item.classList.remove('active');
					item.querySelector('.nav-arrow').textContent = '▶';
				});
				
				// Set active state
				const navItem = document.querySelector(`[data-category="${category}"]`);
				navItem.classList.add('active');
				navItem.querySelector('.nav-arrow').textContent = '▼';
			}

			function hideCategoryWeapons(category) {
				document.querySelector(`[data-category="${category}"]`).classList.remove('active');
			}

			function filterWeaponsByCategory(category) {
				const loadoutItems = document.querySelectorAll('.loadout-item');
				const browserItems = document.querySelectorAll('.browser-item');
				
				// Filter loadout items
				loadoutItems.forEach(item => {
					const weaponId = item.dataset.weaponId;
					const weaponType = item.dataset.weaponType;
					let shouldShow = false;

					if (category === 'knives' && weaponType === 'knife') {
						shouldShow = true;
					} else if (category !== 'knives' && weaponId && weaponCategories[category.charAt(0).toUpperCase() + category.slice(1)]) {
						shouldShow = weaponCategories[category.charAt(0).toUpperCase() + category.slice(1)].includes(parseInt(weaponId));
					}

					if (shouldShow) {
						item.style.display = 'block';
						item.style.opacity = '1';
						item.style.transform = 'scale(1)';
					} else {
						item.style.opacity = '0.3';
						item.style.transform = 'scale(0.95)';
					}
				});

				// Filter browser items
				browserItems.forEach(item => {
					const weaponId = item.dataset.weaponId;
					let shouldShow = false;

					if (category !== 'knives' && weaponId && weaponCategories[category.charAt(0).toUpperCase() + category.slice(1)]) {
						shouldShow = weaponCategories[category.charAt(0).toUpperCase() + category.slice(1)].includes(parseInt(weaponId));
					}

					if (shouldShow) {
						item.style.display = 'block';
						item.style.opacity = '1';
						item.style.transform = 'scale(1)';
					} else {
						item.style.display = 'none';
					}
				});

				// Update headers
				const loadoutHeader = document.querySelector('.loadout-header h2');
				const browserHeader = document.querySelector('.browser-header h2');
				loadoutHeader.textContent = `${category.charAt(0).toUpperCase() + category.slice(1)} Loadout`;
				browserHeader.textContent = `${category.charAt(0).toUpperCase() + category.slice(1)} Browser`;
			}

			function showAllWeapons() {
				const loadoutItems = document.querySelectorAll('.loadout-item');
				const browserItems = document.querySelectorAll('.browser-item');
				
				loadoutItems.forEach(item => {
					item.style.display = 'block';
					item.style.opacity = '1';
					item.style.transform = 'scale(1)';
				});

				browserItems.forEach(item => {
					item.style.display = 'block';
					item.style.opacity = '1';
					item.style.transform = 'scale(1)';
				});

				// Reset headers
				const loadoutHeader = document.querySelector('.loadout-header h2');
				const browserHeader = document.querySelector('.browser-header h2');
				loadoutHeader.textContent = 'Current Loadout';
				browserHeader.textContent = 'Weapon Browser';
			}

			function searchWeapons(query) {
				if (!query.trim()) {
					// If search is empty, apply current filter or show all
					if (currentFilter === 'all') {
						showAllWeapons();
					} else {
						filterWeaponsByCategory(currentFilter);
					}
					return;
				}

				const loadoutItems = document.querySelectorAll('.loadout-item');
				const browserItems = document.querySelectorAll('.browser-item');
				const searchTerm = query.toLowerCase();

				// Search loadout items
				loadoutItems.forEach(item => {
					const weaponId = item.dataset.weaponId;
					const weaponType = item.dataset.weaponType;
					let weaponName = '';
					let skinName = '';

					if (weaponType === 'knife') {
						weaponName = 'knife';
						skinName = item.querySelector('.item-name').textContent.toLowerCase();
					} else if (weaponId && weaponsData[weaponId]) {
						weaponName = weaponsData[weaponId].weapon_name.replace('weapon_', '').toLowerCase();
						skinName = item.querySelector('.item-name').textContent.toLowerCase();
					}

					const matches = weaponName.includes(searchTerm) || skinName.includes(searchTerm);

					if (matches) {
						item.style.display = 'block';
						item.style.opacity = '1';
						item.style.transform = 'scale(1)';
					} else {
						item.style.opacity = '0.3';
						item.style.transform = 'scale(0.95)';
					}
				});

				// Search browser items
				browserItems.forEach(item => {
					const weaponId = item.dataset.weaponId;
					let weaponName = '';
					let skinName = '';

					if (weaponId && weaponsData[weaponId]) {
						weaponName = weaponsData[weaponId].weapon_name.replace('weapon_', '').toLowerCase();
						skinName = item.querySelector('.browser-name').textContent.toLowerCase();
					}

					const matches = weaponName.includes(searchTerm) || skinName.includes(searchTerm);

					if (matches) {
						item.style.display = 'block';
						item.style.opacity = '1';
						item.style.transform = 'scale(1)';
					} else {
						item.style.display = 'none';
					}
				});

				// Update headers
				const loadoutHeader = document.querySelector('.loadout-header h2');
				const browserHeader = document.querySelector('.browser-header h2');
				loadoutHeader.textContent = `Search Results: "${query}"`;
				browserHeader.textContent = `Search Results: "${query}"`;
			}

			function showWeaponSkins(weaponId) {
				const overlay = document.getElementById('skinSelectionOverlay');
				const title = document.getElementById('overlayTitle');
				const skinGrid = document.getElementById('skinGrid');

				if (!skinsData[weaponId]) return;

				title.textContent = `Select ${weaponsData[weaponId].weapon_name.replace('weapon_', '').toUpperCase()} Skin`;
				
				// Clear previous skins
				skinGrid.innerHTML = '';

				// Populate skins
				Object.entries(skinsData[weaponId]).forEach(([paintId, skin]) => {
					const skinItem = document.createElement('div');
					skinItem.className = 'skin-item';
					skinItem.onclick = () => equipSkin(weaponId, paintId);
					
					skinItem.innerHTML = `
						<img src="${skin.image_url}" alt="${skin.paint_name}" class="skin-image">
						<div class="skin-name">${skin.paint_name}</div>
					`;
					
					skinGrid.appendChild(skinItem);
				});

				overlay.classList.remove('hidden');
			}

			function closeSkinSelection() {
				document.getElementById('skinSelectionOverlay').classList.add('hidden');
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

			// Close overlays when clicking outside
			document.addEventListener('click', function(e) {
				const overlay = document.getElementById('skinSelectionOverlay');
				const modal = document.getElementById('customizeModal');
				
				if (e.target === overlay) {
					closeSkinSelection();
				}
				if (e.target === modal) {
					closeCustomizeModal();
				}
			});
		</script>
	<?php endif; ?>

</body>
</html>
