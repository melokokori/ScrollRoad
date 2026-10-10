#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
using System;
using System.Runtime.InteropServices;
using System.Text;

namespace ScrollRoad.Platform
{
    // Oyun penceresinin ve görev çubuğunun Win32 tarafı: Unity penceresini bulma, görev çubuğu
    // şeridinin konumu, şeridi görev çubuğunun üstünde tutma, tam ekran uygulama algılama.
    // Koordinatlar fiziksel piksel, sol-üst orijin (Unity oyuncusu DPI-aware).
    public static class NativeWindow
    {
        public struct ScreenRect
        {
            public int Left, Top, Width, Height;
            public int Bottom => Top + Height;
        }

        public static IntPtr FindUnityWindow()
        {
            foundWindow = IntPtr.Zero;
            currentPid = (uint)System.Diagnostics.Process.GetCurrentProcess().Id;
            EnumWindows(EnumProc, IntPtr.Zero);
            return foundWindow;
        }

        // Görev çubuğunun boş kısmında (Windows 11'de simgeler ortalıysa solda, sola dayalıysa
        // bildirim alanının solunda) taskbar yüksekliğinde bir şerit. Taban genişlik 48 px'lik
        // (%100 ölçek) görev çubuğu içindir, ölçekle büyür. scale = taskbar yüksekliği / 48.
        public static bool TryGetTaskbarStrip(int baseWidth, out ScreenRect strip, out float scale)
        {
            strip = default;
            scale = 1f;

            var data = new APPBARDATA { cbSize = Marshal.SizeOf<APPBARDATA>() };
            if (SHAppBarMessage(ABM_GETTASKBARPOS, ref data) == UIntPtr.Zero) return false;
            var bar = data.rc;
            int barHeight = bar.Bottom - bar.Top;
            // Windows 11 görev çubuğu yalnız altta; dikey/üst kenar (Windows 10) desteklenmiyor.
            if (data.uEdge != ABE_BOTTOM || barHeight <= 0 || barHeight > 200) return false;

            scale = barHeight / 48f;
            int width = (int)(baseWidth * scale);
            int margin = (int)(12 * scale);

            int left;
            if (ReadExplorerDword("TaskbarAl", 1) == 0)
            {
                // Simgeler sola dayalı: bildirim alanının (saat/tepsi) hemen soluna.
                int trayLeft = bar.Right - (int)(300 * scale);
                IntPtr tray = FindWindowEx(FindWindow("Shell_TrayWnd", null), IntPtr.Zero, "TrayNotifyWnd", null);
                if (tray != IntPtr.Zero && GetWindowRect(tray, out var trayRect) && trayRect.Right > trayRect.Left) trayLeft = trayRect.Left;
                left = trayLeft - width - margin;
            }
            else
            {
                // Simgeler ortada: sol boşluk. Pencere öğeleri (hava durumu) açıksa onun sağına.
                bool widgets = ReadExplorerDword("TaskbarDa", 0) != 0;
                left = bar.Left + (widgets ? (int)(190 * scale) : margin);
            }

            strip = new ScreenRect { Left = Math.Max(bar.Left, left), Top = bar.Top, Width = width, Height = barHeight };
            return true;
        }

        // Görev çubuğu da "topmost": ona tıklanınca şeridin önüne geçer. Periyodik olarak yeniden öne alınır
        // (odak çalmadan).
        public static void KeepOnTop(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero) return;
            SetWindowPos(hwnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_NOOWNERZORDER);
        }

        // Ön planda tam ekran bir uygulama (oyun, video, sunum) var mı? Varsa görev çubuğu da gizlidir;
        // şerit oyunun üstünde kalmasın.
        public static bool IsFullscreenAppActive(IntPtr ownWindow)
        {
            if (SHQueryUserNotificationState(out int state) == 0 &&
                (state == QUNS_BUSY || state == QUNS_RUNNING_D3D_FULL_SCREEN || state == QUNS_PRESENTATION_MODE))
                return true;

            IntPtr fg = GetForegroundWindow();
            if (fg == IntPtr.Zero || fg == ownWindow) return false;
            var cls = new StringBuilder(64);
            GetClassName(fg, cls, cls.Capacity);
            string name = cls.ToString();
            if (name == "Progman" || name == "WorkerW" || name == "Shell_TrayWnd") return false; // masaüstü / görev çubuğu

            IntPtr monitor = MonitorFromWindow(fg, MONITOR_DEFAULTTONEAREST);
            var info = new MONITORINFO { cbSize = Marshal.SizeOf<MONITORINFO>() };
            if (!GetMonitorInfo(monitor, ref info) || !GetWindowRect(fg, out var r)) return false;
            return r.Left <= info.rcMonitor.Left && r.Top <= info.rcMonitor.Top &&
                   r.Right >= info.rcMonitor.Right && r.Bottom >= info.rcMonitor.Bottom;
        }

        public static void Show(IntPtr hwnd, bool visible)
        {
            if (hwnd != IntPtr.Zero) ShowWindow(hwnd, visible ? SW_SHOWNOACTIVATE : SW_HIDE);
        }

        public static bool IsVisible(IntPtr hwnd) => hwnd != IntPtr.Zero && IsWindowVisible(hwnd);

        static int ReadExplorerDword(string name, int fallback)
        {
            const string path = @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced";
            if (RegOpenKeyEx(HKEY_CURRENT_USER, path, 0, KEY_QUERY_VALUE, out IntPtr key) != 0) return fallback;
            try
            {
                int size = 4;
                var buffer = new byte[4];
                if (RegQueryValueEx(key, name, IntPtr.Zero, out int type, buffer, ref size) != 0 || type != REG_DWORD) return fallback;
                return BitConverter.ToInt32(buffer, 0);
            }
            finally
            {
                RegCloseKey(key);
            }
        }

        // --- Pencere arama ---

        delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        static readonly EnumWindowsProc EnumProc = OnEnumWindow;
        static IntPtr foundWindow;
        static uint currentPid;

        static bool OnEnumWindow(IntPtr hWnd, IntPtr lParam)
        {
            GetWindowThreadProcessId(hWnd, out uint pid);
            if (pid != currentPid) return true;
            var cls = new StringBuilder(64);
            GetClassName(hWnd, cls, cls.Capacity);
            if (cls.ToString() != "UnityWndClass") return true;
            foundWindow = hWnd;
            return false;
        }

        // --- Win32 ---

        const uint ABM_GETTASKBARPOS = 0x00000005;
        const uint ABE_BOTTOM = 3;
        const int QUNS_BUSY = 2, QUNS_RUNNING_D3D_FULL_SCREEN = 3, QUNS_PRESENTATION_MODE = 4;
        const uint MONITOR_DEFAULTTONEAREST = 2;
        const int SW_HIDE = 0, SW_SHOWNOACTIVATE = 4;
        static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        const uint SWP_NOSIZE = 0x0001, SWP_NOMOVE = 0x0002, SWP_NOACTIVATE = 0x0010, SWP_NOOWNERZORDER = 0x0200;
        static readonly UIntPtr HKEY_CURRENT_USER = new UIntPtr(0x80000001u);
        const int KEY_QUERY_VALUE = 0x0001;
        const int REG_DWORD = 4;

        [StructLayout(LayoutKind.Sequential)]
        struct RECT { public int Left, Top, Right, Bottom; }

        [StructLayout(LayoutKind.Sequential)]
        struct APPBARDATA
        {
            public int cbSize;
            public IntPtr hWnd;
            public uint uCallbackMessage;
            public uint uEdge;
            public RECT rc;
            public IntPtr lParam;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct MONITORINFO
        {
            public int cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public uint dwFlags;
        }

        [DllImport("shell32.dll")]
        static extern UIntPtr SHAppBarMessage(uint dwMessage, ref APPBARDATA pData);

        [DllImport("shell32.dll")]
        static extern int SHQueryUserNotificationState(out int pquns);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll")]
        static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        static extern IntPtr FindWindowEx(IntPtr parent, IntPtr childAfter, string lpszClass, string lpszWindow);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode)]
        static extern int RegOpenKeyEx(UIntPtr hKey, string subKey, int options, int samDesired, out IntPtr phkResult);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode)]
        static extern int RegQueryValueEx(IntPtr hKey, string valueName, IntPtr reserved, out int type, byte[] data, ref int dataSize);

        [DllImport("advapi32.dll")]
        static extern int RegCloseKey(IntPtr hKey);
    }
}
#endif
