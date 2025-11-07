using UnityEngine;

namespace Core {
    public abstract class Player : MonoBehaviour {
        public event System.Action<Move?> OnMoveChosen;

        protected Board Board { get; private set; }
        protected bool IsTurnActive { get; private set; }

        // Initialization entry point called by GameManager
        public void Init(Board board) {
            Board = board;
            OnInitialized();
        }

        // Optional hook for derived classes after board is set
        protected virtual void OnInitialized() {
        }

        // GameManager explicitly drives per-frame logic via Tick instead of relying on Unity's automatic Update
        public virtual void Tick() {
        }

        // Called by GameManager when it's this player's turn
        public void NotifyTurnToPlay() {
            IsTurnActive = true;
            OnTurnStarted();
        }

        // Derived classes implement their turn-start behavior here
        protected virtual void OnTurnStarted() {
        }

        protected void ChooseMove(Move move) {
            if (!IsTurnActive) return; // Ignore if not our turn (defensive)
            IsTurnActive = false; // End turn
            OnMoveChosen?.Invoke(move);
        }

        public bool IsHuman => this is Human;
        public bool IsAI => this is AI.AIPlayer;
    }
}