using UnityEngine;
using System.Collections.Generic;

public class PlatformController : RaycastController {

    [SerializeField]
    private LayerMask passengerMask;
    [SerializeField]
    private Vector3[] localWaypoints;
    [SerializeField]
    private float speed;
    [SerializeField]
    private bool isCyclic;
    [SerializeField]
    private float waitTime;

    private List<PassengerMovement> passengerMovement;
    private Dictionary<Transform, Controller2D> passengerDictionary = new Dictionary<Transform, Controller2D> ();
    private Vector3[] globalWaypoints;
    private int fromWaypointIndex;
    private float percentBetweenWaypoints;
    private float nextMoveTime;

	protected override void Start ()
    {
        base.Start ();
        SetGlobalWaypoints ();
    }

    private void Update ()
    {
        UpdateRaycastOrigins ();

        Vector3 velocity = CalculatePlatformMovement ();

        CalculatePassengerMovement (velocity);

        MovePassengers (true);

        transform.Translate (velocity);

        MovePassengers (false);
    }

    private Vector3 CalculatePlatformMovement ()
    {
        if (Time.time < nextMoveTime)
        {
            return Vector3.zero;
        }

        fromWaypointIndex %= globalWaypoints.Length;
        int toWaypointIndex = (fromWaypointIndex + 1) % globalWaypoints.Length;
        float distBetweenWaypoints = Vector3.Distance (globalWaypoints [fromWaypointIndex], globalWaypoints [toWaypointIndex]);
        percentBetweenWaypoints += Time.deltaTime * speed / distBetweenWaypoints;

        Vector3 newPos = Vector3.Lerp (globalWaypoints [fromWaypointIndex], globalWaypoints [toWaypointIndex], percentBetweenWaypoints);

        if (percentBetweenWaypoints >= 1)
        {
            percentBetweenWaypoints = 0;
            fromWaypointIndex++;
            if (!isCyclic)
            {
                if (fromWaypointIndex >= globalWaypoints.Length - 1)
                {
                    fromWaypointIndex = 0;
                    System.Array.Reverse (globalWaypoints);
                }
            }
            nextMoveTime = Time.time + waitTime;
        }
        return newPos - transform.position;
    }

    private void MovePassengers (bool beforeMovePlatform)
    {
        foreach (PassengerMovement passenger in passengerMovement)
        {
            if (!passengerDictionary.ContainsKey (passenger.transform))
            {
                passengerDictionary.Add (passenger.transform, passenger.transform.GetComponent<Controller2D> ());
            }
            if (passenger.isMovedBeforePlatform == beforeMovePlatform)
            {
                passengerDictionary [passenger.transform].Move (passenger.velocity, passenger.isStandingOnPlatform);
            }
        }
    }

    private void CalculatePassengerMovement (Vector3 velocity)
    {
        HashSet<Transform> movedPassengers = new HashSet<Transform> ();

        passengerMovement = new List<PassengerMovement> ();

        float directionX = Mathf.Sign (velocity.x);
        float directionY = Mathf.Sign (velocity.y);

        // Vertically moving platform
        if (velocity.y != 0)
        {
            float rayLength = Mathf.Abs (velocity.y) + skinWidth;

            for (int i = 0; i < verticalRayCount; i++)
            {
                Vector2 rayOrigin = (directionY == -1) ? raycastOrigins.bottomLeft : raycastOrigins.topLeft;
                rayOrigin += Vector2.right * (verticalRaySpacing * i);

                RaycastHit2D hit = Physics2D.Raycast (rayOrigin, Vector2.up * directionY, rayLength, passengerMask);

                if (hit)
                {
                    if (!movedPassengers.Contains (hit.transform))
                    {
                        movedPassengers.Add (hit.transform);

                        float pushX = (directionY == 1) ? velocity.x : 0;
                        float pushY = velocity.y - (hit.distance - skinWidth) * directionY;

                        passengerMovement.Add (new PassengerMovement (hit.transform, new Vector3 (pushX, pushY), directionY == 1, true));
                    }
                }
            }
        }

        // Horizontally moving platform
        if (velocity.x != 0)
        {
            float rayLength = Mathf.Abs (velocity.x) + skinWidth;

            for (int i = 0; i < horizontalRayCount; i++)
            {
                Vector2 rayOrigin = (directionX == -1) ? raycastOrigins.bottomLeft : raycastOrigins.bottomRight;
                rayOrigin += Vector2.up * (horizontalRaySpacing * i);

                RaycastHit2D hit = Physics2D.Raycast (rayOrigin, Vector2.right * directionX, rayLength, passengerMask);

                if (hit)
                {
                    if (!movedPassengers.Contains (hit.transform))
                    {
                        movedPassengers.Add (hit.transform);

                        float pushX = velocity.x - (hit.distance - skinWidth) * directionX;
                        float pushY = -skinWidth;

                        passengerMovement.Add (new PassengerMovement (hit.transform, new Vector3 (pushX, pushY), false, true));
                    }
                }
            }
        }

        // Passenger on top of horizontally/downward moving platform
        if (directionY == -1 || velocity.y == 0 && velocity.x != 0)
        {
            float rayLength = skinWidth * 2;

            for (int i = 0; i < verticalRayCount; i++)
            {
                Vector2 rayOrigin = raycastOrigins.topLeft + Vector2.right * (verticalRaySpacing * i);

                RaycastHit2D hit = Physics2D.Raycast (rayOrigin, Vector2.up, rayLength, passengerMask);

                if (hit)
                {
                    if (!movedPassengers.Contains (hit.transform))
                    {
                        movedPassengers.Add (hit.transform);

                        float pushX = velocity.x;
                        float pushY = velocity.y;

                        passengerMovement.Add (new PassengerMovement (hit.transform, new Vector3 (pushX, pushY), true, false));
                    }
                }
            }
        }
    }

    private void SetGlobalWaypoints ()
    {
        globalWaypoints = new Vector3 [localWaypoints.Length];
        for (int i = 0; i < globalWaypoints.Length; i++)
        {
            globalWaypoints [i] = localWaypoints [i] + transform.position;
        }
    }

    private void OnDrawGizmos ()
    {
        if (localWaypoints != null)
        {
            Gizmos.color = Color.red;
            float size = 0.3f;

            for (int i = 0; i < localWaypoints.Length; i++)
            {
                Vector3 globalPosition = (Application.isPlaying) ? globalWaypoints[i] : localWaypoints[i] + transform.position;
                Gizmos.DrawLine (globalPosition - Vector3.up * size, globalPosition + Vector3.up * size);
                Gizmos.DrawLine (globalPosition - Vector3.left * size, globalPosition + Vector3.left * size);
            }
        }
    }

    private struct PassengerMovement {
        public Transform transform;
        public Vector3 velocity;
        public bool isStandingOnPlatform;
        public bool isMovedBeforePlatform;

        public PassengerMovement (Transform _transform, Vector3 _velocity, bool _isStandingOnPlatform, bool _isMovedBeforePlatform)
        {
            transform = _transform;
            velocity = _velocity;
            isStandingOnPlatform = _isStandingOnPlatform;
            isMovedBeforePlatform = _isMovedBeforePlatform;
        }
    }

}
