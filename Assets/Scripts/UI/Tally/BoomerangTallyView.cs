using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ScrollRoad
{
    // Bumerang: çetele tahtası. Her vurulan hedefte yeni bir çentik atılır (dört dik + bir çapraz = beşli grup);
    // tahtaya beş grup sığar, yirmi beşincide tahta silinip baştan başlar. Üstte bu ayın hedef sayısı.
    public class BoomerangTallyView : TallyView
    {
        [SerializeField] Image body;
        [SerializeField] TMP_Text count;
        [SerializeField] Image[] marks;   // 25; pivotları çentiğin başladığı uçta

        public const int Board = 25;
        const float CarveTime = 0.22f;

        long shownCount = -1;
        int newMark = -1;
        float carveAge = 99f;

        protected override void ApplySet(string set, SkinInfo skin)
        {
            body.sprite = Load(set, "body");
            count.color = skin.Ink;
            foreach (var m in marks) m.color = skin.Ink;
        }

        protected override void OnUnit(long whole)
        {
            newMark = (int)((whole - 1) % Board);
            carveAge = 0f;
        }

        protected override void Tick(double units, float dt)
        {
            long whole = (long)System.Math.Floor(units);
            if (whole != shownCount)
            {
                shownCount = whole;
                count.SetText("{0}", whole);
            }
            carveAge += dt;
            int onBoard = whole <= 0 ? 0 : (int)((whole - 1) % Board) + 1;
            for (int i = 0; i < marks.Length; i++)
            {
                marks[i].enabled = i < onBoard;
                float s = i == newMark && carveAge < CarveTime ? carveAge / CarveTime : 1f;
                marks[i].rectTransform.localScale = new Vector3(1f, s, 1f);
            }
        }
    }
}
