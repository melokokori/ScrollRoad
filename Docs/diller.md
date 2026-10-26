# Diller (2026-10-26)

Oyun 11 dilde: İngilizce (kaynak) + Basitleştirilmiş Çince, Rusça, İspanyolca, Portekizce (BR), Almanca, Japonca,
Fransızca, Korece, Lehçe, Türkçe.

## Dil nasıl seçilir

1. Oyuncu Ayarlar → Language'dan seçtiyse o (PlayerPrefs `ScrollRoad_Language`, cihaza özel).
2. Yoksa Steam'in oyun dili (`SteamApps.GameLanguage`; `latam` → es, `portuguese` → pt-BR, listede olmayan → en).
3. Steam yoksa Windows dili (`Application.systemLanguage`), o da listede yoksa İngilizce.

Otomatik bulunan dil kaydedilmez; Steam dili sonradan değişirse oyun da değişir.

## Kodda metin

- Anahtar = İngilizce kaynak metin. Gösterirken `L.T("Garage")`, biçimli `L.F("{0} / {1} rares", a, b)`,
  sayıya göre çoğul `L.P(n, "{0} day", "{0} days")`.
- Katalog verisi (ad, açıklama) tanım yerinde İngilizce kalır; gösterildiği yerde `L.T(name)`. Anahtarın
  toplanması için ya `L.N("...")` ile işaretlenir ya da dosyanın başına `// loc:catalog` yazılır (o dosyadaki
  boşluk içeren / büyük harfle başlayan, `_` `/` `#` içermeyen her düz metin anahtar olur).
- Sahne kurucusunun yazdığı sabit etiketler `LocalizedText` ile kendiliğinden çevrilir (`Tmp(...)` ekler).
  Birleşik etiket: `Localize(text, "{0}  Counted in {1}.", a, b)`; çalışma zamanında kodun yazdığı ya da özel
  ad olan etiket: `NoLocalize(text)`.
- Büyük/küçük harf: `L.Upper` / `L.Lower` (Türkçe i→İ; Almancada cümle içinde küçültmez).
- Sayılar `Fmt` ve `L.F` ile seçili dilin kültüründe (1.234,5 / 1 234,5). Genel `CurrentCulture` da seçili dil
  olur (TMP'nin UpperCase stili onu kullanır); kayıtlar kültürden bağımsız okunur/yazılır.
- Kasaba adları (Millbrook…) ve plak adları özel ad: her dilde İngilizce.

## Yeni metin eklendiğinde

1. Unity: `ScrollRoad/Sahneleri Kur` (kurucu değiştiyse) → `ScrollRoad/Dil Anahtarlarını Topla (sahne)`.
2. `python -I Tools/loc/extract.py --missing` → her dil için eksikler `Tools/loc/missing_<kod>.txt`.
3. `python -I Tools/loc/order.py` yeni anahtarları numaralar; `python -I Tools/loc/order.py <ilk> <son>` listeler.
4. Çeviriyi `Tools/loc/src/NN_*.txt`'ye yaz (`#numara` başlığı + `dil<TAB>metin`; çoğul `bir|birkaç|çok`
   ru/pl için, `bir|çok` diğer Avrupa dilleri için, tr/zh/ja/ko tek biçim).
5. `python -I Tools/loc/merge.py` → `Assets/Resources/Localization/<kod>.json`; `extract.py` yeniden çalıştırılıp
   0 eksik / 0 biçim hatası görülmeli (yer tutucular ve `<...>` etiketleri anahtardakiyle aynı olmalı).

Editörde tabloda olmayan anahtar bir kez `[L] Çeviri yok` uyarısı verir.

## Yazı tipleri

Overpass Latin + Kiril'i kapsar (sabit atlasta olmayan harfler çalışma zamanındaki dinamik Overpass yedeğinden).
Çince/Japonca/Korece için `UiFontFallback` Windows'un kendi yazı tiplerini (Microsoft YaHei, Yu Gothic, Malgun
Gothic) dinamik yedek ekler; sıra dile göre (Japoncada önce Yu Gothic, Korecede önce Malgun). Build büyümez.

## Bilinen sınırlar

- Kadran görsellerindeki yazılar (KNOTS gibi) dokuya basılı, çevrilmez.
- Steam başarımlarının Steam arayüzündeki adları/açıklamaları Steamworks panelinden ayrıca girilmeli
  (oyun içi duyuru çevrili).
- Çeviriler yapay zekâ ile yapıldı; yayından önce ana dili konuşan birine okutulması önerilir.
