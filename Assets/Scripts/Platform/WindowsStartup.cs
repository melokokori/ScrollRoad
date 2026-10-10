using System;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

namespace ScrollRoad.Platform
{
    // "Windows açılınca başlat": HKCU\...\Run altına exe yolu + "-autostart" yazılır (yönetici izni
    // gerekmez, yalnız bu kullanıcı). Durumun tek kaynağı kayıt defterinin kendisi — PlayerPrefs'te
    // ayrı bir bayrak tutulmaz; kullanıcı Görev Yöneticisi > Başlangıç'tan kapatırsa da doğru görünür.
    // Microsoft.Win32.Registry .NET Standard 2.1 profilinde yok, bu yüzden doğrudan advapi32.
    // Editor'de devre dışı (Unity.exe'yi başlangıca yazmayalım).
    public static class WindowsStartup
    {
        public const string AutostartArg = "-autostart";

        const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        const string ValueName = "ScrollRoad";

        public static bool IsSupported
        {
            get
            {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
                return true;
#else
                return false;
#endif
            }
        }

        public static bool LaunchedFromAutostart =>
            Array.Exists(Environment.GetCommandLineArgs(), a => string.Equals(a, AutostartArg, StringComparison.OrdinalIgnoreCase));

        public static bool IsEnabled()
        {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            string value = ReadValue();
            // Oyun taşındıysa (Steam kütüphanesi değişti) eski yol geçersiz: kapalı say, yeniden açılınca düzelir.
            return value != null && value.IndexOf(ExePath(), StringComparison.OrdinalIgnoreCase) >= 0;
#else
            return false;
#endif
        }

        public static bool SetEnabled(bool enabled)
        {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            if (RegCreateKeyEx(HKEY_CURRENT_USER, RunKeyPath, 0, null, 0, KEY_SET_VALUE | KEY_QUERY_VALUE,
                    IntPtr.Zero, out IntPtr key, out _) != 0)
            {
                Debug.LogWarning("[Startup] Run anahtarı açılamadı.");
                return false;
            }
            try
            {
                int result;
                if (enabled)
                {
                    string command = $"\"{ExePath()}\" {AutostartArg}";
                    byte[] data = Encoding.Unicode.GetBytes(command + "\0");
                    result = RegSetValueEx(key, ValueName, 0, REG_SZ, data, data.Length);
                }
                else
                {
                    result = RegDeleteValue(key, ValueName);
                    if (result == ERROR_FILE_NOT_FOUND) result = 0;
                }
                if (result != 0) Debug.LogWarning($"[Startup] Kayıt defteri yazılamadı (hata {result}).");
                return result == 0;
            }
            finally
            {
                RegCloseKey(key);
            }
#else
            return false;
#endif
        }

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        static readonly UIntPtr HKEY_CURRENT_USER = new UIntPtr(0x80000001u);
        const int KEY_QUERY_VALUE = 0x0001;
        const int KEY_SET_VALUE = 0x0002;
        const int REG_SZ = 1;
        const int ERROR_FILE_NOT_FOUND = 2;

        static string ExePath()
        {
            var sb = new StringBuilder(1024);
            GetModuleFileName(IntPtr.Zero, sb, sb.Capacity);
            return sb.ToString();
        }

        static string ReadValue()
        {
            if (RegOpenKeyEx(HKEY_CURRENT_USER, RunKeyPath, 0, KEY_QUERY_VALUE, out IntPtr key) != 0) return null;
            try
            {
                int size = 0;
                if (RegQueryValueEx(key, ValueName, IntPtr.Zero, out _, null, ref size) != 0 || size <= 0) return null;
                var buffer = new byte[size];
                if (RegQueryValueEx(key, ValueName, IntPtr.Zero, out _, buffer, ref size) != 0) return null;
                return Encoding.Unicode.GetString(buffer, 0, size).TrimEnd('\0');
            }
            finally
            {
                RegCloseKey(key);
            }
        }

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode)]
        static extern int RegOpenKeyEx(UIntPtr hKey, string subKey, int options, int samDesired, out IntPtr phkResult);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode)]
        static extern int RegCreateKeyEx(UIntPtr hKey, string subKey, int reserved, string lpClass, int options,
            int samDesired, IntPtr lpSecurityAttributes, out IntPtr phkResult, out int lpdwDisposition);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode)]
        static extern int RegQueryValueEx(IntPtr hKey, string valueName, IntPtr reserved, out int type, byte[] data, ref int dataSize);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode)]
        static extern int RegSetValueEx(IntPtr hKey, string valueName, int reserved, int type, byte[] data, int dataSize);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode)]
        static extern int RegDeleteValue(IntPtr hKey, string valueName);

        [DllImport("advapi32.dll")]
        static extern int RegCloseKey(IntPtr hKey);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        static extern uint GetModuleFileName(IntPtr hModule, StringBuilder lpFilename, int nSize);
#endif
    }
}
