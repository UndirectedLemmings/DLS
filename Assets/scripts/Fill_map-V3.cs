using NUnit.Framework;
using System.Collections.Generic;
using System.Numerics;
using UnityEditor.Tilemaps;
using UnityEngine;

using UnityEngine.TextCore.Text;
using UnityEngine.Tilemaps;

public class FILL_MAP_v3 : MonoBehaviour //Заполнение карты пустотой
{
    // Глобальный справочник всех активных перекрестков на карте
    public static Dictionary<Vector3Int, CoordinateSwitcher> GlobalWaypoints = new Dictionary<Vector3Int, CoordinateSwitcher>();
    public Tilemap Map; //игровое поле
    public Tile Void; //пустота

    private UnityEngine.Quaternion quaternion = new UnityEngine.Quaternion(0, 0, 1, 0);
    public void Start()
    {
        UnityEngine.Vector3Int map_vector = new UnityEngine.Vector3Int(0, 0);


        for (map_vector.x = 0; map_vector.x <= 30; map_vector.x++)
        {
            for (map_vector.y = 0; map_vector.y <= 30; map_vector.y++)
            {
                Map.SetTile(map_vector, Void);
            }
        }
        generation_roadmap();
    }

    public GameObject start; // старт
    public GameObject signpost;   //перекрестки
    public Tile road; //дороги



    private UnityEngine.Vector3Int Vector_Start = new UnityEngine.Vector3Int();
    private UnityEngine.Vector3Int Vector_signpost1 = new UnityEngine.Vector3Int();
    private UnityEngine.Vector3Int Vector_signpost2 = new UnityEngine.Vector3Int();
    private UnityEngine.Vector3Int Vector_signpost3 = new UnityEngine.Vector3Int();

    public List<UnityEngine.Vector3Int> Road_list = new List<UnityEngine.Vector3Int>();

    public List<UnityEngine.Vector3Int> GetList()
    {
        return Road_list;
    }

    public Vector3Int Get_Start_road()
    {
        return Vector_Start;
    }

    private List<Vector3Int> GetRoadPath(Vector3Int startPoint, Vector3Int endPoint)
    {
        List<Vector3Int> path = new List<Vector3Int>();
        Vector3Int current = startPoint;

        path.Add(current);
        // Логика заполнения (твои циклы while)
        while (current.x != endPoint.x)
        {
            current.x += (current.x < endPoint.x) ? 1 : -1;
            path.Add(current);
            Map.SetTile(current, road);
        }
        while (current.y != endPoint.y)
        {
            current.y += (current.y < endPoint.y) ? 1 : -1;
            path.Add(current);
            Map.SetTile(current, road);
        }
        return path;
    }

    private List<Vector3Int> ALTGetRoadPath(Vector3Int startPoint, Vector3Int endPoint)
    {
        List<Vector3Int> path = new List<Vector3Int>();
        Vector3Int current = startPoint;

        path.Add(current);
        // Логика заполнения (твои циклы while)
        while (current.y != endPoint.y)
        {
            current.y += (current.y < endPoint.y) ? 1 : -1;
            path.Add(current);
            Map.SetTile(current, road);
        }
        while (current.x != endPoint.x)
        {
            current.x += (current.x < endPoint.x) ? 1 : -1;
            path.Add(current);
            Map.SetTile(current, road);
        } 
        return path;
    }

    public void generation_roadmap()
    {

        Debug.Log("Нажата Большая Красная Кнопка");

        // 4 точки-перекрестка

        int road_leath = 10;
        int road_min = 5;

        Vector_Start = new UnityEngine.Vector3Int(Random.Range(road_min, road_leath), Random.Range(road_min, road_leath));
        Road_list.Add(Vector_Start);
        GameObject start_Object = Instantiate(start, (UnityEngine.Vector3)Vector_Start, UnityEngine.Quaternion.identity);
        Debug.Log("Перекресток 1 создан: " + Vector_Start.x + "/" + Vector_Start.y);

        Vector_signpost1 = new UnityEngine.Vector3Int(Random.Range((road_min), (road_min + road_leath)), Random.Range((int)(Vector_Start.y + road_min), (int)(Vector_Start.y + road_leath)));
        GameObject signpost1_Object = Instantiate(signpost, Vector_signpost1, quaternion);
        Debug.Log("Перекресток 2 создан: " + Vector_signpost1.x + "/" + Vector_signpost1.y);

        Vector_signpost2 = new UnityEngine.Vector3Int(Random.Range((int)(Vector_signpost1.x + road_min), (int)(Vector_signpost1.x + road_leath)), Random.Range((int)(Vector_signpost1.y), (int)(Vector_signpost1.y + road_leath)));
        GameObject signpost2_Object = Instantiate(signpost, Vector_signpost2, quaternion);
        Debug.Log("Перекресток 3 создан: " + Vector_signpost2.x + "/" + Vector_signpost2.y);

        Vector_signpost3 = new UnityEngine.Vector3Int(Random.Range((int)(Vector_signpost2.x + road_min), (int)(Vector_signpost2.x + road_leath)), Random.Range((int)(Vector_signpost2.y - road_leath), (int)(Vector_signpost2.y - road_min)));
        GameObject signpost3_Object = Instantiate(signpost, Vector_signpost3, quaternion);
        Debug.Log("Перекресток 4 создан: " + Vector_signpost3.x + "/" + Vector_signpost3.y);

        // конец 4 точки перекрестков

        //дороги
       
        CoordinateSwitcher providerS = start_Object.GetComponent<CoordinateSwitcher>();
        providerS.pathA = GetRoadPath(Vector_Start, Vector_signpost1);
        providerS.pathB = ALTGetRoadPath(Vector_Start, Vector_signpost1);
        GlobalWaypoints.Add(Vector_Start, providerS);

        CoordinateSwitcher provider1 = signpost1_Object.GetComponent<CoordinateSwitcher>();
        provider1.pathA = GetRoadPath(Vector_signpost1, Vector_signpost2);
        provider1.pathB = ALTGetRoadPath(Vector_signpost1, Vector_signpost2);
        GlobalWaypoints.Add(Vector_signpost1, provider1);

        CoordinateSwitcher provider2 = signpost2_Object.GetComponent<CoordinateSwitcher>();
        provider2.pathA = GetRoadPath(Vector_signpost2, Vector_signpost3);
        provider2.pathB = ALTGetRoadPath(Vector_signpost2, Vector_signpost3);
        GlobalWaypoints.Add(Vector_signpost2, provider2);

        CoordinateSwitcher provider3 = signpost3_Object.GetComponent<CoordinateSwitcher>();
        provider3.pathA = GetRoadPath(Vector_signpost3, Vector_Start);
        provider3.pathB = ALTGetRoadPath(Vector_signpost3, Vector_Start);
        GlobalWaypoints.Add(Vector_signpost3, provider3);
    }
}