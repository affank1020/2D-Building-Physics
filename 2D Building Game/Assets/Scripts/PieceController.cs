using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PieceController : MonoBehaviour
{
    // The plane the object is currently being dragged on
    private Plane dragPlane;

    // The difference between where the mouse is on the drag plane and 
    // where the origin of the object is on the drag plane
    private Vector3 offset;

    private Camera myMainCamera;
    public Image myImage;
    public Vector3 positionMemory;
    public Quaternion rotationMemory;

    public GameObject myHandle;
    private GameManager gameManager;
    public Vector3 NormalSize;

    public Transform rotationHandle;
    public float rotationSpeed = 10f;

    private bool isDragging = false;
    private Vector3 previousMousePosition;
    public bool isHandleClicked;
    private Rigidbody2D rb;

    public GameObject jointPrefab; // Prefab for the joint indicator
    private List<GameObject> jointIndicators = new List<GameObject>(); // List to store the joint indicator game objects

    void Start()
    {
        myMainCamera = Camera.main; // Camera.main is expensive ; cache it here
        gameManager = GameObject.Find("Game Manager").GetComponent<GameManager>();
        rotationHandle = myHandle.transform.GetChild(0);
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {

    }

    public void RotateAroundHandle(Vector3 prevPos, Vector3 newPos)
    {
        Vector3 mouseDelta = newPos - prevPos;


        // Calculate the angle between the previous mouse position and the new mouse position
        float angle = Vector3.SignedAngle(prevPos - transform.position, newPos - transform.position, Vector3.forward);

        // Apply the rotation to the object around its center
        transform.Rotate(Vector3.forward, angle);
    }


    void OnMouseDown()
    {
        gameManager.RemoveConnections();
        if (!gameObject.CompareTag("ActivePiece") && !gameObject.CompareTag("Handle"))
        {
            dragPlane = new Plane(myMainCamera.transform.forward, transform.position);
            Ray camRay = myMainCamera.ScreenPointToRay(Input.mousePosition);

            float planeDist;
            dragPlane.Raycast(camRay, out planeDist);
            offset = transform.position - camRay.GetPoint(planeDist);

            //Selected
            myHandle.transform.localScale = NormalSize;
            if (gameManager.curSelectedPiece && gameManager.curSelectedPiece != gameObject)
            {
                gameManager.curSelectedPiece.GetComponent<PieceController>().DeselectPiece();
            }
            gameManager.curSelectedPiece = gameObject;
            gameObject.GetComponent<SpriteRenderer>().sortingOrder = 16;
        }
        
    }

    public void DeselectPiece()
    {
        myHandle.transform.localScale = new Vector3(0, 0, 0);
        gameObject.GetComponent<SpriteRenderer>().sortingOrder = 14;
    }

    public void SaveMemory()
    {
        positionMemory = transform.position;
        rotationMemory = transform.rotation;
    }

    public void LoadMemory()
    {
        transform.position = positionMemory;
        transform.rotation = rotationMemory;
    }

    public void DestroyPiece()
    {
        GameObject.Destroy(this);
    }

    void OnMouseDrag()
    {
        if (!gameObject.CompareTag("ActivePiece"))
        {
            Ray camRay = myMainCamera.ScreenPointToRay(Input.mousePosition);

            float planeDist;
            dragPlane.Raycast(camRay, out planeDist);
            transform.position = camRay.GetPoint(planeDist) + offset;
        }
    }

    private void OnMouseUp()
    {
        gameManager.CheckAndCreateConnections();
    }
}
