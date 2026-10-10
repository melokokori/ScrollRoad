using UnityEngine;
using UnityEngine.UI;

namespace ScrollRoad
{
    // Ekran üstü bilgi katmanı (normal pencere modu): hız, mesafe, ay rekoru; altta şehre ilerleme
    // çizgisi (dolgu = anlık konum, işaret = bu ayki en uzak nokta / skor); gizli bölge fısıltısı.
    // Masaüstü arkadaşı modunda tamamen gizlenir.
    // Yazı rengi gökyüzünün parlaklığına göre koyu/açık seçilir (gizli bölgede gökyüzü kararıyor).
    public class HudController : MonoBehaviour
    {
        [SerializeField] RoadDrive drive;
        [SerializeField] RoadSceneView view;
        [SerializeField] SecretZoneView secretZone;
        [SerializeField] CompanionWindowController companionWindow;
        [SerializeField] GameObject hudRoot;
        [SerializeField] Text speedText;
        [SerializeField] Text distanceText;
        [SerializeField] Text recordText;
        [SerializeField] Text cityText;
        [SerializeField] Text whisperText;
        [SerializeField] Image progressTrack;
        [SerializeField] RectTransform progressFill;
        [SerializeField] RectTransform maxMarker;
        [SerializeField] Graphic[] inkGraphics; // gökyüzüne göre renk değiştiren ikincil öğeler

        static readonly Color DarkInk = new Color(0.16f, 0.19f, 0.24f);
        static readonly Color LightInk = new Color(0.93f, 0.91f, 0.97f);

        void Update()
        {
            bool companion = companionWindow != null && companionWindow.IsCompanionMode;
            if (hudRoot.activeSelf == companion) hudRoot.SetActive(!companion);
            if (companion) return;

            var sky = view != null ? view.CurrentSkyColor : Color.white;
            float lum = sky.r * 0.3f + sky.g * 0.59f + sky.b * 0.11f;
            var ink = lum > 0.5f ? DarkInk : LightInk;

            speedText.text = $"{drive.CurrentSpeedKmh:0} km/sa";
            distanceText.text = Fmt.Km(drive.CurrentDistanceKmPrecise);
            recordText.text = "Ay rekoru: " + Fmt.Km(drive.MaxDistanceKmPrecise);

            // Şehrin ötesi UI'da asla anılmaz: ulaşıldıktan sonra yalnız "ulaşıldı" yazar.
            double remaining = drive.CityDistanceKm - drive.CurrentDistanceKmPrecise;
            cityText.text = drive.ReachedCity ? "Şehre ulaşıldı" : "Şehre " + Fmt.Km(System.Math.Max(0, remaining), 0);

            speedText.color = distanceText.color = recordText.color = cityText.color = ink;
            if (inkGraphics != null)
            {
                foreach (var g in inkGraphics) if (g != null) g.color = new Color(ink.r, ink.g, ink.b, g.color.a);
            }
            progressTrack.color = new Color(ink.r, ink.g, ink.b, 0.18f);

            float progress = Mathf.Clamp01((float)(drive.CurrentDistanceKmPrecise / drive.CityDistanceKm));
            progressFill.anchorMax = new Vector2(progress, 1f);
            float maxProgress = Mathf.Clamp01((float)(drive.MaxDistanceKmPrecise / drive.CityDistanceKm));
            maxMarker.anchorMin = new Vector2(maxProgress, 0f);
            maxMarker.anchorMax = new Vector2(maxProgress, 1f);

            if (secretZone != null)
            {
                whisperText.text = secretZone.CurrentText;
                var w = SecretZoneView.WhisperColor;
                whisperText.color = new Color(w.r, w.g, w.b, secretZone.CurrentAlpha);
            }
        }
    }
}
