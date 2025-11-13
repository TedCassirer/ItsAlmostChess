using System;
using System.Collections.Generic;

namespace Core {
    public readonly struct Move : IEquatable<Move> {
        public readonly Coord From;
        public readonly Coord To;
        public readonly int CapturedPiece;
        public readonly int PromotionPiece;
        public readonly bool IsEnPassant;
        public readonly bool IsCastling;

        public Coord? EnPassantCapturedPawnSquare => IsEnPassant
            ? Coord.Create(To.File, From.Rank)
            : null;

        public bool IsPromotion => PromotionPiece != Piece.None;
        public bool IsCapture => CapturedPiece != Piece.None;

        public Move(Coord from, Coord to, int capturedPiece = Piece.None, int promotionPiece = Piece.None,
            bool isEnPassant = false, bool isCastling = false) {
            From = from;
            To = to;
            CapturedPiece = capturedPiece;
            PromotionPiece = promotionPiece;
            IsEnPassant = isEnPassant;
            IsCastling = isCastling;
        }

        public static Move EnPassantMove(Coord from, Coord to) {
            var cappedColor = from.Rank < to.Rank ? Piece.Black : Piece.White;
            return new Move(from, to, Piece.Pawn | cappedColor, isEnPassant: true);
        }

        public static Move Castle(Coord from, Coord to) {
            return new Move(from, to, isCastling: true);
        }

        public static List<Move> CreatePromotionMove(Move baseMove, int color) {
            return new List<Move> {
                new(baseMove.From, baseMove.To, baseMove.CapturedPiece, Piece.Queen | color),
                new(baseMove.From, baseMove.To, baseMove.CapturedPiece, Piece.Rook | color),
                new(baseMove.From, baseMove.To, baseMove.CapturedPiece, Piece.Bishop | color),
                new(baseMove.From, baseMove.To, baseMove.CapturedPiece, Piece.Knight | color)
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