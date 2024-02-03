<?php
// Display user's selected skins for different weapons
foreach ($weapons as $defindex => $default) {
?>
    <div class="col-sm-2">
        <div class="card text-center mb-3">
            <div class="card-body">
                <?php
                // Determine the skin to display for the current weapon
                if (array_key_exists($defindex, $selectedSkins)) {
                    echo "<div class='card-header'>";
                    echo "<h5 class='card-title item-name'>{$skins[$defindex][$selectedSkins[$defindex]['weapon_paint_id']]["paint_name"]}</h5>";
                    echo "</div>";
                    echo "<img src='{$skins[$defindex][$selectedSkins[$defindex]['weapon_paint_id']]['image_url']}' class='skin-image'>";
                } else {
                    echo "<div class='card-header'>";
                    echo "<h5 class='card-title item-name'>{$default["paint_name"]}</h5>";
                    echo "</div>";
                    echo "<img src='{$default["image_url"]}' class='skin-image'>";
                }
                ?>
            </div>
            <div class="card-footer">
                <!-- Form for selecting user's skin and settings -->
                <form action="" method="POST">
                    <select name="forma" class="form-control select" onchange="this.form.submit()" class="SelectWeapon">
                        <option disabled>Select skin</option>
                        <?php
                        // Display options for selecting different skins
                        foreach ($skins[$defindex] as $paintKey => $paint) {
                            if (array_key_exists($defindex, $selectedSkins) && $selectedSkins[$defindex]['weapon_paint_id'] == $paintKey)
                                echo "<option selected value=\"{$defindex}-{$paintKey}\">{$paint['paint_name']}</option>";
                            else
                                echo "<option value=\"{$defindex}-{$paintKey}\">{$paint['paint_name']}</option>";
                        }
                        ?>
                    </select>
                    <br></br>
                    <?php
                    // Display settings button for selected skin
                    $selectedSkinInfo = isset($selectedSkins[$defindex]) ? $selectedSkins[$defindex] : null;
                    $steamid = $_SESSION['steamid'];

                    if ($selectedSkinInfo) :
                    ?>
                        <button type="button" class="btn btn-primary" data-toggle="modal" data-target="#weaponModal<?php echo $defindex ?>">
                            Settings
                        </button>
                    <?php else : ?>
                        <!-- Display message if skin is not selected -->
                        <button type="button" class="btn btn-primary" onclick="showSkinSelectionAlert()">
                            Settings
                        </button>
                        <script>
                            function showSkinSelectionAlert() {
                                alert("You need to select a skin first.");
                            }
                        </script>
                    <?php endif; ?>
            </div>

            <?php
            // Display modal for adjusting wear and seed values
            $selectedSkinInfo = isset($selectedSkins[$defindex]['weapon_paint_id']) ? $selectedSkins[$defindex] : null;
            $queryWear = $selectedSkins[$defindex]['weapon_wear'] ?? 1.0;
            $initialWearValue = isset($selectedSkinInfo['weapon_wear']) ? $selectedSkinInfo['weapon_wear'] : (isset($queryWear[0]['weapon_wear']) ? $queryWear[0] : 0.0);
            $querySeed = $selectedSkins[$defindex]['weapon_seed'] ?? 0;
            $initialSeedValue = isset($selectedSkinInfo['weapon_seed']) ? $selectedSkinInfo['weapon_seed'] : 0;
            ?>

            <!-- Modal for adjusting wear and seed values -->
            <div class="modal fade" id="weaponModal<?php echo $defindex ?>" tabindex="-1" role="dialog" aria-labelledby="weaponModalLabel<?php echo $defindex ?>" aria-hidden="true">
                <div class="modal-dialog" role="document">
                    <div class="modal-content">
                        <!-- Modal header -->
                        <div class="modal-header">
                            <h5 class='card-title item-name'>
                                <?php
                                if (array_key_exists($defindex, $selectedSkins)) {
                                    echo "{$skins[$defindex][$selectedSkins[$defindex]['weapon_paint_id']]["paint_name"]} Settings";
                                } else {
                                    echo "{$default["paint_name"]} Settings";
                                }
                                ?>
                            </h5>
                        </div>
                        <!-- Modal body -->
                        <div class="modal-body">
                            <!-- Form for adjusting wear and seed values -->
                            <div class="form-group">
                                <select class="form-select" id="wearSelect<?php echo $defindex ?>" name="wearSelect" onchange="updateWearValue<?php echo $defindex ?>(this.value)">
                                    <option disabled>Select Wear</option>
                                    <option value="0.00" <?php echo ($initialWearValue == 0.00) ? 'selected' : ''; ?>>Factory New</option>
                                    <option value="0.07" <?php echo ($initialWearValue == 0.07) ? 'selected' : ''; ?>>Minimal Wear</option>
                                    <option value="0.15" <?php echo ($initialWearValue == 0.15) ? 'selected' : ''; ?>>Field-Tested</option>
                                    <option value="0.38" <?php echo ($initialWearValue == 0.38) ? 'selected' : ''; ?>>Well-Worn</option>
                                    <option value="0.45" <?php echo ($initialWearValue == 0.45) ? 'selected' : ''; ?>>Battle-Scarred</option>
                                </select>
                            </div>
                            <div class="row">
                                <div class="col-md-6">
                                    <div class="form-group">
                                        <label for="wear">Wear:</label>
                                        <input type="text" value="<?php echo $initialWearValue; ?>" class="form-control" id="wear<?php echo $defindex ?>" name="wear">
                                    </div>
                                </div>
                                <div class="col-md-6">
                                    <div class="form-group">
                                        <label for="seed">Seed:</label>
                                        <input type="text" value="<?php echo $initialSeedValue; ?>" class="form-control" id="seed<?php echo $defindex ?>" name="seed" oninput="validateSeed(this)">
                                    </div>
                                </div>
                            </div>
                        </div>
                        <!-- Modal footer -->
                        <div class="modal-footer">
                            <button type="button" class="btn btn-secondary" data-dismiss="modal">Close</button>
                            <button type="submit" class="btn btn-danger">Use</button>
                            </form>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    </div>
    <!-- JavaScript functions for updating wear and seed values -->
    <script>
        //  wear
        function updateWearValue<?php echo $defindex ?>(selectedValue) {
            var wearInputElement = document.getElementById("wear<?php echo $defindex ?>");
            wearInputElement.value = selectedValue;
        }

        function validateWear(inputElement) {
            inputElement.value = inputElement.value.replace(/[^0-9]/g, '');
        }
        // seed
        function validateSeed(input) {
            // Check entered value
            var inputValue = input.value.replace(/[^0-9]/g, ''); // Just get the numbers

            if (inputValue === "") {
                input.value = 0; // Set to 0 if empty or no numbers
            } else {
                var numericValue = parseInt(inputValue);
                numericValue = Math.min(1000, Math.max(1, numericValue)); // Interval control

                input.value = numericValue;
            }
        }
    </script>
<?php
}
?>
