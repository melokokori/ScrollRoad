using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace ScrollRoad.EditorTools
{
    // Editor'de kartı gerçek oranında görmek için Game görünümüne "ScrollRoad Card 800x340" boyutunu
    // ekleyip seçer. Unity'nin bunun için genel bir API'si yok; GameViewSizes iç sınıfına yansımayla
    // erişilir (Unity sürümü değişince kırılırsa yalnız bu menü çalışmaz, oyun etkilenmez).
    public static class GameViewCardSize
    {
        const string Label = "ScrollRoad Card";

        [MenuItem("ScrollRoad/Game görünümü: kart boyutu (800x340)")]
        public static void Apply()
        {
            try
            {
                var asm = typeof(Editor).Assembly;
                var sizesType = asm.GetType("UnityEditor.GameViewSizes");
                var singleton = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
                var sizes = singleton.GetProperty("instance").GetValue(null);
                var group = sizesType.GetMethod("GetGroup").Invoke(sizes, new object[] { (int)GameViewSizeGroupType.Standalone });
                var groupType = group.GetType();

                int index = FindIndex(group, groupType);
                if (index < 0)
                {
                    var sizeType = asm.GetType("UnityEditor.GameViewSize");
                    var kindType = asm.GetType("UnityEditor.GameViewSizeType");
                    var ctor = sizeType.GetConstructor(new[] { kindType, typeof(int), typeof(int), typeof(string) });
                    var size = ctor.Invoke(new object[] { Enum.Parse(kindType, "FixedResolution"), 800, 340, Label });
                    groupType.GetMethod("AddCustomSize").Invoke(group, new[] { size });
                    sizesType.GetMethod("SaveToHDD")?.Invoke(sizes, null);
                    index = FindIndex(group, groupType);
                }

                var gameViewType = asm.GetType("UnityEditor.GameView");
                var gameView = EditorWindow.GetWindow(gameViewType);
                const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
                var select = gameViewType.GetMethod("SizeSelectionCallback", flags);
                if (select != null) select.Invoke(gameView, new object[] { index, null });
                else gameViewType.GetProperty("selectedSizeIndex", flags)?.SetValue(gameView, index);
                gameView.Repaint();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Game görünümü boyutu ayarlanamadı (Unity iç API'si değişmiş olabilir): {e.Message}");
            }
        }

        static int FindIndex(object group, Type groupType)
        {
            var texts = (string[])groupType.GetMethod("GetDisplayTexts").Invoke(group, null);
            for (int i = 0; i < texts.Length; i++) if (texts[i].StartsWith(Label)) return i;
            return -1;
        }
    }
}
