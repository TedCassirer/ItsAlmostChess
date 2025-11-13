using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Core.AI {
    public class MiniMaxV2 : IMoveProvider {
        private readonly int _maxDepth;
        private readonly MoveOrderer _moveOrderer;
        private readonly BoardEvaluator _boardEvaluator;
        private int _nodesSearched;
        private const int NegInf = -1000000000;
        private const int PosInf = 1000000000;

        public MiniMaxV2(int maxDepth, BoardEvaluator boardEvaluator, MoveOrderer moveOrderer) {
            _maxDepth = maxDepth;
            _boardEvaluator = boardEvaluator;
            _moveOrderer = moveOrderer;
        }

        public Move? GetNextMove(Board board) {
            DateTime startTime = DateTime.Now;

            var moveGenerator = new MoveGenerator(board);
            List<Move> legalMoves = moveGenerator.LegalMoves();
            if (legalMoves.Count == 0) return null;
            List<Move> bestMoves = new();
            int bestScore = NegInf;
            _nodesSearched = 0;

            legalMoves = legalMoves.ToList();

            foreach (Move move in legalMoves) {
                board.CommitMove(move);
                // Depth starts at 1 after making a move
                int score = -Search(board, depth: 1, alpha: NegInf, beta: PosInf);
                board.UndoMove();

                if (score > bestScore) {
                    bestScore = score;
                    bestMoves.Clear();
                    bestMoves.Add(move);
                }
                else if (score == bestScore) {
                    bestMoves.Add(move);
                }
            }

            var chosenMove = bestMoves[0]; // deterministic when tie
            DateTime endTime = DateTime.Now;
            TimeSpan duration = endTime - startTime;
            Debug.Log($"MiniMaxV2 selected move in {duration.TotalMilliseconds} ms at depth {_maxDepth}. Nodes searched: {_nodesSearched}. Best score: {bestScore}");
            return chosenMove;
        }

        private int Search(Board board, int depth, int alpha, int beta) {
            _nodesSearched++;
            if (depth >= _maxDepth) {
                return EvaluateForSideToMove(board);
            }

            var movesGenerator = new MoveGenerator(board);
            List<Move> legalMoves = movesGenerator.LegalMoves()
                .OrderByDescending(_moveOrderer.ScoreMove)
                .ToList();

            if (legalMoves.Count == 0) {
                return EvaluateForSideToMove(board);
            }

            int value = NegInf;
            foreach (Move move in legalMoves) {
                board.CommitMove(move);
                int score = -Search(board, depth + 1, -beta, -alpha);
                board.UndoMove();

                if (score > value) value = score;
                if (value > alpha) alpha = value;
                if (alpha >= beta) break; // Alpha-beta cutoff
            }
            return value;
        }

        private int EvaluateForSideToMove(Board board) {
            // Base evaluator returns (white material - black material).
            int eval = _boardEvaluator.Evaluate(board);
            return board.IsWhitesTurn ? eval : -eval; // orient to side to move
        }
    }
}
