using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CutsceneVfxFadein : CutsceneEffect
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
            Color endColor = new Color(Color.black.r, Color.black.g, Color.black.b, 0.0f);
            image.color = Color.Lerp(Color.black, endColor, progress);
        }

        timePassed += Time.deltaTime;
    }
}
