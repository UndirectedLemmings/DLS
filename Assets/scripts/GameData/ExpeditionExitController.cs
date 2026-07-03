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
        if (exitPopupPanel != null) exitPopupPanel.SetActive(false);

        if (btnReturnToCity != null) btnReturnToCity.onClick.AddListener(ReturnToCity);
        if (btnContinue != null) btnContinue.onClick.AddListener(ContinueExpedition);
        if (btnEvacuate != null) btnEvacuate.onClick.AddListener(TryEvacuate);

        // Изначально кнопка эвакуации выключена (герой ведь только появился и начнет шагать)
        if (btnEvacuate != null) btnEvacuate.interactable = false;
    }

    // === НОВАЯ РЕАКТИВНАЯ ЛОГИКА ===

    /// <summary>
    /// Вызывается самим героем, когда он наступает на тайл старта
    /// </summary>
    public void OnSteppedOnStartTile()
    {
        // 1. Включаем возможность сбежать
        if (btnEvacuate != null) btnEvacuate.interactable = true;

        // 2. Если миссия выполнена и мы не в свободном режиме - сами показываем окно победы!
        if (isObjectiveCompleted && !continueMode)
        {
            if (exitPopupPanel != null) exitPopupPanel.SetActive(true);
        }
    }

    /// <summary>
    /// Вызывается самим героем, когда он сходит со стартового тайла
    /// </summary>
    public void OnLeftStartTile()
    {
        // 1. Выключаем эвакуацию
        if (btnEvacuate != null) btnEvacuate.interactable = false;

        // 2. На всякий случай прячем окно победы (вдруг игрок нажал "Продолжить" или просто ушел)
        if (exitPopupPanel != null) exitPopupPanel.SetActive(false);
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
        if (GameManager.Instance != null) GameManager.Instance.FinishExpedition(false);
        SceneManager.LoadScene(citySceneName);
    }

    private void ReturnToCity()
    {
        Debug.Log("<color=lime>[ЭКСПЕДИЦИЯ]</color> Завершаем поход и возвращаемся в город...");
        if (GameManager.Instance != null) GameManager.Instance.FinishExpedition(true);
        SceneManager.LoadScene(citySceneName);
    }

    private void ContinueExpedition()
    {
        Debug.Log("<color=orange>[ЭКСПЕДИЦИЯ]</color> Включаем свободный режим.");
        if (exitPopupPanel != null) exitPopupPanel.SetActive(false);
        continueMode = true;
    }
}