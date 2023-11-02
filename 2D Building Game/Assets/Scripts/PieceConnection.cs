using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PieceConnection
{
    private GameObject firstPiece;
    private GameObject secondPiece;

    public PieceConnection New(GameObject fP, GameObject sP)
    {
        firstPiece = fP;
        secondPiece = sP;
        return this;
    }

    public GameObject[] GetConnectedPieces()
    {
        GameObject[] pieces = new GameObject[] { firstPiece, secondPiece };
        return pieces;
    }
}
