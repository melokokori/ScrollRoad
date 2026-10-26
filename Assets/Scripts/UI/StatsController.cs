using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ScrollRoad
{
    // İstatistikler paneli. Oyun sahnesinde canlı değerleri (RoadDrive + StatsTracker), ana menüde
    // kayıttaki değerleri (SaveService) gösterir.
    public class StatsController : MonoBehaviour
    {
        [SerializeField] RoadDrive drive;        // ana menüde boş
        [SerializeField] StatsTracker stats;     // ana menüde boş
        [SerializeField] GameObject statsPanel;
        [SerializeField] TMP_Text statsText;
        [SerializeField] Button openButton;
        [SerializeField] Button closeButton;

        public GameObject Panel => statsPanel;

        void Awake()
        {
            openButton.onClick.AddListener(Open);
            closeButton.onClick.AddListener(() => statsPanel.SetActive(false));
            statsPanel.SetActive(false);
        }

        void Open()
        {
            var data = SaveService.Load();
            string month = DateTimeOffset.UtcNow.ToString("yyyy-MM");

            double monthMax = drive != null ? drive.MaxDistanceKmPrecise : (data.raceMonth == month ? data.maxDistanceKm : 0);
            bool reached = drive != null ? drive.ReachedCity : monthMax >= RoadDrive.DefaultCityDistanceKm;
            double lifetimeKm = stats != null ? stats.LifetimeKm : data.lifetimeKm;
            int steps = stats != null ? stats.LifetimeSteps : data.lifetimeSteps;
            int turns = stats != null ? stats.LifetimeTurns : data.lifetimeTurns;
            int cities = stats != null ? stats.CityReachedCount : data.cityReachedCount;
            double bestKm = Math.Max(stats != null ? stats.BestMonthKm : data.bestMonthKm, monthMax);
            string bestMonth = stats != null ? stats.BestMonth : data.bestMonth;
            if (monthMax >= bestKm && monthMax > 0) bestMonth = month;

            statsText.text =
                L.T("THIS MONTH") + "\n" +
                "   " + L.F("Furthest point:  {0}", Fmt.Km(monthMax)) + "\n" +
                (reached ? "   " + L.T("City reached.") + "\n" : "") +
                "\n" + L.T("ALL TIME") + "\n" +
                "   " + L.F("Total distance:  {0}", Fmt.Km(lifetimeKm)) + "\n" +
                "   " + L.F("Scrolls:  {0}", Fmt.Int(steps)) + "\n" +
                "   " + L.F("U-turns:  {0}", Fmt.Int(turns)) + "\n" +
                "   " + L.F("Cities reached:  {0}", Fmt.Int(cities)) + "\n" +
                "   " + L.F("Best month:  {0}", bestKm > 0 ? $"{Fmt.Km(bestKm)}  ({bestMonth})" : "—");

            statsPanel.SetActive(true);
        }
    }
}
