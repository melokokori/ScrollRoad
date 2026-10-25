using UnityEngine;

namespace ScrollRoad
{
    // Plak tablası ortamının sahnesi (2026-10-14): ÜSTTEN bakış. Ahşap masa; solda pikap (gövde, tabla, dönen
    // siyah plak + oluklar, renkli etiket — dönüş etiketteki desenden okunur, mil, ton kolu ve iğne), sağda bu
    // ayın son plaklarının kapakları masaya yelpaze gibi yığılmış (en üstte en son biten plak). Çalarken
    // pikaptan notalar süzülür. Her şey kodla; nadir plakların kapakları ayrı görseller (Gemini).
    //
    // Sahne bu ayın konumunun SAF fonksiyonu (VinylController): plağın açısı konumla orantılı (hız = tempo,
    // geri sarınca ters döner); iğne plağın dışından içine plağın ilerlemesiyle; tabladaki plak ve yığındaki
    // kapaklar ayın sırasından (RecordCatalog.Identify).
    //
    // Görev çubuğu şeridinde: saydam arka plan; şeridin ortasında küçük plak + ton kolu, sağında iki kapak.
    // Yalnız Plak tablası seçiliyken çizer ve kamerayı yönetir.
    public class VinylSceneView : MonoBehaviour
    {
        [SerializeField] VinylController vinyl;
        [SerializeField] CompanionWindowController companionWindow;
        [SerializeField] Camera viewCamera;
        [SerializeField] Sprite[] coverSprites; // RecordCatalog.AllSpecials sırasıyla
        [SerializeField] Material spriteMaterial;

        [Header("Kart")]
        [SerializeField] float cardViewTop = 2.4f;
        [SerializeField] float cardViewBottom = -4.1f;

        [Header("Görev çubuğu")]
        [SerializeField] float stripRecordScreenX = 0.5f;

        // Plak açısı: puan başına derece (seyirde ~33⅓ devir/dk hissi, ~200°/sn).
        const double DegreesPerPoint = 12.0;
        const int PileSize = 4;
        const int NoteCount = 5;

        // Ortam Standardı v2: mekân masayı (RecordShop.Palette 0-1), skin takımı pikabı boyar (SkinInfo.Palette 0-4:
        // gövde, gövde kenarı, tabla, kol, koyu metal). Varsayılanlar Plak Dükkânı ve Klasik.
        static readonly Color TableDefault = new Color(0.62f, 0.45f, 0.32f);
        static readonly Color TableGrainDefault = new Color(0.56f, 0.40f, 0.28f);
        static readonly Color PlinthDefault = new Color(0.80f, 0.62f, 0.42f);
        static readonly Color PlinthEdgeDefault = new Color(0.52f, 0.37f, 0.25f);
        static readonly Color PlatterDefault = new Color(0.62f, 0.64f, 0.66f);
        static readonly Color MetalDefault = new Color(0.82f, 0.83f, 0.85f);
        static readonly Color MetalDarkDefault = new Color(0.45f, 0.47f, 0.50f);

        Color Look(int i, Color fallback)
        {
            var p = vinyl != null ? vinyl.Shop.Palette : null;
            return p != null && i < p.Length ? p[i] : fallback;
        }

        Color Tint(int i, Color fallback)
        {
            var p = vinyl != null ? vinyl.Skin.Palette : null;
            return p != null && i < p.Length ? p[i] : fallback;
        }

        Color Table => Look(0, TableDefault);
        Color TableGrain => Look(1, TableGrainDefault);
        Color Plinth => Tint(0, PlinthDefault);
        Color PlinthEdge => Tint(1, PlinthEdgeDefault);
        Color Platter => Tint(2, PlatterDefault);
        Color Metal => Tint(3, MetalDefault);
        Color MetalDark => Tint(4, MetalDarkDefault);
        static readonly Color Vinyl = new Color(0.09f, 0.09f, 0.10f);
        static readonly Color NoteColor = new Color(1f, 0.95f, 0.85f);

        Transform root;
        Sprite whiteSprite, circleSprite, recordSprite, labelSprite, roundSprite, sleeveSprite;
        SpriteRenderer tableTop, plinthShadow, plinth, plinthTop, platter, record, label, spindle;
        SpriteRenderer armBase, arm, headshell, knob, slider;
        SpriteRenderer[] grain, pile, pileShadow, pileLabel, noteHeads, noteStems;

        bool IsCompanion => companionWindow != null && companionWindow.IsCompanionMode;

        void Awake()
        {
            if (viewCamera == null) viewCamera = Camera.main;
            whiteSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
            circleSprite = CreateCircleSprite(32);
            recordSprite = CreateRecordSprite();
            labelSprite = CreateLabelSprite();
            roundSprite = CreateRoundRectSprite();
            sleeveSprite = CreateSleeveSprite();
            Build();
        }

        void Build()
        {
            root = new GameObject("Turntable").transform;
            root.SetParent(transform, false);
            tableTop = Create("Table", whiteSprite, 0);
            grain = new SpriteRenderer[7];
            for (int i = 0; i < grain.Length; i++) grain[i] = Create($"Grain{i}", whiteSprite, 1);
            plinthShadow = Create("PlinthShadow", roundSprite, 2);
            plinth = Create("Plinth", roundSprite, 3);
            plinthTop = Create("PlinthTop", roundSprite, 4);
            platter = Create("Platter", circleSprite, 5);
            record = Create("Record", recordSprite, 6);
            label = Create("Label", labelSprite, 7);
            spindle = Create("Spindle", circleSprite, 8);
            knob = Create("SpeedKnob", circleSprite, 6);
            slider = Create("PitchSlider", whiteSprite, 6);
            armBase = Create("ArmBase", circleSprite, 9);
            arm = Create("Arm", whiteSprite, 10);
            headshell = Create("Headshell", whiteSprite, 11);
            pile = new SpriteRenderer[PileSize];
            pileShadow = new SpriteRenderer[PileSize];
            pileLabel = new SpriteRenderer[PileSize];
            for (int i = 0; i < PileSize; i++)
            {
                pileShadow[i] = Create($"SleeveShadow{i}", whiteSprite, 20 + i * 3);
                pile[i] = Create($"Sleeve{i}", whiteSprite, 21 + i * 3);
                pileLabel[i] = Create($"SleeveMark{i}", circleSprite, 22 + i * 3);
            }
            noteHeads = new SpriteRenderer[NoteCount];
            noteStems = new SpriteRenderer[NoteCount];
            for (int i = 0; i < NoteCount; i++)
            {
                noteHeads[i] = Create($"NoteHead{i}", circleSprite, 40);
                noteStems[i] = Create($"NoteStem{i}", whiteSprite, 40);
            }
        }

        SpriteRenderer Create(string name, Sprite sprite, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(root, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
            if (spriteMaterial != null) sr.sharedMaterial = spriteMaterial;
            return sr;
        }

        static Sprite CreateCircleSprite(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            float r = size / 2f - 1f;
            var c = new Vector2(size / 2f - 0.5f, size / 2f - 0.5f);
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float a = Mathf.Clamp01(r - Vector2.Distance(new Vector2(x, y), c) + 0.5f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        // Plak: renkler dokuda MUTLAK (sprite rengi yalnız tema/gece karartması): neredeyse siyah zemin, hafif
        // oluk halkaları, kenarda düz pay, parçalar arasında belirgin boşluk halkaları, iki karşılıklı yansıma
        // dilimi (dönüş görünsün).
        static Sprite CreateRecordSprite()
        {
            const int size = 256;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, true) { filterMode = FilterMode.Trilinear };
            var c = new Vector2(size / 2f - 0.5f, size / 2f - 0.5f);
            float R = size / 2f - 1f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                var d = new Vector2(x, y) - c;
                float r = d.magnitude / R;
                float a = Mathf.Clamp01((1f - r) * R + 0.5f);
                float v = 0.085f;
                if (r > 0.36f && r < 0.95f)
                {
                    v = 0.085f + 0.014f * Mathf.Sin(r * 220f);                    // oluklar
                    float t = (0.95f - r) / (0.95f - 0.36f);                        // parçalar arası boşluk
                    for (int g = 1; g < RecordCatalog.TracksPerRecord; g++)
                        if (Mathf.Abs(t - g / (float)RecordCatalog.TracksPerRecord) < 0.012f) v = 0.17f;
                    float ang = Mathf.Atan2(d.y, d.x);
                    v += 0.13f * Mathf.Pow(Mathf.Max(0f, Mathf.Cos(2f * (ang - 0.7f))), 14f); // yansıma dilimleri
                }
                else if (r >= 0.95f) v = 0.13f;
                tex.SetPixel(x, y, new Color(v, v, v, a));
            }
            tex.Apply(true);
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        // Etiket: beyaz daire, yarısında açık bir bant ve kenara yakın bir nokta — dönüş gözle okunsun.
        static Sprite CreateLabelSprite()
        {
            const int size = 96;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, true) { filterMode = FilterMode.Trilinear };
            var c = new Vector2(size / 2f - 0.5f, size / 2f - 0.5f);
            float R = size / 2f - 1f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                var d = new Vector2(x, y) - c;
                float r = d.magnitude / R;
                float a = Mathf.Clamp01((1f - r) * R + 0.5f);
                float v = 0.85f;
                if (d.y > R * 0.12f && d.y < R * 0.42f) v = 1f;                      // bant
                if (Vector2.Distance(d, new Vector2(-R * 0.55f, -R * 0.25f)) < R * 0.12f) v = 1f; // nokta
                if (r < 0.1f) v = 0.35f;                                              // delik çevresi
                tex.SetPixel(x, y, new Color(v, v, v, a));
            }
            tex.Apply(true);
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        // Yuvarlak köşeli dikdörtgen (dokuz parça: kenar payı 12 px).
        static Sprite CreateRoundRectSprite()
        {
            const int size = 64, radius = 12;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float cx = Mathf.Clamp(x + 0.5f, radius, size - radius), cy = Mathf.Clamp(y + 0.5f, radius, size - radius);
                float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy));
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(radius - dist + 0.5f)));
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
        }

        // Sıradan plağın kapağı: kenarı koyu ince çerçeveli düz kare; ortadaki daire baskısı ayrı (pileLabel).
        static Sprite CreateSleeveSprite()
        {
            const int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                bool edge = x < 2 || y < 2 || x >= size - 2 || y >= size - 2;
                float v = edge ? 0.8f : 1f;
                if (!edge && y > size * 0.78f && y < size * 0.84f) v = 0.9f; // ince şerit
                tex.SetPixel(x, y, new Color(v, v, v, 1f));
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        bool Show()
        {
            bool active = EnvironmentManager.Current.Id == EnvironmentId.Vinyl;
            if (root.gameObject.activeSelf != active) root.gameObject.SetActive(active);
            return active;
        }

        // ---------------------------------------------------------------- kare

        void LateUpdate()
        {
            if (!Show() || vinyl == null) return;
            bool strip = IsCompanion;
            bool night = WorldTheme.Current == WorldTheme.Kind.Night;
            float dim = night ? 0.62f : 1f;

            float aspect = Mathf.Max(0.1f, viewCamera.aspect);
            float size, camX, camY;
            // Şeritte plak şerit yüksekliğine sığar: plak yarıçapı 1 birim, bant 2,3 birim.
            float recordR = strip ? 1f : 1.75f;
            Vector2 rc = strip ? new Vector2(0f, 0f) : new Vector2(-2.3f, -1.05f);
            if (strip)
            {
                float bottom = -1.15f, top = 1.15f;
                float band = 1f - (companionWindow != null ? companionWindow.StripOverflowFraction : 0f);
                size = (top - bottom) / 2f / Mathf.Max(0.2f, band);
                camY = bottom + size;
                camX = rc.x - (stripRecordScreenX * 2f - 1f) * size * aspect;
            }
            else
            {
                size = (cardViewTop - cardViewBottom) / 2f;
                camY = (cardViewTop + cardViewBottom) / 2f;
                camX = 0f;
            }
            viewCamera.orthographicSize = size;
            viewCamera.transform.position = new Vector3(camX, camY, -10f);
            float halfW = size * aspect;
            float left = camX - halfW, right = camX + halfW, top0 = camY + size, bottom0 = camY - size;
            viewCamera.backgroundColor = strip ? new Color(0f, 0f, 0f, 0f) : Opaque(WorldTheme.Surface(Table) * dim);

            // Masa: düz renk + birkaç uzun açık damar (yalnız kart).
            tableTop.enabled = !strip;
            if (!strip) PlaceRect(tableTop, camX, camY, halfW * 2f + 0.4f, size * 2f + 0.4f, Opaque(WorldTheme.Surface(Table) * dim));
            for (int i = 0; i < grain.Length; i++)
            {
                grain[i].enabled = !strip;
                if (strip) continue;
                float y = bottom0 + (i + 0.5f) / grain.Length * (top0 - bottom0) + Mathf.Sin(i * 2.3f) * 0.2f;
                float x = left + ((i * 0.37f) % 1f) * (right - left);
                PlaceRect(grain[i], x, y, 4f + (i % 3) * 2f, 0.05f, Opaque(WorldTheme.Surface(TableGrain) * dim));
            }

            // Pikap gövdesi (kart): yuvarlak köşeli ahşap + koyu kenar + gölge.
            float pw = recordR * 2f + 2.1f, ph = recordR * 2f + 0.9f;
            var pc = rc + new Vector2(0.75f, 0f);
            plinthShadow.enabled = plinth.enabled = plinthTop.enabled = !strip;
            if (!strip)
            {
                PlaceSliced(plinthShadow, pc.x + 0.12f, pc.y - 0.16f, pw, ph, new Color(0f, 0f, 0f, 0.22f));
                PlaceSliced(plinth, pc.x, pc.y, pw, ph, Opaque(WorldTheme.Surface(PlinthEdge) * dim));
                PlaceSliced(plinthTop, pc.x, pc.y + 0.05f, pw - 0.16f, ph - 0.18f, Opaque(WorldTheme.Surface(Plinth) * dim));
            }
            knob.enabled = slider.enabled = !strip;
            if (!strip)
            {
                PlaceRect(knob, pc.x - pw / 2f + 0.4f, pc.y - ph / 2f + 0.38f, 0.32f, 0.32f, Opaque(WorldTheme.Surface(MetalDark) * dim));
                PlaceRect(slider, pc.x + pw / 2f - 0.42f, pc.y - 0.75f, 0.1f, 1.1f, Opaque(WorldTheme.Surface(MetalDark) * dim));
            }

            // Tabla + plak + etiket + mil.
            double position = vinyl.Position;
            float angle = (float)(-(position * DegreesPerPoint % 360.0));
            var rec = vinyl.Current;
            PlaceRect(platter, rc.x, rc.y, recordR * 2.08f, recordR * 2.08f, Opaque(WorldTheme.Surface(Platter) * dim));
            PlaceRect(record, rc.x, rc.y, recordR * 2f, recordR * 2f, Opaque(WorldTheme.Surface(Color.white) * (night ? 0.9f : 1f)));
            record.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
            PlaceRect(label, rc.x, rc.y, recordR * 0.66f, recordR * 0.66f, Opaque(WorldTheme.Surface(rec.Label) * (night ? 0.8f : 1f)));
            label.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
            PlaceRect(spindle, rc.x, rc.y, recordR * 0.06f, recordR * 0.06f, Opaque(WorldTheme.Surface(Metal) * dim));

            // Ton kolu: sağ üstteki pivottan iğneye; iğne plağın dış oluğundan iç oluğuna plağın ilerlemesiyle.
            var pivot = rc + new Vector2(recordR * 1.32f, recordR * 0.92f);
            float armLen = recordR * 1.75f;
            float grooveR = Mathf.Lerp(recordR * 0.93f, recordR * 0.40f, vinyl.RecordProgress);
            var needle = NeedlePoint(rc, grooveR, pivot, armLen);
            PlaceRect(armBase, pivot.x, pivot.y, recordR * 0.32f, recordR * 0.32f, Opaque(WorldTheme.Surface(MetalDark) * dim));
            PlaceLine(arm, pivot, needle, recordR * 0.045f, Opaque(WorldTheme.Surface(Metal) * dim));
            var dir = (needle - pivot).normalized;
            PlaceRect(headshell, needle.x - dir.x * recordR * 0.06f, needle.y - dir.y * recordR * 0.06f, recordR * 0.2f, recordR * 0.11f, Opaque(WorldTheme.Surface(MetalDark) * dim));
            headshell.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);

            UpdatePile(strip, rc, recordR, dim, night);
            UpdateNotes(strip, rc, recordR);
        }

        // İğnenin yeri: pivottan armLen uzaklıkta VE plağın merkezinden grooveR uzaklıkta olan nokta
        // (iki çemberin kesişimi; plağın sol-üst tarafındaki çözüm).
        static Vector2 NeedlePoint(Vector2 c, float r, Vector2 p, float l)
        {
            var d = p - c;
            float dist = d.magnitude;
            if (dist < 1e-4f || dist > r + l || dist < Mathf.Abs(r - l)) return c + d.normalized * r;
            float a = (r * r - l * l + dist * dist) / (2f * dist);
            float h = Mathf.Sqrt(Mathf.Max(0f, r * r - a * a));
            var m = c + d * (a / dist);
            var perp = new Vector2(-d.y, d.x) / dist;
            var s1 = m + perp * h;
            var s2 = m - perp * h;
            return s1.y > s2.y ? s1 : s2;
        }

        // Kapak yığını: en üstte en son biten plak, altta öncekiler, hafif dönük ve kaydırılmış.
        void UpdatePile(bool strip, Vector2 rc, float recordR, float dim, bool night)
        {
            long done = vinyl.RecordIndex - 1; // biten plak sayısı
            float sleeve = strip ? 1.8f : 2.5f;
            var basePos = strip ? new Vector2(rc.x + 2.6f, 0f) : new Vector2(4.4f, -1.25f);
            int shown = strip ? 2 : PileSize;
            for (int k = 0; k < PileSize; k++)
            {
                // k = 0 en alttaki (en eski), k = shown-1 en üstteki (en son biten).
                long idx = done - (shown - 1 - k);
                bool on = k < shown && idx >= 1;
                pile[k].enabled = pileShadow[k].enabled = pileLabel[k].enabled = on;
                if (!on) continue;
                var r = vinyl.Identify(idx);
                float rot = ((idx * 37) % 13 - 6) * 1.6f;
                var p = basePos + new Vector2((k - (shown - 1)) * (strip ? 0.5f : 0.38f), (k - (shown - 1)) * (strip ? 0.05f : 0.22f));
                Sprite cover = null;
                if (r.IsSpecial)
                {
                    int ci = RecordCatalog.SpecialIndex(r.Type.Id);
                    if (coverSprites != null && ci >= 0 && ci < coverSprites.Length) cover = coverSprites[ci];
                }
                pile[k].sprite = cover != null ? cover : sleeveSprite;
                PlaceRect(pileShadow[k], p.x + 0.1f, p.y - 0.12f, sleeve, sleeve, new Color(0f, 0f, 0f, strip ? 0.25f : 0.2f));
                pileShadow[k].transform.localRotation = Quaternion.Euler(0f, 0f, rot);
                var tint = cover != null ? WorldTheme.Surface(Color.white) * (night ? 0.8f : 1f)
                                         : WorldTheme.Surface(Color.Lerp(r.Label, Color.white, 0.15f)) * dim;
                PlaceRect(pile[k], p.x, p.y, sleeve, sleeve, Opaque(tint));
                pile[k].transform.localRotation = Quaternion.Euler(0f, 0f, rot);
                // Sıradan kapakta ortada koyu daire baskısı (içindeki plağın ima ettiği).
                pileLabel[k].enabled = cover == null;
                if (cover == null)
                {
                    PlaceRect(pileLabel[k], p.x, p.y, sleeve * 0.55f, sleeve * 0.55f, Opaque(WorldTheme.Surface(Vinyl) * 1.6f * dim));
                }
            }
        }

        // Çalarken pikaptan yukarı süzülen notalar (yalnız kart; zamanla, konumdan bağımsız).
        void UpdateNotes(bool strip, Vector2 rc, float recordR)
        {
            bool playing = !strip && vinyl.SpinDirection > 0f;
            float t = Time.time;
            for (int i = 0; i < NoteCount; i++)
            {
                noteHeads[i].enabled = noteStems[i].enabled = playing;
                if (!playing) continue;
                float life = (t * 0.32f + i / (float)NoteCount) % 1f;
                float x = rc.x + recordR * 1.15f + Mathf.Sin((t + i * 1.7f) * 1.3f) * 0.35f + i * 0.18f;
                float y = rc.y + recordR * 0.2f + life * 2.6f;
                var c = NoteColor;
                c.a = Mathf.Sin(life * Mathf.PI) * 0.7f;
                PlaceRect(noteHeads[i], x, y, 0.2f, 0.15f, c);
                noteHeads[i].transform.localRotation = Quaternion.Euler(0f, 0f, 20f);
                PlaceRect(noteStems[i], x + 0.085f, y + 0.2f, 0.03f, 0.4f, c);
            }
        }

        static Color Opaque(Color c)
        {
            c.a = 1f;
            return c;
        }

        void PlaceRect(SpriteRenderer sr, float x, float y, float w, float h, Color color)
        {
            sr.drawMode = SpriteDrawMode.Simple;
            sr.transform.localPosition = new Vector3(x, y, 0f);
            sr.transform.localRotation = Quaternion.identity;
            var b = sr.sprite.bounds.size;
            sr.transform.localScale = new Vector3(w / b.x, h / b.y, 1f);
            sr.color = color;
        }

        void PlaceSliced(SpriteRenderer sr, float x, float y, float w, float h, Color color)
        {
            sr.drawMode = SpriteDrawMode.Sliced;
            sr.transform.localPosition = new Vector3(x, y, 0f);
            sr.transform.localRotation = Quaternion.identity;
            sr.transform.localScale = Vector3.one; // 64 px = 1 birim; köşe yarıçapı ~0,19 birim
            sr.size = new Vector2(w, h);
            sr.color = color;
        }

        void PlaceLine(SpriteRenderer sr, Vector2 a, Vector2 b, float width, Color color)
        {
            var d = b - a;
            float len = d.magnitude;
            sr.enabled = len > 0.01f;
            if (!sr.enabled) return;
            sr.drawMode = SpriteDrawMode.Simple;
            sr.transform.localPosition = (Vector3)((a + b) / 2f);
            sr.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
            var bs = sr.sprite.bounds.size;
            sr.transform.localScale = new Vector3(len / bs.x, width / bs.y, 1f);
            sr.color = color;
        }
    }
}
