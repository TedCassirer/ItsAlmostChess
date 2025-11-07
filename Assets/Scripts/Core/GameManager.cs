using Core.AI;
using UnityEngine;

namespace Core {
    public class GameManager : MonoBehaviour {
        [SerializeField] private BoardUI boardUI;
        private Board _board = new();
        private MoveGenerator _moveGenerator;
        private Player _whitePlayer;
        private Player _blackPlayer;
        private Player playerToMove => _board.IsWhitesTurn ? _whitePlayer : _blackPlayer;

        // [SerializeField] private string StartingPosition = "rnbq1k1r/pp1Pbppp/2p5/8/2B5/8/PPP1NnPP/RNBQK2R w KQ - 1 8";
        [SerializeField] private string startingPosition = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";


        private void OnEnable() {
            _moveGenerator = new MoveGenerator(_board);
            boardUI = FindFirstObjectByType<BoardUI>();
            boardUI.CreateBoardUI();
            _board.LoadFENPosition(startingPosition);
            boardUI.UpdatePieces(_board);
            _moveGenerator.Refresh();
            _whitePlayer = new Human(_board, boardUI, _moveGenerator);
            _whitePlayer.OnMoveChosen += OnMoveChosen;

            _blackPlayer = new RandomAI(_moveGenerator);
            _blackPlayer.OnMoveChosen += OnMoveChosen;
        }

        public void Start() {
            Debug.Log("Game started. White to move.");
            if (_board.IsWhitesTurn)
                _whitePlayer.NotifyTurnToPlay();
            else {
                _blackPlayer.NotifyTurnToPlay();
            }
        }

        [ContextMenu("Reset Game")]
        public void ResetGame() {
            Debug.Log("Resetting game...");
            _board.LoadFENPosition(startingPosition);
            _moveGenerator.Refresh();
            boardUI.UpdatePieces(_board);
        }

        public void Update() {
            _whitePlayer?.Update();
            _blackPlayer?.Update();
        }

        private void OnMoveChosen(Move move) {
            _board.CommitMove(move);
            boardUI.OnMoveChosen(move, playerToMove.IsHuman);
            _moveGenerator.Refresh();
            playerToMove.NotifyTurnToPlay();
        }
    }
}