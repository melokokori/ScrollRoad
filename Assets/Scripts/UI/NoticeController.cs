using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ScrollRoad
{
    // Durak damgası ve hedef tamamlama duyuruları: üst ortada küçük, sade kart (konfeti yok —
    // o şehre özel). Sırayla gösterilir; birikirse en eskiler atılır. Masaüstü modunda kart yerine
    // şeridin alt satırında kısa metin (CompanionText) görünür. Şehir kutlaması sürerken bekler.
    public class NoticeController : MonoBehaviour
    {
        [SerializeField] RouteProgress route;
        [SerializeField] GoalTracker goals;
        [SerializeField] CompanionWindowController companionWindow;
        [SerializeField] CelebrationController celebration;
        [SerializeField] CanvasGroup cardGroup;
        [SerializeField] Text titleText;
        [SerializeField] Text subtitleText;
        [SerializeField] float fadeSeconds = 0.35f;
        [SerializeField] float holdSeconds = 3.5f;
        [SerializeField] int maxQueued = 3;

        struct Notice
        {
            public string Title;
            public string Subtitle;
            public string Short;
        }

        readonly Queue<Notice> queue = new Queue<Notice>();
        Notice current;
        float timer = -1f; // eksi = boşta

        public string CompanionText => timer >= 0f ? current.Short : null;
        public float CompanionAlpha { get; private set; }

        void Awake()
        {
            cardGroup.alpha = 0f;
            cardGroup.gameObject.SetActive(false);
            route.StopReached += HandleStop;
            goals.GoalCompleted += HandleGoal;
        }

        void OnDestroy()
        {
            if (route != null) route.StopReached -= HandleStop;
            if (goals != null) goals.GoalCompleted -= HandleGoal;
        }

        void HandleStop(RouteMap.Stop stop)
        {
            bool region = stop.Kind == RouteMap.StopKind.Region;
            Enqueue(new Notice
            {
                Title = stop.Name,
                Subtitle = region ? "New region · passport stamped" : "Passport stamped",
                Short = region ? $"{stop.Name} · new region" : $"{stop.Name} · stamped",
            });
        }

        void HandleGoal(GoalTracker.Goal goal, bool periodDone)
        {
            string title, subtitle, shortText;
            switch (goal)
            {
                case GoalTracker.Goal.DailyDistance:
                    title = "Daily distance goal complete";
                    shortText = "Daily distance done";
                    subtitle = periodDone ? $"Both goals done today · streak: {Fmt.Count(goals.DayStreak, "day")}"
                        : $"Time goal: {goals.DayMinutes:0} / {goals.DailyMinutesGoal:0} min";
                    break;
                case GoalTracker.Goal.DailyTime:
                    title = "Daily time goal complete";
                    shortText = "Daily time done";
                    subtitle = periodDone ? $"Both goals done today · streak: {Fmt.Count(goals.DayStreak, "day")}"
                        : $"Distance goal: {Fmt.Int((long)goals.DayKm)} / {goals.DailyKmGoal:0} km";
                    break;
                case GoalTracker.Goal.WeeklyDistance:
                    title = "Weekly distance goal complete";
                    shortText = "Weekly distance done";
                    subtitle = periodDone ? $"Both goals done this week · streak: {Fmt.Count(goals.WeekStreak, "week")}"
                        : $"Time goal: {goals.WeekHours:0.0} / {goals.WeeklyHoursGoal:0} h";
                    break;
                default:
                    title = "Weekly time goal complete";
                    shortText = "Weekly time done";
                    subtitle = periodDone ? $"Both goals done this week · streak: {Fmt.Count(goals.WeekStreak, "week")}"
                        : $"Distance goal: {Fmt.Int((long)goals.WeekKm)} / {goals.WeeklyKmGoal:0} km";
                    break;
            }
            Enqueue(new Notice { Title = title, Subtitle = subtitle, Short = shortText });
        }

        void Enqueue(Notice notice)
        {
            queue.Enqueue(notice);
            while (queue.Count > maxQueued) queue.Dequeue();
        }

        void Update()
        {
            if (timer < 0f)
            {
                if (queue.Count == 0 || (celebration != null && celebration.IsShowing)) return;
                current = queue.Dequeue();
                titleText.text = current.Title;
                subtitleText.text = current.Subtitle;
                timer = 0f;
            }

            timer += Time.unscaledDeltaTime;
            float alpha =
                timer < fadeSeconds ? timer / fadeSeconds :
                timer < fadeSeconds + holdSeconds ? 1f :
                Mathf.Clamp01(1f - (timer - fadeSeconds - holdSeconds) / fadeSeconds);

            bool companion = companionWindow != null && companionWindow.IsCompanionMode;
            if (cardGroup.gameObject.activeSelf == companion) cardGroup.gameObject.SetActive(!companion);
            cardGroup.alpha = alpha;
            CompanionAlpha = alpha;

            if (timer >= fadeSeconds * 2f + holdSeconds)
            {
                timer = -1f;
                CompanionAlpha = 0f;
                cardGroup.gameObject.SetActive(false);
            }
        }
    }
}
