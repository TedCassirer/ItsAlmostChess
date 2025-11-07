namespace Core.AI {
    public abstract class AIPlayer: Player {
        protected AIPlayer(Board board) : base(board) {
        }

        public override void Update() {
            // AI does not need to update per frame
        }

        public override void NotifyTurnToPlay() {
            Move move = ChooseMove();
            ChooseMove(move);
        }
        
        protected abstract Move ChooseMove();
    }
}