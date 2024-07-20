using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Water : MonoBehaviour {

    // This component is separate from the player's underwater movement.
    // Feel free to add whatever you want in here, like a rigidbody buoyancy/floating system or something.
    public WaterType Type;

    private float tickInterval = 1;
    private float timeSinceTick = .9f;

    DateTime lastTimeTicked = DateTime.MinValue;

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (Type == WaterType.Poisonous)
            {
                if (timeSinceTick >= tickInterval)
                {
                    //Player.Instance.Health.ReduceHealth(5);
                    timeSinceTick = 0;
                    lastTimeTicked = DateTime.Now;
                }
                timeSinceTick += Time.deltaTime;
            }
        }
        //Debug.Log("trigger: " + other.name);
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (Type == WaterType.Poisonous)
            {
                StartCoroutine(WaitThenCheck());
            }
        }
    }

    private IEnumerator WaitThenCheck()
    {
        DateTime themomentplayerexited = DateTime.Now;
        yield return new WaitForSeconds(1);
        if (Type == WaterType.Poisonous)
        {
            if (lastTimeTicked < themomentplayerexited)
            {
                timeSinceTick = .9f;
            }
        }
    }
}

public enum WaterType
{
    Normal,
    Poisonous
}
