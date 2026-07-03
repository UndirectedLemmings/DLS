using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(FeatData))]
public class FeatDataEditor : Editor
{
    // Ссылки на свойства из нашего FeatData
    private SerializedProperty featName;
    private SerializedProperty description;
    private SerializedProperty nature;
    private SerializedProperty propertyCategory;
    private SerializedProperty domain;
    private SerializedProperty triggerType;
    private SerializedProperty effectTag;
    private SerializedProperty cancelsTags;
    private SerializedProperty bonusEndurance;
    private SerializedProperty bonusDamage;
    private SerializedProperty damageReduction;

    private void OnEnable()
    {
        // Связываем переменные со скриптом при выделении объекта
        featName = serializedObject.FindProperty("featName");
        description = serializedObject.FindProperty("description");
        nature = serializedObject.FindProperty("nature");
        propertyCategory = serializedObject.FindProperty("propertyCategory");
        domain = serializedObject.FindProperty("domain");
        triggerType = serializedObject.FindProperty("triggerType");
        effectTag = serializedObject.FindProperty("effectTag");
        cancelsTags = serializedObject.FindProperty("cancelsTags");
        bonusEndurance = serializedObject.FindProperty("bonusEndurance");
        bonusDamage = serializedObject.FindProperty("bonusDamage");
        damageReduction = serializedObject.FindProperty("damageReduction");
    }

    public override void OnInspectorGUI()
    {
        // Обязательно обновляем объект перед отрисовкой
        serializedObject.Update();

        // Отрисовываем базовые поля
        EditorGUILayout.PropertyField(featName);
        EditorGUILayout.PropertyField(description);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Новая Таксономия", EditorStyles.boldLabel);

        // Отрисовываем выбор Природы фита
        EditorGUILayout.PropertyField(nature);

        // МАГИЯ: Условная отрисовка
        // Если выбрано свойство (Property), показываем выбор категории
        if (nature.enumValueIndex == (int)FeatNature.Property)
        {
            EditorGUILayout.PropertyField(propertyCategory);

            // Если категория - это Умение (Ability), показываем выбор Домена
            if (propertyCategory.enumValueIndex == (int)PropertyCategory.Ability)
            {
                EditorGUILayout.PropertyField(domain);
            }
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Боевые триггеры и Теги", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(triggerType);
        EditorGUILayout.PropertyField(effectTag);
        EditorGUILayout.PropertyField(cancelsTags);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Модификаторы Характеристик", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(bonusEndurance);
        EditorGUILayout.PropertyField(bonusDamage);
        EditorGUILayout.PropertyField(damageReduction);

        // Применяем изменения
        serializedObject.ApplyModifiedProperties();
    }
}
#endif
