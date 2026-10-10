using System;

namespace ScrollRoad
{
    // Steam Cloud'a JSON olarak giden kayıt anlık görüntüsü (JsonUtility double destekler).
    // Mesafeler double: float ~2000 km'den sonra kare başına ilerlemeyi yutuyordu.
    [Serializable]
    public class ScrollRoadSaveData
    {
        public double currentDistanceKm;
        public double maxDistanceKm;
        public string raceMonth = "";

        // Cloud birleştirmesinde "en son hangi cihaz kapandı" sorusu için (offline ilerleme yok).
        public bool hasLastClose;
        public long lastCloseUnixSeconds;

        // Ömürlük istatistikler (aylık yarışla sıfırlanmaz) — İstatistikler paneli.
        public double lifetimeKm;
        public int lifetimeSteps;
        public int lifetimeTurns;
        public int cityReachedCount;
        public double bestMonthKm;
        public string bestMonth = "";
    }
}
