using UnityEngine;
using TMPro;

public class TooltipManager : MonoBehaviour
{
    public static TooltipManager Instance; // Паттерн Синглтон

    [Header("Ссылки на UI")]
    public GameObject tooltipPanel;
    public TextMeshProUGUI titleText;       // Добавили заголовок (название карты/фита)
    public TextMeshProUGUI descriptionText; // Твое текущее поле описания

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        HideTooltip();
    }

    private void Update()
    {
        if (tooltipPanel.activeSelf)
        {
            // Сдвиг, чтобы курсор не перекрывал текст
            tooltipPanel.transform.position = Input.mousePosition + new Vector3(15f, -15f, 0f);
        }
    }

    // Универсальный метод вызова
    public void ShowTooltip(string title, string description)
    {
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