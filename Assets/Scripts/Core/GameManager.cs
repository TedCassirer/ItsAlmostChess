using System.Collections;
using Core.AI;
using Unity.VisualScripting;
using UnityEngine;

namespace Core {
    public class GameManager : MonoBehaviour {
        [SerializeField] private BoardUI boardUI;
        private Board _board = new();
        private MoveGenerator _moveGenerator;
        private Player _whitePlayer;
        [SerializeField] private bool blackIsAi;
        [SerializeField] private bool whiteIsAi;
        [SerializeField] private float aiMoveDelay = 1f;
        private Player _blackPlayer;
        private Player PlayerToMove => _board.IsWhitesTurn ? _whitePlayer : _blackPlayer;

        public bool BlackIsAI => blackIsAi;
        public bool WhiteIsAI => whiteIsAi;
        public float AIMoveDelay => aiMoveDelay;

        // [SerializeField] private string StartingPosition = "rnbq1k1r/pp1Pbppp/2p5/8/2B5/8/PPP1NnPP/RNBQK2R w KQ - 1 8";
        [SerializeField] private string startingPosition = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";


        private void OnEnable() {
            _moveGenerator = new MoveGenerator(_board);
            boardUI = FindFirstObjectByType<BoardUI>();
            boardUI.CreateBoardUI();
            _board.LoadFenPosition(startingPosition);
            boardUI.UpdatePieces(_board);
            _moveGenerator.Refresh();


        }

        private Player GetPlayer(bool isAi) {
            Player player;
            if (isAi) {
                player = transform.AddComponent<AIPlayer>();
            }
            else {
                player = transform.AddComponent<Human>();
            }
            player.Init(_board);
            player.OnMoveChosen += OnMoveChosen;
            return player;
        }

        public void Start() {
            _whitePlayer = GetPlayer(WhiteIsAI);
            _blackPlayer = GetPlayer(BlackIsAI);
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
            _board.LoadFenPosition(startingPosition);
            _moveGenerator.Refresh();
            boardUI.UpdatePieces(_board);
            OnEnable();
            Start();
        }

        public void Update() {
            _whitePlayer?.Tick();
            _blackPlayer?.Tick();
        }

        private void OnMoveChosen(Move? move) {
            if (move == null) return;
            bool wasAi = PlayerToMove.IsAI;
            _board.CommitMove(move.Value);
            _moveGenerator.Refresh();
            boardUI.OnMoveChosen(move.Value, wasAi);

            if (WhiteIsAI && BlackIsAI) {
                // Sleep for a short duration to allow UI to update
                StartCoroutine(DelayedNextTurn(AIMoveDelay));
            }
            else {
                PlayerToMove.NotifyTurnToPlay();
            }
        }

        private IEnumerator DelayedNextTurn(float delay) {
            yield return new WaitForSeconds(delay);
            
            PlayerToMove.NotifyTurnToPlay();
        }
    }
}