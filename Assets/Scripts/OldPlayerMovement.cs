using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
public class OldPlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float maxSpeed = 3.4f;
    public float jumpHeight = 5.0f;
    public float gravityScale = 1.5f;
    public int airJumpsMax = 2;
    public float wallJumpForce = 5.0f; // Add a new variable for wall jump force
    public float wallJumpDelay = 0.2f; // Delay after wall jump
    public float wallSlideSpeed = 1.0f; // Speed at which the player slides down when wall clinging
    public float wallClingBuffer = 0.5f; // Buffer time before sliding down
    public float currentSpeed; // Variable to track current speed

    [Header("Camera Settings")]
    public Camera mainCamera;
    public float cameraYOffset = 2.0f;
    public float cameraRotationSpeed = 2.0f; // Speed of camera rotation

    [Header("Debug Settings")]
    public bool showRaycasts = true; // Show raycasts in the scene
    public Vector2 currentGravityDirection = Vector2.down; // Current direction of gravity

    private float moveDirection = 0;
    public bool isGrounded = false;
    public bool isWallClinging = false;
    private bool isWallClingingLeft = false;
    private bool isWallClingingRight = false;
    private bool canMove = true; // Variable to control movement
    private int airJumps;
    private Vector3 cameraPos;
    private Rigidbody2D r2d;
    public BoxCollider2D mainCollider;
    private Transform t;
    private bool isSliding = false;
    private bool bufferStarted = false;
    private Quaternion targetCameraRotation; // Target rotation for the camera

    void Start()
    {
        t = transform;
        r2d = GetComponent<Rigidbody2D>();
        mainCollider = GetComponent<BoxCollider2D>();
        r2d.freezeRotation = true;
        r2d.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        r2d.gravityScale = gravityScale;

        if (mainCamera)
        {
            cameraPos = mainCamera.transform.position;
            targetCameraRotation = mainCamera.transform.rotation;
        }
    }

    void Update()
    {
        if (isGrounded)
        {
            airJumps = airJumpsMax;
        }

        // Gravity change controls
        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            ChangeGravity(Vector2.down, Quaternion.Euler(0, 0, 0));
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            ChangeGravity(Vector2.left, Quaternion.Euler(0, 0, -90));
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            ChangeGravity(Vector2.right, Quaternion.Euler(0, 0, 90));
        }
        else if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            ChangeGravity(Vector2.up, Quaternion.Euler(0, 0, 180));
        }

        // Movement controls
        if (canMove)
        {
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.D))
            {
                moveDirection = Input.GetKey(KeyCode.A) ? -1 : 1;
            }
            else
            {
                moveDirection = 0;
            }
        }

        // Jumping
        if (Input.GetKeyDown(KeyCode.W) && canMove)
        {
            if ((isGrounded && !isWallClinging) || (airJumps >= 1 && !isWallClinging))
            {
                r2d.velocity += -currentGravityDirection * jumpHeight;
                if (!isGrounded)
                {
                    airJumps--;
                }
            }

            if (isWallClinging)
            {
                if (isWallClingingLeft)
                {
                    // Apply force to jump away from the left wall
                    r2d.velocity = new Vector2(wallJumpForce * -currentGravityDirection.y, jumpHeight * -currentGravityDirection.x);
                    isWallClinging = false;
                    isWallClingingLeft = false;
                    airJumps = airJumpsMax;
                    StartCoroutine(WallJumpDelay()); // Start delay coroutine
                }

                if (isWallClingingRight)
                {
                    // Apply force to jump away from the right wall
                    r2d.velocity = new Vector2(-wallJumpForce * -currentGravityDirection.y, jumpHeight * -currentGravityDirection.x);
                    isWallClinging = false;
                    isWallClingingRight = false;
                    airJumps = airJumpsMax;
                    StartCoroutine(WallJumpDelay()); // Start delay coroutine
                }
            }
        }

        // Camera follow
        if (mainCamera)
        {
            mainCamera.transform.position = new Vector3(t.position.x, t.position.y + cameraYOffset, cameraPos.z);
            mainCamera.transform.rotation = Quaternion.Lerp(mainCamera.transform.rotation, targetCameraRotation, cameraRotationSpeed * Time.deltaTime);
        }

        // Update current speed
        currentSpeed = r2d.velocity.magnitude;
    }

    void FixedUpdate()
    {
        Bounds colliderBounds = mainCollider.bounds;
        float colliderRadius = mainCollider.size.x * 0.4f * Mathf.Abs(transform.localScale.x);
        Vector3 groundCheckPos = colliderBounds.min + new Vector3(colliderBounds.size.x * 0.5f, colliderRadius * 0.9f, 0);

        // Check if player is grounded
        Collider2D[] colliders = Physics2D.OverlapCircleAll(groundCheckPos, colliderRadius);
        isGrounded = false;
        if (colliders.Length > 0)
        {
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] != mainCollider)
                {
                    isGrounded = true;
                    break;
                }
            }
        }

        // Wall clinging detection using raycasts
        isWallClinging = false;
        isWallClingingLeft = false;
        isWallClingingRight = false;

        Vector2 raycastDirectionLeft = -Vector2.Perpendicular(currentGravityDirection);
        Vector2 raycastDirectionRight = Vector2.Perpendicular(currentGravityDirection);

        RaycastHit2D leftHitTop = Physics2D.Raycast(colliderBounds.center, raycastDirectionLeft, colliderBounds.extents.x + 0.1f);
        RaycastHit2D leftHitBottom = Physics2D.Raycast(new Vector3(colliderBounds.center.x, colliderBounds.min.y, colliderBounds.center.z), raycastDirectionLeft, colliderBounds.extents.x + 0.1f);
        RaycastHit2D rightHitTop = Physics2D.Raycast(colliderBounds.center, raycastDirectionRight, colliderBounds.extents.x + 0.1f);
        RaycastHit2D rightHitBottom = Physics2D.Raycast(new Vector3(colliderBounds.center.x, colliderBounds.min.y, colliderBounds.center.z), raycastDirectionRight, colliderBounds.extents.x + 0.1f);

        // Check for wall clinging to the left
        if (!isGrounded && leftHitTop.collider != null && leftHitBottom.collider != null && leftHitTop.collider != mainCollider && leftHitBottom.collider != mainCollider)
        {
            isWallClinging = true;
            isWallClingingLeft = true;
            if (!bufferStarted)
            {
                StartCoroutine(WallClingBufferCoroutine());
            }
        }

        // Check for wall clinging to the right
        if (!isGrounded && rightHitTop.collider != null && rightHitBottom.collider != null && rightHitTop.collider != mainCollider && rightHitBottom.collider != mainCollider)
        {
            isWallClinging = true;
            isWallClingingRight = true;
            if (!bufferStarted)
            {
                StartCoroutine(WallClingBufferCoroutine());
            }
        }

        // Additional check for movement input with no movement when not grounded
        if (!isGrounded && canMove)
        {
            float playerVelocity = (currentGravityDirection == Vector2.left || currentGravityDirection == Vector2.right) ? r2d.velocity.y : r2d.velocity.x;
            bool pressingLeft = false;
            bool pressingRight = false;

            // Detect movement direction based on gravity
            if (currentGravityDirection == Vector2.left)
            {
                pressingLeft = Input.GetKey(KeyCode.W);
                pressingRight = Input.GetKey(KeyCode.S);
            }
            else if (currentGravityDirection == Vector2.right)
            {
                pressingLeft = Input.GetKey(KeyCode.S);
                pressingRight = Input.GetKey(KeyCode.W);
            }
            else if (currentGravityDirection == Vector2.up)
            {
                pressingLeft = Input.GetKey(KeyCode.D);
                pressingRight = Input.GetKey(KeyCode.A);
            }
            else // currentGravityDirection == Vector2.down
            {
                pressingLeft = Input.GetKey(KeyCode.A);
                pressingRight = Input.GetKey(KeyCode.D);
            }

            // Check if player is pressing right and not moving
            if (pressingRight && playerVelocity == 0)
            {
                isWallClinging = true;
                isWallClingingRight = true;
            }

            // Check if player is pressing left and not moving
            if (pressingLeft && playerVelocity == 0)
            {
                isWallClinging = true;
                isWallClingingLeft = true;
            }
        }

        if (canMove)
        {
            if (!isWallClinging)
            {
                if (moveDirection != 0)
                {
                    if (currentGravityDirection == Vector2.left || currentGravityDirection == Vector2.right)
                    {
                        r2d.velocity = new Vector2(r2d.velocity.x, moveDirection * maxSpeed);
                    }
                    else
                    {
                        r2d.velocity = new Vector2(moveDirection * maxSpeed, r2d.velocity.y);
                    }
                }
                else
                {
                    if (currentGravityDirection == Vector2.left || currentGravityDirection == Vector2.right)
                    {
                        r2d.velocity = new Vector2(r2d.velocity.x, 0);
                    }
                    else
                    {
                        r2d.velocity = new Vector2(0, r2d.velocity.y); // Stop horizontal movement when no keys are pressed
                    }
                }
            }
            else
            {
                // While wall clinging, steer the wall jump without overriding it completely
                float horizontalSteering = moveDirection * maxSpeed * 0.5f; // Adjust the steering factor as needed
                r2d.velocity = new Vector2(r2d.velocity.x + horizontalSteering * Time.fixedDeltaTime, r2d.velocity.y);

                if (isSliding)
                {
                    r2d.velocity = new Vector2(r2d.velocity.x, -wallSlideSpeed);
                }
            }
        }

        // Ensure the player falls down when clinging to a wall
        if (isWallClinging)
        {
            // Ensure gravity takes effect
            r2d.gravityScale = gravityScale;
        }
        else
        {
            // Reset sliding and buffer when not wall clinging
            isSliding = false;
            bufferStarted = false;
        }
    }







    void ChangeGravity(Vector2 newGravityDirection, Quaternion newCameraRotation)
    {
        currentGravityDirection = newGravityDirection;
        Physics2D.gravity = newGravityDirection * 9.81f; // Adjust the gravity scale as needed
        targetCameraRotation = newCameraRotation;
    }

    void OnDrawGizmos()
    {
        if (showRaycasts)
        {
            Gizmos.color = Color.red;
            Bounds colliderBounds = mainCollider.bounds;
            float colliderRadius = mainCollider.size.x * 0.4f * Mathf.Abs(transform.localScale.x);

            Vector2 raycastDirectionLeft = Vector2.left;
            Vector2 raycastDirectionRight = Vector2.right;

            // Adjust raycast directions based on gravity
            if (currentGravityDirection == Vector2.left)
            {
                raycastDirectionLeft = Vector2.up;
                raycastDirectionRight = Vector2.down;
            }
            else if (currentGravityDirection == Vector2.right)
            {
                raycastDirectionLeft = Vector2.down;
                raycastDirectionRight = Vector2.up;
            }
            else if (currentGravityDirection == Vector2.up)
            {
                raycastDirectionLeft = Vector2.right;
                raycastDirectionRight = Vector2.left;
            }

            Gizmos.DrawLine(colliderBounds.center, colliderBounds.center + (Vector3)raycastDirectionLeft * (colliderBounds.extents.x + 0.1f));
            Gizmos.DrawLine(new Vector3(colliderBounds.center.x, colliderBounds.min.y, colliderBounds.center.z), new Vector3(colliderBounds.center.x, colliderBounds.min.y, colliderBounds.center.z) + (Vector3)raycastDirectionLeft * (colliderBounds.extents.x + 0.1f));
            Gizmos.DrawLine(colliderBounds.center, colliderBounds.center + (Vector3)raycastDirectionRight * (colliderBounds.extents.x + 0.1f));
            Gizmos.DrawLine(new Vector3(colliderBounds.center.x, colliderBounds.min.y, colliderBounds.center.z), new Vector3(colliderBounds.center.x, colliderBounds.min.y, colliderBounds.center.z) + (Vector3)raycastDirectionRight * (colliderBounds.extents.x + 0.1f));
        }
    }

    IEnumerator WallJumpDelay()
    {
        canMove = false;
        yield return new WaitForSeconds(wallJumpDelay);
        canMove = true;
    }

    IEnumerator WallClingBufferCoroutine()
    {
        bufferStarted = true;
        yield return new WaitForSeconds(wallClingBuffer);
        if (isWallClinging)
        {
            isSliding = true;
        }
    }
}
