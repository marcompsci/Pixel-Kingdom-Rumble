using UnityEngine;

namespace PKR
{
    /// <summary>End of a Story Quest level. Touching it completes the run.</summary>
    [RequireComponent(typeof(Collider2D))]
    public class GoalGate : MonoBehaviour
    {
        void Awake() => GetComponent<Collider2D>().isTrigger = true;

        void OnTriggerEnter2D(Collider2D other)
        {
            if (!TriggerUtil.IsPlayer(other, out _)) return;
            var flow = LevelFlowController.Current;
            if (flow != null) flow.CompleteLevel();
        }
    }
}
