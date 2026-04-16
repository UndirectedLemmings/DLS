using UnityEngine;
using UnityEngine.UI;

public class Click_object : MonoBehaviour
{
    public Text text;

    public GameObject road_mod;
    
    void OnMouseDown()
    {
        text.GetComponent<Text>().text = "Button was clicked!";
    }
}
