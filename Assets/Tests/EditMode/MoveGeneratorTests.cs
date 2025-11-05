using System.Collections.Generic;
using System.Linq;
using Core;
using NUnit.Framework;

namespace Tests {
    public class MoveGeneratorTests {
        private const string StartingPosition = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR";
        private const string KingInCheckPosition = "3Rr/3B/2N/8/2n/8/r1Q1K/8";
        private const string DoubleCheck = "3Rr/8/8/8/8/8/r3K/8";

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
        public void StartPositionMovesCountIsCorrect() {
            var moves = Load(StartingPosition).ValidMoves();
            Assert.That(moves.Count, Is.EqualTo(20));
        }

        [Test]
        public void CaptureToAvoidCheck() {
            var moves = MovesFor(KingInCheckPosition, Piece.Rook | Piece.White);
            Assert.That(moves.Count, Is.EqualTo(1));
            Assert.That(moves[0].isCapture);
        }

        [Test]
        public void BlockCheckWithBishop() {
            var moves = MovesFor(KingInCheckPosition, Piece.Bishop | Piece.White);
            Assert.That(moves.Count, Is.EqualTo(2));
            Assert.That(moves.Count(m => m.isCapture), Is.EqualTo(1));
        }

        [Test]
        public void PinnedQueenCannotMove() {
            var moves = MovesFor(KingInCheckPosition, Piece.Queen | Piece.White);
            Assert.That(moves.Count, Is.EqualTo(0));
        }

        [Test]
        public void KnightCanBlockCheck() {
            var moves = MovesFor(KingInCheckPosition, Piece.Knight | Piece.White);
            Assert.That(moves.Count, Is.EqualTo(2));
        }

        [Test]
        public void KingCanMoveOutOfCheck() {
            var moves = MovesFor(KingInCheckPosition, Piece.King | Piece.White);
            Assert.That(moves.Count, Is.EqualTo(5));
        }

        [Test]
        public void DoubleCheckAllowsOnlyKingMoves() {
            var moves = Load(DoubleCheck).ValidMoves();
            Assert.That(moves.Count, Is.EqualTo(4));
        }
    }
}