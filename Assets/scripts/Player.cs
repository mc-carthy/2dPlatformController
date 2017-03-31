using UnityEngine;

[RequireComponent (typeof (Controller2D))]
public class Player : MonoBehaviour {

	private Controller2D controller;
    private float moveSpeed = 6f;
    private float maxJumpHeight = 4f;
    private float minJumpHeight = 1f;
    private float timeToJumpApex = 0.4f;
    private float accelerationTimeAir = 0.2f;
    private float accelerationTimeGround = 0.1f;
    private float gravity;
    private float maxJumpVelocity;
    private float minJumpVelocity;
    private float velocitySmoothingX;
    private Vector3 velocity;
    private float wallSlideSpeedMax = 3f;
    private Vector2 wallJumpClimb = new Vector2 (7.5f, 16f);
    private Vector2 wallJumpOff = new Vector2 (8.5f, 7f);
    private Vector2 wallJumpLeap = new Vector2 (18f, 17f);
    private float wallStickTime = 0.25f;
    private float timeToWallRelease;
    private Vector2 directionalInput;
    private bool isWallSliding;
    private int wallDirX;


    private void Awake ()
    {
        controller = GetComponent<Controller2D> ();
    }

    private void Start ()
    {
        gravity = -(2 * maxJumpHeight) / Mathf.Pow (timeToJumpApex, 2);
        maxJumpVelocity = Mathf.Abs (gravity) * timeToJumpApex;
        minJumpVelocity = Mathf.Sqrt (2 * Mathf.Abs (gravity) * minJumpHeight);
    }

    private void Update ()
    {
        CalculateVelocity ();
        HandleWallSliding ();

        controller.Move (velocity * Time.deltaTime, directionalInput);

        if (controller.collisions.above || controller.collisions.below)
        {
            velocity.y = 0;
        }
    }

    public void SetDirectionalInput (Vector2 input)
    {
        directionalInput = input;
    }

    public void OnJumpInputDown ()
    {
        if (isWallSliding)
        {
            if (wallDirX == Mathf.Sign (directionalInput.x))
            {
                velocity.x = -wallDirX * wallJumpClimb.x;
                velocity.y = wallJumpClimb.y;
            }
            else if (directionalInput.x == 0)
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
            velocity.y = maxJumpVelocity;
        }
    }

    public void OnJumpInputUp ()
    {
        if (velocity.y > minJumpVelocity)
        {
            velocity.y = minJumpVelocity;
        } 
    }

    private void CalculateVelocity ()
    {
        float targetVelocityX = directionalInput.x * moveSpeed;
        velocity.x = Mathf.SmoothDamp (velocity.x, targetVelocityX, ref velocitySmoothingX, (controller.collisions.below) ? accelerationTimeGround : accelerationTimeAir);
        velocity.y += gravity * Time.deltaTime;
    }

    private void HandleWallSliding ()
    {
        wallDirX = (controller.collisions.left) ? -1 : 1;
        isWallSliding = false;
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

                if (Mathf.Sign (directionalInput.x) != wallDirX && directionalInput.x != 0)
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
    }

}
