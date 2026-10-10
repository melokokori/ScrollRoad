using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ScrollRoad
{
    // Araç görselleri çalışma zamanında üretilir: oyunda her modelin yalnız TABAN görseli var
    // (StreamingAssets/Vehicles/<model>.png; boyanabilirse mercan gövdeli). Renk varyantları ve
    // kilit silüetleri istendiği anda bu tabandan türetilir, son kullanılanlar önbellekte tutulur.
    // Önceden 67 model × 10 renk hazır doku olarak gömülüyordu: build ~475 MB'tı ve katalog sahnede
    // olduğu için hepsi açılışta belleğe yükleniyordu (mipmap'li NPOT dokular sıkıştırılmıyor).
    //
    // Renk algoritması eski Tools/make_car_variants.py ile aynı: gövde = ton 8° civarı (±22°) ve
    // doygunluk > bodyMinSat olan pikseller; yeni renk mercana göre ORANSAL uygulanır (gölgeli alt
    // gövde aynı oranda koyu kalır). Cam (ton ~200°) yalnız camı da boyayan renklerde değişir.
    public static class VehicleArt
    {
        const float BaseH = 8f / 360f, BaseS = 0.66f, BaseV = 0.95f, BodyHueRange = 22f / 360f;
        const float GlassH = 200f / 360f, GlassHueRange = 25f / 360f, GlassS = 0.34f, GlassV = 0.95f;
        const float PixelsPerUnit = 100f;
        const int CacheSize = 48; // garaj sayfası (5) + önizleme + yoldaki araç rahatça sığar

        static readonly Dictionary<string, LinkedListNode<(string key, Sprite sprite)>> cache = new();
        static readonly LinkedList<(string key, Sprite sprite)> order = new(); // baş = en son kullanılan
        static readonly HashSet<string> missing = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetSession()
        {
            cache.Clear();
            order.Clear();
            missing.Clear();
        }

        public static string BasePath(string id) => Path.Combine(Application.streamingAssetsPath, "Vehicles", id + ".png");

        // paint null ise taban renkleriyle (boyanmamış) döner.
        public static Sprite Get(string id, CarCatalog.Paint paint, float bodyMinSat)
        {
            string key = paint == null ? id : $"{id}|{paint.id}";
            return Cached(key) ?? Store(key, Build(id, pixels => { if (paint != null) Recolor(pixels, paint, bodyMinSat); }, dilate: true));
        }

        public static Sprite Silhouette(string id)
        {
            string key = id + "|silhouette";
            return Cached(key) ?? Store(key, Build(id, pixels =>
            {
                for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color32(255, 255, 255, pixels[i].a);
            }, dilate: false));
        }

        static Sprite Cached(string key)
        {
            if (!cache.TryGetValue(key, out var node)) return null;
            if (node.Value.sprite == null) // sahne kapanırken yok edilmiş olabilir
            {
                order.Remove(node);
                cache.Remove(key);
                return null;
            }
            order.Remove(node);
            order.AddFirst(node);
            return node.Value.sprite;
        }

        static Sprite Store(string key, Sprite sprite)
        {
            if (sprite == null) return null;
            cache[key] = order.AddFirst((key, sprite));
            while (order.Count > CacheSize)
            {
                var last = order.Last.Value;
                order.RemoveLast();
                cache.Remove(last.key);
                if (last.sprite != null)
                {
                    Object.Destroy(last.sprite.texture);
                    Object.Destroy(last.sprite);
                }
            }
            return sprite;
        }

        static Sprite Build(string id, System.Action<Color32[]> process, bool dilate)
        {
            string path = BasePath(id);
            if (!File.Exists(path))
            {
                if (missing.Add(id)) Debug.LogWarning($"[VehicleArt] Görsel yok: {path}");
                return null;
            }

            var decoded = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!decoded.LoadImage(File.ReadAllBytes(path)))
            {
                Object.Destroy(decoded);
                if (missing.Add(id)) Debug.LogWarning($"[VehicleArt] Okunamadı: {path}");
                return null;
            }
            int w = decoded.width, h = decoded.height;
            var pixels = decoded.GetPixels32();
            Object.Destroy(decoded);

            process(pixels);
            if (dilate) DilateIntoTransparent(pixels, w, h, 4);

            // Mipmap şart: araçlar garaj yuvasında ve görev çubuğu şeridinde 6-8 kat küçük çiziliyor,
            // mipmapsiz ince kenarlar (korkuluk, direk) pürüzlü görünüyordu.
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, true)
            {
                name = "Vehicle_" + id,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };
            tex.SetPixels32(pixels);
            tex.Apply(true, true); // CPU kopyası bırakılır
            var sprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), PixelsPerUnit, 0, SpriteMeshType.FullRect);
            sprite.name = tex.name;
            return sprite;
        }

        static void Recolor(Color32[] pixels, CarCatalog.Paint paint, float bodyMinSat)
        {
            Color.RGBToHSV(paint.swatch, out float th, out float ts, out float tv);
            float gh = 0f, gs = 0f, gv = 0f;
            if (paint.tintGlass) Color.RGBToHSV(paint.glass, out gh, out gs, out gv);

            for (int i = 0; i < pixels.Length; i++)
            {
                var c = pixels[i];
                if (c.a == 0) continue;
                Color.RGBToHSV(c, out float ph, out float ps, out float pv);
                float nh, ns, nv;
                if (ps > bodyMinSat && HueDistance(ph, BaseH) < BodyHueRange)
                {
                    nh = th; ns = Mathf.Min(1f, ps * ts / BaseS); nv = Mathf.Min(1f, pv * tv / BaseV);
                }
                else if (paint.tintGlass && ps > 0.15f && HueDistance(ph, GlassH) < GlassHueRange)
                {
                    nh = gh; ns = Mathf.Min(1f, ps * gs / GlassS); nv = Mathf.Min(1f, pv * gv / GlassV);
                }
                else continue;
                Color32 n = Color.HSVToRGB(nh, ns, nv);
                n.a = c.a;
                pixels[i] = n;
            }
        }

        static float HueDistance(float a, float b)
        {
            float d = Mathf.Abs(a - b) % 1f;
            return Mathf.Min(d, 1f - d);
        }

        // Saydam piksellerin rengini komşu opak piksellerden doldurur (alfa 0 kalır): yoksa bilinear
        // süzme ve mipmap'ler kenarlara siyah (0,0,0) karıştırıp koyu hale bırakır. Unity'nin
        // alphaIsTransparency içe aktarma ayarının yaptığı işin aynısı.
        static void DilateIntoTransparent(Color32[] px, int w, int h, int passes)
        {
            var filled = new bool[px.Length];
            for (int i = 0; i < px.Length; i++) filled[i] = px[i].a > 0;
            var next = new List<int>();
            for (int pass = 0; pass < passes; pass++)
            {
                next.Clear();
                for (int y = 0; y < h; y++)
                {
                    for (int x = 0; x < w; x++)
                    {
                        int i = y * w + x;
                        if (filled[i]) continue;
                        int r = 0, g = 0, b = 0, n = 0;
                        if (x > 0 && filled[i - 1]) { var c = px[i - 1]; r += c.r; g += c.g; b += c.b; n++; }
                        if (x < w - 1 && filled[i + 1]) { var c = px[i + 1]; r += c.r; g += c.g; b += c.b; n++; }
                        if (y > 0 && filled[i - w]) { var c = px[i - w]; r += c.r; g += c.g; b += c.b; n++; }
                        if (y < h - 1 && filled[i + w]) { var c = px[i + w]; r += c.r; g += c.g; b += c.b; n++; }
                        if (n == 0) continue;
                        px[i] = new Color32((byte)(r / n), (byte)(g / n), (byte)(b / n), 0);
                        next.Add(i);
                    }
                }
                if (next.Count == 0) break;
                foreach (int i in next) filled[i] = true;
            }
        }
    }
}
