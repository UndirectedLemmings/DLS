using UnityEngine;

public class ClickableObject : MonoBehaviour
{
    Vector3 test;

    void OnMouseDown()
    {
        Debug.Log("Объект был кликнут!");
        test = GetComponent<Transform>().position;
        Debug.Log("координаты:" + test.x + "/" + test.y);
    }
}