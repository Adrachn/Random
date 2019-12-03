using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class SquishTrap : EntityPhysics
{
    [Header("SquishTrap settings")]
    [Tooltip("Fyll i antalet önskade LeverButtons som skall vara länkade till fällan och dra in dem i rutorna.")]
    [SerializeField] private TriggerObject[] linkedLeverButtons;
    [SerializeField] private float movementSpeed;
    [SerializeField] private float movementSpeedDown;
    [Tooltip("Squishie går till denna höjd efter att ha slagit i backen. Den kan starta vart som.")]
    [SerializeField] [Range(-5.0f, 10.0f)] private float maxTopPosition;
    private bool inMaxTopPosition;
    private int verticalDirection = 1;
    [Tooltip("Klicka i denna ruta om alla länkade LeverButtons behöver vara true för att fällan skall aktiveras.")]
    [SerializeField] private bool allNeeded;

    void Start()
    {
        if (linkedLeverButtons == null)
        {
            Debug.LogWarning("A squishTrap is not linked with a lever or button in the inspector window.");
        }
    }

    private void Update()
    {
        if (allNeeded == true)
        {
            if (areAllTrue() == true)
            {
                Maneuver();
            }
        }
        else if (allNeeded == false)
        {
            Maneuver();
        }
    }

    public override void OnOverlapTrigger(Collider2D[] colliders)
    {
        foreach (Collider2D coll in colliders)
        {
            if (coll.gameObject.layer == LayerMask.NameToLayer("Box") || coll.gameObject.layer == LayerMask.NameToLayer("SpiritBox"))
            {
                Destroy(coll.gameObject);
            }
            if (coll.gameObject.layer == LayerMask.NameToLayer("Player"))
            {
                MenuManager.Instance.ReloadScene();
            }
        }
    }

    public override void OnHitGround(EntityPhysics other)
    {
        foreach (TriggerObject linkedLeverButton in linkedLeverButtons)
        {
            if (linkedLeverButton.isActivated)
            {
                VelocityY = 0;
            }
        }
    }

    private void Maneuver()
    {
        foreach (TriggerObject linkedLeverButton in linkedLeverButtons)
        {
            if (!linkedLeverButton.isActivated && transform.position.y >= maxTopPosition)
            {
                VelocityY = 0;
            }
            if (linkedLeverButton.isActivated && !onGround)
            {
                VelocityY = -verticalDirection * movementSpeedDown;
            }
            if (!linkedLeverButton.isActivated && onGround)
            {
                VelocityY = verticalDirection * movementSpeed;
            }
        }
    }

    private bool areAllTrue()
    {
        for (int i = 0; i < linkedLeverButtons.Length; ++i)
        {
            if (linkedLeverButtons[i].isActivated == false)
            {
                return false;
            }
        }
        return true;
    }
}