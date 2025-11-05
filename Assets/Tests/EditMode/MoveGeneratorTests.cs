using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Core;
using NUnit.Framework;

namespace Tests {
    public class MoveGeneratorTests {
        private const string StartingPosition = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR";
        private const string KingInCheckPosition = "3Rr/3B/2N/8/2n/8/r1Q1K/8";
        private const string DoubleCheck = "3Rr/8/8/8/8/8/r3K/8";
        private const string PawnPromotionPosition = "8/4P3/8/8/8/8/8/K7";

        private Board board;
        private MoveGenerator generator;

        private MoveGenerator Load(string fen) {
            board = new Board();
            board.LoadFENPosition(fen);
            generator = new MoveGenerator(board);
            return generator;
        }

        private List<Move> MovesFor(string fen, int piece) {
            Load(fen);
            var coord = board.FindPiece(piece);
            return generator.ValidMovesForSquare(coord);
        }

        [Test]
        public void CaptureToAvoidCheck() {
            List<Move> moves = MovesFor(KingInCheckPosition, Piece.Rook | Piece.White);
            Assert.That(moves.Count, Is.EqualTo(1));
            Assert.That(moves[0].isCapture);
        }

        [Test]
        public void BlockCheckWithBishop() {
            List<Move> moves = MovesFor(KingInCheckPosition, Piece.Bishop | Piece.White);
            Assert.That(moves.Count, Is.EqualTo(2));
            Assert.That(moves.Count(m => m.isCapture), Is.EqualTo(1));
        }

        [Test]
        public void PinnedQueenCannotMove() {
            List<Move> moves = MovesFor(KingInCheckPosition, Piece.Queen | Piece.White);
            Assert.That(moves.Count, Is.EqualTo(0));
        }

        [Test]
        public void KnightCanBlockCheck() {
            List<Move> moves = MovesFor(KingInCheckPosition, Piece.Knight | Piece.White);
            Assert.That(moves.Count, Is.EqualTo(2));
        }

        [Test]
        public void KingCanMoveOutOfCheck() {
            List<Move> moves = MovesFor(KingInCheckPosition, Piece.King | Piece.White);
            Assert.That(moves.Count, Is.EqualTo(5));
        }

        [Test]
        public void DoubleCheckAllowsOnlyKingMoves() {
            List<Move> moves = Load(DoubleCheck).ValidMoves();
            Assert.That(moves.Count, Is.EqualTo(4));
        }

        [Test]
        public void PawnPromotionMoves() {
            List<Move> moves = MovesFor(PawnPromotionPosition, Piece.Pawn | Piece.White);
            Assert.That(moves.Count, Is.EqualTo(4));
            Assert.That(moves.Select(m => m.promotionPiece).Distinct().Count(), Is.EqualTo(4));
            Assert.That(moves.All(m =>
                Piece.Type(m.promotionPiece) is Piece.Queen or Piece.Rook or Piece.Bishop or Piece.Knight));
        }
          
        [TestCase(1, 20)]
        [TestCase(2, 400)]
        [TestCase(3, 8_902)]
        [TestCase(4, 197_281)]
        [TestCase(5, 4_865_609)] // Got 4_865_167, 1 min 27 seconds with root split parallel, 54s with ConcurrentQueue
        public void ShannonNumberCalculation(int depth, int expectedMoves, int timeoutMs = 5_000) {
            Load(StartingPosition);
            CancellationTokenSource source = new CancellationTokenSource();
            CancellationToken token = source.Token;
            source.CancelAfter(timeoutMs);
            var moveCount = generator.CountMovesWithConcurrentQueue(depth, token);
            Assert.That(moveCount, Is.EqualTo(expectedMoves));
        }
    }
}