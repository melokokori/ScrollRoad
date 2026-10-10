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

        // Cihaza özel tercihler — cloud'a bilerek dahil edilmez.
        const string MasterVolumeKey = "ScrollRoad_MasterVolume";
        const string StartInCompanionKey = "ScrollRoad_StartInCompanion";
        const string CloseToTrayKey = "ScrollRoad_CloseToTray";
        const string TrayHintShownKey = "ScrollRoad_TrayHintShown";
        const string OnboardingDoneKey = "ScrollRoad_OnboardingDone";

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
