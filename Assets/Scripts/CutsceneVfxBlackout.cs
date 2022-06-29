using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CutsceneVfxBlackout : CutsceneEffect
{
    private new float timePassed = 0.0f;

    public float animationTime = 1.0f;

    public Image image;

    void Awake()
    {
        if(!image)
        {
            image = this.GetComponent<Image>();
        }

        // The animation should start from the moment of creation.
        this.isAnimating = true;

        Debug.LogFormat(this, "{0}: Starting blackout effect animation.", gameObject.name);
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Because of how frame updates are processed, there will usually be at least 1 frame of delay between
    // the effect being created and it being updated visually. If the animation is supposed to complete the same frame
    // it is created, this method should help.
    public void CompleteAnimation()
    {
        image.color = Color.black;
        this.isAnimating = false;

        Debug.LogFormat(this, "{0}: Blackout effect animation complete.", gameObject.name);
    }

    // Update is called once per frame
    void Update()
    {
        if(timePassed <= animationTime)
        {
            Color startColor = new Color(Color.black.r, Color.black.g, Color.black.b, 0.0f);
            float progress;

            // Prevent division by 0.
            if(animationTime == 0.0f)
            {
                progress = 1.0f;
            }
            else
            {
                progress = timePassed / animationTime;
            }
            
            image.color = Color.Lerp(startColor, Color.black, progress);
        }

        if(timePassed > animationTime && this.isAnimating)
        {
            image.color = Color.black;

            this.isAnimating = false;

            Debug.LogFormat(this, "{0}: Blackout effect animation complete.", gameObject.name);
        }

        timePassed += Time.deltaTime;
    }
}
