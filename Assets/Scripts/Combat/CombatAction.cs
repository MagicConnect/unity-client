using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CombatAction
{
    public bool started = false;
    public List<CombatEffect> steps = new List<CombatEffect>();

    public void StartEffects()
    {
        steps[0].StartEffect(this);
        started = true;
    }

    public void EffectDone()
    {
        steps.RemoveAt(0);
        if(steps.Count > 0)
        {
            steps[0].StartEffect(this);
        }
        else
        {
            CombatSession.ActionDone(this);
        }
    }

    public void UpdateAction()
    {
        if(steps.Count > 0 && steps[0].started)
            steps[0].UpdateEffect();
    }
}
