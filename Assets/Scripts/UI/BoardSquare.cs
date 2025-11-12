using Core;
using UnityEditor.UI;
using UnityEngine;

namespace UI {
    public class BoardSquare : MonoBehaviour {
        private MeshRenderer _square, _highlight, _moveMarker;
        private BoardTheme.SquareColours _squareColors;

        public static BoardSquare Create(GameObject parent, Coord coord, BoardTheme boardTheme) {
            var square = parent.AddComponent<BoardSquare>();
            square.tag = "BoardSquare";
            var squareColors = coord.IsLightSquare() ? boardTheme.lightSquares : boardTheme.darkSquares;
            square.Init(coord, squareColors);
            square.NormalColor();
            return square;
        }

        private void Init(Coord coord, BoardTheme.SquareColours squareColors) {
            _squareColors = squareColors;

            var shader = Shader.Find("Unlit/Color");
            // base square
            var square = GameObject.CreatePrimitive(PrimitiveType.Quad);
            square.name = "Square";
            square.transform.SetParent(transform, false);
            _square = square.GetComponent<MeshRenderer>();
            _square.sharedMaterial = new Material(shader) {
                color = squareColors.normal
            };

            // move marker circle 
            var marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            marker.name = "Move Marker";
            marker.transform.SetParent(transform, false);
            marker.transform.localPosition = new Vector3(0, 0, -0.2f);
            marker.transform.localScale = new Vector3(0.3f, 0.3f, 0f);
            _moveMarker = marker.GetComponent<MeshRenderer>();
            _moveMarker.sharedMaterial = new Material(shader) {
                color = new Color(0.5f, 0.5f, 0.5f, 0.3f)
            };
            _moveMarker.enabled = false;
        }
        
        public void HighlightColor() {
            _square.sharedMaterial.color = _squareColors.highlighted;
        }
        
        public void NormalColor() {
            _square.sharedMaterial.color = _squareColors.normal;
        }
        
        public void MoveIndicatorColor() {
            _square.sharedMaterial.color = _squareColors.moveIndicator;
        }
        
        public void ShowMoveMarker(bool on) => _moveMarker.enabled = on;
    }
}