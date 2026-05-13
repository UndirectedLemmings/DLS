using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    [Header("Настройки движения")]
    public float speed = 10f;

    [Header("Настройки масштаба")]
    // Максимальный размер камеры (отдаление). 
    // Если поставить больше, клетки станут слишком мелкими. Настрой в Инспекторе!
    public float maxOrthographicSize = 15f;

    // Внутренний флаг: разрешено ли игроку двигать камеру
    private bool canMove = false;

    /// <summary>
    /// Этот метод нужно вызвать из генератора карты (FILL_MAP_v4),
    /// передав ему итоговую ширину и высоту сетки.
    /// </summary>
    public void SetupCameraForMap(int mapWidth, int mapHeight)
    {
        Camera cam = GetComponent<Camera>();
        if (cam == null || !cam.orthographic) return;

        // 1. Вычисляем центр карты
        float centerX = mapWidth / 2f;
        float centerY = mapHeight / 2f;

        // Ставим камеру в центр
        transform.position = new Vector3(centerX, centerY, -10f);

        // 2. Рассчитываем идеальный размер под экран игрока
        float screenRatio = (float)Screen.width / (float)Screen.height;
        float targetRatio = (float)mapWidth / (float)mapHeight;
        float padding = 2f;

        float requiredSize;

        if (screenRatio >= targetRatio)
        {
            requiredSize = (mapHeight / 2f) + padding;
        }
        else
        {
            float differenceInSize = targetRatio / screenRatio;
            requiredSize = ((mapHeight / 2f) * differenceInSize) + padding;
        }

        // 3. Принимаем решение: двигаем или стоим?
        if (requiredSize > maxOrthographicSize)
        {
            // Карта ОГРОМНАЯ. Если мы отдалимся на requiredSize, ничего не будет видно.
            // Фиксируем максимальный зум и разрешаем игроку "ездить" по карте.
            cam.orthographicSize = maxOrthographicSize;
            canMove = true;
        }
        else
        {
            // Карта отлично влезает в экран. 
            // Отдаляемся на нужный размер и блокируем WASD.
            cam.orthographicSize = requiredSize;
            canMove = false;
        }
    }

    void Update()
    {
        // Если карта влезла целиком, игнорируем нажатия кнопок
        if (!canMove) return;

        // Получаем ввод от клавиш WASD / Стрелочек
        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");

        // Вычисляем направление и применяем движение
        Vector3 movement = new Vector3(horizontalInput, verticalInput, 0f) * speed * Time.deltaTime;
        transform.Translate(movement);

        // Примечание: в будущем здесь можно добавить Math.Clamp, 
        // чтобы камера не улетала за границы карты (за пределы 0 и mapWidth/Height)
    }
}