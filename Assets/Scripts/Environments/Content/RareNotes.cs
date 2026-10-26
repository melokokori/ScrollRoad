using System.Collections.Generic;

namespace ScrollRoad
{
    // Defter notları (2026-10-25): her nadirin tek cümlelik açıklaması — yeni nadir duyurusunun alt satırı,
    // Mekânlar panelinde üzerine gelince ve defterlerde görünür. Anahtar "<ortam>:<nadir kimliği>" (kimlikler yalnız
    // ortam içinde benzersiz). Notu olmayan nadirde eski metin kalır.
    // loc:catalog — notlar (boşluklu metinler) Tools/loc/extract.py ile çeviri anahtarı olarak toplanır.
    public static class RareNotes
    {
        public static string For(EnvironmentId env, string rareId) =>
            rareId != null && Notes.TryGetValue(env + ":" + rareId, out var note) ? L.T(note) : null;

        static readonly Dictionary<string, string> Notes = Build();

        static Dictionary<string, string> Build()
        {
            var d = new Dictionary<string, string>();
            void Add(EnvironmentId env, params string[] pairs)
            {
                for (int i = 0; i + 1 < pairs.Length; i += 2) d[env + ":" + pairs[i]] = pairs[i + 1];
            }

            Add(EnvironmentId.Fishing,
                // The Lake
                "perch", "Striped and greedy. Where there's one, there's a shoal.",
                "chub", "Eats almost anything, trusts almost nothing.",
                "trout", "A flash of pink along the side, gone if you blink.",
                "bass", "Waits under the lily pads with its mouth open.",
                "zander", "Glassy eyes made for hunting in murky water at dusk.",
                "pike", "The lake's old landlord. Mind the teeth.",
                "golden_carp", "Nobody believes you caught it. Nobody ever will.",
                // The River
                "grayling", "Wears a sail on its back and smells faintly of thyme.",
                "brown_trout", "Red spots, cold water, a short temper.",
                "ide", "Turns golden in the right light and right river.",
                "asp", "Chases small fish across the surface in loud splashes.",
                "barbel", "Feels the gravel with four whiskers, pulls like a mule.",
                "huchen", "The Danube salmon. Some anglers wait a lifetime.",
                // The Coast
                "horse_mackerel", "Comes in by the hundred when the lights go on at the pier.",
                "garfish", "A needle with a green spine and a sense of drama.",
                "gilthead", "A gold bar between the eyes, like a little crown.",
                "bluefish", "Bites through lines, lures and the occasional finger.",
                "seabass", "Silver wolf of the breakers. Loves a rough sea.",
                "leerfish", "Hunts the surf line in long, fast runs.",
                // The Open Sea
                "atlantic_mackerel", "Tiger stripes, never stops swimming.",
                "bonito", "A small tuna in a big hurry.",
                "little_tunny", "Fingerprint marks on its back, a sprinter's build.",
                "mahi", "Every colour of the sea, until it leaves the water.",
                "amberjack", "Dives straight for the rocks the moment it's hooked.",
                "bluefin", "Warm-blooded, ocean-crossing, worth the whole season.",
                // The Ice Hole
                "yellow_perch", "Bright as a lemon against the snow.",
                "whitefish", "Silver and shy, it feeds right under the ice.",
                "arctic_char", "Lives further north than any other freshwater fish.",
                "burbot", "A cod that moved to the lake and spawns under the ice.",
                "lake_trout", "Old, deep and patient. Some are older than you.",
                "taimen", "The Siberian river wolf. It eats ducks.");

            Add(EnvironmentId.Door,
                // The Mall
                "shopper", "Twelve bags, two hands, zero regrets.",
                "mascot", "It's very warm inside. It waves anyway.",
                "mime", "Stuck in the revolving door. Or is it?",
                "santa", "Says he's early this year. Says that every year.",
                "knight", "Here for the medieval fair. Can't find the parking ticket.",
                "astronaut", "Just back from orbit, looking for a good pretzel.",
                "bigfoot", "One blurry visit. The security footage is useless.",
                // The Hotel
                "bellhop", "Knows every guest's secret and keeps every one.",
                "bride", "The wedding is upstairs. The shoes are killing her.",
                "rockstar", "Checked in under a fake name. Everyone knows.",
                "magician", "Asked for room 7. It was already his.",
                "filmstar", "Sunglasses indoors, autographs only on Tuesdays.",
                "king", "Brought his own pillow and a small orchestra.",
                // The Airport
                "pilot", "Has seen more sunrises than anyone in the building.",
                "attendant", "Can fold a blanket in one move.",
                "backpacker", "Three continents, one pair of socks.",
                "surfer", "Board bag bigger than the plane's luggage limit.",
                "mountaineer", "Smells faintly of snow and success.",
                "diver", "Took the helmet on board as hand luggage.",
                // The Museum
                "painter", "Sketches visitors while they look at paintings.",
                "guide", "Never lost a group. Almost never.",
                "archaeologist", "Brought a brush, just in case.",
                "ballerina", "Practising pliés in front of the statues.",
                "centurion", "Took a wrong turn at the Roman wing two thousand years ago.",
                "mummy", "Wandered out of the Egyptian hall for some fresh air.",
                // The Skyscraper
                "window_cleaner", "Has the best view in the city and a long rope.",
                "ceo", "Takes the revolving door at board-meeting speed.",
                "firefighter", "Fire drill on floor 54. Again.",
                "superhero", "Normally uses the window. Tried the door for once.",
                "robot", "Delivers coffee to the top floor without spilling a drop.",
                "alien", "Asked for the manager of Earth. Was sent to floor 88.");

            Add(EnvironmentId.Wind,
                // The Hills
                "bakery", "First lights on in the valley, every single morning.",
                "dairy", "Milking machines hum all through the night.",
                "school", "The bell rings on your power now.",
                "cinema", "One screen, sixty seats, endless popcorn.",
                "fairground", "The Ferris wheel lights up the whole hillside.",
                "observatory", "Needs the grid quiet and the sky dark.",
                // The Coast
                "fish_market", "Ice machines running before the boats are back.",
                "lighthouse", "The lamp turns with your blades.",
                "boathouse", "Winches, lamps and a radio playing old songs.",
                "seaside_pier", "A thousand bulbs along the boards.",
                "aquarium", "Every tank glows a slightly different blue.",
                "lifeboat_station", "Always on, always ready. The most important switch.",
                // The Desert
                "oasis_inn", "Cold drinks for every caravan that passes.",
                "caravanserai", "Lanterns for travellers, as it has been for centuries.",
                "date_farm", "The water pumps run on wind now.",
                "solar_farm", "Shares the grid with you at night.",
                "sand_fort", "Strings of lights along old battlements.",
                "mirage_palace", "Only appears on the map when it's powered.",
                // The Snowy Pass
                "ski_lodge", "Fireplace, fondue and a heated boot room.",
                "mountain_hut", "One bulb and one kettle, at the top of the world.",
                "cable_car", "Carries skiers up while your blades turn below.",
                "ice_rink", "The ice stays smooth because the motors never stop.",
                "research_station", "Measuring snow, wind and the odd yeti footprint.",
                "ice_hotel", "Everything is made of ice except the lights.",
                // The City
                "diner", "Open all night, neon sign included.",
                "train_station", "Departures board, clock and a thousand lamps.",
                "theatre", "Curtain up at eight. Don't let the wind drop.",
                "stadium", "Floodlights that can be seen from the next town.",
                "art_museum", "Climate control for paintings older than electricity.",
                "skyscraper", "Ninety floors lit one by one.");

            Add(EnvironmentId.Vinyl,
                // The Record Shop
                "cover_rainy", "Perfect for a grey afternoon and a hot drink.",
                "cover_praia", "Bossa nova recorded with the windows open.",
                "cover_nightdrive", "Synths for empty motorways at 2 a.m.",
                "cover_bluesmoke", "Late-night blues with a crackle on side B.",
                "cover_mirrorball", "Disco pressed on heavy vinyl, still glittering.",
                "cover_goldengroove", "Only 500 copies were pressed. You found one.",
                // The Jazz Club
                "cover_midnight_sax", "Recorded live, one take, lights down low.",
                "cover_smoke_rings", "A trio that never played the same song twice.",
                "cover_rain_city", "Brushes on the snare like rain on a window.",
                "cover_velvet", "The house band's only record. Signed on the sleeve.",
                "cover_blue_note", "That half hour after sunset, set to music.",
                "cover_gold_trumpet", "A solo so long the audience forgot to clap.",
                // The Disco
                "cover_glitter", "Comes with a free fistful of sequins.",
                "cover_roller", "Four wheels, eight bars, one long groove.",
                "cover_neon_heart", "A love song with a drum machine heartbeat.",
                "cover_dancefloor", "The floor lights up in time with the bass.",
                "cover_platform", "Taller shoes, higher notes.",
                "cover_starlight", "Twelve minutes long. Nobody sits down.",
                // The Beach Bar
                "cover_sunset_bay", "Steel drums and the sound of the tide.",
                "cover_hammock", "So slow the needle almost falls asleep.",
                "cover_surf_wave", "Reverb-soaked guitars and salty air.",
                "cover_coconut", "Recorded in a hut. You can hear the parrot.",
                "cover_tiki", "Exotica for long evenings by the fire.",
                "cover_pearl_shore", "Washed up in a crate on the sand. Still plays.",
                // The Concert Hall
                "cover_grand_piano", "Every note recorded from the front row.",
                "cover_strings", "A string quartet playing the sunrise.",
                "cover_opera", "Three acts, two sides, one standing ovation.",
                "cover_chandelier", "A waltz that makes the crystal ring.",
                "cover_orchestra", "Ninety players, one breath before the downbeat.",
                "cover_golden_baton", "The conductor's farewell concert.");

            Add(EnvironmentId.Kebab,
                // The Kebab House
                "iskender", "Doner on pide, tomato sauce, sizzling butter poured at the table.",
                "chicken_doner", "Lighter, juicier, a lunchtime favourite.",
                "pideli_doner", "Thin slices on soft pide, soaked in the juices.",
                "kasarli_durum", "Doner wrapped with melted kashar, pressed on the grill.",
                "cag_kebab", "Erzurum's horizontal spit, eaten straight off the skewer.",
                "sultans_platter", "Everything the house can carve, on one silver tray.",
                // The Greek Taverna
                "yogurt_gyros", "Thick tzatziki, warm pita, nothing else needed.",
                "double_pita", "For when one pita is clearly not enough.",
                "feta_gyros", "Crumbled feta on top, olive oil on everything.",
                "spicy_gyros", "Paprika, chilli and a cold drink on the side.",
                "lamb_gyros", "The old way: lamb on the spit, oregano in the air.",
                "gyros_tower", "Stacked so high it needs its own skewer.",
                // The Shawarma Stand
                "beef_shawarma", "Seven-spice beef, pickles and tahini.",
                "toum_special", "Garlic sauce so strong you'll taste it tomorrow.",
                "shawarma_fatteh", "Shawarma over crispy bread and warm yogurt.",
                "kaak_shawarma", "Tucked into a sesame ring, Beirut street style.",
                "lamb_shawarma", "Slow-roasted lamb, sliced thin and crisp.",
                "royal_shawarma", "Rice, salads, sauces and a mountain of shawarma.",
                // The Taqueria
                "costra", "A crust of grilled cheese folded around the pastor.",
                "pastor_burrito", "The whole taco in one tight wrap.",
                "huarache_pastor", "On a sandal-shaped masa base, beans underneath.",
                "pineapple_special", "Extra pineapple shaved straight off the trompo.",
                "tacos_arabes", "Puebla's flatbread tacos, where al pastor began.",
                "golden_trompo", "The taquero's best spit of the year.",
                // The Night Grill
                "gemuse_doner", "Berlin style: roasted vegetables, feta, three sauces.",
                "cheese_doner", "Melted cheese, extra crunch, at 3 a.m.",
                "mit_alles", "Everything, with everything. Mit alles.",
                "scharf_doner", "Scharf! The chilli flakes go on twice.",
                "midnight_teller", "Doner, chips and salad for the last train home.",
                "golden_doner", "The Night Grill's legend. Carved once in a blue moon.");

            Add(EnvironmentId.Pottery,
                // The Village Workshop
                "water_jug", "Keeps water cool through the hottest summer.",
                "tall_vase", "Thrown in one long pull, without a wobble.",
                "amphora", "Two handles, pointed base, made to travel by ship.",
                "teapot", "The spout pours clean. That's the hard part.",
                "tulip_vase", "Narrow waist, wide lip, built for a single bloom.",
                "golden_urn", "Gilded rim. The village keeps it for weddings.",
                // The Greek Studio
                "hero_vase", "Black figures fighting across a red sky.",
                "krater", "For mixing wine and water at very long dinners.",
                "hydria", "Three handles: two to lift, one to pour.",
                "owl_jug", "Athena's owl painted on, watching you drink.",
                "dolphin_plate", "Dolphins circling the rim as if it were the sea.",
                "olympic_amphora", "A prize for champions, filled with olive oil.",
                // The Japanese Kiln
                "raku_bowl", "Pulled red-hot from the kiln, cooled in the open air.",
                "tea_caddy", "Small, plain and perfectly balanced in the hand.",
                "ikebana_vase", "Shaped around the flowers it will one day hold.",
                "kintsugi_bowl", "Broken once, mended with gold, more precious now.",
                "dragon_jar", "A dragon coils around it from foot to rim.",
                "moon_jar", "Two halves joined at the middle, never quite round.",
                // The Moroccan Souk
                "blue_tagine", "Cobalt glaze, conical lid, slow-cooked dinner inside.",
                "zellige_plate", "Star patterns that never seem to end.",
                "lantern_pot", "Pierced clay that throws stars on the wall.",
                "brass_ewer", "A clay body with a hammered brass rim.",
                "peacock_vase", "Feathers painted in seven greens.",
                "sultans_jar", "Made for the palace. Took a whole season.",
                // The Modern Studio
                "speckled_vase", "Iron flecks in the glaze, like a quail's egg.",
                "ribbed_lamp", "Lines thrown in by fingertip, lit from inside.",
                "bubble_vase", "A row of soft bubbles. Hard to throw, easy to love.",
                "neon_jar", "A glaze so bright it looks plugged in.",
                "marbled_bowl", "Two clays twisted together, no two alike.",
                "platinum_vase", "Fired three times for a mirror finish.");

            Add(EnvironmentId.Peeler,
                // The Fruit Stall
                "mango", "One long ribbon around the stone, if you're careful.",
                "pineapple", "Spiral cut to take the eyes out in one go.",
                "dragon_fruit", "Pink outside, speckled white inside.",
                "cantaloupe", "The netted skin comes off in a single spiral.",
                "pomelo", "A peel as thick as your thumb, and twice as fragrant.",
                "watermelon", "The stall's pride. Needs both hands and a long knife.",
                // The Tropical Market
                "papaya", "Black seeds, orange flesh, a squeeze of lime.",
                "coconut", "Not peeled so much as persuaded.",
                "rambutan", "Hairy outside, sweet and white within.",
                "mangosteen", "The queen of fruits. Purple juice everywhere.",
                "jackfruit", "The biggest tree fruit in the world. Sticky work.",
                "durian", "Smells like trouble, tastes like custard.",
                // The Orchard
                "pomegranate", "A crown on top, a thousand jewels inside.",
                "fig", "So soft it barely needs a knife.",
                "blood_orange", "Ordinary outside. Ruby red when you cut it.",
                "honeydew", "Pale green and sweeter than it looks.",
                "persimmon", "Wait until it's soft, then it's honey.",
                "golden_apple", "From the oldest tree in the orchard. Just one a year.",
                // The Veggie Garden
                "sweet_potato", "Purple skin, orange heart, earthy and sweet.",
                "butternut", "Tough skin, buttery inside. Peel the neck first.",
                "eggplant", "Glossy as a new shoe.",
                "kohlrabi", "Looks like a tiny alien. Tastes like sweet cabbage.",
                "pumpkin", "Big enough to sit on. Better as soup.",
                "prize_marrow", "First prize at the village show, rosette and all.",
                // The Night Market
                "hami_melon", "From the Silk Road, crisp and honey-sweet.",
                "buddhas_hand", "All peel and perfume, fingers like a blessing.",
                "horned_melon", "Spiky orange shell, green jelly inside.",
                "snake_fruit", "Scaly skin that cracks off like a shell.",
                "pink_pineapple", "Grown for the night market, pink to the core.",
                "square_watermelon", "Grown in a box. Stacks perfectly.");

            Add(EnvironmentId.Globe,
                // The Ancient World
                "stonehenge", "Lined up with the midsummer sunrise for five thousand years.",
                "pyramids", "The only wonder of the ancient world still standing.",
                "great_wall", "Built over centuries, wall by wall, dynasty by dynasty.",
                "colosseum", "Fifty thousand seats and a canvas roof.",
                "lighthouse", "Alexandria's fire could be seen fifty kilometres out to sea.",
                "hanging_gardens", "Babylon's terraces. Nobody has ever found them.",
                // The Middle Ages
                "tower_of_pisa", "Started leaning before it was finished.",
                "notre_dame", "Two hundred years of stonemasons and gargoyles.",
                "hagia_sophia", "A dome that seems to float on light.",
                "alhambra", "Water, tiles and courtyards for the kings of Granada.",
                "angkor_wat", "The largest temple on Earth, built facing west.",
                "machu_picchu", "A city in the clouds, rediscovered in 1911.",
                // The Renaissance
                "florence_dome", "Brunelleschi raised it with no wooden frame underneath.",
                "st_peters", "Michelangelo designed the dome in his seventies.",
                "taj_mahal", "Marble that turns pink at dawn and gold at dusk.",
                "forbidden_city", "Nine hundred buildings, closed to the world for five centuries.",
                "st_basils", "Onion domes painted like sweets on Red Square.",
                "versailles", "A hunting lodge that got out of hand.",
                // The Industrial Age
                "big_ben", "Actually the name of the bell, not the tower.",
                "eiffel_tower", "Meant to stand for twenty years. Still here.",
                "statue_of_liberty", "A gift from France, shipped in 350 pieces.",
                "tower_bridge", "Opens its arms for tall ships.",
                "crystal_palace", "A palace of glass and iron for the Great Exhibition.",
                "brooklyn_bridge", "Steel wire spun across the East River.",
                // The Space Age
                "sydney_opera", "Sails of tiles over the harbour.",
                "cn_tower", "Walk on glass, 342 metres above Toronto.",
                "atomium", "An iron crystal blown up 165 billion times.",
                "burj_khalifa", "So tall you can watch the sunset twice.",
                "observatory_dome", "Opens at night to look back at the universe.",
                "launch_pad", "Three, two, one. The globe has never spun so fast.");

            Add(EnvironmentId.Boomerang,
                // The Outback
                "brass_bell", "Rings across the red dirt when you hit it square.",
                "red_kite", "Knocked clean out of the sky. It'll fly again.",
                "weather_vane", "Spins like mad, then points the way home.",
                "gong", "Heard all the way back at the homestead.",
                "paper_lantern", "Somehow it didn't tear. Lucky throw.",
                "golden_trophy", "First prize at the outback throwing contest.",
                // The Beach
                "ship_bell", "Salvaged from a wreck, still rings true.",
                "message_bottle", "Rolled out of the dunes. The note says 'nice throw'.",
                "giant_conch", "Hold it to your ear: you'll hear the boomerang coming back.",
                "surf_trophy", "Won by a surfer, now won by you.",
                "pirate_flag", "Flying over a sandcastle. Not any more.",
                "golden_pearl", "Knocked out of a clam the size of a suitcase.",
                // The Prairie
                "rooster_vane", "The rooster spins and crows. Well, creaks.",
                "cow_bell", "Every cow in the county looked up.",
                "blue_ribbon", "First prize at the county fair.",
                "windpump", "Clank, clank, then a cheer from the farmhands.",
                "barn_lantern", "Swinging on its hook, never went out.",
                "golden_horseshoe", "Luckiest thing on the prairie. Points up.",
                // The Snowfield
                "sleigh_bell", "Jingles for a whole minute after you hit it.",
                "ice_crystal", "Rings like glass in the frozen air.",
                "aurora_kite", "Green and violet, like the sky at midnight.",
                "igloo_lantern", "Glows through the snow walls when it's hit.",
                "ice_trophy", "Carved from one block. Won't last the spring.",
                "snow_globe", "Shook it without breaking it. Somehow.",
                // The Old Ruins
                "jade_idol", "Older than the jungle around it.",
                "sun_disc", "Catches the morning sun and throws it back.",
                "temple_bell", "Hasn't rung in a thousand years. Until now.",
                "feather_crown", "Quetzal feathers, still bright green.",
                "ruby_eye", "Knocked loose from a stone jaguar.",
                "golden_mask", "The face of a forgotten king, smiling.");

            return d;
        }
    }
}
