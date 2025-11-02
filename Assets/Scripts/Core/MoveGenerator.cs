using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Utils;


namespace Core {
    internal struct Direction {
        public readonly int dFile;
        public readonly int dRank;

        public Direction(int dFile, int dRank) {
            this.dFile = dFile;
            this.dRank = dRank;
        }
    }

    public class MoveGenerator {
        private Board _board;

        private static readonly Direction[] Diagonals = {
            new(1, 1), new(1, -1), new(-1, 1), new(-1, -1)
        };

        private static readonly Direction[] Cardinals = {
            new(1, 0), new(-1, 0), new(0, 1), new(0, -1)
        };

        public MoveGenerator(Board board) {
            _board = board;
        }

        public List<Move> ValidMovesForSquare(Coord square) {
            var piece = _board.GetPiece(square);
            if (piece == Piece.None) return new List<Move>();

            if (_board.IsWhitesTurn ^ Piece.IsColor(piece, Piece.White)) return new List<Move>();

            return Piece.Type(piece) switch {
                Piece.Pawn => GeneratePawnMoves(square),
                Piece.Knight => GenerateKnightMoves(square),
                Piece.Queen => GenerateQueenMoves(square),
                Piece.Rook => GenerateRookMoves(square),
                Piece.Bishop => GenerateBishopMoves(square),
                Piece.King => GenerateKingMoves(square),
                _ => new List<Move>()
            };
        }

        private List<Move> GeneratePawnMoves(Coord square) {
            var piece = _board.GetPiece(square);
            if (Piece.Type(piece) != Piece.Pawn) throw new Exception("Not a pawn piece");

            var moves = new List<Move>();
            var forward = Piece.IsColor(piece, Piece.White) ? 1 : -1;

            if (IsPinned(square, out var pinnedBy))
                // TODO: Check if we can capture
                return moves;


            var nextRank = square.rank + forward;

            if (InBounds(nextRank)) {
                if (_board.GetPiece(square.file, nextRank) == Piece.None) {
                    moves.Add(new Move(square, new Coord(square.file, nextRank)));
                    if (BoardUtils.IsPawnStartRank(square.rank, piece) &&
                        _board.GetPiece(square.file, nextRank + forward) == Piece.None)
                        moves.Add(new Move(square, new Coord(square.file, nextRank + forward)));
                }

                // Check left
                if (InBounds(square.file - 1) &&
                    Piece.IsOppositeColor(piece, _board.GetPiece(square.file - 1, nextRank)))
                    moves.Add(new Move(square, new Coord(square.file - 1, nextRank), true));

                // Check right
                if (InBounds(square.file + 1) &&
                    Piece.IsOppositeColor(piece, _board.GetPiece(square.file + 1, nextRank)))
                    moves.Add(new Move(square, new Coord(square.file + 1, nextRank), true));
                // Todo: Check if next rank is promotion
            }

            return moves;
        }

        private List<Move> GenerateKnightMoves(Coord square) {
            var piece = _board.GetPiece(square);
            if (Piece.Type(piece) != Piece.Knight) throw new Exception("Not a knight piece");

            var moves = new List<Move>();

            if (IsPinned(square, out var pinnedBy))
                // TODO: Check if we can capture
                return moves;

            var targetSquares = new List<Coord> {
                new(square.file + 2, square.rank - 1),
                new(square.file + 2, square.rank + 1),
                new(square.file - 2, square.rank - 1),
                new(square.file - 2, square.rank + 1),

                new(square.file + 1, square.rank - 2),
                new(square.file + 1, square.rank + 2),
                new(square.file - 1, square.rank - 2),
                new(square.file - 1, square.rank + 2)
            };

            foreach (var targetSquare in targetSquares.Where(InBounds)) {
                var targetSquarePiece = _board.GetPiece(targetSquare);
                if (targetSquarePiece == Piece.None)
                    moves.Add(new Move(square, targetSquare));
                else if (Piece.IsOppositeColor(piece, targetSquarePiece))
                    moves.Add(new Move(square, targetSquare, true));
            }

            return moves;
        }

        private List<Move> GenerateBishopMoves(Coord square) {
            var piece = _board.GetPiece(square);
            if (Piece.Type(piece) != Piece.Bishop) throw new Exception("Not a bishop piece");

            if (IsPinned(square, out var pinnedBy))
                // TODO: Check if we can capture
                return new List<Move>();

            return Diagonals.SelectMany(d => GetMovesInDirection(square, d)).ToList();
        }

        private List<Move> GenerateRookMoves(Coord square) {
            var piece = _board.GetPiece(square);
            if (Piece.Type(piece) != Piece.Rook) throw new Exception("Not a rook piece");

            if (IsPinned(square, out var pinnedBy))
                // TODO: Check if we can capture
                return new List<Move>();

            return Cardinals.SelectMany(d => GetMovesInDirection(square, d)).ToList();
        }

        private List<Move> GenerateQueenMoves(Coord square) {
            var piece = _board.GetPiece(square);
            if (Piece.Type(piece) != Piece.Queen) throw new Exception("Not a queen piece");

            if (IsPinned(square, out var pinnedBy))
                // TODO: Check if we can capture
                return new List<Move>();

            return Cardinals.Concat(Diagonals).SelectMany(d => GetMovesInDirection(square, d)).ToList();
        }

        private List<Move> GenerateKingMoves(Coord square) {
            var piece = _board.GetPiece(square);
            if (Piece.Type(piece) != Piece.King) throw new Exception("Not a king piece");

            var moves = new List<Move>();

            var targetSquares = new List<Coord> {
                new(square.file + 1, square.rank + 1),
                new(square.file + 1, square.rank),
                new(square.file + 1, square.rank - 1),

                new(square.file, square.rank + 1),
                new(square.file, square.rank - 1),

                new(square.file - 1, square.rank + 1),
                new(square.file - 1, square.rank),
                new(square.file - 1, square.rank - 1)
            };

            foreach (var targetSquare in targetSquares.Where(InBounds)) {
                var targetSquarePiece = _board.GetPiece(targetSquare);
                // TODO: Check if the target square is being attacked

                if (targetSquarePiece == Piece.None)
                    moves.Add(new Move(square, targetSquare));
                else if (Piece.IsOppositeColor(piece, targetSquarePiece))
                    moves.Add(new Move(square, targetSquare, true));
            }

            return moves;
        }

        private bool IsPinned(Coord square, out Coord pinnedBy) {
            pinnedBy = square;
            return false;
        }

        private bool InBounds(Coord coord) {
            return InBounds(coord.file) && InBounds(coord.rank);
        }

        private bool InBounds(int rankOrIndex) {
            return rankOrIndex is >= 0 and < 8;
        }

        private IEnumerable<Move> GetMovesInDirection(Coord start, Direction dir) {
            var piece = _board.GetPiece(start);
            for (var k = 1;; k++) {
                var nextSquare = new Coord(start.file + dir.dFile * k, start.rank + dir.dRank * k);
                if (!InBounds(nextSquare)) yield break;

                var targetSquarePiece = _board.GetPiece(nextSquare);
                if (Piece.Type(targetSquarePiece) == Piece.None) {
                    yield return new Move(start, nextSquare);
                }
                else {
                    if (Piece.IsOppositeColor(piece, targetSquarePiece)) yield return new Move(start, nextSquare, true);

                    yield break;
                }
            }
        }
    }
}