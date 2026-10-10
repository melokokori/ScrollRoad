using UnityEngine;

namespace ScrollRoad
{
    // Şehrin ötesindeki belgelenmemiş bölge. UI'da HİÇ bahsedilmez (organik keşif): eşikler
    // geçildikçe kısa fısıltı metinleri belirip kaybolur. Bu bileşen yalnız mantığı tutar;
    // metni normal HUD ve masaüstü arkadaşı katmanı (CompanionHudController) kendisi çizer.
    // Gizli bölgenin diğer izleri: gökyüzü morla kararır, yol rengi değişir (RoadSceneView),
    // tik sesi kalınlaşır (SoundController). Şehrin secretCarKmBeyond km ötesinde garajda gizli
    // araç kalıcı olarak açılır; tek ipucu ardından gelen fısıltıdır.
    public class SecretZoneView : MonoBehaviour
    {
        [SerializeField] RoadDrive drive;
        [SerializeField] float fadeSeconds = 1.5f;
        [SerializeField] float holdSeconds = 5f;
        [SerializeField] float secretCarKmBeyond = 120f;

        public static readonly Color WhisperColor = new Color(0.88f, 0.80f, 0.97f);

        // Şehrin kaç km ötesinde hangi metin (küçük harf — fısıltı estetiği).
        static readonly (float kmBeyond, string text)[] Whispers =
        {
            (10f, "still going?"),
            (30f, "the city is behind you."),
            (60f, "this place isn't on the map."),
            (120f, "no one has come this far."),
            (126f, "something is waiting for you in your garage."),
            (250f, "those who turn back lose nothing."),
            (500f, "those who keep going find something."),
            (1000f, "the road never ends. neither should you."),
        };

        readonly bool[] shown = new bool[Whispers.Length];
        bool baselineSet;
        float timer = -1f; // eksi = metin kapalı
        bool secretCarUnlocked;

        void Start() => secretCarUnlocked = SaveService.LoadSecretCarUnlocked();

        public string CurrentText { get; private set; } = "";
        public float CurrentAlpha { get; private set; }

        void Update()
        {
            float beyondKm = (float)(drive.CurrentDistanceKmPrecise - drive.CityDistanceKm);

            // Eşik yüklemede zaten geçilmişse de (eski kayıt) açılır.
            if (!secretCarUnlocked && beyondKm >= secretCarKmBeyond)
            {
                secretCarUnlocked = true;
                SaveService.UnlockSecretCar();
            }

            // Yükleme anında zaten geçilmiş eşikler yeniden fısıldamasın.
            if (!baselineSet)
            {
                baselineSet = true;
                for (int i = 0; i < Whispers.Length; i++) shown[i] = Whispers[i].kmBeyond <= beyondKm;
                return;
            }

            for (int i = 0; i < Whispers.Length; i++)
            {
                if (shown[i] || beyondKm < Whispers[i].kmBeyond) continue;
                shown[i] = true;
                CurrentText = Whispers[i].text;
                timer = 0f;
            }

            if (timer < 0f) { CurrentAlpha = 0f; return; }

            timer += Time.deltaTime;
            CurrentAlpha =
                timer < fadeSeconds ? timer / fadeSeconds :
                timer < fadeSeconds + holdSeconds ? 1f :
                Mathf.Clamp01(1f - (timer - fadeSeconds - holdSeconds) / fadeSeconds);

            if (timer >= fadeSeconds * 2f + holdSeconds)
            {
                timer = -1f;
                CurrentAlpha = 0f;
            }
        }
    }
}
