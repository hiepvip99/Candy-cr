using UnityEngine;

public class Candy : MonoBehaviour
{
    public int X { get; private set; }
    public int Y { get; private set; }
    //public string TypeTag { get; private set; }

    public void Init(int x, int y)
    {
        X = x;
        Y = y;
        //TypeTag = typeTag;
        //gameObject.tag = typeTag; // Gán tag của GameObject cho khớp
        // Đặt tên GameObject cho dễ debug
        gameObject.name = $"Candy_{X}_{Y}_{gameObject.tag}";
    }

    public void UpdatePosition(int newX, int newY)
    {
        X = newX;
        Y = newY;
        gameObject.name = $"Candy_{X}_{Y}_{gameObject.tag}"; // Cập nhật tên
    }
}
