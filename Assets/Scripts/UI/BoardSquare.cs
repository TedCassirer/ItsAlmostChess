using Core;
using UI;
using UnityEngine;

public class BoardSquare : MonoBehaviour {
    // base square
    private MeshRenderer _square;

    // highlight (overlay quad)
    private MeshRenderer _highlight;

    // move marker (small dot in center)
    private MeshRenderer _moveMarker;

    public void Init(Coord coord, BoardTheme theme) {
        var shader = Shader.Find("Unlit/Color");
        // base square
        var square = GameObject.CreatePrimitive(PrimitiveType.Quad);
        square.name = "Square";
        square.transform.SetParent(transform, false);
        _square = square.GetComponent<MeshRenderer>();
        _square.material = new Material(shader) {
            color = theme.Normal(coord)
        };

        // highlight quad
        var highlightObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
        highlightObj.name = "Highlight";
        highlightObj.transform.SetParent(transform, false);
        highlightObj.transform.localPosition = new Vector3(0, 0, -0.002f);
        _highlight = highlightObj.GetComponent<MeshRenderer>();
        _highlight.sharedMaterial = new Material(shader) {
            color = theme.Selected(coord)
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

    public void SetHighlighted(bool on) {
        _highlight.enabled = on;
    }

    public void ShowMoveMarker(bool on) {
        _moveMarker.enabled = on;
    }
}