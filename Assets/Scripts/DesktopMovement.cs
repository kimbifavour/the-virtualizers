
using UnityEngine;
using UnityEngine.InputSystem;

public class DesktopMovement : MonoBehaviour
{
    public Transform head;
    public Transform leftController;
    public Transform rightController;

    public float moveSpeed = 4f;
    public float mouseSensitivity = 0.15f;

    private float pitch;

    void Update()
    {
        if (Keyboard.current == null || head == null) return;

        // WASD movement
        Vector3 forward = head.forward;
        forward.y = 0;
        forward.Normalize();

        Vector3 right = head.right;
        right.y = 0;
        right.Normalize();

        Vector3 move = Vector3.zero;

        if (Keyboard.current.wKey.isPressed) move += forward;
        if (Keyboard.current.sKey.isPressed) move -= forward;
        if (Keyboard.current.dKey.isPressed) move += right;
        if (Keyboard.current.aKey.isPressed) move -= right;

        transform.position += move.normalized * moveSpeed * Time.deltaTime;

        // Mouse look
        if (Mouse.current != null)
        {
            Vector2 delta = Mouse.current.delta.ReadValue();

            transform.Rotate(Vector3.up, delta.x * mouseSensitivity);

            pitch = Mathf.Clamp(
                pitch - delta.y * mouseSensitivity,
                -80f, 80f
            );

            head.localRotation = Quaternion.Euler(pitch, 0, 0);
        }
    }

    void LateUpdate()
    {
        if (head == null) return;

        // Desktop hand positions
        if (leftController != null)
        {
            leftController.position =
                head.TransformPoint(new Vector3(-0.3f, -0.3f, 0.6f));

            leftController.rotation = head.rotation;
        }

        if (rightController != null)
        {
            rightController.position =
                head.TransformPoint(new Vector3(0.3f, -0.3f, 0.6f));

            rightController.rotation = head.rotation;
        }
    }
}
