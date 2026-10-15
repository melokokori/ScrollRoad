# Kod imzalama ve Smart App Control (2026-10-15)

**Karar kullanıcının.** Bu not yalnız seçenekleri toplar; fiyat/uygunluk koşulları değişebilir, satın almadan önce
sağlayıcının güncel sayfasından doğrulanmalı.

## Durum

Smart App Control (SAC), bulut itibarı olmayan ve geçerli imzası bulunmayan exe/DLL'leri engeller. 13.10'da
`LibUniWinC.dll`'i (UniWindowController) engelledi → görev çubuğu şeridi saydam değil, başlıklı siyah pencere.

15.10'da yapılanlar:
- UniWindowController paketi kaldırıldı; şerit saydamlığı, tıklama geçirgenliği ve üstte kalma artık
  `NativeWindow` içinde doğrudan `user32.dll`/`dwmapi.dll` (Windows'un kendi imzalı DLL'leri) ile yapılıyor.
- Burst AOT kapatıldı (`ProjectSettings/BurstAotSettings_StandaloneWindows.json`): imzasız
  `lib_burst_generated.dll` artık üretilmiyor. Oyunda Burst'lü kendi kodumuz yok; paketlerin Burst işleri
  yönetilen (Mono) yoldan çalışır.

Build'deki yerel (native) dosyalar şimdi:

| Dosya | İmza |
|---|---|
| `UnityPlayer.dll`, `UnityCrashHandler64.exe`, `mono-2.0-bdwgc.dll`, `MonoPosixHelper.dll` | Unity Technologies |
| `steam_api64.dll` | Valve |
| `D3D12Core.dll` | Microsoft |
| **`ScrollRoad.exe`** | **İMZASIZ** (Unity oynatıcı exe'sini adı/simgesiyle yeniden yazdığı için imzası düşüyor) |

Yani SAC açısından geriye tek risk `ScrollRoad.exe` kaldı. Bu makinede SAC onu `Builds/Windows`'ta çalıştırıyor,
yeni bir klasördeki kopyasını (ör. `Builds/Review`) engelledi.

## Steam dağıtımında etkisi

- SAC yalnız temiz Windows 11 kurulumlarında açık gelir ve kullanıcı alışkanlığına göre kendiliğinden kapanabilir.
  Oyuncuların çoğunda kapalıdır, ama açık olanlarda imzasız ve itibarsız bir exe engellenebilir.
- Steam imza istemez; birçok indie oyun imzasızdır. Yeni bir oyunun exe'si başta bulut itibarına sahip olmaz.
- Microsoft'un tarifine göre SAC, bulut kesin karar veremezse **geçerli imzası olan** uygulamaya izin verir.
  Yani imza, SAC açık oyuncular için asıl çözüm.

## Seçenekler

1. **İmzalamamak (şimdiki hâl).** Maliyet yok. Risk: SAC'ı açık az sayıda oyuncuda exe engellenebilir;
   destek sayfasına "SAC'ı kapatın" notu gerekir (SAC kapatılınca yeniden açılamaz — kullanıcıya önermek ağır).
2. **Azure Artifact Signing (eski adı Trusted Signing).** Aylık ücretli (Basic katmanı yaklaşık 10 $/ay
   düzeyindeydi), donanım anahtarı gerekmez, imza CI'dan/komut satırından atılır. Kimlik doğrulaması gerekir;
   bireysel geliştiricilere açık olduğu ülkeler sınırlıydı (ABD/Kanada) — Türkiye'den bireysel başvurunun
   mümkün olup olmadığı kontrol edilmeli; şirket olarak başvuruda şirketin belli bir süredir var olması isteniyordu.
3. **OV kod imzalama sertifikası** (Sectigo, SSL.com, Certum vb.). Yıllık ücretli (yüzlerce $ düzeyi),
   2023'ten beri anahtar donanım token'ında/bulut HSM'de tutulmak zorunda. Türkiye'den bireysel ya da şahıs
   şirketi olarak alınabilir. SmartScreen itibarını anında vermez ama SAC için geçerli imza yeterli.
4. **EV sertifika.** Daha pahalı; 2024'ten beri SmartScreen'de OV'ye göre anlık avantajı kalmadı. Bu oyun için
   gereksiz görünüyor.

Not: Certum'un ucuz "Open Source" sertifikası açık kaynak projeler içindir; depo gizli olduğu için uygun değil.

## İmza gelirse yapılacak

- `ScrollRoadBuilder` build sonrası `signtool sign /fd SHA256 /tr <zaman damgası> ...` ile `ScrollRoad.exe`'yi
  imzalar (Artifact Signing'de `signtool` + Azure dlib eklentisi). Unity'nin imzalı DLL'lerine dokunulmaz.
- Steam'e yüklemeden önce imzalı exe'yi SAC açık bir makinede (ya da bu makinede yeni bir klasörde) dene.
