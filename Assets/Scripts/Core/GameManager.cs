using UnityEngine;

namespace Core {
    public class GameManager : MonoBehaviour {
        private BoardUI _boardUI;
        private Board _board;
        private Human _human;
        private MoveGenerator _moveGenerator;

        public string startingPosition = "8/8/8/3pP3/2K/8/8/7 w - d6 0 1";

        private void Awake() {
            _board = new Board();
            _boardUI = FindFirstObjectByType<BoardUI>();
            _board.LoadFENPosition(startingPosition);
            _moveGenerator = new MoveGenerator(_board);
            _human = new Human(_board, _boardUI, _moveGenerator);
        }
        
        [ContextMenu("Reset Game")]
        public void ResetGame() {
            Debug.Log("Resetting game...");
            _board.LoadFENPosition(startingPosition);
            _moveGenerator.Refresh();
            _boardUI.ResetSquares();
            _boardUI.UpdatePosition(_board);
        }

        public void Start() {
            _boardUI.UpdatePosition(_board);
        }

        public void Update() {
            _human.Update();
        }
    }
}