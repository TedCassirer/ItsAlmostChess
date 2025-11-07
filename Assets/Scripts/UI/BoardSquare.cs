using Core;
using UnityEngine;

namespace UI {
    public class BoardSquare : MonoBehaviour {
        private MeshRenderer _square, _highlight, _moveMarker;
        private Coord _coord;

        public static BoardSquare Create(GameObject parent, Coord coord, BoardTheme boardTheme) {
            var square = parent.AddComponent<BoardSquare>();
            square.tag = "BoardSquare";
            square.Init(coord, boardTheme);
            square.ApplyTheme(boardTheme);
            return square;
        }

        private void Init(Coord coord, BoardTheme theme) {
            _coord = coord;

            Shader shader = Shader.Find("Unlit/Color");
            // base square
            var square = GameObject.CreatePrimitive(PrimitiveType.Quad);
            square.name = "Square";
            square.transform.SetParent(transform, false);
            _square = square.GetComponent<MeshRenderer>();
            _square.sharedMaterial = new Material(theme.shader) {
                color = theme.Normal(coord)
            };

            // highlight quad
            var highlightObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
            highlightObj.name = "Highlight";
            highlightObj.transform.SetParent(transform, false);
            highlightObj.transform.localPosition = new Vector3(0, 0, -0.002f);
            _highlight = highlightObj.GetComponent<MeshRenderer>();
            _highlight.sharedMaterial = new Material(theme.shader) {
                color = theme.Highlighted(coord)
            };
            _highlight.enabled = false;

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

        public void ApplyTheme(BoardTheme theme) {
            if (_square == null || _highlight == null) return;
            _square.sharedMaterial = new Material(theme.shader) {
                color = theme.Normal(_coord)
            };

            _highlight.sharedMaterial = new Material(theme.shader) {
                color = theme.Highlighted(_coord)
            };
        }

        public void SetHighlighted(bool on) {
            _highlight.enabled = on;
        }

        public void ShowMoveMarker(bool on) {
            _moveMarker.enabled = on;
        }
    }
}