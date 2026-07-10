using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ExpeditionExitController : MonoBehaviour
{
    [Header("Настройки")]
    public string citySceneName = "CityScene";
    public bool isObjectiveCompleted = false;
    private bool continueMode = false;

    [Header("UI Элементы")]
    public GameObject exitPopupPanel;
    public Button btnReturnToCity;
    public Button btnContinue;
    public Button btnEvacuate;

    private void Start()
    {
        // Изначально скрываем панель и выключаем паузу (на случай если она была включена)
        SetPopupState(false);

        if (btnReturnToCity != null) btnReturnToCity.onClick.AddListener(ReturnToCity);
        if (btnContinue != null) btnContinue.onClick.AddListener(ContinueExpedition);
        if (btnEvacuate != null) btnEvacuate.onClick.AddListener(TryEvacuate);

        // Изначально кнопка эвакуации выключена (герой ведь только появился и начнет шагать)
        if (btnEvacuate != null) btnEvacuate.interactable = false;

        // Подписываемся на событие выполнения миссии
        GameManager.OnMissionCompleted += CompleteObjective;
    }

    private void OnDestroy()
    {
        // Отписываемся при уничтожении объекта
        GameManager.OnMissionCompleted -= CompleteObjective;
    }

    // === НОВЫЙ МЕТОД ДЛЯ УПРАВЛЕНИЯ ОКНОМ И ПАУЗОЙ ===
    private void SetPopupState(bool isVisible)
    {
        if (exitPopupPanel != null)
            exitPopupPanel.SetActive(isVisible);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.isMapPaused = isVisible;
            Debug.Log(isVisible ? "[UI] Пауза включена: окно выхода открыто" : "[UI] Пауза выключена: окно выхода закрыто");
        }
    }

    // === НОВАЯ РЕАКТИВНАЯ ЛОГИКА ===

    /// <summary>
    /// Вызывается самим героем, когда он наступает на тайл старта
    /// </summary>
    public void OnSteppedOnStartTile()
    {
        // 1. Включаем возможность сбежать
        if (btnEvacuate != null) btnEvacuate.interactable = true;

        // 2. Если миссия выполнена и мы не в свободном режиме - сами показываем окно победы и ставим паузу!
        if (GameManager.Instance.IsMissionCompleted && !continueMode)
        {
            SetPopupState(true);
        }
    }

    /// <summary>
    /// Вызывается самим героем, когда он сходит со стартового тайла
    /// </summary>
    public void OnLeftStartTile()
    {
        // 1. Выключаем эвакуацию
        if (btnEvacuate != null) btnEvacuate.interactable = false;

        // 2. На всякий случай прячем окно победы и снимаем с паузы
        SetPopupState(false);
    }

    // ===================================

    public void CompleteObjective()
    {
        if (isObjectiveCompleted) return;

        isObjectiveCompleted = true;
        Debug.Log("<color=yellow>[ЭКСПЕДИЦИЯ]</color> Цель выполнена! Возвращайтесь на стартовую клетку.");
    }

    private void TryEvacuate()
    {
        Debug.Log("<color=red>[ЭКСПЕДИЦИЯ]</color> Эвакуация! Отряд отступает.");

        // Обязательно снимаем игру с паузы перед переходом на другую сцену
        SetPopupState(false);

        if (GameManager.Instance != null) GameManager.Instance.FinishExpedition(false);
        SceneManager.LoadScene(citySceneName);
    }

    private void ReturnToCity()
    {
        Debug.Log("<color=lime>[ЭКСПЕДИЦИЯ]</color> Завершаем поход и возвращаемся в город...");

        // Обязательно снимаем игру с паузы перед переходом на другую сцену
        SetPopupState(false);

        if (GameManager.Instance != null) GameManager.Instance.FinishExpedition(true);
        SceneManager.LoadScene(citySceneName);
    }

    private void ContinueExpedition()
    {
        Debug.Log("<color=orange>[ЭКСПЕДИЦИЯ]</color> Включаем свободный режим.");
        continueMode = true;

        // Закрываем окно и возвращаем ход игре
        SetPopupState(false);
    }
}