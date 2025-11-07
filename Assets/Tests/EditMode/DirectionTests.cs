using Core;
using NUnit.Framework;

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
            var from = Coord.Create(3, 3);
            var dir = new Direction(1, -1);
            var moved = dir.Move(from, 2);
            Assert.AreEqual(5, moved.File);
            Assert.AreEqual(1, moved.Rank);
        }

        [Test]
        public void ReverseInvertsDirection() {
            var dir = new Direction(2, -3).Reverse();
            var from = Coord.Create(4, 4);
            var moved = dir.Move(from, 1);
            Assert.AreEqual(2, moved.File);
            Assert.AreEqual(7, moved.Rank);
        }
    }
}