# ScrollRoad — Yeni Ortamlar (fikir notları)

> 11.10.2026 · Durum: TARTIŞMA — hiçbir şey karara bağlanmadı, kod yok.

## Ana fikir
Oyunun tek mekaniği scroll çevirmek. Araç + yol bu mekaniğin yalnızca bir görünümü.
Aynı scroll başka "dönen / sarılan" şeyleri de sürebilir; her ortamın kendi skinleri olabilir.

## Kullanıcının fikirleri

| # | Ortam | Yukarı scroll | Aşağı scroll (mevcut kural: geri) | İlerleme birimi | Toplanan şey / skin | Görev çubuğu şeridi |
|---|---|---|---|---|---|---|
| 1 | **Oltayı asılan adam** | makara sarar, balık yaklaşır | misina salınır | tutulan balık (kg) | balık türleri (göl/deniz/buz) · balıkçı, olta, tekne skinleri | iskele + su, yatay — iyi |
| 2 | **Dünyayı döndüren el** | dünya döner, gün/gece akar | zaman geri akar | yıl | çağlar / dönemler · el (eldiven, robot, pençe), gezegen skinleri | küçük daire — orta |
| 3 | **AVM döner kapısı** | kapı döner, insanlar girer | insanlar geri çıkar | ziyaretçi | nadir ziyaretçiler (astronot, Noel Baba…) · bina/mağaza skinleri | kaldırım yatay — çok iyi |
| 4 | **Elektrik üreten rüzgârgülü** | pervane döner | ters döner (üretim yok/eksi?) | kWh | aydınlatılan evler → şehir ışıkları · türbin/değirmen skinleri | türbin dizisi — iyi |
| 5 | **Bumerang** | dönüş hızı birikir, fırlatılır | ? | mesafe / vurulan hedef | hedefler · bumerang skinleri | orta — tek sürekli olmayan fikir |
| 6 | **Plak tablası** | plak döner, müzik scroll hızında çalar | scratch / geri sarma | çalınan dakika / şarkı | plak koleksiyonu (türler) · pikap, etiket skinleri | üstten plak — iyi |
| 7 | **Çömlekçi çarkı** | çark döner, hamur uzar/şekillenir | hamur basılır/çöker? | biten çömlek | raf: vazo, testi, amfora · çamur rengi, sır desenleri | dikey nesne — zor |
| 8 | **Meyve soyacağı** | meyve döner, spiral kabuk uzar | ? | soyulan meyve | meyveler (elma → ananas → karpuz), "kopmayan kabuk" rekoru · soyacak skinleri | orta |
| 9 | **Tavuk/et döner, çağ kebabı** | şiş döner, pişer | ? | porsiyon / kg | döner türleri · usta, bıçak, ocak skinleri | çağ kebabı şişi yatay — birebir |

## Kararlar
- **11.10 — Ortam = ayrı ilerleme (seçenek b).** Her ortamın kendi birimi, kendi koleksiyonu ve kendi skinleri olur; yalnız seçili ortam ilerler. Bütün ortamların toplam scroll'u tek bir ortak skora yazılır ve aylık leaderboard bu skordan tutulur. Ortamlar oynayarak açılır (araç merdiveni gibi). İlk prototip için öneri: Olta.
  - Bunun sonucu olarak yolun bölgeleri, pasaportu ve araç kilitleri yalnızca Yol ortamındaki km ile ilerler.

- **11.10 — Ortak skor = arka planda bir puan; ortam birimleri ona uydurulur.** Scroll ve seyir modeli (seyir + boost, rölanti) tek bir gizli puan üretir. Her ortamın birimi bu puandan sabit bir oranla türetilir: Yol için X puan = 1 km, Olta için Y puan = 1 kg balık, vb. Oranlar, aynı çabanın her ortamda benzer hızda ilerleme hissettirmesi için ayarlanır. Mevcut denge kararları (3 dk aktiflik penceresi, rölanti hızı) bu puan katmanında aynen kalır. (Önerdiğim "spins" birimi kullanılmayacak.)

- **11.10 — Leaderboard, bakan oyuncunun o an açık olan ortamının biriminde gösterilir** (Yol'da km, Olta'da kg balık); puanın kendisi hiç görünmez.
- **11.10 — Ortamlar gizli puanın ömür boyu toplamıyla açılır.** Yeni oyuncu Yol ile başlar, Olta ilk birkaç gün içinde açılır, sonrakiler seyrekleşir; eşikler sonra ayarlanacak.

## Olta — içerik (11.10 ONAYLANDI; şamandıra yok, stil spinning/LRF)
- **Stil:** spinning / LRF (sahte yemle at-sar avcılığı). Şamandıra YOK.
- **Sahne:** balıkçı solda (kıyı, kaya, iskele, kayık); sağda su ve suyun altının kesiti görünür; yem ve balık su içinde izlenir.
- **Döngü:** balıkçı kendiliğinden atış yapar → yem uzağa düşer → yukarı scroll makarayı sarar, yem balıkçıya doğru gelir → yolda bir balık yeme vurur → balık sarılarak getirilir, kıyıya varınca tutulur ve koleksiyona girer → yeni atış. Aşağı scroll makarayı geri çevirir: misina boşalır, balık uzaklaşır. Rölantide makara kendiliğinden yavaş sarar.
- **Birim:** kg. Büyük balık daha uzaktan/derinden vurur ve getirmesi daha uzun sürer. Balığın kg'ı sarılan puanla orantılıdır, böylece birim puandan tutarlı biçimde türer.
- **Başarısızlık yok:** misina kopmaz, balık kaçmaz (idle oyun, ceza yok).
- **Mekânlar (bölgelerin karşılığı):** Göl → Nehir → Kıyı → Açık deniz → Buz deliği; her birinin kendi balık seti var, Olta'nın kg toplamıyla açılır. Gizli efsane balık (ör. Altın Sazan) olur; Whisper'ın karşılığı.
- **Koleksiyon = Balık defteri** (pasaportun karşılığı): ilk tutuluşta tür damgalanır ve en büyük kg rekoru tutulur. Yaklaşık 30 tür (5 mekân × 6), gerçek tür adlarıyla.
- **Skinler:** balıkçı kıyafeti, kamış + makara (bambu → karbon → altın), sahte yemler (kaşık, rapala, silikon), tekne. Kurala uygun olarak önce oynanış gelir: ilk prototipte tek balıkçı olur, skinler sonra.
- **Görev çubuğu şeridi:** su çizgisi yatay; balıkçı, misina ve yaklaşan yem/balık.
- **İbre:** makara devri kadranı (Harbor/Airspeed gibi ortama özel skin).
- **Ses:** hıza bağlı makara cırcırı, su ambiyansı, balık çıkınca şıpırtı.

## Prototip planı (11.10 yazıldı; 13.10 gecesi 5 adımın 5'i de uygulandı — ayrıntı ve onay bekleyen kararlar: `Docs/gece_raporu_2026-10-13.md`, commit EDİLMEDİ)
1. **Puan katmanı:** seyir + boost + rölanti `RoadDrive.cs`'den ayrılır, gizli puanı üreten ortak motora taşınır. Yol için km = puan × oran. Mevcut km kaydı puana ve Yol ilerlemesine aktarılır (veri kaybı yok).
2. **Ortam yapısı:** her ortamın birimi, oranı, kart sahnesi, görev çubuğu şeridi ve HUD metinleri olur; mevcut Yol ilk ortam olur (`World/RoadSceneView.cs`, ~1.500 satır).
3. **Birimi aktif ortamdan alanlar:** leaderboard, çekmece başlığı, günlük/haftalık hedefler.
4. **Ortam seçici:** çekmecede; kilitli ortam eşikle görünür.
5. **Olta prototipi:** yalnız Göl, 6 balık, tek balıkçı; at-sar-vur-tut döngüsü, balık defterinin ilk hâli, görev çubuğu şeridi; görseller Gemini ile, ses ve skinler sonra.

1-4 adımlarında Yol'un görünüşü ve davranışı değişmemeli; editör + build'de doğrulanıp öyle 5. adıma geçilir. Başlarken kullanıcı editörde Play modunda olmamalı.

## Tartışılacak sorular
1. **Ortam ne demek?** (a) aynı ilerlemenin farklı görünümü, (b) her ortamın kendi ilerlemesi/koleksiyonu, yalnız aktif olan ilerler, (c) hepsi aynı anda ilerler.
2. **Skor / aylık leaderboard:** tek ortak birim mi (ör. km'yi ortak tutup her ortam kendi birimine çevirir), ortam başına ayrı tablo mu?
3. **Aşağı scroll'un anlamı** her ortamda (mevcut kesin karar: aşağı = yavaşla/geri). Plak, dünya ve kapıda zaten doğal uyuyor; çömlek, soyacak, kebap ve bumerangda açık.
4. **Ortamlar nasıl açılır?** Baştan hepsi açık mı, araç merdiveni gibi oynayarak mı?
5. **Görev çubuğu şeridi (440×48)**: dikey ve dairesel ortamlar şeritte nasıl görünecek?
6. **İbre / kadran**: "devir göstergesi" olarak her ortama genellenebilir mi?
7. **Oyunun adı ve kimliği**: "ScrollRoad" yola odaklı bir ad.
8. **Üretim maliyeti**: her ortam = görsel (Gemini) + ses (ElevenLabs) + animasyon kodu + koleksiyon verisi. Plakta müzik telifi → kendi/telifsiz müzik şart.

## Claude'un ek fikirleri (onaylanmadı)
- Kahve değirmeni (çekirdek → fincan), dönme dolap (yolcular), film makarası (makara döndükçe film oynar), kuyu çıkrığı (kova çıkar), iplik çıkrığı (yün → kazak), pamuk şekeri makinesi.

## 14.10 KARAR — Olta balık sayısıyla (kullanıcı)
- Aktivite başına ayrı puan YOK; gizli ortak puan kalır.
- Olta kg ∝ puan mantığı kaldırıldı: **eşit aralıklı, her 20.000 puanda (Yol'da 20 km eşdeğeri, seyirde ~20 dk) 1 balık** — Yol'daki "durağa varış = damga" gibi. Aralık değerini Claude seçti (hedefler tam sayı: günlük 4, haftalık 25 balık); `EnvironmentCatalog.FishIntervalPoints`.
- Olta kendi ilerlemesiyle ilerler, sayaç **aylık sıfırlanır** (UTC ay); balık defteri kalıcı. kg artık yalnız balığın özelliği (defter/rekor).
- Leaderboard ve hedefler Olta'da **balık sayısı** biriminde (ortak puan / 20.000, aşağı yuvarlanır).
- Balık atışın %75-90'ında vurur. Eski kayıttaki 23 balık deftere sayıldı ama bu ayın sayacı 0'dan başladı.

## 14.10 — Nehir (ikinci mekân) UYGULANDI (commit EDİLMEDİ)
- Kullanıcı "Yeni mekân: Nehir" seçti; ayrıntı kararları Claude verdi (onay bekliyor):
  - **Açılma:** ömür boyu **20 balık** (`FishingSpot.UnlockTotalFish`; aylık sayaç değil). Açılınca bir kez "New spot: The River" tabelası + olta otomatik Nehir'e geçer.
  - **Geçiş:** balık defterinde mekân sekmeleri (The Lake / The River); açık mekânda **FISH HERE** düğmesi. Geçişte sarılan puan korunur, yalnız süren atışın türü yeni mekândan yeniden çekilir.
  - **Türler (6):** Grayling 0,2–1,5 kg · Brown Trout 0,3–3 · Ide 0,4–3 · Asp 1–6 · Barbel 1–7 · Huchen 3–18 (sıklık 28/24/18/14/10/6).
  - **Sahne:** kayalık kıyıda göğüs çizmeli balıkçı (iskelesiz, tek Gemini görseli `fisherman_river.png`), kıyı/dip taşları ve sağdan sola akıntı çizgileri kodla; Pine Forest tonları. Atış mesafesi mekânın en ağır balığına göre.
- Kod: `FishCatalog` (FishingSpot, Spots, All), `FishingController` (Spot, SelectSpot, CheckSpotUnlocks), `FishingSceneView` (Look: mekân başına görünüm), `FishLogController` (sekmeler), `FishingHudController` (tabela mekân adı, defter sayısı mekâna göre), sahne kurucu.
- Doğrulanan: derleme 0 hata, build'de gerçek kayıtla (24 balık) Nehir açılıp otomatik geçti; kart sahnesi gece temasında ekran görüntüsüyle. **Doğrulanmayan:** balık defteri sekmeleri/FISH HERE (oyuna tıklama enjekte edilemedi), gündüz temaları, görev çubuğu şeridi (SAC), Nehir'de balık tutuluşu.

## 14.10 — Kıyı (üçüncü mekân) UYGULANDI (commit EDİLMEDİ)
- Kararlar Claude'un (onay bekliyor): **ömür boyu 50 balıkta** açılır; mendirekten LRF (Ege/Akdeniz).
- Türler: Horse Mackerel 0,05–0,5 kg · Garfish 0,1–1 · Gilthead Bream 0,2–2,5 · Bluefish 0,4–5 · European Seabass 0,5–6 · Leerfish 2–16 (sıklık 28/22/18/14/11/7).
- Sahne: lacivert şapkalı/rüzgârlıklı balıkçı (`fisherman_coast.png`, tek Gemini görseli), kodla beton mendirek (sudan yüksek üst yüzey + dibe dik duvar; şeritte ayak altında tek blok), turkuaz-lacivert deniz, hafif dalga (yüzey salınır, dalga çizgileri yavaşça kıyıya sürüklenir), dipte kayalar, alçak uzak burun.
- Doğrulanan (build, gece teması): olta kaydı geçici olarak 50 balığa çekildi → "New spot: The Coast" duyurusu çıktı, olta otomatik Kıyı'ya geçti, sahne ekran görüntüsüyle; sonra olta kaydı yedekten **bayt bayt geri yüklendi** (`kayit_yedegi_2026-10-14\fishing_kiyi_testi_oncesi.bin`). Ayrıca kullanıcı Nehir'de gerçekten 2 balık tuttu (Nehir tutuluşu çalışıyor).
- Doğrulanmayan: defter sekmeleri (3 sekme + FISH HERE), gündüz temaları, şerit.

## 14.10 — Açık deniz (dördüncü mekân) UYGULANDI (commit EDİLMEDİ)
- Kararlar Claude'un: **ömür boyu 100 balıkta** açılır; kayıktan spinning/jigging.
- Türler: Atlantic Mackerel 0,2–1,5 kg · Atlantic Bonito 1–6 · Little Tunny 1,5–10 · Mahi-mahi 2–18 · Greater Amberjack 4–30 · Bluefin Tuna 20–120 (sıklık 28/22/18/14/11/7).
- Sahne: sarı yağmurluklu balıkçı + ahşap kayık TEK Gemini görseli (`fisherman_boat.png`); kayık dalgayla iner-kalkar ve hafif yalpalar; dip/kara yok, ufukta silik ada, derin lacivert su, dalga çizgileri. Kartta kayık 1,5 birim sağa kaydırıldı (adam sol üst plakanın altında kalıyordu).

## 14.10 — Buz deliği (beşinci mekân) UYGULANDI (commit EDİLMEDİ)
- Kararlar Claude'un: **ömür boyu 180 balıkta** açılır.
- Türler: Yellow Perch 0,1–1 kg · Whitefish 0,3–2,5 · Arctic Char 0,5–5 · Burbot 0,5–6 · Lake Trout 1–15 · Taimen 5–30 (sıklık 28/22/18/14/11/7).
- Sahne: taburede kırmızı bereli balıkçı (`fisherman_ice.png`, kısa buz oltası); buz tabakası + kar şeridi + delik kodla; yem kamış ucundan deliğe DİKEY iner, sarıldıkça dikey yükselir; takılan balık başı yukarı gelir, delikten çıkıp ele uçar. FishingController değişmedi (yalnız FishingSceneView'da dikey yol). Gecede buz soluk maviye çekilir (Land tonu siluete dönüp gökyüzüne karışıyordu).

## 14.10 — Altın Sazan (Göl'ün gizli efsanesi) UYGULANDI (commit EDİLMEDİ)
- `FishingSpot.Legendary`: Göl'de her atışta **%0,4** (ortalama ~250 atışta bir), **ömür boyu 30 balıktan önce çıkmaz**; 6–20 kg.
- Göl'ün 6'lık türlerine girmez → "x / 6" sayımı ve ızgara bozulmaz; defterde tutulana kadar görünmez, tutulunca ızgaranın altında altın şerit ("LEGENDARY · Golden Carp", rekor, sayı, ilk tutuluş). Tutulunca "Legendary: Golden Carp" tabelası.
- Defter 5 mekâna göre: sekmeler 118 px, kilitli mekân yalnız soluk ad (kalan balık sağ üstte), FISH HERE 116 px.
- Doğrulanan (build, gece): 100 balık → Kıyı + Açık deniz duyuruları sırayla, otomatik geçiş; 180 balık → Buz deliği, dikey yol; Göl'de test atışı Altın Sazan'a çevrildi → takıldı, efsane tabelası çıktı, sayım 5/6'da kaldı, LAST Golden Carp 12.34 kg. Her testten sonra olta kaydı yedekten geri yüklendi (`kayit_yedegi_2026-10-14\fishing_aciksular_testi_oncesi.bin`, cmp ile birebir).
- Doğrulanmayan: defter paneli (sekmeler, altın şerit, FISH HERE — tıklama enjekte edilemiyor), gündüz temaları, görev çubuğu şeridi (Açık deniz/Buz).

## 14.10 — Döner kapı (üçüncü ortam) KARAR + prototip (commit EDİLMEDİ)
Kullanıcı Olta'dan sonraki ortam olarak **AVM döner kapısı**nı seçti, içerik önerisini onayladı ("uygun, başla").
- **Sahne:** yandan kesit. Solda kaldırım/şehir, ortada cephe duvarına oturan 4 kanatlı cam döner kapı, sağda AVM'nin içi (vitrinler, asma kat). İnsanlar soldan yürür, kapının bir bölmesine girip içeri geçer.
- **Scroll:** yukarı kapıyı döndürür, insanlar girer. Aşağı kapıyı ters çevirir, son girenler geri çıkar, sayaç da düşer. Rölantide yavaş döner. Başarısızlık yok.
- **Birim:** ziyaretçi sayısı, **200 puanda 1** (kullanıcı seçti). Seyirde ~12 sn'de bir kişi girer, günlük hedef 400, haftalık 2.500. Aylık sıfırlanır, leaderboard ve hedefler bu birimle tutulur.
- **Sahne konumun saf fonksiyonu:** ayın i. ziyaretçisinin kimliği ay + mekân + sıradan belirlenir (`VisitorCatalog.Identify`). Geri çıkıp yeniden giren aynı kişidir ve ayın rekorunu aşmadıkça iki kez sayılmaz.
- **Misafir defteri (Guest book):** sıradan yürüyenler (8 tip) deftere girmez. Ziyaretçi başına %1,2 ihtimalle nadir misafir gelir, ilk gelişte damgalanır. AVM misafirleri: Shopping Spree, Teddy Mascot, The Mime, Santa Claus, A Knight, An Astronaut (en nadiri ~1.700 ziyaretçide bir). Ayın ilk 12 ziyaretçisi hep sıradan.
- **Mekânlar:** AVM → Otel → Havalimanı → Müze → Gökdelen (her biri 6 misafir, ömür boyu ziyaretçi sayısıyla açılır, bir de gizli efsane). Prototipte yalnız AVM var.
- **Açılma:** ömür boyu 500.000 puan (Yol'da 500 km).
- **Görseller:** `Tools/gen_visitors.py` (gen_fishing.py boru hattı) → `Assets/Art/Door/`. Maskotun ilk tarifi Gemini'nin içerik filtresine takıldı, "mall mascot character ... bear suit" tarifiyle üretildi.
- **Kod:** `Assets/Scripts/Door/` (VisitorCatalog, DoorController, DoorSceneView, DoorHudController, GuestBookController). Activities seçicisi 3 kart, çekmecedeki koleksiyon satırı kapıdayken misafir defterini açar, şerit ve tepsi ipucu ziyaretçi sayısını yazar.
- Sırada (kullanıcı onayından sonra): kapı sesi, Otel/Havalimanı/Müze/Gökdelen, gizli efsane misafir.

## 14.10 — Döner kapı: kalan 4 mekân + gizli efsane UYGULANDI
Kullanıcı "kalan 4 mekân"ı seçti (toplu). Kararlarım (onay bekliyor):
- **Açılma (ömür boyu ziyaretçi):** Otel 2.000 · Havalimanı 5.000 · Müze 10.000 · Gökdelen 18.000 — Olta'nın 20/50/100/180 balığının puan karşılığı (200 puan/ziyaretçi). Açılınca duyurulur, kapı oraya geçer.
- **Misafirler** (sıklık 30/24/18/14/9/5, nadir çıkma %1,2 hepsinde aynı):
  - Otel: The Bellhop, A Bride, A Rock Star, The Magician, A Film Star, The King
  - Havalimanı: The Pilot, Flight Attendant, A Backpacker, A Surfer, A Mountaineer, A Deep-sea Diver
  - Müze: The Painter, Tour Guide, An Archaeologist, A Ballerina, A Centurion, The Mummy
  - Gökdelen: Window Cleaner, The CEO, A Firefighter, A Superhero, A Robot, An Alien
- **Gizli efsane: Bigfoot (Koca Ayak), AVM'de** (Altın Sazan'ın Göl'de olması gibi): ziyaretçi başına 1/5.000 (seyirde ~17 sa), ayın ilk 600 ziyaretçisinde çıkmaz. Kimlik saf fonksiyon kalsın diye ömür boyu sayıya değil ayın sırasına bağlandı. "x / 6"ya girmez, defterde gelene kadar görünmez, gelince altın şerit + "Legendary" duyurusu.
- **Sahne (VenueLook):** yerleşim aynı, renkler ve 3 iç bölme mekâna göre: Otel resepsiyon + oda kapıları + avize + kırmızı halı; Havalimanı check-in bankoları + asılı tabelalar + üst katta büyük cam; Müze çerçeveli tablolar + etiket + spot + parke; Gökdelen asansörler + kat göstergesi + cam cephe, granit zemin.
- Görseller `Tools/gen_visitors.py` (mavi anahtar renk yüzünden mavi kostümler bordo/mor/gri/turuncuya çevrildi; Koca Ayak'a referanstaki sürücü de çiziliyordu → ayrı tarif).

## 14.10 — Döner kapı mekânlarının görsel doğrulaması (ajan)
Build'de (Builds/Windows) kapı kaydı geçici olarak her mekâna çekilip (ömür boyu 18.000 ziyaretçi, mekân duyuruları yapılmış sayıldı, kaldırımda bir misafir yaklaşırken) kart ve görev çubuğu şeridi ekran görüntüsüyle görüldü; kullanıcının kaydı her testten sonra yedekten birebir geri yüklendi (`kayit_yedegi_2026-10-14\paketler_oncesi_build.reg`, `reg export` karşılaştırması aynı).
- **Doğrulanan:** Otel (resepsiyon + oda kapıları + avize + kırmızı halı), Havalimanı (check-in bankoları + asılı tabelalar + üst kat cam), Müze (çerçeveli tablolar + etiket + spot + parke), Gökdelen (asansörler + kehribar kat göstergesi + granit) — Classic temada dördü, Gece temada Otel + Gökdelen. **Bigfoot** AVM'de kaldırımda (ayın 7.548. ziyaretçisi, "Here comes Bigfoot" tabelası). Şerit: beş mekân saydam ve doğru renklerde (SAC bu build'de LibUniWinC'yi engellemedi). **Misafir defteri** ilk kez gerçek tıklamayla açıldı: sekmeler, kilitli/açık mekân, GO HERE / YOU ARE HERE, silüet/???, altın LEGENDARY · Bigfoot şeridi çalışıyor.
- **Düzeltilen hata:** sol üstteki sayaç 4+ hanede plakadan taşıyordu ("7,546 visitors"). Olta ve Kapı HUD'unda sayaç metni artık sığmazsa küçülüyor (`FitToWidth`, 22→en az 13 pt); düzeltme build'de ekran görüntüsüyle doğrulandı.
- **Gözlem (değiştirilmedi, karar sizin):** şeritte yürüyenler soldaki "85 visitors" yazısının arkasından geçiyor, yazı okunuyor ama kalabalıkta karışık görünebilir. Çekmecedeki MONTH BEST (ortak puanın ziyaretçi karşılığı) kartın sayacından farklı — bilinen tasarım sonucu.
- **Doğrulanmayan:** Bigfoot'un içeri girince çıkan "Legendary" duyurusu (yalnız defterde kayıtlı hâli görüldü), Sepya teması.
- Ekran görüntüleri ajan oturumunun scratchpad'inde (`shots/p1/`).

## 14.10 — Sesler: Döner kapı ve Olta (ajan; kararlar onay bekliyor)
Araç seslerinin yaklaşımı aynen: `Tools/gen_sound.py` (ElevenLabs Sound Effects v2, örnek düzeyinde işleme) → yeni `--env` kipi → `Assets/Resources/EnvSounds/<ad>.ogg`; oyunda `EnvironmentSound.cs` `Resources.Load` ile yükler. Ses yalnız seçili ortamda ve yalnız kart modunda çalar, Ayarlar'daki ana sese uyar (ikisi de mevcut `SoundController`'ın dinleyici sesiyle — yeni ayar yok).
- **Seviye:** ortam sesleri araçlardan ~4 dB kısık (döngü -27 LUFS, tek seferlik -24 LUFS) ve kodda ayrıca 0,3-0,4 kazançla çalınır — saatlerce arka planda açık kalacağı için.
- **Döner kapı — `door_sweep`:** sürekli döngü yerine **her kanat geçişinde bir yumuşak süpürme** (4 kanat, ziyaretçi başına yarım tur = 2 geçiş). Böylece sıklık dönüş hızının ta kendisi: seyirde ~6 sn'de bir, tam boost'ta ~1,6 sn'de bir, rölantide ~36 sn'de bir; şiddet ve perde (0,85→1,25) de hıza göre, geri dönüşte de çalar. (İlk üretilen döngü 9,5 sn'de tek "vuuş" içeriyordu, sürekli dönüş gibi duyulmayacaktı → silindi.)
- **Olta:** `reel_loop` makara sarma döngüsü — perde 0,8→1,45 scroll hızına göre, misina boşalırken %15 kalın; **yalnız kullanıcı aktifken** (son scroll'dan 3 dk) çalar, rölantide makara sessiz döner (saatlerce tıkırtı olmasın). `fish_cast` (kamış vuuşu + misina + uzakta yem şıpırtısı), `fish_strike` (fren cızırtısı + küçük şıpırtı), `fish_catch` (balık sudan çıkar, damlalar) aşama geçişlerinde, rölantide de.
- **İşaret sesleri (prosedürel, telifsiz):** `guest_chime` kapıdan nadir misafir girince iki notalı yumuşak çan (E6→C6); `legend_chime` efsane gelince (Koca Ayak, Altın Sazan) yükselen dört nota (C6 E6 G6 C7). Yeni balık türüne ses eklenmedi (istenmedi).
- **Doğrulanan:** derleme 0 hata, batchmode sahne + build başarılı, dosyaların süre/LUFS ölçümü, spektrogramla içerik kontrolü (müzik/ses yok, kapı süpürmesi tek olay).
- **Doğrulanmayan (bu commit anında):** sesler kulakla dinlenmedi; build'de çalma testi kullanıcı LoL maçındayken yapılamadı (aşağıda ayrı not düşülecek). Makara döngüsü build'de test edilemez (enjekte scroll sayılmıyor, rölantide bilerek sessiz).

## 14.10 — Rüzgârgülü (dördüncü ortam) — İÇERİK (ajan; kullanıcının fikri "elektrik üreten rüzgârgülü"; bütün ayrıntı kararları onay bekliyor)
Olta ve Döner kapı ile aynı mimari: ayrı ilerleme, gizli ortak puandan birim, aylık sıfırlanan sayaç, leaderboard/hedef birimi bu ortamın birimi, ömür boyu puanla açılma, Activities'te kart, koleksiyon defteri, şerit görünümü, başarısızlık yok.
- **Ad / tarif (oyunda):** "Wind turbine" — "Spin the blades, power the valley."
- **Sahne (yandan kesit):** solda bir tepenin üstünde büyük beyaz rüzgâr türbini (kule + gövde + 3 pervane; kodla çizilir, pervane döner), arkada uzak tepelerde iki küçük türbin (aynı rüzgârla döner). Türbinin dibinden sağa bir enerji hattı iner, hattın ucunda bir **bağlantı direği** (trafo) durur. Vadideki yapılar sağdan sola dizili: direğin sağındakiler henüz elektriksiz (koyu silüet), direğin önünden geçen yapı **bağlanır** (kıvılcım + renkleri açılır, pencereleri yanar), solda ışıklı köy olarak türbinin tepesinin arkasında kaybolur. Kamera vadi boyunca hattı izliyor gibi okunur (Döner kapıdaki "ziyaretçi kapıdan geçer" modelinin aynısı). Hattın üzerinde sarı enerji darbeleri pervane hızıyla akar.
- **Scroll:** yukarı → pervane hızlanır, yapılar bağlanır. **Aşağı** → pervane yavaşlar/ters döner, son bağlanan yapılar yeniden kararıp sağa geri çekilir, sayaç düşer (ay rekoru aşılmadan aynı yapı iki kez sayılmaz). Rölantide hafif esinti: pervane yavaş döner. Başarısızlık yok.
- **Birim:** **kWh** (tam sayı). **1 kWh = 500 puan** → seyirde ~30 sn'de 1 kWh (saatte ~120), rölantide ~3 dk'da 1. Günlük hedef 80.000 puan = **160 kWh**, haftalık 500.000 = **1.000 kWh**. Aylık sıfırlanır; leaderboard ve hedefler kWh ile.
- **Kimlik saf fonksiyon:** ayın i. kWh'ı = vadideki i. yapı; kimliği ay + mekân + sıradan (`GridCatalog.Identify`, kapıdaki hash'in aynısı).
- **Yapılar:** sıradan evler (6 tip: taş kulübe, çiftlik evi, kırmızı ahır, dar şehir evi, bungalov, köşe dükkânı) deftere girmez. **Nadir yapı** her kWh'da **%3** (kapıdaki %1,2'den yüksek, çünkü 1 kWh = 2,5 ziyaretçilik puan; gerçek zamanda benzer sıklık: seyirde ~17 dk'da bir), ayın ilk 12 kWh'ı hep sıradan.
- **Koleksiyon = Şebeke defteri ("Grid log")**: ilk bağlanışta damgalanır, kaç kez bağlandığı + ilk tarih. **Tepeler (The Hills)** mekânının 6 nadir yapısı (sıklık 30/24/18/14/9/5): The Bakery, The Dairy Farm, The Village School, The Cinema, The Fairground (dönme dolap), The Observatory (en nadiri seyirde ~9 sa).
- **Mekânlar (sonraki aşama):** Tepeler → Kıyı (deniz feneri, liman…) → Açık deniz (yüzer türbin; petrol platformu, gemi…) → Çöl (güneş + rüzgâr; vaha, kervansaray…) → Kar (araştırma istasyonu…) + gizli efsane yapı (öneri: şarj olmaya inen uçan daire). **Prototipte yalnız Tepeler**, efsane yok.
- **Açılma:** ömür boyu **1.000.000 puan** (Yol'da 1.000 km; Kapı 500.000'di). Kullanıcının kaydı ~533.000 → bugün KİLİTLİ, mevcut temposuyla birkaç günde açılır.
- **Görev çubuğu şeridi:** saydam zemin, ince çim şeridi; solda bu ayın kWh'ı + durum, şeridin ~%32'sinde küçük türbin (pervane şerit yüksekliğine sığar), sağında hat + direk + kayan yapılar.
- **Kart HUD:** sol üst plaka GENERATED "123 kWh" + GENERATING / IDLE · LIGHT BREEZE / BRAKING; sağ üst yeşil tabela THE HILLS + "Powering <nadir yapı>" (5 kWh içinde geliyorsa) ya da "Next: home #124"; sol alt GRID LOG x/6 · LAST …; sağ alt bugünkü hedef (ortak).
- **Ses:** prototipte yok (Olta'daki sıra: önce oynanış). Sonraki adım önerisi: rüzgâr uğultusu + pervane geçiş "vuuu"su (kapı süpürmesi gibi kanat geçişine bağlı), bağlanışta küçük "klik".
- **Görseller:** `Tools/gen_wind.py` (gen_fishing boru hattı, mavi anahtar) → `Assets/Art/Wind/` (6 ev + 6 nadir + Activities kartı için türbin resmi); türbin oyunda kodla.


## 14.10 — Rüzgârgülü prototipi (Tepeler) UYGULANDI (ajan)
- Kod: `Assets/Scripts/Wind/` — `GridCatalog` (yapılar, Tepeler, `Identify`, `WindProgress`), `WindController` (aylık konum, ay rekoru, bağlanma, defter, "Powered up" duyurusu), `WindSceneView` (tepe + türbin + uzak türbinler + hat + darbeler + direk + kayan yapılar + kıvılcım + rüzgâr çizgileri; Gece'de bağlı yapıların arkasında sıcak hale), `WindHudController`, `GridLogController` (şebeke defteri, 6 yuva, sekmesiz). Ortak yerler: `EnvironmentCatalog` (Wind, `KwhIntervalPoints` = 500, açılma 1.000.000), `SaveService` (`ScrollRoad_Wind` JSON), şerit (`CompanionHudController.UpdateWind`), tepsi ipucu, çekmecedeki koleksiyon satırı ("Grid log"), sahne kurucu (Activities 4. kart `turbine_card.png`; dar kartlarda ad/tarif küçülüyor). `VisitorCatalog.Fnv/Hash01` internal yapıldı (aynı hash).
- Görseller (Gemini, `Tools/gen_wind.py`, hepsi ilk denemede kullanıldı, reddedilen yok): 6 ev + 6 nadir yapı + Activities kartı türbini → `Assets/Art/Wind/`, ham kaynaklar `Tools/art_source/raw/wind_*.png`.
- **Doğrulanan:** Unity dışı derleme 0 hata, batchmode sahne kurulumu + build başarılı (143 MB), build'in Wind kodunu içerdiği.
- **Doğrulanmayan (bu commit anında):** ekranda hiç görülmedi — kullanıcı LoL maçındaydı, pencere açılmadı. Ses yok (bilerek).
## 14.10 — Plak tablası (beşinci ortam) — İÇERİK (ajan; kullanıcının fikri "müzik scroll'a göre"; bütün ayrıntı kararları onay bekliyor)
Aynı mimari: ayrı ilerleme, gizli ortak puandan birim, aylık sayaç, leaderboard/hedef birimi, ömür boyu puanla açılma, Activities kartı, koleksiyon, şerit, başarısızlık yok.
- **Ad / tarif (oyunda):** "Record player" — "Spin the record, the music follows your scroll."
- **Sahne (kart):** üstten bakış. Solda ahşap pikap: tabla, dönen siyah plak (oluklar, ortada renkli etiket — dönüş etiketin deseninden okunur), sağ üstte ton kolu; iğne plakta dıştan içe ilerler. Sağda plak sandığı: bu ay çalınan son plakların kapakları sırt sırta dizilir, en öndeki kapak görünür. Notalar hoparlörden yukarı süzülür (müzik çalarken).
- **Scroll:** yukarı → plak hızlanır, **müziğin temposu scroll hızını izler** (seyirde normal tempo, boost'ta %160'a kadar hızlı, rölantide %40 yavaş ve kısık). Gerçek pikap hissi için perde de hafifçe (±%8) kayar. **Aşağı** → plak ters döner, iğne geri gider (parça ilerlemesi ve sayaç düşer), müzik susar, tek bir "scratch" sesi (mevcut `scratch_001`). Başarısızlık yok.
- **Birim:** **parça (tracks)**. **1 parça = 4.000 puan** → seyirde ~4 dk (gerçek bir şarkı uzunluğu), rölantide ~24 dk. Günlük hedef 80.000 puan = **20 parça**, haftalık 500.000 = **125 parça**. Aylık sıfırlanır; leaderboard ve hedefler parça ile.
- **Plaklar:** her plak (EP) **4 parça**; 4 parça bitince plak sandığa girer, yenisi takılır (seyirde ~16 dk'da bir plak). Ayın i. plağının kimliği ay + mekân + sıradan (kapıdaki hash). Sıradan plaklar deftere girmez (düz renkli kapaklar, kodla).
- **Koleksiyon = Plak sandığı ("Record crate")**: nadir plak **plak başına %30** (seyirde ~53 dk'da bir), ilk çalınışta sandığa damgalanır, kaç kez çalındığı + ilk tarih. İlk mekân **The Record Shop**'un 6 nadir plağı (sıklık 30/24/18/14/9/5) — kurgusal adlar, gerçek sanatçı/albüm yok, her biri kendi tarzında çalar: **Rainy Window** (lo-fi), **Praia** (bossa nova), **Night Drive** (synthwave), **Blue Smoke** (caz), **Mirror Ball** (disko), **Golden Groove** (funk; en nadiri, seyirde ~18 sa).
- **Müzik — telifsiz, PROSEDÜREL (karar):** hiçbir kayıt/örnek kullanılmaz; müzik oyun içinde gerçek zamanlı sentezlenir (`VinylMusic.cs`, `OnAudioFilterRead`): akor dizisi + bas + basit davul (kick/snare/hi-hat gürültüden) + arpej/melodi, hepsi sinüs/üçgen dalga ve gürültüden; tarz (tempo, swing, akor kümesi, davul kalıbı, enstrüman tınısı) plağın türünden, akorlar ve melodi o parçanın tohumundan (ay + sıra) gelir — her parça farklı ama aynı parça geri sarılıp tekrar çalınınca aynısı. Üstüne hafif plak cızırtısı ve yumuşak alçak geçiren filtre; sevye düşük (saatlerce arka planda). Neden: telif riski sıfır, dosya boyutu sıfır, tempo perdeden bağımsız değiştirilebilir (scroll'a bağlamak için şart). Bedeli: "gerçek şarkı" kalitesinde değil, sade bir lo-fi/oda müziği.
- **Ses kuralları:** yalnız kart modunda ve yalnız bu ortam seçiliyken (SoundController'ın dinleyici sesi + ana ses ayarı aynen geçerli).
- **Açılma:** ömür boyu **1.500.000 puan** (Yol'da 1.500 km; Rüzgârgülü'nden sonra).
- **Görev çubuğu şeridi:** saydam zemin; solda bu ayın parça sayısı + durum, şeridin ortasında üstten küçük plak (dönen etiket) + ton kolu, sağında birkaç kapak.
- **Kart HUD:** sol üst PLAYED "12 tracks" + PLAYING / IDLE · SLOW SPIN / REWINDING; sağ üst tabela THE RECORD SHOP + çalan plak adı ve "Track 2 / 4"; sol alt RECORD CRATE x/6 · LAST …
- **Mekânlar (sonraki aşama):** Record Shop → Jazz Club → Disco → Festival → Radio Station + gizli efsane plak. Prototipte yalnız Record Shop.
- **Görseller:** `Tools/gen_vinyl.py` → `Assets/Art/Vinyl/` (6 nadir plak kapağı — yazısız, + Activities kartı için pikap resmi); pikap ve plak kodla.

## 14.10 — Plak tablası prototipi (Record Shop) UYGULANDI (ajan)
- Kod: `Assets/Scripts/Vinyl/` — `RecordCatalog` (tarzlar, 6 nadir plak, `Identify`, parça tohumu, `VinylProgress`), `VinylController` (aylık konum, ay rekoru, parça/plak bitişi, sandık, "New record" duyurusu), `VinylSceneView` (üstten pikap: dönen plak + oluklar + etiket, ton kolu iğnesi plağın ilerlemesiyle dıştan içe, masada kapak yığını, notalar), `VinylMusic` (prosedürel müzik), `VinylHudController`, `CrateLogController`. Ortak yerler Rüzgârgülü ile aynı (katalog: `TrackIntervalPoints` = 4.000, açılma 1.500.000; `ScrollRoad_Vinyl` JSON; şerit; tepsi ipucu; "Record crate" satırı; Activities 5. kart).
- **Müzik (karar, telifsiz):** tamamen prosedürel sentez, kayıt/örnek yok. 6 tarz (lo-fi 80 bpm swing, bossa 126, synthwave 100 arpej+ped, caz 132 swing walking bas, disko 116 four-on-the-floor, funk 100), her parçada tohumdan akor dizisi (4 ölçü), ton ve pentatonik melodi. Tempo scroll'u izler (rölanti %40 → boost %160), perde ±%8. Ses yüksekliği çevrimdışı çizimle ölçülüp her tarz **~-30 LUFS**'a dengelendi (araç döngülerinin oyundaki etkin düzeyi). CPU: tek çekirdeğin ~%5-7'si (yalnız Plak tablası seçiliyken; başka ortamda sentez durur).
- **Bulunan ve düzeltilen hata:** `OnAudioFilterRead` ile üretilen sese `AudioListener.volume` uygulanmıyor — müzik ana sesi yok sayıyor ve görev çubuğu şeridinde de çalıyordu (build'de ölçüldü). Artık dinleyici sesi elle çarpılıyor; düzeltmeden sonra kartta ana sese uygun (tepe 0,0076 ≈ beklenen), şeritte sessiz.
- **Rüzgârgülü düzeltmeleri (bu commit'te):** şeritte küçük türbin kayan yapıların arkasında kalıyordu → şeritte türbin önde, yapılar türbine varmadan söner; elektriksiz yapılar biraz daha koyu; gecede rüzgâr çizgileri tel gibi duruyordu → daha silik; şebeke defteri (ve plak sandığı) ızgarasının 2. satırı kartın altından taşıyordu → ızgara yukarı alındı.
- Görseller (Gemini, `Tools/gen_vinyl.py`): 6 kapak + Activities kartı pikabı → `Assets/Art/Vinyl/`. İlk turda 4 kapak, ortak istemdeki "düz mavi zemin" talimatı yüzünden mavi çerçeveli çıktı (biri "12" yazılı) → kapaklar için ayrı istem (kenardan kenara, yazısız, 1:1) yazılıp yeniden üretildi, Blue Smoke bir kez daha (beyaz kenar); reddedilenler silindi, yalnız kullanılanlar kaldı.

### 14.10 — doğrulama (build, kullanıcının kaydı geçici değiştirilip birebir geri yüklendi)
- **Paket 2 sesleri — build'de, yalnız ScrollRoad sürecinin ses oturumu tepe ölçeriyle (pycaw):** kapı: Otel'de #87 nadir misafir girerken çan + süpürme; kullanıcı o sırada Chrome'da gerçekten scroll yapınca süpürmeler ~1,75 sn arayla sıklaştı (hıza bağlılık). Olta: Altın Sazan tutulurken şıpırtı + efsane çanı, 2,3 sn sonra yeni atış (vuuş + şıpırtı); balık vurma sesi. Kulakla dinlenmedi; makara döngüsü ölçülemedi (enjekte scroll sayılmıyor, rölantide bilerek sessiz).
- **Rüzgârgülü — ekran görüntüleriyle:** kart (Classic + Gece), pervane dönüyor, direğin sağındaki yapılar karanlık, "Powering The Cinema" tabelası, "New activity" duyurusu, şebeke defteri (silüetler), şerit (düzeltmeden sonra türbin önde), Activities'te 5 kart.
- **Plak tablası — ekran görüntüleri + ses ölçümü:** kart (Classic + Gece), plak dönüyor (etiket açısı değişiyor), ton kolu plağın %35'inde, yığında nadir kapak (Blue Smoke), "Track 2/4 · Rainy Window" tabelası, plak sandığı, şerit; müzik build'de sürekli çalıyor.
- **Doğrulanmayan:** müzik kulakla dinlenmedi (tarzların kulağa nasıl geldiği bilinmiyor — yalnız ses düzeyi, ritim yapısı spektrogramda ve CPU ölçüldü); gerçek tekerlekle tempo değişimi ve aşağı scroll'daki "scratch" (enjekte scroll sayılmıyor); yeni yapı/plak damgalanma anı ve duyuruları; Sepya teması.

## 14.10 — Kalan 5 ortam: ortak altyapı (ajan; kullanıcı "yeni ortamların hepsini bi ajan çalıştırıp yap" dedi)
Dünya, bumerang, çömlek, soyacak ve kebap Rüzgârgülü/Plak'ın kalıbıyla, ama kod tekrarı olmasın diye ortak bir altyapı üzerinde:
- `Assets/Scripts/Environments/Interval/` — `IntervalEnvironment` (aylık konum, ay rekoru, öğe bitişi, defter, nadir duyurusu; WindController'ın genellemesi), `IntervalCatalog` (`ItemType`, `ItemSite`, `Identify` = kapının hash'i, `IntervalProgress` JSON), `IntervalHudController` (sol üst sayı + durum, sağ üst tabela: sıradaki nadirse "Rising/Aiming at…", 5 öğe içinde nadir varsa "Coming up", yoksa ortamın kendi satırı; sol alt defter özeti ya da rekor), `IntervalLogController` (6 yuvalı defter, sekmesiz), `IntervalSceneView` (sahne iskeleti, kart/şerit kamerası), `IntervalSound` (çevrim ipuçları + yalnız aktifken çalan döngü + nadirde `guest_chime`).
- Ortak yerler bir kez genellendi: şerit (`CompanionHudController.UpdateInterval`), tepsi ipucu, koleksiyon satırı (`PassportController.intervalLogs`), `SaveService.LoadJson/SaveJson`, sahne kurucu (`IntervalSetup` listesi: HUD + defter paneli + Activities kartı).
- **Activities seçicisi:** 5'ten fazla ortamda iki sıra küçük kart (tarif yerine yalnız "Counted in …"; kilitliyken durum + ilerleme çubuğu aynen). **Onay bekliyor.**
- **Açılma merdiveni (ajan kararı, onay bekliyor):** Plak 1.500.000'den sonra 500.000'er: Dünya 2.000.000 · Bumerang 2.500.000 · Çömlek 3.000.000 · Soyacak 3.500.000 · Kebap 4.000.000 (Yol'da 2.000…4.000 km).
- **Nadir sıklığı (ajan kararı):** her ortamda gerçek zamanda Kapı/Rüzgârgülü'ne yakın — ~16.700 puanda bir (seyirde ~17 dk); nadirlerin göreli sıklığı 30/24/18/14/9/5, ayın ilk 12 öğesi hep sıradan. Sıradanlar deftere girmez.

## 14.10 — Dünyayı döndüren el (altıncı ortam) — İÇERİK (ajan; kullanıcının fikri; bütün ayrıntı kararları onay bekliyor)
- **Ad / tarif (oyunda):** "Turn the world" — "Spin the globe, let the years roll by."
- **Sahne (kart):** koyu mavi uzayda küçük bir gezegen (okyanus + kodla üretilen kıtalar, yıldızlar), solda Güneş; gezegenin sağ yarısı gölgede, yüzeydeki yapılar dönerken gündüzden geceye geçer (gün/gece akışı). Gezegenin kenarında 12 yuva: her yıl bir yuvada yeni bir yapı yükselir, 12 yıl önceki yapı o yuvadan toprağa iner. Sağda bir el (tek karakter), işaret parmağıyla gezegenin kenarını tur başına bir kez yukarı iter (gezegen saat yönünün tersine döner).
- **Scroll:** yukarı → gezegen hızlanır, yıllar akar. **Aşağı → zaman geri akar:** gezegen ters döner, el aşağı iter, bu yıl yükselen yapı yeniden toprağa iner, sayaç düşer (ay rekoru aşılmadan aynı yıl iki kez sayılmaz). Rölantide yavaş döner ("IDLE · SLOW DAYS"). Başarısızlık yok.
- **Birim:** **yıl (years)**, **1 yıl = 1.000 puan** → seyirde dakikada bir yıl, rölantide ~6 dk'da bir. Günlük hedef 80.000 puan = **80 yıl**, haftalık 500.000 = **500 yıl**. Aylık sıfırlanır; leaderboard ve hedefler yıl ile. Gezegen puan başına 2,7° döner (seyirde ~8 sn'de bir tur, boost'ta ~2 sn, rölantide ~48 sn).
- **Mekân = çağ:** prototipte yalnız **İlk Çağ (The Ancient World)**. Sonraki aşama: Orta Çağ → Rönesans → Sanayi Çağı → Uzay Çağı (çağlar/dönemler koleksiyonu böylece mekân sırası olur) + gizli efsane.
- **Koleksiyon = Kronik ("Chronicle"):** sıradan yıllarda kulübe, çadır, zeytin ağacı, taş ev, saman yığını, gözetleme kulesi (deftere girmez). **Nadir yıl %6** (seyirde ~17 dk'da bir) o çağın bir harikasını yükseltir, ilk yükselişte kroniğe damgalanır ("Wonder raised: …"): **Stonehenge, The Pyramids, The Great Wall, The Colosseum, The Great Lighthouse, The Hanging Gardens** (en nadiri seyirde ~9 sa).
- **Görev çubuğu şeridi (yatay yorum):** gezegenin ekvatoru açılmış bir yer çizgisi olarak; yapılar sağ uçta (elin olduğu yer, gece) doğar, sola akarken gündüze çıkar, soldaki sayının altında söner. El sağda parmağıyla zemini sola iter. Çizginin rengi gün/gece bandını gösterir.
- **Kart HUD:** ELAPSED "37 years" + TURNING / IDLE · SLOW DAYS / TIME REWINDS; tabela THE ANCIENT WORLD + "Next: year 38" ya da "Rising · The Pyramids"; sol alt CHRONICLE x / 6 · LAST …
- **Ses (ElevenLabs, `gen_sound.py --env`):** `globe_push` (avuç dünyayı iterken yumuşak sürtünme + vuuş; tur başına bir, seyirde ~8 sn'de bir, geri sararken de), `year_page` (yıl dönünce tek sayfa çevirme, seyirde dakikada bir, çok kısık). Nadir yılda mevcut `guest_chime`.
- **Görseller:** `Tools/gen_globe.py` (gen_fishing boru hattı, mavi anahtar) → `Assets/Art/Globe/` (6 sıradan + 6 harika + el + Activities kartı); gezegen, Güneş, yıldızlar kodla. El için "sürücüyü çizme" kalıbı (CREATURE). Hepsi ilk denemede kullanıldı, reddedilen yok.

## 14.10 — Dünyayı döndüren el prototipi UYGULANDI (ajan)
- Kod: `Assets/Scripts/Globe/` — `GlobeEnvironment` (veri, metinler, ses ipuçları), `GlobeSceneView` (gezegen + kıta dokusu + gece gölgesi + 12 yuva + el + açılmış ekvator şeridi).
- **Doğrulama (editör, Play, inceleme modu; editör kaydı önce yedeklendi, sonra birebir geri yüklendi — `kayit_yedegi_2026-10-14\editor_yeni_ortamlar_oncesi.reg`, sha256 aynı):** geçici bir sürücüyle kamera RenderTexture'a çizildi (arayüz dahil): kart Classic + Gece, nadir yükselirken ("Rising · The Great Wall"), şerit Classic + Gece (ekran boyutu 440×48 oranında), kronik paneli (6 harika dolu hâli), Activities (iki sıra). Güneş halesi fazla opaktı → küçültüldü; şeritte el ≡ düğmesinin altına giriyordu → sola alındı.
- **Doğrulanmayan:** gerçek scroll ile dönüş/geri sarma hissi (enjekte scroll sayılmıyor), "Wonder raised" duyurusunun çıkış anı, seslerin kulakla dinlenmesi (yalnız süre/LUFS/spektrogram), build'de görev çubuğu şeridi.

## 14.10 — Bumerang (yedinci ortam) — İÇERİK (ajan; kullanıcının fikri; bütün ayrıntı kararları onay bekliyor)
- **Ad / tarif (oyunda):** "Boomerang" — "Spin it up, let it fly, hit the target."
- **Sahne (kart):** yandan kesit, Avustralya çölü (kızıl toprak, uzakta yassı kaya tepesi, çalılar). Solda atıcı (tek karakter, şapkalı, eli boş; Gemini), bumerang kodla çizilir. Sağda hedef, mesafesine göre yakında ya da uzakta durur.
- **Bir atış (öğenin ilerlemesi):** %0–15 bumerang önceki hedeften aşağıdan kıvrılarak ele döner (yakalama) → %15–70 elde döndürülerek **dönüş hızı birikir** (atıcı geriye yaslanır, bumerang gittikçe hızlı döner) → %70'te fırlatılır (öne eğilir) → yay çizerek uçar → %100'de hedefi vurur: sayılır, hedef arkaya devrilip söner, kıvılcım saçılır. Sıradaki hedef dönüş sırasında belirir. "Tek sürekli olmayan fikir" böylece sürekli bir çevrime oturdu.
- **Aşağı scroll (tablodaki "?" — karar): atışı geri sarar.** Bumerang aynı yoldan tersine gider, son vurulan hedef yeniden ayağa kalkar, sayaç düşer (ay rekoru aşılmadan aynı hedef iki kez sayılmaz). Rölantide yavaş atışlar ("IDLE · LAZY THROWS"). Başarısızlık yok: her atış vurur.
- **Birim:** **vurulan hedef (targets)**, **1 hedef = 250 puan** → seyirde ~15 sn'de bir atış, tam boost'ta ~4 sn, rölantide ~1,5 dk. Günlük hedef 80.000 puan = **320 hedef**, haftalık 500.000 = **2.000 hedef**. Aylık sıfırlanır; leaderboard ve hedefler bu birimle.
- **Mesafe (tablodaki "mesafe / vurulan hedef" — karar):** birim hedef sayısı, mesafe hedefin özelliği (balığın kg'ı gibi): sıradan hedefler 22–58 m, nadirler 45–90 m; tabelada "Next: target 38 · 35 m", ömür boyu **en uzun vuruş rekoru** ("LONGEST HIT 88 m") ve her nadir hedefin en uzak vuruşu deftere yazılır.
- **Mekân:** prototipte yalnız **The Outback**. Sıradan hedefler (deftere girmez): saman hedef, teneke kutular, direkte şişe, kütükte bal kabağı, direkte toprak çömlek, tahta nişan levhası. **Nadir hedef %1,5** (seyirde ~17 dk'da bir), ilk vuruşta **hedef defterine ("Target log")** damgalanır: **The Brass Bell, The Red Kite, The Weather Vane, The Gong, The Paper Lantern, The Golden Trophy** (en nadiri seyirde ~9 sa). Tabela nadir hedefe nişan alırken "Aiming at · The Gong".
- **Görev çubuğu şeridi (yatay, doğal):** ince kızıl zemin çizgisi, atıcı solda yazının yanında (%36), hedef sağda mesafeye göre, bumerang aradaki alçak yayda uçar.
- **Kart HUD:** TARGETS HIT "37 targets" + THROWING / IDLE · LAZY THROWS / REWINDING; tabela THE OUTBACK + "Next: target 38 · 35 m"; sol alt TARGET LOG x / 6 · LONGEST HIT 88 m.
- **Ses (ElevenLabs):** `boom_throw` (fırlatma vuuşu, atışın %70'inde), `boom_hit` (uzakta tok tahta vuruş, sayıldığı an), `boom_catch` (dönüşte yakalama şaplağı, %15'te) — atış başına birer, seyirde ~15 sn'de bir. Nadirde `guest_chime`.
- **Görseller:** `Tools/gen_boomerang.py` → `Assets/Art/Boomerang/` (atıcı, 6 sıradan + 6 nadir hedef, Activities kartı). Kâğıt fener ilk üretimde kenarında mavi hale (anahtar renk sızıntısı) kaldı → yeniden üretildi; ilk hâli silindi.

## 14.10 — Bumerang prototipi UYGULANDI (ajan)
- Kod: `Assets/Scripts/Boomerang/` — `BoomerangEnvironment` (veri, mesafe, metinler, ses ipuçları), `BoomerangSceneView` (çöl, atıcının yaslanması, bumerangın dönüş birikimi + uçuş/dönüş yayları, hedefin belirmesi/devrilmesi, kıvılcım; şerit).
- **Doğrulama (editör, Play, aynı sürücü; editör kaydı sonra birebir geri yüklendi, sha256 aynı):** kartta hız biriktirme, uçuş, vuruş anı (devrilen kutular + kıvılcım), ele dönüş, nadir hedefe nişan ("Aiming at · The Gong"), Gece; şerit Classic + Gece; hedef defteri (dolu). Hedefler atıcıya göre küçük görünüyordu → kartta 1,3×, şeritte 1,05× büyütüldü (büyütülmüş hâli bu commit'te yeniden çekilmedi).
- **Doğrulanmayan:** gerçek scroll ile atış/geri sarma, duyurunun çıkış anı, sesler kulakla (yalnız süre/LUFS/zarf), build'de şerit.

## 14.10 — Çömlekçi çarkı (sekizinci ortam) — İÇERİK (ajan; kullanıcının fikri; bütün ayrıntı kararları onay bekliyor)
- **Ad / tarif (oyunda):** "Pottery wheel" — "Spin the wheel, shape the clay."
- **Sahne (kart):** köy atölyesi (bej duvar, pencere, ahşap zemin). Solda taburede çömlekçi (tek karakter; Gemini), elleri çarktaki çamurda; çark kodla (ayak, çamur teknesi, dönen tabla — tabladaki işaretler yatay kayar). **Çamur kodla çizilir** (her karede küçük bir dokuya): topak, o çömleğin profiline doğru yükselip biçimlenir; dönüş yüzeyde kayan parıltı bantlarından ve çark izlerinden okunur. Biten çömlek pişmiş/sırlı görseline (Gemini) geçer, sağdaki rafa taşınır; raftaki çömlekler birer yuva kayar, en eskisi söner.
- **Bir çömlek (öğenin ilerlemesi):** %0–8 çarka yeni topak konur → %8–80 yükselip biçimlenir → %80–88 pişmiş/sırlı hâline geçer → %88–100 rafa taşınır (sayıldığı an rafa konduğu an).
- **Aşağı scroll (tablodaki "hamur basılır/çöker?" — karar): çamur bastırılır.** Çömlek raftan çarka geri gelir, sırı kaybolur, yeniden topağa çöker, sayaç düşer. Rölantide çark yavaş döner ("IDLE · SLOW WHEEL"). Başarısızlık yok (çömlek çökmez/kırılmaz).
- **Birim:** **biten çömlek (pots)**, **1 çömlek = 2.000 puan** → seyirde ~2 dk'da bir, rölantide ~12 dk. Günlük hedef 80.000 puan = **40 çömlek**, haftalık 500.000 = **250 çömlek**. Aylık sıfırlanır; leaderboard ve hedefler çömlekle.
- **Mekân:** prototipte yalnız **The Village Workshop**. Sıradanlar (deftere girmez): kâse, saksı, kupa, kavanoz, çay kâsesi, tabak. **Nadir çömlek %12** (seyirde ~17 dk'da bir), ilk pişirilişte **çömlek rafına ("Pottery shelf")** damgalanır: **The Water Jug (testi), The Tall Vase (vazo), The Amphora (amfora), The Teapot, The Tulip Vase (İznik), The Golden Urn** (en nadiri seyirde ~9 sa). Kullanıcının saydığı vazo/testi/amfora ilk üç sırada.
- **Görev çubuğu şeridi (tablodaki "dikey nesne — zor" — yatay yorum):** çark solda yazının yanında (çamur küçük ölçekte biçimlenir), sağına uzanan uzun bir **raf**: bu ay biten son 9 çömlek yan yana dizilir, yenisi çarktan rafın başına konur.
- **Kart HUD:** POTS MADE "37 pots" + SHAPING / IDLE · SLOW WHEEL / PRESSING DOWN; tabela THE VILLAGE WORKSHOP + "On the wheel: bowl" ya da "Shaping · The Amphora"; sol alt POTTERY SHELF x / 6 · LAST …
- **Ses (ElevenLabs):** `wheel_loop` (çark uğultusu + ıslak çamur; Olta'nın makara kuralıyla yalnız kullanıcı aktifken, perde scroll hızına göre), `clay_slap` (çarka topak konması, %2), `pot_shelf` (çömleğin rafa konması, sayıldığı an).
- **Görseller:** `Tools/gen_pottery.py` → `Assets/Art/Pottery/` (çömlekçi, 6 sıradan + 6 nadir çömlek, Activities kartı). Amfora ilk istemde ("black-figure" süsleme) Gemini güvenlik filtresine takıldı → geometrik bant/menderes tarifle üretildi; tabak dik (90° dönük), çay kâsesi saksı gibi çıktı → yeniden üretildi; reddedilenler silindi.

## 14.10 — Çömlekçi çarkı prototipi UYGULANDI (ajan)
- Kod: `Assets/Scripts/Pottery/` — `PotteryEnvironment` (veri + çömlek profilleri, metinler, ses), `PotterySceneView` (atölye, çark, dinamik çamur dokusu, pişirme geçişi, rafa taşıma, şerit rafı).
- **Doğrulama (editör, Play, sürücü; editör kaydı sonra birebir geri yüklendi, sha256 aynı):** kartta topak, biçimlenme, pişmiş hâle geçiş, rafa taşıma, nadir şekillenirken ("Shaping · The Amphora"), Gece; şerit (çark + raf) Classic + Gece; çömlek rafı paneli. İlk çekimde çark masa gibi görünüyordu, şerit çömlekleri çok küçüktü → tekne/taban yeniden çizildi, pencere eklendi, şerit büyütüldü, yeniden çekilip görüldü.
- **Uyarı (ajanın hatası):** bu son yeniden çekim, güvenlik kontrolündeki bir boru (pipe) hatası yüzünden kullanıcı LoL maçındayken yaklaşık 1 dakika editör penceresi açarak yapıldı. Kontrol düzeltildi; maç bitene kadar pencere açılmadı.
- **Doğrulanmayan:** gerçek scroll ile çark hızı/geri bastırma, duyuru anı, sesler kulakla, build'de şerit.
