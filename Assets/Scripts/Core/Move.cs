using System;
using System.Collections.Generic;

namespace Core {
    public readonly struct Move : IEquatable<Move> {
        public readonly Coord From;
        public readonly Coord To;
        public readonly int CapturedPiece;
        public readonly int PromotionPiece;
        public readonly bool IsEnPassant;

        public Coord? EnPassantCapturedPawnSquare => IsEnPassant
            ? Coord.Create(To.File, From.Rank)
            : null;

        public bool IsPromotion => PromotionPiece != Piece.None;
        public bool IsCapture => CapturedPiece != Piece.None;

        public Move(Coord from, Coord to, int capturedPiece = Piece.None, int promotionPiece = Piece.None,
            bool isEnPassant = false) {
            From = from;
            To = to;
            CapturedPiece = capturedPiece;
            PromotionPiece = promotionPiece;
            IsEnPassant = isEnPassant;
        }

        public static Move CreateEnPassantMove(Coord from, Coord to) {
            var cappedColor = from.Rank < to.Rank ? Piece.Black : Piece.White;
            return new Move(from, to, capturedPiece: Piece.Pawn | cappedColor, isEnPassant: true);
        }

        public static List<Move> CreatePromotionMove(Move baseMove, int color) {
            return new List<Move> {
                new(baseMove.From, baseMove.To, baseMove.CapturedPiece, promotionPiece: Piece.Queen | color),
                new(baseMove.From, baseMove.To, baseMove.CapturedPiece, promotionPiece: Piece.Rook | color),
                new(baseMove.From, baseMove.To, baseMove.CapturedPiece, promotionPiece: Piece.Bishop | color),
                new(baseMove.From, baseMove.To, baseMove.CapturedPiece, promotionPiece: Piece.Knight | color)
            };
        }

        public override string ToString() {
            return $"Move({From}, {To}, {CapturedPiece})";
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