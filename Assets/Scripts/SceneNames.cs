using System.Globalization;

namespace ScrollRoad
{
    // Sahne adları tek yerde — hem runtime kod hem sahne kurucu aynı sabitleri kullanır.
    public static class SceneNames
    {
        public const string MainMenu = "MainMenu";
        public const string Game = "Game";
    }

    // UI sayı biçimi (Türkçe: binlik ayırıcı nokta, ondalık virgül).
    public static class Fmt
    {
        static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");

        public static string Km(double km, int decimals = 1) => km.ToString("N" + decimals, Tr) + " km";
        public static string Int(long value) => value.ToString("N0", Tr);
    }
}
