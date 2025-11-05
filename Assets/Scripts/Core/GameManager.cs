using UnityEngine;

namespace Core {
    public class GameManager : MonoBehaviour {
        private BoardUI _boardUI;
        private Board _board;
        private Human _human;
        private MoveGenerator _moveGenerator;

        private string startingPosition = "rnbq1k1r/pp1Pbppp/2p5/8/2B5/8/PPP1NnPP/RNBQK2R w KQ - 1 8";

        private void Awake() {
            _board = new Board();
            _boardUI = FindFirstObjectByType<BoardUI>();
            _moveGenerator = new MoveGenerator(_board);
            _human = new Human(_board, _boardUI, _moveGenerator);
        }

        public void Start() {
            _board.LoadFENPosition(startingPosition);
            _boardUI.UpdatePosition(_board);
            _moveGenerator.Refresh();
        }
        
        [ContextMenu("Reset Game")]
        public void ResetGame() {
            Debug.Log("Resetting game...");
            _board.LoadFENPosition(startingPosition);
            _moveGenerator.Refresh();
            _boardUI.ResetSquares();
            _boardUI.UpdatePosition(_board);
        }


        public void Update() {
            _human.Update();
        }
    }
}