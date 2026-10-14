using UnityEngine;

namespace PKR
{
    /// <summary>A key: touch to carry it; a locked door opens for each key you bring to it.</summary>
    [RequireComponent(typeof(Collider2D))]
    public class KeyPickup : MonoBehaviour
    {
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
            if (_art != null) _art.localPosition = new Vector3(0f, Mathf.Sin(_age * 3f) * 0.12f, 0f);
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (_taken || !TriggerUtil.IsPlayer(other, out _)) return;
            _taken = true;
            LevelPuzzleState.AddKey();
            var sr = GetComponentInChildren<SpriteRenderer>();
            PuffEffect.Play(transform.position, new Color32(255, 214, 90, 255), sr != null ? sr.sharedMaterial : null, 0.2f, 3.5f, 0.45f);
            Sfx.Play(Sfx.Relic, 0.8f, 0f);
            EventBus<GameHint>.Raise(new GameHint { id = "key", text = $"KEY! ({LevelPuzzleState.Keys}) TOUCH A LOCKED DOOR TO OPEN IT" });
            gameObject.SetActive(false);
        }
    }
}
