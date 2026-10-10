#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace ScrollRoad.InputCapture
{
    // Sistem genelindeki fare tekerleğini (oyun odakta olmasa da) WH_MOUSE_LL ile yakalar.
    // Neden Raw Input değil: fare için Raw Input kaydı süreç başına tektir; kendi penceremize
    // RIDEV_INPUTSINK ile kaydetmek Unity'nin (ve Editor'ün) kendi fare girişini çalar.
    // Kanca kendi thread'inde, kendi mesaj döngüsüyle çalışır; callback sadece sayaç ekler —
    // Windows yavaş kalan LL kancayı (LowLevelHooksTimeout) sessizce devreden çıkarır.
    public static class GlobalScrollHook
    {
        public const int WheelDelta = 120; // bir "çentik"

        const int WH_MOUSE_LL = 14;
        const int WM_MOUSEWHEEL = 0x020A;
        const int WM_MOUSEHWHEEL = 0x020E;
        const int WM_QUIT = 0x0012;
        const uint PM_NOREMOVE = 0x0000;
        const uint LLMHF_INJECTED = 0x01;
        const uint LLMHF_LOWER_IL_INJECTED = 0x02;

        // MSLLHOOKSTRUCT: POINT pt (8) | DWORD mouseData (+8) | DWORD flags (+12) | ...
        const int OffsetMouseData = 8;
        const int OffsetFlags = 12;

        delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

        // GC delegeyi toplamasın diye statik alanda tutulur.
        static readonly LowLevelMouseProc Proc = HookCallback;

        static Thread _thread;
        static volatile uint _threadId;
        static IntPtr _hook;

        // Ham tekerlek birimi (120 = 1 çentik). Ana thread Drain ile sıfırlayarak okur.
        static long _pendingSigned;      // gerçek, dikey, işaretli (yukarı +)
        static long _pendingAbs;         // gerçek, dikey, mutlak
        static long _pendingInjectedAbs; // yazılımla üretilmiş (SendInput/makro), dikey+yatay, mutlak
        static long _pendingHorizontalAbs;

        public static bool IsRunning => _hook != IntPtr.Zero;
        public static string LastError { get; private set; }

        public struct Sample
        {
            public long Signed;
            public long Abs;
            public long InjectedAbs;
            public long HorizontalAbs;
        }

        public static void Start()
        {
            if (_thread != null) return;

            LastError = null;
            var ready = new ManualResetEventSlim(false);
            _thread = new Thread(() => Run(ready)) { IsBackground = true, Name = "GlobalScrollHook" };
            _thread.Start();
            ready.Wait(2000);
        }

        public static void Stop()
        {
            var thread = _thread;
            if (thread == null) return;

            if (_threadId != 0) PostThreadMessage(_threadId, WM_QUIT, IntPtr.Zero, IntPtr.Zero);
            thread.Join(1000);
            _thread = null;
        }

        public static Sample Drain()
        {
            return new Sample
            {
                Signed = Interlocked.Exchange(ref _pendingSigned, 0),
                Abs = Interlocked.Exchange(ref _pendingAbs, 0),
                InjectedAbs = Interlocked.Exchange(ref _pendingInjectedAbs, 0),
                HorizontalAbs = Interlocked.Exchange(ref _pendingHorizontalAbs, 0),
            };
        }

        static void Run(ManualResetEventSlim ready)
        {
            // Mesaj kuyruğunu oluştur ki Stop'taki PostThreadMessage kaybolmasın.
            PeekMessage(out _, IntPtr.Zero, 0, 0, PM_NOREMOVE);
            _threadId = GetCurrentThreadId();

            _hook = SetWindowsHookEx(WH_MOUSE_LL, Proc, GetModuleHandle(null), 0);
            if (_hook == IntPtr.Zero) LastError = $"SetWindowsHookEx başarısız (Win32 hata {Marshal.GetLastWin32Error()})";
            ready.Set();

            if (_hook != IntPtr.Zero)
            {
                while (GetMessage(out var msg, IntPtr.Zero, 0, 0) > 0)
                {
                    TranslateMessage(ref msg);
                    DispatchMessage(ref msg);
                }
                UnhookWindowsHookEx(_hook);
                _hook = IntPtr.Zero;
            }

            _threadId = 0;
        }

        static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                int msg = wParam.ToInt32();
                if (msg == WM_MOUSEWHEEL || msg == WM_MOUSEHWHEEL)
                {
                    uint mouseData = (uint)Marshal.ReadInt32(lParam, OffsetMouseData);
                    uint flags = (uint)Marshal.ReadInt32(lParam, OffsetFlags);
                    int delta = (short)(mouseData >> 16);
                    int abs = Math.Abs(delta);

                    if ((flags & (LLMHF_INJECTED | LLMHF_LOWER_IL_INJECTED)) != 0)
                        Interlocked.Add(ref _pendingInjectedAbs, abs);
                    else if (msg == WM_MOUSEHWHEEL)
                        Interlocked.Add(ref _pendingHorizontalAbs, abs);
                    else
                    {
                        Interlocked.Add(ref _pendingSigned, delta);
                        Interlocked.Add(ref _pendingAbs, abs);
                    }
                }
            }
            // Olayı asla yutma: scroll hedef uygulamaya aynen ulaşmalı.
            return CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
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

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")]
        static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        static extern int GetMessage(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool PeekMessage(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax, uint wRemoveMsg);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool TranslateMessage(ref MSG lpMsg);

        [DllImport("user32.dll")]
        static extern IntPtr DispatchMessage(ref MSG lpMsg);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool PostThreadMessage(uint idThread, int msg, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll")]
        static extern uint GetCurrentThreadId();

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        static extern IntPtr GetModuleHandle(string lpModuleName);
    }
}
#endif
