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
    Coroutine fadeInCoroutine;
    Coroutine fadeOutCoroutine;

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

    // Command to fade a character out until they're invisible. Optional rgb values determine what color the character should fade into,
    // so for example the character can fade to black instead of just turning invisible.
    [YarnCommand("fade_out_character")]
    public IEnumerator FadeOutAsync(float animationTime = 0.2f, bool waitForAnimation = false, float r = 255.0f, float g = 255.0f, float b = 255.0f)
    {
        // If there's already a fade out animation running, cancel it.
        if(fadeOutCoroutine != null)
        {
            StopCoroutine(fadeOutCoroutine);
        }

        // If there's a fade in animation running, cancel it so they don't conflict with one another.
        if(fadeInCoroutine != null)
        {
            // Note: Any cleanup the fade in coroutine would have had to do should be handled here, or
            // refactored into a special StopFadeInCoroutine() method.
            StopCoroutine(fadeInCoroutine);
            fadeInCoroutine = null;
        }

        fadeOutCoroutine = StartCoroutine(FadeOut(animationTime, r, g, b));
        
        if(waitForAnimation)
        {
            yield return new WaitUntil(() => fadeOutCoroutine == null);
        }
    }

    public IEnumerator FadeOut(float animationTime = 0.2f, float r = 255.0f, float g = 255.0f, float b = 255.0f)
    {
        Debug.LogFormat("{0} fading out over {1} seconds.", gameObject.name, animationTime);
        float timePassed = 0.0f;
        Color newColor = new Color(r, g, b, 0.0f);
        Color oldColor = characterImage.color;

        while(timePassed <= animationTime)
        {
            float progress;

            // Make sure not to divide by 0.
            if(animationTime <= 0.0f)
            {
                progress = 1.0f;
            }
            else
            {
                progress = timePassed / animationTime;
            }

            characterImage.color = Color.Lerp(oldColor, newColor, progress);

            timePassed += Time.deltaTime;
            yield return null;
        }

        // Make sure the desired color result is set after the animation completes, in case the timing was off.
        characterImage.color = newColor;

        // The character is already invisible, but just in case lets deactivate the image too. There might be a performance
        // cost from having too many transparent objects floating around.
        HideCharacter();

        fadeOutCoroutine = null;
        Debug.LogFormat("{0} fade out animation complete.", gameObject.name);
    }

    // Command to fade a character in until they're visible. Optional rgb value arguments determine what the starting color of the
    // character should be, so for example the character can fade in from black.
    // TODO: Potentially add more color value parameters to choose which color the character fades back into.
    [YarnCommand("fade_in_character")]
    public IEnumerator FadeInAsync(float animationTime = 0.2f, bool waitForAnimation = false, float r = 255.0f, float g = 255.0f, float b = 255.0f)
    {
        if(fadeInCoroutine != null)
        {
            StopCoroutine(fadeInCoroutine);
        }

        // If there's a fade out animation running, cancel it so they don't conflict with one another.
        if(fadeOutCoroutine != null)
        {
            // Note: Any cleanup the fade out coroutine would have had to do should be handled here, or
            // refactored into a special StopFadeInCoroutine() method.
            StopCoroutine(fadeOutCoroutine);
            fadeOutCoroutine = null;
        }

        fadeInCoroutine = StartCoroutine(FadeIn(animationTime, r, g, b));
        
        if(waitForAnimation)
        {
            yield return new WaitUntil(() => fadeInCoroutine == null);
        }
    }

    public IEnumerator FadeIn(float animationTime = 0.2f, float r = 255.0f, float g = 255.0f, float b = 255.0f)
    {
        Debug.LogFormat("{0} fading in over {1} seconds.", gameObject.name, animationTime);
        float timePassed = 0.0f;
        Color oldColor = new Color(r, g, b, 0.0f);

        // Make sure the character is capable of being seen. By default character images are deactivated until used.
        ShowCharacter();

        while(timePassed <= animationTime)
        {
            float progress;

            // Make sure not to divide by 0.
            if(animationTime <= 0.0f)
            {
                progress = 1.0f;
            }
            else
            {
                progress = timePassed / animationTime;
            }

            characterImage.color = Color.Lerp(oldColor, Color.white, progress);

            timePassed += Time.deltaTime;
            yield return null;
        }

        // Make sure the desired color result is set after the animation completes, in case the timing was off.
        characterImage.color = Color.white;

        fadeInCoroutine = null;
        Debug.LogFormat("{0} fade in animation complete.", gameObject.name);
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
    public IEnumerator DimCharacterAsync(float animationTime = 0.2f, bool waitForAnimation = false)
    {
        // If there's already a dimming animation playing, override it.
        if(dimmingCoroutine != null)
        {
            StopCoroutine(dimmingCoroutine);
        }

        // If there's an undimming animation playing, cancel it so there isn't a conflict.
        if(undimmingCoroutine != null)
        {
            StopCoroutine(undimmingCoroutine);
            undimmingCoroutine = null;
            isUndimming = false;
        }

        dimmingCoroutine = StartCoroutine(DimCharacter(animationTime));

        if(waitForAnimation)
        {
            yield return new WaitUntil(() => !isDimming);
        }
    }

    public IEnumerator DimCharacter(float animationTime = 0.2f)
    {
        Debug.LogFormat("{0}: Dimming over {1} seconds.", gameObject.name, animationTime);
        isDimming = true;
        float timePassed = 0.0f;

        while(timePassed <= animationTime)
        {
            float progress;

            // Make sure not to divide by 0.
            if(animationTime <= 0.0f)
            {
                progress = 1.0f;
            }
            else
            {
                progress = timePassed / animationTime;
            }

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
    public IEnumerator UndimCharacterAsync(float animationTime = 0.2f, bool waitForAnimation = false)
    {
        // If there's already an undimming animation playing, override it.
        if(undimmingCoroutine != null)
        {
            StopCoroutine(undimmingCoroutine);
        }

        // If there's already a dimming animation playing, cancel it so there's no conflict.
        if(dimmingCoroutine != null)
        {
            StopCoroutine(dimmingCoroutine);
            dimmingCoroutine = null;
            isDimming = false;
        }

        undimmingCoroutine = StartCoroutine(UndimCharacter(animationTime));

        if(waitForAnimation)
        {
            yield return new WaitUntil(() => !isUndimming);
        }
    }

    public IEnumerator UndimCharacter(float animationTime = 0.2f)
    {
        Debug.LogFormat("{0}: Undimming over {1} seconds.", gameObject.name, animationTime);
        isUndimming = true;
        float timePassed = 0.0f;

        while(timePassed <= animationTime)
        {
            float progress;

            // Make sure not to divide by 0.
            if(animationTime <= 0.0f)
            {
                progress = 1.0f;
            }
            else
            {
                progress = timePassed / animationTime;
            }

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
    public IEnumerator SlideCharacterAsync(GameObject stagePosition, float animationTime = 0.2f, bool waitForAnimation = false)
    {
        // If the character is already moving to a position, cancel that move animation. If we want more complex movement,
        // like zigzagging across the screen, we can change this later or add specialized commands to handle that problem.
        if(movingCoroutine != null)
        {
            StopCoroutine(movingCoroutine);
        }

        movingCoroutine = StartCoroutine(SlideCharacter(stagePosition, animationTime));

        if(waitForAnimation)
        {
            yield return new WaitUntil(() => movingCoroutine == null);
        }
    }

    public IEnumerator SlideCharacter(GameObject stagePosition, float animationTime = 0.2f)
    {
        Debug.LogFormat("{0}: Moving to {1} over {2} seconds.", gameObject.name, stagePosition.name, animationTime);
        float timePassed = 0.0f;
        Vector3 originalPosition = this.rectTransform.position;

        while(timePassed <= animationTime)
        {
            float progress;

            if(animationTime <= 0.0f)
            {
                progress = 1.0f;
            }
            else
            {
                progress = timePassed / animationTime;
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
    public IEnumerator MoveCharacterCoordinateAsync(float x, float y, float animationTime = 0.2f, bool waitForAnimation = false)
    {
        if(movingCoroutine != null)
        {
            StopCoroutine(movingCoroutine);
        }

        movingCoroutine = StartCoroutine(MoveCharacterToCoordinate(x, y, animationTime));

        if(waitForAnimation)
        {
            yield return new WaitUntil(() => movingCoroutine == null);
        }
    }

    // Like MoveCharacter()/move_character but for specific coordinates instead of a preset screen position.
    public IEnumerator MoveCharacterToCoordinate(float x, float y, float animationTime = 0.2f)
    {
        Debug.LogFormat("{0}: Moving to ({1},{2}) over {3} seconds.", gameObject.name, x, y, animationTime);
        float timePassed = 0.0f;
        Vector3 originalPosition = this.rectTransform.position;
        Vector3 newPosition = new Vector3(x, y);

        while(timePassed <= animationTime)
        {
            float progress;
            
            if(animationTime <= 0.0f)
            {
                progress = 1.0f;
            }
            else
            {
                progress = timePassed / animationTime;
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
