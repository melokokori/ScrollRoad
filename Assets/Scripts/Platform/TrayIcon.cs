#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
using System;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Threading;

namespace ScrollRoad.Platform
{
    // Windows bildirim alanı (sistem tepsisi) simgesi. GlobalScrollHook gibi kendi thread'inde,
    // kendi gizli penceresi ve mesaj döngüsüyle çalışır: Unity ana thread'i hiç bloklanmaz,
    // pencere gizliyken de simge tıklamaları işlenir. Callback'ler Unity API'sine DOKUNMAZ —
    // komutlar kuyruğa yazılır, ana thread TryDequeue ile okur.
    public static class TrayIcon
    {
        public enum Command { Show, ToggleCompanion, Quit }

        const int IdShow = 1, IdCompanion = 2, IdQuit = 3;

        const uint WM_DESTROY = 0x0002;
        const uint WM_CLOSE = 0x0010;
        const uint WM_NULL = 0x0000;
        const uint WM_COMMAND = 0x0111;
        const uint WM_LBUTTONUP = 0x0202;
        const uint WM_LBUTTONDBLCLK = 0x0203;
        const uint WM_RBUTTONUP = 0x0205;
        const uint WM_APP = 0x8000;
        const uint TrayCallbackMessage = WM_APP + 1;

        const uint NIM_ADD = 0, NIM_MODIFY = 1, NIM_DELETE = 2;
        const uint NIF_MESSAGE = 0x01, NIF_ICON = 0x02, NIF_TIP = 0x04, NIF_INFO = 0x10;
        const uint NIIF_INFO = 0x01, NIIF_NOSOUND = 0x10;

        const uint MF_STRING = 0x0000, MF_CHECKED = 0x0008, MF_SEPARATOR = 0x0800;
        const uint TPM_RIGHTBUTTON = 0x0002, TPM_RETURNCMD = 0x0100, TPM_NONOTIFY = 0x0080;

        const string WindowClassName = "ScrollRoadTrayWindow";
        static readonly IntPtr HWND_MESSAGE = new IntPtr(-3);

        delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        // GC delegeyi toplamasın diye statik alanda tutulur.
        static readonly WndProcDelegate Proc = WndProc;

        static readonly ConcurrentQueue<Command> Commands = new ConcurrentQueue<Command>();
        static readonly object NotifyLock = new object();

        static Thread _thread;
        static IntPtr _hwnd;
        static IntPtr _icon;
        static string _tooltip = "ScrollRoad";
        static volatile bool _companionChecked;

        public static bool IsRunning => _hwnd != IntPtr.Zero;
        public static string LastError { get; private set; }

        // Windows kapanıyor/oturum kapanıyor: pencereyi tepsiye gizlemek yerine gerçekten çıkılmalı.
        // (Message-only pencere WM_QUERYENDSESSION almadığı için sistem metriğinden okunur.)
        public static bool SessionEnding => GetSystemMetrics(SM_SHUTTINGDOWN) != 0;

        const int SM_SHUTTINGDOWN = 0x2000;

        // Sağ tık menüsündeki "Masaüstü modu" işaretini ana thread günceller.
        public static bool CompanionChecked { set => _companionChecked = value; }

        public static void Start(string tooltip)
        {
            if (_thread != null) return;

            LastError = null;
            _tooltip = Truncate(tooltip, 127);
            var ready = new ManualResetEventSlim(false);
            _thread = new Thread(() => Run(ready)) { IsBackground = true, Name = "TrayIcon" };
            _thread.Start();
            ready.Wait(2000);
        }

        public static void Stop()
        {
            var thread = _thread;
            if (thread == null) return;

            // Simge NIM_DELETE ile silinmezse süreç bittikten sonra fare üstüne gelene kadar
            // tepside "hayalet" olarak kalır.
            if (_hwnd != IntPtr.Zero) PostMessage(_hwnd, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
            thread.Join(1000);
            _thread = null;
        }

        public static bool TryDequeue(out Command command) => Commands.TryDequeue(out command);

        public static void SetTooltip(string text)
        {
            _tooltip = Truncate(text, 127);
            if (_hwnd == IntPtr.Zero) return;
            var data = NewData(NIF_TIP);
            data.szTip = _tooltip;
            if (!Notify(NIM_MODIFY, ref data)) AddIcon(); // Explorer yeniden başladı: simge silinmişti
        }

        public static void ShowBalloon(string title, string text)
        {
            if (_hwnd == IntPtr.Zero) return;
            var data = NewData(NIF_INFO);
            data.szInfoTitle = Truncate(title, 63);
            data.szInfo = Truncate(text, 255);
            data.dwInfoFlags = NIIF_INFO | NIIF_NOSOUND;
            Notify(NIM_MODIFY, ref data);
        }

        static void Run(ManualResetEventSlim ready)
        {
            IntPtr hInstance = GetModuleHandle(null);

            var wc = new WNDCLASSEX
            {
                cbSize = (uint)Marshal.SizeOf<WNDCLASSEX>(),
                lpfnWndProc = Marshal.GetFunctionPointerForDelegate(Proc),
                hInstance = hInstance,
                lpszClassName = WindowClassName,
            };
            RegisterClassEx(ref wc); // ikinci kez (editor domain reload) ERROR_CLASS_ALREADY_EXISTS döner, sorun değil

            // Message-only pencere (HWND_MESSAGE): EnumWindows'ta GÖRÜNMEZ. Üst düzey bir pencere
            // UniWindowController'ın "kendi penceremi bul" aramasına takılıyor, saydamlık/boyut oyun
            // penceresi yerine ona uygulanıyordu. Bedeli: "TaskbarCreated" yayını gelmez — Explorer
            // yeniden başlarsa simge SetTooltip'te NIM_MODIFY başarısız olunca geri eklenir.
            IntPtr hwnd = CreateWindowEx(0, WindowClassName, "ScrollRoad Tray", 0, 0, 0, 0, 0,
                HWND_MESSAGE, IntPtr.Zero, hInstance, IntPtr.Zero);
            if (hwnd == IntPtr.Zero)
            {
                LastError = $"CreateWindowEx başarısız (Win32 hata {Marshal.GetLastWin32Error()})";
                ready.Set();
                return;
            }

            _icon = LoadAppIcon();
            _hwnd = hwnd;
            if (!AddIcon()) LastError = "Shell_NotifyIcon(NIM_ADD) başarısız";
            ready.Set();

            while (GetMessage(out var msg, IntPtr.Zero, 0, 0) > 0)
            {
                TranslateMessage(ref msg);
                DispatchMessage(ref msg);
            }

            _hwnd = IntPtr.Zero;
            if (_icon != IntPtr.Zero) { DestroyIcon(_icon); _icon = IntPtr.Zero; }
        }

        static IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            if (msg == TrayCallbackMessage)
            {
                uint mouse = (uint)lParam.ToInt64() & 0xFFFF;
                if (mouse == WM_LBUTTONUP || mouse == WM_LBUTTONDBLCLK) Commands.Enqueue(Command.Show);
                else if (mouse == WM_RBUTTONUP) ShowContextMenu(hWnd);
                return IntPtr.Zero;
            }

            switch (msg)
            {
                case WM_COMMAND:
                    HandleMenuId((int)(wParam.ToInt64() & 0xFFFF));
                    return IntPtr.Zero;
                case WM_CLOSE:
                    var data = NewData(0);
                    Notify(NIM_DELETE, ref data);
                    DestroyWindow(hWnd);
                    return IntPtr.Zero;
                case WM_DESTROY:
                    PostQuitMessage(0);
                    return IntPtr.Zero;
            }
            return DefWindowProc(hWnd, msg, wParam, lParam);
        }

        static void ShowContextMenu(IntPtr hWnd)
        {
            IntPtr menu = CreatePopupMenu();
            AppendMenu(menu, MF_STRING, (UIntPtr)IdShow, "ScrollRoad'u göster");
            AppendMenu(menu, MF_STRING | (_companionChecked ? MF_CHECKED : 0), (UIntPtr)IdCompanion, "Masaüstü modu");
            AppendMenu(menu, MF_SEPARATOR, UIntPtr.Zero, null);
            AppendMenu(menu, MF_STRING, (UIntPtr)IdQuit, "Çıkış");
            SetMenuDefaultItem(menu, IdShow, 0);

            GetCursorPos(out var pt);
            // Bilinen Win32 tuhaflığı: SetForegroundWindow olmadan menü dışına tıklayınca kapanmaz,
            // sonrasındaki WM_NULL olmadan ikinci açılışta hemen kapanır.
            SetForegroundWindow(hWnd);
            int id = TrackPopupMenu(menu, TPM_RIGHTBUTTON | TPM_RETURNCMD | TPM_NONOTIFY, pt.X, pt.Y, 0, hWnd, IntPtr.Zero);
            PostMessage(hWnd, WM_NULL, IntPtr.Zero, IntPtr.Zero);
            DestroyMenu(menu);

            if (id != 0) HandleMenuId(id);
        }

        static void HandleMenuId(int id)
        {
            switch (id)
            {
                case IdShow: Commands.Enqueue(Command.Show); break;
                case IdCompanion: Commands.Enqueue(Command.ToggleCompanion); break;
                case IdQuit: Commands.Enqueue(Command.Quit); break;
            }
        }

        static bool AddIcon()
        {
            var data = NewData(NIF_MESSAGE | NIF_ICON | NIF_TIP);
            data.uCallbackMessage = TrayCallbackMessage;
            data.hIcon = _icon;
            data.szTip = _tooltip;
            return Notify(NIM_ADD, ref data);
        }

        static IntPtr LoadAppIcon()
        {
            // Build'de exe'nin kendi simgesi (Player Settings'teki ikon); bulunamazsa Windows varsayılanı.
            var path = new System.Text.StringBuilder(1024);
            if (GetModuleFileName(IntPtr.Zero, path, path.Capacity) > 0)
            {
                IntPtr[] small = new IntPtr[1];
                if (ExtractIconEx(path.ToString(), 0, null, small, 1) > 0 && small[0] != IntPtr.Zero) return small[0];
            }
            return LoadIcon(IntPtr.Zero, new IntPtr(32512)); // IDI_APPLICATION (paylaşılan, DestroyIcon zararsız)
        }

        static NOTIFYICONDATA NewData(uint flags) => new NOTIFYICONDATA
        {
            cbSize = Marshal.SizeOf<NOTIFYICONDATA>(),
            hWnd = _hwnd,
            uID = 1,
            uFlags = flags,
        };

        static bool Notify(uint message, ref NOTIFYICONDATA data)
        {
            lock (NotifyLock) return Shell_NotifyIcon(message, ref data);
        }

        static string Truncate(string s, int max) => s == null ? "" : s.Length <= max ? s : s.Substring(0, max);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        struct NOTIFYICONDATA
        {
            public int cbSize;
            public IntPtr hWnd;
            public uint uID;
            public uint uFlags;
            public uint uCallbackMessage;
            public IntPtr hIcon;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string szTip;
            public uint dwState;
            public uint dwStateMask;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)] public string szInfo;
            public uint uTimeoutOrVersion;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)] public string szInfoTitle;
            public uint dwInfoFlags;
            public Guid guidItem;
            public IntPtr hBalloonIcon;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        struct WNDCLASSEX
        {
            public uint cbSize;
            public uint style;
            public IntPtr lpfnWndProc;
            public int cbClsExtra;
            public int cbWndExtra;
            public IntPtr hInstance;
            public IntPtr hIcon;
            public IntPtr hCursor;
            public IntPtr hbrBackground;
            public string lpszMenuName;
            public string lpszClassName;
            public IntPtr hIconSm;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct MSG
        {
            public IntPtr hwnd;
            public uint message;
            public IntPtr wParam;
            public IntPtr lParam;
            public uint time;
            public int ptX;
            public int ptY;
            public uint lPrivate;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct POINT { public int X; public int Y; }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool Shell_NotifyIcon(uint dwMessage, ref NOTIFYICONDATA lpData);

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        static extern uint ExtractIconEx(string lpszFile, int nIconIndex, IntPtr[] phiconLarge, IntPtr[] phiconSmall, uint nIcons);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        static extern ushort RegisterClassEx(ref WNDCLASSEX lpwcx);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        static extern IntPtr CreateWindowEx(uint dwExStyle, string lpClassName, string lpWindowName, uint dwStyle,
            int x, int y, int nWidth, int nHeight, IntPtr hWndParent, IntPtr hMenu, IntPtr hInstance, IntPtr lpParam);

        [DllImport("user32.dll")]
        static extern int GetSystemMetrics(int nIndex);

        [DllImport("user32.dll")]
        static extern IntPtr DefWindowProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool DestroyWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        static extern void PostQuitMessage(int nExitCode);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool PostMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        static extern int GetMessage(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool TranslateMessage(ref MSG lpMsg);

        [DllImport("user32.dll")]
        static extern IntPtr DispatchMessage(ref MSG lpMsg);

        [DllImport("user32.dll")]
        static extern IntPtr CreatePopupMenu();

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool AppendMenu(IntPtr hMenu, uint uFlags, UIntPtr uIDNewItem, string lpNewItem);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool SetMenuDefaultItem(IntPtr hMenu, int uItem, int fByPos);

        [DllImport("user32.dll")]
        static extern int TrackPopupMenu(IntPtr hMenu, uint uFlags, int x, int y, int nReserved, IntPtr hWnd, IntPtr prcRect);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool DestroyMenu(IntPtr hMenu);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        static extern IntPtr LoadIcon(IntPtr hInstance, IntPtr lpIconName);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool DestroyIcon(IntPtr hIcon);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        static extern uint GetModuleFileName(IntPtr hModule, System.Text.StringBuilder lpFilename, int nSize);
    }
}
#endif
