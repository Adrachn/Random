using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using FMODUnity;

public enum STATES
{
    AWAKENING, TARGETTING, FOLLOWING, SWAPTOCLOSESTTARGET, KNOCKEDDOWN, PICKEDUP, DEAD, ATTACKING,
}

public class Llamas : DamageableObjects
{
    [SerializeField] private int penPoints = 0;
    [SerializeField] private int deathPoints = 0;

    [Header("Design variables")]
    [SerializeField] private int damage;
    [SerializeField] protected STATES stateSwap;
    [SerializeField][Tooltip("How far the lama will look for new target when searching for nearest target to swap to. Has to be less or equal to trigger-collider size")]
    private float proximityRange = 7.0f;
    [SerializeField] private LLAMATYPE llamaType = LLAMATYPE.PLAYERLLAMA;
    [SerializeField] public LayerMask layersToFind; ////Bit shift 1 to place 8 = player layer. 1<< 8 || 1 << 9. lager 8 och 9
    [SerializeField] private float faceTargetSpeed = 0.05f;
    [SerializeField] private GameObject dummyModel;
    [SerializeField] private GameObject ragdollModel;
    [SerializeField] private GameObject pickup;
    //private GameObject runToThisWhenDead;
    [SerializeField] [Tooltip("The remaining time of set Knockdown time = invulnerable time. E.g. 15-14 = 1s invulnerable. Logical. Don't complain.")] private float notInvulnerableTime = 14.0f;
   
    [Header("Sound")]
    FMOD.Studio.EventInstance rushLlamaSoundPlayer;
    [EventRef] [SerializeField] protected string knockDown, llamaDead, spawnSound;

    [Header("Testing variables")]
    NavMeshHit hit;
    protected NavMeshAgent agent;
    protected GameObject currentLlamaTarget;
    private Collider[] overlappedObjects = new Collider[10];  //size matters
    [SerializeField] protected bool inFront = false;
    protected bool dealtDamage = false;
    [SerializeField] private int dropChance;
    [SerializeField] private ParticleSystem deathParticle;
    [SerializeField] private ParticleSystem hitParticle;
    protected string playerTag = "Player";
    protected string harmlessTag = "harmless";
   
    [SerializeField] private AchievementManager.LLAMAACHIEVTYPES achievType;

    //////////////////Timer Crap /////////////////////////////
    private TimerClass timer = new TimerClass();            //Knocked down timer   
    private bool knockedTimerSet = false;
    [SerializeField] private float downedTime = 15;
    private TimerClass timer2 = new TimerClass();           //Chase Achievment timer & time til dead timer
    protected bool chaseTimer = false;
    private float chaseTimeTilDead = 30;
    private float chaseTimeTilAchievement = 10;             //chaseTimeTilDead-The actual time you want it to take = this variable
    protected TimerClass timer3 = new TimerClass();         //Leaving range, swap target delay
    protected bool swapTargetTimer = false;
    [SerializeField] protected float swapTargetDelay = 2.0f;
    //////////////////////////////////////////////////////////

    protected Animator anim;
    private Transform closest = null;
    private GameObject chosenQuadrant = null;        
    private List<GameObject> notKnocked = null;

    public AchievementManager.LLAMAACHIEVTYPES AchievType {
        get{
            return achievType;
        }
    }
    public enum LLAMATYPE
    {
        PLAYERLLAMA = 0,
        BUILDINGLLAMA = 1,
        //PATROLLINGLLAMA = 2,
    }

    #region properties

    public bool InFront
    {
        set { inFront = value; }
        get { return inFront; }
    }

    public GameObject CurrentLlamaTarget
    {
        set { currentLlamaTarget = value; }
        get { return currentLlamaTarget; }
    }

    public int PenPoints
    {
        get { return penPoints; }
    }

    public GameObject DummyModel
    {
        get { return dummyModel; }
    }

    public GameObject RagdollModel
    {
        get { return ragdollModel; }
    }

    public TimerClass Timer
    {
        get { return timer; }
    }

    public STATES StateSwap
    {
        set { stateSwap = value; }
        get { return stateSwap; }
    }

    #endregion

    public virtual void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = gameObject.GetComponent<Animator>();
        FreezeAgent();
    }
    
    protected virtual void OnEnable()
    {
        if (knockedDown)
        {
            anim.SetBool("LYING", true);
        }
        stateSwap = STATES.AWAKENING;
    }

    protected override void Start()
    {
        RuntimeManager.PlayOneShot(spawnSound, this.transform.position);
        stateSwap = STATES.AWAKENING;
        base.Start();
    }


    private void Update()
    {
        //Not very optimal state machine. Unity apparantly has one

        UpdateHitColor();

        switch (stateSwap)
        {
            case STATES.AWAKENING:
                Awakening();
                break;
            case STATES.TARGETTING:
                Targetting(llamaType);
                stateSwap = STATES.FOLLOWING;
                break;
            case STATES.FOLLOWING:
                    Following(llamaType);
                break;
            case STATES.SWAPTOCLOSESTTARGET:
                SwapTarget();
                stateSwap = STATES.FOLLOWING;
                break;
            case STATES.KNOCKEDDOWN:
                KnockedDown();
                break;
            case STATES.PICKEDUP:
                break;
            case STATES.ATTACKING:
                AttackState();
                break;
            case STATES.DEAD:
                DeadState();
                break;
            default:
                //stateSwap = STATES.TARGETTING;
                break;
        }
        
        if (knockedDown)
        {
            stateSwap = STATES.KNOCKEDDOWN;
        }
        
        timer.TimeUpdate();                 //knocked down timer
        timer2.TimeUpdate();                //Achievement chase delay 
        timer3.TimeUpdate();                //Leaving range swap target delay
        
        //Check if player was out of range too long                                                                 
        if (timer3.TimeLeft <= 0.0f && swapTargetTimer)
        {
            swapTargetTimer = false;
            stateSwap = STATES.SWAPTOCLOSESTTARGET;
        }
    }
    
    private void Awakening()
    {
        anim.SetBool("ATTACKING", false);
        stateSwap = STATES.TARGETTING;
    }

    //Target from original player or building array
    protected virtual void Targetting(LLAMATYPE llamaType)
    {
        inFront = false;
        switch (llamaType)
        {
            case LLAMATYPE.PLAYERLLAMA:
                currentLlamaTarget = OverallManager.Instance.livePlayers[Random.Range(0, OverallManager.Instance.livePlayers.Count)];
                break;
            case LLAMATYPE.BUILDINGLLAMA:
                NextBuilding();
                agent.SetDestination(currentLlamaTarget.transform.position);
                break;
        }
    }

    //Used in NextBuilding: Check if all buildings are knocked down in their quadrant. If so-remove quadrant from search array
    List<GameObject> KnockedCheck(GameObject[] arrayToCheck)
    {
        notKnocked = null;
        int knockedCount = 0;
        for (int i = 0; i < arrayToCheck.Length; i++)
        {
            if (arrayToCheck[i].GetComponent<DamageableObjects>().IsKnockedDown)
            {
                knockedCount++;
            }
        }
        if (knockedCount == arrayToCheck.Length)
        {
            OverallManager.Instance.quadrantPositions.Remove(closest);
            Destroy(closest.gameObject);
        }
        return notKnocked;
    }

    //Find closest quadrant. Returns 1 of 4 GameObject quadrants that are set manually in the scene
    void NextBuilding()
    {   
        GetClosest(OverallManager.Instance.quadrantPositions);

        switch (closest.name)
        {
            case "NE":
                KnockedCheck(OverallManager.Instance.stationaryLlamatargetsNE);
                //targets random in array, knocked too
                currentLlamaTarget = OverallManager.Instance.stationaryLlamatargetsNE[Random.Range(0, OverallManager.Instance.stationaryLlamatargetsNE.Length + 1)]; 
                break;

            case "NW":
                KnockedCheck(OverallManager.Instance.stationaryLlamatargetsNW);
                currentLlamaTarget = OverallManager.Instance.stationaryLlamatargetsNW[Random.Range(0, OverallManager.Instance.stationaryLlamatargetsNE.Length + 1)]; //+1 magic solution
                break;

            case "SE":
                KnockedCheck(OverallManager.Instance.stationaryLlamatargetsSE);
                currentLlamaTarget = OverallManager.Instance.stationaryLlamatargetsSE[Random.Range(0, OverallManager.Instance.stationaryLlamatargetsNE.Length + 1)];
                break;

            case "SW":
                KnockedCheck(OverallManager.Instance.stationaryLlamatargetsSW);
                currentLlamaTarget = OverallManager.Instance.stationaryLlamatargetsSW[Random.Range(0, OverallManager.Instance.stationaryLlamatargetsNE.Length + 1)];
                break;
            default:
                break;
        }
    }

    protected virtual void Following(LLAMATYPE llamaType)
    {
        anim.SetBool("ATTACKING", false);
        anim.SetBool("FOLLOWING", true);
        
        if (anim.GetBool("DIZZY") == false) //stupid charge llamas Fix
            UnFreezeAgent();

        switch (llamaType)
        {
            case LLAMATYPE.PLAYERLLAMA:  
                if (!agent.pathPending)
                    agent.SetDestination(currentLlamaTarget.transform.position);
                if (currentLlamaTarget == null || currentLlamaTarget.GetComponent<DamageableObjects>().IsKnockedDown || currentLlamaTarget.activeSelf == false)
                {
                    chaseTimer = false;
                    stateSwap = STATES.TARGETTING;
                }
                break;
            case LLAMATYPE.BUILDINGLLAMA:
                if (currentLlamaTarget.layer == LayerMask.NameToLayer("Player") && !agent.pathPending)
                    agent.SetDestination(currentLlamaTarget.transform.position);
                if (currentLlamaTarget.GetComponent<DamageableObjects>().IsKnockedDown || currentLlamaTarget.activeSelf == false)
                {
                    chaseTimer = false;
                    stateSwap = STATES.TARGETTING;
                }
                break;
            default:
                break;
        }

        if (agent.remainingDistance <= agent.stoppingDistance)
            InStoppingDistance();

        ChaseTimer();
    }

    //For achievement & Remove llamas that get stuck on edges if thrown by players. For areas in map use not walkable areas in navmesh
    void ChaseTimer()
    {
        //RESETTAR bara chaseTimer bool

        if (!timer2.TimerSet && !chaseTimer)
        {
            timer2.StartTimer(chaseTimeTilDead);
            chaseTimer = true;
        }
        if (timer2.TimeLeft < chaseTimeTilAchievement && chaseTimer)
        {
            AchievementManager.Instance.UnlockAchievement("If You Wanna Be My Llama");
        }
        //remove llamas stuck below y pos -15  after timer
        if (timer2.TimeLeft <= 0.0f && chaseTimer && agent.transform.position.y < -15)
        {
            Destroy(this.gameObject);
            chaseTimer = false;
        }
    }
    
    protected virtual void InStoppingDistance()
    {
        FaceTarget(currentLlamaTarget.GetComponent<Collider>().bounds.center); 
    }

    protected void FaceTarget(Vector3 destination)
    {
        Vector3 lookPos = (destination - transform.position).normalized;
        
        if (lookPos != Vector3.zero)
        {
            Quaternion rotation = Quaternion.LookRotation(lookPos);
            transform.rotation = Quaternion.Slerp(transform.rotation, rotation, faceTargetSpeed * Time.deltaTime);
        }
    }

    //Find closest gameObject in overlapsphere and set target to that
    private GameObject SwapTarget()
    {
        inFront = false;
        float closest = 0;
        //store all nearby targets (based on collider size) in array
        int amountFound = Physics.OverlapSphereNonAlloc(transform.position, proximityRange, overlappedObjects, layersToFind.value);

        //Find closest target
        for (int i = 0; i < amountFound; i++)
        {
            if (i == 0)
            {
                closest = Vector3.Distance(overlappedObjects[i].gameObject.transform.position, transform.position);
                currentLlamaTarget = overlappedObjects[i].gameObject;
            }
            else if (i != 0 && closest > Vector3.Distance(overlappedObjects[i].gameObject.transform.position, transform.position))
            {
                closest = Vector3.Distance(overlappedObjects[i].gameObject.transform.position, transform.position);
                currentLlamaTarget = overlappedObjects[i].gameObject;
            }
        }
        return currentLlamaTarget;
    }
    
    private void DropAmmo()
    {
        if ((int)Random.Range(1, dropChance) == 1)
        {
            GameObject tempPickup = Instantiate(pickup, transform.position + new Vector3(1, 0, 0), transform.rotation);
        }
    }
    
    private void KnockedDown()
    {
        anim.SetBool("FOLLOWING", false);
        anim.SetBool("KNOCKEDOUT", true);
        agent.updateRotation = false;
        Invulnerable = true;

        FreezeAgent();

        //timer until not knocked down
        if (!timer.TimerSet && !knockedTimerSet)
        {
            timer.StartTimer(downedTime);
            knockedTimerSet = true;
        }

        //logic name.. The time of knockdown - notinvuln = time the lama can't be damaged after being knocked down.
        else if (timer.TimeLeft < notInvulnerableTime )      
        {
            Invulnerable = false;

            if (currentHealth < 0)
            {
                knockedTimerSet = false;
                knockedDown = false;
                stateSwap = STATES.DEAD;
            }
        }
        if (timer.TimeLeft <= 1.4)              //magic number which is the time of the get up/rise animation
        {
            anim.SetBool("KNOCKEDOUT", false);
            anim.SetBool("LYING", false);       // temp solution until slerp from ragdoll to animation
        }
        if (timer.TimeLeft == 0.0f)
        {
            knockedDown = false;
            currentHealth = maxHealth;
            knockedTimerSet = false;
            agent.updateRotation = true;
            stateSwap = STATES.TARGETTING;
        }
    }
    
    public virtual void AttackState()
    {
        chaseTimer = false;
        try
        {
            FaceTarget(currentLlamaTarget.GetComponent<Collider>().bounds.center);
            if (inFront && !currentLlamaTarget.GetComponent<DamageableObjects>().IsKnockedDown && !knockedDown && currentLlamaTarget.activeSelf)
            {
                anim.SetBool("ATTACKING", true);            //Damage dealing is done via Attack() that is triggered in specific keyframe in the headbuttAnim. Not too clear..
            }
            else
            {
                anim.SetBool("ATTACKING", false);
                stateSwap = STATES.SWAPTOCLOSESTTARGET;
            }
        }
        catch
        {
            // stateSwap = STATES.FOLLOWING;        
        }
    }

    void FreezeAgent()
    { 
        agent.isStopped = true;
    }

    void UnFreezeAgent()
    {
        agent.isStopped = false;
        anim.SetBool("IDLE", false);
        anim.SetBool("KNOCKEDOUT", false);
    }

    public virtual void Attack()
    {
        Instantiate(hitParticle, currentLlamaTarget.transform.position, transform.rotation);  
        currentLlamaTarget.GetComponent<DamageableObjects>().TakeDamage(damage);
    }

    protected virtual void DeadState()
    {
        //for achievement purpose only
        AchievementManager.Instance.UpdateLlamaDeaths(achievType);
       
        RuntimeManager.PlayOneShot(llamaDead, this.transform.position);
        Instantiate(deathParticle, transform.position, transform.rotation);
        try
        {
            OverallManager.Instance.gameObject.GetComponent<HighScore>().AddPoints(deathPoints);
            FloatingImageController.CreateFloatingText("-" + points.ToString() + "p", transform, Color.red, true);
        }
        catch
        {
            Debug.Log("No Highscore in Overall Manager");
        }
        Destroy(this.gameObject);
    }
    
    //Get closest Transform from submitted array to this gameobject. In sqr distance
    Transform GetClosest(List<Transform> targetList)
    {
        float closestDistanceSqr = Mathf.Infinity;
        Vector3 currentPosition = transform.position;

        for (int i = 0; i < targetList.Count; i++)
        {
            Vector3 vectorToTarget = targetList[i].gameObject.transform.position - currentPosition;
            float sqrDistToTarget = vectorToTarget.sqrMagnitude;
            if (sqrDistToTarget < closestDistanceSqr)
            {
                closestDistanceSqr = sqrDistToTarget;
                closest = targetList[i];
            }
        }
        return closest;
    }
    
    #region #####################################################################################


    //void Raycast(float range)
    //{
    //    //Notes: Raycasts will not detect Colliders for which the Raycast origin is inside the Collider.
    //    Vector3 fwd = transform.TransformDirection(Vector3.forward);

    //    Debug.DrawRay(transform.position, fwd, Color.cyan, 2.0f);
    //    if (Physics.Raycast(transform.position, fwd, out hit, layersToFind.value))
    //    {
    //        inFront = true;
    //        hit.collider.gameObject.SetActive(false);
    //    }
    //    else
    //    {
    //        inFront = false;
    //    }
    //}

    void OnDrawGizmos()
    {
        //Gizmos.DrawIcon(runToThisWhenDead, "coinGold.png", true);
        //Gizmos.DrawSphere(transform.position, proximityRange);
        // Gizmos.DrawRay(transform.position, transform.forward);
    }

    //The llamas should swap to an intersecting player as long as they are not attacking something
    protected virtual void OnTriggerEnter(Collider other)    // If what enters trigger range is of the same layer, the llama won't swap atm. No aggro bouncing
    {
        try
        {
            if (other.gameObject == currentLlamaTarget)
            {
                swapTargetTimer = false;
            }
            //if other is a player, if not already chasing a player & if not busy attacking - Swap target to the intersecting player  //harmless tag check nödvändig?
            if (other.gameObject.CompareTag(playerTag) && !other.gameObject.CompareTag(harmlessTag) && stateSwap != STATES.ATTACKING  && !currentLlamaTarget.layer.Equals(other.gameObject.layer))
            {
                currentLlamaTarget = other.gameObject;
                chaseTimer = false;
                inFront = false; //Llamas will attack player in all cases except when spawned next to player. Going from knocked to awake again = wont attack
            }
        }
        catch
        {

        }
    }

    //The llamas should stop chasing the player if it leaves its aggro range for a certain amount of time
    protected virtual void OnTriggerExit(Collider other)
    {
        if (other.gameObject == currentLlamaTarget && stateSwap != STATES.ATTACKING)  
        {
            if (!timer3.TimerSet && !swapTargetTimer)
            {
                timer3.StartTimer(swapTargetDelay);
                swapTargetTimer = true;
            }
        }
    }

    protected override void OnKnockdown()
    {
        base.OnKnockdown();
        DropAmmo();
        RuntimeManager.PlayOneShot(knockDown, this.transform.position);
    }

    protected override void Onhit()
    {
        if (!knockedDown)
        {
            anim.SetTrigger("Hit");
        }
        if(!Invulnerable)
        RuntimeManager.PlayOneShot(knockDown, this.transform.position);
    }
    #endregion
}
