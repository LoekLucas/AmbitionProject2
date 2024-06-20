using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
public class CharacterMovement : MonoBehaviour
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

    [Header("Camera Settings")]
    public Camera mainCamera;
    public float cameraYOffset = 2.0f;

    private float moveDirection = 0;
    private bool isGrounded = false;
    public bool isWallClinging = false;
    private bool isWallClingingLeft = false;
    private bool isWallClingingRight = false;
    private bool canMove = true; // Variable to control movement
    private int airJumps;
    private Vector3 cameraPos;
    private Rigidbody2D r2d;
    private BoxCollider2D mainCollider;
    private Transform t;
    private float currentSpeed; // Variable to track current speed
    private bool isSliding = false;
    private bool bufferStarted = false;

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
        }
    }

    void Update()
    {
        if (isGrounded)
        {
            airJumps = airJumpsMax;
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
                r2d.velocity = new Vector2(r2d.velocity.x, jumpHeight);
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
                    r2d.velocity = new Vector2(wallJumpForce, jumpHeight);
                    isWallClinging = false;
                    isWallClingingLeft = false;
                    airJumps = airJumpsMax;
                    StartCoroutine(WallJumpDelay()); // Start delay coroutine
                }

                if (isWallClingingRight)
                {
                    // Apply force to jump away from the right wall
                    r2d.velocity = new Vector2(-wallJumpForce, jumpHeight);
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

        RaycastHit2D leftHitTop = Physics2D.Raycast(colliderBounds.center, Vector2.left, colliderBounds.extents.x + 0.1f);
        RaycastHit2D leftHitBottom = Physics2D.Raycast(new Vector3(colliderBounds.center.x, colliderBounds.min.y, colliderBounds.center.z), Vector2.left, colliderBounds.extents.x + 0.1f);
        RaycastHit2D rightHitTop = Physics2D.Raycast(colliderBounds.center, Vector2.right, colliderBounds.extents.x + 0.1f);
        RaycastHit2D rightHitBottom = Physics2D.Raycast(new Vector3(colliderBounds.center.x, colliderBounds.min.y, colliderBounds.center.z), Vector2.right, colliderBounds.extents.x + 0.1f);

        // Check for wall clinging to the left
        if (Input.GetKey(KeyCode.A) && !isGrounded && (leftHitTop.collider != null || leftHitBottom.collider != null))
        {
            if (leftHitTop.collider != mainCollider && leftHitBottom.collider != mainCollider)
            {
                isWallClinging = true;
                isWallClingingLeft = true;
                if (!bufferStarted)
                {
                    StartCoroutine(WallClingBufferCoroutine());
                }
            }
        }

        // Check for wall clinging to the right
        if (Input.GetKey(KeyCode.D) && !isGrounded && (rightHitTop.collider != null || rightHitBottom.collider != null))
        {
            if (rightHitTop.collider != mainCollider && rightHitBottom.collider != mainCollider)
            {
                isWallClinging = true;
                isWallClingingRight = true;
                if (!bufferStarted)
                {
                    StartCoroutine(WallClingBufferCoroutine());
                }
            }
        }

        // Additional check for wall clinging when speed is 0
        if (Input.GetKey(KeyCode.A) && !isGrounded && r2d.velocity.x == 0)
        {
            isWallClinging = true;
            isWallClingingLeft = true;
            if (!bufferStarted)
            {
                StartCoroutine(WallClingBufferCoroutine());
            }
        }

        if (Input.GetKey(KeyCode.D) && !isGrounded && r2d.velocity.x == 0)
        {
            isWallClinging = true;
            isWallClingingRight = true;
            if (!bufferStarted)
            {
                StartCoroutine(WallClingBufferCoroutine());
            }
        }

        // Apply movement velocity only if not wall clinging
        if (canMove)
        {
            if (!isWallClinging)
            {
                if (moveDirection != 0)
                {
                    r2d.velocity = new Vector2(moveDirection * maxSpeed, r2d.velocity.y);
                }
                else
                {
                    r2d.velocity = new Vector2(0, r2d.velocity.y); // Stop horizontal movement when no keys are pressed
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
            // Reset gravity scale when not clinging
            r2d.gravityScale = gravityScale;
            isSliding = false; // Reset sliding when not wall clinging
            bufferStarted = false; // Reset bufferStarted when not wall clinging
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
