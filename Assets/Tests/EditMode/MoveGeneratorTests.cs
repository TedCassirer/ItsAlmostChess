using System.Collections.Generic;
using System.Linq;
using Core;
using NUnit.Framework;

namespace Tests {
    public class MoveGeneratorTests {
        private const string StartingPosition = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
        private const string KingInCheckPosition = "3Rr/3B/2N/8/2n/8/r1Q1K/8 w - - 0 1";
        private const string DoubleCheck = "3Rr/8/8/8/8/8/r3K/8 w - - 0 1";
        private const string PawnPromotionPosition = "8/4P3/8/8/8/8/8/K7 w - - 0 1";
        private const string EnPassantCheckThreatPosition = "8/8/8/3pP3/2K/8/4r3/7 w - d6 0 1";
        private const string CastlingPosition = "r3k2r/8/8/8/8/8/8/R3K2R w KQkq - 0 1";


        private Board board;
        private MoveGenerator generator;

        private MoveGenerator Load(string fen) {
            board = new Board();
            board.LoadFenPosition(fen);
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
            Assert.That(moves[0].CapturedPiece == (Piece.Rook | Piece.Black));
        }

        [Test]
        public void BlockCheckWithBishop() {
            List<Move> moves = MovesFor(KingInCheckPosition, Piece.Bishop | Piece.White);
            Assert.That(moves.Count, Is.EqualTo(2));
            Assert.That(moves.Count(m => m.CapturedPiece != Piece.None), Is.EqualTo(1));
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
            List<Move> moves = Load(DoubleCheck).LegalMoves();
            Assert.That(moves.Count, Is.EqualTo(4));
        }

        [Test]
        public void PawnPromotionMoves() {
            List<Move> moves = MovesFor(PawnPromotionPosition, Piece.Pawn | Piece.White);
            Assert.That(moves.Count, Is.EqualTo(4));
            Assert.That(moves.Select(m => m.PromotionPiece).Distinct().Count(), Is.EqualTo(4));
            Assert.That(moves.All(m =>
                Piece.Type(m.PromotionPiece) is Piece.Queen or Piece.Rook or Piece.Bishop or Piece.Knight));
        }

        [TestCase("8/8/8/3pP3/8/8/8/K7 w - d6 0 1", Piece.White)]
        [TestCase("8/8/8/8/3pP3/8/8/K7 b - e3 0 1", Piece.Black)]
        public void EnPassantMove(string position, int color) {
            List<Move> moves = MovesFor(position, Piece.Pawn | color);
            Assert.That(moves.Count, Is.EqualTo(2));
            Move epMove = moves.First(m => m.IsEnPassant);
            board.CommitMove(epMove);
            Assert.That(board.GetPiece(epMove.EnPassantCapturedPawnSquare.Value), Is.EqualTo(Piece.None));
        }

        [Test]
        public void EnPassantToAvoidCheck() {
            List<Move> moves = MovesFor(EnPassantCheckThreatPosition, Piece.Pawn | Piece.White);
            Assert.That(moves.Count, Is.EqualTo(1));
            Move epMove = moves[0];
            Assert.That(epMove.IsEnPassant);
        }

        [Test]
        public void CastlingMoves() {
            List<Move> moves = MovesFor(CastlingPosition, Piece.King | Piece.White);

            Assert.That(moves.Count(m => m.IsCastling), Is.EqualTo(2));
        }

        [TestCase("8/8/8/2KpP1r1/8/8/8/8 w - d6 0 1")]
        [TestCase("8/8/8/2KpPr2/8/8/8/8 w - d6 0 1")]
        [TestCase("8/8/8/2KPpr2/8/8/8/8 w - e6 0 1")]
        [TestCase("8/8/8/2rpP1K1/8/8/8/8 w - d6 0 1")]
        [TestCase("8/8/8/2rPpK2/8/8/8/8 w - e6 0 1")]
        public void EnPassantPinnedTest(string pos) {
            List<Move> moves = MovesFor(pos, Piece.Pawn | Piece.White);
            
            Assert.That(moves.Count(m => m.IsEnPassant), Is.EqualTo(0));
        }
        
        [TestCase("8/8/8/KN1pP1r1/8/8/8/8 w - d6 0 1")]
        [TestCase("8/8/8/K1NpPr2/8/8/8/8 w - d6 0 1")]
        [TestCase("8/8/8/K1NPpr2/8/8/8/8 w - e6 0 1")]
        [TestCase("8/8/8/2rpPNK/8/8/8/8 w - d6 0 1")]
        [TestCase("8/8/8/2rPpN1K/8/8/8/8 w - e6 0 1")]
        public void EnPassantNotPinnedTest(string pos) {
            List<Move> moves = MovesFor(pos, Piece.Pawn | Piece.White);
            
            Assert.That(moves.Count(m => m.IsEnPassant), Is.EqualTo(1));
        }
        
        [TestCase(1, 20)]
        [TestCase(2, 400)]
        [TestCase(3, 8_902)]
        [TestCase(4, 197_281)]
        [TestCase(5, 4_865_609)]
        // [TestCase(6, 119_060_324)]
        public void ShannonNumberCalculation(int depth, int expectedMoves) {
            /***
             * For 5, got 4_865_167, 1 min 27 seconds with root split parallel, 54s with ConcurrentQueue.
             * Some optimizations later it's down to 8 seconds. Still copying the board for each move though.
             *
             * Got 4865351 after fixing a bug for checking if a piece is pinned.
             */
            Load(StartingPosition);
            var moveCount = generator.CountMoves(depth);
            Assert.That(moveCount, Is.EqualTo(expectedMoves));
        }
        
        [TestCase(1, 44)]
        [TestCase(2, 1486)]
        [TestCase(3, 62379)]
        [TestCase(4, 2103487)]
        // [TestCase(5, 89941194)]
        public void PerformanceTestPos5(int depth, int expectedMoves) {
            // https://www.chessprogramming.org/Perft_Results#Position_5
            const string position5Perft = "rnbq1k1r/pp1Pbppp/2p5/8/2B5/8/PPP1NnPP/RNBQK2R w KQ - 1 8";
            Load(position5Perft);
            var moveCount = generator.CountMoves(depth);
            Assert.That(moveCount, Is.EqualTo(expectedMoves));
        }
    }
}
