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
        bool isWallSliding = false;
        if ((controller.collisions.left || controller.collisions.right) && !controller.collisions.below && velocity.y < 0)
        {
            isWallSliding = true;
            if (velocity.y < -wallSlideSpeedMax)
            {
                velocity.y = -wallSlideSpeedMax;
            }
        }

        if (controller.collisions.above || controller.collisions.below)
        {
            velocity.y = 0;
        }

        Vector2 input = new Vector2 (
            Input.GetAxisRaw ("Horizontal"),
            Input.GetAxisRaw ("Vertical")
        );

        if (Input.GetKeyDown (KeyCode.Space) && controller.collisions.below)
        {
            velocity.y = jumpVelocity;
        }

        float targetVelocityX = input.x * moveSpeed;
        velocity.x = Mathf.SmoothDamp (velocity.x, targetVelocityX, ref velocitySmoothingX, (controller.collisions.below) ? accelerationTimeGround : accelerationTimeAir);
        velocity.y += gravity * Time.deltaTime;
        controller.Move (velocity * Time.deltaTime);
    }

}
