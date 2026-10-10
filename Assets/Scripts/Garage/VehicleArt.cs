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
        static readonly Dictionary<string, Wheel[]> wheelCache = new();
        static readonly Dictionary<string, Vector2?> rearLights = new();
        static Dictionary<string, Vector4[]> wheelTable;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetSession()
        {
            cache.Clear();
            order.Clear();
            missing.Clear();
            wheelCache.Clear();
            rearLights.Clear();
            wheelTable = null;
        }

        // Dönen jant işareti: görseldeki tekerleğin göbeğine oturan, üç koyu dilimli saydam disk.
        // Boyalı tekerlekler düz renkli (dönüşleri görünmez); işaret döndükçe tekerlek dönüyor görünür.
        // Konum ve yarıçaplar araç görselinin yerel biriminde (merkez pivot, PixelsPerUnit).
        public struct Wheel
        {
            public Sprite mark;
            public Vector2 center;
            public float tireRadius;
        }

        static readonly Wheel[] NoWheels = new Wheel[0];

        public static Wheel[] Wheels(string id)
        {
            if (wheelCache.TryGetValue(id, out var cached) && (cached.Length == 0 || cached[0].mark != null)) return cached;
            var table = LoadWheelTable();
            if (!table.TryGetValue(id, out var defs) || !File.Exists(BasePath(id))) return wheelCache[id] = NoWheels;

            var decoded = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!decoded.LoadImage(File.ReadAllBytes(BasePath(id))))
            {
                Object.Destroy(decoded);
                return wheelCache[id] = NoWheels;
            }
            int w = decoded.width, h = decoded.height;
            var pixels = decoded.GetPixels32();
            Object.Destroy(decoded);

            var wheels = new Wheel[defs.Length];
            rearLights[id] = FindRearLight(pixels, w, h, defs);
            for (int i = 0; i < defs.Length; i++)
            {
                var d = defs[i]; // x, y (oran), r, hub (genişliğe oran)
                float cx = d.x * w, cy = d.y * h, hubPx = d.w * w;
                wheels[i] = new Wheel
                {
                    mark = BuildHubMark($"{id}_wheel{i}", SampleHub(pixels, w, h, cx, cy, hubPx), hubPx),
                    center = new Vector2((cx - w / 2f) / PixelsPerUnit, (cy - h / 2f) / PixelsPerUnit),
                    tireRadius = d.z * w / PixelsPerUnit,
                };
            }
            return wheelCache[id] = wheels;
        }

        // Stop lambası (geri viteste/frende yanar): arka tekerleğin biraz üstündeki satırda aracın arka
        // kenarı. Görseller sağa bakıyor, arka = sol. Yerel birimde (Wheel.center gibi); yoksa null.
        public static Vector2? RearLight(string id)
        {
            Wheels(id);
            return rearLights.TryGetValue(id, out var p) ? p : null;
        }

        static Vector2? FindRearLight(Color32[] px, int w, int h, Vector4[] defs)
        {
            if (defs.Length == 0) return null;
            var rear = defs[0];
            foreach (var d in defs) if (d.x < rear.x) rear = d;
            float rPx = rear.z * w;
            int y = Mathf.Clamp(Mathf.RoundToInt(rear.y * h + rPx * 1.35f), 0, h - 1);
            for (int x = 0; x < w; x++)
            {
                if (px[y * w + x].a < 128) continue;
                float lx = x + Mathf.Max(3f, rPx * 0.15f);
                return new Vector2((lx - w / 2f) / PixelsPerUnit, (y - h / 2f) / PixelsPerUnit);
            }
            return null;
        }

        // wheels.txt (Tools/find_wheels.py): "<model> x y r hub; x y r hub".
        static Dictionary<string, Vector4[]> LoadWheelTable()
        {
            if (wheelTable != null) return wheelTable;
            wheelTable = new Dictionary<string, Vector4[]>();
            string path = Path.Combine(Application.streamingAssetsPath, "Vehicles", "wheels.txt");
            if (!File.Exists(path)) return wheelTable;
            var inv = System.Globalization.CultureInfo.InvariantCulture;
            foreach (var raw in File.ReadAllLines(path))
            {
                string line = raw.Trim();
                if (line.Length == 0 || line[0] == '#') continue;
                int space = line.IndexOf(' ');
                if (space < 0) continue;
                var parts = line.Substring(space + 1).Split(';');
                var list = new List<Vector4>();
                foreach (var part in parts)
                {
                    var v = part.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
                    if (v.Length < 4) continue;
                    list.Add(new Vector4(float.Parse(v[0], inv), float.Parse(v[1], inv), float.Parse(v[2], inv), float.Parse(v[3], inv)));
                }
                wheelTable[line.Substring(0, space)] = list.ToArray();
            }
            return wheelTable;
        }

        // Göbeğin ortalama rengi (merkez çevresi, saydam olmayan pikseller).
        static Color SampleHub(Color32[] px, int w, int h, float cx, float cy, float hubPx)
        {
            float r = Mathf.Max(1f, hubPx * 0.6f);
            Vector3 sum = Vector3.zero;
            int n = 0;
            for (int y = Mathf.FloorToInt(cy - r); y <= Mathf.CeilToInt(cy + r); y++)
            for (int x = Mathf.FloorToInt(cx - r); x <= Mathf.CeilToInt(cx + r); x++)
            {
                if (x < 0 || y < 0 || x >= w || y >= h) continue;
                if ((x - cx) * (x - cx) + (y - cy) * (y - cy) > r * r) continue;
                var c = px[y * w + x];
                if (c.a < 200) continue;
                sum += new Vector3(c.r, c.g, c.b);
                n++;
            }
            if (n == 0) return new Color(0.7f, 0.72f, 0.75f);
            sum /= n * 255f;
            return new Color(sum.x, sum.y, sum.z);
        }

        // Göbekte üç dilim (merkez kapağı boş kalır); 4x4 alt örneklemeyle yumuşak kenarlı.
        static Sprite BuildHubMark(string name, Color hub, float hubPx)
        {
            int size = Mathf.Max(8, Mathf.CeilToInt(hubPx * 2f) + 2);
            float c = size / 2f, outer = hubPx * 0.92f, inner = hubPx * 0.22f;
            const float wedge = 40f;
            var color = new Color(hub.r * 0.42f, hub.g * 0.42f, hub.b * 0.46f); // yolda göbek ~8 px: belirgin olmalı
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                int hits = 0;
                for (int sy = 0; sy < 4; sy++)
                for (int sx = 0; sx < 4; sx++)
                {
                    float dx = x + (sx + 0.5f) / 4f - c, dy = y + (sy + 0.5f) / 4f - c;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    if (d > outer || d < inner) continue;
                    float ang = (Mathf.Atan2(dy, dx) * Mathf.Rad2Deg + 360f) % 120f;
                    if (ang < wedge) hits++;
                }
                pixels[y * size + x] = new Color32((byte)(color.r * 255), (byte)(color.g * 255), (byte)(color.b * 255), (byte)(hits * 255 * 0.95f / 16f));
            }
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, true)
            {
                name = name,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };
            tex.SetPixels32(pixels);
            tex.Apply(true, true);
            var sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), PixelsPerUnit, 0, SpriteMeshType.FullRect);
            sprite.name = name;
            return sprite;
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
