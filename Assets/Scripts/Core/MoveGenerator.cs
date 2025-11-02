using System;
using System.Collections.Generic;
using NUnit.Framework;
using Utils;

namespace Core {
    public class MoveGenerator {
        private Board _board;

        public MoveGenerator(Board board) {
            _board = board;
        }

        public List<Move> ValidMovesForSquare(Coord square) {
            var piece = _board.GetPiece(square.file, square.rank);
            if (piece == Piece.None) {
                return new List<Move>();
            }

            if (_board.IsWhitesTurn ^ Piece.IsColor(piece, Piece.White)) {
                return new List<Move>();
            }

            return piece switch {
                Piece.Pawn => GeneratePawnMoves(square),
                _ => new List<Move>()
            };
        }

        private List<Move> GeneratePawnMoves(Coord square) {
            var piece = _board.GetPiece(square.file, square.rank);
            if (piece != Piece.Pawn) {
                throw new Exception("Not a pawn piece");
            }

            var moves = new List<Move>();
            var forward = Piece.IsColor(piece, Piece.White) ? 1 : -1;

            if (IsPinned(square, out var pinnedBy)) {
                // TODO: Check if we can capture
                return moves;
            }


            var nextRank = square.rank + forward;
            if (InBounds(nextRank)) {
                if (_board.GetPiece(square.file, nextRank) == Piece.None) {
                    moves.Add(new Move(square, new Coord(square.file, nextRank)));
                    if (BoardUtils.IsPawnStartRank(square.rank, piece) &&
                        _board.GetPiece(square.file, nextRank + forward) == Piece.None) {
                        moves.Add(new Move(square, new Coord(square.file, nextRank + forward)));
                    }
                }

                // Check left
                if (InBounds(square.file - 1) && _board.GetPiece(square.file - 1, nextRank) != Piece.None) {
                    moves.Add(new Move(square, new Coord(square.file - 1, nextRank), true));
                }

                // Check right
                if (InBounds(square.file + 1) && _board.GetPiece(square.file + 1, nextRank) != Piece.None) {
                    moves.Add(new Move(square, new Coord(square.file + 1, nextRank), true));
                }
                // Todo: Check if next rank is promotion
            }

            return moves;
        }

        private bool IsPinned(Coord square, out Coord pinnedBy) {
            pinnedBy = square;
            return false;
        }

        private bool InBounds(int rankOrIndex) {
            return rankOrIndex is >= 0 and < 8;
        }
    }
}