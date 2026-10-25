# ScrollRoad — Başarımlar (2026-10-25)

Kod: `Assets/Scripts/Steam/Achievements.cs`. Koşullar oyunun kendi kaydından 5 sn'de bir değerlendirilir; açılanlar yerelde tutulur ve Steam bağlıyken Steam'e işlenir (sonradan bağlanırsa bekleyenler o zaman gider). Test app'inde (480, Spacewar) gönderilmez.

**Steamworks'te yapılacak (gerçek App ID alınınca):** App Admin → Stats & Achievements → Achievements'ta aşağıdaki **API adlarıyla** 20 başarım oluşturulmalı (Display name / Description İngilizce, aşağıdaki gibi). Her biri için 256×256 açık + gri (kilitli) ikon gerekir — henüz yok, pixel art'la birlikte çizilebilir. Sonra `SteamManager.TestAppId` gerçek App ID olmalı.

| API adı | Display name | Description |
|---|---|---|
| FIRST_KM | Ignition | Drive your first kilometre. |
| KM_100 | Road Tripper | Drive 100 km in total. |
| KM_1000 | Long Haul | Drive 1,000 km in total. |
| KM_10000 | Million Scrolls | Drive 10,000 km in total. |
| GARAGE_10 | Garage Sale | Unlock 10 vehicles. |
| ALL_ACTIVITIES | Jack of All Trades | Try every activity. |
| FIRST_RARE | Beginner's Luck | Find your first rare. |
| RARES_50 | Collector | Find 50 different rares. |
| RARES_150 | Curator | Find 150 different rares. |
| RARES_ALL | The Whole Album | Find every rare in every place. |
| PLACE_COMPLETE | Full Shelf | Find all six rares of one place. |
| ACTIVITY_COMPLETE | Master of One | Complete every place of one activity. |
| NIGHT_OWL | Night Owl | Find a rare that only comes out after dark. |
| LUCKY_TIMING | Right Place, Right Time | Find a rare during a happening. |
| FOCUSED | Eyes on the Prize | Find the rare you were focusing on. |
| FIRST_ORDER | Order Up | Complete an order. |
| ORDERS_50 | Regular | Complete 50 orders. |
| STREAK_7 | Seven Days | Finish all of the day's orders seven days in a row. |
| NEW_LOOK | New Look | Unlock a new look in any activity. |
| SIDE_GIG | Side Gig | Put an activity in the side slot. |

Notlar:
- "Rare" sayımı 9 içerikli ortamın 5×6 nadiri (270); efsaneler (Altın Sazan, Koca Ayak) dahil değil.
- Mevcut kayıtta zaten sağlanan koşullar ilk açılışta hemen açılır (birkaç duyuru art arda gelebilir; duyuru kuyruğu en çok 3 tutar, fazlası yalnız Steam'de görünür).
- İnceleme modunda (`-unlockall` / editör menüsü) değerlendirilmez.
