using UnityEngine;

namespace Core {
    public class GameManager : MonoBehaviour {
        [SerializeField] private BoardUI boardUI;
        private static Board _board = new();
        private static MoveGenerator _moveGenerator = new(_board);
        private Human _human;

        [SerializeField] private string startingPosition = "rnbq1k1r/pp1Pbppp/2p5/8/2B5/8/PPP1NnPP/RNBQK2R w KQ - 1 8";

        private void OnEnable() {
            boardUI = FindFirstObjectByType<BoardUI>();
            _board.LoadFENPosition(startingPosition);
            boardUI.UpdatePosition(_board);
            _moveGenerator.Refresh();
            _human = new Human(_board, boardUI, _moveGenerator);
        }

        [ContextMenu("Reset Game")]
        public void ResetGame() {
            Debug.Log("Resetting game...");
            _board.LoadFENPosition(startingPosition);
            _moveGenerator.Refresh();
            boardUI.UpdatePosition(_board);
        }

        public void Update() {
            _human?.Update();
        }
    }
}