using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UI_to_2D_Anchor : MonoBehaviour
{
    public GameObject followedObject;
    public Vector2 offset;

    // Update is called once per frame
    void Update()
    {
        Vector2 screenPos = Camera.main.WorldToScreenPoint(followedObject.transform.position);
        transform.position = screenPos + offset;
    }
}
