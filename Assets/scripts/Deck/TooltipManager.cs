using UnityEngine;
using TMPro;

public class TooltipManager : MonoBehaviour
{
    public static TooltipManager Instance;

    [Header("Ссылки на UI")]
    public GameObject tooltipPanel;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI descriptionText;

    [Header("Настройки")]
    public Vector3 offset = new Vector3(15f, -15f, 0f);

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        HideTooltip();
    }

    private void Update()
    {
        // Обновляем позицию только если панель активна
        if (tooltipPanel != null && tooltipPanel.activeSelf)
        {
            tooltipPanel.transform.position = Input.mousePosition + offset;
        }
    }

    public void ShowTooltip(string title, string description)
    {
        // Проверка на случай, если ты забыл перетащить объекты в инспекторе
        if (tooltipPanel == null || titleText == null || descriptionText == null)
        {
            Debug.LogError("TooltipManager: Не назначены ссылки в Инспекторе!");
            return;
        }

        titleText.text = title;
        descriptionText.text = description;
        tooltipPanel.SetActive(true);
    }

    public void HideTooltip()
    {
        tooltipPanel.SetActive(false);
        titleText.text = "";
        descriptionText.text = "";
    }
}