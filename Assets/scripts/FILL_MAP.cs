using NUnit.Framework;
using System.Collections.Generic;
using System.Numerics;
using UnityEditor.Tilemaps;
using UnityEngine;

using UnityEngine.TextCore.Text;
using UnityEngine.Tilemaps;

public class FILL_MAP_TEST : MonoBehaviour //Заполнение карты пустотой
{
    private Dictionary<UnityEngine.Vector3, GameObject> Cell_map = new Dictionary<UnityEngine.Vector3, GameObject>();
    public Tilemap Map; //игровое поле
    public GameObject Void; //пустота
    
    private UnityEngine.Quaternion quaternion = new UnityEngine.Quaternion(0, 0, 1, 0);
    public void Start()
    {
        UnityEngine.Vector3Int map_vector = new UnityEngine.Vector3Int(0,0);


        for (map_vector.x = 0; map_vector.x <= 30; map_vector.x++)
        {
            for (map_vector.y = 0; map_vector.y <= 30; map_vector.y++)
            {
                Cell_map.Add(map_vector, Instantiate(Void, map_vector, quaternion));
            }
        }
        generation_roadmap();
    }

    public GameObject start; // старт
    public GameObject signpost;   //перекрестки
    public GameObject road; //дороги
    


    private UnityEngine.Vector3 Vector_Start = new UnityEngine.Vector3();
    private UnityEngine.Vector3 Vector_signpost1 = new UnityEngine.Vector3();
    private UnityEngine.Vector3 Vector_signpost2 = new UnityEngine.Vector3();
    private UnityEngine.Vector3 Vector_signpost3 = new UnityEngine.Vector3();

    public List<GameObject> Road_list = new List<GameObject>();

    public List<GameObject> GetList()
    {
        return Road_list;
    }

    private void road_bild(UnityEngine.Vector3 point_1, UnityEngine.Vector3 point_2)
    {
        UnityEngine.Vector3 road_point1 = point_1;
        UnityEngine.Vector3 road_point2 = point_2;

        Road_list.Add(Instantiate(road, road_point1, quaternion));

        while (road_point1.x != road_point2.x)
        {
            if (road_point1.x < road_point2.x)
            {
                road_point1.x++;
            }
            else
            {
                road_point1.x--;
            }
            Destroy(Cell_map[road_point1]);
            Road_list.Add(Instantiate(road, road_point1, quaternion));
        }

        while (road_point1.y != road_point2.y)
        {
            if (road_point1.y < road_point2.y)
            {
                road_point1.y++;
            }
            else
            {
                road_point1.y--;
            }
            Destroy(Cell_map[road_point1]);
            Road_list.Add(Instantiate(road, road_point1, quaternion));
        }
    }

    public void generation_roadmap()
    {

        Debug.Log("Нажата Большая Красная Кнопка");

        // 4 точки-перекрестка

        int road_leath = 10;
        int road_min = 5;

        Vector_Start = new UnityEngine.Vector3(Random.Range(road_min, road_leath), Random.Range(road_min, road_leath));
        Destroy(Cell_map[Vector_Start]);
        Road_list.Add(Instantiate(start, Vector_Start, quaternion));
        Debug.Log("Перекресток 1 создан: " + Vector_Start.x + "/" + Vector_Start.y);

        Vector_signpost1 = new UnityEngine.Vector3(Random.Range((road_min), (road_min + road_leath)), Random.Range((int)(Vector_Start.y + road_min), (int)(Vector_Start.y + road_leath)));
        Destroy(Cell_map[Vector_signpost1]);
        Instantiate(signpost, Vector_signpost1, quaternion);
        Debug.Log("Перекресток 2 создан: " + Vector_signpost1.x + "/" + Vector_signpost1.y);

        Vector_signpost2 = new UnityEngine.Vector3(Random.Range((int)(Vector_signpost1.x + road_min), (int)(Vector_signpost1.x + road_leath)), Random.Range((int)(Vector_signpost1.y), (int)(Vector_signpost1.y + road_leath)));
        Destroy(Cell_map[Vector_signpost2]);
        Instantiate(signpost, Vector_signpost2, quaternion);
        Debug.Log("Перекресток 3 создан: " + Vector_signpost2.x + "/" + Vector_signpost2.y);

        Vector_signpost3 = new UnityEngine.Vector3(Random.Range((int)(Vector_signpost2.x + road_min), (int)(Vector_signpost2.x + road_leath)), Random.Range((int)(Vector_signpost2.y - road_leath), (int)(Vector_signpost2.y - road_min)));
        Destroy(Cell_map[Vector_signpost3]);
        Instantiate(signpost, Vector_signpost3, quaternion);
        Debug.Log("Перекресток 4 создан: " + Vector_signpost3.x + "/" + Vector_signpost3.y);

        // конец 4 точки перекрестков

        //дороги
       
        road_bild(Vector_Start, Vector_signpost1);
        road_bild(Vector_signpost1, Vector_signpost2);
        road_bild(Vector_signpost2, Vector_signpost3);
        road_bild(Vector_signpost3, Vector_Start);

    }
}
