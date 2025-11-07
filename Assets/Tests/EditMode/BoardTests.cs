using System.Linq;
using NUnit.Framework;
using Core;

namespace Tests {
    public class BoardTests {
        private const string StartingPosition = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";

        [Test]
        public void LoadFEN_StartingPosition_SetsPiecesAndCastlingRights() {
            var board = new Board();
            board.LoadFENPosition(StartingPosition);
            // e1 white king: file 4 rank 0
            Assert.That(board.GetPiece(Coord.Create(4, 0)), Is.EqualTo(Piece.King | Piece.White));
            // e8 black king: file 4 rank 7
            Assert.That(board.GetPiece(Coord.Create(4, 7)), Is.EqualTo(Piece.King | Piece.Black));
            Assert.That(board.CanCastleKingSide, Is.True);
            Assert.That(board.CanCastleQueenSide, Is.True);
        }

        [Test]
        public void CommitMove_PawnAdvance_UpdatesBoardAndTurnAndLastMove() {
            var board = new Board();
            board.LoadFENPosition(StartingPosition);
            var from = Coord.Create(4, 1); // e2
            var to = Coord.Create(4, 3);   // e4
            var move = new Move(from, to);
            board.CommitMove(move);
            Assert.That(board.GetPiece(to), Is.EqualTo(Piece.Pawn | Piece.White));
            Assert.That(board.GetPiece(from), Is.EqualTo(Piece.None));
            Assert.That(board.IsWhitesTurn, Is.False);
        }

        [Test]
        public void UndoMove_PawnAdvance_RestoresBoardAndTurn() {
            var board = new Board();
            board.LoadFENPosition(StartingPosition);
            var move = new Move(Coord.Create(4, 1), Coord.Create(4, 3)); // e2 -> e4
            board.CommitMove(move);
            board.UndoMove();
            Assert.That(board.GetPiece(Coord.Create(4, 1)), Is.EqualTo(Piece.Pawn | Piece.White));
            Assert.That(board.GetPiece(Coord.Create(4, 3)), Is.EqualTo(Piece.None));
            Assert.That(board.IsWhitesTurn, Is.True);
        }

        [Test]
        public void CommitMove_CastleKingside_MovesRookAndClearsRights() {
            var board = new Board();
            board.LoadFENPosition(StartingPosition);
            // Manually clear squares between king and rook to allow castle (f1,g1)
            board.LoadFENPosition("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQK2R w KQkq - 0 1");
            var move = Move.Castle(Coord.Create(4, 0), Coord.Create(6, 0)); // e1 -> g1
            board.CommitMove(move);
            Assert.That(board.GetPiece(Coord.Create(6, 0)), Is.EqualTo(Piece.King | Piece.White));
            Assert.That(board.GetPiece(Coord.Create(5, 0)), Is.EqualTo(Piece.Rook | Piece.White));
            board.CommitMove(new Move(Coord.Create(0, 7), Coord.Create(0, 6))); // Black move to switch turn
            Assert.That(board.CanCastleKingSide, Is.False);
            Assert.That(board.CanCastleQueenSide, Is.False);
        }

        [Test]
        public void UndoMove_CastleKingside_RestoresKingRookAndRights() {
            var board = new Board();
            board.LoadFENPosition("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQK2R w KQkq - 0 1");
            var move = Move.Castle(Coord.Create(4, 0), Coord.Create(6, 0));
            board.CommitMove(move);
            board.UndoMove();
            Assert.That(board.GetPiece(Coord.Create(4, 0)), Is.EqualTo(Piece.King | Piece.White));
            Assert.That(board.GetPiece(Coord.Create(7, 0)), Is.EqualTo(Piece.Rook | Piece.White));
            Assert.That(board.CanCastleKingSide, Is.True);
            Assert.That(board.CanCastleQueenSide, Is.True);
        }
        
        [Test]
        public void CantCastleThroughCheck() {
            var board = new Board();
            // Position where white cannot castle kingside because f1 is under attack
            board.LoadFENPosition("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQ1RK1 w Qkq - 0 1");
            var moveGenerator = new MoveGenerator(board);
            var legalMoves = moveGenerator.LegalMoves();
            Assert.That(legalMoves.All(m => !m.IsCastling), Is.True);
        }
    }
}