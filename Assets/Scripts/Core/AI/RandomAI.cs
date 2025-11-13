using System;
using System.Collections.Generic;

namespace Core.AI {
    public class RandomAI : IMoveProvider {
        private static readonly Random _random = new();

        public Move? GetNextMove(Board board) {
            MoveGenerator moveGenerator = new MoveGenerator(board);
            List<Move> legalMoves = moveGenerator.LegalMoves();
            if (legalMoves.Count == 0) throw new InvalidOperationException("No legal moves available.");

            var randomIndex = _random.Next(0, legalMoves.Count);
            if (legalMoves.Count == 0) return null;

            return legalMoves[randomIndex];
        }
    }
}