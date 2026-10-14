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

        // Gizli ortak puan (2026-10-13): tüm ortamların ilerlemesi buradan türer. pointsVersion 0 =
        // puan katmanından önceki kayıt (cloud birleştirmede km'den türetilir).
        public int pointsVersion;
        public string pointsRaceMonth = "";
        public double monthPoints;
        public double monthBestPoints;
        public double lifetimePoints;
        public double bestMonthPoints;
        public string bestPointsMonth = "";

        // Gizli bölge ödülü (garajdaki gizli araç) — bir kez açılınca kalıcı.
        public bool secretCarUnlocked;
    }
}
