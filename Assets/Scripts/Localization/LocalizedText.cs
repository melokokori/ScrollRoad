using TMPro;
using UnityEngine;

namespace ScrollRoad
{
    // Sahne kurucusunun yazdığı sabit etiketler (başlık, düğme, ayar satırı) için: metin anahtardır (İngilizce),
    // açılışta ve dil değişince çevirisiyle değiştirilir. args doluysa anahtar bir biçimdir ("Counted in {0}.")
    // ve her arg da ayrı çevrilip yerine konur. Kod etiketi sonradan kendisi yazdıysa (metin son uygulanan
    // çeviriden farklıysa) dokunmaz — o metni yazan bileşen kendi yenilemesini yapar.
    [RequireComponent(typeof(TMP_Text))]
    public class LocalizedText : MonoBehaviour
    {
        [SerializeField] string key;
        [SerializeField] string[] args;

        TMP_Text text;
        string applied;

        public string Key => key;
        public string[] Args => args;

        void Awake()
        {
            text = GetComponent<TMP_Text>();
            applied = text.text; // kurucunun yazdığı İngilizce metin
            Apply();
            L.Changed += Apply;
        }

        void OnDestroy() => L.Changed -= Apply;

        void Apply()
        {
            if (text == null || text.text != applied) return;
            if (args == null || args.Length == 0) applied = L.T(key);
            else
            {
                var translated = new object[args.Length];
                for (int i = 0; i < args.Length; i++) translated[i] = L.T(args[i]);
                applied = L.F(key, translated);
            }
            text.text = applied;
        }
    }
}
