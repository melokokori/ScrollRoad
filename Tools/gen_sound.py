"""Araç seslerini üretir: ElevenLabs Sound Effects v2 — ELEVENLABS_API_KEY tanımlıysa doğrudan
ElevenLabs'ten, değilse fal.ai üzerinden (FAL_KEY).

Her araç için iki ses:
  <model>_loop.ogg   kesintisiz döngü (sürüş sesi; oyunda perdesi ve şiddeti hıza göre kayar)
  <model>_start.ogg  kısa tek seferlik ses (garajda seçilince: marş, korna, düdük...)
Çıktı: Assets/Resources/VehicleSounds/ (VehicleSound çalışma zamanında Resources.Load ile yükler).
Ham indirmeler Tools/sound_source/raw/ altında kalır (git dışı).

Ses yüksekliği ffmpeg loudnorm ile eşitlenir (döngü -23 LUFS, tek seferlik -20 LUFS) ki araçlar
arasında ses düzeyi zıplamasın.

Kullanım:
  python Tools/gen_sound.py car vesper            # listedeki tarifle üret
  python Tools/gen_sound.py car --only loop       # yalnız döngüyü yeniden üret
  python Tools/gen_sound.py car --loop "<tarif>"  # tarifi elle ver (listedekini geçersiz kılar)
  python Tools/gen_sound.py --env door_loop reel_loop   # ortam sesleri (ENV_SOUNDS) -> Resources/EnvSounds

Ortam sesleri (2026-10-14): oyun saatlerce arka planda açık kaldığı için sade, yumuşak, alçak
seviyeli (döngü -27 LUFS, tek seferlik -24 LUFS; araçlardan ~4 dB kısık). İnce işaret sesleri
(misafir/efsane: guest_chime, legend_chime) yapay zekâyla değil prosedürel üretilir (sinüs çan, telifsiz).
"""
import argparse
import json
import os
import subprocess
import sys
import time
import urllib.request

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(ROOT, "Assets", "Resources", "VehicleSounds")
RAW = os.path.join(ROOT, "Tools", "sound_source", "raw")
ENDPOINT = "https://fal.run/fal-ai/elevenlabs/sound-effects/v2"

# Tarifler gerçek modellerin KARAKTERİNİ anlatır (marka adı yok). Döngü: dışarıdan, sabit hızda,
# müziksiz. Tek seferlik: kısa ve karakteristik.
RECIPES = {
    "car": (
        "Small 1990s European hatchback with a 1.3 litre four-cylinder petrol engine cruising at a steady speed, "
        "heard from outside, smooth even engine hum with light tyre roll on asphalt, no music, no voices",
        "Small hatchback petrol engine start: key turn, short starter crank, engine catches and settles into a calm idle",
    ),
    "vesper": (
        "Classic Italian 125cc two-stroke scooter riding at a steady speed, buzzy high-pitched two-stroke engine, "
        "light wind, heard from outside, no music, no voices",
        "Vintage two-stroke scooter kick start, engine sputters to life and idles, then one short cheerful horn beep",
    ),
    "heavyboy": (
        "Big American V-twin cruiser motorcycle cruising at a steady speed, deep loping uneven potato-potato exhaust rumble, "
        "heard from outside, no music, no voices",
        "Big V-twin motorcycle electric start, deep loping idle and one throttle blip",
    ),
    "ferry": (
        "Old Istanbul passenger ferry crossing the Bosphorus at steady speed, slow deep diesel engine thumping, "
        "water rushing along the hull, a few distant seagulls, no music, no voices",
        "Large old passenger ferry steam horn, one long deep blast echoing over the water",
    ),
    "airliner": (
        "Commercial twin-engine jet airliner cruising high in the sky, steady turbofan engine roar and wind rush "
        "heard from outside, no music, no voices",
        "Jet airliner engines spooling up from idle to take-off thrust",
    ),
}

# Kalan kadro: tarifte ilham alınan GERÇEK modelin adı geçer (yalnız üretime yol gösterir; oyunda
# kurgusal adlar kullanılır, ses dosyasında marka yoktur). (araç, döngü tarifi, tek seferlik tarif)
LOOP_TAIL = ", steady constant speed, heard from outside, continuous, no music, no voices"
_ROSTER = [
    # Otomobiller
    ("atmaca", "Tofas Sahin, a 1990s Turkish Fiat 131 based sedan with a 1.6 litre carburetted four-cylinder petrol engine driving",
     "Old Tofas Sahin sedan start: key turn, long carburettor starter crank, engine coughs to life and idles roughly"),
    ("zefir", "1970s Turkish Anadol sedan with a small Ford Kent four-cylinder engine driving, slightly rough old engine",
     "1970s small four-cylinder car starting on a cold morning, short crank and a light throttle blip"),
    ("coralsedan", "Toyota Corolla sedan with a quiet 1.6 litre four-cylinder petrol engine driving, smooth and refined",
     "Toyota Corolla push-button start, quiet engine idles, one short polite horn beep"),
    ("t10s", "Togg T10F electric sedan driving, soft electric motor whine and tyre roll on asphalt, no engine",
     "Modern electric car powering on: soft futuristic chime and a gentle electric hum rising"),
    ("dreier", "BMW 3 Series E46 with a 2.5 litre straight-six petrol engine driving, smooth silky six-cylinder hum",
     "BMW straight-six engine starting and settling into a smooth idle with one rev"),
    ("van", "1960s camper van with an air-cooled flat-four engine driving, distinctive clattery boxer engine buzz",
     "Air-cooled flat-four camper van starting, clattery starter and lumpy idle"),
    ("impulso", "Subaru Impreza WRX with a turbocharged flat-four boxer engine driving, deep uneven boxer rumble with turbo whistle",
     "Subaru boxer engine starting with its burbling uneven idle and a turbo blow-off on a rev"),
    ("type3", "Tesla Model 3 electric car driving, quiet high-pitched electric motor whine and tyre roll",
     "Tesla style electric car waking up: door chime and a soft electric hum"),
    ("chaser", "Dodge Charger with a 6.4 litre Hemi V8 driving, loud deep American V8 burble",
     "Dodge Charger Hemi V8 cold start, loud bark then deep rumbling idle"),
    ("towne", "Lincoln Town Car with a soft 4.6 litre V8 driving, smooth quiet luxury cruiser",
     "Old American luxury V8 sedan starting smoothly and idling softly"),
    ("panorama", "Porsche Panamera with a twin-turbo V8 driving, refined sporty growl",
     "Porsche Panamera V8 start with a sharp rev flare then settling"),
    ("apparition", "Rolls-Royce Phantom with a V12 engine driving, almost silent, only a faint deep hum and soft tyre roll",
     "Rolls-Royce V12 engine starting almost silently, a soft deep murmur"),
    # Arazi, SUV, pikap
    ("duner", "Dacia Duster with a 1.5 litre diesel engine driving on a gravel road, diesel clatter and gravel crunch",
     "Small diesel SUV starting with diesel clatter"),
    ("taiga", "Lada Niva 4x4 with a 1.7 litre petrol engine driving on a dirt track, old gearbox whine and rattles",
     "Old Lada Niva starting with a choke, rough idle"),
    ("jinny", "Suzuki Jimny small 4x4 driving on a dirt trail, buzzy small four-cylinder and bumpy suspension",
     "Suzuki Jimny engine starting and revving playfully"),
    ("pickup", "Toyota Hilux pickup with a 2.8 litre turbo diesel driving on a dirt road, diesel rumble and gravel",
     "Toyota Hilux diesel pickup starting, glow plug pause then diesel idle"),
    ("atv", "Quad bike ATV with a 450cc single-cylinder four-stroke engine riding on a dirt trail, buzzy thumping",
     "Quad bike ATV starting with a pull and a few throttle blips"),
    ("t10x", "Togg T10X electric SUV driving on a gravel track, electric motor whine and tyre crunch, no engine",
     "Electric SUV powering on: futuristic startup chime and soft hum"),
    ("landcrawler", "Toyota Land Cruiser 70 series with a V8 diesel driving off-road, deep diesel rumble and gravel",
     "Toyota Land Cruiser V8 diesel starting, deep diesel idle"),
    ("guardian", "classic Land Rover Defender with a 2.4 litre diesel driving off-road, agricultural diesel clatter and rattles",
     "Old Land Rover Defender diesel starting, clattery idle"),
    ("wrangel", "Jeep Wrangler with a 3.6 litre V6 driving off-road, open top wind and rugged engine",
     "Jeep Wrangler V6 starting with a rev"),
    ("mercatorg", "Mercedes-Benz G63 with a twin-turbo V8 driving, deep aggressive exhaust rumble",
     "Mercedes G63 V8 starting with a loud bark"),
    ("ibex", "Ram 3500 dually pickup with a Cummins turbo diesel driving, loud heavy diesel and turbo spool",
     "Big Cummins diesel pickup starting, heavy diesel clatter and turbo whistle"),
    ("escapade", "Cadillac Escalade with a 6.2 litre V8 driving, smooth heavy luxury SUV",
     "Cadillac Escalade V8 starting and idling smoothly"),
    ("hammerhx", "Hummer H1 with a 6.5 litre turbo diesel V8 driving off-road, gruff heavy diesel and chunky tyres",
     "Hummer H1 diesel V8 starting, gruff military truck idle"),
    ("toro", "Lamborghini Urus with a twin-turbo V8 driving, sporty aggressive super SUV exhaust",
     "Lamborghini Urus V8 starting with a loud rev flare"),
    # İki teker
    ("mobilo", "Motobecane Mobylette 50cc moped riding, high-pitched two-stroke moped buzz like a sewing machine",
     "Mobylette moped pedal start, tiny two-stroke engine sputters to life"),
    ("pcx", "Honda PCX 125 scooter riding, smooth quiet four-stroke scooter hum with CVT whine",
     "Honda PCX scooter push-button start and short beep of the horn"),
    ("javo", "Jawa 350 vintage two-stroke twin motorcycle riding, ringing two-stroke exhaust",
     "Jawa 350 two-stroke motorcycle kick start, ringing idle with blue smoke crackle"),
    ("chimp", "Honda Monkey 125 mini bike riding, cute small four-stroke single putter",
     "Honda Monkey mini bike starting and blipping cheerfully"),
    ("atlasgs", "BMW R 1250 GS adventure motorcycle with a boxer twin riding on a gravel road, distinctive boxer thrum",
     "BMW GS boxer twin motorcycle starting, the bike rocks with a boxer twin rev"),
    ("anfield", "Royal Enfield Classic 350 riding, slow deep single-cylinder thump thump",
     "Royal Enfield Classic 350 starting, classic single-cylinder thumping idle"),
    ("bonnevale", "Triumph Bonneville parallel-twin motorcycle riding, smooth British twin burble",
     "Triumph Bonneville parallel twin starting with a rev"),
    ("baron", "KTM 390 Duke single-cylinder naked bike riding, sharp punchy single-cylinder bark",
     "KTM 390 Duke starting with sharp throttle blips"),
    ("snodoo", "Ski-Doo snowmobile with a two-stroke engine riding over snow, buzzy engine and track clatter on snow",
     "Snowmobile pull start, two-stroke engine catches and revs"),
    ("tarantula", "Can-Am Spyder three-wheel roadster with a Rotax three-cylinder riding, smooth triple growl",
     "Can-Am Spyder Rotax triple engine starting with a rev"),
    ("hayate", "Suzuki Hayabusa sport bike riding fast, high-revving inline-four howl",
     "Suzuki Hayabusa inline-four starting and revving high"),
    ("monza", "Ducati V-twin sport bike riding, loud desmo V-twin with dry clutch rattle",
     "Ducati V-twin starting, dry clutch rattle and loud rev"),
    # Deniz
    ("swanboat", "Swan pedal boat moving on a lake, clearly audible paddle wheel churning and splashing water, "
     "rhythmic pedal creaks, close up",
     "Pedal boat creaking as someone starts pedalling, water splashes"),
    ("rowboat", "Small wooden rowboat on a calm lake, rhythmic oars splashing and creaking oarlocks",
     "Wooden oars dipping into water for the first strokes, oarlocks creak"),
    ("sailboat", "Small sailboat sailing, wind in the sails, sail flapping and water lapping on the hull",
     "Sail being hoisted and filling with wind with a snap"),
    ("tugboat", "Harbour tugboat chugging along, deep slow diesel thumping and water wash",
     "Tugboat horn, two short deep toots"),
    ("watercraft", "Sea-Doo jet ski personal watercraft riding, revving jet pump engine and water spray slaps",
     "Jet ski engine starting and revving in water"),
    ("speedboat", "Speedboat with a V8 inboard engine racing across water, engine roar and hull slapping waves",
     "Speedboat V8 engine starting with a deep burble on the water"),
    ("seiner", "Fishing boat with an old diesel engine at sea, slow diesel putter, waves and seagulls",
     "Fishing boat diesel engine starting, then seagulls cry"),
    ("yacht", "Luxury motor yacht with twin diesels cruising, smooth deep engines and water wash",
     "Luxury yacht horn, one elegant short blast"),
    ("hovercraft", "Hovercraft with large propeller fans moving over water, loud fan roar and air rush",
     "Hovercraft fans spooling up and the skirt inflating with air"),
    ("seabus", "Istanbul high-speed sea bus catamaran ferry cruising, engines whine and water jets",
     "Passenger sea bus ferry horn, one medium blast"),
    ("container", "Huge container ship at sea, very slow deep diesel engine thump and heavy water",
     "Huge container ship horn, very deep long blast"),
    ("icebreaker", "Icebreaker ship pushing through sea ice, crunching cracking ice and deep engines",
     "Icebreaker ship horn and a loud ice crack"),
    ("submarine", "Submarine moving underwater, deep low engine hum, muffled water and occasional distant sonar ping",
     "Submarine sonar ping echoing twice"),
    ("liner", "1910s steam ocean liner at sea, rhythmic steam engines, water against the hull",
     "Old ocean liner steam whistle, deep long blast"),
    ("cruise", "Modern cruise ship at sea, soft deep engine hum, sea breeze and waves",
     "Cruise ship horn, deep and long"),
    # Hava
    ("cropduster", "Boeing Stearman biplane with a radial engine flying, loud rough radial engine drone",
     "Radial engine biplane starting: whine, cough, puffs of smoke and the engine catches"),
    ("helicopter", "Bell 206 JetRanger helicopter flying, turbine whine and steady rotor blade thwop",
     "Helicopter turbine starting and rotor blades spinning up"),
    ("seaplane", "de Havilland Beaver floatplane with a radial engine flying, steady radial drone",
     "Beaver floatplane radial engine starting with coughs and catching"),
    ("glider", "Sailplane glider soaring silently, only smooth wind rushing over the wings",
     "Glider tow release click and wind rushing"),
    ("balloon", "Hot air balloon floating high, gentle wind with occasional loud propane burner roars",
     "Hot air balloon propane burner firing, one long roar"),
    ("bizjet", "Gulfstream business jet cruising, smooth high turbofan whine and wind",
     "Business jet engines spooling up"),
    ("airship", "Zeppelin NT airship floating, soft propeller drone and gentle wind",
     "Airship propeller engines starting up softly"),
    ("fighter", "F-16 fighter jet flying fast, loud jet engine roar",
     "Fighter jet afterburner lighting with a thunderous roar"),
    ("shuttle", "Space Shuttle rocket ascending, thunderous rocket engine roar and rumble",
     "Space Shuttle main engines igniting, countdown-free thunderous ignition"),
]
for _id, _loop, _start in _ROSTER:
    RECIPES[_id] = (_loop + LOOP_TAIL, _start)

# Ortam sesleri (Döner kapı, Olta, sonraki ortamlar): ad -> (döngü mü, tarif, süre sn).
ENV_OUT = os.path.join(ROOT, "Assets", "Resources", "EnvSounds")
ENV_SOUNDS = {
    # Rüzgârgülü (Ortam Standardı v2, 2026-10-25): uğultu döngüsü (perde hıza göre) ve bağlanma kliği.
    "wind_loop": (True, "Steady breeze on an open grassy hill with the slow rhythmic whoosh of large wind turbine blades "
                        "turning, soft and calm, distant, no birds, no music" + LOOP_TAIL, 8.0),
    "space_hum": (True, "procedural", 8.0),
    "breeze_loop": (True, "procedural", 8.0),
    "grid_click": (False, "One tiny soft electrical relay click with a faint warm hum as a light switches on, close up, "
                          "very quiet, no music, no voices", 0.8),
    # Döner kapı: her kanat geçişinde bir süpürme (oyunda kanat sıklığı = dönüş hızı; şiddet/perde hıza göre).
    "door_sweep": (False, "One single soft whoosh of a glass revolving door wing sweeping past, rubber seal gently brushing "
                          "the floor and curved glass, airy and calm, close up, quiet, no music, no voices", 1.5),
    # Olta: makara sarma (scroll hızına bağlı perde), atış, vuruş, tutuş.
    "reel_loop": (True, "Spinning fishing reel handle being cranked steadily by hand, soft smooth gear whirr with a light "
                        "rhythmic ticking, quiet lakeside, close up" + LOOP_TAIL, 8.0),
    # Olta takımı skinleri (2026-10-15, FishingGear): her takımın kendi makara döngüsü (Klasik = reel_loop).
    "reel_loop_centerpin": (True, "Old wooden centerpin fishing reel spinning freely as line is wound in by hand: a soft "
                                  "dry clicking ratchet, light hollow wooden rattle, slow and gentle, quiet riverside, "
                                  "close up" + LOOP_TAIL, 8.0),
    "reel_loop_carbon": (True, "Modern high-end carbon spinning fishing reel cranked steadily: very smooth silky "
                               "ball-bearing whirr, barely any clicking, precise and quiet, close up" + LOOP_TAIL, 8.0),
    "reel_loop_gold": (True, "Luxury polished brass baitcasting fishing reel cranked steadily: a smooth fine metallic "
                             "purr with a delicate high soft ticking, quiet, close up" + LOOP_TAIL, 8.0),
    "fish_cast": (False, "Fishing rod cast: one quick soft whoosh of the rod, line zipping off a spinning reel, then a small "
                         "lure plop into calm water in the distance, quiet, no music", 2.5),
    "fish_strike": (False, "A fish bites: short zip of a fishing reel drag as the line tightens, then a small splash, quiet, no music", 1.8),
    "fish_catch": (False, "A medium fish lifted out of the water: a gentle splash and water dripping, quiet lakeside, no music", 2.2),
    # Dünyayı döndüren el (2026-10-14): tur başına bir el itişi, yıl dönünce sayfa çevirme.
    "globe_push": (False, "A hand giving a big wooden desk globe one gentle push: a soft dry brush of a palm on a smooth "
                          "surface and a low airy whoosh as it spins, calm, close up, quiet, no music, no voices", 1.4),
    "year_page": (False, "One single soft page turn of an old thick book, dry paper, gentle and quiet, close up, "
                         "no music, no voices", 1.2),
    # Bumerang (2026-10-14): fırlatma, hedefe vuruş, ele dönüşte yakalama (atış başına birer; seyirde ~15 sn'de bir).
    "boom_throw": (False, "A wooden boomerang thrown by hand: one quick airy whoosh with a soft spinning flutter fading "
                          "into the distance, outdoors, calm, quiet, no music, no voices", 1.6),
    "boom_hit": (False, "A wooden boomerang hitting a target at a distance: one soft hollow wooden knock and a light "
                        "rattle, outdoors, quiet, no music, no voices", 1.2),
    "boom_catch": (False, "Catching a returning wooden boomerang: a short soft whoosh ending in one gentle slap of a "
                          "hand catching it, outdoors, quiet, no music, no voices", 1.2),
    # Çömlekçi çarkı (2026-10-14): çark döngüsü (yalnız kullanıcı aktifken), çarka topak konması, rafa konma.
    "wheel_loop": (True, "Electric pottery wheel spinning steadily with soft wet clay being shaped by hands, a gentle "
                         "smooth motor hum and quiet slippery squelching, calm workshop, close up" + LOOP_TAIL, 8.0),
    "clay_slap": (False, "One soft wet lump of clay slapped down onto a pottery wheel, a single dull thud, quiet, "
                         "no music, no voices", 1.0),
    "pot_shelf": (False, "A small ceramic pot placed gently on a wooden shelf: one soft hollow clunk of fired clay on "
                         "wood, quiet, close up, no music, no voices", 1.0),
    # Meyve soyacağı (2026-10-14): kol + soyma döngüsü (yalnız kullanıcı aktifken), meyvenin kâseye düşmesi.
    "peel_loop": (True, "A hand-cranked cast iron apple peeler turning steadily: soft rhythmic gear clicks and a gentle "
                        "continuous scraping of a blade peeling fruit skin, quiet kitchen, close up" + LOOP_TAIL, 8.0),
    "fruit_drop": (False, "A peeled apple dropping into a wooden fruit bowl: one soft dull wooden thud, quiet, close "
                          "up, no music, no voices", 1.0),
    # Çağ kebabı (2026-10-14): ateş + cızırtı döngüsü (yalnız kullanıcı aktifken), kesim, porsiyonun tezgâha konması.
    "sizzle_loop": (True, "Meat slowly roasting on a turning horizontal spit over a wood fire: soft fat sizzle and gentle "
                          "crackling embers, calm kebab kitchen, close up" + LOOP_TAIL, 8.0),
    "knife_slice": (False, "A long sharp kebab knife shaving thin slices off roasted meat on a spit: a few soft quick "
                           "slicing swishes, close up, quiet, no music, no voices", 1.8),
    "plate_serve": (False, "A ceramic plate slid onto a steel serving counter: one short soft scrape and a gentle clink, "
                           "quiet, no music, no voices", 1.0),
    # İnce işaret sesleri: tarif yok, prosedürel (aşağıda _chime).
    "guest_chime": (False, None, 0),
    "legend_chime": (False, None, 0),
    # Ortam nesneleri (2026-10-25, TallyView): yeni birimde çok kısa işaretler, prosedürel.
    "tally_click": (False, None, 0),
    "register_ding": (False, None, 0),
    "ticket_ding": (False, None, 0),
}


def _chime(notes, gap, tau, length, path):
    """Yumuşak çan: her nota sinüs + çan benzeri iki üst kısmi (2,76x / 5,4x), 6 ms atak, üstel sönüm."""
    import numpy as np
    sr = 44100
    t = np.arange(int(length * sr)) / sr
    x = np.zeros_like(t)
    for i, (f, amp) in enumerate(notes):
        tt = t - i * gap
        on = tt >= 0
        env = np.where(on, np.minimum(1.0, np.maximum(tt, 0) / 0.006) * np.exp(-np.maximum(tt, 0) / tau), 0.0)
        tone = np.sin(2 * np.pi * f * tt) + 0.16 * np.sin(2 * np.pi * f * 2.76 * tt) * np.exp(-np.maximum(tt, 0) / (tau * 0.35))             + 0.05 * np.sin(2 * np.pi * f * 5.4 * tt) * np.exp(-np.maximum(tt, 0) / (tau * 0.2))
        x += amp * env * tone
    x = (x / np.abs(x).max() * 0.5).astype(np.float32)
    stereo = np.stack([x, x], axis=1)
    subprocess.run(["ffmpeg", "-y", "-loglevel", "error", "-f", "f32le", "-ac", "2", "-ar", str(sr), "-i", "-",
                    "-c:a", "pcm_s16le", path], input=stereo.tobytes(), check=True)


def _write_wav(x, path, sr=44100):
    import numpy as np
    x = (x / np.abs(x).max() * 0.5).astype(np.float32)
    stereo = np.stack([x, x], axis=1)
    subprocess.run(["ffmpeg", "-y", "-loglevel", "error", "-f", "f32le", "-ac", "2", "-ar", str(sr), "-i", "-",
                    "-c:a", "pcm_s16le", path], input=stereo.tobytes(), check=True)


def _wind_loop(path, length=8.0, blade_hz=0.75):
    """Rüzgâr + pervane uğultusu (2026-10-25; fal.ai kapalı, ElevenLabs anahtarı yok): frekans uzayında
    şekillendirilmiş gürültü (dairesel → dikişsiz döngü), üstüne pervane geçişi kadar (tam sayı döngü) yavaş bir
    'vuuş' genliği ve onunla gelen hafif bant gürültüsü."""
    import numpy as np
    sr = 44100
    n = int(length * sr)
    rng = np.random.default_rng(7)
    f = np.fft.rfftfreq(n, 1 / sr)
    def shaped(lo, hi, slope):
        spec = np.fft.rfft(rng.standard_normal(n))
        gain = np.where(f > 0, (np.maximum(f, 1.0) / 100.0) ** (-slope), 0.0)
        gain *= 1 / (1 + (f / hi) ** 4) * (1 - 1 / (1 + (f / lo) ** 4))
        return np.fft.irfft(spec * gain, n)
    base = shaped(20, 900, 0.9)        # derin rüzgâr
    swish = shaped(250, 2500, 0.4)     # kanat sürtünmesi
    t = np.arange(n) / sr
    whoosh = np.sin(np.pi * blade_hz * t) ** 2   # 8 sn'de tam sayı geçiş: dikişsiz
    gust = 0.85 + 0.15 * np.sin(2 * np.pi * t / length)
    x = base / np.abs(base).max() * gust + 0.55 * swish / np.abs(swish).max() * (0.25 + 0.75 * whoosh)
    _write_wav(x, path, sr)


def _relay_click(path):
    """Çok kısa, yumuşak röle kliği + sönen sıcak uğultu (yapı şebekeye bağlanınca)."""
    import numpy as np
    sr = 44100
    t = np.arange(int(0.6 * sr)) / sr
    rng = np.random.default_rng(3)
    click = rng.standard_normal(t.size) * np.exp(-t / 0.0025)
    hum = (np.sin(2 * np.pi * 110 * t) + 0.4 * np.sin(2 * np.pi * 220 * t)) * np.minimum(1, t / 0.01) * np.exp(-t / 0.18)
    _write_wav(0.6 * click + 0.25 * hum, path, sr)


def _space_hum(path, length=8.0):
    """Dünya (Ortam Standardı v2): alçak, yumuşak bir uzay uğultusu — 8 sn'de tam sayı döngü yapan üç sinüs
    akoru ve çok kısık şekillendirilmiş gürültü; dikişsiz döngü."""
    import numpy as np
    sr = 44100
    n = int(length * sr)
    t = np.arange(n) / sr
    tones = sum(a * np.sin(2 * np.pi * f * t) * (0.7 + 0.3 * np.sin(2 * np.pi * m * t / length))
                for f, a, m in ((55.0, 1.0, 1), (82.5, 0.6, 2), (110.0, 0.35, 3), (164.875, 0.15, 1)))
    rng = np.random.default_rng(11)
    spec = np.fft.rfft(rng.standard_normal(n))
    f = np.fft.rfftfreq(n, 1 / sr)
    air = np.fft.irfft(spec * np.where(f > 0, 1 / (1 + (f / 400) ** 2) / np.maximum(f, 20) ** 0.5, 0), n)
    _write_wav(tones / np.abs(tones).max() + 0.25 * air / np.abs(air).max(), path, sr)


def _breeze_loop(path, length=8.0):
    """Bumerang (Ortam Standardı v2): açık arazide hafif esinti — şekillendirilmiş gürültü, iki yavaş rüzgâr
    dalgası; dikişsiz döngü."""
    import numpy as np
    sr = 44100
    n = int(length * sr)
    rng = np.random.default_rng(5)
    f = np.fft.rfftfreq(n, 1 / sr)
    spec = np.fft.rfft(rng.standard_normal(n))
    gain = np.where(f > 0, (np.maximum(f, 1.0) / 100.0) ** -0.7, 0.0) / (1 + (f / 1500) ** 4) * (1 - 1 / (1 + (f / 60) ** 4))
    x = np.fft.irfft(spec * gain, n)
    t = np.arange(n) / sr
    gust = 0.6 + 0.25 * np.sin(2 * np.pi * t / length) + 0.15 * np.sin(2 * np.pi * 3 * t / length + 1.3)
    _write_wav(x / np.abs(x).max() * gust, path, sr)


def _tally_click(path):
    """Kapı'nın el sayacı: basma ve bırakma — iki kısa mekanik tık (bant geçiren gürültü + ince metal tını)."""
    import numpy as np
    sr = 44100
    t = np.arange(int(0.25 * sr)) / sr
    rng = np.random.default_rng(9)
    x = np.zeros_like(t)
    for at, amp in ((0.0, 1.0), (0.075, 0.6)):
        tt = t - at
        on = tt >= 0
        noise = rng.standard_normal(t.size)
        spec = np.fft.rfft(noise)
        f = np.fft.rfftfreq(t.size, 1 / sr)
        noise = np.fft.irfft(spec * np.exp(-((f - 3200) / 1400) ** 2), t.size)
        env = np.where(on, np.exp(-np.maximum(tt, 0) / 0.004), 0.0)
        ring = np.where(on, np.sin(2 * np.pi * 2350 * tt) * np.exp(-np.maximum(tt, 0) / 0.02), 0.0)
        x += amp * (noise / np.abs(noise).max() * env + 0.25 * ring)
    _write_wav(x, path, sr)


def _register_ding(path):
    """Soyacak'ın yazar kasası: çekmecenin boğuk 'çat'ı ve ardından tek, parlak kasa zili."""
    import numpy as np
    sr = 44100
    _chime([(2093.0, 1.0)], 0.0, 0.45, 1.4, path)
    import subprocess as sp
    raw = sp.run(["ffmpeg", "-loglevel", "error", "-i", path, "-f", "f32le", "-ac", "1", "-ar", str(sr), "-"],
                 capture_output=True, check=True).stdout
    bell = np.frombuffer(raw, dtype=np.float32).copy()
    t = np.arange(bell.size) / sr
    rng = np.random.default_rng(4)
    thump = rng.standard_normal(bell.size) * np.exp(-t / 0.012) * 0.5 + np.sin(2 * np.pi * 140 * t) * np.exp(-t / 0.05)
    shift = int(0.06 * sr)  # zil çattan hemen sonra
    x = 0.55 * thump
    x[shift:] += bell[:-shift]
    _write_wav(x, path, sr)


PROCEDURAL = {
    "space_hum": _space_hum,
    "breeze_loop": _breeze_loop,
    "wind_loop": _wind_loop,
    "grid_click": _relay_click,
    "tally_click": _tally_click,
    "register_ding": _register_ding,
    # Kebap'ın sıra numaratörü: elektronik "dıt-dıt" (G5 → E5), kısa sönüm.
    "ticket_ding": lambda path: _chime([(784.0, 1.0), (659.3, 0.9)], 0.2, 0.28, 1.0, path),
    # Kapı zili gibi iki nota (E6 → C6), kısa ve yumuşak.
    "guest_chime": lambda path: _chime([(1318.5, 1.0), (1046.5, 0.9)], 0.17, 0.55, 1.8, path),
    # Efsane: yükselen dört nota (C6 E6 G6 C7), biraz daha uzun sönüm.
    "legend_chime": lambda path: _chime([(1046.5, 0.9), (1318.5, 0.85), (1568.0, 0.8), (2093.0, 0.55)], 0.13, 0.9, 2.8, path),
}


def generate(text, seconds, loop, influence):
    # ELEVENLABS_API_KEY varsa doğrudan ElevenLabs, yoksa fal.ai (aynı model).
    eleven = os.environ.get("ELEVENLABS_API_KEY")
    if eleven:
        body = {"text": text, "loop": loop, "prompt_influence": influence, "model_id": "eleven_text_to_sound_v2"}
        if seconds:
            body["duration_seconds"] = seconds
        req = urllib.request.Request("https://api.elevenlabs.io/v1/sound-generation?output_format=mp3_44100_192",
                                     data=json.dumps(body).encode(), method="POST",
                                     headers={"xi-api-key": eleven, "Content-Type": "application/json"})
        with urllib.request.urlopen(req, timeout=300) as r:
            return r.read()

    key = os.environ.get("FAL_KEY")
    if not key:
        sys.exit("ELEVENLABS_API_KEY ya da FAL_KEY tanımlı değil")
    body = json.dumps({
        "text": text,
        "duration_seconds": seconds,
        "loop": loop,
        "prompt_influence": influence,
        "output_format": "mp3_44100_192",
    }).encode()
    req = urllib.request.Request(ENDPOINT, data=body, method="POST", headers={
        "Authorization": "Key " + key,
        "Content-Type": "application/json",
    })
    with urllib.request.urlopen(req, timeout=300) as r:
        result = json.load(r)
    url = result["audio"]["url"]
    with urllib.request.urlopen(url, timeout=300) as r:
        return r.read()


CROSSFADE_SECONDS = 0.5


def _decode(path):
    import numpy as np
    pcm = subprocess.run(["ffmpeg", "-v", "error", "-i", path, "-f", "f32le", "-ac", "2", "-ar", "44100", "-"],
                         capture_output=True, check=True).stdout
    return np.frombuffer(pcm, dtype=np.float32).reshape(-1, 2).copy()


def _loudness(x):
    """Bütünleşik ses yüksekliği (LUFS), ffmpeg ebur128 ile."""
    out = subprocess.run(["ffmpeg", "-hide_banner", "-nostats", "-f", "f32le", "-ac", "2", "-ar", "44100", "-i", "-",
                          "-af", "loudnorm=print_format=json", "-f", "null", "-"],
                         input=x.tobytes(), capture_output=True).stderr.decode("utf-8", "replace")
    return float(json.loads(out[out.rindex("{"):out.rindex("}") + 1])["input_i"])


def to_ogg(raw_path, out_path, lufs, loop):
    """Tüm işlem örnek düzeyinde (gecikme yok, döngü eki bozulmaz):
    döngüde son 0,5 sn başa eşit güçlü geçişle bindirilir; ses yüksekliği ölçülüp sabit kazanç
    verilir (bas ağırlıklı seslerde kazanç büyük kalmasın diye en fazla +9 dB); tepeler yumuşak
    doyumla 0,92'nin altına çekilir; tek seferlik sesin son 0,35 sn'si söner."""
    import numpy as np
    x = _decode(raw_path)
    n = int(CROSSFADE_SECONDS * 44100)
    if loop and len(x) > 4 * n:
        t = np.linspace(0, np.pi / 2, n, dtype=np.float32)[:, None]
        x = np.concatenate([x[:n] * np.sin(t) + x[-n:] * np.cos(t), x[n:-n]])
    # Kazanç: ses yüksekliği hedefi, ama en fazla +9 dB ve tepe en çok 3 dB doyuma girecek kadar
    # (ani tepeli sesler — tık, çatırtı — aşırı doyumda kare dalgaya döner, Vorbis onu tepeden taşırır).
    peak = float(np.abs(x).max()) or 1.0
    gain_db = min(9.0, lufs - _loudness(x), 20 * np.log10(0.8 / peak) + 3.0)
    x = x * (10 ** (gain_db / 20))
    # Yumuşak doyum: |x| > 0.6 bölgesi tanh ile 0.8 tavanına bükülür (Vorbis'e taşma payı).
    t, ceil = 0.6, 0.8
    a = np.abs(x)
    over = a > t
    x[over] = np.sign(x[over]) * (t + (ceil - t) * np.tanh((a[over] - t) / (ceil - t)))
    if not loop:
        f = min(len(x), int(0.35 * 44100))
        x[-f:] *= np.linspace(1, 0, f, dtype=np.float32)[:, None]
    wav = out_path + ".tmp.wav"
    subprocess.run(["ffmpeg", "-y", "-loglevel", "error", "-f", "f32le", "-ac", "2", "-ar", "44100", "-i", "-",
                    "-c:a", "pcm_s16le", wav], input=x.astype(np.float32).tobytes(), check=True)
    subprocess.run(["ffmpeg", "-y", "-loglevel", "error", "-i", wav, "-c:a", "libvorbis", "-q:a", "6", out_path], check=True)
    os.remove(wav)


def latest_raw(model, kind):
    files = sorted(f for f in os.listdir(RAW) if f.startswith(f"{model}_{kind}_") and f.endswith(".mp3"))
    return os.path.join(RAW, files[-1]) if files else None


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("models", nargs="+")
    ap.add_argument("--only", choices=["loop", "start"])
    ap.add_argument("--loop", dest="loop_text")
    ap.add_argument("--start", dest="start_text")
    ap.add_argument("--seconds", type=float, default=10.0, help="döngü süresi")
    ap.add_argument("--start-seconds", type=float, default=3.5, help="tek seferlik ses süresi")
    ap.add_argument("--reprocess", action="store_true", help="üretmeden son ham kaydı yeniden işle")
    ap.add_argument("--env", action="store_true", help="adlar ENV_SOUNDS'tan (ortam sesleri)")
    args = ap.parse_args()

    if args.env:
        os.makedirs(ENV_OUT, exist_ok=True)
        os.makedirs(RAW, exist_ok=True)
        for name in args.models:
            loop, text, seconds = ENV_SOUNDS[name]
            t0 = time.time()
            if name in PROCEDURAL:
                raw = os.path.join(RAW, f"{name}_env.wav")
                PROCEDURAL[name](raw)
            elif args.reprocess:
                raw = latest_raw(name, "env")
            else:
                raw = os.path.join(RAW, f"{name}_env_{int(t0)}.mp3")
                with open(raw, "wb") as f:
                    f.write(generate(text, seconds, loop, 0.6))
            out = os.path.join(ENV_OUT, f"{name}.ogg")
            to_ogg(raw, out, -27 if loop else -24, loop)
            print(f"{name}: {time.time() - t0:.1f} sn -> {os.path.relpath(out, ROOT)}")
        return

    os.makedirs(OUT, exist_ok=True)
    os.makedirs(RAW, exist_ok=True)
    for model in args.models:
        loop_text, start_text = RECIPES.get(model, (None, None))
        loop_text = args.loop_text or loop_text
        start_text = args.start_text or start_text
        jobs = []
        if args.only in (None, "loop") and loop_text:
            jobs.append(("loop", loop_text, args.seconds, True, -23))
        if args.only in (None, "start") and start_text:
            jobs.append(("start", start_text, args.start_seconds, False, -20))
        if not jobs:
            print(f"{model}: tarif yok, atlandı")
            continue
        for kind, text, seconds, loop, lufs in jobs:
            t0 = time.time()
            if args.reprocess:
                raw = latest_raw(model, kind)
                if raw is None:
                    print(f"{model}_{kind}: ham kayıt yok, atlandı")
                    continue
            else:
                data = generate(text, seconds, loop, 0.5)
                raw = os.path.join(RAW, f"{model}_{kind}_{int(t0)}.mp3")
                with open(raw, "wb") as f:
                    f.write(data)
            out = os.path.join(OUT, f"{model}_{kind}.ogg")
            to_ogg(raw, out, lufs, loop)
            print(f"{model}_{kind}: {time.time() - t0:.1f} sn -> {os.path.relpath(out, ROOT)}")


if __name__ == "__main__":
    main()
