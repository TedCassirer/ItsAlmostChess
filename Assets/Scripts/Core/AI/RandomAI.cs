using System;
using System.Collections.Generic;

namespace Core.AI {
    public class RandomAI : IMoveProvider {
        private readonly MoveGenerator _moveGenerator;
        private static readonly Random _random = new();

        public RandomAI(Board board) {
            _moveGenerator = new MoveGenerator(board);
        }

        public Move? GetNextMove() {
            _moveGenerator.Refresh();
            List<Move> legalMoves = _moveGenerator.LegalMoves();
            if (legalMoves.Count == 0) throw new InvalidOperationException("No legal moves available.");

            var randomIndex = _random.Next(0, legalMoves.Count);
            if (legalMoves.Count == 0) return null;

            return legalMoves[randomIndex];
        }
    }
}