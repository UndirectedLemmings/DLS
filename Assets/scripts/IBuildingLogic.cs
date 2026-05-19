using UnityEngine;

// Интерфейс для всех интерактивных зданий
public interface IBuildingLogic
{
    // Метод инициализации, в который мы передаем координаты постройки
    void InitializeAt(Vector2Int cellPosition);
}