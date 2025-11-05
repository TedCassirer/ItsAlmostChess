using NUnit.Framework;
using System.Collections.Generic;
using Core;
using Utils;

namespace Tests {
    public class DirectionTests {
        [Test]
        public void DiagonalFlagsWork() {
            Assert.IsTrue(new Direction(1, 1).IsDiagonal);
            Assert.IsTrue(new Direction(-1, -1).IsDiagonal);
            Assert.IsFalse(new Direction(1, 0).IsDiagonal);
            Assert.IsFalse(new Direction(0, 1).IsDiagonal);
        }

        [Test]
        public void CardinalFlagsWork() {
            Assert.IsTrue(new Direction(1, 0).IsCardinal);
            Assert.IsTrue(new Direction(0, -1).IsCardinal);
            Assert.IsFalse(new Direction(1, 1).IsCardinal);
            Assert.IsFalse(new Direction(-1, -1).IsCardinal);
        }

        [Test]
        public void MoveAppliesSteps() {
            var from = new Coord(3, 3);
            var dir = new Direction(1, -1);
            var moved = dir.Move(from, 2);
            Assert.AreEqual(5, moved.file);
            Assert.AreEqual(1, moved.rank);
        }

        [Test]
        public void ReverseInvertsDirection() {
            var dir = new Direction(2, -3).Reverse();
            var from = new Coord(4, 4);
            var moved = dir.Move(from, 1);
            Assert.AreEqual(2, moved.file);
            Assert.AreEqual(7, moved.rank);
        }

        [Test]
        public void MoveUntilStopsAtPredicateExcludingStopSquare() {
            var dir = new Direction(1, 0);
            var start = new Coord(2, 0);
            // Stop when file == 5
            var path = new List<Coord>(dir.MoveUntil(start, c => c.file == 5));
            // Should contain files 3 and 4 only
            Assert.AreEqual(2, path.Count);
            Assert.IsTrue(path.Exists(c => c.file == 3));
            Assert.IsTrue(path.Exists(c => c.file == 4));
        }

        [Test]
        public void MoveUntilStopsIncludingStopSquare() {
            var dir = new Direction(1, 0);
            var start = new Coord(1, 0);
            var path = new List<Coord>(dir.MoveUntil(start, c => c.file == 4, includeStopSquare: true));
            // Files: 2,3,4
            Assert.AreEqual(3, path.Count);
            Assert.AreEqual(2, path[0].file);
            Assert.AreEqual(3, path[1].file);
            Assert.AreEqual(4, path[2].file);
        }

        [Test]
        public void MoveUntilIncludeStartSquareWorks() {
            var dir = new Direction(0, 1);
            var start = new Coord(0, 0);
            var path = new List<Coord>(dir.MoveUntil(start, c => c.rank == 2, includeStartSquare: true, includeStopSquare: true));
            // Ranks: 0(start),1,2(stop)
            Assert.AreEqual(3, path.Count);
            Assert.AreEqual(0, path[0].rank);
            Assert.AreEqual(1, path[1].rank);
            Assert.AreEqual(2, path[2].rank);
        }

        [Test]
        public void MoveUntilOutOfBoundsTerminates() {
            var dir = new Direction(0, 1);
            var start = new Coord(5, 6);
            var path = new List<Coord>(dir.MoveUntil(start));
            // Should include rank 7 only (8 is out of bounds)
            Assert.AreEqual(1, path.Count);
            Assert.AreEqual(7, path[0].rank);
        }
    }
}

