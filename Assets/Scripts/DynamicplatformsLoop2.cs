using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class DynamicplatformsLoop2 : MonoBehaviour
{
    public float exactInternalTimer = 0;
    public float internalTimer = 0;
    public bool platformExists = false;
    public float delay = 4;


    private BoxCollider2D myCollider;


    // Start is called before the first frame update
    void Start()
    {
        myCollider = GetComponent<BoxCollider2D>();
    }

    // Update is called once per frame
    void Update() 
    { 

        exactInternalTimer += Time.deltaTime;
        

        if (exactInternalTimer >= 1)
        {
            exactInternalTimer = 0;
            internalTimer++;
        }

        if (internalTimer / 4 == 1)
        {
            if (platformExists)
            {
                internalTimer = 0;
                transform.position = new Vector3(transform.position.x, transform.position.y, transform.position.z + 2);
                myCollider.isTrigger = true;
                platformExists = false;

            }

            else if (!platformExists)
            {
                internalTimer = 0;
                transform.position = new Vector3(transform.position.x, transform.position.y, transform.position.z - 2);
                myCollider.isTrigger = false;
                platformExists = true;
            }
        }
    }
}
