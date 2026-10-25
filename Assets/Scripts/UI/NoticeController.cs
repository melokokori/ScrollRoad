using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ScrollRoad
{
    // Durak damgası ve diğer sistemlerin (sipariş, açılma, nadir) duyuruları: üst ortada küçük, sade kart (konfeti yok —
    // o şehre özel; durak duyurusunda köşeye pasaport damgası basılır). Sırayla gösterilir; birikirse en eskiler atılır. Masaüstü modunda kart yerine
    // şeridin alt satırında kısa metin (CompanionText) görünür. Şehir kutlaması sürerken bekler.
    public class NoticeController : MonoBehaviour
    {
        [SerializeField] RouteProgress route;
        [SerializeField] CompanionWindowController companionWindow;
        [SerializeField] CelebrationController celebration;
        [SerializeField] CanvasGroup cardGroup;
        [SerializeField] TMP_Text titleText;
        [SerializeField] TMP_Text subtitleText;
        [SerializeField] Image stamp;          // durak duyurusunda köşeye basılan damga
        [SerializeField] TMP_Text stampText;
        [SerializeField] float fadeSeconds = 0.35f;
        [SerializeField] float holdSeconds = 3.5f;
        [SerializeField] int maxQueued = 3;

        [Header("Damga")]
        [SerializeField] float stampDelay = 0.3f;   // kart belirdikten sonra
        [SerializeField] float stampDropSeconds = 0.13f;
        [SerializeField] float stampSettleSeconds = 0.16f;
        [SerializeField] float stampStartScale = 1.9f;

        static readonly Color DarkInk = new Color(0.12f, 0.14f, 0.18f);
        static readonly Color LightInk = new Color(0.97f, 0.97f, 0.94f);

        struct Notice
        {
            public string Title;
            public string Subtitle;
            public string Short;
            public bool Stamped;
            public Color StampColor;
            public float StampAngle;
        }

        readonly Queue<Notice> queue = new Queue<Notice>();
        Notice current;
        float timer = -1f; // eksi = boşta

        public string CompanionText => timer >= 0f ? current.Short : null;
        public float CompanionAlpha { get; private set; }

        void Awake()
        {
            cardGroup.alpha = 0f;
            cardGroup.gameObject.SetActive(false);
            route.StopReached += HandleStop;
        }

        void OnDestroy()
        {
            if (route != null) route.StopReached -= HandleStop;
        }

        void HandleStop(RouteMap.Stop stop)
        {
            bool region = stop.Kind == RouteMap.StopKind.Region;
            int index = 0;
            while (index < RouteMap.Stops.Count && RouteMap.Stops[index].Name != stop.Name) index++;
            Enqueue(new Notice
            {
                Title = stop.Name,
                Subtitle = region ? "New region · passport stamped" : "Passport stamped",
                Short = region ? $"{stop.Name} · new region" : $"{stop.Name} · stamped",
                Stamped = true,
                StampColor = RouteMap.Regions[stop.RegionIndex].NearLand,
                // Pasaporttaki damgayla aynı eğik açı ailesi, köşede biraz daha belirgin.
                StampAngle = -6f + ((index * 37) % 9 - 4) * 0.8f,
            });
        }

        // Diğer sistemlerin duyuruları (ör. yeni araç açıldı). shortText: şerit modundaki kısa satır.
        public void Announce(string title, string subtitle, string shortText) =>
            Enqueue(new Notice { Title = title, Subtitle = subtitle, Short = shortText });

        void Enqueue(Notice notice)
        {
            queue.Enqueue(notice);
            while (queue.Count > maxQueued) queue.Dequeue();
        }

        void Update()
        {
            if (timer < 0f)
            {
                if (queue.Count == 0 || (celebration != null && celebration.IsShowing)) return;
                current = queue.Dequeue();
                titleText.text = current.Title;
                subtitleText.text = current.Subtitle;
                timer = 0f;
                if (stamp != null)
                {
                    stamp.gameObject.SetActive(false);
                    if (current.Stamped)
                    {
                        var c = current.StampColor;
                        stamp.color = c;
                        float lum = c.r * 0.3f + c.g * 0.59f + c.b * 0.11f;
                        stampText.color = lum > 0.6f ? DarkInk : LightInk;
                    }
                }
            }

            timer += Time.unscaledDeltaTime;
            float alpha =
                timer < fadeSeconds ? timer / fadeSeconds :
                timer < fadeSeconds + holdSeconds ? 1f :
                Mathf.Clamp01(1f - (timer - fadeSeconds - holdSeconds) / fadeSeconds);

            bool companion = companionWindow != null && companionWindow.IsCompanionMode;
            if (cardGroup.gameObject.activeSelf == companion) cardGroup.gameObject.SetActive(!companion);
            cardGroup.alpha = alpha;
            CompanionAlpha = alpha;
            if (current.Stamped && !companion) AnimateStamp(timer - fadeSeconds - stampDelay);

            if (timer >= fadeSeconds * 2f + holdSeconds)
            {
                timer = -1f;
                CompanionAlpha = 0f;
                cardGroup.gameObject.SetActive(false);
            }
        }

        // Damga yukarıdan büyük ve saydam gelir, hızlanarak "küt" diye iner (kart bir an basılır),
        // sonra hafifçe sekip yerine oturur.
        void AnimateStamp(float t)
        {
            if (stamp == null) return;
            bool visible = t >= 0f;
            if (stamp.gameObject.activeSelf != visible) stamp.gameObject.SetActive(visible);
            var cardRect = (RectTransform)cardGroup.transform;
            if (!visible)
            {
                cardRect.localScale = Vector3.one;
                return;
            }

            float scale, alpha, cardPress = 0f;
            if (t < stampDropSeconds)
            {
                float k = t / stampDropSeconds;
                scale = Mathf.Lerp(stampStartScale, 1f, k * k); // hızlanarak iner
                alpha = Mathf.Clamp01(k * 2.5f);
            }
            else
            {
                float k = Mathf.Clamp01((t - stampDropSeconds) / stampSettleSeconds);
                scale = 1f - 0.08f * Mathf.Sin(k * Mathf.PI) * (1f - k); // küçük sekme
                alpha = 1f;
                cardPress = 0.02f * (1f - k) * (1f - k);                  // tabela darbeyle bir an basılır
            }
            var rect = stamp.rectTransform;
            rect.localScale = new Vector3(scale, scale, 1f);
            rect.localRotation = Quaternion.Euler(0f, 0f, current.StampAngle + (scale - 1f) * 6f);
            var c = stamp.color;
            stamp.color = new Color(c.r, c.g, c.b, alpha);
            stampText.alpha = alpha;
            cardRect.localScale = new Vector3(1f - cardPress, 1f - cardPress, 1f);
        }
    }
}
