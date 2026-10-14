using System;
using System.Globalization;
using System.Text;
using Steamworks;
using UnityEngine;

namespace ScrollRoad
{
    // Kalıcılığa dokunan TEK yer burası. Yerel depo PlayerPrefs; Steam açıksa aynı veri
    // JSON anlık görüntüsü olarak Steam Cloud'a da yazılıyor (cihazlar arası senkron).
    // Oturum başına bir kez, ilk Load'da cloud ile yerel kayıt birleştirilir; sonrasında
    // oturum boyunca yerel kayıt esas alınır. Steam yoksa sessizce yalnız PlayerPrefs.
    public static class SaveService
    {
        // double mesafeler PlayerPrefs'te string olarak (InvariantCulture, "R") saklanır.
        const string CurrentDistanceKey = "ScrollRoad_CurrentDistanceKmD";
        const string MaxDistanceKey = "ScrollRoad_MaxDistanceKmD";
        const string RaceMonthKey = "ScrollRoad_RaceMonth";
        const string LastCloseUnixSecondsKey = "ScrollRoad_LastCloseUnixSeconds";

        const string LifetimeKmKey = "ScrollRoad_LifetimeKmD";
        const string LifetimeStepsKey = "ScrollRoad_LifetimeSteps";
        const string LifetimeTurnsKey = "ScrollRoad_LifetimeTurns";
        const string CityReachedCountKey = "ScrollRoad_CityReachedCount";
        const string BestMonthKmKey = "ScrollRoad_BestMonthKmD";
        const string BestMonthKey = "ScrollRoad_BestMonth";
        const string SecretCarUnlockedKey = "ScrollRoad_SecretCarUnlocked";

        // Gizli ortak puan (2026-10-13). 1 puan = Yol'da 1 metre (EnvironmentCatalog.Road), böylece
        // aylık leaderboard'a giden tamsayı skor eski "metre" skoruyla birebir aynı kalır.
        // Yol'un kendi anahtarları (...KmD) DEĞİŞMEDEN durur: bölge/pasaport/araç kilitleri onlardan.
        public const int CurrentPointsVersion = 1;
        const string PointsVersionKey = "ScrollRoad_PointsVersion";
        const string PointsRaceMonthKey = "ScrollRoad_PointsRaceMonth";
        const string MonthPointsKey = "ScrollRoad_MonthPointsD";
        const string MonthBestPointsKey = "ScrollRoad_MonthBestPointsD";
        const string LifetimePointsKey = "ScrollRoad_LifetimePointsD";
        const string BestMonthPointsKey = "ScrollRoad_BestMonthPointsD";
        const string BestPointsMonthKey = "ScrollRoad_BestPointsMonth";
        const string PointsMigratedFromKmKey = "ScrollRoad_PointsMigratedFromKmD"; // yalnız iz/teşhis için

        // Cihaza özel tercihler — cloud'a bilerek dahil edilmez.
        const string MasterVolumeKey = "ScrollRoad_MasterVolume";
        const string StartInCompanionKey = "ScrollRoad_StartInCompanion";
        const string CloseToTrayKey = "ScrollRoad_CloseToTray";
        const string TrayHintShownKey = "ScrollRoad_TrayHintShown";
        const string OnboardingDoneKey = "ScrollRoad_OnboardingDone";
        const string SelectedCarKey = "ScrollRoad_SelectedCar";
        const string WorldThemeKey = "ScrollRoad_WorldTheme";
        const string CardSizeKey = "ScrollRoad_CardSize";
        const string CardOnTopKey = "ScrollRoad_CardOnTop";
        const string CardLeftKey = "ScrollRoad_CardLeft";
        const string CardTopKey = "ScrollRoad_CardTop";
        const string GoalsKey = "ScrollRoad_Goals"; // günlük/haftalık hedefler (JSON) — şimdilik yalnız yerel
        const string EnvironmentKey = "ScrollRoad_Environment"; // seçili ortam (yerel)
        const string FishingKey = "ScrollRoad_Fishing";         // Olta ilerlemesi + balık defteri (JSON, şimdilik yerel)
        const string DoorKey = "ScrollRoad_Door";               // Döner kapı ilerlemesi + misafir defteri (JSON, şimdilik yerel)
        const string WindKey = "ScrollRoad_Wind";               // Rüzgârgülü ilerlemesi + şebeke defteri (JSON, şimdilik yerel)

        const string CloudFileName = "scrollroad_save.json";

        static bool cloudMergeDone;
        static bool cloudWarningLogged;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetSessionState()
        {
            cloudMergeDone = false;
            cloudWarningLogged = false;
        }

        public static ScrollRoadSaveData Load()
        {
            EnsurePointsMigrated();
            var local = LoadLocal();

            if (!cloudMergeDone && CloudAvailable())
            {
                cloudMergeDone = true; // Steam hazır değilse bayrak kalkmaz, sonraki Load'da yeniden denenir
                var cloud = TryReadCloud();
                if (cloud != null)
                {
                    local = Merge(local, cloud);
                    WriteLocal(local);
                }
                TryWriteCloud(local);
            }

            return local;
        }

        public static void SaveProgress(string raceMonth, double currentDistanceKm, double maxDistanceKm)
        {
            PlayerPrefs.SetString(RaceMonthKey, raceMonth);
            SetDouble(CurrentDistanceKey, currentDistanceKm);
            SetDouble(MaxDistanceKey, maxDistanceKm);
            PlayerPrefs.Save();
            PushLocalToCloud();
        }

        public static void SavePoints(string raceMonth, double monthPoints, double monthBestPoints,
            double lifetimePoints, double bestMonthPoints, string bestMonth)
        {
            PlayerPrefs.SetString(PointsRaceMonthKey, raceMonth);
            SetDouble(MonthPointsKey, monthPoints);
            SetDouble(MonthBestPointsKey, monthBestPoints);
            SetDouble(LifetimePointsKey, lifetimePoints);
            SetDouble(BestMonthPointsKey, bestMonthPoints);
            PlayerPrefs.SetString(BestPointsMonthKey, bestMonth);
            PlayerPrefs.Save();
            PushLocalToCloud();
        }

        // Ömür boyu puan (ScoreEngine yokken ucuz okuma).
        public static double LoadLifetimePoints()
        {
            EnsurePointsMigrated();
            return GetDouble(LifetimePointsKey);
        }

        // Puan katmanından önceki kaydı bir kez puana taşır (2026-10-13). İdempotent: sürüm anahtarı
        // yazıldıktan sonra hiçbir şey yapmaz. Yol km anahtarlarına DOKUNMAZ (silmez, değiştirmez) —
        // Yol ilerlemesi onlardan aynen devam eder. 1 km = 1000 puan.
        public static void EnsurePointsMigrated()
        {
            if (PlayerPrefs.GetInt(PointsVersionKey, 0) >= CurrentPointsVersion) return;

            double ppk = 1.0 / EnvironmentCatalog.Road.UnitsPerPoint;
            string raceMonth = PlayerPrefs.GetString(RaceMonthKey, "");
            double current = GetDouble(CurrentDistanceKey) * ppk;
            double max = Math.Max(GetDouble(MaxDistanceKey) * ppk, current);
            double lifetime = Math.Max(GetDouble(LifetimeKmKey) * ppk, max);
            double bestMonth = GetDouble(BestMonthKmKey) * ppk;
            string bestMonthName = PlayerPrefs.GetString(BestMonthKey, "");
            if (max > bestMonth)
            {
                bestMonth = max;
                bestMonthName = raceMonth;
            }

            PlayerPrefs.SetString(PointsRaceMonthKey, raceMonth);
            SetDouble(MonthPointsKey, current);
            SetDouble(MonthBestPointsKey, max);
            SetDouble(LifetimePointsKey, lifetime);
            SetDouble(BestMonthPointsKey, bestMonth);
            PlayerPrefs.SetString(BestPointsMonthKey, bestMonthName);
            SetDouble(PointsMigratedFromKmKey, GetDouble(LifetimeKmKey));
            PlayerPrefs.SetInt(PointsVersionKey, CurrentPointsVersion);
            PlayerPrefs.Save();
            Debug.Log($"Puan katmanına taşındı: ömür boyu {lifetime:0} puan, bu ay en iyi {max:0} puan ({raceMonth}).");
        }

        // Açıldığı duyurulmuş ortamlar (bit maskesi, yerel): her ortam bir kez duyurulur.
        const string EnvironmentsAnnouncedKey = "ScrollRoad_EnvironmentsAnnounced";

        public static bool IsEnvironmentAnnounced(int id) => (PlayerPrefs.GetInt(EnvironmentsAnnouncedKey, 0) & (1 << id)) != 0;

        public static void MarkEnvironmentAnnounced(int id)
        {
            PlayerPrefs.SetInt(EnvironmentsAnnouncedKey, PlayerPrefs.GetInt(EnvironmentsAnnouncedKey, 0) | (1 << id));
            PlayerPrefs.Save();
        }

        public static int LoadEnvironment() => PlayerPrefs.GetInt(EnvironmentKey, 0);

        public static void SaveEnvironment(int id)
        {
            PlayerPrefs.SetInt(EnvironmentKey, id);
            PlayerPrefs.Save();
        }

        public static FishingProgress LoadFishing()
        {
            string json = PlayerPrefs.GetString(FishingKey, "");
            if (string.IsNullOrEmpty(json)) return new FishingProgress();
            try
            {
                return JsonUtility.FromJson<FishingProgress>(json) ?? new FishingProgress();
            }
            catch (Exception)
            {
                return new FishingProgress();
            }
        }

        public static void SaveFishing(FishingProgress progress)
        {
            PlayerPrefs.SetString(FishingKey, JsonUtility.ToJson(progress));
            PlayerPrefs.Save();
        }

        public static DoorProgress LoadDoor()
        {
            string json = PlayerPrefs.GetString(DoorKey, "");
            if (string.IsNullOrEmpty(json)) return new DoorProgress();
            try
            {
                return JsonUtility.FromJson<DoorProgress>(json) ?? new DoorProgress();
            }
            catch (Exception)
            {
                return new DoorProgress();
            }
        }

        public static void SaveDoor(DoorProgress progress)
        {
            PlayerPrefs.SetString(DoorKey, JsonUtility.ToJson(progress));
            PlayerPrefs.Save();
        }

        public static WindProgress LoadWind()
        {
            string json = PlayerPrefs.GetString(WindKey, "");
            if (string.IsNullOrEmpty(json)) return new WindProgress();
            try
            {
                return JsonUtility.FromJson<WindProgress>(json) ?? new WindProgress();
            }
            catch (Exception)
            {
                return new WindProgress();
            }
        }

        public static void SaveWind(WindProgress progress)
        {
            PlayerPrefs.SetString(WindKey, JsonUtility.ToJson(progress));
            PlayerPrefs.Save();
        }

        public static void SaveCloseTimestamp(long unixSeconds)
        {
            PlayerPrefs.SetString(LastCloseUnixSecondsKey, unixSeconds.ToString(CultureInfo.InvariantCulture));
            PlayerPrefs.Save();
            PushLocalToCloud();
        }

        public static void SaveStats(double lifetimeKm, int lifetimeSteps, int lifetimeTurns,
            int cityReachedCount, double bestMonthKm, string bestMonth)
        {
            SetDouble(LifetimeKmKey, lifetimeKm);
            PlayerPrefs.SetInt(LifetimeStepsKey, lifetimeSteps);
            PlayerPrefs.SetInt(LifetimeTurnsKey, lifetimeTurns);
            PlayerPrefs.SetInt(CityReachedCountKey, cityReachedCount);
            SetDouble(BestMonthKmKey, bestMonthKm);
            PlayerPrefs.SetString(BestMonthKey, bestMonth);
            PlayerPrefs.Save();
            PushLocalToCloud();
        }

        // Gizli bölge ödülü — kalıcı, cihazlar arası taşınır (cloud birleştirmede OR).
        // Yalnız yerel değer (cloud birleştirmesi tetiklenmez): garaj kilitleri için ucuz okuma.
        public static double LoadLifetimeKm() => GetDouble(LifetimeKmKey);
        public static double LoadBestMonthKm() => GetDouble(BestMonthKmKey);

        public static bool LoadSecretCarUnlocked() => PlayerPrefs.GetInt(SecretCarUnlockedKey, 0) == 1;

        public static void UnlockSecretCar()
        {
            if (LoadSecretCarUnlocked()) return;
            PlayerPrefs.SetInt(SecretCarUnlockedKey, 1);
            PlayerPrefs.Save();
            PushLocalToCloud();
        }

        // Garaj seçimi kozmetik ve cihaza özel (cloud dışı); kilit durumu CarCatalog'da denetlenir.
        public static string LoadSelectedCar() => PlayerPrefs.GetString(SelectedCarKey, "");

        public static void SaveSelectedCar(string id)
        {
            PlayerPrefs.SetString(SelectedCarKey, id);
            PlayerPrefs.Save();
        }

        public static int LoadWorldTheme() => PlayerPrefs.GetInt(WorldThemeKey, 0);

        public static void SaveWorldTheme(int theme)
        {
            PlayerPrefs.SetInt(WorldThemeKey, theme);
            PlayerPrefs.Save();
        }

        public static GoalProgress LoadGoals()
        {
            string json = PlayerPrefs.GetString(GoalsKey, "");
            if (string.IsNullOrEmpty(json)) return new GoalProgress();
            try
            {
                return JsonUtility.FromJson<GoalProgress>(json) ?? new GoalProgress();
            }
            catch (Exception)
            {
                return new GoalProgress();
            }
        }

        public static void SaveGoals(GoalProgress goals)
        {
            PlayerPrefs.SetString(GoalsKey, JsonUtility.ToJson(goals));
            PlayerPrefs.Save();
        }

        public static float LoadMasterVolume() => PlayerPrefs.GetFloat(MasterVolumeKey, 0.8f);

        public static void SaveMasterVolume(float volume)
        {
            PlayerPrefs.SetFloat(MasterVolumeKey, volume);
            PlayerPrefs.Save();
        }

        public static bool LoadStartInCompanion() => PlayerPrefs.GetInt(StartInCompanionKey, 0) == 1;

        public static void SaveStartInCompanion(bool value)
        {
            PlayerPrefs.SetInt(StartInCompanionKey, value ? 1 : 0);
            PlayerPrefs.Save();
        }

        // Pencereyi kapatınca (X) çıkmak yerine tepsiye gizlen — varsayılan açık.
        public static bool LoadCloseToTray() => PlayerPrefs.GetInt(CloseToTrayKey, 1) == 1;

        public static void SaveCloseToTray(bool value)
        {
            PlayerPrefs.SetInt(CloseToTrayKey, value ? 1 : 0);
            PlayerPrefs.Save();
        }

        // "Arka planda çalışıyor" balonu yalnız ilk gizlenişte gösterilir.
        public static bool LoadTrayHintShown() => PlayerPrefs.GetInt(TrayHintShownKey, 0) == 1;

        public static void MarkTrayHintShown()
        {
            PlayerPrefs.SetInt(TrayHintShownKey, 1);
            PlayerPrefs.Save();
        }

        // Kart penceresi (cihaza özel): boyut ön ayarı (0 küçük, 1 orta, 2 büyük), her zaman üstte,
        // son konum (fiziksel piksel, sol-üst köşe).
        public static int LoadCardSize() => Mathf.Clamp(PlayerPrefs.GetInt(CardSizeKey, 1), 0, 2);

        public static void SaveCardSize(int preset)
        {
            PlayerPrefs.SetInt(CardSizeKey, preset);
            PlayerPrefs.Save();
        }

        public static bool LoadCardOnTop() => PlayerPrefs.GetInt(CardOnTopKey, 0) == 1;

        public static void SaveCardOnTop(bool value)
        {
            PlayerPrefs.SetInt(CardOnTopKey, value ? 1 : 0);
            PlayerPrefs.Save();
        }

        public static bool TryLoadCardPosition(out int left, out int top)
        {
            left = PlayerPrefs.GetInt(CardLeftKey, 0);
            top = PlayerPrefs.GetInt(CardTopKey, 0);
            return PlayerPrefs.HasKey(CardLeftKey) && PlayerPrefs.HasKey(CardTopKey);
        }

        public static void SaveCardPosition(int left, int top)
        {
            PlayerPrefs.SetInt(CardLeftKey, left);
            PlayerPrefs.SetInt(CardTopKey, top);
            PlayerPrefs.Save();
        }

        // İlk açılış rehberi bitti/atlandı.
        public static bool LoadOnboardingDone() => PlayerPrefs.GetInt(OnboardingDoneKey, 0) == 1;

        public static void MarkOnboardingDone()
        {
            PlayerPrefs.SetInt(OnboardingDoneKey, 1);
            PlayerPrefs.Save();
        }

        static ScrollRoadSaveData LoadLocal()
        {
            var data = new ScrollRoadSaveData
            {
                currentDistanceKm = GetDouble(CurrentDistanceKey),
                maxDistanceKm = GetDouble(MaxDistanceKey),
                raceMonth = PlayerPrefs.GetString(RaceMonthKey, ""),
                hasLastClose = PlayerPrefs.HasKey(LastCloseUnixSecondsKey),
                lifetimeKm = GetDouble(LifetimeKmKey),
                lifetimeSteps = PlayerPrefs.GetInt(LifetimeStepsKey, 0),
                lifetimeTurns = PlayerPrefs.GetInt(LifetimeTurnsKey, 0),
                cityReachedCount = PlayerPrefs.GetInt(CityReachedCountKey, 0),
                bestMonthKm = GetDouble(BestMonthKmKey),
                bestMonth = PlayerPrefs.GetString(BestMonthKey, ""),
                secretCarUnlocked = LoadSecretCarUnlocked(),
                pointsVersion = PlayerPrefs.GetInt(PointsVersionKey, 0),
                pointsRaceMonth = PlayerPrefs.GetString(PointsRaceMonthKey, ""),
                monthPoints = GetDouble(MonthPointsKey),
                monthBestPoints = GetDouble(MonthBestPointsKey),
                lifetimePoints = GetDouble(LifetimePointsKey),
                bestMonthPoints = GetDouble(BestMonthPointsKey),
                bestPointsMonth = PlayerPrefs.GetString(BestPointsMonthKey, ""),
            };

            if (data.hasLastClose &&
                long.TryParse(PlayerPrefs.GetString(LastCloseUnixSecondsKey), NumberStyles.Integer, CultureInfo.InvariantCulture, out long close))
            {
                data.lastCloseUnixSeconds = close;
            }
            else
            {
                data.hasLastClose = false;
            }

            return data;
        }

        static void WriteLocal(ScrollRoadSaveData data)
        {
            PlayerPrefs.SetString(RaceMonthKey, data.raceMonth);
            SetDouble(CurrentDistanceKey, data.currentDistanceKm);
            SetDouble(MaxDistanceKey, data.maxDistanceKm);
            SetDouble(LifetimeKmKey, data.lifetimeKm);
            PlayerPrefs.SetInt(LifetimeStepsKey, data.lifetimeSteps);
            PlayerPrefs.SetInt(LifetimeTurnsKey, data.lifetimeTurns);
            PlayerPrefs.SetInt(CityReachedCountKey, data.cityReachedCount);
            SetDouble(BestMonthKmKey, data.bestMonthKm);
            PlayerPrefs.SetString(BestMonthKey, data.bestMonth);
            PlayerPrefs.SetInt(SecretCarUnlockedKey, data.secretCarUnlocked ? 1 : 0);
            PlayerPrefs.SetString(PointsRaceMonthKey, data.pointsRaceMonth);
            SetDouble(MonthPointsKey, data.monthPoints);
            SetDouble(MonthBestPointsKey, data.monthBestPoints);
            SetDouble(LifetimePointsKey, data.lifetimePoints);
            SetDouble(BestMonthPointsKey, data.bestMonthPoints);
            PlayerPrefs.SetString(BestPointsMonthKey, data.bestPointsMonth);
            if (data.hasLastClose)
            {
                PlayerPrefs.SetString(LastCloseUnixSecondsKey, data.lastCloseUnixSeconds.ToString(CultureInfo.InvariantCulture));
            }
            PlayerPrefs.Save();
        }

        // İki cihazın kaydını çakıştırma kuralları:
        // - Baz kayıt, en son kapatılan (en güncel oynanan) cihazınki.
        // - Aynı yarış ayındaysa mesafelerde yüksek olan kazanır (high-water mark —
        //   geri gitmenin skora etkisi yok, max almak güvenli).
        static ScrollRoadSaveData Merge(ScrollRoadSaveData local, ScrollRoadSaveData cloud)
        {
            // Puan katmanından önceki bir cihazın bulut kaydı: puanları onun km'sinden türet.
            if (cloud.pointsVersion < CurrentPointsVersion)
            {
                double ppk = 1.0 / EnvironmentCatalog.Road.UnitsPerPoint;
                cloud.pointsRaceMonth = cloud.raceMonth;
                cloud.monthPoints = cloud.currentDistanceKm * ppk;
                cloud.monthBestPoints = Math.Max(cloud.maxDistanceKm * ppk, cloud.monthPoints);
                cloud.lifetimePoints = Math.Max(cloud.lifetimeKm * ppk, cloud.monthBestPoints);
                cloud.bestMonthPoints = cloud.bestMonthKm * ppk;
                cloud.bestPointsMonth = cloud.bestMonth;
                cloud.pointsVersion = CurrentPointsVersion;
            }

            long localClose = local.hasLastClose ? local.lastCloseUnixSeconds : long.MinValue;
            long cloudClose = cloud.hasLastClose ? cloud.lastCloseUnixSeconds : long.MinValue;
            var baseData = cloudClose > localClose ? cloud : local;
            var otherData = ReferenceEquals(baseData, cloud) ? local : cloud;

            var result = new ScrollRoadSaveData
            {
                currentDistanceKm = baseData.currentDistanceKm,
                maxDistanceKm = baseData.maxDistanceKm,
                raceMonth = baseData.raceMonth,
                hasLastClose = baseData.hasLastClose || otherData.hasLastClose,
                lastCloseUnixSeconds = Math.Max(localClose, cloudClose),
            };

            if (!result.hasLastClose) result.lastCloseUnixSeconds = 0;

            if (otherData.raceMonth == baseData.raceMonth)
            {
                result.currentDistanceKm = Math.Max(baseData.currentDistanceKm, otherData.currentDistanceKm);
                result.maxDistanceKm = Math.Max(baseData.maxDistanceKm, otherData.maxDistanceKm);
            }

            // Ömürlük sayaçlar iki cihazda ayrı ayrı birikir — toplamak çift saymaya yol açar,
            // max almak güvenli taraf (iki cihazda dönüşümlü oynayan biraz eksik sayılır).
            result.lifetimeKm = Math.Max(local.lifetimeKm, cloud.lifetimeKm);
            result.lifetimeSteps = Math.Max(local.lifetimeSteps, cloud.lifetimeSteps);
            result.lifetimeTurns = Math.Max(local.lifetimeTurns, cloud.lifetimeTurns);
            result.cityReachedCount = Math.Max(local.cityReachedCount, cloud.cityReachedCount);
            var best = cloud.bestMonthKm > local.bestMonthKm ? cloud : local;
            result.bestMonthKm = best.bestMonthKm;
            result.bestMonth = best.bestMonth;
            result.secretCarUnlocked = local.secretCarUnlocked || cloud.secretCarUnlocked;

            // Ortak puan: Yol mesafesiyle aynı kurallar (aynı ayda yüksek olan, ömür boyu max).
            result.pointsVersion = CurrentPointsVersion;
            result.pointsRaceMonth = baseData.pointsRaceMonth;
            result.monthPoints = baseData.monthPoints;
            result.monthBestPoints = baseData.monthBestPoints;
            if (otherData.pointsRaceMonth == baseData.pointsRaceMonth)
            {
                result.monthPoints = Math.Max(baseData.monthPoints, otherData.monthPoints);
                result.monthBestPoints = Math.Max(baseData.monthBestPoints, otherData.monthBestPoints);
            }
            result.lifetimePoints = Math.Max(local.lifetimePoints, cloud.lifetimePoints);
            var bestP = cloud.bestMonthPoints > local.bestMonthPoints ? cloud : local;
            result.bestMonthPoints = bestP.bestMonthPoints;
            result.bestPointsMonth = bestP.bestPointsMonth;

            return result;
        }

        static bool CloudAvailable()
        {
            if (!SteamManager.IsInitialized) return false;
            try
            {
                return SteamRemoteStorage.IsCloudEnabled;
            }
            catch (Exception)
            {
                return false;
            }
        }

        static ScrollRoadSaveData TryReadCloud()
        {
            try
            {
                if (!SteamRemoteStorage.FileExists(CloudFileName)) return null;
                byte[] bytes = SteamRemoteStorage.FileRead(CloudFileName);
                if (bytes == null || bytes.Length == 0) return null;
                return JsonUtility.FromJson<ScrollRoadSaveData>(Encoding.UTF8.GetString(bytes));
            }
            catch (Exception e)
            {
                WarnOnce($"Steam Cloud kaydı okunamadı, yerel kayıtla devam: {e.Message}");
                return null;
            }
        }

        static void TryWriteCloud(ScrollRoadSaveData data)
        {
            try
            {
                byte[] bytes = Encoding.UTF8.GetBytes(JsonUtility.ToJson(data));
                if (!SteamRemoteStorage.FileWrite(CloudFileName, bytes))
                {
                    WarnOnce("Steam Cloud'a yazılamadı (FileWrite false döndü) — yerel kayıt etkilenmez.");
                }
            }
            catch (Exception e)
            {
                WarnOnce($"Steam Cloud'a yazılamadı, yerel kayıt etkilenmez: {e.Message}");
            }
        }

        static void PushLocalToCloud()
        {
            if (!CloudAvailable()) return;
            TryWriteCloud(LoadLocal());
        }

        static void WarnOnce(string message)
        {
            if (cloudWarningLogged) return;
            cloudWarningLogged = true;
            Debug.LogWarning(message);
        }

        static double GetDouble(string key)
        {
            return double.TryParse(PlayerPrefs.GetString(key, "0"), NumberStyles.Float, CultureInfo.InvariantCulture, out double v) ? v : 0.0;
        }

        static void SetDouble(string key, double value)
        {
            PlayerPrefs.SetString(key, value.ToString("R", CultureInfo.InvariantCulture));
        }
    }
}
