using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ScrollRoad
{
    // Vitrin (Ortam Standardı v2 · Dayanak 2, görünür birikim; 2026-10-25): kartın sol altında, defter plakasının
    // üstünde bulunulan mekânın 6 nadirinin rafı. Bulunmamış nadir koyu silüet, bulunan renkli; yeni bulunan
    // bir an büyüyüp yerine oturur. Oyuncu karta döndüğünde rafın dolduğunu görür. Yalnız kart modunda ve
    // içerikli ortamlarda (Yol'da pasaport var). Nadir görselleri sahne kurucudan "<ortam>:<nadir>" anahtarıyla.
    public class ShowcaseView : MonoBehaviour
    {
        [SerializeField] CompanionWindowController companionWindow;
        [SerializeField] GameObject root;
        [SerializeField] Image[] slotImages;
        [SerializeField] string[] spriteKeys;
        [SerializeField] Sprite[] sprites;
        [SerializeField] float popSeconds = 1.2f;

        static readonly Color Silhouette = new Color(0.02f, 0.03f, 0.05f, 0.55f);

        readonly HashSet<string> found = new HashSet<string>();
        readonly float[] popTimers = new float[6];
        string shownKey = "";
        float refreshTimer;

        Sprite SpriteFor(EnvironmentId env, string rareId)
        {
            string key = env + ":" + rareId;
            for (int i = 0; i < spriteKeys.Length && i < sprites.Length; i++) if (spriteKeys[i] == key) return sprites[i];
            return null;
        }

        void Update()
        {
            var c = EnvironmentContent.Current;
            bool visible = c != null && (companionWindow == null || !companionWindow.IsCompanionMode);
            if (root.activeSelf != visible) root.SetActive(visible);
            if (!visible) return;

            for (int i = 0; i < slotImages.Length && i < popTimers.Length; i++)
            {
                if (popTimers[i] <= 0f) continue;
                popTimers[i] = Mathf.Max(0f, popTimers[i] - Time.unscaledDeltaTime);
                float t = 1f - popTimers[i] / popSeconds;
                float scale = 1f + 0.6f * Mathf.Sin(t * Mathf.PI) * (1f - t);
                slotImages[i].rectTransform.localScale = new Vector3(scale, scale, 1f);
            }

            refreshTimer -= Time.unscaledDeltaTime;
            if (refreshTimer > 0f) return;
            refreshTimer = 0.5f;

            var env = c.Def.Id;
            string key = env + "/" + c.VenueId;
            bool sameVenue = key == shownKey;
            if (!sameVenue) found.Clear();
            shownKey = key;

            var rares = c.RaresAt(c.VenueId);
            for (int i = 0; i < slotImages.Length; i++)
            {
                var slot = slotImages[i];
                bool exists = i < rares.Length;
                slot.transform.parent.gameObject.SetActive(exists);
                if (!exists) continue;
                var rare = rares[i];
                if (slot.sprite == null || !sameVenue) slot.sprite = SpriteFor(env, rare.Id);
                bool logged = c.IsLogged(rare.Id);
                slot.color = logged ? Color.white : Silhouette;
                if (logged && found.Add(rare.Id) && sameVenue) popTimers[i] = popSeconds; // yeni bulundu
                if (!sameVenue) slot.rectTransform.localScale = Vector3.one;
            }
        }
    }
}
