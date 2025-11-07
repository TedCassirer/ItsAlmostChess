using System.Threading.Tasks;

namespace Core.AI {
    public abstract class AIPlayer : Player {
        protected AIPlayer(Board board) : base(board) {
        }

        public override void Update() {
            // AI does not need to update per frame
        }

        public override void NotifyTurnToPlay() {
            Move? move = GetNextMove();
            if (move.HasValue) {
                ChooseMove(move.Value);
            }
        }

        protected abstract Move? GetNextMove();
    }
}