using UnityEngine;

[RequireComponent (typeof (Controller2D))]
public class Player : MonoBehaviour {

	private Controller2D controller;
    private float gravity = -20f;
    private Vector3 velocity;

    private void Awake ()
    {
        controller = GetComponent<Controller2D> ();
    }

    private void Update ()
    {
        velocity.y += gravity * Time.deltaTime;
        controller.Move (velocity * Time.deltaTime);
    }

}
