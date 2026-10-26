using System.Globalization;
using UnityEngine;

namespace ScrollRoad
{
    // Sahne adları tek yerde — hem runtime kod hem sahne kurucu aynı sabitleri kullanır.
    // Ayrı ana menü sahnesi yok: oyun doğrudan kartta, yolda açılır.
    public static class SceneNames
    {
        public const string Game = "Game";
    }

    // UI sayı biçimi: seçili dilin kültürüyle (L.Culture; İngilizcede binlik ayırıcı virgül, ondalık nokta).
    public static class Fmt
    {
        static readonly CultureInfo En = CultureInfo.GetCultureInfo("en-US");

        // Açılışta en-US: $"{x:0.0}" gibi biçimler sistemin bölge ayarına (ör. tr-TR sistemde İngilizce oyunda
        // "0,0") kaymasın. Dil yüklenince L genel kültürü seçili dilin kültürüne çevirir (metin ile sayı aynı dilde).
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void UseGameCulture()
        {
            CultureInfo.DefaultThreadCurrentCulture = En;
            CultureInfo.CurrentCulture = En;
        }

        public static string Km(double km, int decimals = 1) => km.ToString("N" + decimals, L.Culture) + " " + L.T("km");
        public static string Int(long value) => value.ToString("N0", L.Culture);

        // Ortam birimiyle miktar: "12.4 kg", "1,234 km" (birim İngilizce verilir, burada çevrilir).
        public static string Amount(double value, int decimals, string unit) => value.ToString("N" + decimals, L.Culture) + " " + L.T(unit);
    }
}
