using System;

namespace Core {
    public readonly struct Move : IEquatable<Move> {
        public readonly Coord From;
        public readonly Coord To;
        public readonly int CapturedPiece;
        public readonly int PromotionPiece;

        public Move(Coord from, Coord to, int capturedPiece = Piece.None, int promotionPiece = Piece.None) {
            From = from;
            To = to;
            CapturedPiece = capturedPiece;
            PromotionPiece = promotionPiece;
        }

        public override string ToString() {
            return $"Move({From}, {To}, {CapturedPiece})";
        }

        public bool Equals(Move other) {
            return From.Equals(other.From) && To.Equals(other.To) && CapturedPiece == other.CapturedPiece && PromotionPiece == other.PromotionPiece;
        }

        public override bool Equals(object obj) {
            return obj is Move other && Equals(other);
        }

        public override int GetHashCode() {
            return HashCode.Combine(From, To, CapturedPiece, PromotionPiece);
        }
    }
}