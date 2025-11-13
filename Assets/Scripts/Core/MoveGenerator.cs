using System;
using System.Collections.Generic;
using System.Linq;
using Utils;

namespace Core {
    public struct Direction {
        // CHANGED: was internal
        private readonly int _dFile;
        private readonly int _dRank;

        public bool IsDiagonal => Math.Abs(_dFile) == Math.Abs(_dRank) && _dFile != 0;
        public bool IsCardinal => (_dFile == 0) ^ (_dRank == 0);

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
                if (!c.InBounds) yield break;

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
        public bool InCheck => _checkingPieces.Count > 0;
        private bool InDoubleCheck => _checkingPieces.Count >= 2;

        private static readonly Direction[] Diagonals = {
            new(1, 1), new(1, -1), new(-1, 1), new(-1, -1)
        };

        private static readonly Direction[] Cardinals = {
            new(1, 0), new(-1, 0), new(0, 1), new(0, -1)
        };

        private static readonly Direction[] QueenDirections = Cardinals.Concat(Diagonals).ToArray();

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

            var type = Piece.Type(piece);
            IEnumerable<Coord> candidateTargets;
            if (InDoubleCheck) {
                candidateTargets = type == Piece.King ? GenerateKingMoves(square) : Enumerable.Empty<Coord>();
            }
            else {
                candidateTargets = type switch {
                    Piece.Pawn => GeneratePawnAttacks(square).Concat(GeneratePawnMoves(square)),
                    Piece.Knight => GenerateKnightMoves(square),
                    Piece.Bishop => GenerateSlidingMoves(square, Diagonals),
                    Piece.Rook => GenerateSlidingMoves(square, Cardinals),
                    Piece.Queen => GenerateSlidingMoves(square, QueenDirections),
                    Piece.King => GenerateKingMoves(square),
                    _ => Enumerable.Empty<Coord>()
                };
                if (InCheck && !square.Equals(_friendlyKing))
                    candidateTargets = candidateTargets.Where(ts =>
                        _squaresBlockingCheck.Contains(ts) ||
                        (ts.Equals(_board.EnPassantTarget) && type == Piece.Pawn));
            }

            return candidateTargets
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

            foreach (Move m in Move.CreatePromotionMove(baseMove, _board.ColorToMove)) yield return m;
        }


        private Move CreateMove(Coord from, Coord to) {
            if (to.Equals(_board.EnPassantTarget) &&
                Piece.Type(_board.GetPiece(from)) == Piece.Pawn)
                return Move.EnPassantMove(from, to);

            if (from.Equals(_friendlyKing) && Math.Abs(to.File - from.File) == 2)
                // Castling move
                return Move.Castle(from, to);

            return new Move(from, to, _board.GetPiece(to));
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

            var forward = Piece.IsColor(piece, Piece.White) ? 1 : -1;

            if (square.File > 0) {
                var leftAttackSquare = Coord.Create(square.File - 1, square.Rank + forward);
                if (Piece.Color(_board.GetPiece(leftAttackSquare)) == _board.OpponentColor)
                    yield return leftAttackSquare;
            }

            if (square.File < 7) {
                var rightAttackSquare = Coord.Create(square.File + 1, square.Rank + forward);
                if (Piece.Color(_board.GetPiece(rightAttackSquare)) == _board.OpponentColor)
                    yield return rightAttackSquare;
            }

            if (_board.EnPassantTarget.HasValue && IsEnPassantPossible(square)) {
                yield return _board.EnPassantTarget.Value;
            }
        }

        private bool IsEnPassantPossible(Coord pawnSquare) {
            if (!_board.EnPassantTarget.HasValue) {
                return false;
            }

            var forward = _board.IsWhitesTurn ? 1 : -1;
            if (pawnSquare.Rank + forward != _board.EnPassantTarget.Value.Rank) {
                return false;
            }

            if (Math.Abs(pawnSquare.File - _board.EnPassantTarget.Value.File) != 1) {
                return false;
            }

            if (_friendlyKing.Rank != pawnSquare.Rank) {
                return true;
            }
            // King might be in check after capturing. 
            
            // First check if there is another piece between the King and pawns
            var dFile = pawnSquare.File - _friendlyKing.File;
            var dir = new Direction(Math.Clamp(dFile, -1, 1), 0);
            Coord opponentPawnSquare = Coord.Create(_board.EnPassantTarget.Value.File, _friendlyKing.Rank);

            IEnumerable<Coord> coordsFromKing = dir.MoveToEnd(_friendlyKing);
            Coord suspect = coordsFromKing.Where(sq => !_board.IsEmpty(sq)).Skip(2).FirstOrDefault();
            if (suspect.Equals(default)) {
                // No other pieces on other side of pawns. We're good
                return true;
            }

            if (suspect.Equals(pawnSquare) || suspect.Equals(opponentPawnSquare)) {
                // This means another piece is blocking any potential checks between the pawns and the king. We're safe
                return true;
            }

            if (_board.GetPiece(suspect) == (Piece.Rook | _board.OpponentColor) ||
                _board.GetPiece(suspect) == (Piece.Queen | _board.OpponentColor)) {
                // Uh oh. This will cause a check
                return false;
            }

            return true;
        }

        private IEnumerable<Coord> GetPawnThreats(Coord square) {
            var forward = _board.IsWhitesTurn ? -1 : 1;

            if (square.File > 0) yield return Coord.Create(square.File - 1, square.Rank + forward);

            if (square.File < 7) yield return Coord.Create(square.File + 1, square.Rank + forward);
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

        private IEnumerable<Coord> GenerateSlidingMoves(Coord from, IEnumerable<Direction> dirs) {
            foreach (Direction dir in dirs)
            foreach (Coord coord in MovesIncludingCapturesInDirection(from, dir))
                if (!InCheck || _squaresBlockingCheck.Contains(coord))
                    yield return coord;
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

            if (_board.CanCastleKingSide && !InCheck) {
                var rightSquare = Coord.Create(square.File + 1, square.Rank);
                var right2Square = Coord.Create(square.File + 2, square.Rank);
                if (_board.IsEmpty(rightSquare) && _board.IsEmpty(right2Square) &&
                    _attackedSquares[rightSquare.File, rightSquare.Rank] == 0 &&
                    _attackedSquares[right2Square.File, right2Square.Rank] == 0)
                    moves = moves.Append(right2Square);
            }

            if (_board.CanCastleQueenSide && !InCheck) {
                var leftSquare = Coord.Create(square.File - 1, square.Rank);
                var left2Square = Coord.Create(square.File - 2, square.Rank);
                var left3Square = Coord.Create(square.File - 3, square.Rank);
                if (_board.IsEmpty(leftSquare) && _board.IsEmpty(left2Square) && _board.IsEmpty(left3Square) &&
                    _attackedSquares[leftSquare.File, leftSquare.Rank] == 0 &&
                    _attackedSquares[left2Square.File, left2Square.Rank] == 0)
                    moves = moves.Append(left2Square);
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
                    if (c.Equals(_friendlyKing)) _checkingPieces.Add(attackingPiece);
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
                foreach (Coord sq in dir.MoveToEnd(checkingSquare).TakeWhile(sq => !sq.Equals(_friendlyKing)))
                    _squaresBlockingCheck.Add(sq);
            }

            _checkingPieces.ForEach(checkingSquare => {
                var checkingPiece = _board.GetPiece(checkingSquare);
                if (Piece.Type(checkingPiece) is Piece.Queen or Piece.Bishop or Piece.Rook) {
                    // Mark the spaces behind the king as attacked to prevent king from moving there
                    var dRank = _friendlyKing.Rank - checkingSquare.Rank;
                    var dFile = _friendlyKing.File - checkingSquare.File;
                    var dir = new Direction(Math.Clamp(dFile, -1, 1), Math.Clamp(dRank, -1, 1));
                    foreach (Coord sq in dir
                                 .MoveToEnd(_friendlyKing)
                                 .TakeWhile(_board.IsEmpty))
                        _attackedSquares[sq.File, sq.Rank]++;
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
            if (square.Equals(_friendlyKing)) return _ => true;

            var dRank = _friendlyKing.Rank - square.Rank;
            var dFile = _friendlyKing.File - square.File;
            if (dRank != 0 && dFile != 0 && Math.Abs(dRank) != Math.Abs(dFile)) return _ => true;
            var dir = new Direction(Math.Clamp(dFile, -1, 1), Math.Clamp(dRank, -1, 1)); // Direction towards the king
            if (dir is { IsDiagonal: false, IsCardinal: false }) return _ => true;

            // Towards the king
            var pathToKing = new List<Coord>();
            foreach (Coord sq in dir.MoveToEnd(square)) {
                if (sq.Equals(_friendlyKing)) break;

                if (_board.IsEmpty(sq))
                    pathToKing.Add(sq);
                else
                    // Another piece is blocking the path to the king
                    return _ => true;
            }

            // Away from the king
            var pathAwayFromKing = new List<Coord>();
            foreach (Coord sq in dir.Reverse().MoveToEnd(square))
                if (_board.IsEmpty(sq)) {
                    pathAwayFromKing.Add(sq);
                }
                else {
                    pathAwayFromKing.Add(sq); // Include the first piece encountered
                    break;
                }

            if (pathAwayFromKing.Count == 0) return _ => true;

            var pieceInDir = _board.GetPiece(pathAwayFromKing.Last());
            if (Piece.IsColor(pieceInDir, _board.ColorToMove))
                // Friendly piece blocking the path
                return _ => true;

            var tsPieceType = Piece.Type(pieceInDir);
            if (tsPieceType == Piece.Queen || (tsPieceType == Piece.Bishop && dir.IsDiagonal) ||
                (tsPieceType == Piece.Rook && dir.IsCardinal))
                return sq => pathToKing.Contains(sq) || pathAwayFromKing.Contains(sq);

            return _ => true;
        }

        public int CountMoves(int depth) {
            if (depth == 0) {
                return 1;
            }
            var total = 0;
            Refresh();
            foreach (Move move in LegalMoves()) {
                _board.CommitMove(move);
                total += CountMoves(depth - 1);
                _board.UndoMove();
            }

            return total;
        }

        public bool ValidateMove(Coord from, Coord to, out Move validMove) {
            validMove = ValidMovesForSquare(from)
                .FirstOrDefault(m => m.To.Equals(to));

            // check whether a valid move was actually found
            if (!validMove.Equals(default)) return true;

            return false;
        }

        private IEnumerable<Coord> MovesIncludingCapturesInDirection(Coord square, Direction direction,
            bool bothColors = false) {
            foreach (Coord coord in direction.MoveToEnd(square)) {
                var targetPiece = _board.GetPiece(coord);
                if (targetPiece == Piece.None) {
                    yield return coord;
                }
                else {
                    if (bothColors || Piece.IsColor(targetPiece, _board.OpponentColor)) yield return coord;
                    yield break;
                }
            }
        }

        private int NextPieceInDir(Coord square, Direction direction) {
            foreach (Coord coord in direction.MoveToEnd(square)) {
                var targetPiece = _board.GetPiece(coord);
                if (targetPiece != Piece.None) {
                    return _board.GetPiece(coord);
                }
            }

            return Piece.None;
        }

        private IEnumerable<Coord> GetBishopThreats(Coord square) {
            return Diagonals.SelectMany(d => MovesIncludingCapturesInDirection(square, d, true));
        }

        private IEnumerable<Coord> GetRookThreats(Coord square) {
            return Cardinals.SelectMany(d => MovesIncludingCapturesInDirection(square, d, true));
        }

        private IEnumerable<Coord> GetQueenThreats(Coord square) {
            return QueenDirections.SelectMany(d => MovesIncludingCapturesInDirection(square, d, true));
        }
    }
}