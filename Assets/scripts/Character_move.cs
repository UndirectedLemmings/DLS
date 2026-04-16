using NUnit.Framework;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine; 
using static Unity.Collections.Unicode;


public class Character_move : MonoBehaviour
{
    public List<GameObject> target;
    public float speed = 0.7f;
    public int R = 0; //счетчик кругов
    int x=0; //обнуление кругов
             //никаких Translate, его нужно крутить по оси, у нас не крутится

    public void Update()
    {

            if (target != null)
            { transform.position = Vector3.MoveTowards(transform.position, target[x].transform.position, speed * Time.deltaTime); }
            if (transform.position == target[x].transform.position)
        {



            x++; 
            if (x==target.Count)
            {
                x = 0;
                R++;
            }
        }
    }

    public int Round()
    {
        return R;
    }
}
   