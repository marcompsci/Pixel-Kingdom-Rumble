using PKR.Core;
using UnityEngine;

namespace PKR
{
    /// <summary>
    /// A boarded-up manor window. Zombies pressing against it tear a board off every so often; with no boards left the
    /// window is open and they climb through. Standing next to it and holding USE nails boards back on (+10 points each).
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class Barricade : MonoBehaviour
    {
        public SpriteRenderer[] boards;
        public float tearEvery = 1.1f;
        public float repairEvery = 0.55f;
        public float repairReach = 1.9f;

        public int Boards { get; private set; }
        public bool Open => Boards <= 0;
        /// <summary>Raised for each board the hero nails on (the survival controller pays for it).</summary>
        public event System.Action Repaired;

        BoxCollider2D _box;
        // A disabled collider reports empty bounds, so the window's area is computed from the transform.
        Vector2 Center => transform.TransformPoint(_box.offset);
        Vector2 Size => Vector2.Scale(_box.size, (Vector2)transform.lossyScale);
        float _tearTimer, _repairTimer;
        static readonly Collider2D[] Hits = new Collider2D[8];

        void Awake()
        {
            _box = GetComponent<BoxCollider2D>();
            SetBoards(ZombieRules.BarricadeBoards);
        }

        public void SetBoards(int n)
        {
            Boards = Mathf.Clamp(n, 0, ZombieRules.BarricadeBoards);
            if (boards != null)
                for (int i = 0; i < boards.Length; i++)
                    if (boards[i] != null) boards[i].enabled = i < Boards;
            if (_box != null) _box.enabled = Boards > 0;
        }

        void Update()
        {
            if (Services.State != null && Services.State.State != GameState.Playing) return;
            float dt = Time.deltaTime;

            // Zombies at the window tear it down.
            if (Boards > 0)
            {
                var filter = new ContactFilter2D { useLayerMask = true, layerMask = 1 << PKRLayers.Enemy, useTriggers = false };
                Vector2 size = Size + new Vector2(0.9f, 0f);
                int n = Physics2D.OverlapBox(Center, size, 0f, filter, Hits);
                if (n > 0)
                {
                    _tearTimer += dt * Mathf.Min(n, 3);
                    if (_tearTimer >= tearEvery)
                    {
                        _tearTimer = 0f;
                        SetBoards(Boards - 1);
                        PuffEffect.Play(Center, new Color32(160, 110, 70, 255), boards != null && boards.Length > 0 && boards[0] != null
                                        ? boards[0].sharedMaterial : null, 0.18f, 4f, 0.35f);
                        Sfx.Play(Sfx.Crate, 0.5f, 0.12f);
                    }
                }
                else _tearTimer = Mathf.Max(0f, _tearTimer - dt);
            }

            // The hero nails boards back on.
            var gun = ZombieGunner.Player;
            if (gun == null || Boards >= ZombieRules.BarricadeBoards) { _repairTimer = 0f; return; }
            Vector2 d = (Vector2)gun.transform.position - Center;
            if (Mathf.Abs(d.x) > repairReach || Mathf.Abs(d.y) > 2f || !gun.UseHeld) { _repairTimer = 0f; return; }
            _repairTimer += dt;
            if (_repairTimer < repairEvery) return;
            _repairTimer = 0f;
            // Not while a zombie is standing in the gap.
            var enemyFilter = new ContactFilter2D { useLayerMask = true, layerMask = 1 << PKRLayers.Enemy, useTriggers = false };
            if (Boards == 0 && Physics2D.OverlapBox(Center, Size, 0f, enemyFilter, Hits) > 0) return;
            SetBoards(Boards + 1);
            Sfx.Play(Sfx.Board, 0.6f, 0.1f);
            Repaired?.Invoke();
        }
    }
}
