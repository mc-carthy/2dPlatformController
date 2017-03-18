using UnityEngine;

[RequireComponent (typeof (Controller2D))]
public class Player : MonoBehaviour {

	private Controller2D controller;
    private float gravity = -20f;
    private float moveSpeed = 6f;
    private Vector3 velocity;

    private void Awake ()
    {
        controller = GetComponent<Controller2D> ();
    }

    private void Update ()
    {
        if (controller.collisions.above || controller.collisions.below)
        {
            velocity.y = 0;
        }
        
        Vector2 input = new Vector2 (
            Input.GetAxisRaw ("Horizontal"),
            Input.GetAxisRaw ("Vertical")
        );

        velocity.x = input.x * moveSpeed;
        velocity.y += gravity * Time.deltaTime;
        controller.Move (velocity * Time.deltaTime);
    }

}
