namespace Core {
    public struct Move {
        public readonly Coord from;
        public readonly Coord to;
        public readonly bool isCapture;
        public readonly int promotionPiece;

        public Move(Coord from, Coord to, bool isCapture = false) {
            this.from = from;
            this.to = to;
            this.isCapture = isCapture;
            this.promotionPiece = Piece.None;
        }

        public override string ToString() {
            return $"Move({from}, {to}, {isCapture})";
        }
    }
}