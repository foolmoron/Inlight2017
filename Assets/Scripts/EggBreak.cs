using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EggBreak : MonoBehaviour
{

	     
    Animator _anim;
    public string MyTrigger;
    int collisionCount;
    public Collider egg_Collider;

    void Awake()
    {
        egg_Collider = GetComponent<Collider>();
        _anim = GetComponent<Animator>();
    }
    
    void OnTriggerEnter(Collider col)
    {
        if (col.transform.CompareTag("Player"))
        {
            _anim.SetTrigger(MyTrigger);
        }
       // collisionCount ++;
        //if (collisionCount == 3)
       // {
          //  Destroy(gameObject);
            //egg_Collider.enabled = true;
       // }
    }
    
}