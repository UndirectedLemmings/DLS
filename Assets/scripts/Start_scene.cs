using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Tilemaps;

public class Start_scene : MonoBehaviour
{
    public Tilemap Tilemap;
    public Character_move character; // префаб персонажа
    public FILL_MAP_v4 FMT; // генератор карты
    public Text round;

    private Character_move character_clone;

    public void Start()
    {
        if (FMT != null)
        {
            FMT.StartGenerationWithRetries();
        }
        else
        {
            Debug.LogError("Забыл перетащить FMT");
            return;
        }

        // --- ИСПРАВЛЕНО 1: Принимаем Vector2Int из обновленного генератора ---
        Vector2Int startVector = FMT.Get_Start_road();

        // --- ИСПРАВЛЕНО 2: Создаем Vector3Int локально ТОЛЬКО для запроса позиции у Tilemap ---
        Vector3Int tilemapPos = new Vector3Int(startVector.x, startVector.y, 0);

        // Спавним героя
        character_clone = Instantiate(character, Tilemap.GetCellCenterWorld(tilemapPos), UnityEngine.Quaternion.Euler(45, 0, 0));

        // Передаем карту
        character_clone.Tilemap = Tilemap;

        // Передаем 2D-координаты старта в обновленный мозг героя
        character_clone.StartJourney(startVector);

        round.text = ("круги=" + character_clone.Round().ToString());
    }

    public void RefreshRoundUI()
    {
        if (character_clone != null)
            round.text = "круги=" + character_clone.Round().ToString();
    }

    public void Update()
    {
        // Пока пусто
    }
}