using Core;
using UI;
using UnityEngine;

public class BoardSquare : MonoBehaviour {
    // base square
    private MeshRenderer _squareRenderer;
    private Material _squareMat;

    // highlight (overlay quad)
    private MeshRenderer _highlightRenderer;
    private Material _highlightMat;

    // move marker (small dot in center)
    private MeshRenderer _markerRenderer;
    private Material _markerMat;

    public void Init(Coord coord, BoardTheme theme) {
        var shader = Shader.Find("Unlit/Color");
        // base square
        _squareRenderer = gameObject.AddComponent<MeshRenderer>();
        var filter = gameObject.AddComponent<MeshFilter>();
        filter.mesh = CreateQuadMesh();
        _squareMat = new Material(shader);
        _squareMat.color = theme.Normal(coord);
        _squareRenderer.material = _squareMat;

        // highlight quad
        var highlightObj = new GameObject("Highlight");
        highlightObj.transform.SetParent(transform);
        highlightObj.transform.localPosition = new Vector3(0, 0, -0.002f);
        highlightObj.transform.localScale = Vector3.one * 0.95f;

        var highlightFilter = highlightObj.AddComponent<MeshFilter>();
        highlightFilter.mesh = CreateQuadMesh();
        _highlightRenderer = highlightObj.AddComponent<MeshRenderer>();
        _highlightMat = new Material(shader);
        _highlightMat.color = theme.Selected(coord);
        _highlightRenderer.material = _highlightMat;
        _highlightRenderer.enabled = false;

        // move marker circle (procedural mesh)
        var markerObj = new GameObject("MoveMarker");
        markerObj.transform.SetParent(transform);
        markerObj.transform.localPosition = new Vector3(0, 0, -0.2f);
        markerObj.transform.localScale = Vector3.one * 0.25f;

        var markerFilter = markerObj.AddComponent<MeshFilter>();
        markerFilter.mesh = CreateQuadMesh(); 

        _markerRenderer = markerObj.AddComponent<MeshRenderer>();
        _markerMat = new Material(shader);
        _markerMat.color = new Color(0.5f, 0.5f, 0.5f, 0.35f); // semi-transparent gray
        // _markerMat.color = Color.red; // visibility test
        _markerRenderer.material = _markerMat;
        _markerRenderer.enabled = true;
    }
    
    public void SetHighlighted(bool on) {
        _highlightRenderer.enabled = on;
    }

    public void ShowMoveMarker(bool on) {
        _markerRenderer.enabled = on;
    }

    // Helpers
    private static Mesh CreateQuadMesh() {
        var m = new Mesh();
        m.vertices = new[] {
            new Vector3(-0.5f, -0.5f, 0), new Vector3(0.5f, -0.5f, 0),
            new Vector3(-0.5f, 0.5f, 0), new Vector3(0.5f, 0.5f, 0)
        };
        m.triangles = new[] { 0, 2, 1, 2, 3, 1 };
        return m;
    }
}