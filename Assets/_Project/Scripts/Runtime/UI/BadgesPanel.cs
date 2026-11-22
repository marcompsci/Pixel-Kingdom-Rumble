using PKR.Core;
using UnityEngine;
using UnityEngine.UI;

namespace PKR
{
    /// <summary>ALL BADGES: every badge, earned or not, with how close you are. Paged, 9 to a page.</summary>
    public class BadgesPanel : MonoBehaviour
    {
        public const int PerPage = 9;
        RectTransform _canvasRoot, _root;
        System.Action _onClose;
        int _page;
        bool _unearnedOnly;

        public bool UnearnedOnly => _unearnedOnly;
        public void SetUnearnedOnly(bool on) { _unearnedOnly = on; _page = 0; Build(); }

        System.Collections.Generic.List<Badges.Badge> Shown()
        {
            var s = Services.Save != null ? Services.Save.Data : null;
            var o = new System.Collections.Generic.List<Badges.Badge>();
            foreach (var b in Badges.All) if (!_unearnedOnly || !Badges.Has(s, b.id)) o.Add(b);
            return o;
        }

        int PageCount => System.Math.Max(1, (Shown().Count + PerPage - 1) / PerPage);

        public int Page => _page;
        public static int Pages => (Badges.All.Length + PerPage - 1) / PerPage;

        public static BadgesPanel Show(Transform parent, System.Action onClose = null)
        {
            var go = new GameObject("BadgesPanel");
            go.transform.SetParent(parent, false);
            var p = go.AddComponent<BadgesPanel>();
            p._onClose = onClose;
            p._canvasRoot = UIFactory.CreateCanvas(go.transform, "BadgesCanvas", 89, landscape: false, out _);
            p.Build();
            UiMotion.ModalIn(p._canvasRoot);
            return p;
        }

        public void SetPage(int page)
        {
            int n = PageCount;
            _page = ((page % n) + n) % n;
            Build();
        }

        void Build()
        {
            UIFactory.Clear(_canvasRoot);
            UIFactory.Dim(_canvasRoot);
            var s = Services.Save != null ? Services.Save.Data : null;
            var th = UITheme.Current;
            _root = UIFactory.Panel(_canvasRoot, new Vector2(960f, 1640f));
            int earned = s != null ? s.lifetime.badges.Count : 0;
            UIFactory.Label(_root, "ALL BADGES", 72, TextAnchor.MiddleCenter, 100f, title: true);
            var shown = Shown();
            UIFactory.Label(_root, $"{earned}/{Badges.All.Length} earned   ·   page {_page + 1}/{PageCount}   ·   {BadgeRewards.ToNext(s)} more for +{BadgeRewards.Shards} shards", 28,
                            TextAnchor.MiddleCenter, 46f).color = th.subtle;
            if (shown.Count == 0) UIFactory.Label(_root, "Every badge earned!", 40, TextAnchor.MiddleCenter, 112f).color = th.title;
            for (int i = _page * PerPage; i < shown.Count && i < (_page + 1) * PerPage; i++)
            {
                var b = shown[i];
                bool has = Badges.Has(s, b.id);
                string progress = has ? "EARNED" : b.need > 1 && s != null ? $"{b.Have(s):N0}/{b.need:N0}" : "not yet";
                var t = UIFactory.Label(_root, $"{b.name}   <size=26>{progress}</size>\n<size=26>{b.how}</size>", 36, TextAnchor.MiddleCenter, 112f);
                t.color = has ? th.title : th.text;
            }
            var row = UIFactory.Rect("Buttons", _root);
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = UIFactory.RowHeight;
            var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 24f;
            h.childControlWidth = true; h.childControlHeight = true;
            h.childForceExpandWidth = true; h.childForceExpandHeight = true;
            UIFactory.Button(row, "< PREV", () => SetPage(_page - 1));
            var close = UIFactory.Button(row, "CLOSE", Close);
            UIFactory.Button(row, _unearnedOnly ? "SHOW ALL" : "TO EARN", () => SetUnearnedOnly(!_unearnedOnly));
            UIFactory.Button(row, "NEXT >", () => SetPage(_page + 1));
            UIFactory.Select(close);
        }

        public void Close()
        {
            Destroy(gameObject);
            _onClose?.Invoke();
        }
    }
}
