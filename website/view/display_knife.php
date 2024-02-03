<div class="col-sm-2">
    <div class="card text-center mb-3 border border-primary">
        <div class="card-body">
            <?php
            // Determine the user's selected knife
            $actualKnife = $knifes[0];
            if ($selectedKnife != null) {
                foreach ($knifes as $knife) {
                    if ($selectedKnife == $knife['weapon_name']) {
                        $actualKnife = $knife;
                        break;
                    }
                }
            }

            // Display user's selected knife information
            echo "<div class='card-header'>";
            echo "<h6 class='card-title item-name'>Knife type</h6>";
            echo "<h5 class='card-title item-name'>{$actualKnife["paint_name"]}</h5>";
            echo "</div>";
            echo "<img src='{$actualKnife["image_url"]}' class='skin-image'>";
            ?>
        </div>
        <div class="card-footer">
            <!-- Form for selecting user's knife -->
            <form action="" method="POST">
                <select name="forma" class="form-control select" onchange="this.form.submit()" class="SelectWeapon">
                    <option disabled>Select knife</option>
                    <?php
                    // Display options for selecting different knives
                    foreach ($knifes as $knifeKey => $knife) {
                        if ($selectedKnife == $knife['weapon_name'])
                            echo "<option selected value=\"knife-{$knifeKey}\">{$knife['paint_name']}</option>";
                        else
                            echo "<option value=\"knife-{$knifeKey}\">{$knife['paint_name']}</option>";
                    }
                    ?>
                </select>
            </form>
        </div>
    </div>
</div>
