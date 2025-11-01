using System;

namespace Core {
    public struct Coord : IComparable<Coord> {
        public readonly int file;
        public readonly int rank;

        public Coord (int file, int rank) {
            this.file = file;
            this.rank = rank;
        }

        public bool IsLightSquare () {
            return (file + rank) % 2 != 1;
        }

        public int CompareTo (Coord other) {
            return (file == other.file && rank == other.rank) ? 0 : 1;
        }
    }
}