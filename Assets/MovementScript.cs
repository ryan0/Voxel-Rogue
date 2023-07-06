using System.Collections;
using UnityEngine;

public class VoxelBasedMovement : MonoBehaviour
{
    public float maxMoveDiff = 1.0f;
    public float moveSpeed = 3.0f;
    public float gravity = 9.8f;
    public float jumpForce = 5.0f;

    private World world;
    private float verticalSpeed = 0.0f;
    private bool isMoving = false;

    private void Start()
    {
        world = FindObjectOfType<World>();
    }

    private void Update()
    {
        if (isMoving) return;

        Vector3 moveDirection = Vector3.zero;

        Vector3 cameraForward = Camera.main.transform.forward;
        Vector3 cameraRight = Camera.main.transform.right;

        cameraForward.y = 0; // Keep only the horizontal component
        cameraRight.y = 0; // Keep only the horizontal component

        cameraForward.Normalize();
        cameraRight.Normalize();

        if (Input.GetKey(KeyCode.W))
        {
            moveDirection += cameraForward;
        }
        else if (Input.GetKey(KeyCode.S))
        {
            moveDirection -= cameraForward;
        }
        else if (Input.GetKey(KeyCode.A))
        {
            moveDirection -= cameraRight;
        }
        else if (Input.GetKey(KeyCode.D))
        {
            moveDirection += cameraRight;
        }

        if (moveDirection != Vector3.zero)
        {
            Move(moveDirection.normalized);
        }

        // Handle gravity
        bool isGrounded = IsGrounded();
        if (!isGrounded)
        {
            verticalSpeed -= gravity * Time.deltaTime;
        }
        else
        {
            verticalSpeed = 0;

            // Jump logic
            if (Input.GetKeyDown(KeyCode.Space))
            {
                verticalSpeed = jumpForce;
            }
        }

        // Apply vertical movement
        Vector3 verticalMove = new Vector3(0, verticalSpeed * Time.deltaTime, 0);
        transform.position += verticalMove;
    }

    private void Move(Vector3 direction)
    {
        Vector3Int currentVoxelCoord = world.WorldCoordToVoxelCoord(transform.position);
        Vector3Int targetVoxelCoord = currentVoxelCoord + Vector3Int.RoundToInt(direction);

        int heightDifference = targetVoxelCoord.y - currentVoxelCoord.y;

        if (Mathf.Abs(heightDifference) <= maxMoveDiff)
        {
            Substance voxelSub = world.GetVoxelType(targetVoxelCoord);
            State voxelType = voxelSub.state;

            if (voxelType != State.SOLID)
            {
                Debug.Log("currentVoxelCoord " + currentVoxelCoord.x + "  " + currentVoxelCoord.y + " " +currentVoxelCoord.z);
                Debug.Log("targetVoxelCoord " + targetVoxelCoord.x + " " + targetVoxelCoord.y + " " + targetVoxelCoord.z);
                Vector3 heightAdjustment = new Vector3(0, heightDifference * Voxel.size, 0);
                Vector3 targetWorldPosition = world.VoxelCoordToWorldCoord(targetVoxelCoord) + heightAdjustment;
                StartCoroutine(SmoothMove(transform.position, targetWorldPosition));
            }
        }
    }

    IEnumerator SmoothMove(Vector3 startpos, Vector3 endpos)
    {
        isMoving = true;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * moveSpeed;
            transform.position = Vector3.Lerp(startpos, endpos, t);
            yield return null;
        }
        isMoving = false;
    }

    private bool IsGrounded()
    {
        Vector3Int currentVoxelCoord = world.WorldCoordToVoxelCoord(transform.position);

        // Check the voxel directly below the player
        Vector3Int belowVoxelCoord = new Vector3Int(currentVoxelCoord.x, currentVoxelCoord.y - 1, currentVoxelCoord.z);

        // Check if the voxel below is solid
        State voxelType = world.GetVoxelType(belowVoxelCoord).state;

        return voxelType == State.SOLID;
    }
}
