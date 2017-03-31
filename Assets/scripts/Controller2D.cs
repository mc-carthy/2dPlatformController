using UnityEngine;

public class Controller2D : RaycastController {

    private Vector2 playerInput;
    public Vector2 PlayerInput {
        get {
            return playerInput;
        }
    }

    public CollisionInfo collisions;
    private float maxSlopeAngle = 10f;

    protected override void Start ()
    {
        base.Start ();
        collisions.faceDirection = 1;
    }

    public void Move (Vector2 moveDistance, bool isStandingOnPlatform = false)
    {
        Move (moveDistance, Vector2.zero, isStandingOnPlatform);
    }

    public void Move (Vector2 moveDistance, Vector2 input, bool isStandingOnPlatform = false)
    {
        UpdateRaycastOrigins ();
        collisions.Reset ();
        collisions.velocityOld = moveDistance;
        playerInput = input;

        if (moveDistance.y < 0)
        {
            DescendSlope (ref moveDistance);
        }

        if (moveDistance.x != 0)
        {
            collisions.faceDirection = (int) Mathf.Sign (moveDistance.x);
        }
        
        HorizontalCollisions (ref moveDistance);
        
        if (moveDistance.y != 0)
        {
            VerticalCollisions (ref moveDistance);
        }

        transform.Translate (moveDistance);

        if (isStandingOnPlatform)
        {
            collisions.below = true;
        }
    }

    private void HorizontalCollisions (ref Vector2 moveDist)
    {
        float directionX = collisions.faceDirection;
        float rayLength = Mathf.Abs (moveDist.x) + skinWidth;

        if (Mathf.Abs (moveDist.x) < skinWidth)
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

                if (i == 0 && slopeAngle <= maxSlopeAngle)
                {
                    float distToSlopeStart = 0;
                    if (slopeAngle != collisions.slopeAngleOld)
                    {
                        distToSlopeStart = hit.distance - skinWidth;
                        if (collisions.descendingSlope)
                        {
                            collisions.descendingSlope = false;
                            moveDist = collisions.velocityOld;
                        }
                        moveDist.x -= distToSlopeStart * directionX;
                    }
                    ClimbSlope (ref moveDist, slopeAngle, hit.normal);
                    moveDist.x += distToSlopeStart * directionX;
                }

                if (!collisions.climbingSlope || slopeAngle > maxSlopeAngle)
                {
                    moveDist.x = (hit.distance - skinWidth) * directionX;
                    rayLength = hit.distance;

                    if (collisions.climbingSlope)
                    {
                        moveDist.y = Mathf.Tan (collisions.slopeAngle * Mathf.Deg2Rad) * Mathf.Abs (moveDist.x);
                    }

                    collisions.left = directionX == -1;
                    collisions.right = directionX == 1;
                }
            }

            Debug.DrawRay (rayOrigin, Vector2.right * directionX * rayLength, Color.red);
        }
    }

    private void VerticalCollisions (ref Vector2 moveDist)
    {
        float directionY = Mathf.Sign (moveDist.y);
        float rayLength = Mathf.Abs (moveDist.y) + skinWidth;

        for (int i = 0; i < verticalRayCount; i++)
        {
            Vector2 rayOrigin = (directionY == -1) ? raycastOrigins.bottomLeft : raycastOrigins.topLeft;
            rayOrigin += Vector2.right * (verticalRaySpacing * i + moveDist.x);

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

                moveDist.y = (hit.distance - skinWidth) * directionY;
                rayLength = hit.distance;

                if (collisions.climbingSlope)
                {
                    moveDist.x = moveDist.y / Mathf.Tan (collisions.slopeAngle * Mathf.Deg2Rad) * Mathf.Sign (moveDist.x);
                }

                collisions.below = directionY == -1;
                collisions.above = directionY == 1;
            }

            Debug.DrawRay (rayOrigin, Vector2.up * directionY * rayLength, Color.red);
        }

        if (collisions.climbingSlope)
        {
            float directionX = Mathf.Sign (moveDist.x);
            rayLength = Mathf.Abs (moveDist.x) + skinWidth;
            Vector2 rayOrigin = ((directionX == -1) ? raycastOrigins.bottomLeft : raycastOrigins.bottomRight) + (Vector2.up * moveDist.y);

            RaycastHit2D hit = Physics2D.Raycast (rayOrigin, Vector2.right * directionX, rayLength, collisionMask);

            if (hit)
            {
                float slopeAngle = Vector2.Angle (hit.normal, Vector2.up);

                if (slopeAngle != collisions.slopeAngle)
                {
                    moveDist.x = (hit.distance - skinWidth) * directionX;
                    collisions.slopeAngle = slopeAngle;
                    collisions.slopeNormal = hit.normal;
                }
            }
        }
    }

    private void ClimbSlope (ref Vector2 moveDist, float slopeAngle, Vector2 slopeNormal)
    {
        float moveDistance = Mathf.Abs (moveDist.x);
        float climbVelocityY = Mathf.Sin (slopeAngle * Mathf.Deg2Rad) * moveDistance;
        if (moveDist.y <= climbVelocityY)
        {
            moveDist.y = climbVelocityY;
            moveDist.x = Mathf.Cos (slopeAngle * Mathf.Deg2Rad) * moveDistance * Mathf.Sign (moveDist.x);
            collisions.below = true;
            collisions.climbingSlope = true;
            collisions.slopeAngle = slopeAngle;
            collisions.slopeNormal = slopeNormal;
        }
    }

    private void DescendSlope (ref Vector2 moveDist)
    {
        RaycastHit2D maxSlopeHitLeft = Physics2D.Raycast (raycastOrigins.bottomLeft, Vector2.down, Mathf.Abs (moveDist.y) + skinWidth, collisionMask);
        RaycastHit2D maxSlopeHitRight = Physics2D.Raycast (raycastOrigins.bottomRight, Vector2.down, Mathf.Abs (moveDist.y) + skinWidth, collisionMask);
        
        if (maxSlopeHitLeft ^ maxSlopeHitRight)
        {
            SlideDownMaxSlope (maxSlopeHitLeft, ref moveDist);
            SlideDownMaxSlope (maxSlopeHitRight, ref moveDist);
        }


        if (!collisions.slidingDownSlope)
        {
            float directionX = Mathf.Sign (moveDist.x);
            Vector2 rayOrigin = (directionX == -1) ? raycastOrigins.bottomRight : raycastOrigins.bottomLeft;
            RaycastHit2D hit = Physics2D.Raycast (rayOrigin, -Vector2.up, Mathf.Infinity, collisionMask);

            if (hit)
            {
                float slopeAngle = Vector2.Angle (hit.normal, Vector2.up);

                if (slopeAngle != 0 && slopeAngle <= maxSlopeAngle)
                {
                    if (Mathf.Sign (hit.normal.x) == directionX)
                    {
                        if (hit.distance - skinWidth <= Mathf.Tan (slopeAngle * Mathf.Deg2Rad * Mathf.Abs (moveDist.x)))
                        {
                            float moveDistance = Mathf.Abs (moveDist.x);
                            float descendVelocityY = Mathf.Sin (slopeAngle * Mathf.Deg2Rad) * moveDistance;
                            moveDist.x = Mathf.Cos (slopeAngle * Mathf.Deg2Rad) * moveDistance * Mathf.Sign (moveDist.x);
                            moveDist.y -= descendVelocityY;

                            collisions.slopeAngle = slopeAngle;
                            collisions.descendingSlope = true;
                            collisions.below = true;
                            collisions.slopeNormal = hit.normal;
                        }
                    }
                }
            }
        }
    }

    private void SlideDownMaxSlope (RaycastHit2D hit, ref Vector2 moveDist)
    {
        if (hit)
        {
            float slopeAngle = Vector2.Angle (hit.normal, Vector2.up);

            if (slopeAngle > maxSlopeAngle)
            {
                moveDist.x = Mathf.Sign (hit.normal.x) * (Mathf.Abs (moveDist.y) - hit.distance) / (Mathf.Tan (slopeAngle * Mathf.Deg2Rad));
                collisions.slopeAngle = slopeAngle;
                collisions.slidingDownSlope = true;
                collisions.slopeNormal = hit.normal;
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
        public bool slidingDownSlope;
        public float slopeAngle, slopeAngleOld;
        public Vector2 slopeNormal;

        public Vector2 velocityOld;
        public int faceDirection;
        public bool isFallingThroughPlatform;

        public void Reset ()
        {
            above = below = false;
            left = right = false;
            climbingSlope = descendingSlope = false;
            slidingDownSlope = false;
            slopeAngleOld = slopeAngle;
            slopeAngle = 0f;
            slopeNormal = Vector2.zero;
        }

    }
}
