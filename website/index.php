<?php
require_once 'class/config.php';
require_once 'class/database.php';
require_once 'steamauth/steamauth.php';
require_once 'class/utils.php';
require_once 'class/weapon_handler.php';

// Handle weapon updates
if (isset($_SESSION['steamid']) && isset($_POST['forma'])) {
	$weaponHandler = new WeaponHandler($_SESSION['steamid']);
	if ($weaponHandler->handleWeaponUpdate($_POST)) {
		header("Location: {$_SERVER['PHP_SELF']}");
		exit;
	}
}

// Get loadout data for logged in users
$loadoutData = null;
$weaponCategories = [];
if (isset($_SESSION['steamid'])) {
	require_once 'steamauth/userInfo.php';
	$weaponHandler = new WeaponHandler($_SESSION['steamid']);
	$loadoutData = $weaponHandler->getLoadoutData();
	$weaponCategories = $weaponHandler->getOrganizedWeapons();
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
					<span class="user-info">Welcome, <?php echo htmlspecialchars($_SESSION['steam_personaname'] ?? 'Player'); ?></span>
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
							<?php if (empty($categoryWeapons)) continue; ?>
							<div class="nav-category">
								<div class="nav-item category-header" data-category="<?php echo strtolower($categoryName); ?>" onclick="toggleCategory('<?php echo strtolower($categoryName); ?>')">
									<div class="nav-content">
										<span class="nav-text"><?php echo $categoryName; ?></span>
										<span class="nav-count"><?php echo count($categoryWeapons); ?></span>
									</div>
									<span class="nav-arrow">▶</span>
								</div>
								
								<div class="weapon-list" data-category="<?php echo strtolower($categoryName); ?>">
									<?php if ($categoryName === 'Knives'): ?>
										<?php foreach ($categoryWeapons as $knifeId => $knife): ?>
											<div class="weapon-container">
												<div class="weapon-item" onclick="toggleKnifeSkins(<?php echo $knifeId; ?>)">
													<img src="<?php echo htmlspecialchars($knife['image_url']); ?>" alt="<?php echo htmlspecialchars($knife['paint_name']); ?>" class="weapon-icon">
													<span class="weapon-name"><?php echo htmlspecialchars($knife['paint_name']); ?></span>
													<span class="weapon-arrow">▶</span>
												</div>
												<div class="weapon-skins-grid" data-weapon="knife-<?php echo $knifeId; ?>"></div>
											</div>
										<?php endforeach; ?>
									<?php else: ?>
										<?php foreach ($categoryWeapons as $weaponId => $weapon): ?>
											<div class="weapon-container">
												<div class="weapon-item" onclick="toggleWeaponSkins(<?php echo $weaponId; ?>)">
													<img src="<?php echo htmlspecialchars($weapon['image_url']); ?>" alt="<?php echo htmlspecialchars($weapon['paint_name']); ?>" class="weapon-icon">
													<span class="weapon-name"><?php echo htmlspecialchars(ucfirst(strtolower(str_replace('weapon_', '', $weapon['weapon_name'])))); ?></span>
													<span class="weapon-arrow">▶</span>
												</div>
												<div class="weapon-skins-grid" data-weapon="<?php echo $weaponId; ?>"></div>
											</div>
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
						<?php 
						$displayKnife = $loadoutData['displayKnife'];
						$knifeEquipped = ($displayKnife['source'] === 'skin') ? 'true' : 'false';
						?>
						
						<div class="loadout-item" data-weapon-type="knife" data-equipped="<?php echo $knifeEquipped; ?>">
							<div class="item-image-container">
								<img src="<?php echo htmlspecialchars($displayKnife['data']['image_url']); ?>" 
									 alt="<?php echo htmlspecialchars($displayKnife['data']['paint_name']); ?>" 
									 class="item-image">
								<div class="item-overlay">
									<button class="customize-btn" onclick="openCustomizeModal('knife', 0)">Customize</button>
								</div>
							</div>
							<div class="item-info">
								<div class="item-category">Knife</div>
								<div class="item-name"><?php echo htmlspecialchars($displayKnife['data']['paint_name']); ?></div>
							</div>
						</div>

						<?php foreach ($loadoutData['weapons'] as $defindex => $weapon): ?>
							<?php if (UtilsClass::isKnifeWeapon($weapon)) continue; ?>
							<?php 
							$hasCustomSkin = array_key_exists($defindex, $loadoutData['selectedSkins']);
							$isEquipped = $hasCustomSkin ? 'true' : 'false';
							?>
							<div class="loadout-item" data-weapon-id="<?php echo $defindex; ?>" data-equipped="<?php echo $isEquipped; ?>">
								<div class="item-image-container">
									<?php if ($hasCustomSkin): ?>
										<?php 
										$skins = UtilsClass::skinsFromJson();
										$selectedSkin = $skins[$defindex][$loadoutData['selectedSkins'][$defindex]['weapon_paint_id']];
										?>
										<img src="<?php echo htmlspecialchars($selectedSkin['image_url']); ?>" 
											 alt="<?php echo htmlspecialchars($selectedSkin['paint_name']); ?>" 
											 class="item-image">
										<div class="item-overlay">
											<button class="customize-btn" onclick="openCustomizeModal('weapon', <?php echo $defindex; ?>)">Customize</button>
										</div>
									<?php else: ?>
										<img src="<?php echo htmlspecialchars($weapon['image_url']); ?>" 
											 alt="<?php echo htmlspecialchars($weapon['paint_name']); ?>" 
											 class="item-image">
									<?php endif; ?>
								</div>
								<div class="item-info">
									<div class="item-category"><?php echo htmlspecialchars(ucfirst(strtolower(str_replace('weapon_', '', $weapon['weapon_name'])))); ?></div>
									<div class="item-name">
										<?php if ($hasCustomSkin): ?>
											<?php echo htmlspecialchars($selectedSkin['paint_name']); ?>
										<?php else: ?>
											<?php echo htmlspecialchars($weapon['paint_name']); ?>
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
			// Optimized JavaScript with embedded data (restored functionality)
			const weaponsData = <?php echo json_encode($loadoutData['weapons']); ?>;
			const skinsData = <?php echo json_encode(UtilsClass::skinsFromJson()); ?>;
			const selectedSkinsData = <?php echo json_encode($loadoutData['selectedSkins']); ?>;
			const knivesData = <?php echo json_encode($loadoutData['knifes']); ?>;

			const WeaponApp = {
				init() {
					this.bindEvents();
				},

				bindEvents() {
					document.addEventListener('click', (e) => {
						if (e.target.id === 'customizeModal') {
							this.closeCustomizeModal();
						}
					});
				},

				toggleCategory(category) {
					const categoryHeader = document.querySelector(`.category-header[data-category="${category}"]`);
					const weaponList = document.querySelector(`.weapon-list[data-category="${category}"]`);
					const arrow = categoryHeader.querySelector('.nav-arrow');
					
					if (weaponList.classList.contains('expanded')) {
						weaponList.classList.remove('expanded');
						arrow.textContent = '▶';
						categoryHeader.classList.remove('active');
					} else {
						// Collapse other categories
						document.querySelectorAll('.weapon-list').forEach(list => list.classList.remove('expanded'));
						document.querySelectorAll('.category-header').forEach(header => {
							header.classList.remove('active');
							header.querySelector('.nav-arrow').textContent = '▶';
						});
						
						weaponList.classList.add('expanded');
						arrow.textContent = '▼';
						categoryHeader.classList.add('active');
					}
				},

				searchWeapons(query) {
					const searchTerm = query.toLowerCase().trim();
					
					if (!searchTerm) {
						document.querySelectorAll('.nav-category').forEach(category => category.style.display = 'block');
						document.querySelectorAll('.weapon-list').forEach(list => list.classList.remove('expanded'));
						document.querySelectorAll('.category-header').forEach(header => {
							header.classList.remove('active');
							header.querySelector('.nav-arrow').textContent = '▶';
						});
						return;
					}

					document.querySelectorAll('.nav-category').forEach(category => {
						const categoryName = category.querySelector('.nav-text').textContent.toLowerCase();
						const weaponItems = category.querySelectorAll('.weapon-item');
						let hasMatches = categoryName.includes(searchTerm);

						weaponItems.forEach(item => {
							const weaponName = item.querySelector('.weapon-name').textContent.toLowerCase();
							const matches = weaponName.includes(searchTerm);
							item.style.display = matches ? 'flex' : 'none';
							if (matches) hasMatches = true;
						});

						category.style.display = hasMatches ? 'block' : 'none';
						if (hasMatches) {
							const weaponList = category.querySelector('.weapon-list');
							const header = category.querySelector('.category-header');
							weaponList.classList.add('expanded');
							header.classList.add('active');
							header.querySelector('.nav-arrow').textContent = '▼';
						}
					});
				},

				toggleWeaponSkins(weaponId) {
					const weaponItem = event.target.closest('.weapon-item');
					const skinGrid = weaponItem.parentNode.querySelector('.weapon-skins-grid');

					if (!skinsData[weaponId]) return;

					if (skinGrid.classList.contains('expanded')) {
						skinGrid.classList.remove('expanded');
						weaponItem.classList.remove('expanded');
						return;
					}

					// Collapse others
					document.querySelectorAll('.weapon-skins-grid').forEach(grid => grid.classList.remove('expanded'));
					document.querySelectorAll('.weapon-item').forEach(item => item.classList.remove('expanded'));

					this.populateWeaponSkins(weaponId, skinGrid);
					skinGrid.classList.add('expanded');
					weaponItem.classList.add('expanded');
				},

				toggleKnifeSkins(knifeType) {
					const weaponItem = event.target.closest('.weapon-item');
					const skinGrid = weaponItem.parentNode.querySelector('.weapon-skins-grid');

					if (skinGrid.classList.contains('expanded')) {
						skinGrid.classList.remove('expanded');
						weaponItem.classList.remove('expanded');
						return;
					}

					// Collapse others
					document.querySelectorAll('.weapon-skins-grid').forEach(grid => grid.classList.remove('expanded'));
					document.querySelectorAll('.weapon-item').forEach(item => item.classList.remove('expanded'));

					this.populateKnifeTypeSkins(knifeType, skinGrid);
					skinGrid.classList.add('expanded');
					weaponItem.classList.add('expanded');
				},

				populateWeaponSkins(weaponId, skinGrid) {
					const skinsContainer = document.createElement('div');
					skinsContainer.className = 'skins-container';
					
					skinGrid.innerHTML = '';
					
					Object.entries(skinsData[weaponId]).forEach(([paintId, skin]) => {
						const skinOption = document.createElement('div');
						skinOption.className = 'skin-option';
						
						if (selectedSkinsData[weaponId] && selectedSkinsData[weaponId].weapon_paint_id == paintId) {
							skinOption.classList.add('active');
						}
						
						skinOption.onclick = () => this.equipSkin(weaponId, paintId);
						
						skinOption.innerHTML = `
							<img src="${skin.image_url}" alt="${skin.paint_name}" loading="lazy">
							<div class="skin-option-name">${skin.paint_name.replace(/.*\| /, '')}</div>
						`;
						
						skinsContainer.appendChild(skinOption);
					});
					
					skinGrid.appendChild(skinsContainer);
				},

				populateKnifeTypeSkins(knifeType, skinGrid) {
					const skinsContainer = document.createElement('div');
					skinsContainer.className = 'skins-container';
					
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
						
						basicKnifeOption.onclick = () => this.equipKnife(knifeType);
						
						basicKnifeOption.innerHTML = `
							<img src="${knife.image_url}" alt="${knife.paint_name}" loading="lazy">
							<div class="skin-option-name">Default</div>
						`;
						
						skinsContainer.appendChild(basicKnifeOption);
					}
					
					// Then show knife skins if available
					if (skinsData[knifeType]) {
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
							
							skinOption.onclick = () => this.equipSkin(knifeType, paintId);
							
							skinOption.innerHTML = `
								<img src="${skin.image_url}" alt="${skin.paint_name}" loading="lazy">
								<div class="skin-option-name">${skin.paint_name.replace(/.*\| /, '')}</div>
							`;
							
							skinsContainer.appendChild(skinOption);
						});
					}
					
					skinGrid.appendChild(skinsContainer);
				},

				equipSkin(weaponId, paintId) {
					this.submitForm(`${weaponId}-${paintId}`, { wear: '0.00', seed: '0' });
				},

				equipKnife(knifeId) {
					this.submitForm(`knife-${knifeId}`);
				},

				submitForm(forma, additionalData = {}) {
					const form = document.createElement('form');
					form.method = 'POST';
					form.style.display = 'none';

					const formaInput = document.createElement('input');
					formaInput.name = 'forma';
					formaInput.value = forma;
					form.appendChild(formaInput);

					Object.entries(additionalData).forEach(([name, value]) => {
						const input = document.createElement('input');
						input.name = name;
						input.value = value;
						form.appendChild(input);
					});

					document.body.appendChild(form);
					form.submit();
				},

				openCustomizeModal(type, weaponId) {
					const modal = document.getElementById('customizeModal');
					const title = document.getElementById('modalTitle');
					const weaponIdInput = document.getElementById('customizeWeaponId');
					const wearSelect = document.getElementById('wearSelect');
					const wearInput = document.getElementById('wearInput');
					const seedInput = document.getElementById('seedInput');

					if (type === 'knife') {
						title.textContent = 'Customize Knife';
						weaponIdInput.value = 'knife-0';
					} else {
						const weaponName = weaponsData[weaponId] ? weaponsData[weaponId].weapon_name.replace('weapon_', '').toUpperCase() : 'Weapon';
						title.textContent = `Customize ${weaponName}`;
						weaponIdInput.value = `${weaponId}-${selectedSkinsData[weaponId]?.weapon_paint_id || 0}`;
						
						if (selectedSkinsData[weaponId]) {
							const wear = parseFloat(selectedSkinsData[weaponId].weapon_wear);
							wearInput.value = selectedSkinsData[weaponId].weapon_wear;
							seedInput.value = selectedSkinsData[weaponId].weapon_seed;
							
							// Set wear select
							if (wear <= 0.00) wearSelect.value = "0.00";
							else if (wear <= 0.07) wearSelect.value = "0.07";
							else if (wear <= 0.15) wearSelect.value = "0.15";
							else if (wear <= 0.38) wearSelect.value = "0.38";
							else wearSelect.value = "0.45";
						}
					}

					modal.classList.remove('hidden');
				},

				closeCustomizeModal() {
					document.getElementById('customizeModal').classList.add('hidden');
				},

				updateWearValue(selectedValue) {
					document.getElementById('wearInput').value = selectedValue;
				}
			};

			// Global functions for onclick handlers
			const toggleCategory = (category) => WeaponApp.toggleCategory(category);
			const searchWeapons = (query) => WeaponApp.searchWeapons(query);
			const toggleWeaponSkins = (weaponId) => WeaponApp.toggleWeaponSkins(weaponId);
			const toggleKnifeSkins = (knifeType) => WeaponApp.toggleKnifeSkins(knifeType);
			const openCustomizeModal = (type, weaponId) => WeaponApp.openCustomizeModal(type, weaponId);
			const closeCustomizeModal = () => WeaponApp.closeCustomizeModal();
			const updateWearValue = (value) => WeaponApp.updateWearValue(value);

			// Initialize app
			WeaponApp.init();
		</script>
	<?php endif; ?>

</body>
</html>