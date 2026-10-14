using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ScrollRoad
{
    // Bu ayın pasaportu: her satır bir bölge (bölge damgası + kasabaları), son satır şehir.
    // Damgalar ayın en uzak noktasından türetilir; ay başında pasaport yenilenir.
    // stampFrames/stampTexts sırası RouteMap.Stops ile birebir (sahne kurucu öyle dizer).
    public class PassportController : MonoBehaviour
    {
        [SerializeField] RoadDrive drive;
        [SerializeField] GameObject passportPanel;
        [SerializeField] TMP_Text titleText;
        [SerializeField] TMP_Text countText;
        [SerializeField] Image[] stampFrames;
        [SerializeField] TMP_Text[] stampTexts;
        [SerializeField] Button openButton;
        [SerializeField] Button closeButton;
        [SerializeField] FishLogController fishLog; // Olta'dayken koleksiyon satırı balık defterini açar
        [SerializeField] GuestBookController guestBook; // Döner kapı'dayken misafir defterini
        [SerializeField] GridLogController gridLog;     // Rüzgârgülü'ndeyken şebeke defterini
        [SerializeField] CrateLogController crateLog;   // Plak tablasındayken plak sandığını
        [SerializeField] IntervalLogController[] intervalLogs; // eşit aralıklı ortamlarda kendi defterlerini

        static readonly Color EmptyFrame = new Color(1f, 1f, 1f, 0.05f);
        static readonly Color EmptyText = new Color(0.95f, 0.95f, 0.93f, 0.28f);
        static readonly Color DarkInk = new Color(0.12f, 0.14f, 0.18f);
        static readonly Color LightInk = new Color(0.97f, 0.97f, 0.94f);

        public GameObject Panel => passportPanel;

        void Awake()
        {
            openButton.onClick.AddListener(Open);
            closeButton.onClick.AddListener(() => passportPanel.SetActive(false));
            passportPanel.SetActive(false);
        }

        void Open()
        {
            if (fishLog != null && EnvironmentManager.Current.Id == EnvironmentId.Fishing)
            {
                fishLog.Open();
                return;
            }
            if (guestBook != null && EnvironmentManager.Current.Id == EnvironmentId.Door)
            {
                guestBook.Open();
                return;
            }
            if (gridLog != null && EnvironmentManager.Current.Id == EnvironmentId.Wind)
            {
                gridLog.Open();
                return;
            }
            if (crateLog != null && EnvironmentManager.Current.Id == EnvironmentId.Vinyl)
            {
                crateLog.Open();
                return;
            }
            if (intervalLogs != null)
            {
                foreach (var log in intervalLogs)
                {
                    if (log == null || log.Environment == null || log.Environment.Def.Id != EnvironmentManager.Current.Id) continue;
                    log.Open();
                    return;
                }
            }

            int passed = RouteMap.CountPassed(drive.MaxDistanceKmPrecise);
            var en = CultureInfo.GetCultureInfo("en-US");
            titleText.text = "Passport · " + DateTime.Now.ToString("MMMM yyyy", en);
            countText.text = $"{passed} / {RouteMap.Stops.Count} stamps · a new passport every month";

            for (int i = 0; i < stampFrames.Length && i < RouteMap.Stops.Count; i++)
            {
                var stop = RouteMap.Stops[i];
                bool stamped = i < passed;
                var frame = stampFrames[i];
                var text = stampTexts[i];

                if (stamped)
                {
                    var c = RouteMap.Regions[stop.RegionIndex].NearLand;
                    frame.color = c;
                    float lum = c.r * 0.3f + c.g * 0.59f + c.b * 0.11f;
                    text.color = lum > 0.6f ? DarkInk : LightInk;
                    // Damga hissi: her durak kendi sabit, hafif eğik açısıyla.
                    frame.rectTransform.localRotation = Quaternion.Euler(0f, 0f, ((i * 37) % 9 - 4) * 0.8f);
                }
                else
                {
                    frame.color = EmptyFrame;
                    text.color = EmptyText;
                    frame.rectTransform.localRotation = Quaternion.identity;
                }
            }

            passportPanel.SetActive(true);
        }
    }
}
