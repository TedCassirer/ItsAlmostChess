using System;
using System.Collections;
using Core.AI;
using Unity.VisualScripting;
using UnityEngine;

namespace Core {
    public class GameManager : MonoBehaviour {
        [SerializeField] private BoardUI boardUI;
        private Board _board;
        private MoveGenerator _moveGenerator;
        private Player _whitePlayer;
        [SerializeField] private bool blackIsAi;
        [SerializeField] private bool whiteIsAi;
        [SerializeField] private float aiMoveDelay = 1f;
        private Player _blackPlayer;
        private Player PlayerToMove => _board.IsWhitesTurn ? _whitePlayer : _blackPlayer;

        public event Action<Move, bool> OnMoveExecuted; // (move, wasAi)

        public float AIMoveDelay => aiMoveDelay;

        // [SerializeField] private string StartingPosition = "rnbq1k1r/pp1Pbppp/2p5/8/2B5/8/PPP1NnPP/RNBQK2R w KQ - 1 8";
        [SerializeField] private string startingPosition = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";


        private void OnEnable() {
            InitializeBoard();
        }

        private void OnDisable() {
            ClearPlayers();
        }

        private void InitializeBoard() {
            if (_board == null) {
                _board = new Board();
            }

            if (_moveGenerator == null) {
                _moveGenerator = new MoveGenerator(_board);
            }

            if (boardUI == null) {
                boardUI = FindFirstObjectByType<BoardUI>();
            }

            boardUI.CreateBoardUI();
            _board.LoadFenPosition(startingPosition);
            _moveGenerator.Refresh();
            boardUI.UpdatePieces(_board);
            OnMoveExecuted += (move, wasAi) => boardUI.OnMoveChosen(move, wasAi);
        }

        private void InitializePlayers() {
            ClearPlayers();
            _whitePlayer = CreatePlayer(whiteIsAi);
            _blackPlayer = CreatePlayer(blackIsAi);
        }

        private void ClearPlayers() {
            if (_whitePlayer != null) {
                _whitePlayer.OnMoveChosen -= OnMoveChosen;
                Destroy(_whitePlayer);
                _whitePlayer = null;
            }

            if (_blackPlayer != null) {
                _blackPlayer.OnMoveChosen -= OnMoveChosen;
                Destroy(_blackPlayer);
                _blackPlayer = null;
            }
        }

        public void Start() {
            InitializePlayers();
            PlayerToMove.NotifyTurnToPlay();
            Debug.Log("Game started. White to move.");
        }


        [ContextMenu("Reset Game")]
        public void ResetGame() {
            Debug.Log("Resetting game...");
            StopAllCoroutines();
            _board.LoadFenPosition(startingPosition);
            _moveGenerator.Refresh();
            boardUI.Reset();
            InitializePlayers();
            PlayerToMove.NotifyTurnToPlay();
        }

        [ContextMenu("Undo Last Move")]
        public void UndoLastMove() {
            Debug.Log("Undoing last move...");
            StopAllCoroutines();
            _blackPlayer?.StopAllCoroutines();
            _whitePlayer?.StopAllCoroutines();
            _board.UndoMove();
            _moveGenerator.Refresh();
            boardUI.Reset();
            DelayAction(1f, () => PlayerToMove.NotifyTurnToPlay());
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
            OnMoveExecuted?.Invoke(move.Value, wasAi);
            NextTurn();
        }

        private void NextTurn() {
            if (_whitePlayer.IsAI && _blackPlayer.IsAI) {
                DelayAction(aiMoveDelay, () => PlayerToMove.NotifyTurnToPlay());
            }
            else {
                PlayerToMove.NotifyTurnToPlay();
            }
        }

        private void DelayAction(float delay, Action action) {
            StartCoroutine(DelayedActionCoroutine(delay, action));
            return;

            IEnumerator DelayedActionCoroutine(float delay, Action action) {
                yield return new WaitForSeconds(delay);
                action?.Invoke();
            }
        }


        private Player CreatePlayer(bool isAi) {
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

#if UNITY_EDITOR
        private void OnValidate() {
            if (!Application.isPlaying) return;
            if (_board != null) {
                StopAllCoroutines();
                InitializePlayers();
                if (PlayerToMove != null) {
                    PlayerToMove.NotifyTurnToPlay();
                }
            }
        }
#endif
    }
}