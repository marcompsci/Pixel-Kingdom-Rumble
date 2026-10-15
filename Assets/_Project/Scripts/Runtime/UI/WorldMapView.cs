using UnityEngine;

namespace PKR
{
    /// <summary>
    /// Animates the level-select world map: the marker hops along to the chosen node (a little arc, then a bob while it
    /// waits) and the chosen node gently pulses. Unscaled time, so it runs on menus with the game paused.
    /// </summary>
    public class WorldMapView : MonoBehaviour
    {
        RectTransform[] _nodes = new RectTransform[0];
        RectTransform _marker;
        int _target = -1;
        Vector2 _from, _to;
        float _hop = 1f;
        const float HopTime = 0.35f, HopHeight = 60f, MarkerLift = 86f;

        public int Target => _target;
        public bool Hopping => _hop < 1f;

        public void Init(RectTransform[] nodes, RectTransform marker, int start)
        {
            _nodes = nodes ?? new RectTransform[0];
            _marker = marker;
            if (_marker != null) _marker.SetAsLastSibling();
            _target = -1;
            if (start >= 0) MoveTo(start, true);
        }

        Vector2 Spot(int i)
        {
            var n = _nodes[i];
            var parent = (RectTransform)transform;
            float w = parent.rect.width > 1f ? parent.rect.width : 904f;
            return new Vector2(n.anchorMin.x * w, n.anchoredPosition.y + MarkerLift);
        }

        public void MoveTo(int index, bool instant = false)
        {
            if (index < 0 || index >= _nodes.Length) return;
            if (_target >= 0 && _target < _nodes.Length) _nodes[_target].localScale = Vector3.one;
            _from = _marker != null && !instant && _target >= 0 ? _marker.anchoredPosition : Spot(index);
            _target = index;
            _to = Spot(index);
            _hop = instant ? 1f : 0f;
            if (_marker != null && instant) _marker.anchoredPosition = _to;
        }

        void Update()
        {
            if (_marker == null || _target < 0 || _target >= _nodes.Length) return;
            float dt = Time.unscaledDeltaTime;
            _to = Spot(_target); // the layout may settle after the first frame
            if (_hop < 1f)
            {
                _hop = Mathf.Min(1f, _hop + dt / HopTime);
                float e = _hop * _hop * (3f - 2f * _hop);
                _marker.anchoredPosition = Vector2.Lerp(_from, _to, e) + Vector2.up * (Mathf.Sin(_hop * Mathf.PI) * HopHeight);
                _marker.localScale = new Vector3(1f + 0.15f * Mathf.Sin(_hop * Mathf.PI), 1f - 0.1f * Mathf.Sin(_hop * Mathf.PI), 1f);
            }
            else
            {
                float t = Time.unscaledTime;
                _marker.anchoredPosition = _to + Vector2.up * (Mathf.Abs(Mathf.Sin(t * 3f)) * 10f);
                _marker.localScale = Vector3.one;
            }
            float pulse = 1f + 0.06f * Mathf.Sin(Time.unscaledTime * 4f);
            _nodes[_target].localScale = new Vector3(pulse, pulse, 1f);
        }
    }
}
