using UnityEngine;
using TMPro;

public class TooltipManager : MonoBehaviour
{
    public static TooltipManager Instance; // Паттерн Синглтон для быстрого доступа

    public GameObject tooltipPanel;
    public TextMeshProUGUI tooltipText;

    private void Awake()
    {
        // Настраиваем синглтон
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        HideTooltip(); // Прячем при старте
    }

    private void Update()
    {
        // Если тултип активен, заставляем его следовать за мышкой
        if (tooltipPanel.activeSelf)
        {
            // Небольшой сдвиг, чтобы курсор не перекрывал текст
            tooltipPanel.transform.position = Input.mousePosition + new Vector3(15f, -15f, 0f);
        }
    }

    public void ShowTooltip(string text)
    {
        tooltipText.text = text;
        tooltipPanel.SetActive(true);
    }

    public void HideTooltip()
    {
        tooltipPanel.SetActive(false);
        tooltipText.text = "";
    }
}