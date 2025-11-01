using System;
using Core;
using UI;
using UnityEngine;
using Utils;

public class BoardUI : MonoBehaviour {
    MeshRenderer[,] squareRenderers;

    SpriteRenderer[,] squarePieceRenderers;
    public BoardTheme boardTheme;
    public PieceTheme pieceTheme;
    float pieceScale = 4.5f;


    void Awake() {
        Debug.Log("Awake");
        CreateBoardUI();
        
    }

    private void CreateBoardUI() {
        Debug.Log("Creating and stuff");
        Shader squareShader = Shader.Find("Unlit/Color");
        squareRenderers = new MeshRenderer[8, 8];
        squarePieceRenderers = new SpriteRenderer[8, 8];
        for (int rank = 0; rank < 8; rank++) {
            for (int file = 0; file < 8; file++) {
                Transform square = GameObject.CreatePrimitive(PrimitiveType.Quad).transform;
                square.parent = transform;
                square.name = BoardUtils.SquareName(file, rank);
                square.position = new Vector3(-4f + file, -4f + rank, 0);

                MeshRenderer squareRenderer = square.gameObject.GetComponent<MeshRenderer>();
                squareRenderer.material = new Material(squareShader);

                SpriteRenderer pieceRenderer = new GameObject("Piece").AddComponent<SpriteRenderer>();
                pieceRenderer.transform.parent = square;
                pieceRenderer.transform.position = square.position;
                pieceRenderer.transform.localScale = Vector3.one / pieceScale;

                squareRenderers[file, rank] = squareRenderer;
                squarePieceRenderers[file, rank] = pieceRenderer;
            }
        }

        Debug.Log("Done");
    }

    public void UpdatePosition(Board board) {
        for (int rank = 0; rank < 8; rank++) {
            for (int file = 0; file < 8; file++) {
                int piece = board.GetSquare(file, rank);
                if (piece == Piece.None) {
                    continue;
                }

                Sprite sprite = pieceTheme.GetPieceSprite(piece);
                squarePieceRenderers[file, rank].sprite = sprite;
            }
        }
    }

    // Update is called once per frame
    void Update() {
        for (int rank = 0; rank < 8; rank++) {
            for (int file = 0; file < 8; file++) {
                MeshRenderer squareRenderer = squareRenderers[file, rank];
                Color color = BoardUtils.IsLightSquare(file, rank)
                    ? boardTheme.lightSquares.normal
                    : boardTheme.darkSquares.normal;
                
                squareRenderer.material.color = color;
            }
        }
    }
}