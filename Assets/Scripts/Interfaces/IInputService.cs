using UnityEngine;

public interface IInputService
{
    Vector2 GetTouchPosition();
    bool IsTouchDown();
    bool IsTouchUp();
    bool IsTouching();
}
