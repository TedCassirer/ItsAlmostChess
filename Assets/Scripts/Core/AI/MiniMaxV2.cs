using System;
using System.Collections.Generic;
using System.Linq;
using Random = System.Random;

namespace Core.AI {
    public class MiniMaxV2 : IMoveProvider {
        private static readonly Random _random = new();

        private static readonly Dictionary<int, int> PieceValues = new() {
            { Piece.Pawn, 100 },
            { Piece.Knight, 300 },
            { Piece.Bishop, 300 },
            { Piece.Rook, 500 },
            { Piece.Queen, 900 },
            { Piece.King, 9000 }
        };

        private const int MaxDepth = 4;

        public Move? GetNextMove(Board board) {
            var moveGenerator = new MoveGenerator(board);
            List<Move> legalMoves = moveGenerator.LegalMoves();
            if (legalMoves.Count == 0) return null;

            var bestScore = int.MinValue;
            List<Move> bestMoves = new();

            legalMoves.AsParallel()
                .WithDegreeOfParallelism(Environment.ProcessorCount)
                .Select(move => {
                    Board clone = board.Clone();
                    clone.CommitMove(move);
                    var moveScore = -MiniMax(clone, 1);
                    return (move, moveScore);
                })
                .AsSequential()
                .ToList()
                .ForEach(tuple => {
                    (Move move, var moveScore) = tuple;
                    if (moveScore > bestScore) {
                        bestScore = moveScore;
                        bestMoves.Clear();
                        bestMoves.Add(move);
                    }
                    else if (moveScore == bestScore) {
                        bestMoves.Add(move);
                    }
                });


            var randomIndex = _random.Next(bestMoves.Count);
            return bestMoves[randomIndex];
        }

        private int MiniMax(Board board, int depth, int alpha = int.MinValue, int beta = int.MaxValue) {
            if (depth == MaxDepth) return BoardEvaluation(board);

            var movesGenerator = new MoveGenerator(board);
            List<Move> legalMoves = movesGenerator.LegalMoves().OrderBy(m => -m.CapturedPiece).ToList();

            if (legalMoves.Count == 0) {
                if (movesGenerator.InCheck) {
                    return -10_000; // Checkmate
                }

                return 0; // Stalemate
            }

            var bestScore = int.MinValue;
            foreach (Move mv in legalMoves) {
                board.CommitMove(mv);
                var score = -MiniMax(board, depth + 1, -beta, -alpha);
                board.UndoMove();

                bestScore = Math.Max(bestScore, score);
                alpha = Math.Max(alpha, score);

                if (alpha >= beta) break;
            }

            return bestScore;
        }

        private int BoardEvaluation(Board board) {
            var score = 0;
            foreach ((var piece, Coord _) in board.GetPieceLocations())
                score += PieceValues[Piece.Type(piece)] * (Piece.IsColor(piece, Piece.White) ? 1 : -1);

            return score * (board.IsWhitesTurn ? 1 : -1);
        }
    }
}