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
        // The animation should start from the moment of creation.
        this.isAnimating = true;

        Debug.LogFormat(this, "{0}: Starting blackout effect animation.", gameObject.name);
    }

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
        if(timePassed <= animationTime)
        {
            float progress = timePassed / animationTime;
            Color startColor = new Color(Color.black.r, Color.black.g, Color.black.b, 0.0f);
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
