<div class="col-sm-2">
    <div class="card text-center mb-3 border border-primary">
        <div class="card-body">
            <?php
            // Determine the user's selected knife
            $actualGloves = $selectedGloves;
            /*if ($selectedGloves != null) {
                foreach ($gloves as $glove) {
                    if ($selectedGloves == $glove['weapon_defindex']) {
                        $actualGloves = $glove;
                        break;
                    }
                }
            }*/

            // Display user's selected knife information
            echo "<div class='card-header'>";
            echo "<h5 class='card-title item-name'>{$actualGloves["paint_name"]}</h5>";
            echo "</div>";
            echo "<img id='glove-image' src='{$actualGloves["image_url"]}'  class='skin-image'>";
            ?>
        </div>
        <div class="card-footer">
            <div class="form-group">
                <label for="glovesSelect">Select Gloves:</label>
                <select id="glovesSelect" class="form-control" onchange="updateGlovePaints(this.value)">
                    <option disabled selected>Select Gloves</option>
                    <?php
                    foreach ($gloves as $weapon_defindex => $glove) {
                        echo "<option value=\"{$weapon_defindex}\">{$weapon_defindex}</option>";
                    }
                    ?>
                </select>
            </div>
            <div class="form-group">
                <label for="paintSelect">Select Paint:</label>
                <select id="paintSelect" class="form-control" onchange="updateGloveImage(this)" >
                    <option disabled selected>Select Paint</option>
                </select>
            </div>

            <script>
                var gloves = <?php echo json_encode($gloves); ?>;

                function updateGlovePaints(weapon_defindex) {
                    var paintSelect = document.getElementById('paintSelect');
                    paintSelect.innerHTML = ""; // Clear the select options

                    for (var defindex in gloves) {
                        if (defindex == weapon_defindex) {
                            for (var paint in gloves[defindex]) {
                                var option = document.createElement('option');
                                option.value = paint;
                                option.text = gloves[defindex][paint].paint_name;
                                paintSelect.appendChild(option);
                                document.getElementById('glove-image').src = gloves[defindex][paint].image_url;
                            }
                        }
                    }
                }
                function updateGloveImage(select) {
                    // here it will update glove-image with the selected paint from updateGlovePaints
                    var weapon_defindex = document.getElementById('glovesSelect').value;
                    var paint = select.value;
                    document.getElementById('glove-image').src = gloves[weapon_defindex][paint].image_url;

                }
            </script>
        </div>
    </div>
</div>
