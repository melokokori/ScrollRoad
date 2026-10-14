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

        // Görev çubuğu tam ekran bir pencerenin (oyun, video, sunum) altında mı kaldı? Öyleyse şerit de
        // gizlenmeli (yoksa oyunun üstünde yüzer). Görev çubuğunun sağ kenarında (saat) en üstteki pencereye
        // bakılır: görev çubuğunun kendisi değilse ve monitörü tamamen kaplıyorsa örtülmüştür.
        // SHQueryUserNotificationState ve "ön plandaki pencere monitörü kaplıyor mu" kullanılmadı:
        // büyütülmüş Chrome monitör boyutunda görünüp QUNS_BUSY döndürebiliyor, görev çubuğu ise üstte
        // ve görünür kalıyordu — şerit sebepsiz gizleniyordu.
        public static bool IsTaskbarCovered(IntPtr ownWindow)
        {
            var data = new APPBARDATA { cbSize = Marshal.SizeOf<APPBARDATA>() };
            if (SHAppBarMessage(ABM_GETTASKBARPOS, ref data) == UIntPtr.Zero) return false;
            var bar = data.rc;
            var probe = new POINT { X = bar.Right - 4, Y = (bar.Top + bar.Bottom) / 2 };

            IntPtr top = GetAncestor(WindowFromPoint(probe), GA_ROOT);
            if (top == IntPtr.Zero || top == ownWindow) return false;
            var cls = new StringBuilder(64);
            GetClassName(top, cls, cls.Capacity);
            if (cls.ToString() == "Shell_TrayWnd") return false;

            // Araç ipucu, bildirim gibi küçük pencereler saatin üstüne çıkabilir: yalnız monitörü kaplayan sayılır.
            IntPtr monitor = MonitorFromPoint(probe, MONITOR_DEFAULTTONEAREST);
            var info = new MONITORINFO { cbSize = Marshal.SizeOf<MONITORINFO>() };
            if (!GetMonitorInfo(monitor, ref info) || !GetWindowRect(top, out var r)) return false;
            return r.Left <= info.rcMonitor.Left && r.Top <= info.rcMonitor.Top &&
                   r.Right >= info.rcMonitor.Right && r.Bottom >= info.rcMonitor.Bottom;
        }

        public static void Show(IntPtr hwnd, bool visible)
        {
            if (hwnd != IntPtr.Zero) ShowWindow(hwnd, visible ? SW_SHOWNOACTIVATE : SW_HIDE);
        }

        public static bool IsVisible(IntPtr hwnd) => hwnd != IntPtr.Zero && IsWindowVisible(hwnd);

        // --- Kart penceresi ---

        // Başlık çubuğu ve kenarlık olmadan, verilen dış dikdörtgende. WS_MINIMIZEBOX/WS_SYSMENU
        // kalır: görev çubuğu düğmesine tıklayınca simge durumuna küçülme çalışsın. Windows 11'de
        // köşeler DWM ile yuvarlatılır (Windows 10'da çağrı başarısız olur, köşeler düz kalır).
        public static void ApplyBorderless(IntPtr hwnd, ScreenRect bounds)
        {
            if (hwnd == IntPtr.Zero) return;
            long style = GetWindowLongPtr(hwnd, GWL_STYLE).ToInt64();
            style &= ~(long)(WS_CAPTION | WS_THICKFRAME | WS_MAXIMIZEBOX);
            style |= WS_POPUP | WS_MINIMIZEBOX | WS_SYSMENU;
            SetWindowLongPtr(hwnd, GWL_STYLE, new IntPtr(style));

            SetDwmFrame(hwnd, DWMWCP_ROUND, DWMWA_COLOR_DEFAULT, DWMNCRP_USEWINDOWSTYLE);

            SetWindowPos(hwnd, IntPtr.Zero, bounds.Left, bounds.Top, bounds.Width, bounds.Height,
                SWP_NOZORDER | SWP_NOACTIVATE | SWP_NOOWNERZORDER | SWP_FRAMECHANGED);
        }

        // Görev çubuğu şeridi: Windows 11'in pencereye çizdiği yuvarlak köşe, 1 px sistem kenarlığı
        // ve gölge kapatılır — şerit görev çubuğunun parçası gibi görünsün. Kart moduna dönüşte
        // ApplyBorderless varsayılanları geri yükler. (Windows 10'da çağrılar sessizce başarısız olur.)
        public static void ApplyStripFrame(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero) return;
            SetDwmFrame(hwnd, DWMWCP_DONOTROUND, DWMWA_COLOR_NONE, DWMNCRP_DISABLED);
        }

        static void SetDwmFrame(IntPtr hwnd, int corner, int borderColor, int ncPolicy)
        {
            DwmSetWindowAttribute(hwnd, DWMWA_WINDOW_CORNER_PREFERENCE, ref corner, sizeof(int));
            DwmSetWindowAttribute(hwnd, DWMWA_BORDER_COLOR, ref borderColor, sizeof(int));
            DwmSetWindowAttribute(hwnd, DWMWA_NCRENDERING_POLICY, ref ncPolicy, sizeof(int));
        }

        public static bool TryGetBounds(IntPtr hwnd, out ScreenRect bounds)
        {
            bounds = default;
            if (hwnd == IntPtr.Zero || !GetWindowRect(hwnd, out var r)) return false;
            bounds = new ScreenRect { Left = r.Left, Top = r.Top, Width = r.Right - r.Left, Height = r.Bottom - r.Top };
            return true;
        }

        // Stile dokunmadan boyut + konum (sol-üst orijinli fiziksel piksel).
        public static void SetBounds(IntPtr hwnd, ScreenRect bounds)
        {
            if (hwnd == IntPtr.Zero) return;
            SetWindowPos(hwnd, IntPtr.Zero, bounds.Left, bounds.Top, bounds.Width, bounds.Height,
                SWP_NOZORDER | SWP_NOACTIVATE | SWP_NOOWNERZORDER);
        }

        public static void MoveTo(IntPtr hwnd, int left, int top)
        {
            if (hwnd == IntPtr.Zero) return;
            SetWindowPos(hwnd, IntPtr.Zero, left, top, 0, 0, SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE | SWP_NOOWNERZORDER);
        }

        public static void SetTopmost(IntPtr hwnd, bool topmost)
        {
            if (hwnd == IntPtr.Zero) return;
            SetWindowPos(hwnd, topmost ? HWND_TOPMOST : HWND_NOTOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_NOOWNERZORDER);
        }

        public static void Minimize(IntPtr hwnd)
        {
            if (hwnd != IntPtr.Zero) ShowWindow(hwnd, SW_MINIMIZE);
        }

        // Pencerenin bulunduğu monitörün ölçeği (%100 = 1). Pencere yoksa birincil monitör.
        public static float DpiScale(IntPtr hwnd)
        {
            uint dpi = 0;
            try { dpi = hwnd != IntPtr.Zero ? GetDpiForWindow(hwnd) : GetDpiForSystem(); }
            catch (EntryPointNotFoundException) { } // Windows 10 1607 öncesi
            return dpi > 0 ? dpi / 96f : 1f;
        }

        // Noktaya en yakın monitörün çalışma alanı (görev çubuğu hariç).
        public static ScreenRect WorkAreaAt(int x, int y)
        {
            IntPtr monitor = MonitorFromPoint(new POINT { X = x, Y = y }, MONITOR_DEFAULTTONEAREST);
            var info = new MONITORINFO { cbSize = Marshal.SizeOf<MONITORINFO>() };
            if (!GetMonitorInfo(monitor, ref info)) return new ScreenRect { Width = 1920, Height = 1040 };
            var w = info.rcWork;
            return new ScreenRect { Left = w.Left, Top = w.Top, Width = w.Right - w.Left, Height = w.Bottom - w.Top };
        }

        public static bool TryGetCursor(out int x, out int y)
        {
            bool ok = GetCursorPos(out var p);
            x = p.X;
            y = p.Y;
            return ok;
        }

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
        const uint GA_ROOT = 2;
        const uint MONITOR_DEFAULTTONEAREST = 2;
        const int SW_HIDE = 0, SW_SHOWNOACTIVATE = 4, SW_MINIMIZE = 6;
        static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);
        const uint SWP_NOSIZE = 0x0001, SWP_NOMOVE = 0x0002, SWP_NOZORDER = 0x0004, SWP_NOACTIVATE = 0x0010,
            SWP_FRAMECHANGED = 0x0020, SWP_NOOWNERZORDER = 0x0200;
        const int GWL_STYLE = -16;
        const long WS_POPUP = 0x80000000L, WS_CAPTION = 0x00C00000L, WS_SYSMENU = 0x00080000L,
            WS_THICKFRAME = 0x00040000L, WS_MINIMIZEBOX = 0x00020000L, WS_MAXIMIZEBOX = 0x00010000L;
        const int DWMWA_WINDOW_CORNER_PREFERENCE = 33, DWMWCP_DONOTROUND = 1, DWMWCP_ROUND = 2;
        const int DWMWA_BORDER_COLOR = 34, DWMWA_COLOR_DEFAULT = unchecked((int)0xFFFFFFFF), DWMWA_COLOR_NONE = unchecked((int)0xFFFFFFFE);
        const int DWMWA_NCRENDERING_POLICY = 2, DWMNCRP_USEWINDOWSTYLE = 0, DWMNCRP_DISABLED = 1;
        static readonly UIntPtr HKEY_CURRENT_USER = new UIntPtr(0x80000001u);
        const int KEY_QUERY_VALUE = 0x0001;
        const int REG_DWORD = 4;

        [StructLayout(LayoutKind.Sequential)]
        struct RECT { public int Left, Top, Right, Bottom; }

        [StructLayout(LayoutKind.Sequential)]
        struct POINT { public int X, Y; }

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

        [DllImport("user32.dll")]
        static extern IntPtr WindowFromPoint(POINT point);

        [DllImport("user32.dll")]
        static extern IntPtr GetAncestor(IntPtr hwnd, uint flags);

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

        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
        static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
        static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("dwmapi.dll")]
        static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

        [DllImport("user32.dll")]
        static extern uint GetDpiForWindow(IntPtr hwnd);

        [DllImport("user32.dll")]
        static extern uint GetDpiForSystem();

        [DllImport("user32.dll")]
        static extern IntPtr MonitorFromPoint(POINT pt, uint dwFlags);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode)]
        static extern int RegOpenKeyEx(UIntPtr hKey, string subKey, int options, int samDesired, out IntPtr phkResult);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode)]
        static extern int RegQueryValueEx(IntPtr hKey, string valueName, IntPtr reserved, out int type, byte[] data, ref int dataSize);

        [DllImport("advapi32.dll")]
        static extern int RegCloseKey(IntPtr hKey);
    }
}
#endif
