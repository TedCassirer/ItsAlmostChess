namespace Core {
    public abstract class Player {
        public event System.Action<Move> OnMoveChosen;

        public abstract void Update();

        protected virtual void ChooseMove(Move move) {
            OnMoveChosen?.Invoke(move);
        }

        public abstract void NotifyTurnToPlay();
    }
}