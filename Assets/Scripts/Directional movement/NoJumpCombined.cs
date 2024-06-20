using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
public class NJGravitySwitchingMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float maxSpeed = 3.4f;
    public float gravityScale = 1.5f;

    [Header("Camera Settings")]
    public Camera mainCamera;
    public float cameraYOffset = 2.0f;
    public float cameraRotationSpeed = 0.5f;

    private float moveDirection = 0;
    public bool isGrounded = false;
    private bool canMove = true;
    private Vector3 cameraPos;
    private Rigidbody2D r2d;
    private BoxCollider2D mainCollider;
    private Transform t;
    public float speedX;
    public float speedY;
    public float speedZ;

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

        // Apply movement velocity
        if (canMove)
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
