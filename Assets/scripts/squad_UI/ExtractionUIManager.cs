using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ExtractionUIManager : MonoBehaviour
{
    [Header("Кнопки на основном экране")]
    public Button evacuateButton;         // Кнопка "Сбежать" (Красная)
    public Button finishMissionButton;    // Кнопка "Завершить миссию" (Появляется, когда миссия готова)
    public TMP_Text statusText;           // Текст для ошибок ("Вы не на старте!")

    [Header("Всплывающее окно после выполнения")]
    public GameObject choicePanel;        // Панель с выбором
    public Button exitWithLootButton;     // Уйти с победой
    public Button stayAndRiskButton;      // Остаться

    // Ссылка на контроллер перемещения героя
    public Character_move playerCharacter;

    void Start()
    {
        evacuateButton.onClick.AddListener(TryEvacuate);
        finishMissionButton.onClick.AddListener(TryFinishMission);
        exitWithLootButton.onClick.AddListener(ConfirmSuccessExit);
        stayAndRiskButton.onClick.AddListener(StayInExpedition);

        choicePanel.SetActive(false);
    }

    void Update()
    {
        // Кнопка "Завершить миссию" активна только если шкала прогресса заполнена
        if (GameManager.Instance != null)
        {
            finishMissionButton.interactable = GameManager.Instance.IsMissionCompleted;
        }
    }

    // Проверка: стоит ли герой на стартовом тайле
    private bool IsPlayerOnStartTile()
    {
        // Если у тебя переменная в Character_move называется иначе (например, currentCell), замени targetPosition на неё.
        return playerCharacter.currentPosition == GameManager.Instance.startTilePosition;
    }

    // Нажатие на "Эвакуация"
    public void TryEvacuate()
    {
        if (IsPlayerOnStartTile())
        {
            Debug.Log("ЭВАКУАЦИЯ! Миссия провалена, отряд сбежал.");
            // Тут код возврата в город с потерей части лута или без награды
            // GameManager.Instance.ReturnToCity(success: false);
        }
        else
        {
            ShowStatus("Вы должны находиться на стартовом тайле для эвакуации!");
        }
    }

    // Нажатие на "Завершить миссию"
    public void TryFinishMission()
    {
        if (IsPlayerOnStartTile())
        {
            if (GameManager.Instance.IsMissionCompleted)
            {
                choicePanel.SetActive(true); // Открываем окно выбора
            }
        }
        else
        {
            ShowStatus("Вы должны находиться на стартовом тайле, чтобы сдать миссию!");
        }
    }

    // Выбор в панели: Уйти
    public void ConfirmSuccessExit()
    {
        Debug.Log("ПОБЕДА! Возвращаемся в город с полной добычей.");
        choicePanel.SetActive(false);
        // GameManager.Instance.ReturnToCity(success: true);
    }

    // Выбор в панели: Остаться
    public void StayInExpedition()
    {
        Debug.Log("Отряд решает остаться и рискнуть.");
        choicePanel.SetActive(false);
        // Можно скрыть кнопку завершения или позволить нажать её снова позже, если герой вернется на старт.
    }

    private void ShowStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
            Invoke(nameof(ClearStatus), 2f); // Очистить через 2 секунды
        }
        Debug.LogWarning(message);
    }

    private void ClearStatus()
    {
        if (statusText != null) statusText.text = "";
    }
}