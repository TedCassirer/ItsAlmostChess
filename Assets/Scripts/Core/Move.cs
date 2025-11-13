using System;
using System.Collections.Generic;

namespace Core {
    public readonly struct Move : IEquatable<Move> {
        public readonly Coord From;
        public readonly Coord To;
        public readonly int MovePiece;
        public readonly int CapturedPiece;
        public readonly int PromotionPiece;
        public readonly bool IsEnPassant;
        public readonly bool IsCastling;

        public Coord? EnPassantCapturedPawnSquare => IsEnPassant
            ? Coord.Create(To.File, From.Rank)
            : null;

        public bool IsPromotion => PromotionPiece != Piece.None;
        public bool IsCapture => CapturedPiece != Piece.None;

        public Move(Coord from, Coord to, int movePiece, int capturedPiece = Piece.None,
            int promotionPiece = Piece.None,
            bool isEnPassant = false, bool isCastling = false) {
            From = from;
            To = to;
            MovePiece = movePiece;
            CapturedPiece = capturedPiece;
            PromotionPiece = promotionPiece;
            IsEnPassant = isEnPassant;
            IsCastling = isCastling;
        }

        public static Move EnPassantMove(Coord from, Coord to, int color) {
            var cappedColor = color == Piece.White ? Piece.Black : Piece.White;
            return new Move(from, to, movePiece: Piece.Pawn | color, capturedPiece: Piece.Pawn | cappedColor, isEnPassant: true);
        }

        public static Move Castle(Coord from, Coord to, int color) {
            return new Move(from, to, movePiece: Piece.King | color, isCastling: true);
        }

        public static List<Move> CreatePromotionMove(Move baseMove, int color) {
            return new List<Move> {
                new(baseMove.From,
                    baseMove.To,
                    movePiece: baseMove.MovePiece,
                    capturedPiece: baseMove.CapturedPiece,
                    promotionPiece: Piece.Queen | color),
                new(baseMove.From,
                    baseMove.To,
                    movePiece: baseMove.MovePiece,
                    capturedPiece: baseMove.CapturedPiece,
                    promotionPiece: Piece.Rook | color),
                new(baseMove.From,
                    baseMove.To,
                    movePiece: baseMove.MovePiece,
                    capturedPiece: baseMove.CapturedPiece,
                    promotionPiece: Piece.Bishop | color),
                new(baseMove.From,
                    baseMove.To,
                    movePiece: baseMove.MovePiece,
                    capturedPiece: baseMove.CapturedPiece,
                    promotionPiece: Piece.Knight | color)
            };
        }

        public override string ToString() {
            return $"{From}{To}{Piece.TypeChar(PromotionPiece)}";
        }

        public bool Equals(Move other) {
            return From.Equals(other.From) && To.Equals(other.To) && CapturedPiece == other.CapturedPiece &&
                   PromotionPiece == other.PromotionPiece;
        }

        public override bool Equals(object obj) {
            return obj is Move other && Equals(other);
        }

        public override int GetHashCode() {
            return HashCode.Combine(From, To, CapturedPiece, PromotionPiece);
        }
    }
}