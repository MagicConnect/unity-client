using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Yarn.Unity;
using UnityEngine.UI;

public class CutsceneObject : MonoBehaviour
{
    private Image objectImage;
    public RectTransform rectTransform;

    public Coroutine movingCoroutine;
    public Coroutine fadeOutCoroutine;
    public Coroutine fadeInCoroutine;

    void Awake()
    {

    }

    // Start is called before the first frame update
    void Start()
    {
        objectImage = GetComponent<Image>();
        rectTransform = GetComponent<RectTransform>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    [YarnCommand("set_object_scale")]
    public void SetObjectScale(float x, float y, float z)
    {
        rectTransform.localScale = new Vector3(x, y, z);
    }

    [YarnCommand("animate_object_scale")]
    public IEnumerator AnimateObjectScale(float x, float y, float z, float animationTime, bool waitForAnimation)
    {
        yield break;
    }

    public IEnumerator AnimateObjectScaleAsync(float x, float y, float z, float animationTIme)
    {
        yield break;
    }

    [YarnCommand("set_object_rotation")]
    public void SetObjectRotation(float x, float y, float z)
    {
        rectTransform.Rotate(x, y, z);
    }

    [YarnCommand("animate_object_rotation")]
    public IEnumerator AnimateObjectRotation(float x, float y, float z, float animationTime, bool waitForAnimation)
    {
        yield break;
    }

    public IEnumerator AnimateObjectRotationAsync(float x, float y, float z, float animationTime)
    {
        yield break;
    }

    // Starts a rotation "effect" that plays until told to stop. The x,y,z values are the rate of rotation in degrees per second.
    [YarnCommand("set_continuous_rotation")]
    public void ContinuousObjectRotation(float x, float y, float z)
    {

    }

    // This rotation coroutine will run until told to stop.
    public IEnumerator ContinuousObjectRotationCoroutine(float x, float y, float z)
    {
        yield break;
    }

    // Stops any rotation animations playing on the object, including the continous rotation.
    [YarnCommand("stop_object_rotation")]
    public void StopObjectRotation()
    {

    }

    // TODO: Show and hide object currently work for sprite-based cutscene objects. It probably won't work for other objects with
    // different rendering components, like particle effects. Either adapt these for multiple cutscene object types, or move the functionality
    // to the descendant components.
    [YarnCommand("hide_object")]
    public void HideObject()
    {
        objectImage.enabled = false;
    }

    [YarnCommand("show_object")]
    public void ShowObject()
    {
        objectImage.enabled = true;
    }

    // Command to fade a character out until they're invisible. Optional rgb values determine what color the character should fade into,
    // so for example the character can fade to black instead of just turning invisible.
    [YarnCommand("fade_out_object")]
    public IEnumerator FadeOutObject_Handler(float animationTime = 0.2f, bool waitForAnimation = false, float r = 255.0f, float g = 255.0f, float b = 255.0f)
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

        fadeOutCoroutine = StartCoroutine(FadeOutObject(animationTime, r, g, b));
        
        if(waitForAnimation)
        {
            yield return new WaitUntil(() => fadeOutCoroutine == null);
        }
    }

    public IEnumerator FadeOutObject(float animationTime = 0.2f, float r = 255.0f, float g = 255.0f, float b = 255.0f)
    {
        Debug.LogFormat("{0} fading out over {1} seconds.", gameObject.name, animationTime);
        float timePassed = 0.0f;
        Color newColor = new Color(r, g, b, 0.0f);
        Color oldColor = objectImage.color;

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

            objectImage.color = Color.Lerp(oldColor, newColor, progress);

            timePassed += Time.deltaTime;
            yield return null;
        }

        // Make sure the desired color result is set after the animation completes, in case the timing was off.
        objectImage.color = newColor;

        // The character is already invisible, but just in case lets deactivate the image too. There might be a performance
        // cost from having too many transparent objects floating around.
        HideObject();

        fadeOutCoroutine = null;
        Debug.LogFormat("Cutscene Object {0}: Fade out animation complete.", gameObject.name);
    }

    // Command to fade a character in until they're visible. Optional rgb value arguments determine what the starting color of the
    // character should be, so for example the character can fade in from black.
    // TODO: Potentially add more color value parameters to choose which color the character fades back into.
    [YarnCommand("fade_in_object")]
    public IEnumerator FadeInObject_Handler(float animationTime = 0.2f, bool waitForAnimation = false, float r = 255.0f, float g = 255.0f, float b = 255.0f)
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

        fadeInCoroutine = StartCoroutine(FadeInObject(animationTime, r, g, b));
        
        if(waitForAnimation)
        {
            yield return new WaitUntil(() => fadeInCoroutine == null);
        }
    }

    public IEnumerator FadeInObject(float animationTime = 0.2f, float r = 255.0f, float g = 255.0f, float b = 255.0f)
    {
        Debug.LogFormat("Cutsene Object {0}: Fading in over {1} seconds.", gameObject.name, animationTime);
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

            objectImage.color = Color.Lerp(oldColor, Color.white, progress);

            timePassed += Time.deltaTime;
            yield return null;
        }

        // Make sure the desired color result is set after the animation completes, in case the timing was off.
        objectImage.color = Color.white;

        fadeInCoroutine = null;
        Debug.LogFormat("Cutscene Object {0}: Fade in animation complete.", gameObject.name);
    }

    // Moves the character over time to a given "stage position", a preset position on the screen.
    [YarnCommand("move_object")]
    public IEnumerator MoveObject_Handler(GameObject stagePosition, float animationTime = 0.2f, bool waitForAnimation = false)
    {
        // If the character is already moving to a position, cancel that move animation. If we want more complex movement,
        // like zigzagging across the screen, we can change this later or add specialized commands to handle that problem.
        if(movingCoroutine != null)
        {
            StopCoroutine(movingCoroutine);
        }

        movingCoroutine = StartCoroutine(MoveObject(stagePosition, animationTime));

        if(waitForAnimation)
        {
            yield return new WaitUntil(() => movingCoroutine == null);
        }
    }

    public IEnumerator MoveObject(GameObject stagePosition, float animationTime = 0.2f)
    {
        Debug.LogFormat("Cutscene Object {0}: Moving to {1} over {2} seconds.", gameObject.name, stagePosition.name, animationTime);
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
        Debug.LogFormat("Cutscene Object {0}: Moving animation complete.", gameObject.name);
    }

    [YarnCommand("move_object_to_coordinate")]
    public IEnumerator MoveObjectCoordinate_Handler(float x, float y, float animationTime = 0.2f, bool waitForAnimation = false)
    {
        if(movingCoroutine != null)
        {
            StopCoroutine(movingCoroutine);
        }

        movingCoroutine = StartCoroutine(MoveObjectToCoordinate(x, y, animationTime));

        if(waitForAnimation)
        {
            yield return new WaitUntil(() => movingCoroutine == null);
        }
    }

    // Like MoveCharacter()/move_character but for specific coordinates instead of a preset screen position.
    public IEnumerator MoveObjectToCoordinate(float x, float y, float animationTime = 0.2f)
    {
        Debug.LogFormat("Cutscene Object {0}: Moving to ({1},{2}) over {3} seconds.", gameObject.name, x, y, animationTime);
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
        Debug.LogFormat("Cutscene Object {0}: Moving animation complete.", gameObject.name);
    }
}
