using System;
using System.Collections.Generic;
using System.Linq;
using Random = System.Random;

namespace Core.AI {
    public class MiniMaxV2: IMoveProvider {
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
        private const int MaxDepth = 2;

        public MiniMaxV2(Board board) {
            _board = board;
        }


        public Move? GetNextMove() {
            var moveGenerator = new MoveGenerator(_board);
            List<Move> legalMoves = moveGenerator.LegalMoves();
            if (legalMoves.Count == 0) return null;

            int bestScore = Int32.MinValue;
            List<Move> bestMoves = new();

            legalMoves.AsParallel()
                .WithDegreeOfParallelism(Environment.ProcessorCount)
                .Select(move => {
                    Board clone = _board.Clone();
                    clone.CommitMove(move);
                    int moveScore = -MiniMax(clone, 1);
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

        private int MiniMax(Board board, int depth, int alpha = Int32.MinValue, int beta = Int32.MaxValue) {
            if (depth == MaxDepth) {
                return BoardEvaluation(board);
            }

            var movesGenerator = new MoveGenerator(board);
            var legalMoves = movesGenerator.LegalMoves().OrderBy(m => -m.CapturedPiece).ToList();
            if (legalMoves.Count == 0) {
                // TODO: distinguish checkmate vs stalemate using board state (e.g., inCheck)
                return -10_000; // Losing (no legal moves) from side-to-move perspective
            }

            int bestScore = Int32.MinValue;
            foreach (var mv in legalMoves) {
                board.CommitMove(mv);
                int score = -MiniMax(board, depth + 1, -beta, -alpha);
                board.UndoMove();

                if (score > bestScore) {
                    bestScore = score;
                }

                if (score > alpha) {
                    alpha = score;
                }

                if (alpha >= beta) {
                    break;
                }
            }

            return bestScore;
        }

        private int BoardEvaluation(Board board) {
            int score = 0;
            foreach ((int piece, Coord _) in board.GetPieceLocations()) {
                score += PieceValues[Piece.Type(piece)] * (Piece.IsColor(piece, Piece.White) ? 1 : -1);
            }

            return score * (board.IsWhitesTurn ? 1 : -1);
        }
    }
}