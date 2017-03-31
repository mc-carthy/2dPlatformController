using UnityEngine;

public class CameraController : MonoBehaviour {

	private Controller2D target;
    private Bounds targetBounds;
    private Vector2 focusAreaSize = new Vector2 (3f, 5f);
    private FocusArea focusArea;
    private float verticalOffset = 1f;
    private float lookAheadDistX = 4f;
    private float lookSmoothTimeX = 0.5f;
    private float lookSmoothTimeY = 0.1f;
    private float currentLookAheadX;
    private float targetLookAheadX;
    private float lookAheadDirectionX;
    private float smoothLookVelocityX;
    private float smoothLookVelocityY;
    private bool isLookAheadStopped;

    private void Awake ()
    {
        target = GameObject.FindGameObjectWithTag ("Player").GetComponent<Controller2D> ();
    }

    private void Start ()
    {
        focusArea = new FocusArea (targetBounds, focusAreaSize);
    }

    private void LateUpdate ()
    {
        focusArea.Update (target.Collider.bounds);

        Vector2 focusPosition = focusArea.centre + Vector2.up * verticalOffset;

        if (focusArea.velocity.x != 0)
        {
            lookAheadDirectionX = Mathf.Sign (focusArea.velocity.x);
            if (Mathf.Sign (target.PlayerInput.x) == Mathf.Sign (focusArea.velocity.x) && target.PlayerInput.x != 0)
            {
                isLookAheadStopped = false;
                targetLookAheadX = lookAheadDirectionX * lookAheadDistX;
            }
            else
            {
                if (!isLookAheadStopped)
                {
                    isLookAheadStopped = true;
                    targetLookAheadX = currentLookAheadX + (lookAheadDirectionX * lookAheadDistX - currentLookAheadX) * 0.25f;
                }
            }
        }

        currentLookAheadX = Mathf.SmoothDamp (currentLookAheadX, targetLookAheadX, ref smoothLookVelocityX, lookSmoothTimeX);

        focusPosition.y = Mathf.SmoothDamp (transform.position.y, focusPosition.y, ref smoothLookVelocityY, lookSmoothTimeY);
        focusPosition += Vector2.right * currentLookAheadX;

        transform.position = (Vector3) focusPosition + Vector3.forward * -10f;
    }

    private void OnDrawGizmos ()
    {
        Gizmos.color = new Color (1, 0, 0, 0.5f);
        Gizmos.DrawCube (focusArea.centre, focusAreaSize);
    }

    private struct FocusArea {
        public Vector2 centre;
        public Vector2 velocity;
        private float left, right;
        private float top, bottom;

        public FocusArea (Bounds targetBounds, Vector2 size)
        {
            left = targetBounds.center.x - size.x / 2f;
            right = targetBounds.center.x + size.x / 2f;
            top = targetBounds.min.y + size.y;
            bottom = targetBounds.min.y;

            centre = new Vector2 ((left + right) / 2f, (top + bottom) / 2f);
            velocity = Vector2.zero;
        }

        public void Update (Bounds targetBounds)
        {
            float shiftX = 0f;
            if (targetBounds.min.x < left)
            {
                shiftX = targetBounds.min.x - left;
            }
            else if (targetBounds.max.x > right)
            {
                shiftX = targetBounds.max.x - right;
            }

            left += shiftX;
            right += shiftX;

            float shiftY = 0f;
            if (targetBounds.min.y < bottom)
            {
                shiftY = targetBounds.min.y - bottom;
            }
            else if (targetBounds.max.y > top)
            {
                shiftY = targetBounds.max.y - top;
            }

            top += shiftY;
            bottom += shiftY;

            centre = new Vector2 ((left + right) / 2f, (top + bottom) / 2f);
            velocity = new Vector2 (shiftX, shiftY);
        }
    }

}
