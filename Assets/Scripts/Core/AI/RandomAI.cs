using System.Collections.Generic;

namespace Core.AI {
    public class RandomAI : AIPlayer {
        public RandomAI(Board board) : base(board) {
        }


        protected override Move? GetNextMove() {
            List<Move> legalMoves = GetLegalMoves();
            if (legalMoves.Count == 0) {
                throw new System.InvalidOperationException("No legal moves available.");
            }

            var randomIndex = UnityEngine.Random.Range(0, legalMoves.Count);
            if (legalMoves.Count == 0) return null;

            return legalMoves[randomIndex];
        }
    }
}