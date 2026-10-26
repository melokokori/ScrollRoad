using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace ScrollRoad
{
    // Oyun metinlerinin dili (2026-10-26). Anahtar = İngilizce kaynak metin (gettext gibi): kod
    // L.T("Garage") yazar, tablo Resources/Localization/<kod>.json'da {"Garage": "Garaj"} tutar. Tabloda
    // olmayan anahtar İngilizce kalır (editörde bir kez uyarı). Sözlük/katalog verisi tanım yerinde
    // L.N("Perch") ile işaretlenir (değeri değiştirmez, yalnız Tools/loc/extract.py anahtarı toplasın diye),
    // gösterildiği yerde L.T(name) ile çevrilir — dil değişince yeniden çevrilebilsin.
    // Dil: oyuncu Ayarlar'da seçtiyse o (PlayerPrefs, cihaza özel), yoksa Steam dili, yoksa Windows dili.
    // Seçilmeyen otomatik dil kaydedilmez: Steam dili sonradan değişirse oyun da değişir.
    public static class L
    {
        public readonly struct Language
        {
            public readonly string code, nativeName, culture, steam;
            public Language(string code, string nativeName, string culture, string steam)
            {
                this.code = code;
                this.nativeName = nativeName;
                this.culture = culture;
                this.steam = steam;
            }
        }

        // Steam ilk 10 + Türkçe. steam = SteamApps.GameLanguage API adı.
        public static readonly Language[] All =
        {
            new Language("en", "English", "en-US", "english"),
            new Language("zh-Hans", "简体中文", "zh-CN", "schinese"),
            new Language("ru", "Русский", "ru-RU", "russian"),
            new Language("es", "Español", "es-ES", "spanish"),
            new Language("pt-BR", "Português (Brasil)", "pt-BR", "brazilian"),
            new Language("de", "Deutsch", "de-DE", "german"),
            new Language("ja", "日本語", "ja-JP", "japanese"),
            new Language("fr", "Français", "fr-FR", "french"),
            new Language("ko", "한국어", "ko-KR", "koreana"),
            new Language("pl", "Polski", "pl-PL", "polish"),
            new Language("tr", "Türkçe", "tr-TR", "turkish"),
        };

        const string PrefKey = "ScrollRoad_Language";

        static bool ready, quitHooked;
        static string code = "en";
        static CultureInfo culture = CultureInfo.GetCultureInfo("en-US");
        static Dictionary<string, string> table = new Dictionary<string, string>();
        static readonly HashSet<string> reportedMissing = new HashSet<string>();

        // Dil değişince (Ayarlar). Sürekli yenilenmeyen metinler (LocalizedText, açık paneller) buna abone.
        public static event Action Changed;

        public static string Code { get { EnsureReady(); return code; } }

        // Sayı/tarih biçimi ve büyük/küçük harf seçili dile göre. Genel CurrentCulture da bu olur (TMP'nin
        // UpperCase stili char.ToUpper ile CurrentCulture'ı kullanır: Türkçede i→İ). Kayıtlar kültürden
        // bağımsız (JsonUtility, PlayerPrefs, VehicleArt InvariantCulture ile okur) — 2026-10-26'da tarandı.
        public static CultureInfo Culture { get { EnsureReady(); return culture; } }

        // Dilin Ayarlar'da elle seçilip seçilmediği (seçilmediyse otomatik algılanan dil).
        public static bool IsChosen => PlayerPrefs.HasKey(PrefKey);

        public static string T(string key)
        {
            if (string.IsNullOrEmpty(key)) return key;
            EnsureReady();
            if (code == "en") return key;
            if (table.TryGetValue(key, out var value)) return value;
            ReportMissing(key);
            return key;
        }

        // Biçimli metin: anahtardaki {0}, {1:0.0}… seçili dilin kültürüyle doldurulur.
        public static string F(string key, params object[] args)
        {
            string format = T(key);
            try
            {
                return string.Format(culture, format, args);
            }
            catch (FormatException)
            {
                Debug.LogWarning($"[L] Bozuk çeviri biçimi ({code}): {format}");
                return string.Format(CultureInfo.InvariantCulture, key, args);
            }
        }

        // Katalog verisi işareti: değeri olduğu gibi döndürür (çeviri gösterim yerinde L.T ile).
        public static string N(string key) => key;

        // Sayıya göre çoğul: P(n, "{0} day", "{0} days"). Tablodaki çeviri "|" ile ayrılmış biçimler tutar
        // (dilin kuralı sırasıyla, ör. Rusça "один|несколько|много"); tek biçimli dillerde tek parça.
        public static string P(long n, string singular, string plural)
        {
            EnsureReady();
            string form;
            if (code == "en") form = n == 1 ? singular : plural;
            else
            {
                string[] forms = T(plural).Split('|');
                form = forms[Mathf.Clamp(PluralIndex(n), 0, forms.Length - 1)];
                if (forms.Length == 1 && !table.ContainsKey(plural)) form = n == 1 ? singular : plural;
            }
            return string.Format(culture, form, Fmt.Int(n));
        }

        // Büyük/küçük harf dile göre (Türkçe i→İ). Almancada isimler büyük harfle başlar: cümle içinde
        // küçültülmez.
        public static string Upper(string s) => s?.ToUpper(Culture);
        public static string Lower(string s) => s == null || Code == "de" ? s : s.ToLower(Culture);

        static int PluralIndex(long n)
        {
            n = Math.Abs(n);
            long mod10 = n % 10, mod100 = n % 100;
            switch (code)
            {
                case "ru":
                    if (mod10 == 1 && mod100 != 11) return 0;
                    if (mod10 >= 2 && mod10 <= 4 && (mod100 < 12 || mod100 > 14)) return 1;
                    return 2;
                case "pl":
                    if (n == 1) return 0;
                    if (mod10 >= 2 && mod10 <= 4 && (mod100 < 12 || mod100 > 14)) return 1;
                    return 2;
                case "fr":
                case "pt-BR":
                    return n <= 1 ? 0 : 1;
                default:
                    return n == 1 ? 0 : 1;
            }
        }

        public static void Set(string newCode)
        {
            EnsureReady();
            PlayerPrefs.SetString(PrefKey, newCode);
            PlayerPrefs.Save();
            if (newCode == code) return;
            Load(newCode);
            Changed?.Invoke();
        }

        public static int IndexOf(string languageCode)
        {
            for (int i = 0; i < All.Length; i++)
                if (All[i].code == languageCode) return i;
            return -1;
        }

        static void EnsureReady()
        {
            if (ready) return;
            ready = true;
            // Editor'de Play bitince editörün kültürü en-US'e dönsün (domain reload kapalıysa bir kez bağlanır).
            if (!quitHooked)
            {
                quitHooked = true;
                Application.quitting += () =>
                {
                    CultureInfo.DefaultThreadCurrentCulture = CultureInfo.GetCultureInfo("en-US");
                    CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
                };
            }
            string chosen = PlayerPrefs.GetString(PrefKey, "");
            Load(IndexOf(chosen) >= 0 ? chosen : Detect());
        }

        // Editor'de domain reload kapalıysa önceki Play'den kalan dil temizlensin.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            ready = false;
            code = "en";
            table = new Dictionary<string, string>();
            reportedMissing.Clear();
            Changed = null;
        }

        static string Detect()
        {
            string steam = SteamLanguage();
            if (!string.IsNullOrEmpty(steam))
            {
                foreach (var language in All)
                    if (language.steam == steam) return language.code;
                if (steam == "latam") return "es";
                if (steam == "portuguese") return "pt-BR";
                return "en"; // Steam başka bir dilde: İngilizce
            }
            switch (Application.systemLanguage)
            {
                case SystemLanguage.ChineseSimplified:
                case SystemLanguage.Chinese: return "zh-Hans";
                case SystemLanguage.Russian:
                case SystemLanguage.Ukrainian:
                case SystemLanguage.Belarusian: return "ru";
                case SystemLanguage.Spanish: return "es";
                case SystemLanguage.Portuguese: return "pt-BR";
                case SystemLanguage.German: return "de";
                case SystemLanguage.Japanese: return "ja";
                case SystemLanguage.French: return "fr";
                case SystemLanguage.Korean: return "ko";
                case SystemLanguage.Polish: return "pl";
                case SystemLanguage.Turkish: return "tr";
                default: return "en";
            }
        }

        static string SteamLanguage()
        {
            if (!SteamManager.IsInitialized) return null;
            try
            {
                return Steamworks.SteamApps.GameLanguage;
            }
            catch (Exception)
            {
                return null;
            }
        }

        static void Load(string newCode)
        {
            int index = IndexOf(newCode);
            if (index < 0) { newCode = "en"; index = 0; }
            code = newCode;
            culture = MakeCulture(All[index].culture);
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.CurrentCulture = culture;
            table = new Dictionary<string, string>();
            reportedMissing.Clear();
            if (code == "en") return;
            var asset = Resources.Load<TextAsset>("Localization/" + code);
            if (asset == null)
            {
                Debug.LogWarning($"[L] Dil tablosu yok: Resources/Localization/{code}.json");
                return;
            }
            try
            {
                table = FlatJson.Parse(asset.text);
            }
            catch (FormatException e)
            {
                Debug.LogError($"[L] {code}.json okunamadı: {e.Message}");
            }
            Resources.UnloadAsset(asset);
        }

        // Binlik ayırıcı boşluk olan dillerde (fr, ru, pl) yazı tipinde bulunan bölünmez boşluk (U+00A0)
        // kullanılsın — .NET bazı kültürlerde dar bölünmez boşluk (U+202F) verir.
        static CultureInfo MakeCulture(string name)
        {
            var c = (CultureInfo)CultureInfo.GetCultureInfo(name).Clone();
            var number = c.NumberFormat;
            if (string.IsNullOrWhiteSpace(number.NumberGroupSeparator) || number.NumberGroupSeparator == " ")
                number.NumberGroupSeparator = " ";
            return c;
        }

        static void ReportMissing(string key)
        {
            if (!Application.isEditor || !reportedMissing.Add(key)) return;
            Debug.LogWarning($"[L] Çeviri yok ({code}): \"{key}\"");
        }

        // Yalnız { "anahtar": "değer", ... } biçiminde düz JSON (iç içe nesne/dizi yok).
        static class FlatJson
        {
            public static Dictionary<string, string> Parse(string json)
            {
                var result = new Dictionary<string, string>(1024);
                int i = 0;
                Skip(json, ref i);
                Expect(json, ref i, '{');
                Skip(json, ref i);
                if (Peek(json, i) == '}') return result;
                while (true)
                {
                    Skip(json, ref i);
                    string key = ReadString(json, ref i);
                    Skip(json, ref i);
                    Expect(json, ref i, ':');
                    Skip(json, ref i);
                    string value = ReadString(json, ref i);
                    result[key] = value;
                    Skip(json, ref i);
                    char c = Peek(json, i++);
                    if (c == '}') return result;
                    if (c != ',') throw new FormatException($"',' bekleniyordu ({i})");
                }
            }

            static char Peek(string s, int i) => i < s.Length ? s[i] : '\0';

            static void Skip(string s, ref int i)
            {
                while (i < s.Length && char.IsWhiteSpace(s[i])) i++;
            }

            static void Expect(string s, ref int i, char c)
            {
                if (Peek(s, i) != c) throw new FormatException($"'{c}' bekleniyordu ({i})");
                i++;
            }

            static string ReadString(string s, ref int i)
            {
                Expect(s, ref i, '"');
                var sb = new StringBuilder();
                while (true)
                {
                    if (i >= s.Length) throw new FormatException("bitmemiş metin");
                    char c = s[i++];
                    if (c == '"') return sb.ToString();
                    if (c != '\\') { sb.Append(c); continue; }
                    char e = s[i++];
                    switch (e)
                    {
                        case 'n': sb.Append('\n'); break;
                        case 't': sb.Append('\t'); break;
                        case 'r': sb.Append('\r'); break;
                        case 'b': sb.Append('\b'); break;
                        case 'f': sb.Append('\f'); break;
                        case 'u': sb.Append((char)Convert.ToInt32(s.Substring(i, 4), 16)); i += 4; break;
                        default: sb.Append(e); break;
                    }
                }
            }
        }
    }
}
