namespace Core {
    public struct Move {
        public readonly Coord from;
        public readonly Coord to;
        public readonly bool isCapture;

        public Move(Coord from, Coord to, bool isCapture = false) {
            this.from = from;
            this.to = to;
            this.isCapture = isCapture;
        }

        public bool IsInValid => from.file == to.file && from.rank == to.rank;

        public override string ToString() {
            return $"Move({from}, {to})";
        }
    }
}