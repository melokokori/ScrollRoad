using System;
using UnityEngine;

namespace ScrollRoad
{
    // Garajdaki araçlar. Sahne kurucu (ScrollRoadSceneBuilder) Assets/Data/CarCatalog.asset'i
    // koddan doldurur; ilk kayıt varsayılan araçtır. Gizli kayıt yalnız kilidi açılınca görünür
    // (gizli bölge ödülü — UI'da önceden hiç belirtilmez).
    [CreateAssetMenu(menuName = "ScrollRoad/Car Catalog")]
    public class CarCatalog : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            public string id;
            public string displayName;
            public Sprite sprite;
            public bool secret;
        }

        public Entry[] entries = Array.Empty<Entry>();

        public Entry Default => entries.Length > 0 ? entries[0] : null;

        public bool IsAvailable(Entry entry) => entry != null && (!entry.secret || SaveService.LoadSecretCarUnlocked());

        // Kayıttaki seçim; silinmiş/kilitli ise varsayılana düşer.
        public Entry Selected
        {
            get
            {
                string id = SaveService.LoadSelectedCar();
                foreach (var entry in entries)
                {
                    if (entry.id == id && IsAvailable(entry)) return entry;
                }
                return Default;
            }
        }

        // Seçim değişince sahnedeki araba ve menü önizlemesi güncellensin.
        public static event Action SelectionChanged;

        public void Select(Entry entry)
        {
            if (!IsAvailable(entry)) return;
            SaveService.SaveSelectedCar(entry.id);
            SelectionChanged?.Invoke();
        }
    }
}
