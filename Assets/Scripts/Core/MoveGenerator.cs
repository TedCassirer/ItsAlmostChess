using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Utils;

namespace Core {
    public struct Direction {
        // CHANGED: was internal
        private readonly int _dFile;
        private readonly int _dRank;

        public bool IsDiagonal => Math.Abs(_dFile) == Math.Abs(_dRank) && _dFile != 0;
        public bool IsCardinal => _dFile == 0 ^ _dRank == 0;

        public Direction(int dFile, int dRank) {
            _dFile = dFile;
            _dRank = dRank;
        }

        public Coord Move(Coord fromCoord, int steps) {
            return Coord.Create(fromCoord.file + _dFile * steps, fromCoord.rank + _dRank * steps);
        }

        public IEnumerable<Coord> MoveUntil(Coord start, [CanBeNull] Predicate<Coord> stopCondition = null,
            bool includeStartSquare = false,
            bool includeStopSquare = false) {
            for (var k = includeStartSquare ? 0 : 1;; k++) {
                Coord c = Move(start, k);
                if (!c.InBounds) {
                    yield break;
                }

                if (stopCondition != null && stopCondition(c)) {
                    if (includeStopSquare) {
                        yield return c;
                    }

                    yield break;
                }

                yield return c;
            }
        }

        public Direction Reverse() {
            return new Direction(-_dFile, -_dRank);
        }
    }

    public class MoveGenerator {
        private readonly Board _board;
        private readonly int[,] _attackedSquares = new int[8, 8];
        private Coord _friendlyKing;
        private readonly List<Coord> _checkingPieces = new();
        private readonly HashSet<Coord> _squaresBlockingCheck = new();
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
            Refresh();
        }
        
        private void Refresh() {
            Array.Clear(_attackedSquares, 0, _attackedSquares.Length);
            _checkingPieces.Clear();
            _squaresBlockingCheck.Clear();
            _friendlyKing = _board.GetFriendlyKing();
            CalculateAttackedData();
        }

        public List<Move> ValidMoves() {
            var moves = new List<Move>();
            for (var file = 0; file < 8; file++)
            for (var rank = 0; rank < 8; rank++) {
                var square = Coord.Create(file, rank);
                moves.AddRange(ValidMovesForSquare(square));
            }

            return moves;
        }

        public List<Move> ValidMovesForSquare(Coord square) {
            var piece = _board.GetPiece(square);
            if (piece == Piece.None || _board.IsWhitesTurn ^ Piece.IsColor(piece, Piece.White)) return new List<Move>();

            IEnumerable<Coord> targetSquares;
            if (InDoubleCheck) {
                // Double check, we must move the king.
                targetSquares = Piece.Type(piece) == Piece.King ? GenerateKingMoves(square) : new List<Coord>();
            } else {
                // Only keep moves that stops check
                targetSquares = Piece.Type(piece) switch {
                    Piece.Pawn => GeneratePawnAttacks(square)
                        .Where(ts => _board.IsPieceColor(ts, _board.OpponentColor))
                        .Concat(GeneratePawnMoves(square)),
                    Piece.Knight => GenerateKnightMoves(square),
                    Piece.Queen => GenerateQueenMoves(square),
                    Piece.Rook => GenerateRookMoves(square),
                    Piece.Bishop => GenerateBishopMoves(square),
                    Piece.King => GenerateKingMoves(square),
                    _ => new List<Coord>()
                };
                if (InCheck && !square.Equals(_friendlyKing)) {
                    targetSquares = targetSquares.Where(ts => _squaresBlockingCheck.Contains(ts));
                }
            }

            return targetSquares
                .Where(PinnedRestriction(square).Invoke)
                .Where(ts => {
                    var tsPiece = _board.GetPiece(ts);
                    return tsPiece == Piece.None || Piece.IsColor(tsPiece, _board.OpponentColor);
                })
                .Select(ts => CreateMove(square, ts))
                .SelectMany(ts => {
                    if (Piece.Type(piece) == Piece.Pawn &&
                        BoardUtils.IsPromotionRank(ts.To.rank, piece)) {
                        return new List<Move> {
                            new(ts.From, ts.To, capturedPiece: ts.CapturedPiece,
                                promotionPiece: Piece.Queen | _board.ColorToMove),
                            new(ts.From, ts.To, capturedPiece: ts.CapturedPiece,
                                promotionPiece: Piece.Rook | _board.ColorToMove),
                            new(ts.From, ts.To, capturedPiece: ts.CapturedPiece,
                                promotionPiece: Piece.Bishop | _board.ColorToMove),
                            new(ts.From, ts.To, capturedPiece: ts.CapturedPiece,
                                promotionPiece: Piece.Knight | _board.ColorToMove)
                        };
                    }

                    return new List<Move> { ts };
                })
                .ToList();
        }


        private Move CreateMove(Coord from, Coord to) {
            return new Move(from, to, _board.GetPiece(to));
        }

        private IEnumerable<Coord> GeneratePawnMoves(Coord square) {
            var piece = _board.GetPiece(square);
            if (Piece.Type(piece) != Piece.Pawn) throw new Exception("Not a pawn piece");

            var forward = Piece.IsColor(piece, Piece.White) ? 1 : -1;
            var nextRank = square.rank + forward;
            if (_board.GetPiece(square.file, nextRank) != Piece.None) yield break;
            yield return Coord.Create(square.file, nextRank);
            if (BoardUtils.IsPawnStartRank(square.rank, piece) &&
                _board.GetPiece(square.file, nextRank + forward) == Piece.None)
                yield return Coord.Create(square.file, nextRank + forward);
        }

        private IEnumerable<Coord> GeneratePawnAttacks(Coord square) {
            var piece = _board.GetPiece(square);
            if (Piece.Type(piece) != Piece.Pawn) throw new Exception("Not a pawn piece");

            var forward = Piece.IsColor(piece, Piece.White) ? 1 : -1;

            var nextRank = square.rank + forward;
            var attackedSquares = new List<Coord> {
                Coord.Create(square.file - 1, nextRank),
                Coord.Create(square.file + 1, nextRank)
            };
            return attackedSquares.Where(sq => sq.InBounds);
        }

        private List<Coord> GenerateKnightMoves(Coord square) {
            var piece = _board.GetPiece(square);
            if (Piece.Type(piece) != Piece.Knight) throw new Exception("Not a knight piece");

            var targetSquares = new List<Coord> {
                Coord.Create(square.file + 2, square.rank - 1),
                Coord.Create(square.file + 2, square.rank + 1),
                Coord.Create(square.file - 2, square.rank - 1),
                Coord.Create(square.file - 2, square.rank + 1),

                Coord.Create(square.file + 1, square.rank - 2),
                Coord.Create(square.file + 1, square.rank + 2),
                Coord.Create(square.file - 1, square.rank - 2),
                Coord.Create(square.file - 1, square.rank + 2)
            };
            return targetSquares.Where(sq => sq.InBounds).Where(ts => {
                var targetSquarePiece = _board.GetPiece(ts);
                return targetSquarePiece == Piece.None || Piece.IsOppositeColor(piece, targetSquarePiece);
            }).ToList();
        }

        private List<Coord> GenerateBishopMoves(Coord square) {
            var piece = _board.GetPiece(square);
            if (Piece.Type(piece) != Piece.Bishop) throw new Exception("Not a bishop piece");

            return Diagonals.SelectMany(d =>
                    d.MoveUntil(square, sq => _board.GetPiece(sq) != Piece.None, includeStopSquare: true))
                .ToList();
        }

        private List<Coord> GenerateRookMoves(Coord square) {
            var piece = _board.GetPiece(square);
            if (Piece.Type(piece) != Piece.Rook) throw new Exception("Not a rook piece");

            return Cardinals.SelectMany(d =>
                    d.MoveUntil(square, sq => _board.GetPiece(sq) != Piece.None, includeStopSquare: true))
                .ToList();
        }

        private List<Coord> GenerateQueenMoves(Coord square) {
            var piece = _board.GetPiece(square);
            if (Piece.Type(piece) != Piece.Queen) throw new Exception("Not a queen piece");

            return Cardinals.Concat(Diagonals).SelectMany(d =>
                    d.MoveUntil(square, sq => _board.GetPiece(sq) != Piece.None, includeStopSquare: true))
                .ToList();
        }

        private List<Coord> GenerateKingMoves(Coord square) {
            var piece = _board.GetPiece(square);
            if (Piece.Type(piece) != Piece.King) throw new Exception("Not a king piece");
            var targetSquares = new List<Coord> {
                Coord.Create(square.file + 1, square.rank + 1),
                Coord.Create(square.file + 1, square.rank),
                Coord.Create(square.file + 1, square.rank - 1),

                Coord.Create(square.file, square.rank + 1),
                Coord.Create(square.file, square.rank - 1),

                Coord.Create(square.file - 1, square.rank + 1),
                Coord.Create(square.file - 1, square.rank),
                Coord.Create(square.file - 1, square.rank - 1)
            };

            return targetSquares.Where(ts => ts.InBounds)
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
                var attackingPiece = Coord.Create(file, rank);
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
                    _squaresBlockingCheck.Add(checkingSquare);
                    return;
                }

                var dRank = _friendlyKing.rank - checkingSquare.rank;
                var dFile = _friendlyKing.file - checkingSquare.file;
                var dir = new Direction(Math.Clamp(dFile, -1, 1), Math.Clamp(dRank, -1, 1));
                foreach (Coord sq in dir.MoveUntil(checkingSquare, includeStartSquare: true)
                             .TakeWhile(sq => !sq.Equals(_friendlyKing))) {
                    _squaresBlockingCheck.Add(sq);
                }
            }

            _checkingPieces.ForEach(checkingSquare => {
                var checkingPiece = _board.GetPiece(checkingSquare);
                if (Piece.Type(checkingPiece) is Piece.Queen or Piece.Bishop or Piece.Rook) {
                    // Mark the spaces behind the king as attacked to prevent king from moving there
                    var dRank = _friendlyKing.rank - checkingSquare.rank;
                    var dFile = _friendlyKing.file - checkingSquare.file;
                    var dir = new Direction(Math.Clamp(dFile, -1, 1), Math.Clamp(dRank, -1, 1));
                    foreach (var sq in dir
                                 .MoveUntil(_friendlyKing, sq => !_board.IsEmpty(sq), includeStopSquare: true)) {
                        _attackedSquares[sq.file, sq.rank]++;
                    }
                }
            });
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

        private Predicate<Coord> PinnedRestriction(Coord square) {
            if (square.Equals(_friendlyKing)) {
                return _ => true;
            }

            var dRank = _friendlyKing.rank - square.rank;
            var dFile = _friendlyKing.file - square.file;
            var dir = new Direction(Math.Clamp(dFile, -1, 1), Math.Clamp(dRank, -1, 1)); // Direction towards the king
            if (dir is { IsDiagonal: false, IsCardinal: false }) {
                return _ => true;
            }

            // Towards the king
            List<Coord> pathToKing = dir.MoveUntil(square, _friendlyKing.Equals).ToList();
            if (pathToKing.Any(c => _board.GetPiece(c) != Piece.None)) {
                // Another piece is blocking the path to the king
                return _ => true;
            }

            // Away from the king
            List<Coord> pathAwayFromKing = dir.Reverse()
                .MoveUntil(square, sq => _board.GetPiece(sq) != Piece.None, includeStopSquare: true).ToList();
            if (pathAwayFromKing.Count == 0) {
                return _ => true;
            }

            var pieceInDir = _board.GetPiece(pathAwayFromKing.Last());
            if (Piece.IsColor(pieceInDir, _board.ColorToMove)) {
                // Friendly piece blocking the path
                return _ => true;
            }

            var tsPieceType = Piece.Type(pieceInDir);
            if (tsPieceType == Piece.Queen || (tsPieceType == Piece.Bishop && dir.IsDiagonal) ||
                (tsPieceType == Piece.Rook && dir.IsCardinal)) {
                return sq => pathToKing.Contains(sq) || pathAwayFromKing.Contains(sq);
            }

            return _ => true;
        }

        public int CountMoves(int depth) {
            if (depth == 0) return 1;
            Refresh();
            var total = 0;
            foreach (var move in ValidMoves()) {
                _board.MakeMove(move);
                total += CountMoves(depth - 1);
                _board.UndoMove(move);
            }

            return total;
        }

        public int CountMovesParallel(int depth, CancellationToken cancellationToken) {
            if (depth == 0) return 1;
            return ValidMoves().AsParallel().WithDegreeOfParallelism(Environment.ProcessorCount)
                .WithCancellation(cancellationToken)
                .Select(move => {
                    var copy = _board.Clone();
                    return copy.MakeMove(move)
                        ? new MoveGenerator(copy).CountMoves(depth - 1)
                        : // sequential below root
                        1;
                }).Sum();
        }

        public int CountMovesWithConcurrentQueue(int depth, CancellationToken ct) {
            if (depth == 0) return 1;
            var queue = new ConcurrentQueue<(Board board, int d)>();
            foreach (var move in ValidMoves()) {
                var copy = _board.Clone();
                if (copy.MakeMove(move))
                    queue.Enqueue((copy, depth - 1));
            }

            int total = 0;
            int workers = Environment.ProcessorCount;

            Parallel.For(0, workers, new ParallelOptions { CancellationToken = ct }, _ => {
                var stack = new Stack<(Board board, int d)>();
                while (!queue.IsEmpty || stack.Count > 0) {
                    ct.ThrowIfCancellationRequested();

                    if (stack.Count == 0 && queue.TryDequeue(out var rootItem)) {
                        stack.Push(rootItem);
                    }

                    if (stack.Count == 0) continue;

                    var (b, d) = stack.Pop();
                    if (d == 0) {
                        Interlocked.Increment(ref total);
                        continue;
                    }

                    var gen = new MoveGenerator(b);
                    foreach (var mv in gen.ValidMoves()) {
                        var child = b.Clone();
                        child.MakeMove(mv);

                        int nd = d - 1;
                        if (nd == 0) {
                            Interlocked.Increment(ref total);
                        }
                        else {
                            // Occasionally re-enqueue to help other threads if local stack is large
                            if (stack.Count > 64) {
                                queue.Enqueue((child, nd));
                            }
                            else {
                                stack.Push((child, nd));
                            }
                        }
                    }
                }
            });

            return total;
        }
    }
}