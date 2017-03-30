using UnityEngine;

[RequireComponent (typeof (Controller2D))]
public class Player : MonoBehaviour {

	private Controller2D controller;
    private float moveSpeed = 6f;
    private float jumpHeight = 4;
    private float timeToJumpApex = 0.4f;
    private float accelerationTimeAir = 0.2f;
    private float accelerationTimeGround = 0.1f;
    private float gravity;
    private float jumpVelocity;
    private float velocitySmoothingX;
    private Vector3 velocity;
    private float wallSlideSpeedMax = 3f;
    private Vector2 wallJumpClimb = new Vector2 (7.5f, 16f);
    private Vector2 wallJumpOff = new Vector2 (8.5f, 7f);
    private Vector2 wallJumpLeap = new Vector2 (18f, 17f);
    private float wallStickTime = 0.25f;
    private float timeToWallRelease;

    private void Awake ()
    {
        controller = GetComponent<Controller2D> ();
    }

    private void Start ()
    {
        gravity = -(2 * jumpHeight) / Mathf.Pow (timeToJumpApex, 2);
        jumpVelocity = Mathf.Abs (gravity) * timeToJumpApex;
    }

    private void Update ()
    {
        Vector2 input = new Vector2 (
            Input.GetAxisRaw ("Horizontal"),
            Input.GetAxisRaw ("Vertical")
        );

        int wallDirX = (controller.collisions.left) ? -1 : 1;

        float targetVelocityX = input.x * moveSpeed;
        velocity.x = Mathf.SmoothDamp (velocity.x, targetVelocityX, ref velocitySmoothingX, (controller.collisions.below) ? accelerationTimeGround : accelerationTimeAir);
        
        bool isWallSliding = false;
        if ((controller.collisions.left || controller.collisions.right) && !controller.collisions.below && velocity.y < 0)
        {
            isWallSliding = true;
            if (velocity.y < -wallSlideSpeedMax)
            {
                velocity.y = -wallSlideSpeedMax;
            }

            if (timeToWallRelease > 0)
            {
                velocitySmoothingX = 0;
                velocity.x = 0;

                if (Mathf.Sign (input.x) != wallDirX && input.x != 0)
                {
                    timeToWallRelease -= Time.deltaTime;
                }
                else
                {
                    timeToWallRelease = wallStickTime;
                }
            }
            else
            {
                timeToWallRelease = wallStickTime;
            }
        }

        if (controller.collisions.above || controller.collisions.below)
        {
            velocity.y = 0;
        }

        if (Input.GetKeyDown (KeyCode.Space))
        {
            if (isWallSliding)
            {
                if (wallDirX == Mathf.Sign (input.x))
                {
                    velocity.x = -wallDirX * wallJumpClimb.x;
                    velocity.y = wallJumpClimb.y;
                }
                else if (input.x == 0)
                {
                    velocity.x = -wallDirX * wallJumpOff.x;
                    velocity.y = wallJumpOff.y; 
                }
                else
                {
                    velocity.x = -wallDirX * wallJumpLeap.x;
                    velocity.y = wallJumpLeap.y;
                }
            }
            if (controller.collisions.below)
            {
                velocity.y = jumpVelocity;
            }
        }

        velocity.y += gravity * Time.deltaTime;
        controller.Move (velocity * Time.deltaTime);
    }

}
