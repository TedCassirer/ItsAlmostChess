using Core.AI;
using UnityEngine;

namespace Core {
    public class GameManager : MonoBehaviour {
        [SerializeField] private BoardUI boardUI;
        private static Board _board = new();
        private static MoveGenerator _moveGenerator = new(_board);
        private Player _whitePlayer;
        private Player _blackPlayer;

        // [SerializeField] private string StartingPosition = "rnbq1k1r/pp1Pbppp/2p5/8/2B5/8/PPP1NnPP/RNBQK2R w KQ - 1 8";
        [SerializeField] private string startingPosition = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";


        private void OnEnable() {
            boardUI = FindFirstObjectByType<BoardUI>();
            _board.LoadFENPosition(startingPosition);
            boardUI.UpdatePosition(_board);
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
            boardUI.UpdatePosition(_board);
        }

        public void Update() {
            _whitePlayer?.Update();
            _blackPlayer?.Update();
        }

        private void OnMoveChosen(Move move) {
            _board.CommitMove(move);
            boardUI.UpdatePosition(_board);
            _moveGenerator.Refresh();

            if (_board.IsWhitesTurn) {
                Debug.Log("White to move");
                _whitePlayer.NotifyTurnToPlay();
            }
            else {
                Debug.Log("Black to move");
                _blackPlayer.NotifyTurnToPlay();
            }
        }
    }
}