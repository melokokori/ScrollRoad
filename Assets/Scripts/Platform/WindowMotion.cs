#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
using System;
using System.Collections;
using UnityEngine;

namespace ScrollRoad.Platform
{
    // Pencerenin kendisinin hareketi (2026-10-26): konum + bütün pencere opaklığı. Kart ↔ şerit geçişi,
    // şeridin yapışması, tepsiye gizlenme ve ortam değişimi bunu kullanır — ekrana yeni bir şey çizilmez.
    // Yalnız build'de çağrılır (Editor'de pencere Editor'ün kendisi olurdu).
    public static class WindowMotion
    {
        // alphaFrom/alphaTo negatifse opaklığa dokunulmaz (yalnız konum).
        public static IEnumerator Glide(IntPtr hwnd, NativeWindow.ScreenRect from, NativeWindow.ScreenRect to,
            float alphaFrom, float alphaTo, float seconds, Func<float, float> ease)
        {
            float start = Time.realtimeSinceStartup;
            bool fade = alphaFrom >= 0f && alphaTo >= 0f;
            while (true)
            {
                float t = seconds > 0f ? Mathf.Clamp01((Time.realtimeSinceStartup - start) / seconds) : 1f;
                float e = ease(t);
                NativeWindow.MoveTo(hwnd, Mathf.RoundToInt(Mathf.LerpUnclamped(from.Left, to.Left, e)), Mathf.RoundToInt(Mathf.LerpUnclamped(from.Top, to.Top, e)));
                if (fade) NativeWindow.SetOpacity(hwnd, Mathf.Lerp(alphaFrom, alphaTo, e));
                if (t >= 1f) yield break;
                yield return null;
            }
        }

        // r'nin merkezini target'ın merkezine doğru verilen oranda kaydırır (boyut aynı kalır).
        public static NativeWindow.ScreenRect Toward(NativeWindow.ScreenRect r, NativeWindow.ScreenRect target, float fraction)
        {
            r.Left += Mathf.RoundToInt((target.Left + target.Width / 2 - (r.Left + r.Width / 2)) * fraction);
            r.Top += Mathf.RoundToInt((target.Top + target.Height / 2 - (r.Top + r.Height / 2)) * fraction);
            return r;
        }

        public static NativeWindow.ScreenRect Offset(NativeWindow.ScreenRect r, int dx, int dy)
        {
            r.Left += dx;
            r.Top += dy;
            return r;
        }

        public static float EaseIn(float t) => t * t * t;
        public static float EaseOut(float t) => 1f - (1f - t) * (1f - t) * (1f - t);

        public static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f, c3 = c1 + 1f;
            float u = t - 1f;
            return 1f + c3 * u * u * u + c1 * u * u;
        }
    }
}
#endif
