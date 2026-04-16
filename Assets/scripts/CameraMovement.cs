using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    public float speed = 5f; // Скорость движения

    void Update()
    {
        // Получаем ввод от клавиш WASD
        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");

        // Вычисляем направление движения
        Vector3 movement = new Vector3(horizontalInput, verticalInput, 0f) * speed * Time.deltaTime;

        // Применяем движение к камере
        transform.Translate(movement);
    }
}
