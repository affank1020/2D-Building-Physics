using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PieceRotation : MonoBehaviour
{
    public PieceController pieceController;
    public bool isRotating = false;
    public Vector3 previousMousePosition;
    public Vector3 handlePos;
    GameManager manager;

    private void Start()
    {
        pieceController = GetComponentInParent<PieceController>();
        gameObject.tag = "Handle";
        gameObject.GetComponent<SpriteRenderer>().sortingOrder = 15;
        manager = GameObject.Find("Game Manager").GetComponent<GameManager>();
    }

    private void OnMouseDown()
    {
        isRotating = true;
        previousMousePosition = GetMouseWorldPosition();
        pieceController.isHandleClicked = true; // Set the handle clicked flag in PieceController
        manager.RemoveConnections();
    }

    private void OnMouseUp()
    {
        isRotating = false;
        pieceController.isHandleClicked = false; // Reset the handle clicked flag in PieceController
        //pieceController.CheckAndCreateConnections();
        manager.CheckAndCreateConnections();
    }

    private void Update()
    {
        if (isRotating)
        {
            Vector3 currentMousePosition = GetMouseWorldPosition();

            // Rotate the piece around the handle based on the mouse movement
            pieceController.RotateAroundHandle(previousMousePosition, currentMousePosition);

            previousMousePosition = currentMousePosition;
        }
        transform.localPosition = handlePos;
    }

    private Vector3 GetMouseWorldPosition()
    {
        // Get the mouse position in screen space
        Vector3 mousePositionScreen = Input.mousePosition;

        // Create a ray from the camera to the mouse position
        Ray ray = Camera.main.ScreenPointToRay(mousePositionScreen);

        RaycastHit hit;
        if (Physics.Raycast(ray, out hit))
        {
            // If the raycast hits something, return the hit point
            return hit.point;
        }

        // If no hit point is found, return the mouse position projected onto a plane at z = 0
        Plane plane = new Plane(Vector3.forward, Vector3.zero);
        float distance;
        if (plane.Raycast(ray, out distance))
        {
            return ray.GetPoint(distance);
        }

        // If all else fails, return Vector3.zero
        return Vector3.zero;
    }

}
