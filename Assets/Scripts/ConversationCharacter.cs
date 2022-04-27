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

    public bool isDimming = false;

    public bool isUndimming = false;

    public bool isDimmed = false;

    // If animations need to stop playing for whatever reason, or we need to check if there's an animation running,
    // these coroutine handlers will be necessary.
    Coroutine dimmingCoroutine;
    Coroutine undimmingCoroutine;

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

    public IEnumerator ShowCharacterAnimation(float timeToComplete)
    {
        yield return null;
    }

    public IEnumerator HideCharacterAnimation(float timeToComplete)
    {
        yield return null;
    }

    // Yarn Spinner waits for a coroutine command to finish, and we want the option to start the animation and keep
    // the dialogue moving. To solve this problem there needs to be 2 coroutines to handle the same behavior.
    [YarnCommand("dim_character")]
    public IEnumerator DimCharacterAsync(float timeToComplete = 0.2f, bool waitForAnimation = false)
    {
        // If there's already a dimming animation playing, override it.
        if(dimmingCoroutine != null)
        {
            StopCoroutine(dimmingCoroutine);
        }

        dimmingCoroutine = StartCoroutine(DimCharacter(timeToComplete));

        if(waitForAnimation)
        {
            yield return new WaitUntil(() => !isDimming);
        }
        else
        {
            yield return null;
        }
    }

    public IEnumerator DimCharacter(float timeToComplete = 0.2f)
    {
        Debug.LogFormat("{0}: Dimming over {1} seconds.", gameObject.name, timeToComplete);
        isDimming = true;
        float timePassed = 0.0f;

        while(timePassed <= timeToComplete)
        {
            float progress = timePassed / timeToComplete;
            characterImage.color = Color.Lerp(Color.white, Color.gray, progress);

            timePassed += Time.deltaTime;
            yield return null;
        }

        isDimming = false;
        isDimmed = true;
        dimmingCoroutine = null;
        Debug.LogFormat("{0}: Dimming animation complete.", gameObject.name);
    }

    // Yarn Spinner waits for a coroutine command to finish, and we want the option to start the animation and keep
    // the dialogue moving. To solve this problem there needs to be 2 coroutines to handle the same behavior.
    [YarnCommand("undim_character")]
    public IEnumerator UndimCharacterAsync(float timeToComplete = 0.2f, bool waitForAnimation = false)
    {
        // If there's already an undimming animation playing, override it.
        if(undimmingCoroutine != null)
        {
            StopCoroutine(undimmingCoroutine);
        }

        undimmingCoroutine = StartCoroutine(UndimCharacter(timeToComplete));

        if(waitForAnimation)
        {
            yield return new WaitUntil(() => !isUndimming);
        }
        else
        {
            yield return null;
        }
    }

    public IEnumerator UndimCharacter(float timeToComplete = 0.2f)
    {
        Debug.LogFormat("{0}: Undimming over {1} seconds.", gameObject.name, timeToComplete);
        isUndimming = true;
        float timePassed = 0.0f;

        while(timePassed <= timeToComplete)
        {
            float progress = timePassed / timeToComplete;
            characterImage.color = Color.Lerp(Color.gray, Color.white, progress);

            timePassed += Time.deltaTime;
            yield return null;
        }

        isUndimming = false;
        isDimmed = false;
        undimmingCoroutine = null;
        Debug.LogFormat("{0}: Undimming animation complete.", gameObject.name);
    }

    public IEnumerator SlideCharacter(GameObject stagePosition, float timeToComplete)
    {
        yield return null;
    }
}
