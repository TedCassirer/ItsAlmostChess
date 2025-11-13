using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

namespace Core.AI {
    public class AIPlayer : Player {
        private IMoveProvider _moveProvider;
        [SerializeField] private AIKind aiKind = AIKind.MiniMax; // default

        protected override void OnInitialized() {
            ConfigureProvider();
        }

        private void ConfigureProvider() {
            // choose implementation based on enum
            _moveProvider = aiKind switch {
                AIKind.Random => new RandomAI(),
                AIKind.MiniMax => new MiniMaxV2(),
                _ => throw new System.ArgumentOutOfRangeException()
            };
        }

        protected override void OnTurnStarted() {
            StartCoroutine(ChooseNextMoveCoroutine());
        }

#if UNITY_EDITOR
        private void OnValidate() {
            StopAllCoroutines();
            ConfigureProvider();
            if (Board != null && IsTurnActive) {
                StartCoroutine(ChooseNextMoveCoroutine());
            }
        }
#endif

        private IEnumerator ChooseNextMoveCoroutine() {
            Task<Move?> task = Task.Run(GetNextMove); // heavy work off-thread

            while (!task.IsCompleted)
                yield return null; // keep UI responsive

            if (task.IsFaulted) {
                Debug.LogException(task.Exception);
                yield break;
            }

            Move? move = task.Result; // back on main thread here
            if (move.HasValue) ChooseMove(move.Value); // safe to touch Unity API
        }

        private Move? GetNextMove() {
            return _moveProvider.GetNextMove(Board.Clone());
        }
    }
}