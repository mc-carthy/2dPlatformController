using UnityEngine;

[RequireComponent (typeof (BoxCollider2D))]
public class Controller2D : MonoBehaviour {

    private const float skinWidth = 0.05f;

    public CollisionInfo collisions;

    [SerializeField]
    private LayerMask collisionMask;

    private int horizontalRayCount = 4;
    private int verticalRayCount = 4;
    private float horizontalRaySpacing;
    private float verticalRaySpacing;

    private float maxClimbAngle = 60;

	private BoxCollider2D collider;
    private RaycastOrigins raycastOrigins;

    private void Awake ()
    {
        collider = GetComponent<BoxCollider2D> ();
    }

    private void Start ()
    {
        CalculateRaySpacing ();
    }

    public void Move (Vector3 velocity)
    {
        UpdateRaycastOrigins ();
        collisions.Reset ();

        if (velocity.x != 0)
        {
            HorizontalCollisions (ref velocity);
        }
        if (velocity.y != 0)
        {
            VerticalCollisions (ref velocity);
        }

        transform.Translate (velocity);
    }

    private void HorizontalCollisions (ref Vector3 velocity)
    {
        float directionX = Mathf.Sign (velocity.x);
        float rayLength = Mathf.Abs (velocity.x) + skinWidth;

        for (int i = 0; i < horizontalRayCount; i++)
        {
            Vector2 rayOrigin = (directionX == -1) ? raycastOrigins.bottomLeft : raycastOrigins.bottomRight;
            rayOrigin += Vector2.up * (horizontalRaySpacing * i);

            RaycastHit2D hit = Physics2D.Raycast (rayOrigin, Vector2.right * directionX, rayLength, collisionMask);

            if (hit)
            {
                float slopeAngle = Vector2.Angle (hit.normal, Vector2.up);

                if (i == 0 && slopeAngle <= maxClimbAngle)
                {
                    float distToSlopeStart = 0;
                    if (slopeAngle != collisions.slopeAngleOld)
                    {
                        distToSlopeStart = hit.distance - skinWidth;
                        velocity.x -= distToSlopeStart * directionX;
                    }
                    ClimbSlope (ref velocity, slopeAngle);
                    velocity.x += distToSlopeStart * directionX;
                }

                if (!collisions.climbingSlope || slopeAngle > maxClimbAngle)
                {
                    velocity.x = (hit.distance - skinWidth) * directionX;
                    rayLength = hit.distance;

                    if (collisions.climbingSlope)
                    {
                        velocity.y = Mathf.Tan (collisions.slopeAngle * Mathf.Deg2Rad) * Mathf.Abs (velocity.x);
                    }

                    collisions.left = directionX == -1;
                    collisions.right = directionX == 1;
                }
            }

            Debug.DrawRay (rayOrigin, Vector2.right * directionX * rayLength, Color.red);
        }
    }

    private void VerticalCollisions (ref Vector3 velocity)
    {
        float directionY = Mathf.Sign (velocity.y);
        float rayLength = Mathf.Abs (velocity.y) + skinWidth;

        for (int i = 0; i < verticalRayCount; i++)
        {
            Vector2 rayOrigin = (directionY == -1) ? raycastOrigins.bottomLeft : raycastOrigins.topLeft;
            rayOrigin += Vector2.right * (verticalRaySpacing * i + velocity.x);

            RaycastHit2D hit = Physics2D.Raycast (rayOrigin, Vector2.up * directionY, rayLength, collisionMask);

            if (hit)
            {
                velocity.y = (hit.distance - skinWidth) * directionY;
                rayLength = hit.distance;

                collisions.below = directionY == -1;
                collisions.above = directionY == 1;
            }

            Debug.DrawRay (rayOrigin, Vector2.up * directionY * rayLength, Color.red);
        }
    }

    private void ClimbSlope (ref Vector3 velocity, float slopeAngle)
    {
        float moveDistance = Mathf.Abs (velocity.x);
        float climbVelocityY = Mathf.Sin (slopeAngle * Mathf.Deg2Rad) * moveDistance;
        if (velocity.y <= climbVelocityY)
        {
            velocity.y = climbVelocityY;
            velocity.x = Mathf.Cos (slopeAngle * Mathf.Deg2Rad) * moveDistance * Mathf.Sign (velocity.x);
            collisions.below = true;
            collisions.climbingSlope = true;
            collisions.slopeAngle = slopeAngle;
        }
    }

    private void UpdateRaycastOrigins ()
    {
        Bounds bounds = collider.bounds;
        bounds.Expand (skinWidth * -2);

        raycastOrigins.bottomLeft = new Vector2 (bounds.min.x, bounds.min.y);
        raycastOrigins.bottomRight = new Vector2 (bounds.max.x, bounds.min.y);
        raycastOrigins.topLeft = new Vector2 (bounds.min.x, bounds.max.y);
        raycastOrigins.topRight = new Vector2 (bounds.max.x, bounds.max.y);
    }

    private void CalculateRaySpacing ()
    {
        Bounds bounds = collider.bounds;
        bounds.Expand (skinWidth * -2);

        horizontalRayCount = Mathf.Clamp (horizontalRayCount, 2, int.MaxValue);
        verticalRayCount = Mathf.Clamp (verticalRayCount, 2, int.MaxValue);

        horizontalRaySpacing = bounds.size.y / (horizontalRayCount - 1);
        verticalRaySpacing = bounds.size.x / (verticalRayCount - 1);
    }

    private struct RaycastOrigins {
        public Vector2 topLeft, topRight, bottomLeft, bottomRight;
    }

    public struct CollisionInfo {
        public bool above, below; 
        public bool left, right;

        public bool climbingSlope;
        public float slopeAngle, slopeAngleOld;

        public void Reset ()
        {
            above = below = false;
            left = right = false;
            climbingSlope = false;
            slopeAngleOld = slopeAngle;
            slopeAngle = 0f;
        }

    }
}
