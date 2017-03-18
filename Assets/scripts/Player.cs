using UnityEngine;

[RequireComponent (typeof (Controller2D))]
public class Player : MonoBehaviour {

	private Controller2D controller;

    private void Awake ()
    {
        controller = GetComponent<Controller2D> ();
    }

}
