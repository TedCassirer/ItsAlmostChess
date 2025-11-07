using System.Collections.Generic;

namespace Core.AI {
    public class RandomAI : Player {
        private readonly MoveGenerator _moveGenerator;
        
        public RandomAI(MoveGenerator moveGenerator) {
            _moveGenerator = moveGenerator;
        }

        public override void Update() {
            // AI does not need to update per frame
        }

        public override void NotifyTurnToPlay() {
            Move move = ChooseRandomMove();
            ChooseMove(move);
        }

        private Move ChooseRandomMove() {
            List<Move> legalMoves = _moveGenerator.LegalMoves();
            if (legalMoves.Count == 0) {
                throw new System.InvalidOperationException("No legal moves available.");
            }

            var randomIndex = UnityEngine.Random.Range(0, legalMoves.Count);
            return legalMoves[randomIndex];
        }
    }
}