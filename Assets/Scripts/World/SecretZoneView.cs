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
            (10f, "hâlâ mı gidiyorsun?"),
            (30f, "şehir arkanda kaldı."),
            (60f, "burası haritada yok."),
            (120f, "kimse bu kadar uzağa gelmedi."),
            (126f, "garajında seni bir şey bekliyor."),
            (250f, "geri dönenler bir şey kaybetmez."),
            (500f, "devam edenler bir şey bulur."),
            (1000f, "yol bitmez. sen de bitme."),
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
