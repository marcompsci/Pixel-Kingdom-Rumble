using PKR.Core;
using UnityEngine;

namespace PKR
{
    /// <summary>WHAT'S NEW: a one-time card for returning players after an update (WhatsNew.Items).</summary>
    public class WhatsNewCard : MonoBehaviour
    {
        public static WhatsNewCard Show(Transform parent)
        {
            var go = new GameObject("WhatsNewCard");
            go.transform.SetParent(parent, false);
            var c = go.AddComponent<WhatsNewCard>();
            var root = UIFactory.CreateCanvas(go.transform, "WhatsNewCanvas", 89, landscape: false, out _);
            UIFactory.Dim(root);
            var panel = UIFactory.Panel(root, new Vector2(900f, 1300f));
            UIFactory.Label(panel, "WHAT'S NEW", 76, TextAnchor.MiddleCenter, 110f, title: true);
            UIFactory.Label(panel, "Version " + WhatsNew.Version, 30, TextAnchor.MiddleCenter, 44f).color = UITheme.Current.subtle;
            foreach (var item in WhatsNew.Items)
                UIFactory.Label(panel, "·  " + item, 32, TextAnchor.MiddleLeft, 120f);
            var ok = UIFactory.Button(panel, "LET'S GO!", c.Close);
            UIFactory.Select(ok);
            UiMotion.ModalIn(root);
            return c;
        }

        public void Close()
        {
            var save = Services.Save;
            if (save != null) { WhatsNew.MarkSeen(save.Data); save.SaveNow(); }
            Destroy(gameObject);
        }
    }
}
