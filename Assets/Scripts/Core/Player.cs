using System.Collections.Generic;
using System.Linq;

namespace Core {
    public abstract class Player {
        public event System.Action<Move?> OnMoveChosen;
        
        private readonly Board _board;
        private readonly MoveGenerator _moveGenerator;
        
        public Player(Board board) {
            _board = board;
            _moveGenerator = new MoveGenerator(_board);
        }
        
        public abstract void Update();

        protected void ChooseMove(Move move) {
            OnMoveChosen?.Invoke(move);
        }

        public abstract void NotifyTurnToPlay();
        
        public List<Move> GetLegalMoves() {
            _moveGenerator.Refresh();
            return _moveGenerator.LegalMoves().OrderBy(m => -m.CapturedPiece).ToList();
            // return _moveGenerator.LegalMoves();
        }

        public bool IsHuman =>  this is Human;
        public bool IsAI =>  this is AI.AIPlayer;
    }
}