using Core;
using UnityEngine;

namespace UI {
    [CreateAssetMenu(menuName = "Theme/Pieces")]
    public class PieceTheme : ScriptableObject {
        public PieceSprites whitePieces;
        public PieceSprites blackPieces;

        public Sprite GetPieceSprite(int piece) {
            PieceSprites pieceSprites = Piece.IsColor(piece, Piece.White) ? whitePieces : blackPieces;
            return pieceSprites[Piece.Type(piece)];
        }

        [System.Serializable]
        public class PieceSprites {
            public Sprite pawn, knight, bishop, rook, queen, king;

            public Sprite this[int i] {
                get { return new Sprite[] { null, pawn, knight, bishop, rook, queen, king }[i]; }
            }
        }
    }
}