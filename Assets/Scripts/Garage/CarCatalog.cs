using System;
using UnityEngine;

namespace ScrollRoad
{
    // Garaj: araç MODELİ × boya RENGİ. Sahne kurucu (ScrollRoadSceneBuilder) Assets/Data/CarCatalog.asset'i
    // koddan doldurur; ilk model ve ilk renk varsayılandır.
    //  - Görseller VehicleArt'tan: her modelin tek taban görseli (boyanabilirse mercan gövdeli),
    //    renkler ve silüetler çalışma zamanında türetilir. Sabit renkli araçlara (gemi, balon) renk
    //    seçimi uygulanmaz.
    //  - Kategori aracın sahnedeki şeridini belirler: kara araçları yolda, deniz araçları nehirde,
    //    hava araçları gökyüzünde.
    //  - Açılma yolu: ücretsiz, ömür boyu mesafe ya da bir ayda ulaşılan en uzak mesafe (ör. göl
    //    kıyısı, şehir). Her araç oynayarak açılır (DLC yok). Kilit durumu kaydedilmez, ilerlemeden türetilir (Progress).
    //  - Gizli renk ("Whisper") yalnız gizli bölge ödülüyle açılır, öncesinde UI'da hiç görünmez.
    //  - "Coming soon" yuvaları yalnız silüet; seçilemez.
    // Seçim tek anahtarda "<model>_<renk>" (eski "car_teal" kayıtları aynen geçerli), cihaza özel.
    [CreateAssetMenu(menuName = "ScrollRoad/Car Catalog")]
    public class CarCatalog : ScriptableObject
    {
        public enum Kind { Car, OffRoad, Suv, Pickup, Motorcycle, Boat, Aircraft }
        public enum Lane { Road, Water, Air }
        public enum Unlock { Free, LifetimeKm, MonthBestKm }

        // Garaj sekmeleri: birden çok kategoriyi toplar.
        public enum Tab { Cars, OffRoad, TwoWheels, Sea, Sky }
        public static readonly string[] TabNames = { "CARS", "OFF-ROAD", "TWO WHEELS", "SEA", "SKY" };

        public static Tab TabOf(Kind kind) => kind switch
        {
            Kind.Car => Tab.Cars,
            Kind.OffRoad or Kind.Suv or Kind.Pickup => Tab.OffRoad,
            Kind.Motorcycle => Tab.TwoWheels,
            Kind.Boat => Tab.Sea,
            _ => Tab.Sky,
        };

        public static Lane LaneOf(Kind kind) => kind switch
        {
            Kind.Boat => Lane.Water,
            Kind.Aircraft => Lane.Air,
            _ => Lane.Road,
        };

        [Serializable]
        public class Paint
        {
            public string id;
            public string displayName;
            public Color swatch;
            public bool tintGlass;          // camı da boyar (gizli renk: lavanta cam)
            public Color glass;
            public bool secret;
        }

        [Serializable]
        public class Model
        {
            public string id;
            public string displayName;
            public Kind kind;
            public bool paintable = true;
            public Unlock unlock;
            public double unlockValue;      // km (LifetimeKm / MonthBestKm)
            public string unlockLabel;      // MonthBestKm: hedefin adı (ör. "reach Lakeshore in one month")
            public float widthScale = 1f;   // yolda görünen genişlik çarpanı
            public float bodyMinSat = 0.3f; // gövde sayılan en düşük doygunluk (sürücülülerde yüksek: ten boyanmasın)
            public bool openSea;            // büyük gemiler: nehir yerine açık deniz (küçük tekneler nehirde)
            public bool contrail;           // jetler: arkada uçuş izi bırakır

            public Lane Lane => LaneOf(kind);
        }

        [Serializable]
        public class Teaser
        {
            public string id;
            public Kind kind;
        }

        public Paint[] paints = Array.Empty<Paint>();
        public Model[] models = Array.Empty<Model>();
        public Teaser[] comingSoon = Array.Empty<Teaser>();

        // Kilitlerin dayandığı ilerleme. Oyunda CarUnlockTracker canlı değerleri yazar; öncesinde
        // (ya da sahne dışında) kayıttaki yerel değerler okunur.
        public struct Progress
        {
            public double lifetimeKm;
            public double monthBestKm; // tüm aylar içinde bir ayda ulaşılan en uzak mesafe
        }

        static Progress? liveProgress;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetSession() => liveProgress = null;

        public static Progress CurrentProgress
        {
            get => liveProgress ?? new Progress { lifetimeKm = SaveService.LoadLifetimeKm(), monthBestKm = SaveService.LoadBestMonthKm() };
            set => liveProgress = value;
        }

        // İNCELEME MODU (yalnız Editor ve geliştirici build'i): "-unlockall" argümanıyla her araç,
        // renk (gizli dahil) ve gösterge skini açık görünür. Hiçbir şey kaydedilmez; yayın build'inde
        // derlenmez (kozmetik koleksiyonun hilesi olmasın).
#if UNITY_EDITOR
        // Editor'de menüden: ScrollRoad → İnceleme modu (her şey açık).
        public const string ReviewPrefKey = "ScrollRoad_ReviewUnlockAll";
        public static bool ReviewUnlockAll => UnityEditor.EditorPrefs.GetBool(ReviewPrefKey, false);
#elif DEVELOPMENT_BUILD
        static bool? reviewUnlockAll;
        public static bool ReviewUnlockAll => reviewUnlockAll ??= Array.Exists(Environment.GetCommandLineArgs(),
            a => string.Equals(a, "-unlockall", StringComparison.OrdinalIgnoreCase));
#else
        public static bool ReviewUnlockAll => false;
#endif

        public static bool IsUnlocked(Model model)
        {
            if (model == null) return false;
            if (ReviewUnlockAll) return true;
            var p = CurrentProgress;
            return model.unlock switch
            {
                Unlock.Free => true,
                Unlock.LifetimeKm => p.lifetimeKm >= model.unlockValue,
                Unlock.MonthBestKm => p.monthBestKm >= model.unlockValue,
                _ => false,
            };
        }

        // Kilitli modelin açıklaması ve kalan miktar (garajda gösterilir).
        public static string LockDescription(Model model)
        {
            var p = CurrentProgress;
            switch (model.unlock)
            {
                case Unlock.LifetimeKm:
                    return $"{model.displayName} unlocks at {Fmt.Km(model.unlockValue, 0)} total  ·  {Fmt.Km(model.unlockValue - p.lifetimeKm, 0)} to go";
                case Unlock.MonthBestKm:
                    return $"{model.displayName}: {model.unlockLabel}  ·  your best: {Fmt.Km(p.monthBestKm, 0)} of {Fmt.Km(model.unlockValue, 0)}";
                default:
                    return "";
            }
        }

        // Kilit plakasında görünen kısa metin.
        public static string LockPlate(Model model) => model.unlock switch
        {
            Unlock.LifetimeKm => Fmt.Km(model.unlockValue, 0).ToUpperInvariant(),
            Unlock.MonthBestKm => "TRIP",
            _ => "",
        };

        public static bool IsAvailable(Paint paint) => paint != null && (!paint.secret || ReviewUnlockAll || SaveService.LoadSecretCarUnlocked());

        public Model DefaultModel => models.Length > 0 ? models[0] : null;
        public Paint DefaultPaint => paints.Length > 0 ? paints[0] : null;

        Model StoredModel()
        {
            string id = SaveService.LoadSelectedCar();
            Model best = null;
            foreach (var model in models)
            {
                // En uzun önek kazanır ("van" ile "vanX" gibi çakışmalar olmasın).
                if (id.StartsWith(model.id + "_") && (best == null || model.id.Length > best.id.Length)) best = model;
            }
            return best;
        }

        // Kayıttaki seçim; bilinmeyen/kilitli kısım varsayılana düşer.
        public Model SelectedModel
        {
            get
            {
                var model = StoredModel();
                return model != null && IsUnlocked(model) ? model : DefaultModel;
            }
        }

        public Paint SelectedPaint
        {
            get
            {
                string id = SaveService.LoadSelectedCar();
                var model = StoredModel();
                string paintId = model != null ? id.Substring(model.id.Length + 1) : "";
                foreach (var paint in paints)
                {
                    if (paint.id == paintId && IsAvailable(paint)) return paint;
                }
                return DefaultPaint;
            }
        }

        // Taban görseller varsayılan renkte (mercan): o renk ve sabit renkli araçlar boyanmaz.
        public Sprite SpriteFor(Model model, Paint paint)
        {
            if (model == null) return null;
            bool recolor = model.paintable && paint != null && paint != DefaultPaint;
            return VehicleArt.Get(model.id, recolor ? paint : null, model.bodyMinSat);
        }

        public static Sprite SilhouetteFor(Model model) => model == null ? null : VehicleArt.Silhouette(model.id);
        public static Sprite SilhouetteFor(Teaser teaser) => teaser == null ? null : VehicleArt.Silhouette(teaser.id);

        public Sprite SelectedSprite => SpriteFor(SelectedModel, SelectedPaint);

        // Seçim değişince sahnedeki araba güncellensin.
        public static event Action SelectionChanged;

        public void Select(Model model) => Select(model, SelectedPaint);
        public void Select(Paint paint) => Select(SelectedModel, paint);

        // Renk, sabit renkli araçlarda da saklanır: boyanabilir araca dönünce seçili renk geri gelir.
        void Select(Model model, Paint paint)
        {
            if (!IsUnlocked(model) || !IsAvailable(paint)) return;
            SaveService.SaveSelectedCar($"{model.id}_{paint.id}");
            SelectionChanged?.Invoke();
        }
    }
}
