using System;
using System.Linq;
using Utils;

namespace Core {
    public readonly struct Coord : IComparable<Coord>, IEquatable<Coord> {
        public static readonly Coord Invalid = Create(-1, -1);
        public readonly int File;
        public readonly int Rank;

        private static Coord[] _allSquares = Enumerable.Range(0, 8)
            .SelectMany(file => Enumerable.Range(0, 8).Select(rank => new Coord(file, rank)))
            .ToArray();

        private Coord(int file, int rank) {
            File = file;
            Rank = rank;
        }

        public static Coord Create(int file, int rank) {
            if (file < 0 || file >= 8 || rank < 0 || rank >= 8)
                return new Coord(file, rank);
            return _allSquares[file * 8 + rank];
        }

        public static Coord Parse(string squareName) {
            if (squareName.Length != 2)
                throw new ArgumentException("Invalid square name");
            var file = squareName[0] - 'a';
            var rank = squareName[1] - '1';
            return Create(file, rank);
        }

        public bool IsLightSquare() {
            return (File + Rank) % 2 != 0;
        }

        public int CompareTo(Coord other) {
            return File == other.File && Rank == other.Rank ? 0 : 1;
        }

        public bool InBounds => File is >= 0 and < 8 && Rank is >= 0 and < 8;

        public override string ToString() {
            return BoardUtils.SquareName(File, Rank);
        }

        public bool Equals(Coord other) {
            return File == other.File && Rank == other.Rank;
        }

        public override bool Equals(object obj) {
            return obj is Coord other && Equals(other);
        }

        public override int GetHashCode() {
            return HashCode.Combine(File, Rank);
        }
    }
}