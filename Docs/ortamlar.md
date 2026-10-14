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
