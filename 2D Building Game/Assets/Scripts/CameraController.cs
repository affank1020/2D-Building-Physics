using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public float minX;
    public float maxX;
    public float minY;
    public float maxY;
    public float zoomSpeed = 1f;
    public float minZoom = 1f;
    public float maxZoom = 10f;

    private float dist;
    private Vector3 mouseStart;
    private bool movingObject;

    void Start()
    {
        dist = transform.position.z;  // Distance camera is above map
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2 mousePos2D = new Vector2(mousePos.x, mousePos.y);

            RaycastHit2D hit = Physics2D.Raycast(mousePos2D, Vector2.zero);
            if (hit.collider != null)
            {
                if (hit.collider.gameObject.CompareTag("Piece") || hit.collider.gameObject.CompareTag("Handle"))
                {
                    movingObject = true;
                }
                else
                {
                    movingObject = false;
                    mouseStart = GetWorldMousePosition();
                }
            }
            else
            {
                movingObject = false;
                mouseStart = GetWorldMousePosition();
            }
        }
        else if (Input.GetMouseButton(0) && !movingObject)
        {
            Vector3 mouseMove = GetWorldMousePosition();
            Vector3 newPosition = transform.position - (mouseMove - mouseStart);
            newPosition.x = Mathf.Clamp(newPosition.x, minX, maxX);
            newPosition.y = Mathf.Clamp(newPosition.y, minY, maxY);
            transform.position = newPosition;
        }

        // Zoom functionality
        float zoomAmount = Input.GetAxis("Mouse ScrollWheel") * zoomSpeed;
        float newZoom = Camera.main.orthographicSize - zoomAmount;
        Camera.main.orthographicSize = Mathf.Clamp(newZoom, minZoom, maxZoom);
    }

    Vector3 GetWorldMousePosition()
    {
        Vector3 mousePos = new Vector3(Input.mousePosition.x, Input.mousePosition.y, dist);
        return Camera.main.ScreenToWorldPoint(mousePos);
    }
}
