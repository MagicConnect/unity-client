using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActivityIconScript : MonoBehaviour
{
    public GameObject outerIcon;

    public GameObject innerIcon;

    public float outerRotationRate = 30.0f;

    public float innerRotationRate = 1.0f;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(outerIcon != null)
        {
            outerIcon.transform.Rotate(new Vector3(outerRotationRate * Time.deltaTime, outerRotationRate * Time.deltaTime, outerRotationRate * Time.deltaTime * -1.0f));
        }

        if(innerIcon != null)
        {
            innerIcon.transform.Rotate(new Vector3(0.0f, 0.0f, innerRotationRate * Time.deltaTime * -1.0f));
        }
    }
}
