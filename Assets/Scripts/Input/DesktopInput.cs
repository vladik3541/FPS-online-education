using UnityEngine;

public class DesktopInput : IInputHandler
{
    public Vector2 GetMouseInput()
    {
        return new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));
    }

    public Vector2 GetMovementInput()
    {
        return new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
    }

    public bool IsJumpPressed()
    {
        return Input.GetButtonDown("Jump");
    }

    public bool IsSprintPressed()
    {
        return Input.GetKey(KeyCode.LeftShift);
    }
}
