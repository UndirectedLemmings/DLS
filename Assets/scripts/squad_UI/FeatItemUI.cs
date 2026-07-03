using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FeatItemUI : MonoBehaviour
{
    [Header("Ссылки на элементы префаба")]
    public TMP_Text titleText;       // Сюда перетащи текст для Названия
    public TMP_Text descriptionText; // Сюда перетащи текст для Описания
    public Image iconImage;          // Сюда перетащи картинку иконки

    public void Setup(FeatData feat)
    {
        if (feat == null) return;

        // ИСПРАВЛЕНО: Берем featName (наше кастомное поле), а не name (имя файла)
        if (titleText != null)
            titleText.text = feat.featName;

        if (descriptionText != null)
            descriptionText.text = feat.description;

        if (iconImage != null)
        {
            if (feat.icon != null)
            {
                iconImage.sprite = feat.icon;
                iconImage.gameObject.SetActive(true);
            }
            else
            {
                // Если иконки нет, скрываем картинку, чтобы не было белого квадрата
                iconImage.gameObject.SetActive(false);
            }
        }
    }
}