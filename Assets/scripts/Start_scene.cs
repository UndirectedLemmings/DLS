using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.TextCore.Text;
using UnityEngine.Tilemaps;
using UnityEngine.UI;




public class Start_scene : MonoBehaviour

{
    public Tilemap Tilemap;
    public Character_move character;//персонажа
    public FILL_MAP_v4 FMT;//карта
    public Text round;
    Character_move character_clone;
    public void Start()
    {

        if (FMT != null)
        {
            FMT.StartGenerationWithRetries();
        } 
        else
        { Debug.LogError("Забыл перетащить");
            return;
        }

        // Получаем координаты старта
        Vector3Int startVector = FMT.Get_Start_road();

        // Спавним героя (сохраняем твой наклон)
        character_clone = Instantiate(character, Tilemap.GetCellCenterWorld(startVector), UnityEngine.Quaternion.Euler(45, 0, 0));

        // Передаем карту
        character_clone.Tilemap = Tilemap;

        // БОЛЬШЕ НЕ ПЕРЕДАЕМ target_Vint! 
        // Вместо этого активируем его мозг, передав точку старта:
        character_clone.StartJourney(startVector);

        round.text = ("круги=" + character_clone.Round().ToString());
    }

    // Важно добавить проверку на null, чтобы не было ошибок, если герой еще не заспавнился
    public void RefreshRoundUI()
    {
        if (character_clone != null)
            round.text = "круги=" + character_clone.Round().ToString();
    }

    public void Update()
    {
        

    }
}
