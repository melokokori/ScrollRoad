# ScrollRoad — Gece Raporu (13.10.2026, ~00:45–01:35)

Görev: `Docs/ortamlar.md`'deki 5 adımlı prototip planının tamamı. **Beş adımın beşi de bitti.** Proje derleniyor, `Builds/Windows` güncel (130 MB, sabah denenebilir).
**Hiçbir şey commit/push EDİLMEDİ.** Önceden commit edilmemiş işe (araç sesleri, VehicleSound, props, şerit cilası vb.) dokunulmadı; yalnız aşağıda adı geçen dosyalara ekleme yapıldı.

> ⚠️ **Sabah önce bunu okuyun — kodla ilgisi olmayan bir engel:** Smart App Control bu gece `LibUniWinC.dll`'yi (UniWindowController'ın yerel eklentisi; 10.10'dan beri aynı dosya) hem editörde hem build'de **engellemeye başladı** (CodeIntegrity 3033/3077, yalnız 13.10 tarihli olaylar). Sonuç: **görev çubuğu modu saydam şerit yerine başlıklı, siyah zeminli normal bir pencere olarak açılıyor** — Yol için de Olta için de. Kart modu etkilenmiyor (o kendi Win32 çağrılarımızla çalışıyor). SAC'ı kapatmak geri alınamaz olduğu için dokunmadım. Belki yalnız bu geceye özgü bir itibar sorgusu sorunudur; sabah bir kez deneyin, sürerse karar sizin (SAC / DLL imzalama).

---

## Kayıt yedeği ve geri yükleme

Her şeyden önce alındı: `C:\Users\WİN10\Desktop\IDLE\kayit_yedegi_2026-10-13\`
- `build_playerprefs.reg` — oyunun kaydı (`HKCU\Software\DefaultCompany\ScrollRoad`)
- `editor_playerprefs.reg` — editörün kaydı (`HKCU\Software\Unity\UnityEditor\DefaultCompany\ScrollRoad`)
- `LocalLow_ScrollRoad\` — persistentDataPath (yalnız günlükler; SaveService dosya kullanmıyor)
- `kod_oncesi\` — işe başlamadan önceki `Assets/Scripts`, `Assets/Editor`, `Game.unity` kopyası

**Test sonunda iki kayıt da yedekten birebir geri yüklendi** (dışa aktarılan hâli yedekle bayt bayt aynı). Yani sizin oyun kaydınız bu gecenin başındaki hâlinde: 319,6 km ömür boyu, ay 316,2 km. Puana taşıma sabah oyunu ilk açtığınızda (bir kez) yapılacak.

Geri yükleme komutu (PowerShell ya da cmd; önce oyunu kapatın):
```
reg delete "HKCU\Software\DefaultCompany\ScrollRoad" /f
reg import "C:\Users\WİN10\Desktop\IDLE\kayit_yedegi_2026-10-13\build_playerprefs.reg"
```
Editör için aynı iki satır, anahtar `HKCU\Software\Unity\UnityEditor\DefaultCompany\ScrollRoad`, dosya `editor_playerprefs.reg`.

---

## Ne yapıldı (adım adım)

### 1. Puan katmanı — `Assets/Scripts/Score/ScoreEngine.cs` (yeni)
- Seyir + boost + 3 dk aktiflik penceresi + rölanti modeli `RoadDrive`'dan **aynen** buraya taşındı (değerler aynı: 60 / 10 / ±30 çentik / 220 / -60 / 2 sn dönüş).
- Motor ortamdan bağımsız bir "tempo" ve gizli puan üretir: ay konumu (eksiye inmez), ay rekoru (= aylık skor), ömür boyu puan (ileri adımların toplamı), en iyi ay.
- **1 puan = Yol'da 1 metre.** Yol'da tempo birimi km/sa'e birebir eşit olduğu için Yol'un hızı/mesafesi/görünüşü değişmedi; leaderboard'a giden tamsayı skor da eskisiyle aynı ölçekte (eskiden "metre" gönderiliyordu).
- `RoadDrive` artık hız modelini tutmuyor; yalnız Yol aktif ortamken motorun puanıyla ilerliyor (km = puan × 0,001). Genel API'si (CurrentSpeedKmh, IsUserActive, olaylar…) aynı kaldı, diğer 20 dosya bozulmadı.
- **Taşıma (veri kaybı yok, idempotent):** `SaveService.EnsurePointsMigrated()` ilk yüklemede bir kez çalışır: Yol km anahtarlarından puan anahtarlarını üretir, `ScrollRoad_PointsVersion=1` yazar; sonraki açılışlarda hiçbir şey yapmaz. **Yol'un km anahtarlarına dokunmaz** (silmez/değiştirmez) — bölgeler, pasaport, araç kilitleri aynen onlardan devam eder. Günlük/haftalık hedef kaydı da bir kez km→puana çevrilir. Puanlar Steam Cloud birleştirmesine eklendi (eski sürümden gelen bulut kaydında puan km'den türetilir).

### 2. Ortam yapısı — `Assets/Scripts/Environments/`
- `EnvironmentCatalog.cs`: her ortamın adı, tarifi, birimi, oranı, açılma eşiği, hedef duyurusu sözcüğü, koleksiyon adı. Yol ilk ortam.
- `EnvironmentManager.cs`: seçili ortam (yerel PlayerPrefs `ScrollRoad_Environment`), kilit = ömür boyu puan ≥ eşik (kaydedilmez, türetilir), her ortamın açıldığı **bir kez** yeşil tabelayla duyurulur ("New activity: Fishing") — eşiği zaten geçmiş oyuncuya da ilk açılışta bir kez.
- Kart sahnesi / görev çubuğu şeridi / HUD metinleri ortama göre: Yol seçiliyken her şey eskisi gibi; başka ortamda Yol dünyası gizlenir, araç sesi susar, Yol HUD'unun kadran/tabela/şehir plakası gizlenir; o ortamın görünümü kamerayı devralır.

### 3. Birimi aktif ortamdan alanlar (puan hiç görünmez)
- Leaderboard (gönderim puan; gösterim bakan oyuncunun aktif ortam biriminde, rakiplerin şehir kırpması puan cinsinden aynen).
- Çekmece başlığı (MONTH BEST, haftalık hedef satırı).
- Günlük/haftalık hedefler (kart HUD'u "TODAY", çekmece, hedef duyuruları: "Daily distance goal" / "Daily catch goal").
- Tepsi ipucu metni (Olta'da "… kg caught").

### 4. Ortam seçici
- ≡ çekmecesinin en üstüne **Activities** satırı eklendi (7 satır). Panel: her ortam bir kart (görsel, ad, tarif, "Counted in km/kg"); açık olana tıklayınca o ortam seçilir; kilitliyse koyu silüet + ilerleme çubuğu + "LOCKED · 48 km to go" (kalan miktar o an açık ortamın biriminde).
- Olta seçiliyken çekmecedeki **Passport** satırı **Fish log** olur ve balık defterini açar.

### 5. Olta prototipi — `Assets/Scripts/Fishing/`
- `FishingController.cs`: oto atış → yem uzağa düşer → yukarı scroll makarayı sarar, yem kıyıya gelir → yolda balık yeme vurur ("Fish on!") → sarılıp kıyıda tutulur → deftere girer → yeni atış. **Aşağı scroll makarayı geri çevirir** (misina boşalır, yem/balık uzaklaşır; atılan mesafeden öteye gitmez). Rölantide makara yavaş sarar. **Başarısızlık yok.**
- **kg ∝ puan:** balık (tür + kg) atışta belirlenir, getirmek için gereken puan = kg ÷ oran; tutulan toplam kg, sarılan puanın tam oran katıdır (test: 12,94 kg = o aralıkta gelen puan + sudaki balığın önceden sarılmış payı). Atış/tutuş animasyonu sırasında gelen puan kaybolmaz, sonraki atışa sayılır. Büyük balık daha uzağa/derine atılır, gelmesi uzun sürer. Süren atış kaydedilir (kapatıp açınca kaldığı yerden).
- `FishingSceneView.cs`: solda iskelede balıkçı, sağda göl ve su altı kesiti; misina (kamış ucundan yeme), yem, yaklaşan/takılı/çıkarılan balık, sıçrama damlaları, sazlar, kıyı. Lakeshore bölge renkleri; dünya teması (Classic/Night/Sepia) uygulanıyor. Görev çubuğu şeridi: saydam zemin, yatay su bandı, balıkçı şeridin ~%30'unda, misina/yem/balık; solda toplam kg + durum.
- `FishingHudController.cs`: kart HUD'u — sol üstte TOTAL CATCH (kg) + REELING / IDLE · SLOW REEL / LETTING LINE OUT; sağ üstte yeşil tabela THE LAKE + durum (Casting… / Retrieving / Fish on! / "Northern Pike 9.16 kg") + yemin uzaklığı (m); sol altta balık defteri özeti + son balık; sağ alt bugünkü hedef (ortak).
- `FishLogController.cs`: balık defteri ilk hâli — 6 yuva; tutulmamış tür koyu silüet + "???" + kg aralığı; tutulunca renkli görsel, ad, en büyük kg rekoru, kaç kez, ilk tutuluş tarihi. Yeni türde yeşil tabela duyurusu.
- Görseller **Gemini** (`gemini-3.1-flash-image`) ile, yeni `Tools/gen_fishing.py` (gen_vehicle.py kalıbı; stil referansı vesper.png'deki yüzsüz sürücü figürü; zemin **mavi** #0000FF — yeşil zemin yeşil balıkları, macenta alabalığın pembe şeridini silebilirdi). Balıkçı **iskeleyle birlikte tek istekte** çizildi. Çıktılar `Assets/Art/Fishing/` (7 dosya, hepsi kullanılıyor; reddedilen/ara görsel yok), ham kaynaklar `Tools/art_source/raw/fishing_*.png` (araçlardaki düzenle aynı). fal.ai kullanılmadı. Ses ve skin yok.

### Değişen dosyalar
Yeni: `Score/ScoreEngine.cs`, `Environments/{EnvironmentCatalog,EnvironmentManager,ActivitiesController}.cs`, `Fishing/{FishCatalog,FishingController,FishingSceneView,FishingHudController,FishLogController}.cs`, `Assets/Art/Fishing/*.png`, `Tools/gen_fishing.py`.
Değişen: `RoadDrive`, `SaveService`, `ScrollRoadSaveData`, `GoalTracker`, `LeaderboardSync`, `ScrollRoadLeaderboard`, `CardMenuController`, `HudController`, `NoticeController`, `PassportController`, `CompanionHudController`, `BackgroundLifecycle`, `RoadSceneView` (yalnız ortam kapısı), `VehicleSound` (yalnız ortam sessizliği), `SceneNames` (`Fmt.Amount`), `Editor/ScrollRoadSceneBuilder`, `Scenes/Game.unity` (kurucuyla yeniden üretildi). TMP font varlıkları dinamik atlas yüzünden değişmiş görünüyor (Unity gürültüsü).

---

## Doğrulananlar
- **Derleme:** Unity dışı `dotnet build` (oyun + editör derlemeleri) 0 hata; Unity batchmode ve editörde 0 derleme hatası; sahne kurucu bağlama hatası yok; editör günlüğünde kodumdan istisna yok.
- **Taşıma gerçek verinizle** (editöre kopyalanan kaydınız ve build'in kendisi): ömür boyu 319,64 km → 319.637 puan, ay rekoru 316,2 km → 316.203 puan; Yol km'si aynen devam etti. İkinci açılışta tekrar taşımadı.
- **Yol değişmedi:** kart ekran görüntüleri (kendi biplanınız, gece teması) öncekiyle aynı; Olta'dan Yol'a dönüş sorunsuz; Yol km'si Olta seçiliyken donuk kaldı.
- **Olta döngüsü** (editör Play, tempo test için sabitlendi): atış, sarma, vuruş, takılı turna, tutuş + sıçrama, deftere kayıt, yeni tür duyurusu, kg ∝ puan hesabı, kayıt/devam (Play'ler arası atış ve ortam korunuyor), gece/sepya temaları.
- **Paneller:** Activities (açık + kilitli hâl), Fish log, çekmecede "Fish log" satırı ve kg birimleri — ekran görüntüleriyle.
- **Şerit yerleşimi:** editörde Game görünümü 440×48 yapılarak gerçek oranda (Olta + Yol) görüldü.
- **Build:** gerçek fare tıklamalarıyla ≡ → Activities → Fishing → Olta kartı; geri Yol'a; "New activity: Fishing" duyurusu; süreç yanıt veriyor.

## Doğrulanmayanlar
- **Gerçek fare tekerleğiyle Olta** (testte tempo kodla sabitlendi; enjekte scroll zaten sayılmıyor). Motor, Yol'un kullandığı aynı GlobalScrollSource yolunu kullanıyor.
- **Aşağı scroll'un Olta'daki görünümü** (misina boşalması) — yalnız kodla; ekranda görülmedi.
- **Build'de görev çubuğu şeridi** — SAC engeli yüzünden saydam şerit oluşmadı (yukarıdaki uyarı).
- Steam (leaderboard gönderimi/okuma, cloud birleştirme) — Steam bağlı değildi.

---

## Benim verdiğim kararlar (onayınıza)
| Konu | Karar |
|---|---|
| Puan ölçeği | 1 puan = Yol'da 1 m (Yol oranı 0,001 km/puan). Leaderboard skoru eski ölçekte kalır. |
| Olta oranı | 1 kg = 500 puan (0,002 kg/puan) → aktif scroll'da ~120 kg/sa, rölantide ~20 kg/sa; ortalama bir balık aktifken ~0,5–1 dk. |
| Olta açılma eşiği | Ömür boyu 250.000 puan (= Yol'da 250 km) — günde 1–2 sa aktif oynayan için 2–4 gün. Sizin hesabınızda zaten açık (319 km). |
| Hedefler | Puanla: günlük 80.000 / haftalık 500.000 (Yol'da 80 / 500 km, Olta'da 160 / 1.000 kg). Değerler eskisiyle aynı. |
| Balık listesi (Göl) | Perch 0,1–1,2 kg · Chub 0,3–2,5 · Rainbow Trout 0,5–3,5 · Largemouth Bass 0,5–4 · Zander 1–8 · Northern Pike 2–14. Sıklık 30/24/18/14/9/5 (küçük çok, iri nadir; kg aralığın küçüğüne eğilimli). |
| Defterde tutulmamış tür | "???" + kg aralığı ipucu. |
| Çekmece "MONTH BEST" | Aylık skorun (ortak puan) aktif ortam birimindeki karşılığı — bu yüzden Olta'da tuttuğunuz kg'dan büyük görünebilir (ör. "635 kg"): o ay Yol'da gidilen yol da puana dahil. Kararınızın (leaderboard aktif ortam biriminde) doğal sonucu. |
| Çekmece | Activities en üstte; Passport satırı Olta'da "Fish log". Garage/Stats Yol'a özel kaldı. |
| Ses | Olta'da araç sesi ve scroll "tik"i susar (Olta sesi sonraki aşama). |
| Olta kayıt | Defter + süren atış yerel PlayerPrefs (`ScrollRoad_Fishing`), Steam Cloud'a henüz girmiyor (hedefler gibi). Puanlar cloud'da. |
| Şerit (Olta) | Kadran stili ayarı Olta'da uygulanmaz: solda kg + durum yazısı ("makara devri kadranı" skin aşamasında). |
| Uzaklık "m" | Yalnız görsel ölçek (en uzak atış ≈ 30 m). |
| Atış sırası puan | Atış/tutuş animasyonunda gelen puan sonraki balığa en çok "1,5 sn'de bir balık" hızıyla aktarılır (küçük balık görünmeden tutulmasın diye; puan kaybolmaz). |

## Bilinen sorunlar
1. **SAC → LibUniWinC engeli** (en üstte). Görev çubuğu modu şu an bozuk görünecek.
2. Açılışın ilk saniyelerinde kuyruğa giren duyurular, açılış takılması yüzünden görünmeden bitebiliyor (ortam duyurusu için 4 sn erteleyerek düzelttim; araç kilidi gibi eski duyurularda aynı durum olabilir — dokunmadım).
3. Atış/tutuş animasyonunda biriken birkaç saniyelik puan, tam o an oyun kapanırsa kaydedilmez (birkaç puan; önemsiz).
4. Editörün Game görünümüne test için "Night 440x48" adlı özel boyut eklendi (zararsız; Game görünümü boyut listesinden silinebilir).
5. İlk açılış rehberi Activities'ten bahsetmiyor.

## Sıradaki adım önerisi
1. Sabah build'i açın: önce "New activity: Fishing" duyurusu gelir; ≡ → Activities → Fishing ile deneyin, gerçek tekerlekle sarın, aşağı scroll'u deneyin; balık defterine bakın.
2. Görev çubuğu modu siyah pencere çıkarsa: SAC/LibUniWinC kararı.
3. Oranları/eşiği/balık listesini onaylayın ya da değiştirin (hepsi `EnvironmentCatalog.cs` ve `FishCatalog.cs`'de tek satır).
4. Sonra (sizin sıranızla): Olta sesi (makara cırcırı, su, şıpırtı), makara kadranı skin'i, sonraki mekânlar (Nehir → Kıyı → Açık deniz → Buz deliği) ve gizli Altın Sazan.
