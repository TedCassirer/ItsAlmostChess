using System.Collections;
using Core.AI;
using UnityEngine;

namespace Core {
    public class GameManager : MonoBehaviour {
        [SerializeField] private BoardUI boardUI;
        private Board _board = new();
        private MoveGenerator _moveGenerator;
        private Player _whitePlayer;
        public bool BlackIsAi;
        public bool WhiteIsAi;
        public float AiMoveDelay = 1f;
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
            if (WhiteIsAi) {
            }

            _whitePlayer = GetPlayer(WhiteIsAi);
            _blackPlayer = GetPlayer(BlackIsAi);
        }

        private Player GetPlayer(bool isAi) {
            Player player;
            if (isAi) {
                player = new MiniMaxV2(_board);
            }
            else {
                player = new Human(_board, boardUI, _moveGenerator);
            }

            player.OnMoveChosen += OnMoveChosen;
            return player;
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
            OnEnable();
            Start();
        }

        public void Update() {
            _whitePlayer?.Update();
            _blackPlayer?.Update();
        }

        private void OnMoveChosen(Move? move) {
            if (move == null) return;
            bool wasAi = playerToMove.IsAI;
            _board.CommitMove(move.Value);
            _moveGenerator.Refresh();
            boardUI.OnMoveChosen(move.Value, wasAi);

            if (WhiteIsAi && BlackIsAi) {
                // Sleep for a short duration to allow UI to update
                StartCoroutine(DelayedNextTurn(AiMoveDelay));
            }
            else {
                playerToMove.NotifyTurnToPlay();
            }
        }

        private IEnumerator DelayedNextTurn(float delay) {
            yield return new WaitForSeconds(delay);
            
            playerToMove.NotifyTurnToPlay();
        }
    }
}