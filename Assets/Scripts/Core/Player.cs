using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Core {
    public abstract class Player: MonoBehaviour {
        public event System.Action<Move?> OnMoveChosen;
        
        protected Board Board;


        public abstract void Init(Board board);
        
        public abstract void Update();

        protected void ChooseMove(Move move) {
            OnMoveChosen?.Invoke(move);
        }

        public abstract void NotifyTurnToPlay();
        
        public bool IsHuman =>  this is Human;
        public bool IsAI =>  this is AI.AIPlayer;
    }
}