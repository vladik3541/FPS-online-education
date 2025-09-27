using UnityEngine;

public interface IInputHandler
{
    Vector2 GetMouseInput();
    Vector2 GetMovementInput();
    bool IsJumpPressed();
    bool IsSprintPressed();
}