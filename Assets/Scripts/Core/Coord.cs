using System;
using System.Linq;
using Unity.VisualScripting;
using Utils;

namespace Core {
    public readonly struct Coord : IComparable<Coord>, IEquatable<Coord> {
        public readonly int file;
        public readonly int rank;

        private static Coord[] _allSquares = Enumerable.Range(0, 8)
            .SelectMany(file => Enumerable.Range(0, 8).Select(rank => new Coord(file, rank)))
            .ToArray();

        private Coord(int file, int rank) {
            this.file = file;
            this.rank = rank;
        }

        public static Coord Create(int file, int rank) {
            if (file < 0 || file >= 8 || rank < 0 || rank >= 8)
                return new Coord(file, rank);
            return _allSquares[file * 8 + rank];
        }

        public bool IsLightSquare() {
            return (file + rank) % 2 != 1;
        }

        public int CompareTo(Coord other) {
            return file == other.file && rank == other.rank ? 0 : 1;
        }

        public bool InBounds => (file is >= 0 and < 8) && (rank is >= 0 and < 8);

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