using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FMODUnity;


public class FireLlama : Llamas
{
    [SerializeField] private float fireSpawnDelay = 0.5f;
    [SerializeField] private GameObject fire;
    [SerializeField] private float distFromLlama = 2.5f;

 
    protected override void OnEnable()
    {
        base.OnEnable();
        StartCoroutine(FireStarter());
    }

    private void OnDisable()
    {
        //isPlaying = false;
        //fireLlamaSoundPlayer.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
        StopCoroutine(FireStarter());
    }
    
    IEnumerator FireStarter()
    {
        while (gameObject.activeSelf == true)
        {
            yield return new WaitForSeconds(fireSpawnDelay);
            if (agent.velocity !=Vector3.zero)
                Instantiate(fire, (transform.position - transform.forward * distFromLlama), transform.rotation);
            //if (knockedDown)
            //{
            //    isPlaying = false;
            //    fireLlamaSoundPlayer.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
            //}
            //else if (!isPlaying) {
            //    fireLlamaSoundPlayer.start();
            //}
            
        }
    }
}