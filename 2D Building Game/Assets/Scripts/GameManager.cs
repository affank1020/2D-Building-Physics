using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public GameObject[] Pieces;

    public RectTransform toolbox;
    public Vector2 openedPosition;
    public Vector2 closedPosition;
    private float movementSpeed = 200f;

    private bool isToolboxOpen = true;
    private Coroutine movementCoroutine;

    public Button startButton, clearButton, resetButton;
    public GameObject openToolbox;

    public GameObject ItemOptionUI;
    public GameObject toolboxArrow;

    public GameObject curSelectedPiece;
    private bool simActive;

    private List<GameObject> curPieces = new List<GameObject>();

    public GameObject jointPrefab; // Prefab for the joint indicator
    private List<GameObject> jointIndicators = new List<GameObject>(); // List to store the joint indicator game objects
    private List<Joint2D> joints = new List<Joint2D>();

    void Start()
    {
        // Set the initial position of the toolbox
        toolbox.anchoredPosition = openedPosition;

        //Adding items to toolbox
        int counter = 50;
        foreach (GameObject piece in Pieces)
        {
            GameObject curItemOption = Instantiate(ItemOptionUI, new Vector3(counter, 50, 0), Quaternion.identity, toolbox);
            curItemOption.GetComponent<Button>().onClick.AddListener(() => SpawnPiece(piece));
            Image itemImage = Instantiate(piece.GetComponent<PieceController>().myImage, curItemOption.GetComponent<RectTransform>());
            counter += 100;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
        
    }



    private GameObject GetMouseOverObject()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit2D hit = Physics2D.GetRayIntersection(ray);

        if (hit.collider != null)
        {
            return hit.collider.gameObject;
        }

        return null;
    }

    public void EnablePieces()
    {
        startButton.gameObject.SetActive(false);
        clearButton.gameObject.SetActive(false);
        resetButton.gameObject.SetActive(true);

        simActive = true;

        if (curSelectedPiece)
        {
            curSelectedPiece.GetComponent<PieceController>().DeselectPiece();
        }

        if (isToolboxOpen)
        {
            ToggleToolbox();
            openToolbox.SetActive(false);
        }

        foreach (GameObject piece in curPieces)
        {
            piece.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Dynamic;
            piece.tag = "ActivePiece";
            PieceController pieceController = piece.GetComponent<PieceController>();
            pieceController.SaveMemory();
            pieceController.myHandle.SetActive(false);
        }
    }

    public void ResetPieces()
    {
        startButton.gameObject.SetActive(true);
        clearButton.gameObject.SetActive(true);
        resetButton.gameObject.SetActive(false);

        simActive = false;

        ToggleToolbox();
        openToolbox.SetActive(true);

        foreach (GameObject piece in curPieces)
        {
            piece.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Static;
            piece.tag = "Piece";
            PieceController pieceController = piece.GetComponent<PieceController>();
            pieceController.LoadMemory();
            pieceController.myHandle.SetActive(true);
        }
    }

    public void ClearPieces()
    {
        foreach (GameObject piece in curPieces)
        {
            Destroy(piece);
        }
        curPieces.Clear();
    }

    public void ToggleToolbox()
    {
        if (movementCoroutine != null)
        {
            StopCoroutine(movementCoroutine);
        }

        toolboxArrow.transform.Rotate(Vector3.forward, 180);

        if (isToolboxOpen)
        {
            movementCoroutine = StartCoroutine(MoveToolbox(closedPosition));
        }
        else
        {
            movementCoroutine = StartCoroutine(MoveToolbox(openedPosition));
        }

        isToolboxOpen = !isToolboxOpen;
    }

    private IEnumerator MoveToolbox(Vector2 targetPosition)
    {
        while (Vector2.Distance(toolbox.anchoredPosition, targetPosition) > 0.01f)
        {
            toolbox.anchoredPosition = Vector2.MoveTowards(toolbox.anchoredPosition, targetPosition, movementSpeed * Time.deltaTime);
            yield return null;
        }
    }

    public void SpawnPiece(GameObject piece)
    {
        Vector3 spawnPos = new Vector3(0, 0, 0);
        GameObject newPiece = Instantiate(piece, spawnPos, Quaternion.identity);
        curPieces.Add(newPiece);
    }

    public void RemoveConnections()
    {
        if (!simActive)
        {
            // Remove all joints from the piece
            foreach (HingeJoint2D joint in joints)
            {
                Destroy(joint);
            }
            joints.Clear();

            // Destroy all joint indicators
            foreach (GameObject jointIndicator in jointIndicators)
            {
                Destroy(jointIndicator);
            }

            jointIndicators.Clear();
        }
    }

    public void CheckAndCreateConnections()
    {
        if (!simActive)
        {
            foreach (GameObject piece in curPieces)
            {
                List<Collider2D> overlappingColliders = new List<Collider2D>();
                Physics2D.OverlapCollider(piece.GetComponent<Collider2D>(), new ContactFilter2D(), overlappingColliders);

                //Collider2D[] overlappingColliders = Physics2D.OverlapBoxAll(piece.transform.position, piece.transform.localScale, piece.transform.rotation.eulerAngles.z);

                foreach (Collider2D collider in overlappingColliders)
                {
                    if (collider.gameObject != piece.gameObject && (collider.gameObject.CompareTag("Piece") || collider.gameObject.CompareTag("ActivePiece")))
                    {
                        // Create a connection between this piece and the overlapping piece
                        ConnectPieces(piece, collider.gameObject);
                    }
                }
            }
        }   
    }

    private void ConnectPieces(GameObject onePiece, GameObject otherPiece)
    {
        Vector3 overlapPoint;
        if (FindOverlapPoint(onePiece, otherPiece, 0.55f, out overlapPoint))
        {
            // Instantiate a joint indicator at the overlap point
            GameObject jointIndicator = Instantiate(jointPrefab, overlapPoint, Quaternion.identity);
            jointIndicator.transform.parent = onePiece.transform;
            jointIndicators.Add(jointIndicator);


            // Create a joint between the two pieces
            HingeJoint2D joint = onePiece.AddComponent<HingeJoint2D>();
            joint.connectedBody = otherPiece.GetComponent<Rigidbody2D>();
            joints.Add(joint);
        }
    }

    private bool FindOverlapPoint(GameObject onePiece, GameObject otherPiece, float tolerance, out Vector3 centerPoint)
    {
        // Get the colliders of both pieces
        Collider2D thisCollider = onePiece.GetComponent<Collider2D>();
        Collider2D otherCollider = otherPiece.GetComponent<Collider2D>();

        // Calculate the overlapping bounds in world space
        Bounds thisBounds = thisCollider.bounds;
        Bounds otherBounds = otherCollider.bounds;

        // Calculate the extents of the overlapping area
        float minX = Mathf.Max(thisBounds.min.x, otherBounds.min.x);
        float minY = Mathf.Max(thisBounds.min.y, otherBounds.min.y);
        float maxX = Mathf.Min(thisBounds.max.x, otherBounds.max.x);
        float maxY = Mathf.Min(thisBounds.max.y, otherBounds.max.y);
        // Calculate the size of the overlapping area
        float width = maxX - minX;
        float height = maxY - minY;

        // Check if the overlapping area is smaller than the tolerance
        if (width < tolerance || height < tolerance)
        {
            centerPoint = Vector3.zero;
            return false;
        }

        // Calculate the center point of the overlapping area
        centerPoint = new Vector3((minX + maxX) / 2f, (minY + maxY) / 2f, 0f);
        return true;
    }

    //private bool AreRectanglesAcute(GameObject onePiece, GameObject otherPiece)
    //{
    //    // Get the transforms of both pieces
    //    Transform thisTransform = onePiece.transform;
    //    Transform otherTransform = otherPiece.transform;

    //    // Calculate the direction vector from the center of one rectangle to the other
    //    Vector3 direction = otherTransform.position - thisTransform.position;

    //    // Calculate the angle between the direction vector and the forward vector of the first rectangle
    //    float angle = Vector3.Angle(direction, thisTransform.right);

    //    // Check if the angle is acute (less than 90 degrees)
    //    return angle < 60f;
    //}


    //public bool FindOverlapPoint(GameObject onePiece, GameObject otherPiece, out Vector2 overlapPoint)
    //{
    //    // Get the necessary information from the game objects
    //    Vector2 pos0 = onePiece.transform.position;
    //    float width0 = GetWidth(onePiece);
    //    float height0 = GetHeight(onePiece);
    //    float rotation0 = GetRotation(onePiece);

    //    Vector2 pos1 = otherPiece.transform.position;
    //    float width1 = GetWidth(otherPiece);
    //    float height1 = GetHeight(otherPiece);
    //    float rotation1 = GetRotation(otherPiece);

    //    // Calculate the overlapping point using the collision math
    //    if (CollisionMath.RectIntersect(pos0, width0, height0, rotation0, pos1, width1, height1, rotation1, 0.001f, out overlapPoint))
    //    {
    //        Debug.Log(overlapPoint);
    //        return true;
    //    }
    //    Debug.Log(overlapPoint);
    //    return false;
    //}

    private float GetWidth(GameObject obj)
    {
        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer != null)
        {
            return renderer.bounds.size.x;
        }

        Collider2D collider2D = obj.GetComponent<Collider2D>();
        if (collider2D != null)
        {
            return collider2D.bounds.size.x;
        }

        return 0f;
    }

    private float GetHeight(GameObject obj)
    {
        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer != null)
        {
            return renderer.bounds.size.y;
        }

        Collider2D collider2D = obj.GetComponent<Collider2D>();
        if (collider2D != null)
        {
            return collider2D.bounds.size.y;
        }

        return 0f;
    }

    private float GetRotation(GameObject obj)
    {
        return obj.transform.eulerAngles.z;
    }


}

// CollisionMath
public class CollisionMath
{
    public static bool RectIntersect(Vector2 pos0, float w0, float h0, float r0, Vector2 pos1, float w1, float h1, float r1, float tolerance, out Vector2 overlapPoint)
    {
        Vector2 vector = Rot2Vect(r0 + 90f) * (h0 * 0.5f);
        Vector2 a = pos0 + vector;
        Vector2 b = pos0 - vector;

        Vector2 vector2 = Rot2Vect(r1 + 90f) * (h1 * 0.5f);
        Vector2 c = pos1 + vector2;
        Vector2 d = pos1 - vector2;

        if (Intersect(a, b, c, d, out overlapPoint))
        {
            return true;
        }

        Vector2[] rectPointsA = new Vector2[4];
        Vector2[] rectPointsB = new Vector2[4];

        Vector2 rectEdgePointA1, rectEdgePointA2;
        Vector2 rectEdgePointB1, rectEdgePointB2;

        GetRectEdgePoints(pos0, h0, r0, out rectEdgePointA1, out rectEdgePointA2);
        GetRectEdgePoints(pos1, h1, r1, out rectEdgePointB1, out rectEdgePointB2);

        rectPointsA[0] = pos0 + vector + (Rot2Vect(r0) * (w0 * 0.5f));
        rectPointsA[1] = pos0 - vector + (Rot2Vect(r0) * (w0 * 0.5f));
        rectPointsA[2] = pos0 - vector - (Rot2Vect(r0) * (w0 * 0.5f));
        rectPointsA[3] = pos0 + vector - (Rot2Vect(r0) * (w0 * 0.5f));

        rectPointsB[0] = pos1 + vector2 + (Rot2Vect(r1) * (w1 * 0.5f));
        rectPointsB[1] = pos1 - vector2 + (Rot2Vect(r1) * (w1 * 0.5f));
        rectPointsB[2] = pos1 - vector2 - (Rot2Vect(r1) * (w1 * 0.5f));
        rectPointsB[3] = pos1 + vector2 - (Rot2Vect(r1) * (w1 * 0.5f));

        List<Vector2> intersectPoints = new List<Vector2>();

        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                if (Intersect(rectPointsA[i], rectPointsA[(i + 1) % 4], rectPointsB[j], rectPointsB[(j + 1) % 4], out var intersection))
                {
                    intersectPoints.Add(intersection);
                }
            }
        }

        for (int k = 0; k < 4; k++)
        {
            if (InsideRect(rectPointsA[0], rectPointsA[1], rectPointsA[2], rectPointsA[3], rectPointsB[k]))
            {
                intersectPoints.Add(rectPointsB[k]);
            }
        }

        for (int l = 0; l < 4; l++)
        {
            if (InsideRect(rectPointsB[0], rectPointsB[1], rectPointsB[2], rectPointsB[3], rectPointsA[l]))
            {
                intersectPoints.Add(rectPointsA[l]);
            }
        }

        if (intersectPoints.Count == 0)
        {
            return false;
        }

        overlapPoint = intersectPoints[0];

        for (int m = 1; m < intersectPoints.Count; m++)
        {
            if (Vector2.Distance(pos0, intersectPoints[m]) < Vector2.Distance(pos0, overlapPoint))
            {
                overlapPoint = intersectPoints[m];
            }
        }

        if (tolerance > 0 && Vector2.Distance(overlapPoint, pos0) > tolerance)
        {
            return false;
        }

        return true;
    }


    private static Vector2 Rot2Vect(float rot)
    {
        return new Vector2(Mathf.Cos((float)Math.PI / 180f * rot), Mathf.Sin((float)Math.PI / 180f * rot));
    }

    private static bool Intersect(Vector2 A, Vector2 B, Vector2 C, Vector2 D, out Vector2 interPoint)
    {
        interPoint = default(Vector2);
        float num = B.y - A.y;
        float num2 = A.x - B.x;
        float num3 = num * A.x + num2 * A.y;
        float num4 = D.y - C.y;
        float num5 = C.x - D.x;
        float num6 = num4 * C.x + num5 * C.y;
        float num7 = num * num5 - num4 * num2;
        if (num7 == 0f)
        {
            return false;
        }
        interPoint.x = (num5 * num3 - num2 * num6) / num7;
        interPoint.y = (num * num6 - num4 * num3) / num7;
        if (Mathf.Min(A.x, B.x) <= interPoint.x + 0.0001f && Mathf.Max(A.x, B.x) >= interPoint.x - 0.0001f && Mathf.Min(A.y, B.y) <= interPoint.y && Mathf.Max(A.y, B.y) >= interPoint.y && Mathf.Min(C.x, D.x) <= interPoint.x + 0.0001f && Mathf.Max(C.x, D.x) >= interPoint.x - 0.0001f && Mathf.Min(C.y, D.y) <= interPoint.y)
        {
            return Mathf.Max(C.y, D.y) >= interPoint.y;
        }
        return false;
    }

    public static bool InsideRect(Vector2 A, Vector2 B, Vector2 C, Vector2 D, Vector2 M)
    {
        float num = Vector2.Dot(M - A, B - A);
        float num2 = Vector2.Dot(M - A, D - A);
        if (0f < num && num < Vector2.Dot(B - A, B - A))
        {
            if (0f < num2)
            {
                return num2 < Vector2.Dot(D - A, D - A);
            }
            return false;
        }
        return false;
    }

    private static void GetRectEdgePoints(Vector2 pos, float height, float rot, out Vector2 edgePoint1, out Vector2 edgePoint2)
    {
        Vector2 vector = height * 0.5f * Rot2Vect(rot + 90f);
        edgePoint1 = pos + vector;
        edgePoint2 = pos - vector;
    }

    public static bool RectCircleIntersectionPoint(Vector2 rectPos, float rectHeight, float rectRot, Vector2 circlePos, float radius, out Vector2 interPoint)
    {
        GetRectEdgePoints(rectPos, rectHeight, rectRot, out var edgePoint, out var edgePoint2);
        Vector2 intersection;
        Vector2 intersection2;
        switch (FindLineCircleIntersections(circlePos.x, circlePos.y, radius, edgePoint, edgePoint2, out intersection, out intersection2))
        {
            case 0:
                interPoint = Vector2.zero;
                return false;
            case 1:
                if (Vector2.Distance(rectPos, intersection) > rectHeight * 0.5f)
                {
                    interPoint = Vector2.zero;
                    return false;
                }
                interPoint = intersection;
                return true;
            default:
                {
                    bool flag = Vector2.Distance(edgePoint, circlePos) <= radius;
                    bool flag2 = Vector2.Distance(edgePoint2, circlePos) <= radius;
                    if (flag && flag2)
                    {
                        interPoint = rectPos;
                        return true;
                    }
                    float num = Vector2.Distance(rectPos, intersection);
                    float num2 = Vector2.Distance(rectPos, intersection2);
                    if (!flag && !flag2)
                    {
                        if (num > rectHeight * 0.5f && num2 > rectHeight * 0.5f)
                        {
                            interPoint = Vector2.zero;
                            return false;
                        }
                        interPoint = (intersection + intersection2) / 2f;
                        return true;
                    }
                    Vector2 vector = ((num < num2) ? intersection : intersection2);
                    if (flag && !flag2)
                    {
                        interPoint = (vector + edgePoint) / 2f;
                        return true;
                    }
                    if (flag2 && !flag)
                    {
                        interPoint = (vector + edgePoint2) / 2f;
                        return true;
                    }
                    interPoint = Vector2.zero;
                    return false;
                }
        }
    }

    public static int FindLineCircleIntersections(float cx, float cy, float radius, Vector2 point1, Vector2 point2, out Vector2 intersection1, out Vector2 intersection2)
    {
        float num = point2.x - point1.x;
        float num2 = point2.y - point1.y;
        float num3 = num * num + num2 * num2;
        float num4 = 2f * (num * (point1.x - cx) + num2 * (point1.y - cy));
        float num5 = (point1.x - cx) * (point1.x - cx) + (point1.y - cy) * (point1.y - cy) - radius * radius;
        float num6 = num4 * num4 - 4f * num3 * num5;
        if ((double)num3 <= 1E-07 || num6 < 0f)
        {
            intersection1 = new Vector2(float.NaN, float.NaN);
            intersection2 = new Vector2(float.NaN, float.NaN);
            return 0;
        }
        float num7;
        if (num6 == 0f)
        {
            num7 = (0f - num4) / (2f * num3);
            intersection1 = new Vector2(point1.x + num7 * num, point1.y + num7 * num2);
            intersection2 = new Vector2(float.NaN, float.NaN);
            return 1;
        }
        num7 = (0f - num4 + Mathf.Sqrt(num6)) / (2f * num3);
        intersection1 = new Vector2(point1.x + num7 * num, point1.y + num7 * num2);
        num7 = (0f - num4 - Mathf.Sqrt(num6)) / (2f * num3);
        intersection2 = new Vector2(point1.x + num7 * num, point1.y + num7 * num2);
        return 2;
    }
}
