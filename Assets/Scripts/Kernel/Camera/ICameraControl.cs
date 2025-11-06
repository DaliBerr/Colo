using UnityEngine;

namespace Kernel
{
    public abstract class CameraControl: MonoBehaviour
    {
        public float panSpeed = 20f;
        public float zoomSpeed = 2f;
        public float minZoom = 5f;
        public float maxZoom = 50f;

        // Provide the Camera instance from subclasses via this abstract property.
        public abstract Camera CameraComponent { get; set; }

        // Subclasses can implement the property like:
        // protected override Camera CameraComponent => GetComponentInChildren<Camera>();

        protected virtual void HandlePan()
        {
            if (Input.GetKey(KeyCode.LeftShift))
            {
                panSpeed *= 1.8f;
            }
            float moveX = Input.GetAxis("Horizontal") * panSpeed * Time.deltaTime;
            float moveY = Input.GetAxis("Vertical") * panSpeed * Time.deltaTime;


            transform.Translate(new Vector3(moveX, moveY, 0));
        }

        protected virtual void HandleZoom()
        {
            panSpeed = Mathf.Lerp(10f, 50f, (CameraComponent.orthographicSize - minZoom) / (maxZoom - minZoom));
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll != 0f)
            {
                float newSize = CameraComponent.orthographicSize - scroll * zoomSpeed;
                CameraComponent.orthographicSize = Mathf.Clamp(newSize, minZoom, maxZoom);
            }
        }
    }


}