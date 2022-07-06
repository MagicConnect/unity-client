using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Yarn.Unity;

public class CutsceneEffect : CutsceneObject
{
    // How long the cutscene effect should stay active in the scene.
    public float duration;

    // If the effect is persistent it will stay in the scene until destroyed. Otherwise it will tick down its duration.
    public bool isPersistent = false;

    // How long the cutscene effect has been active in the scene.
    public float timePassed {get; private set;} = 0.0f;

    // The EffectExpired event will be fired off when the effect's duration has clocked out, allowing listeners to
    // handle effect destruction/recycling.
    public event Action<GameObject> EffectExpired;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        timePassed += Time.deltaTime;

        if(!isPersistent && timePassed >= duration)
        {
            // Destruction of this effect should be handled elsewhere. Fire off the event and let someone else deal with it.
            //EffectExpired?.Invoke(gameObject);
            Destroy(this.gameObject);
        }
    }

    // Removes this single effect from the scene.
    [YarnCommand("vfx_clear")]
    public void ClearEffect()
    {
        Destroy(this.gameObject);
    }
}
