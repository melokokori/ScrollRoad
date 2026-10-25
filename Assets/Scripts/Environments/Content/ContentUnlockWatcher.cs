using UnityEngine;

namespace ScrollRoad
{
    // Açılma duyuruları (Ortam Standardı v2, 2026-10-25): seçili ortamın merdiveninde yeni bir mekân ya da
    // skin takımı açılınca bir kez duyurur. Yeni mekâna kendiliğinden geçmez (oyuncunun Odak'ı bozulmasın);
    // geçiş ≡ → Places'tan. Eski "duyuruldu" listeleri (olta/kapı kaydı) ilk açılışta bir kez taşınır.
    // Açılışın ilk saniyelerinde bekler: tabela pencere kurulurken görünmeden bitiyordu.
    public class ContentUnlockWatcher : MonoBehaviour
    {
        [SerializeField] NoticeController notice;

        void Update()
        {
            if (Time.realtimeSinceStartup < 4f || Time.frameCount % 30 != 0) return;
            var c = EnvironmentContent.Current;
            if (c == null) return;

            var id = c.Def.Id;
            var state = EnvironmentContent.State(id);
            bool dirty = false;
            if (!state.migrated)
            {
                foreach (var key in c.LegacyAnnounced) if (!state.announced.Contains(key)) state.announced.Add(key);
                state.migrated = true;
                dirty = true;
            }

            // İnceleme modu (her şey açık) duyuru harcamasın: gerçek eşik geçilmiş olmalı.
            double points = c.LifetimePoints;
            var venues = c.Venues;
            for (int i = 1; i < venues.Length; i++)
            {
                string key = "venue:" + venues[i].Id;
                if (state.announced.Contains(key) || points < UnlockLadder.VenuePoints(i)) continue;
                state.announced.Add(key);
                dirty = true;
                if (notice != null)
                {
                    notice.Announce($"New {c.VenueNoun}: {venues[i].Name}", "Go there from Places in the ≡ menu", $"{venues[i].Name} unlocked");
                }
            }

            var skins = c.Skins;
            for (int i = 1; i < skins.Length; i++)
            {
                string key = "skin:" + skins[i].Id;
                if (state.announced.Contains(key) || points < UnlockLadder.SkinPoints(i)) continue;
                state.announced.Add(key);
                dirty = true;
                if (notice != null) notice.Announce($"New {c.SkinNoun}: {skins[i].Name}", skins[i].Blurb, $"{skins[i].Name} unlocked");
            }

            if (dirty) EnvironmentContent.Save(id);
        }
    }
}
