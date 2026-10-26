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

            // Durum tabelanın üst satırında, mekânın yanında (Ortam Standardı v2: sol üstte kadran var).
            var engine = ScoreEngine.Instance;
            bool active = engine != null && engine.IsUserActive;
            string status = env.Direction < 0f ? env.StatusBack : !active ? env.StatusIdle : env.StatusActive;
            if (statusText != null)
            {
                statusText.text = status;
                statusText.color = !active && env.Direction >= 0f ? Idle : Normal;
                placeText.SetText(L.Upper(L.T(env.Site.Name)));
            }
            else
            {
                string hex = ColorUtility.ToHtmlStringRGB(!active && env.Direction >= 0f ? Idle : Color.white);
                placeText.text = $"{L.Upper(L.T(env.Site.Name))}  ·  <color=#{hex}>{status}</color>";
            }
            long next = env.Done + 1;
            var item = env.Identify(next);
            string comingName = null, phrase = null;
            if (item.IsSpecial)
            {
                phrase = env.RarePhrase;
                comingName = L.T(item.Type.Name);
            }
            else
            {
                for (long i = next + 1; i < next + LookAhead; i++)
                {
                    var b = env.Identify(i);
                    if (!b.IsSpecial) continue;
                    phrase = L.T("Coming up");
                    comingName = L.T(b.Type.Name);
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
                tail = last != null ? $"  ·  <alpha=#99>{L.T("LAST")}<alpha=#FF>  {L.T(last.Name)}" : "";
            logText.text = $"<alpha=#99>{env.CollectionShort}<alpha=#FF>  {env.LoggedAt(env.Site)} / {env.Site.Specials.Length}{tail}";
        }
    }
}
