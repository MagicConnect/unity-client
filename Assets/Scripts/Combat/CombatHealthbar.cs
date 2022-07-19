using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CombatHealthbar : MonoBehaviour
{
    public RectTransform fill;
    public RectTransform background;
    public CombatEntity entity;

    private float healthPercentage; // TEMP var for testing
    private bool healthTestUp; // TEMP

    public void UpdateHealthBar()
    {
        //float percentage = entity.GetHealth() / entity.GetMaxHealth();

        float totalSize = background.sizeDelta.x;
        float fillSize = (1f-healthPercentage) * totalSize;
        fill.offsetMax = new Vector2(-fillSize, fill.offsetMax.y);
    }

    public void Update()
    {
        healthPercentage += (healthTestUp ? 0.3f : -0.3f) * Time.deltaTime;
        if(healthPercentage > 1f)
        {
            healthPercentage = 1f;
            healthTestUp = false;
        }
        if(healthPercentage < 0f)
        {
            healthPercentage = 0f;
            healthTestUp = true;
        }

        UpdateHealthBar();
    }
}
