using System;
using Utils;

namespace Core {
    public struct Coord : IComparable<Coord>, IEquatable<Coord> {
        public readonly int file;
        public readonly int rank;

        public Coord(int file, int rank) {
            this.file = file;
            this.rank = rank;
        }

        public bool IsLightSquare() {
            return (file + rank) % 2 != 1;
        }

        public int CompareTo(Coord other) {
            return file == other.file && rank == other.rank ? 0 : 1;
        }

        public override string ToString() {
            return BoardUtils.SquareName(file, rank);
        }

        public bool Equals(Coord other) {
            return file == other.file && rank == other.rank;
        }

        public override bool Equals(object obj) {
            return obj is Coord other && Equals(other);
        }

        public override int GetHashCode() {
            return HashCode.Combine(file, rank);
        }
    }
}