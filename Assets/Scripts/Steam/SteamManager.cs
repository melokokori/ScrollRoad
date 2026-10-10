using System;
using Steamworks;
using UnityEngine;

namespace ScrollRoad
{
    // Steam bağlantısı. Sahne düzenlemesi gerektirmez: ilk sahne yüklenmeden kendini kurar
    // (SaveService'in cloud birleştirmesi RoadDrive.Start'tan önce Steam'i hazır bulsun diye).
    // Steam kapalı/kurulu değilse IsInitialized=false kalır ve oyun yerel kayıtla sorunsuz çalışır.
    [DefaultExecutionOrder(-200)]
    public class SteamManager : MonoBehaviour
    {
        // Henüz gerçek bir Steamworks App ID yok — Valve'ın herkese açık test app'i (Spacewar).
        // Gerçek App ID alınınca burası ve proje kökündeki steam_appid.txt değişmeli.
        public const uint TestAppId = 480;

        public static bool IsInitialized { get; private set; }
        public static string StatusText { get; private set; } = "Steam not connected";

        static SteamManager instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Bootstrap()
        {
            if (instance != null) return;
            var go = new GameObject(nameof(SteamManager));
            DontDestroyOnLoad(go);
            go.AddComponent<SteamManager>();
        }

        void Awake()
        {
            if (instance != null && instance != this) { Destroy(gameObject); return; }
            instance = this;

            try
            {
                // Editor'de domain reload kapalıysa önceki Play oturumundan kalan bağlantı geçerli olabilir.
                if (!SteamClient.IsValid) SteamClient.Init(TestAppId, asyncCallbacks: false);
                IsInitialized = SteamClient.IsValid;
                StatusText = IsInitialized ? $"Steam: {SteamClient.Name}" : "Steam not connected";
                if (IsInitialized) Debug.Log($"Steam bağlandı: {SteamClient.Name}, ülke: {SteamUtils.IpCountry}");
            }
            catch (Exception e)
            {
                IsInitialized = false;
                StatusText = "Steam not connected";
                Debug.Log($"Steam başlatılamadı, yerel modda devam (Steam açık ve giriş yapılmış mı?): {e.Message}");
            }
        }

        void Update()
        {
            if (!IsInitialized) return;
            try
            {
                SteamClient.RunCallbacks();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Steam callback hatası: {e.Message}");
            }
        }

        // OnApplicationQuit yerine OnDestroy: diğer bileşenler kapanışta (OnApplicationQuit'te)
        // kaydı cloud'a yazıp skoru gönderebilsin; Steam bütün bunlardan sonra kapanır.
        void OnDestroy()
        {
            if (instance != this || !IsInitialized) return;
            SteamClient.Shutdown();
            IsInitialized = false;
        }
    }
}
