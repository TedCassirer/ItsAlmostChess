using System;

namespace Core {
    public class Board {
        private int[,] _squares = new int[8, 8];
        public bool IsWhitesTurn { get; private set; } = true;


        public int ColorToMove => IsWhitesTurn ? Piece.White : Piece.Black;
        public int OpponentColor => IsWhitesTurn ? Piece.Black : Piece.White;
        public Coord? EnPassantTarget;
        private bool _whiteCanCastleKingside;
        private bool _whiteCanCastleQueenside;
        private bool _blackCanCastleKingside;
        private bool _blackCanCastleQueenside;
        public bool CanCastleKingSide => IsWhitesTurn ? _whiteCanCastleKingside : _blackCanCastleKingside;
        public bool CanCastleQueenSide => IsWhitesTurn ? _whiteCanCastleQueenside : _blackCanCastleQueenside;
        
        public int MoveNumber = 1;
        public int HalfmoveClock = 0;

        public void LoadFENPosition(string fen) {
            var rank = 7;
            var file = 0;
            Array.Clear(_squares, 0, _squares.Length);
            string[] parts = fen.Split(' ');
            string boardPlacement = parts[0];
            IsWhitesTurn = parts[1] == "w";
            string castlingRights = parts[2];
            _whiteCanCastleKingside = castlingRights.Contains('K');
            _whiteCanCastleQueenside = castlingRights.Contains('Q');
            _blackCanCastleKingside = castlingRights.Contains('k');
            _blackCanCastleQueenside = castlingRights.Contains('q');
            
            EnPassantTarget = parts[3] != "-" ? Coord.Parse(parts[3]) : null;
            HalfmoveClock = int.Parse(parts[4]);
            MoveNumber = int.Parse(parts[5]);

            foreach (var symbol in boardPlacement)
                if (symbol == '/') {
                    rank--;
                    file = 0;
                }
                else if (char.IsDigit(symbol)) {
                    file += (int)char.GetNumericValue(symbol);
                }
                else {
                    var piece = Piece.FromFENSymbol(symbol);
                    _squares[file, rank] = piece;
                    file++;
                }
        }

        public int GetPiece(Coord coord) {
            return GetPiece(coord.File, coord.Rank);
        }

        public int GetPiece(int file, int rank) {
            return _squares[file, rank];
        }

        public void CommitMove(Move move) {
            _squares[move.To.File, move.To.Rank] = _squares[move.From.File, move.From.Rank];
            _squares[move.From.File, move.From.Rank] = Piece.None;
            if (move.PromotionPiece != Piece.None) {
                // Handle promotion
                _squares[move.To.File, move.To.Rank] = move.PromotionPiece;
            }

            if (move.IsEnPassant) {
                var capturedPawnSquare = move.EnPassantCapturedPawnSquare.Value;
                _squares[capturedPawnSquare.File, capturedPawnSquare.Rank] = Piece.None;
            }

            if (move.IsCastling) {
                Coord rookFrom, rookTo;
                if (move.To.File == 6) {
                    // Kingside
                    rookFrom = Coord.Create(7, move.From.Rank);
                    rookTo = Coord.Create(5, move.From.Rank);
                } else {
                    // Queenside
                    rookFrom = Coord.Create(0, move.From.Rank);
                    rookTo = Coord.Create(3, move.From.Rank);
                }
                _squares[rookTo.File, rookTo.Rank] = _squares[rookFrom.File, rookFrom.Rank];
                _squares[rookFrom.File, rookFrom.Rank] = Piece.None;
                if (IsWhitesTurn) {
                    _whiteCanCastleKingside = false;
                    _whiteCanCastleQueenside = false;
                } else {
                    _blackCanCastleKingside = false;
                    _blackCanCastleQueenside = false;
                }
            } 

            IsWhitesTurn ^= true;
            MoveNumber += IsWhitesTurn ? 1 : 0;
            HalfmoveClock++;
        }

        public void UndoMove(Move move) {
            _squares[move.From.File, move.From.Rank] = _squares[move.To.File, move.To.Rank];
            _squares[move.To.File, move.To.Rank] = move.CapturedPiece;
            if (move.PromotionPiece != Piece.None) {
                // Revert promotion
                _squares[move.From.File, move.From.Rank] = Piece.Pawn | Piece.Color(move.PromotionPiece);
            }
            if (move.IsEnPassant) {
                var capturedPawnSquare = move.EnPassantCapturedPawnSquare.Value;
                _squares[capturedPawnSquare.File, capturedPawnSquare.Rank] = Piece.Pawn | OpponentColor;
            }
            

            IsWhitesTurn ^= true;
            MoveNumber -= IsWhitesTurn ? 0 : 1;
            HalfmoveClock--;
        }


        public Board Clone() {
            var newBoard = new Board();
            Array.Copy(_squares, newBoard._squares, _squares.Length);
            newBoard.IsWhitesTurn = IsWhitesTurn;
            return newBoard;
        }

        public bool IsPieceColor(Coord sq, int color) {
            return Piece.IsColor(GetPiece(sq), color);
        }

        public Coord GetFriendlyKing() {
            int kingPiece = Piece.King | ColorToMove;
            for (var file = 0; file < 8; file++) {
                for (var rank = 0; rank < 8; rank++) {
                    if (_squares[file, rank] == kingPiece) {
                        return Coord.Create(file, rank);
                    }
                }
            }

            return Coord.Invalid;
        }

        public bool IsEmpty(Coord sq) {
            return GetPiece(sq) == Piece.None;
        }

        public Coord FindPiece(int piece) {
            for (var file = 0; file < 8; file++) {
                for (var rank = 0; rank < 8; rank++) {
                    if (_squares[file, rank] == piece) {
                        return Coord.Create(file, rank);
                    }
                }
            }

            throw new Exception("Couldn't find the target piece");
        }
    }
}