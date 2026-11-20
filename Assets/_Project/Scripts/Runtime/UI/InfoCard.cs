using System.Collections.Generic;
using UnityEngine;

namespace PKR
{
    /// <summary>A simple full-screen card with a title, lines and one button (the weekly recap and similar notes).</summary>
    public class InfoCard : MonoBehaviour
    {
        System.Action _onClose;

        public static InfoCard Show(Transform parent, string title, string subtitle, IList<string> lines, string button = "NICE!", System.Action onClose = null)
        {
            var go = new GameObject("InfoCard");
            go.transform.SetParent(parent, false);
            var c = go.AddComponent<InfoCard>();
            c._onClose = onClose;
            var root = UIFactory.CreateCanvas(go.transform, "InfoCanvas", 88, landscape: false, out _);
            UIFactory.Dim(root);
            var panel = UIFactory.Panel(root, new Vector2(900f, 380f + 70f * (lines != null ? lines.Count : 0)));
            UIFactory.Label(panel, title, 70, TextAnchor.MiddleCenter, 100f, title: true);
            if (!string.IsNullOrEmpty(subtitle)) UIFactory.Label(panel, subtitle, 30, TextAnchor.MiddleCenter, 44f).color = UITheme.Current.subtle;
            if (lines != null) foreach (var l in lines) UIFactory.Label(panel, l, 34, TextAnchor.MiddleCenter, 58f);
            var ok = UIFactory.Button(panel, button, c.Close);
            UIFactory.Select(ok);
            UiMotion.ModalIn(root);
            return c;
        }

        public void Close()
        {
            Destroy(gameObject);
            _onClose?.Invoke();
        }
    }
}
