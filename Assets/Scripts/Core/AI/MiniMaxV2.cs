using System;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

namespace Core.AI {
    public class MiniMaxV2 : AIPlayer {
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

        public MiniMaxV2(Board board) : base(board) {
            _board = board;
        }


        protected override Move? GetNextMove() {
            var startTime = DateTime.Now;
            List<Move> legalMoves = GetLegalMoves();
            if (legalMoves.Count == 0) return null;

            int bestScore = Int32.MinValue;
            List<Move> bestMoves = new();

            foreach (var move in legalMoves) {
                _board.CommitMove(move);
                int moveScore = -MiniMax(1);
                _board.UndoMove();

                if (moveScore > bestScore) {
                    bestScore = moveScore;
                    bestMoves.Clear();
                    bestMoves.Add(move);
                }
                else if (moveScore == bestScore) {
                    bestMoves.Add(move);
                }
            }

            Debug.Log($"Current board {BoardEvaluation(_board)}; BestScore: {bestScore}; Moves considered: {legalMoves.Count}");
            var randomIndex = _random.Next(bestMoves.Count);
            Debug.Log($"MiniMaxV2 chose move in {(DateTime.Now - startTime).TotalMilliseconds} ms");
            return bestMoves[randomIndex];
        }

        private int MiniMax(int depth, int alpha = Int32.MinValue, int beta = Int32.MaxValue) {
            if (depth == MaxDepth) {
                return BoardEvaluation(_board);
            }

            var legalMoves = GetLegalMoves();
            if (legalMoves.Count == 0) {
                // TODO: distinguish checkmate vs stalemate using board state (e.g., inCheck)
                return -10_000; // Losing (no legal moves) from side-to-move perspective
            }

            int bestScore = Int32.MinValue;
            foreach (var mv in legalMoves) {
                _board.CommitMove(mv);
                int score = -MiniMax(depth + 1, -beta, -alpha);
                _board.UndoMove();

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

            return score * (_board.IsWhitesTurn ? 1 : -1);
        }
    }
}