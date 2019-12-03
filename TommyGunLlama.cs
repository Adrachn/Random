using UnityEngine;
using System.Collections;
using FMODUnity;

public class TommyGunLlama: Llamas
{
    FMOD.Studio.EventInstance spottLjudSoundPlayer;
    [EventRef] [SerializeField] private string spottSound;
    [SerializeField] private GameObject mouth;
    [SerializeField] private GameObject spit;
    [SerializeField] private float spitSpeed;
    private Vector3 offset = new Vector3(0, 2, 0);
    [SerializeField] private int sprayChance;
    

    public override void Attack()
    {
        int temp = (int)Random.Range(1, sprayChance);
        anim.SetInteger("sprayChance", temp);
    }

    public void Spit()
    {
        GameObject spittle = Instantiate(spit, mouth.transform.position, gameObject.transform.rotation); //ParentCollector.Instance.ProjectileParent
        Rigidbody rb = spittle.GetComponent<Rigidbody>();
        //Send spit towards center of current llamatarget and a bit down (offset) to not overshoot
        rb.velocity = ((currentLlamaTarget.GetComponent<Collider>().bounds.center - offset) - transform.position).normalized * spitSpeed;
        spittle.transform.rotation = Quaternion.LookRotation(currentLlamaTarget.GetComponent<Collider>().bounds.center - transform.position);
        RuntimeManager.PlayOneShot(spottSound, this.transform.position);
    }
    
    void SpraySpit(float angle)
    {
        GameObject spittle = Instantiate(spit, mouth.transform.position, mouth.transform.rotation); 

        //send spit forward in arc
        //var direction = Vector3.Slerp(transform.TransformDirection(Vector3.forward), Random.onUnitSphere, angle);
        
        Rigidbody rb = spittle.GetComponent<Rigidbody>();
        rb.velocity = ((spittle.transform.position - transform.position)-offset).normalized /** Time.deltaTime */* spitSpeed;
        RuntimeManager.PlayOneShot(spottSound, this.transform.position);
    }
}