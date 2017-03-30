using UnityEngine;

public class Controller2D : RaycastController {

    private Vector2 playerInput;
    public Vector2 PlayerInput {
        get {
            return playerInput;
        }
    }
    
    public CollisionInfo collisions;
    private float maxClimbAngle = 60;
    private float maxDescendAngle = 75;

    protected override void Start ()
    {
        base.Start ();
        collisions.faceDirection = 1;
    }

    public void Move (Vector3 velocity, bool isStandingOnPlatform = false)
    {
        Move (velocity, Vector2.zero, isStandingOnPlatform);
    }

    public void Move (Vector3 velocity, Vector2 input, bool isStandingOnPlatform = false)
    {
        UpdateRaycastOrigins ();
        collisions.Reset ();
        collisions.velocityOld = velocity;
        playerInput = input;

        if (velocity.x != 0)
        {
            collisions.faceDirection = (int) Mathf.Sign (velocity.x);
        }

        if (velocity.y < 0)
        {
            DescendSlope (ref velocity);
        }

        HorizontalCollisions (ref velocity);
        
        if (velocity.y != 0)
        {
            VerticalCollisions (ref velocity);
        }

        transform.Translate (velocity);

        if (isStandingOnPlatform)
        {
            collisions.below = true;
        }
    }

    private void HorizontalCollisions (ref Vector3 velocity)
    {
        float directionX = collisions.faceDirection;
        float rayLength = Mathf.Abs (velocity.x) + skinWidth;

        if (Mathf.Abs (velocity.x) < skinWidth)
        {
            rayLength = 2 * skinWidth;
        }

        for (int i = 0; i < horizontalRayCount; i++)
        {
            Vector2 rayOrigin = (directionX == -1) ? raycastOrigins.bottomLeft : raycastOrigins.bottomRight;
            rayOrigin += Vector2.up * (horizontalRaySpacing * i);

            RaycastHit2D hit = Physics2D.Raycast (rayOrigin, Vector2.right * directionX, rayLength, collisionMask);

            if (hit)
            {
                if (hit.distance == 0)
                {
                    continue;
                }
                
                float slopeAngle = Vector2.Angle (hit.normal, Vector2.up);

                if (i == 0 && slopeAngle <= maxClimbAngle)
                {
                    float distToSlopeStart = 0;
                    if (slopeAngle != collisions.slopeAngleOld)
                    {
                        distToSlopeStart = hit.distance - skinWidth;
                        if (collisions.descendingSlope)
                        {
                            collisions.descendingSlope = false;
                            velocity = collisions.velocityOld;
                        }
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
                if (hit.collider.tag == "passThrough")
                {
                    if (directionY == 1 || hit.distance == 0)
                    {
                        continue;
                    }
                    if (collisions.isFallingThroughPlatform)
                    {
                        continue;
                    }
                    if (playerInput.y == -1) // TODO - Change this to trigger over certain deadzone threshold
                    {
                        collisions.isFallingThroughPlatform = true;
                        Invoke ("ResetFallingThroughPlatform", 0.25f);
                        continue;
                    }
                }

                velocity.y = (hit.distance - skinWidth) * directionY;
                rayLength = hit.distance;

                if (collisions.climbingSlope)
                {
                    velocity.x = velocity.y / Mathf.Tan (collisions.slopeAngle * Mathf.Deg2Rad) * Mathf.Sign (velocity.x);
                }

                collisions.below = directionY == -1;
                collisions.above = directionY == 1;
            }

            Debug.DrawRay (rayOrigin, Vector2.up * directionY * rayLength, Color.red);
        }

        if (collisions.climbingSlope)
        {
            float directionX = Mathf.Sign (velocity.x);
            rayLength = Mathf.Abs (velocity.x) + skinWidth;
            Vector2 rayOrigin = ((directionX == -1) ? raycastOrigins.bottomLeft : raycastOrigins.bottomRight) + (Vector2.up * velocity.y);

            RaycastHit2D hit = Physics2D.Raycast (rayOrigin, Vector2.right * directionX, rayLength, collisionMask);

            if (hit)
            {
                float slopeAngle = Vector2.Angle (hit.normal, Vector2.up);

                if (slopeAngle != collisions.slopeAngle)
                {
                    velocity.x = (hit.distance - skinWidth) * directionX;
                    collisions.slopeAngle = slopeAngle;
                }
            }
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

    private void DescendSlope (ref Vector3 velocity)
    {
        float directionX = Mathf.Sign (velocity.x);
        Vector2 rayOrigin = (directionX == -1) ? raycastOrigins.bottomRight : raycastOrigins.bottomLeft;
        RaycastHit2D hit = Physics2D.Raycast (rayOrigin, -Vector2.up, Mathf.Infinity, collisionMask);

        if (hit)
        {
            float slopeAngle = Vector2.Angle (hit.normal, Vector2.up);

            if (slopeAngle != 0 && slopeAngle <= maxDescendAngle)
            {
                if (Mathf.Sign (hit.normal.x) == directionX)
                {
                    if (hit.distance - skinWidth <= Mathf.Tan (slopeAngle * Mathf.Deg2Rad * Mathf.Abs (velocity.x)))
                    {
                        float moveDistance = Mathf.Abs (velocity.x);
                        float descendVelocityY = Mathf.Sin (slopeAngle * Mathf.Deg2Rad) * moveDistance;
                        velocity.x = Mathf.Cos (slopeAngle * Mathf.Deg2Rad) * moveDistance * Mathf.Sign (velocity.x);
                        velocity.y -= descendVelocityY;

                        collisions.slopeAngle = slopeAngle;
                        collisions.descendingSlope = true;
                        collisions.below = true;
                    }
                }
            }
        }
    }
    
    private void ResetFallingThroughPlatform ()
    {
        collisions.isFallingThroughPlatform = false;
    }

    public struct CollisionInfo {
        public bool above, below; 
        public bool left, right;

        public bool climbingSlope;
        public bool descendingSlope;
        public float slopeAngle, slopeAngleOld;

        public Vector3 velocityOld;
        public int faceDirection;
        public bool isFallingThroughPlatform;

        public void Reset ()
        {
            above = below = false;
            left = right = false;
            climbingSlope = descendingSlope = false;
            slopeAngleOld = slopeAngle;
            slopeAngle = 0f;
        }

    }
}
