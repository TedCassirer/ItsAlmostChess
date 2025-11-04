using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
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
        private readonly Board _board;
        private readonly int[,] _attackedSquares = new int[8, 8];
        private readonly Coord _friendlyKing;
        private readonly List<Coord> _checkingPieces = new();
        private readonly HashSet<Coord> _squaresStoppingCheck = new();
        private bool InCheck => _checkingPieces.Count > 0;
        private bool InDoubleCheck => _checkingPieces.Count >= 2;

        private static readonly Direction[] Diagonals = {
            new(1, 1), new(1, -1), new(-1, 1), new(-1, -1)
        };

        private static readonly Direction[] Cardinals = {
            new(1, 0), new(-1, 0), new(0, 1), new(0, -1)
        };

        public MoveGenerator(Board board) {
            _board = board;
            _friendlyKing = _board.GetFriendlyKing();
            CalculateAttackedData();
        }

        public List<Move> ValidMovesForSquare(Coord square) {
            var piece = _board.GetPiece(square);
            if (piece == Piece.None || _board.IsWhitesTurn ^ Piece.IsColor(piece, Piece.White)) return Empty<Move>.list;

            IEnumerable<Coord> targetSquares;
            if (InDoubleCheck) {
                // Double check, we must move the king.
                targetSquares = Piece.Type(piece) == Piece.King ? GenerateKingMoves(square) : Empty<Coord>.list;
            }
            else if (InCheck) {
                // Only keep moves that stops check
                if (Piece.Type(piece) == Piece.King) {
                    targetSquares = GenerateKingMoves(square);
                }
                else {
                    targetSquares = (Piece.Type(piece) switch {
                        Piece.Pawn => GeneratePawnAttacks(square)
                            .Where(ts => _board.IsPieceColor(ts, _board.OpponentColor))
                            .Concat(GeneratePawnMoves(square)),
                        Piece.Knight => GenerateKnightMoves(square),
                        Piece.Queen => GenerateQueenMoves(square),
                        Piece.Rook => GenerateRookMoves(square),
                        Piece.Bishop => GenerateBishopMoves(square),
                        _ => Empty<Coord>.list
                    }).Where(sq => _squaresStoppingCheck.Contains(sq));
                }
            }
            else {
                targetSquares = Piece.Type(piece) switch {
                    Piece.Pawn => GeneratePawnAttacks(square)
                        .Where(ts => _board.IsPieceColor(ts, _board.OpponentColor))
                        .Concat(GeneratePawnMoves(square)),
                    Piece.Knight => GenerateKnightMoves(square),
                    Piece.Queen => GenerateQueenMoves(square),
                    Piece.Rook => GenerateRookMoves(square),
                    Piece.Bishop => GenerateBishopMoves(square),
                    Piece.King => GenerateKingMoves(square),
                    _ => Empty<Coord>.list
                };
            }

            return targetSquares
                .Where(ts => {
                    var tsPiece = _board.GetPiece(ts);
                    return Piece.Type(tsPiece) == Piece.None || Piece.IsColor(tsPiece, _board.OpponentColor);
                })
                .Select(ts => CreateMove(square, ts)).ToList();
        }

        private Move CreateMove(Coord from, Coord to) {
            // TODO: Check if promotion
            return new Move(from, to, _board.GetPiece(to) != Piece.None);
        }

        private IEnumerable<Coord> GeneratePawnMoves(Coord square) {
            var piece = _board.GetPiece(square);
            if (Piece.Type(piece) != Piece.Pawn) throw new Exception("Not a pawn piece");

            if (IsPinned(square, out Coord pinnedBy))
                // TODO: Check if we can capture
                yield break;

            var forward = Piece.IsColor(piece, Piece.White) ? 1 : -1;
            var nextRank = square.rank + forward;
            if (InBounds(nextRank)) {
                if (_board.GetPiece(square.file, nextRank) != Piece.None) yield break;
                yield return new Coord(square.file, nextRank);
                if (BoardUtils.IsPawnStartRank(square.rank, piece) &&
                    _board.GetPiece(square.file, nextRank + forward) == Piece.None)
                    yield return new Coord(square.file, nextRank + forward);
            }
        }

        private IEnumerable<Coord> GeneratePawnAttacks(Coord square) {
            var piece = _board.GetPiece(square);
            if (Piece.Type(piece) != Piece.Pawn) throw new Exception("Not a pawn piece");

            var forward = Piece.IsColor(piece, Piece.White) ? 1 : -1;

            var nextRank = square.rank + forward;
            var attackedSquares = new List<Coord> {
                new(square.file - 1, nextRank),
                new(square.file + 1, nextRank)
            };
            return attackedSquares.Where(InBounds);
        }

        private List<Coord> GenerateKnightMoves(Coord square) {
            var piece = _board.GetPiece(square);
            if (Piece.Type(piece) != Piece.Knight) throw new Exception("Not a knight piece");

            if (IsPinned(square, out var pinnedBy))
                // TODO: Check if we can capture
                return new List<Coord>();

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
            return targetSquares.Where(InBounds).Where(ts => {
                var targetSquarePiece = _board.GetPiece(ts);
                return targetSquarePiece == Piece.None || Piece.IsOppositeColor(piece, targetSquarePiece);
            }).ToList();
        }

        private List<Coord> GenerateBishopMoves(Coord square) {
            var piece = _board.GetPiece(square);
            if (Piece.Type(piece) != Piece.Bishop) throw new Exception("Not a bishop piece");

            if (IsPinned(square, out var pinnedBy))
                // TODO: Check if we can capture
                return new List<Coord>();

            return Diagonals.SelectMany(d => GetMovesInDirection(square, d)).ToList();
        }

        private List<Coord> GenerateRookMoves(Coord square) {
            var piece = _board.GetPiece(square);
            if (Piece.Type(piece) != Piece.Rook) throw new Exception("Not a rook piece");

            if (IsPinned(square, out Coord pinnedBy))
                // TODO: Check if we can capture
                return new List<Coord>();

            return Cardinals.SelectMany(d => GetMovesInDirection(square, d)).ToList();
        }

        private List<Coord> GenerateQueenMoves(Coord square) {
            var piece = _board.GetPiece(square);
            if (Piece.Type(piece) != Piece.Queen) throw new Exception("Not a queen piece");

            if (IsPinned(square, out var pinnedBy))
                // TODO: Check if we can capture
                return new List<Coord>();

            return Cardinals.Concat(Diagonals).SelectMany(d => GetMovesInDirection(square, d)).ToList();
        }

        private List<Coord> GenerateKingMoves(Coord square) {
            var piece = _board.GetPiece(square);
            if (Piece.Type(piece) != Piece.King) throw new Exception("Not a king piece");

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


            return targetSquares.Where(InBounds)
                .Where(ts => {
                    var targetSquarePiece = _board.GetPiece(ts);
                    return targetSquarePiece == Piece.None || Piece.IsOppositeColor(piece, targetSquarePiece);
                })
                .Where(sq => _attackedSquares[sq.file, sq.rank] == 0)
                .ToList();
        }

        private void CalculateAttackedData() {
            for (var file = 0; file < 8; file++)
            for (var rank = 0; rank < 8; rank++) {
                var piece = _board.GetPiece(file, rank);
                if (!Piece.IsColor(piece, _board.OpponentColor)) continue;
                var attackingPiece = new Coord(file, rank);
                foreach (Coord c in GetAttackedSquares(attackingPiece)) {
                    _attackedSquares[c.file, c.rank]++;
                    if (c.Equals(_friendlyKing)) {
                        _checkingPieces.Add(attackingPiece);
                    }
                }
            }

            if (InCheck && !InDoubleCheck) {
                Coord checkingSquare = _checkingPieces[0];
                var checkingPiece = _board.GetPiece(checkingSquare);
                if (Piece.Type(checkingPiece) == Piece.Knight) {
                    _squaresStoppingCheck.Add(checkingSquare);
                    return;
                }

                var dRank = _friendlyKing.rank - checkingSquare.rank;
                var dFile = _friendlyKing.file - checkingSquare.file;
                var dir = new Direction(Math.Clamp(dFile, -1, 1), Math.Clamp(dRank, -1, 1));
                foreach (Coord sq in GetMovesInDirection(checkingSquare, dir)
                             .TakeWhile(sq => !sq.Equals(_friendlyKing))) {
                    _squaresStoppingCheck.Add(sq);
                }
            }
        }

        public IEnumerable<Coord> GetAttackedSquares(Coord square) {
            var piece = _board.GetPiece(square);
            return Piece.Type(piece) switch {
                Piece.Pawn => GeneratePawnAttacks(square),
                Piece.Knight => GenerateKnightMoves(square),
                Piece.Queen => GenerateQueenMoves(square),
                Piece.Rook => GenerateRookMoves(square),
                Piece.Bishop => GenerateBishopMoves(square),
                Piece.King => GenerateKingMoves(square),
                _ => new List<Coord>()
            };
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

        private IEnumerable<Coord> GetMovesInDirection(Coord start, Direction dir) {
            var piece = _board.GetPiece(start);
            for (var k = 1;; k++) {
                var nextSquare = new Coord(start.file + dir.dFile * k, start.rank + dir.dRank * k);
                if (!InBounds(nextSquare)) yield break;

                var targetSquarePiece = _board.GetPiece(nextSquare);
                if (Piece.Type(targetSquarePiece) == Piece.None) {
                    yield return nextSquare;
                }
                else {
                    if (Piece.IsOppositeColor(piece, targetSquarePiece)) yield return nextSquare;
                    yield break;
                }
            }
        }
    }
}