using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

namespace Core.AI {
    public class AIPlayer : Player {
        private IMoveProvider _moveProvider;

        protected override void OnInitialized() {
            _moveProvider = new MiniMaxV2(Board);
        }

        protected override void OnTurnStarted() {
            StartCoroutine(ChooseNextMoveCoroutine());
        }

        private IEnumerator ChooseNextMoveCoroutine() {
            var task = Task.Run(GetNextMove); // heavy work off-thread

            while (!task.IsCompleted)
                yield return null; // keep UI responsive

            if (task.IsFaulted) {
                Debug.LogException(task.Exception);
                yield break;
            }

            var move = task.Result; // back on main thread here
            if (move.HasValue) ChooseMove(move.Value); // safe to touch Unity API
        }

        private Move? GetNextMove() {
            return _moveProvider.GetNextMove();
        }
    }
}