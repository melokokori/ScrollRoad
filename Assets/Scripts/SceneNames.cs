using System.Globalization;
using UnityEngine;

namespace ScrollRoad
{
    // Sahne adları tek yerde — hem runtime kod hem sahne kurucu aynı sabitleri kullanır.
    public static class SceneNames
    {
        public const string MainMenu = "MainMenu";
        public const string Game = "Game";
    }

    // UI sayı biçimi (İngilizce: binlik ayırıcı virgül, ondalık nokta).
    public static class Fmt
    {
        static readonly CultureInfo En = CultureInfo.GetCultureInfo("en-US");

        // Oyun İngilizce: $"{x:0.0}" gibi biçimler sistemin bölge ayarına (ör. tr-TR "0,0") kaymasın.
        // Diğer diller gelince bu, seçili dile bağlanacak.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void UseGameCulture()
        {
            CultureInfo.DefaultThreadCurrentCulture = En;
            CultureInfo.CurrentCulture = En;
        }

        public static string Km(double km, int decimals = 1) => km.ToString("N" + decimals, En) + " km";
        public static string Int(long value) => value.ToString("N0", En);

        // "1 day", "3 days" (yalnız düzenli çoğul alan birimler için).
        public static string Count(long value, string unit) => $"{Int(value)} {unit}{(value == 1 ? "" : "s")}";
    }
}
