using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class GoldenLlama1 : DamageableObjects {
    //[SerializeField] private Transform[] waypoints;
    private int currentWP = 0;
    private NavMeshAgent agent;
    private Animator anim;
    [SerializeField] private int dropChance;
    [SerializeField] private GameObject pickup;
    [SerializeField] private ParticleSystem deathParticle;
    [SerializeField] private AchievementManager.LLAMAACHIEVTYPES achievType;

    void Awake()
    {
        //ev Warp för sätta till annan position
        anim = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();
       // agent.SetDestination(waypoints[currentWP].position);
        agent.SetDestination(OverallManager.Instance.goldenWaypoints[currentWP].transform.position);
        anim.SetBool("FOLLOWING", true);
    }

   
    private void Update()
    {
        agent.isStopped = false;
        
        if(agent.remainingDistance <= agent.stoppingDistance && !agent.pathPending)
        GetNewWaypoint();
    }

    void GetNewWaypoint()
    {
        try
        {
            currentWP++;
            
            if (currentWP > OverallManager.Instance.goldenWaypoints.Length)
            {
                Destroy(this.gameObject);
                //Do it all over again. reapeat path
                //currentWP = 0;
            }
            agent.SetDestination(OverallManager.Instance.goldenWaypoints[currentWP].transform.position);
        }
        catch
        {
        }
    }

    protected override void Onhit()
    {
        anim.SetTrigger("Hit");
       
    }

    protected override void OnKnockdown()
    {
        base.OnKnockdown();
        DropAmmo();
        Instantiate(deathParticle, transform.position, transform.rotation);
        Destroy(this.gameObject);
        AchievementManager.Instance.UpdateLlamaDeaths(achievType);
    }

    private void DropAmmo()
    {
        if ((int)Random.Range(1, dropChance) == 1)
        {
            GameObject tempPickup = Instantiate(pickup, transform.position + new Vector3(1, 0, 0), transform.rotation);
        }
    }
}