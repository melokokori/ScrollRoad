using TMPro;
using UnityEngine;

namespace ScrollRoad
{
    // Eşit aralıklı ortamların kart bilgi katmanı (Rüzgârgülü HUD'unun yerleşimiyle): sol üstte koyu plakada
    // bu ayın sayısı + durum, sağ üstte yeşil tabelada mekân + sıradaki öğe (5 öğe içinde nadir bir öğe varsa
    // onun adı), sol altta defter özeti. Bugünkü hedef plakası (sağ alt) ortak (HudController).
    public class IntervalHudController : MonoBehaviour
    {
        [SerializeField] IntervalEnvironment env;
        [SerializeField] GameObject root;
        [SerializeField] TMP_Text labelText;
        [SerializeField] TMP_Text totalText;
        [SerializeField] TMP_Text statusText;
        [SerializeField] TMP_Text placeText;
        [SerializeField] TMP_Text phaseText;
        [SerializeField] TMP_Text nextText;
        [SerializeField] TMP_Text logText;

        const int LookAhead = 5;

        static readonly Color Idle = new Color(0.949f, 0.702f, 0.239f);
        static readonly Color Normal = new Color(0.957f, 0.945f, 0.902f, 0.75f);

        void Update()
        {
            bool show = env != null && EnvironmentManager.Current.Id == env.Def.Id;
            if (root.activeSelf != show) root.SetActive(show);
            if (!show) return;

            if (labelText != null) labelText.SetText(env.TotalLabel);
            if (totalText != null) totalText.SetText(env.Def.Format(env.Done * env.Interval));

            var engine = ScoreEngine.Instance;
            bool active = engine != null && engine.IsUserActive;
            if (env.Direction < 0f)
            {
                statusText.text = env.StatusBack;
                statusText.color = Normal;
            }
            else if (!active)
            {
                statusText.text = env.StatusIdle;
                statusText.color = Idle;
            }
            else
            {
                statusText.text = env.StatusActive;
                statusText.color = Normal;
            }

            placeText.SetText(env.Site.Name.ToUpperInvariant());
            long next = env.Done + 1;
            var item = env.Identify(next);
            string comingName = null, phrase = null;
            if (item.IsSpecial)
            {
                phrase = env.RarePhrase;
                comingName = item.Type.Name;
            }
            else
            {
                for (long i = next + 1; i < next + LookAhead; i++)
                {
                    var b = env.Identify(i);
                    if (!b.IsSpecial) continue;
                    phrase = "Coming up";
                    comingName = b.Type.Name;
                    break;
                }
            }
            nextText.gameObject.SetActive(comingName != null);
            if (comingName != null)
            {
                phaseText.SetText(phrase);
                nextText.SetText(comingName);
            }
            else
            {
                phaseText.SetText(env.NextLine(next, item));
            }

            var last = env.LastSpecial;
            string tail;
            if (env.RecordLabel != null && env.Progress.best > 0f)
                tail = $"  ·  <alpha=#99>{env.RecordLabel}<alpha=#FF>  {env.FormatValue(env.Progress.best)}";
            else
                tail = last != null ? $"  ·  <alpha=#99>LAST<alpha=#FF>  {last.Name}" : "";
            logText.text = $"<alpha=#99>{env.CollectionShort}<alpha=#FF>  {env.LoggedAt(env.Site)} / {env.Site.Specials.Length}{tail}";
        }
    }
}
