using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Yarn.Unity;

public class Character : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    [YarnCommand("show")]
    public void ShowCharacter()
    {
        gameObject.GetComponent<SpriteRenderer>().enabled = true;
    }

    [YarnCommand("hide")]
    public void HideCharacter()
    {
        gameObject.GetComponent<SpriteRenderer>().enabled = false;
    }

    [YarnCommand("move")]
    public void MoveCharacter(GameObject stagePosition)
    {
        gameObject.transform.position = stagePosition.transform.position;
    }

    [YarnCommand("fade_out")]
    public void FadeOut()
    {
        gameObject.GetComponent<SpriteRenderer>().color = Color.gray;
    }

    [YarnCommand("fade_in")]
    public void FadeIn()
    {
        gameObject.GetComponent<SpriteRenderer>().color = Color.white;
    }
}
