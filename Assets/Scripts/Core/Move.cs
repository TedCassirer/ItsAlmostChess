namespace Core {
    public struct Move {
        public readonly Coord from;
        public readonly Coord to;

        public Move(Coord from, Coord to) {
            this.from = from;
            this.to = to;
        }

        public bool IsInValid => from.file == to.file && from.rank == to.rank;

        public override string ToString() {
            return $"Move({from}, {to})";
        }
    }
}