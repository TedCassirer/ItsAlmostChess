using Core;
using UnityEngine;

namespace UI {
    [CreateAssetMenu(menuName = "Theme/Board")]
    public class BoardTheme : ScriptableObject {
        public SquareColours lightSquares;
        public SquareColours darkSquares;

        [System.Serializable]
        public struct SquareColours {
            public Color normal;
            public Color legal;
            public Color selected;
            public Color moveFromHighlight;
            public Color moveToHighlight;
        }

        public Color Normal(Coord coord) {
            return coord.IsLightSquare() ? lightSquares.normal : darkSquares.normal;
        }

        public Color Selected(Coord coord) {
            return coord.IsLightSquare() ? lightSquares.selected : darkSquares.selected;
        }
    }
}