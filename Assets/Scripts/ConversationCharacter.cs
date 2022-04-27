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
    Coroutine movingCoroutine;

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

    // TODO: Right now since animations are pretty simple there aren't any issues with stopping animations
    // before they're done. As long as the coroutine is "pure" and doesn't change any global settings or create
    // new gameobjects (or something along those lines) everything should be cleaned up and ready to go.
    // However, as we add more animations, manipulate game data, and allow delaying and queuing of animations,
    // it will be important to make sure each coroutine is cleaned up properly if it has to be canceled for
    // whatever reason. For example, if a move animation has to be canceled, make sure the movement coroutine
    // is set to null. If a queue of animations has to be canceled, make sure the queue is cleared and all
    // queued coroutines are stopped and set to null. If a boolean flag is set during the coroutine to indicate
    // the game object's state, reset the flag. So on and so forth.

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

    //[YarnCommand("move_character")]
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
    // TODO: For dimming and undimming characters, even though they are two separate commands it can be assumed
    // that they are mutually exclusive in their use. That is, if the character is dimming and the writer says to
    // undim the character, then the dimming animation should be canceled before the undimming animation plays. 
    [YarnCommand("dim_character")]
    public IEnumerator DimCharacterAsync(float timeToComplete = 0.2f, bool waitForAnimation = false)
    {
        // If there's already a dimming animation playing, override it.
        if(dimmingCoroutine != null)
        {
            StopCoroutine(dimmingCoroutine);
        }

        dimmingCoroutine = StartCoroutine(DimCharacter(timeToComplete));

        // TODO: The else block may be unnecessary, or at least the yield return is. Right now it's basically waiting
        // for a frame for no reason.
        if(waitForAnimation)
        {
            yield return new WaitUntil(() => !isDimming);
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

        // Make sure the desired color is set after the animation completes, just in case the timing was off by a fraction of a second.
        characterImage.color = Color.gray;

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

        // TODO: The else block may be unnecessary, or at least the yield return is. Right now it's basically waiting
        // for a frame for no reason.
        if(waitForAnimation)
        {
            yield return new WaitUntil(() => !isUndimming);
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

        // Make sure the desired color is set after the animation completes, just in case the timing was off by a fraction of a second.
        characterImage.color = Color.white;

        isUndimming = false;
        isDimmed = false;
        undimmingCoroutine = null;
        Debug.LogFormat("{0}: Undimming animation complete.", gameObject.name);
    }

    // Moves the character over time to a given "stage position", a preset position on the screen.
    [YarnCommand("move_character")]
    public IEnumerator SlideCharacterAsync(GameObject stagePosition, float timeToComplete = 0.2f, bool waitForAnimation = false)
    {
        // If the character is already moving to a position, cancel that move animation. If we want more complex movement,
        // like zigzagging across the screen, we can change this later or add specialized commands to handle that problem.
        if(movingCoroutine != null)
        {
            StopCoroutine(movingCoroutine);
        }

        movingCoroutine = StartCoroutine(SlideCharacter(stagePosition, timeToComplete));

        if(waitForAnimation)
        {
            yield return new WaitUntil(() => movingCoroutine == null);
        }
    }

    public IEnumerator SlideCharacter(GameObject stagePosition, float timeToComplete = 0.2f)
    {
        Debug.LogFormat("{0}: Moving to {1} over {2} seconds.", gameObject.name, stagePosition.name, timeToComplete);
        float timePassed = 0.0f;
        Vector3 originalPosition = this.rectTransform.position;

        while(timePassed <= timeToComplete)
        {
            float progress;

            if(timeToComplete <= 0.0f)
            {
                progress = 1.0f;
            }
            else
            {
                progress = timePassed / timeToComplete;
            }

            this.rectTransform.position = Vector3.Lerp(originalPosition, stagePosition.transform.position, progress);

            timePassed += Time.deltaTime;
            yield return null;
        }
        
        // Make sure that the game object is at the desired final position after the animation completes, just in case the timing isn't exact.
        this.rectTransform.position = stagePosition.transform.position;

        movingCoroutine = null;
        Debug.LogFormat("{0}: Moving animation complete.", gameObject.name);
    }

    [YarnCommand("move_character_to_coordinate")]
    public IEnumerator MoveCharacterCoordinateAsync(float x, float y, float timeToComplete = 0.2f, bool waitForAnimation = false)
    {
        if(movingCoroutine != null)
        {
            StopCoroutine(movingCoroutine);
        }

        movingCoroutine = StartCoroutine(MoveCharacterToCoordinate(x, y, timeToComplete));

        if(waitForAnimation)
        {
            yield return new WaitUntil(() => movingCoroutine == null);
        }
    }

    // Like MoveCharacter()/move_character but for specific coordinates instead of a preset screen position.
    public IEnumerator MoveCharacterToCoordinate(float x, float y, float timeToComplete = 0.2f)
    {
        Debug.LogFormat("{0}: Moving to ({1},{2}) over {3} seconds.", gameObject.name, x, y, timeToComplete);
        float timePassed = 0.0f;
        Vector3 originalPosition = this.rectTransform.position;
        Vector3 newPosition = new Vector3(x, y);

        while(timePassed <= timeToComplete)
        {
            float progress;
            
            if(timeToComplete <= 0.0f)
            {
                progress = 1.0f;
            }
            else
            {
                progress = timePassed / timeToComplete;
            }

            this.rectTransform.position = Vector3.Lerp(originalPosition, newPosition, progress);

            timePassed += Time.deltaTime;
            yield return null;
        }

        // Make sure that the game object is at the desired final position after the animation completes, just in case the timing isn't exact.
        this.rectTransform.position = newPosition;

        movingCoroutine = null;
        Debug.LogFormat("{0}: Moving animation complete.", gameObject.name);
    }
}
