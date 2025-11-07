using System;
using System.Collections.Generic;
using Random = System.Random;

namespace Core.AI {
    public class MinMax : AIPlayer {
        private static readonly Random _random = new();

        private static readonly Dictionary<int, int> PieceValues = new() {
            { Piece.Pawn, 100 },
            { Piece.Knight, 300 },
            { Piece.Bishop, 300 },
            { Piece.Rook, 500 },
            { Piece.Queen, 900 },
            { Piece.King, 9000 }
        };

        private Board _board;
        private const int MaxDepth = 4;

        public MinMax(Board board) : base(board) {
            _board = board;
        }


        protected override Move ChooseMove() {
            List<Move> legalMoves = GetLegalMoves();
            int bestScore = Int32.MaxValue;
            List<Move> bestMoves = new List<Move>();
            foreach (Move move in legalMoves) {
                int moveScore = MiniMax(move, 1, true);
                if (moveScore < bestScore) {
                    bestScore = moveScore;
                    bestMoves.Clear();
                    bestMoves.Add(move);
                }
                else if (moveScore == bestScore) {
                    bestMoves.Add(move);
                }
            }

            var randomIndex = _random.Next(bestMoves.Count);
            return bestMoves[randomIndex];
        }

        private int MiniMax(Move move, int depth, bool isMaximizingPlayer) {
            _board.CommitMove(move);
            if (depth == MaxDepth - 1) {
                var score = BoardEvaluation(_board);
                _board.UndoMove();
                return score;
            }

            if (isMaximizingPlayer) {
                int bestScore = Int32.MinValue;
                foreach (Move mv in GetLegalMoves()) {
                    var moveScore = MiniMax(mv, depth + 1, false);
                    bestScore = Math.Max(moveScore, bestScore);
                }

                _board.UndoMove();
                return bestScore;
            }
            else {
                int bestScore = Int32.MaxValue;
                foreach (Move mv in GetLegalMoves()) {
                    var moveScore = MiniMax(mv, depth + 1, true);
                    bestScore = Math.Min(moveScore, bestScore);
                }

                _board.UndoMove();
                return bestScore;
            }
        }

        private int BoardEvaluation(Board board) {
            int score = 0;
            foreach ((int piece, Coord _) in board.GetPieceLocations()) {
                score += PieceValues[Piece.Type(piece)] * (Piece.IsColor(piece, Piece.White) ? 1 : -1);
            }

            return score;
        }
    }
}