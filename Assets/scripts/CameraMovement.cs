using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    [Header("Настройки движения")]
    public float speed = 10f;

    [Header("Настройки масштаба")]
    public float maxOrthographicSize = 15f;

    // Внутренние переменные
    private bool canMove = false;
    private Camera cam;

    // Переменные для хранения вычисленных границ (запирание камеры)
    private float limitMinX;
    private float limitMaxX;
    private float limitMinY;
    private float limitMaxY;

    public void SetupCameraForMap(int mapWidth, int mapHeight)
    {
        cam = GetComponent<Camera>();
        if (cam == null || !cam.orthographic) return;

        // 1. Вычисляем центр карты и ставим туда камеру
        float centerX = mapWidth / 2f;
        float centerY = mapHeight / 2f;
        transform.position = new Vector3(centerX, centerY, -10f);

        // 2. Рассчитываем идеальный размер под экран
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

        // 3. Принимаем решение: блокируем или разрешаем движение
        if (requiredSize > maxOrthographicSize)
        {
            // Карта большая — включаем управление
            cam.orthographicSize = maxOrthographicSize;
            canMove = true;

            // --- РАСЧЕТ ГРАНИЦ ДЛЯ ЗАПИРАНИЯ КАМЕРЫ ---
            // cam.orthographicSize — это половина высоты экрана
            float camHeight = cam.orthographicSize;
            // Умножаем на соотношение сторон, чтобы получить половину ширины экрана
            float camWidth = cam.orthographicSize * cam.aspect;

            // Устанавливаем границы с учетом отступа (padding), чтобы края карты смотрелись красиво
            // Предполагается, что карта генерируется от координат (0,0) до (mapWidth, mapHeight)
            limitMinX = camWidth - padding;
            limitMaxX = mapWidth - camWidth + padding;

            limitMinY = camHeight - padding;
            limitMaxY = mapHeight - camHeight + padding;

            // Защита от инверсии (если карта длинная, но узкая)
            if (limitMinX > limitMaxX) limitMinX = limitMaxX = centerX;
            if (limitMinY > limitMaxY) limitMinY = limitMaxY = centerY;
        }
        else
        {
            // Карта полностью поместилась — блокируем движение
            cam.orthographicSize = requiredSize;
            canMove = false;
        }
    }

    void Update()
    {
        if (!canMove) return;

        // 1. Получаем ввод и двигаем камеру
        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");

        Vector3 movement = new Vector3(horizontalInput, verticalInput, 0f) * speed * Time.deltaTime;
        transform.Translate(movement);

        // 2. ЗАПИРАНИЕ КАМЕРЫ (Clamping)
        // Mathf.Clamp не даст координате X и Y выйти за пределы наших вычисленных лимитов
        float clampedX = Mathf.Clamp(transform.position.x, limitMinX, limitMaxX);
        float clampedY = Mathf.Clamp(transform.position.y, limitMinY, limitMaxY);

        // Применяем запертые координаты, сохраняя Z равным -10 (чтобы камера не провалилась под текстуры)
        transform.position = new Vector3(clampedX, clampedY, transform.position.z);
    }
}