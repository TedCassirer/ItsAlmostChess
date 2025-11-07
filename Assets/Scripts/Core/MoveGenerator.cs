using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Unity.VisualScripting;
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
            return Coord.Create(fromCoord.File + _dFile * steps, fromCoord.Rank + _dRank * steps);
        }

        public IEnumerable<Coord> MoveToEnd(Coord start) {
            for (var k = 1;; k++) {
                Coord c = Move(start, k);
                if (!c.InBounds) {
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

        public void Refresh() {
            Array.Clear(_attackedSquares, 0, _attackedSquares.Length);
            _checkingPieces.Clear();
            _squaresBlockingCheck.Clear();
            _friendlyKing = _board.GetFriendlyKing();
            CalculateAttackedData();
        }

        public List<Move> LegalMoves() {
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
            }
            else {
                // Only keep moves that stops check
                targetSquares = Piece.Type(piece) switch {
                    Piece.Pawn => GeneratePawnAttacks(square)
                        .Concat(GeneratePawnMoves(square)),
                    Piece.Knight => GenerateKnightMoves(square),
                    Piece.Queen => GenerateQueenMoves(square),
                    Piece.Rook => GenerateRookMoves(square),
                    Piece.Bishop => GenerateBishopMoves(square),
                    Piece.King => GenerateKingMoves(square),
                    _ => new List<Coord>()
                };
                if (InCheck && !square.Equals(_friendlyKing)) {
                    targetSquares = targetSquares.Where(ts =>
                        _squaresBlockingCheck.Contains(ts) ||
                        (ts.Equals(_board.EnPassantTarget) && Piece.Type(piece) == Piece.Pawn));
                }
            }

            return targetSquares
                .Where(PinnedRestriction(square).Invoke)
                .Select(ts => CreateMove(square, ts))
                .SelectMany(GeneratePromotionMoves)
                .ToList();
        }

        private IEnumerable<Move> GeneratePromotionMoves(Move baseMove) {
            if (Piece.Type(_board.GetPiece(baseMove.From)) != Piece.Pawn ||
                !BoardUtils.IsPromotionRank(baseMove.To.Rank, _board.GetPiece(baseMove.From))) {
                yield return baseMove;
                yield break;
            }

            foreach (Move m in Move.CreatePromotionMove(baseMove, _board.ColorToMove)) {
                yield return m;
            }
        }


        private Move CreateMove(Coord from, Coord to) {
            if (to.Equals(_board.EnPassantTarget) &&
                Piece.Type(_board.GetPiece(from)) == Piece.Pawn) {
                return Move.EnPassantMove(from, to);
            }

            if (from.Equals(_friendlyKing) && Math.Abs(to.File - from.File) == 2) {
                // Castling move
                return Move.Castle(from, to);
            }

            return new Move(from, to, capturedPiece: _board.GetPiece(to));
        }

        private IEnumerable<Coord> GeneratePawnMoves(Coord square) {
            var piece = _board.GetPiece(square);
            if (Piece.Type(piece) != Piece.Pawn) throw new Exception("Not a pawn piece");

            var forward = Piece.IsColor(piece, Piece.White) ? 1 : -1;
            var nextRank = square.Rank + forward;
            if (_board.GetPiece(square.File, nextRank) != Piece.None) yield break;
            yield return Coord.Create(square.File, nextRank);
            if (BoardUtils.IsPawnStartRank(square.Rank, piece) &&
                _board.GetPiece(square.File, nextRank + forward) == Piece.None)
                yield return Coord.Create(square.File, nextRank + forward);
        }

        private IEnumerable<Coord> GeneratePawnAttacks(Coord square) {
            var piece = _board.GetPiece(square);
            if (Piece.Type(piece) != Piece.Pawn) throw new Exception("Not a pawn piece");

            var forward = Piece.IsColor(piece, Piece.White) ? 1 : -1;

            if (square.File > 0) {
                var leftAttackSquare = Coord.Create(square.File - 1, square.Rank + forward);
                if (Piece.Color(_board.GetPiece(leftAttackSquare)) == _board.OpponentColor) {
                    yield return leftAttackSquare;
                }
                else if (_board.EnPassantTarget.Equals(leftAttackSquare)) {
                    // TODO: Check if this reveals a check
                    yield return leftAttackSquare;
                }
            }

            if (square.File < 7) {
                var rightAttackSquare = Coord.Create(square.File + 1, square.Rank + forward);
                if (Piece.Color(_board.GetPiece(rightAttackSquare)) == _board.OpponentColor) {
                    yield return rightAttackSquare;
                }
                else if (_board.EnPassantTarget.Equals(rightAttackSquare)) {
                    // TODO: Check if this reveals a check
                    yield return rightAttackSquare;
                }
            }
        }

        private IEnumerable<Coord> GetPawnThreats(Coord square) {
            var forward = _board.IsWhitesTurn ? -1 : 1;

            if (square.File > 0) {
                yield return Coord.Create(square.File - 1, square.Rank + forward);
            }

            if (square.File < 7) {
                yield return Coord.Create(square.File + 1, square.Rank + forward);
            }
        }

        private IEnumerable<Coord> GenerateKnightMoves(Coord square) {
            var targetSquares = new List<Coord> {
                Coord.Create(square.File + 2, square.Rank - 1),
                Coord.Create(square.File + 2, square.Rank + 1),
                Coord.Create(square.File - 2, square.Rank - 1),
                Coord.Create(square.File - 2, square.Rank + 1),

                Coord.Create(square.File + 1, square.Rank - 2),
                Coord.Create(square.File + 1, square.Rank + 2),
                Coord.Create(square.File - 1, square.Rank - 2),
                Coord.Create(square.File - 1, square.Rank + 2)
            };
            return targetSquares
                .Where(sq => sq.InBounds)
                .Where(ts => _board.IsEmpty(ts) || Piece.IsColor(_board.GetPiece(ts), _board.OpponentColor))
                .Where(sq => !InCheck || _squaresBlockingCheck.Contains(sq));
        }

        private List<Coord> GetKnightThreats(Coord square) {
            var targetSquares = new List<Coord> {
                Coord.Create(square.File + 2, square.Rank - 1),
                Coord.Create(square.File + 2, square.Rank + 1),
                Coord.Create(square.File - 2, square.Rank - 1),
                Coord.Create(square.File - 2, square.Rank + 1),

                Coord.Create(square.File + 1, square.Rank - 2),
                Coord.Create(square.File + 1, square.Rank + 2),
                Coord.Create(square.File - 1, square.Rank - 2),
                Coord.Create(square.File - 1, square.Rank + 2)
            };
            return targetSquares.Where(sq => sq.InBounds).ToList();
        }

        private IEnumerable<Coord> GenerateBishopMoves(Coord square) {
            return Diagonals.SelectMany(d => MovesIncludingCapturesInDirection(square, d))
                .Where(sq => !InCheck || _squaresBlockingCheck.Contains(sq));
        }

        private IEnumerable<Coord> GetBishopThreats(Coord square) {
            return Diagonals.SelectMany(d => MovesIncludingCapturesInDirection(square, d, bothColors: true));
        }

        private IEnumerable<Coord> GenerateRookMoves(Coord square) {
            return Cardinals.SelectMany(d => MovesIncludingCapturesInDirection(square, d))
                .Where(sq => !InCheck || _squaresBlockingCheck.Contains(sq));
        }

        private IEnumerable<Coord> GetRookThreats(Coord square) {
            return Cardinals.SelectMany(d => MovesIncludingCapturesInDirection(square, d, bothColors: true));
        }

        private IEnumerable<Coord> GenerateQueenMoves(Coord square) {
            return Cardinals.Concat(Diagonals)
                .SelectMany(d => MovesIncludingCapturesInDirection(square, d))
                .Where(sq => !InCheck || _squaresBlockingCheck.Contains(sq));
        }

        private IEnumerable<Coord> GetQueenThreats(Coord square) {
            return Cardinals.Concat(Diagonals)
                .SelectMany(d => MovesIncludingCapturesInDirection(square, d, bothColors: true));
        }

        private IEnumerable<Coord> MovesIncludingCapturesInDirection(Coord square, Direction direction,
            bool bothColors = false) {
            foreach (Coord coord in direction.MoveToEnd(square)) {
                var targetPiece = _board.GetPiece(coord);
                if (targetPiece == Piece.None) {
                    yield return coord;
                }
                else {
                    if (bothColors || Piece.IsColor(targetPiece, _board.OpponentColor)) {
                        yield return coord;
                    }

                    yield break;
                }
            }
        }

        private IEnumerable<Coord> GenerateKingMoves(Coord square) {
            var targetSquares = new List<Coord> {
                Coord.Create(square.File + 1, square.Rank + 1),
                Coord.Create(square.File + 1, square.Rank),
                Coord.Create(square.File + 1, square.Rank - 1),

                Coord.Create(square.File, square.Rank + 1),
                Coord.Create(square.File, square.Rank - 1),

                Coord.Create(square.File - 1, square.Rank + 1),
                Coord.Create(square.File - 1, square.Rank),
                Coord.Create(square.File - 1, square.Rank - 1)
            };

            IEnumerable<Coord> moves = targetSquares.Where(ts => ts.InBounds)
                .Where(ts => _board.IsEmpty(ts) || Piece.IsColor(_board.GetPiece(ts), _board.OpponentColor))
                .Where(sq => _attackedSquares[sq.File, sq.Rank] == 0);

            if (_board.CanCastleKingSide) {
                var rightSquare = Coord.Create(square.File + 1, square.Rank);
                var right2Square = Coord.Create(square.File + 2, square.Rank);
                if (_board.IsEmpty(rightSquare) && _board.IsEmpty(right2Square) &&
                    _attackedSquares[rightSquare.File, rightSquare.Rank] == 0 &&
                    _attackedSquares[right2Square.File, right2Square.Rank] == 0) {
                    moves = moves.Append(right2Square);
                }
            }

            if (_board.CanCastleQueenSide) {
                var leftSquare = Coord.Create(square.File - 1, square.Rank);
                var left2Square = Coord.Create(square.File - 2, square.Rank);
                var left3Square = Coord.Create(square.File - 3, square.Rank);
                if (_board.IsEmpty(leftSquare) && _board.IsEmpty(left2Square) && _board.IsEmpty(left3Square) &&
                    _attackedSquares[leftSquare.File, leftSquare.Rank] == 0 &&
                    _attackedSquares[left2Square.File, left2Square.Rank] == 0) {
                    moves = moves.Append(left2Square);
                }
            }

            return moves;
        }

        private IEnumerable<Coord> GetKingThreats(Coord square) {
            var targetSquares = new List<Coord> {
                Coord.Create(square.File + 1, square.Rank + 1),
                Coord.Create(square.File + 1, square.Rank),
                Coord.Create(square.File + 1, square.Rank - 1),

                Coord.Create(square.File, square.Rank + 1),
                Coord.Create(square.File, square.Rank - 1),

                Coord.Create(square.File - 1, square.Rank + 1),
                Coord.Create(square.File - 1, square.Rank),
                Coord.Create(square.File - 1, square.Rank - 1)
            };

            return targetSquares.Where(ts => ts.InBounds);
        }

        private void CalculateAttackedData() {
            for (var file = 0; file < 8; file++)
            for (var rank = 0; rank < 8; rank++) {
                var piece = _board.GetPiece(file, rank);
                if (!Piece.IsColor(piece, _board.OpponentColor)) continue;
                var attackingPiece = Coord.Create(file, rank);
                foreach (Coord c in GetThreats(attackingPiece)) {
                    _attackedSquares[c.File, c.Rank]++;
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

                var dRank = _friendlyKing.Rank - checkingSquare.Rank;
                var dFile = _friendlyKing.File - checkingSquare.File;
                var dir = new Direction(Math.Clamp(dFile, -1, 1), Math.Clamp(dRank, -1, 1));
                _squaresBlockingCheck.Add(checkingSquare);
                foreach (Coord sq in dir.MoveToEnd(checkingSquare).TakeWhile(sq => !sq.Equals(_friendlyKing))) {
                    _squaresBlockingCheck.Add(sq);
                }
            }

            _checkingPieces.ForEach(checkingSquare => {
                var checkingPiece = _board.GetPiece(checkingSquare);
                if (Piece.Type(checkingPiece) is Piece.Queen or Piece.Bishop or Piece.Rook) {
                    // Mark the spaces behind the king as attacked to prevent king from moving there
                    var dRank = _friendlyKing.Rank - checkingSquare.Rank;
                    var dFile = _friendlyKing.File - checkingSquare.File;
                    var dir = new Direction(Math.Clamp(dFile, -1, 1), Math.Clamp(dRank, -1, 1));
                    foreach (var sq in dir
                                 .MoveToEnd(_friendlyKing)
                                 .TakeWhile(_board.IsEmpty)) {
                        _attackedSquares[sq.File, sq.Rank]++;
                    }
                }
            });
        }

        public IEnumerable<Coord> GetThreats(Coord square) {
            var piece = _board.GetPiece(square);
            return Piece.Type(piece) switch {
                Piece.Pawn => GetPawnThreats(square),
                Piece.Knight => GetKnightThreats(square),
                Piece.Queen => GetQueenThreats(square),
                Piece.Rook => GetRookThreats(square),
                Piece.Bishop => GetBishopThreats(square),
                Piece.King => GetKingThreats(square),
                _ => new List<Coord>()
            };
        }

        private Predicate<Coord> PinnedRestriction(Coord square) {
            if (square.Equals(_friendlyKing)) {
                return _ => true;
            }

            var dRank = _friendlyKing.Rank - square.Rank;
            var dFile = _friendlyKing.File - square.File;
            var dir = new Direction(Math.Clamp(dFile, -1, 1), Math.Clamp(dRank, -1, 1)); // Direction towards the king
            if (dir is { IsDiagonal: false, IsCardinal: false }) {
                return _ => true;
            }

            // Towards the king
            List<Coord> pathToKing = new List<Coord>();
            foreach (Coord sq in dir.MoveToEnd(square)) {
                if (sq.Equals(_friendlyKing)) {
                    break;
                }

                if (_board.IsEmpty(sq)) {
                    pathToKing.Add(sq);
                }
                else {
                    // Another piece is blocking the path to the king
                    return _ => true;
                }
            }

            dir.MoveToEnd(square).TakeWhile(sq => _board.IsEmpty(sq)).ToList();

            // Away from the king
            List<Coord> pathAwayFromKing = new List<Coord>();
            foreach (Coord sq in dir.Reverse().MoveToEnd(square)) {
                if (_board.IsEmpty(sq)) {
                    pathAwayFromKing.Add(sq);
                }
                else {
                    pathAwayFromKing.Add(sq); // Include the first piece encountered
                    break;
                }
            }

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
            var total = 0;
            Refresh();
            foreach (var move in LegalMoves()) {
                _board.CommitMove(move);
                total += CountMoves(depth - 1);
                _board.UndoMove();
            }

            return total;
        }

        public int CountMovesParallel(int depth) {
            if (depth == 0) return 1;
            return LegalMoves().AsParallel().WithDegreeOfParallelism(Environment.ProcessorCount)
                .Select(move => {
                    var copy = _board.Clone();
                    copy.CommitMove(move);
                    return new MoveGenerator(copy).CountMoves(depth - 1);
                }).Sum();
        }

        public int CountMovesWithConcurrentQueue(int depth) {
            if (depth == 0) return 1;
            var queue = new ConcurrentQueue<(Board board, int d)>();
            foreach (var move in LegalMoves()) {
                var copy = _board.Clone();
                copy.CommitMove(move);
                queue.Enqueue((copy, depth - 1));
            }

            int total = 0;
            int workers = Environment.ProcessorCount;

            Parallel.For(0, workers, new ParallelOptions(), _ => {
                var stack = new Stack<(Board board, int d)>();
                while (!queue.IsEmpty || stack.Count > 0) {
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
                    foreach (var mv in gen.LegalMoves()) {
                        var child = b.Clone();
                        child.CommitMove(mv);

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

        public bool ValidateMove(Coord from, Coord to, out Move validMove) {
            validMove = ValidMovesForSquare(from)
                .FirstOrDefault(m => m.To.Equals(to));

            // check whether a valid move was actually found
            if (!validMove.Equals(default(Move))) {
                return true;
            }

            return false;
        }
    }
}