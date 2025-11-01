using Core;
using UnityEngine;

public class GameManager : MonoBehaviour {
    BoardUI boardUI;
    
    Board board;
    static readonly string StartingPosition = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR";
    
    void Start() {
        board = new Board();
        boardUI = FindFirstObjectByType<BoardUI> ();
        board.LoadFENPosition(StartingPosition);
        boardUI.UpdatePosition(board);
    }
}
