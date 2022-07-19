using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CombatEffect
{
    // Effectively an interface, should be extended in subclasses

    // Tracks an animation until completion, used to chain actions together
    public CombatAction source;
    public bool started;
    public float delayAtStart = 0;
    public float delayAtEnd = 0;

    public delegate void EffectStarted();
    public delegate void EffectComplete();
    public EffectStarted startScript;
    public EffectComplete endScript;

    public virtual void StartEffect(CombatAction source)
    {
        this.source = source;
        startScript?.Invoke();
        started = true;
    }

    public virtual void EndEffect()
    {
        endScript?.Invoke();
        started = false;
        source.EffectDone();
    }
    
    public virtual void UpdateEffect()
    {

    }
}
