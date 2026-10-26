using UnityEngine;

namespace ScrollRoad
{
    // Açılma duyuruları (Ortam Standardı v2, 2026-10-25): seçili ortamın merdiveninde yeni bir mekân ya da
    // skin takımı açılınca bir kez duyurur. Yeni mekâna kendiliğinden geçmez (oyuncunun Odak'ı bozulmasın);
    // geçiş ≡ → Places'tan. Eski "duyuruldu" listeleri (olta/kapı kaydı) ilk açılışta bir kez taşınır.
    // Açılışın ilk saniyelerinde bekler: tabela pencere kurulurken görünmeden bitiyordu. Yan yuvadaki ortam
    // (SideActivity) da izlenir; onun açılmaları ortamın adıyla duyurulur.
    public class ContentUnlockWatcher : MonoBehaviour
    {
        [SerializeField] NoticeController notice;

        void Update()
        {
            if (Time.realtimeSinceStartup < 4f || Time.frameCount % 30 != 0) return;
            var c = EnvironmentContent.Current;
            if (c != null) Check(c, side: false);
            var side = SideActivity.Current;
            if (side != null && side != c) Check(side, side: true);
        }

        void Check(IEnvironmentContent c, bool side)
        {
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
            double points = EnvironmentContent.LadderPoints(c);
            string where = side ? $"{L.T(c.Def.Name)} · " : "";
            var venues = c.Venues;
            for (int i = 1; i < venues.Length; i++)
            {
                string key = "venue:" + venues[i].Id;
                if (state.announced.Contains(key) || points < UnlockLadder.VenuePoints(i)) continue;
                state.announced.Add(key);
                dirty = true;
                if (notice != null)
                {
                    string name = L.T(venues[i].Name);
                    notice.Announce(where + L.F(NewLine(c.VenueNoun), name),
                        side ? L.T("Unlocked by your side activity") : L.T("Go there from Places in the menu"), L.F("{0} unlocked", name));
                }
            }

            var skins = c.Skins;
            for (int i = 1; i < skins.Length; i++)
            {
                string key = "skin:" + skins[i].Id;
                if (state.announced.Contains(key) || points < UnlockLadder.SkinPoints(i)) continue;
                state.announced.Add(key);
                dirty = true;
                if (notice == null) continue;
                string name = L.T(skins[i].Name);
                notice.Announce(where + L.F(NewLine(c.SkinNoun), name), side ? L.T("Unlocked by your side activity") : L.T(skins[i].Blurb), L.F("{0} unlocked", name));
            }

            if (dirty) EnvironmentContent.Save(id);
        }

        // "New spot: {0}" — ad ile cins/çekim uyumu dile göre değiştiği için her ad ayrı anahtar.
        static string NewLine(string noun) => noun switch
        {
            "venue" => L.N("New venue: {0}"),
            "place" => L.N("New place: {0}"),
            "spot" => L.N("New spot: {0}"),
            "shop" => L.N("New shop: {0}"),
            "site" => L.N("New site: {0}"),
            "tackle" => L.N("New tackle: {0}"),
            _ => L.N("New look: {0}"),
        };
    }
}
