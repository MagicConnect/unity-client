using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CutsceneBlackoutVFX : CutsceneEffect
{
    private new float timePassed = 0.0f;

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
        if(timePassed <= animationTime)
        {
            float progress = timePassed / animationTime;
            Color startColor = new Color(Color.black.r, Color.black.g, Color.black.b, 0.0f);
            image.color = Color.Lerp(startColor, Color.black, progress);
        }

        timePassed += Time.deltaTime;
    }
}
