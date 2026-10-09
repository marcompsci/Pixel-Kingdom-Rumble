using UnityEngine;

namespace PKR
{
    /// <summary>End of a Story Quest level. Touching it completes the run.</summary>
    [RequireComponent(typeof(Collider2D))]
    public class GoalGate : MonoBehaviour
    {
        void Awake() => GetComponent<Collider2D>().isTrigger = true;

        void OnTriggerStay2D(Collider2D other) => OnTriggerEnter2D(other); // standing in it after grabbing the objective

        void OnTriggerEnter2D(Collider2D other)
        {
            if (!TriggerUtil.IsPlayer(other, out _)) return;
            // Stealth levels: the gate stays shut until the objective is stolen.
            if (StealthTracker.Current != null && !StealthTracker.Current.CanFinish()) return;
            var flow = LevelFlowController.Current;
            if (flow != null) flow.CompleteLevel();
        }
    }
}
