using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
public class GravitySwitchingMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float maxSpeed = 3.4f;
    public float jumpHeight = 5.0f;
    public float gravityScale = 1.5f;
    public int airJumpsMax = 2;
    public float wallJumpForce = 5.0f;
    public float wallJumpDelay = 0.2f;
    public float wallSlideSpeed = 1.0f;
    public float wallClingBuffer = 1f; //0.5

    [Header("Camera Settings")]
    public Camera mainCamera;
    public float cameraYOffset = 2.0f;
    public float cameraRotationSpeed = 0.5f;

    private float moveDirection = 0;
    public bool isGrounded = false;
    public bool isWallClinging = false;
    private bool isWallClingingLeft = false;
    private bool isWallClingingRight = false;
    private bool canMove = true;
    private int airJumps;
    private Vector3 cameraPos;
    private Rigidbody2D r2d;
    private BoxCollider2D mainCollider;
    private Transform t;
    public float speedX;
    public float speedY;
    public float speedZ;
    private bool isSliding = false;
    private bool bufferStarted = false;

    private enum GravityDirection
    {
        Down,
        Left,
        Right,
        Up
    }

    private GravityDirection currentGravity = GravityDirection.Down;

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

        SetGravity(GravityDirection.Down);
    }

    void Update()
    {
        if (isGrounded)
        {
            airJumps = airJumpsMax;
        }

        if (Input.GetKeyDown(KeyCode.T))
        {
            transform.position = new Vector3(-48.5f, 0.800000012f, 0);
            SetGravity(GravityDirection.Down);
        }

        // Movement controls
        if (canMove)
        {
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.D))
            {
                switch (currentGravity)
                {
                    case GravityDirection.Down:
                        moveDirection = Input.GetKey(KeyCode.A) ? -1 : 1;
                        break;
                    case GravityDirection.Left:
                        moveDirection = Input.GetKey(KeyCode.A) ? 1 : -1; // Reversed controls for Left gravity
                        break;
                    case GravityDirection.Right:
                        moveDirection = Input.GetKey(KeyCode.A) ? -1 : 1;
                        break;
                    case GravityDirection.Up:
                        moveDirection = Input.GetKey(KeyCode.A) ? 1 : -1;
                        break;
                }
            }
            else
            {
                moveDirection = 0;
            }
        }

        // Jumping
        if (Input.GetKeyDown(KeyCode.W) && canMove || Input.GetKeyDown(KeyCode.Space) && canMove)
        {
            if ((isGrounded && !isWallClinging) || (airJumps >= 1 && !isWallClinging))
            {
                Vector2 jumpVelocity = Vector2.zero;
                switch (currentGravity)
                {
                    case GravityDirection.Down:
                        jumpVelocity = new Vector2(r2d.velocity.x, jumpHeight);
                        break;
                    case GravityDirection.Left:
                        jumpVelocity = new Vector2(jumpHeight, r2d.velocity.y);
                        break;
                    case GravityDirection.Right:
                        jumpVelocity = new Vector2(-jumpHeight, r2d.velocity.y);
                        break;
                    case GravityDirection.Up:
                        jumpVelocity = new Vector2(r2d.velocity.x, -jumpHeight);
                        break;
                }
                r2d.velocity = jumpVelocity;

                if (!isGrounded)
                {
                    airJumps--;
                }
            }

            if (isWallClinging)
            {
                Vector2 wallJumpVelocity = Vector2.zero;
                if (isWallClingingLeft)
                {
                    // Apply force to jump away from the left wall
                    switch (currentGravity)
                    {
                        case GravityDirection.Down:
                            wallJumpVelocity = new Vector2(wallJumpForce, jumpHeight);
                            break;
                        case GravityDirection.Left:
                            wallJumpVelocity = new Vector2(jumpHeight, -wallJumpForce);
                            break;
                        case GravityDirection.Right:
                            wallJumpVelocity = new Vector2(-jumpHeight, wallJumpForce); // Apply physics from RPlayerMovement script
                            break;
                        case GravityDirection.Up:
                            wallJumpVelocity = new Vector2(-wallJumpForce, -jumpHeight);
                            break;
                    }
                    r2d.velocity = wallJumpVelocity;
                    isWallClinging = false;
                    isWallClingingLeft = false;
                    airJumps = airJumpsMax;
                    StartCoroutine(WallJumpDelay());
                }

                if (isWallClingingRight)
                {
                    // Apply force to jump away from the right wall
                    switch (currentGravity)
                    {
                        case GravityDirection.Down:
                            wallJumpVelocity = new Vector2(-wallJumpForce, jumpHeight);
                            break;
                        case GravityDirection.Left:
                            wallJumpVelocity = new Vector2(jumpHeight, wallJumpForce);
                            break;
                        case GravityDirection.Right:
                            wallJumpVelocity = new Vector2(-jumpHeight, -wallJumpForce); // Apply physics from RPlayerMovement script
                            break;
                        case GravityDirection.Up:
                            wallJumpVelocity = new Vector2(wallJumpForce, -jumpHeight);
                            break;
                    }
                    r2d.velocity = wallJumpVelocity;
                    isWallClinging = false;
                    isWallClingingRight = false;
                    airJumps = airJumpsMax;
                    StartCoroutine(WallJumpDelay());
                }
            }

        }

        // Camera follow
        if (mainCamera)
        {
            mainCamera.transform.position = new Vector3(t.position.x, t.position.y + cameraYOffset, cameraPos.z);
        }

        // Update current speed
        speedX = r2d.velocity.x;
        speedY = r2d.velocity.y;
        speedZ = 0; // Assuming 2D movement, Z speed is 0

        // Check for gravity change input
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            SetGravity(GravityDirectionRelativeToCamera(Vector3.left));
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            SetGravity(GravityDirectionRelativeToCamera(Vector3.right));
        }
        else if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            SetGravity(GravityDirectionRelativeToCamera(Vector3.up));
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            SetGravity(GravityDirectionRelativeToCamera(Vector3.down));
        }
    }

    void FixedUpdate()
    {
        Bounds colliderBounds = mainCollider.bounds;
        float colliderRadius = currentGravity == GravityDirection.Left || currentGravity == GravityDirection.Right
            ? mainCollider.size.y * 0.4f * Mathf.Abs(transform.localScale.y)
            : mainCollider.size.x * 0.4f * Mathf.Abs(transform.localScale.x);
        Vector3 groundCheckPos = colliderBounds.center;

        switch (currentGravity)
        {
            case GravityDirection.Down:
                groundCheckPos += new Vector3(0, colliderRadius * -0.9f, 0);
                break;
            case GravityDirection.Left:
                groundCheckPos += new Vector3(colliderRadius * -0.9f, 0);
                break;
            case GravityDirection.Right:
                groundCheckPos += new Vector3(colliderRadius * 0.9f, 0);
                break;
            case GravityDirection.Up:
                groundCheckPos += new Vector3(0, colliderRadius * 0.9f, 0);
                break;
        }

        // Check if player is grounded
        isGrounded = false;
        Collider2D[] colliders = Physics2D.OverlapCircleAll(groundCheckPos, colliderRadius);
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

        RaycastHit2D leftHitTop, leftHitBottom, rightHitTop, rightHitBottom;

        switch (currentGravity)
        {
            case GravityDirection.Down:
            case GravityDirection.Up:
                leftHitTop = RaycastInDirection(colliderBounds.center, Vector2.left, colliderBounds.extents);
                leftHitBottom = RaycastInDirection(new Vector3(colliderBounds.center.x, colliderBounds.min.y, colliderBounds.center.z), Vector2.left, colliderBounds.extents);
                rightHitTop = RaycastInDirection(colliderBounds.center, Vector2.right, colliderBounds.extents);
                rightHitBottom = RaycastInDirection(new Vector3(colliderBounds.center.x, colliderBounds.min.y, colliderBounds.center.z), Vector2.right, colliderBounds.extents);
                break;
            case GravityDirection.Left:
            case GravityDirection.Right:
                leftHitTop = RaycastInDirection(colliderBounds.center, Vector2.down, colliderBounds.extents);
                leftHitBottom = RaycastInDirection(new Vector3(colliderBounds.center.x, colliderBounds.min.y, colliderBounds.center.z), Vector2.down, colliderBounds.extents);
                rightHitTop = RaycastInDirection(colliderBounds.center, Vector2.up, colliderBounds.extents);
                rightHitBottom = RaycastInDirection(new Vector3(colliderBounds.center.x, colliderBounds.min.y, colliderBounds.center.z), Vector2.up, colliderBounds.extents);
                break;
            default:
                leftHitTop = leftHitBottom = rightHitTop = rightHitBottom = default;
                break;
        }

        // Check for wall clinging to the left
        if ((currentGravity == GravityDirection.Down || currentGravity == GravityDirection.Up) && Input.GetKey(KeyCode.A) && !isGrounded && (leftHitTop.collider != null || leftHitBottom.collider != null))
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
        else if ((currentGravity == GravityDirection.Left || currentGravity == GravityDirection.Right) && Input.GetKey(KeyCode.A) && !isGrounded && (leftHitTop.collider != null || leftHitBottom.collider != null))
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
        if ((currentGravity == GravityDirection.Down || currentGravity == GravityDirection.Up) && Input.GetKey(KeyCode.D) && !isGrounded && (rightHitTop.collider != null || rightHitBottom.collider != null))
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
        else if ((currentGravity == GravityDirection.Left || currentGravity == GravityDirection.Right) && Input.GetKey(KeyCode.D) && !isGrounded && (rightHitTop.collider != null || rightHitBottom.collider != null))
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
        if (currentGravity == GravityDirection.Down || currentGravity == GravityDirection.Up)
        {
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
        }
        else if (currentGravity == GravityDirection.Left || currentGravity == GravityDirection.Right)
        {
            if (Input.GetKey(KeyCode.A) && !isGrounded && r2d.velocity.y == 0)
            {
                isWallClinging = true;
                isWallClingingLeft = true;
                if (!bufferStarted)
                {
                    StartCoroutine(WallClingBufferCoroutine());
                }
            }

            if (Input.GetKey(KeyCode.D) && !isGrounded && r2d.velocity.y == 0)
            {
                isWallClinging = true;
                isWallClingingRight = true;
                if (!bufferStarted)
                {
                    StartCoroutine(WallClingBufferCoroutine());
                }
            }
        }

        // Apply movement velocity only if not wall clinging
        if (canMove)
        {
            if (!isWallClinging)
            {
                if (moveDirection != 0)
                {
                    switch (currentGravity)
                    {
                        case GravityDirection.Down:
                            r2d.velocity = new Vector2(moveDirection * maxSpeed, r2d.velocity.y);
                            break;
                        case GravityDirection.Left:
                            r2d.velocity = new Vector2(r2d.velocity.x, moveDirection * maxSpeed);
                            break;
                        case GravityDirection.Right:
                            r2d.velocity = new Vector2(r2d.velocity.x, moveDirection * maxSpeed);
                            break;
                        case GravityDirection.Up:
                            r2d.velocity = new Vector2(moveDirection * maxSpeed, r2d.velocity.y);
                            break;
                    }
                }
                else
                {
                    switch (currentGravity)
                    {
                        case GravityDirection.Down:
                        case GravityDirection.Up:
                            r2d.velocity = new Vector2(0, r2d.velocity.y);
                            break;
                        case GravityDirection.Left:
                        case GravityDirection.Right:
                            r2d.velocity = new Vector2(r2d.velocity.x, 0);
                            break;
                    }
                }
            }
            else
            {
                // Lock horizontal (or vertical for left/right gravity) movement when wall clinging
                switch (currentGravity)
                {
                    case GravityDirection.Down:
                    case GravityDirection.Up:
                        r2d.velocity = new Vector2(0, r2d.velocity.y);
                        break;
                    case GravityDirection.Left:
                    case GravityDirection.Right:
                        r2d.velocity = new Vector2(r2d.velocity.x, 0);
                        break;
                }

                // While wall clinging, steer the wall jump without overriding it completely
                float steeringFactor = moveDirection * maxSpeed * 0.5f;
                switch (currentGravity)
                {
                    case GravityDirection.Down:
                    case GravityDirection.Up:
                        r2d.velocity = new Vector2(r2d.velocity.x + steeringFactor * Time.fixedDeltaTime, r2d.velocity.y);
                        break;
                    case GravityDirection.Left:
                    case GravityDirection.Right:
                        r2d.velocity = new Vector2(r2d.velocity.x, r2d.velocity.y + steeringFactor * Time.fixedDeltaTime);
                        break;
                }

                if (isSliding)
                {
                    switch (currentGravity)
                    {
                        case GravityDirection.Down:
                            r2d.velocity = new Vector2(r2d.velocity.x, -wallSlideSpeed);
                            break;
                        case GravityDirection.Up:
                            r2d.velocity = new Vector2(r2d.velocity.x, wallSlideSpeed);
                            break;
                        case GravityDirection.Left:
                            r2d.velocity = new Vector2(-wallSlideSpeed, r2d.velocity.y);
                            break;
                        case GravityDirection.Right:
                            r2d.velocity = new Vector2(wallSlideSpeed, r2d.velocity.y);
                            break;
                    }
                }
            }
        }

        // Ensure the player falls correctly when clinging to a wall
        if (isWallClinging)
        {
            // Ensure gravity takes effect
            r2d.gravityScale = gravityScale;

            // Add velocity threshold check to eliminate small movements
            if (currentGravity == GravityDirection.Down || currentGravity == GravityDirection.Up)
            {
                if (Mathf.Abs(r2d.velocity.x) < 0.01f)
                {
                    r2d.velocity = new Vector2(0, r2d.velocity.y);
                }
            }
            else if (currentGravity == GravityDirection.Left || currentGravity == GravityDirection.Right)
            {
                if (Mathf.Abs(r2d.velocity.y) < 0.01f)
                {
                    r2d.velocity = new Vector2(r2d.velocity.x, 0);
                }
            }
        }
        else
        {
            // Reset gravity scale when not clinging
            r2d.gravityScale = gravityScale;
            isSliding = false;
            bufferStarted = false;
        }
    }

    void SetGravity(GravityDirection direction)
    {
        currentGravity = direction;
        Quaternion targetRotation = Quaternion.identity;
        switch (direction)
        {
            case GravityDirection.Down:
                Physics2D.gravity = new Vector2(0, -9.81f * gravityScale);
                targetRotation = Quaternion.Euler(0, 0, 0);
                break;
            case GravityDirection.Left:
                Physics2D.gravity = new Vector2(-9.81f * gravityScale, 0);
                targetRotation = Quaternion.Euler(0, 0, -90);
                break;
            case GravityDirection.Right:
                Physics2D.gravity = new Vector2(9.81f * gravityScale, 0);
                targetRotation = Quaternion.Euler(0, 0, 90);
                break;
            case GravityDirection.Up:
                Physics2D.gravity = new Vector2(0, 9.81f * gravityScale);
                targetRotation = Quaternion.Euler(0, 0, 180);
                break;
        }
        StartCoroutine(SmoothRotateCamera(targetRotation));
    }

    GravityDirection GravityDirectionRelativeToCamera(Vector3 direction)
    {
        Vector3 relativeDirection = mainCamera.transform.TransformDirection(direction);
        if (Mathf.Abs(relativeDirection.x) > Mathf.Abs(relativeDirection.y))
        {
            if (relativeDirection.x > 0)
            {
                return GravityDirection.Right;
            }
            else
            {
                return GravityDirection.Left;
            }
        }
        else
        {
            if (relativeDirection.y > 0)
            {
                return GravityDirection.Up;
            }
            else
            {
                return GravityDirection.Down;
            }
        }
    }

    private Coroutine currentRotationCoroutine;

    IEnumerator SmoothRotateCamera(Quaternion targetRotation)
    {
        if (currentRotationCoroutine != null)
        {
            StopCoroutine(currentRotationCoroutine);
        }
        currentRotationCoroutine = StartCoroutine(SmoothRotate(targetRotation));
        yield return null; // This line ensures the method returns an IEnumerator
    }

    private IEnumerator SmoothRotate(Quaternion targetRotation)
    {
        Quaternion startRotation = mainCamera.transform.rotation;
        float time = 0;

        while (time < 1)
        {
            mainCamera.transform.rotation = Quaternion.Lerp(startRotation, targetRotation, time);
            time += Time.deltaTime * cameraRotationSpeed;
            yield return null;
        }
        mainCamera.transform.rotation = targetRotation;
        currentRotationCoroutine = null;
    }

    RaycastHit2D RaycastInDirection(Vector3 origin, Vector2 direction, Vector3 extents)
    {
        float distance = (currentGravity == GravityDirection.Left || currentGravity == GravityDirection.Right) ? extents.y + 0.1f : extents.x + 0.1f;
        Debug.DrawRay(origin, direction * distance, Color.red); // Draw the ray in the scene view
        return Physics2D.Raycast(origin, direction, distance);
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

    void OnDrawGizmos()
    {
        if (mainCollider == null) return;

        Bounds colliderBounds = mainCollider.bounds;
        float colliderRadius = currentGravity == GravityDirection.Left || currentGravity == GravityDirection.Right
            ? mainCollider.size.y * 0.4f * Mathf.Abs(transform.localScale.y)
            : mainCollider.size.x * 0.4f * Mathf.Abs(transform.localScale.x);
        Vector3 groundCheckPos = colliderBounds.center;

        switch (currentGravity)
        {
            case GravityDirection.Down:
                groundCheckPos += new Vector3(0, colliderRadius * -0.9f, 0);
                break;
            case GravityDirection.Left:
                groundCheckPos += new Vector3(colliderRadius * -0.9f, 0);
                break;
            case GravityDirection.Right:
                groundCheckPos += new Vector3(colliderRadius * 0.9f, 0);
                break;
            case GravityDirection.Up:
                groundCheckPos += new Vector3(0, colliderRadius * 0.9f, 0);
                break;
        }

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(groundCheckPos, colliderRadius);

        Gizmos.color = Color.red;
        Vector3 leftRayStartTop = colliderBounds.center;
        Vector3 leftRayStartBottom = new Vector3(colliderBounds.center.x, colliderBounds.min.y, colliderBounds.center.z);
        Vector3 rightRayStartTop = colliderBounds.center;
        Vector3 rightRayStartBottom = new Vector3(colliderBounds.center.x, colliderBounds.min.y, colliderBounds.center.z);

        switch (currentGravity)
        {
            case GravityDirection.Down:
            case GravityDirection.Up:
                Gizmos.DrawRay(leftRayStartTop, Vector2.left * (colliderBounds.extents.x + 0.1f));
                Gizmos.DrawRay(leftRayStartBottom, Vector2.left * (colliderBounds.extents.x + 0.1f));
                Gizmos.DrawRay(rightRayStartTop, Vector2.right * (colliderBounds.extents.x + 0.1f));
                Gizmos.DrawRay(rightRayStartBottom, Vector2.right * (colliderBounds.extents.x + 0.1f));
                break;
            case GravityDirection.Left:
            case GravityDirection.Right:
                Gizmos.DrawRay(leftRayStartTop, Vector2.down * (colliderBounds.extents.y + 0.1f));
                Gizmos.DrawRay(leftRayStartBottom, Vector2.down * (colliderBounds.extents.y + 0.1f));
                Gizmos.DrawRay(rightRayStartTop, Vector2.up * (colliderBounds.extents.y + 0.1f));
                Gizmos.DrawRay(rightRayStartBottom, Vector2.up * (colliderBounds.extents.y + 0.1f));
                break;
        }
    }
}
