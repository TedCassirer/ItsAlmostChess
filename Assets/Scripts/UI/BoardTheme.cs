using System;
using Core;
using UnityEngine;
using UnityEngine.Serialization;

namespace UI {
    [CreateAssetMenu(menuName = "Theme/Board")]
    public class BoardTheme : ScriptableObject {
        public Shader shader;
        public SquareColours lightSquares, darkSquares;

        public event Action Changed;

        [Serializable]
        public struct SquareColours {
            public Color normal;
            [FormerlySerializedAs("selected")] public Color highlighted;
            public Color moveIndicator;
        }

        public Color Normal(Coord c) => c.IsLightSquare() ? lightSquares.normal : darkSquares.normal;
        public Color Highlighted(Coord c) => c.IsLightSquare() ? lightSquares.highlighted : darkSquares.highlighted;

#if UNITY_EDITOR
        void OnValidate() {
            // fires when values change in inspector
            Changed?.Invoke();
        }
#endif
    }
}