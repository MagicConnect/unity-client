using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Yarn.Unity;

public class ConversationCharacter : MonoBehaviour
{
    // References to the object's components so they don't have to be searched for every time they're needed.
    private Image characterImage;
    private RectTransform rectTransform;

    void Awake()
    {
        
    }

    // Start is called before the first frame update
    void Start()
    {
        characterImage = GetComponent<Image>();
        rectTransform = GetComponent<RectTransform>();

        // Don't show the character after creation unless told to by the yarn script.
        HideCharacter();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    [YarnCommand("show_character")]
    public void ShowCharacter()
    {
        //gameObject.GetComponent<SpriteRenderer>().enabled = true;
        characterImage.enabled = true;
    }

    [YarnCommand("hide_character")]
    public void HideCharacter()
    {
        //gameObject.GetComponent<SpriteRenderer>().enabled = false;
        characterImage.enabled = false;
    }

    [YarnCommand("move_character")]
    public void MoveCharacter(GameObject stagePosition)
    {
        //gameObject.transform.position = stagePosition.transform.position;
        rectTransform.position = stagePosition.GetComponent<RectTransform>().position;
    }

    [YarnCommand("fade_out_character")]
    public void FadeOut()
    {
        //gameObject.GetComponent<SpriteRenderer>().color = Color.gray;
        characterImage.color = Color.gray;
    }

    [YarnCommand("fade_in_character")]
    public void FadeIn()
    {
        //gameObject.GetComponent<SpriteRenderer>().color = Color.white;
        characterImage.color = Color.white;
    }
}
