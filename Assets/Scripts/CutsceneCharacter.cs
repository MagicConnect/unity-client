using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Yarn.Unity;

public class CutsceneCharacter : CutsceneObject
{
    // References to the object's components so they don't have to be searched for every time they're needed.
    public Image characterImage;

    public bool isDimming = false;

    public bool isUndimming = false;

    public bool isDimmed = false;

    public GameObject effectsContainer;

    // If animations need to stop playing for whatever reason, or we need to check if there's an animation running,
    // these coroutine handlers will be necessary.
    public Coroutine dimmingCoroutine;
    public Coroutine undimmingCoroutine;

    void Awake()
    {
        characterImage = GetComponent<Image>();
        rectTransform = GetComponent<RectTransform>();

        // Don't show the character after creation unless told to by the yarn script.
        HideObject();
    }

    // Start is called before the first frame update
    void Start()
    {
        
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

    //[YarnCommand("hide_character")]
    public override void HideObject()
    {
        characterImage.enabled = false;
    }

    //[YarnCommand("show_character")]
    public override void ShowObject()
    {
        characterImage.enabled = true;
    }

    //[YarnCommand("move_character")]
    public void MoveCharacter(GameObject stagePosition)
    {
        rectTransform.position = stagePosition.GetComponent<RectTransform>().position;
    }

    public void MoveCharacter(float x, float y)
    {
        rectTransform.position = new Vector3(x, y, rectTransform.position.z);
    }

    // Command to fade a character out until they're invisible. Optional rgb values determine what color the character should fade into,
    // so for example the character can fade to black instead of just turning invisible.
    [YarnCommand("char_hide")]
    public IEnumerator FadeOutCharacter_Handler(float animationTime = 0.0f, bool waitForAnimation = true, float r = 1.0f, float g = 1.0f, float b = 1.0f)
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

        if(animationTime <= 0.0f)
        {
            HideObject();
            yield break;
        }
        else
        {
            fadeOutCoroutine = StartCoroutine(FadeOutCharacter(animationTime, r, g, b));
        }
        
        if(waitForAnimation)
        {
            yield return new WaitUntil(() => fadeOutCoroutine == null);
        }
    }

    public IEnumerator FadeOutCharacter(float animationTime = 0.0f, float r = 1.0f, float g = 1.0f, float b = 1.0f)
    {
        Debug.LogFormat("Cutscene Character {0}: Fading out over {1} seconds.", gameObject.name, animationTime);
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

            if(timePassed < animationTime)
            {
                yield return null;
            }
        }

        // Make sure the desired color result is set after the animation completes, in case the timing was off.
        characterImage.color = newColor;

        // The character is already invisible, but just in case lets deactivate the image too. There might be a performance
        // cost from having too many transparent objects floating around.
        HideObject();

        fadeOutCoroutine = null;
        Debug.LogFormat("Cutscene Character {0}: fade out animation complete.", gameObject.name);
    }

    // Command to fade a character in until they're visible. Optional rgb value arguments determine what the starting color of the
    // character should be, so for example the character can fade in from black.
    // TODO: Potentially add more color value parameters to choose which color the character fades back into.
    [YarnCommand("char_show")]
    public IEnumerator FadeInCharacter_Handler(float animationTime = 0.0f, bool waitForAnimation = true, float r = 1.0f, float g = 1.0f, float b = 1.0f)
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

        if(animationTime <= 0.0f)
        {
            ShowObject();
            yield break;
        }
        else
        {
            fadeInCoroutine = StartCoroutine(FadeInCharacter(animationTime, r, g, b));
        }
        
        if(waitForAnimation)
        {
            yield return new WaitUntil(() => fadeInCoroutine == null);
        }
    }

    public IEnumerator FadeInCharacter(float animationTime = 0.0f, float r = 1.0f, float g = 1.0f, float b = 1.0f)
    {
        Debug.LogFormat("Cutscene Character {0}: Fading in over {1} seconds.", gameObject.name, animationTime);
        float timePassed = 0.0f;
        Color oldColor = new Color(r, g, b, 0.0f);

        // Make sure the character is capable of being seen. By default character images are deactivated until used.
        ShowObject();

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

            if(timePassed < animationTime)
            {
                yield return null;
            }
        }

        // Make sure the desired color result is set after the animation completes, in case the timing was off.
        characterImage.color = Color.white;

        fadeInCoroutine = null;
        Debug.LogFormat("Cutscene Character {0}: Fade in animation complete.", gameObject.name);
    }

    public void SetColor(Color color)
    {
        characterImage.color = color;
    }

    // Yarn Spinner waits for a coroutine command to finish, and we want the option to start the animation and keep
    // the dialogue moving. To solve this problem there needs to be 2 coroutines to handle the same behavior.
    [YarnCommand("char_dim")]
    public IEnumerator DimCharacter_Handler(float animationTime = 0.0f, bool waitForAnimation = true)
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

        // TODO: Make a method which instantly sets the character color to gray, so no coroutine is
        // created when the animation time is 0.
        //dimmingCoroutine = StartCoroutine(DimCharacter(animationTime));

        if(animationTime <= 0.0f)
        {
            SetColor(Color.gray);
            isDimmed = true;
            yield break;
        }
        else
        {
            dimmingCoroutine = StartCoroutine(DimCharacter(animationTime));
        }

        if(waitForAnimation)
        {
            yield return new WaitUntil(() => !isDimming);
        }
    }

    public IEnumerator DimCharacter(float animationTime = 0.0f)
    {
        Debug.LogFormat("Cutscene Character {0}: Dimming over {1} seconds.", gameObject.name, animationTime);
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

            if(timePassed < animationTime)
            {
                yield return null;
            }
        }

        // Make sure the desired color is set after the animation completes, just in case the timing was off by a fraction of a second.
        characterImage.color = Color.gray;

        isDimming = false;
        isDimmed = true;
        dimmingCoroutine = null;
        Debug.LogFormat("Cutscene Character {0}: Dimming animation complete.", gameObject.name);
    }

    // Yarn Spinner waits for a coroutine command to finish, and we want the option to start the animation and keep
    // the dialogue moving. To solve this problem there needs to be 2 coroutines to handle the same behavior.
    [YarnCommand("char_undim")]
    public IEnumerator UndimCharacter_Handler(float animationTime = 0.0f, bool waitForAnimation = true)
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

        // TODO: Make a method which instantly sets the character's color back to the default so no coroutine is
        // started when the animation time is 0.
        //undimmingCoroutine = StartCoroutine(UndimCharacter(animationTime));

        if(animationTime <= 0.0f)
        {
            SetColor(Color.white);
            isDimmed = false;
            yield break;
        }
        else
        {
            undimmingCoroutine = StartCoroutine(UndimCharacter(animationTime));
        }

        if(waitForAnimation)
        {
            yield return new WaitUntil(() => !isUndimming);
        }
    }

    public IEnumerator UndimCharacter(float animationTime = 0.0f)
    {
        Debug.LogFormat("Cutscene Character {0}: Undimming over {1} seconds.", gameObject.name, animationTime);
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

            if(timePassed < animationTime)
            {
                yield return null;
            }
        }

        // Make sure the desired color is set after the animation completes, just in case the timing was off by a fraction of a second.
        characterImage.color = Color.white;

        isUndimming = false;
        isDimmed = false;
        undimmingCoroutine = null;
        Debug.LogFormat("Cutscene Character {0}: Undimming animation complete.", gameObject.name);
    }

    // Moves the character over time to a given "stage position", a preset position on the screen.
    [YarnCommand("char_move_time")]
    public IEnumerator MoveCharacter_Handler(GameObject stagePosition, float animationTime = 0.0f, bool smoothLerp = true, bool waitForAnimation = true)
    {
        // If the stageposition is null then something went wrong with the command.
        if(!stagePosition)
        {
            Debug.LogErrorFormat(this, "Cutscene Character - {0}: Yarn Spinner couldn't find a valid stage position object in the scene. Please check for typos or missing arguments.", gameObject.name);
            yield break;
        }

        // If the character is already moving to a position, cancel that move animation. If we want more complex movement,
        // like zigzagging across the screen, we can change this later or add specialized commands to handle that problem.
        if(movingCoroutine != null)
        {
            StopCoroutine(movingCoroutine);
        }

        if(animationTime <= 0.0f)
        {
            MoveCharacter(stagePosition);
            yield break;
        }
        else
        {
            movingCoroutine = StartCoroutine(MoveCharacter(stagePosition, animationTime, smoothLerp));
        }

        if(waitForAnimation)
        {
            yield return new WaitUntil(() => movingCoroutine == null);
        }
    }

    public IEnumerator MoveCharacter(GameObject stagePosition, float animationTime = 0.0f, bool smoothLerp = false)
    {
        Debug.LogFormat("Cutscene Character {0}: Moving to {1} over {2} seconds.", gameObject.name, stagePosition.name, animationTime);
        float timePassed = 0.0f;
        Vector3 originalPosition = this.rectTransform.position;
        Vector3 destination = stagePosition.transform.position;

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

            if(smoothLerp)
            {
                float t = progress * progress * (3f - 2f * progress);
                this.rectTransform.position = Vector3.Lerp(originalPosition, destination, t);
            }
            else
            {
                this.rectTransform.position = Vector3.Lerp(originalPosition, destination, progress);
            }

            timePassed += Time.deltaTime;

            if(timePassed < animationTime)
            {
                yield return null;
            }
        }
        
        // Make sure that the game object is at the desired final position after the animation completes, just in case the timing isn't exact.
        this.rectTransform.position = stagePosition.transform.position;

        movingCoroutine = null;
        Debug.LogFormat("Cutscene Character {0}: Moving animation complete.", gameObject.name);
    }

    [YarnCommand("char_move_coord_time")]
    public IEnumerator MoveCharacterCoordinate_Handler(float x, float y, float animationTime = 0.0f, bool smoothLerp = true, bool waitForAnimation = true)
    {
        if(movingCoroutine != null)
        {
            StopCoroutine(movingCoroutine);
        }

        //movingCoroutine = StartCoroutine(MoveCharacterToCoordinate(x, y, animationTime, smoothLerp));

        if(animationTime <= 0.0f)
        {
            MoveCharacter(x, y);
            yield break;
        }
        else
        {
            movingCoroutine = StartCoroutine(MoveCharacterToCoordinate(x, y, animationTime, smoothLerp));
        }

        if(waitForAnimation)
        {
            yield return new WaitUntil(() => movingCoroutine == null);
        }
    }

    // Like MoveCharacter()/move_character but for specific coordinates instead of a preset screen position.
    public IEnumerator MoveCharacterToCoordinate(float x, float y, float animationTime = 0.0f, bool smoothLerp = false)
    {
        Debug.LogFormat("Cutscene Character {0}: Moving to ({1},{2}) over {3} seconds.", gameObject.name, x, y, animationTime);
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

            if(smoothLerp)
            {
                float t = progress * progress * (3f - 2f * progress);
                this.rectTransform.position = Vector3.Lerp(originalPosition, newPosition, t);
            }
            else
            {
                this.rectTransform.position = Vector3.Lerp(originalPosition, newPosition, progress);
            }

            timePassed += Time.deltaTime;

            if(timePassed < animationTime)
            {
                yield return null;
            }
        }

        // Make sure that the game object is at the desired final position after the animation completes, just in case the timing isn't exact.
        this.rectTransform.position = newPosition;

        movingCoroutine = null;
        Debug.LogFormat("Cutscene Character {0}: Moving animation complete.", gameObject.name);
    }

    //[YarnCommand("char_move")]
    public IEnumerator MoveCharacterPreset_Handler(int position, float speed, bool smoothLerp = false, bool waitForAnimation = false)
    {
        if(movingCoroutine != null)
        {
            StopCoroutine(movingCoroutine);
        }

        GameObject stagePosition = null;

        switch(position)
        {
            case 0:
                stagePosition = GameObject.Find("FarLeft");
                break;
            case 1: 
                stagePosition = GameObject.Find("Left");
                break;
            case 2: 
                stagePosition = GameObject.Find("Center");
                break;
            case 3:
                stagePosition = GameObject.Find("Right");
                break;
            case 4:
                stagePosition = GameObject.Find("FarRight");
                break;
            case 5:
                stagePosition = GameObject.Find("OffscreenLeft");
                break;
            case 6:
                stagePosition = GameObject.Find("OffscreenRight");
                break;
            default: 
                Debug.LogErrorFormat(this, "There is no stage position corresponding to the number '{0}'.", position);
                yield break;
        }

        movingCoroutine = StartCoroutine(MoveCharacterPositionBySpeed(stagePosition, speed, smoothLerp));

        if(waitForAnimation)
        {
            yield return new WaitUntil(() => movingCoroutine == null);
        }
    }


    // Move the character to a given stage position object in the scene.
    [YarnCommand("char_move")]
    public IEnumerator MoveCharacterPositionSpeed_Handler(GameObject position, float speed, bool smoothLerp = true, bool waitForAnimation = true)
    {
        // If the position object is null, Yarn Spinner couldn't find the gameobject in the scene.
        if(!position)
        {
            Debug.LogErrorFormat(this, "Cutscene Character - {0}: Yarn Spinner couldn't find a valid stage position object in the scene. Please check for typos or missing arguments.", gameObject.name);
            yield break;
        }

        if(movingCoroutine != null)
        {
            StopCoroutine(movingCoroutine);
        }

        movingCoroutine = StartCoroutine(MoveCharacterPositionBySpeed(position, speed, smoothLerp));

        if(waitForAnimation)
        {
            yield return new WaitUntil(() => movingCoroutine == null);
        }
    }

    // The actual movement coroutine which uses a flat rate for movement.
    public IEnumerator MoveCharacterPositionBySpeed(GameObject position, float speed, bool smoothLerp = false)
    {
        Debug.LogFormat(this, "{0} moving to {1}.", gameObject.name, position.name);

        RectTransform positionTransform = position.GetComponent<RectTransform>();

        // We need to store the original positions of both the origin and the destination because the transforms
        // of either object can and will change over the duration of the animation.
        Vector3 originalPosition = rectTransform.position;
        Vector3 destination = positionTransform.position;

        float distance = Vector3.Distance(positionTransform.position, rectTransform.position);
        float timeToMove = distance / speed;
        float timePassed = 0.0f;

        while(timePassed <= timeToMove)
        {
            float progress;
            
            if(timeToMove <= 0.0f)
            {
                progress = 1.0f;
            }
            else
            {
                progress = timePassed / timeToMove;
            }

            if(smoothLerp)
            {
                float t = progress * progress * (3f - 2f * progress);
                this.rectTransform.position = Vector3.Lerp(originalPosition, destination, t);
            }
            else
            {
                this.rectTransform.position = Vector3.Lerp(originalPosition, destination, progress);
            }

            timePassed += Time.deltaTime;

            // If the timepassed value equals or exceeds the time to move, don't bother advancing to the next frame.
            if(timePassed < timeToMove)
            {
                yield return null;
            }
        }

        // Set the object's position to the target position, in case we overshoot due to framerate nonsense.
        rectTransform.position = positionTransform.position;

        movingCoroutine = null;

        Debug.LogFormat(this, "{0} finished moving to {1}.", gameObject.name, position.name);
    }

    // Removes this character from the scene.
    [YarnCommand("char_clear")]
    public void ClearCharacter()
    {
        Destroy(this.gameObject);
    }

    // Removes all visual effects attached to this character.
    [YarnCommand("vfx_char_clear")]
    public void ClearAllVfx()
    {
        foreach(Transform effect in effectsContainer.transform)
        {
            Destroy(effect.gameObject);
        }
    }

    public override bool IsAnimating()
    {
        bool isFading = fadeInCoroutine != null || fadeOutCoroutine != null;
        bool isMoving = movingCoroutine != null;
        bool isDimming = dimmingCoroutine != null || undimmingCoroutine != null;
        bool isScaling = scalingCoroutine != null;
        bool isRotating = rotationCoroutine != null;

        return isFading || isMoving || isDimming || isScaling || isRotating;
    }
}
