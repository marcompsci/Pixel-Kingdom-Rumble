using PKR.Core;
using UnityEngine;

namespace PKR
{
    /// <summary>An ancient relic: worth points in a Shadow Contract. Touch to take it.</summary>
    [RequireComponent(typeof(Collider2D))]
    public class RelicPickup : MonoBehaviour
    {
        public string relicName = "Ancient Relic";
        bool _taken;
        float _age;
        Transform _art;

        void Awake()
        {
            GetComponent<Collider2D>().isTrigger = true;
            _art = transform.childCount > 0 ? transform.GetChild(0) : null;
            _age = Random.value * 3f;
        }

        void Update()
        {
            _age += Time.deltaTime;
            if (_art != null) _art.localPosition = new Vector3(0f, Mathf.Sin(_age * 2.5f) * 0.1f, 0f);
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (_taken || !TriggerUtil.IsPlayer(other, out _)) return;
            _taken = true;
            if (MissionTracker.Current != null) MissionTracker.Current.CollectRelic(relicName);
            var sr = GetComponentInChildren<SpriteRenderer>();
            PuffEffect.Play(transform.position, new Color32(120, 230, 210, 255), sr != null ? sr.sharedMaterial : null, 0.2f, 3.5f, 0.45f);
            gameObject.SetActive(false);
        }
    }
}
