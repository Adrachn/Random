using UnityEngine;
using System.Collections;
using FMODUnity;

public class RangedLlama : Llamas
{
    FMOD.Studio.EventInstance spottLjudSoundPlayer;
    [EventRef] [SerializeField] private string spottSound;
    [SerializeField] private GameObject mouth;
    [SerializeField] private GameObject spit;
    [SerializeField] private float spitSpeed;
    private Vector3 offset = new Vector3(0,2,0);

    public override void Attack()
    {
        GameObject spittle = Instantiate(spit, mouth.transform.position, gameObject.transform.rotation);
        Rigidbody rb = spittle.GetComponent<Rigidbody>();
        //Send spit towards center of current llamatarget
        rb.velocity = ((currentLlamaTarget.GetComponent<Collider>().bounds.center-offset) - transform.position).normalized * Time.deltaTime * spitSpeed;
        spittle.transform.rotation = Quaternion.LookRotation(currentLlamaTarget.GetComponent<Collider>().bounds.center - transform.position);
        RuntimeManager.PlayOneShot(spottSound, this.transform.position);
    }
}
