using UnityEngine;

namespace Core {
    public class GameManager : MonoBehaviour {
        private BoardUI _boardUI;
        private Board _board;
        private Human _human;

        private static readonly string StartingPosition = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR";

        private void Awake() {
            _board = new Board();
            _boardUI = FindFirstObjectByType<BoardUI>();
            _board.LoadFENPosition(StartingPosition);
            _boardUI.UpdatePosition(_board);

            _human = new Human(_board, _boardUI, Piece.White);
        }

        public void Update() {
            _human.Update();
        }
    }
}