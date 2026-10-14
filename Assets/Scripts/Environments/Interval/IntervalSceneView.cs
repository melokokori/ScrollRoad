using UnityEngine;

namespace ScrollRoad
{
    // Eşit aralıklı ortamların sahnelerinin ortak iskeleti (Rüzgârgülü'nün WindSceneView'undan çıkarıldı):
    // kök nesne, düz renk sprite yardımcıları, kart/şerit kamerası. Alt sınıf yalnız kendi sahnesini çizer
    // (Draw). Sahne ortamın konumunun SAF fonksiyonu olmalı (IntervalEnvironment.Units / Phase / Position).
    // Yalnız kendi ortamı seçiliyken çizer ve kamerayı yönetir.
    public abstract class IntervalSceneView : MonoBehaviour
    {
        [SerializeField] protected CompanionWindowController companionWindow;
        [SerializeField] protected Camera viewCamera;
        [SerializeField] protected Material spriteMaterial;

        protected Transform root;
        protected Sprite whiteSprite, circleSprite;

        // Kartın görünen bandı (Rüzgârgülü ile aynı: 800x340 kartta x ≈ ±7,65).
        protected const float CardTop = 2.4f;
        protected const float CardBottom = -4.1f;

        protected abstract IntervalEnvironment Env { get; }
        protected abstract string RootName { get; }
        protected abstract void Build();

        // strip: görev çubuğu şeridi; left/right/top/bottom: kameranın gördüğü dünya kenarları.
        protected abstract void Draw(bool strip, bool night, float left, float right, float top, float bottom);

        // Şeritte kameranın dikey bandı (dünya birimi) ve bandın şeridin hangi yatay noktasına (0..1) hangi
        // dünya x'inin geleceği. Solda sayı/durum yazısı var: sahne ~%32'den sağa çizilir.
        protected abstract void StripBand(out float bottom, out float top, out float anchorWorldX, out float anchorScreenX);

        protected bool IsCompanion => companionWindow != null && companionWindow.IsCompanionMode;

        protected virtual void Awake()
        {
            if (viewCamera == null) viewCamera = Camera.main;
            whiteSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
            circleSprite = CreateCircleSprite(64);
            root = new GameObject(RootName).transform;
            root.SetParent(transform, false);
            Build();
            root.gameObject.SetActive(false);
        }

        bool Show()
        {
            bool active = Env != null && EnvironmentManager.Current.Id == Env.Def.Id;
            if (root.gameObject.activeSelf != active) root.gameObject.SetActive(active);
            return active;
        }

        void LateUpdate()
        {
            if (!Show()) return;
            bool strip = IsCompanion;
            bool night = WorldTheme.Current == WorldTheme.Kind.Night;

            float aspect = Mathf.Max(0.1f, viewCamera.aspect);
            float size, camX, camY;
            if (strip)
            {
                StripBand(out float bottom, out float top, out float anchorX, out float anchorScreen);
                float band = 1f - (companionWindow != null ? companionWindow.StripOverflowFraction : 0f);
                size = (top - bottom) / 2f / Mathf.Max(0.2f, band);
                camY = bottom + size;
                camX = anchorX - (anchorScreen * 2f - 1f) * size * aspect;
            }
            else
            {
                size = (CardTop - CardBottom) / 2f;
                camY = (CardTop + CardBottom) / 2f;
                camX = 0f;
            }
            viewCamera.orthographicSize = size;
            viewCamera.transform.position = new Vector3(camX, camY, -10f);
            float halfW = size * aspect;
            Draw(strip, night, camX - halfW, camX + halfW, camY + size, camY - size);
        }

        // Kartta gökyüzü rengi (temaya göre), şeritte saydam.
        protected void Background(bool strip, Color dayColor)
        {
            var sky = WorldTheme.Sky(dayColor);
            sky.a = 1f;
            viewCamera.backgroundColor = strip ? new Color(0f, 0f, 0f, 0f) : sky;
        }

        // ---------------------------------------------------------------- yardımcılar

        protected SpriteRenderer Create(string name, Sprite sprite, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(root, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
            if (spriteMaterial != null) sr.sharedMaterial = spriteMaterial;
            return sr;
        }

        protected SpriteRenderer[] CreatePool(string name, Sprite sprite, int order, int count)
        {
            var pool = new SpriteRenderer[count];
            for (int i = 0; i < count; i++) pool[i] = Create(name + i, sprite, order);
            return pool;
        }

        protected static Sprite CreateCircleSprite(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            float r = size / 2f - 1f;
            var c = new Vector2(size / 2f - 0.5f, size / 2f - 0.5f);
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float a = Mathf.Clamp01(r - Vector2.Distance(new Vector2(x, y), c) + 0.5f);
                px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
            }
            tex.SetPixels32(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        protected static Color Opaque(Color c)
        {
            c.a = 1f;
            return c;
        }

        protected static Color WithAlpha(Color c, float a)
        {
            c.a = a;
            return c;
        }

        protected static float Smooth(float a, float b, float x)
        {
            float t = Mathf.Clamp01((x - a) / Mathf.Max(1e-5f, b - a));
            return t * t * (3f - 2f * t);
        }

        protected void PlaceRect(SpriteRenderer sr, float x, float y, float w, float h, Color color, float angle = 0f)
        {
            sr.enabled = true;
            sr.transform.localPosition = new Vector3(x, y, 0f);
            sr.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
            var b = sr.sprite.bounds.size;
            sr.transform.localScale = new Vector3(w / b.x, h / b.y, 1f);
            sr.color = color;
        }

        protected void PlaceSpan(SpriteRenderer sr, float x0, float x1, float y0, float y1, Color color)
        {
            sr.enabled = x1 > x0 && y1 > y0;
            if (!sr.enabled) return;
            PlaceRect(sr, (x0 + x1) / 2f, (y0 + y1) / 2f, x1 - x0, y1 - y0, color);
        }

        protected void PlaceLine(SpriteRenderer sr, Vector2 a, Vector2 b, float width, Color color)
        {
            var d = b - a;
            float len = d.magnitude;
            sr.enabled = len > 0.005f;
            if (!sr.enabled) return;
            sr.transform.localPosition = (Vector3)((a + b) / 2f);
            sr.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
            var bs = sr.sprite.bounds.size;
            sr.transform.localScale = new Vector3(len / bs.x, width / bs.y, 1f);
            sr.color = color;
        }

        // Sprite'ı yüksekliği h olacak şekilde, alt ortası (x, y)'de, açıyla yerleştirir (pivot ortada varsayılır).
        protected void PlaceSprite(SpriteRenderer sr, Sprite sprite, float x, float y, float h, Color color, float angle = 0f, bool flipX = false)
        {
            sr.sprite = sprite != null ? sprite : whiteSprite;
            sr.enabled = h > 0.001f && color.a > 0.004f;
            if (!sr.enabled) return;
            var bs = sr.sprite.bounds.size;
            float s = h / Mathf.Max(0.001f, bs.y);
            sr.transform.localScale = new Vector3(flipX ? -s : s, s, 1f);
            float rad = angle * Mathf.Deg2Rad;
            // Pivot ortada: yukarı yönde h/2 kaydır.
            var up = new Vector2(-Mathf.Sin(rad), Mathf.Cos(rad));
            sr.transform.localPosition = new Vector3(x + up.x * h / 2f, y + up.y * h / 2f, 0f);
            sr.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
            sr.color = color;
        }

        // Sprite'ın görünen genişliği (yükseklik h iken).
        protected static float WidthAt(Sprite sprite, float h)
        {
            if (sprite == null) return h;
            var bs = sprite.bounds.size;
            return h * bs.x / Mathf.Max(0.001f, bs.y);
        }
    }
}
