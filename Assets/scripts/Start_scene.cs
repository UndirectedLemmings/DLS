using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.TextCore.Text;




public class Start_scene : MonoBehaviour   
{
    public Character_move character;//персонажа
    public FILL_MAP_TEST FMT;//карта
    public Text round;
    Character_move character_clone;
    public void Start()
    {

        character_clone = Instantiate(character, FMT.Road_list[0].transform.position, UnityEngine.Quaternion.Euler(45, 0, 0));
        character_clone.target = FMT.GetList();
        round.text = ("круги=" + character_clone.Round().ToString()); 
    }

    public void Update()
    {
        round.text = ("круги=" + character_clone.Round().ToString());
    }
}
