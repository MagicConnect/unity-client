using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CutsceneVfxFadein : CutsceneEffect
{
    private new float timePassed = 0.0f;

    private float finishedDuration = 0.0f;

    private bool markedForDestruction = false;

    // After finishing its animation, this effect will no longer be needed (it will be invisible and won't be reused).
    // The gameobject should be destroyed automatically so no one has to worry about it.
    public float autoDeleteTime = 5.0f;

    public float animationTime = 1.0f;

    public Image image;

    // Start is called before the first frame update
    void Start()
    {
        if(!image)
        {
            image = this.GetComponent<Image>();
        }
    }

    // Update is called once per frame
    void Update()
    {
        Color endColor = new Color(Color.black.r, Color.black.g, Color.black.b, 0.0f);

        if(timePassed <= animationTime)
        {
            float progress = timePassed / animationTime;
            image.color = Color.Lerp(Color.black, endColor, progress);
        }
        
        // Once the animation is complete, set the final result and mark the object for destruction.
        if(timePassed > animationTime && !markedForDestruction)
        {
            image.color = endColor;
            Destroy(gameObject, autoDeleteTime);
        }

        timePassed += Time.deltaTime;
    }
}
