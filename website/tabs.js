				// tabs
				function showTab(tabName) {
				    var tabs = document.getElementsByClassName('card');
				    for (var i = 0; i < tabs.length; i++) {
				        if (tabs[i].classList.contains(tabName)) {
				            tabs[i].style.display = 'block';
				        } else {
				            tabs[i].style.display = 'none';
				        }
				    }
				}

				function showCategory(category) {
				    var skins = document.getElementsByClassName('skinlist');
				    var tablinks = document.getElementsByClassName('tablinks');
				    for (var i = 0; i < skins.length; i++) {
				        var defindex = skins[i].getAttribute('data-defindex');
				        if (category === 'all') {
				            skins[i].style.display = 'block';
				        } else {
				            var skinCategory = getCategoryByDefindex(defindex);
				            if (skinCategory === category) {
				                skins[i].style.display = 'block';
				            } else {
				                skins[i].style.display = 'none';
				            }
				        }
				    }

				    if (category === 'knifes' || category === 'all') {
				        var defaultKnifeCard = document.querySelector('.knifelist');
				        if (defaultKnifeCard) {
				            defaultKnifeCard.style.display = 'block';
				        }
				    } else {
				        var defaultKnifeCard = document.querySelector('.knifelist');
				        if (defaultKnifeCard) {
				            defaultKnifeCard.style.display = 'none';
				        }
				    }

				    for (var j = 0; j < tablinks.length; j++) {
				        tablinks[j].classList.remove('active');
				    }

				    var clickedTab = Array.from(tablinks).find(tab => {
				        var tabText = tab.textContent.toUpperCase();
				        var categoryWords = category.toUpperCase().split(' ');
				        return categoryWords.every(word => tabText.includes(word));
				    });

				    if (clickedTab) {
				        clickedTab.classList.add('active');
				    }
				}

				function getCategoryByDefindex(defindex) {
				    if (defindex >= 500 && defindex <= 525) {
				        return 'tablist1';
				    } else if (defindex >= 1 && defindex <= 4 || defindex >= 30 && defindex <= 32 || defindex == 36 || defindex >= 61 && defindex <= 64) {
				        return 'tablist2';
				    } else if (defindex >= 7 && defindex <= 8 || defindex == 10 || defindex == 13 || defindex == 16 || defindex == 60 || defindex == 39) {
				        return 'tablist3';
				    } else if (defindex == 26 || defindex == 17 || defindex >= 33 && defindex <= 34 || defindex == 19 || defindex >= 23 && defindex <= 24) {
				        return 'tablist4';
				    } else if (defindex == 14 || defindex == 28) {
				        return 'tablist5';
				    } else if (defindex == 9 || defindex == 11 || defindex == 38 || defindex == 40) {
				        return 'tablist6';
				    } else if (defindex == 27 || defindex == 35 || defindex == 29 || defindex == 25) {
				        return 'tablist7';
				    }

				    return 'other';
				}
