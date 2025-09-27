using UnityEngine;

public class WeaponSway : MonoBehaviour
{
    [SerializeField] private float swayAmount;
    [SerializeField] private float smoothTime;
    [SerializeField] private Vector3 offsetRotation;
    void Update()
    {
        float mouseX = Input.GetAxis("Mouse X") * swayAmount;
        float mouseY = Input.GetAxis("Mouse Y") * swayAmount;

        Quaternion rotationX = Quaternion.AngleAxis(mouseY, Vector3.right);
        Quaternion rotationY = Quaternion.AngleAxis(mouseX, Vector3.up);
        Quaternion offset = Quaternion.Euler(offsetRotation);

        Quaternion targetRot = (rotationX * rotationY)*offset;

        transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRot, smoothTime *Time.deltaTime);
    }
}
