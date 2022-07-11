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
    public Coroutine scalingCoroutine;
    public Coroutine rotationCoroutine;

    // This flag can be used for checking if an object is animating (like for the wait_anim command).
    // Each cutscene object implementation should be responsible for setting this.
    public bool isAnimating = false;

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

    //[YarnCommand("obj_scale")]
    public void SetObjectScale(float x, float y, float z)
    {
        rectTransform.localScale = new Vector3(x, y, z);
    }

    [YarnCommand("obj_scale")]
    public IEnumerator AnimateObjectScale_Handler(float x, float y, float z, float animationTime, bool waitForAnimation)
    {
        // If there's already a scaling animation running, cancel it.
        if(scalingCoroutine != null)
        {
            StopCoroutine(scalingCoroutine);
        }

        scalingCoroutine = StartCoroutine(AnimateObjectScale(x, y, z, animationTime));
        
        if(waitForAnimation)
        {
            yield return new WaitUntil(() => scalingCoroutine == null);
        }
    }

    public IEnumerator AnimateObjectScale(float x, float y, float z, float animationTime)
    {
        Debug.LogFormat("{0} scaling by ({1}, {2}, {3}) over {4} seconds.", gameObject.name, animationTime);
        float timePassed = 0.0f;
        Vector3 originalScale = rectTransform.localScale;
        Vector3 newScale = new Vector3(x, y, z);

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

            rectTransform.localScale = Vector3.Lerp(originalScale, newScale, progress);

            timePassed += Time.deltaTime;
            yield return null;
        }

        // Make sure the desired scale result is set after the animation completes, in case the timing was off.
        rectTransform.localScale = newScale;

        scalingCoroutine = null;
        Debug.LogFormat("Cutscene Object {0}: Scaling animation complete.", gameObject.name);
    }

    //[YarnCommand("set_object_rotation")]
    public void SetObjectRotation(float x, float y, float z)
    {
        rectTransform.Rotate(x, y, z);
    }

    [YarnCommand("obj_rotation")]
    public IEnumerator AnimateObjectRotation_Handler(float x, float y, float z, float animationTime, bool waitForAnimation)
    {
        // If there's already a rotation animation running, cancel it.
        if(rotationCoroutine != null)
        {
            StopCoroutine(rotationCoroutine);
        }

        rotationCoroutine = StartCoroutine(AnimateObjectRotation(x, y, z, animationTime));
        
        if(waitForAnimation)
        {
            yield return new WaitUntil(() => rotationCoroutine == null);
        }
    }

    public IEnumerator AnimateObjectRotation(float x, float y, float z, float animationTime)
    {
        Debug.LogFormat("{0} rotating by ({1}, {2}, {3}) over {4} seconds.", gameObject.name, animationTime);
        float timePassed = 0.0f;
        Quaternion originalRotation = rectTransform.rotation;
        Quaternion newRotation = Quaternion.Euler(x, y, z);

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

            rectTransform.rotation = Quaternion.Lerp(originalRotation, newRotation, progress);

            timePassed += Time.deltaTime;
            yield return null;
        }

        // Make sure the desired rotation result is set after the animation completes, in case the timing was off.
        rectTransform.rotation = newRotation;

        rotationCoroutine = null;
        Debug.LogFormat("Cutscene Object {0}: Rotation animation complete.", gameObject.name);
    }

    // Starts a rotation "effect" that plays until told to stop. The x,y,z values are the rate of rotation in degrees per second.
    //[YarnCommand("start_continuous_rotation")]
    public void StartContinuousObjectRotation(float x, float y, float z)
    {
        // If there's already a rotation animation running, cancel it.
        if(rotationCoroutine != null)
        {
            StopCoroutine(rotationCoroutine);
        }

        rotationCoroutine = StartCoroutine(ContinuousObjectRotation(x, y, z));
    }

    // This rotation coroutine will run until told to stop.
    public IEnumerator ContinuousObjectRotation(float x, float y, float z)
    {
        while(true)
        {
            rectTransform.Rotate(x * Time.deltaTime, y * Time.deltaTime, z * Time.deltaTime);

            yield return null;
        }
    }

    // Stops any rotation animations playing on the object, including the continous rotation.
    //[YarnCommand("stop_object_rotation")]
    public void StopObjectRotation()
    {
        if(rotationCoroutine != null)
        {
            StopCoroutine(rotationCoroutine);
            rotationCoroutine = null;
        }
    }

    // TODO: Show and hide object currently work for sprite-based cutscene objects. It probably won't work for other objects with
    // different rendering components, like particle effects. Either adapt these for multiple cutscene object types, or move the functionality
    // to the descendant components.
    //[YarnCommand("hide_object")]
    public virtual void HideObject()
    {
        objectImage.enabled = false;
    }

    //[YarnCommand("show_object")]
    public virtual void ShowObject()
    {
        objectImage.enabled = true;
    }

    // Command to fade a character out until they're invisible. Optional rgb values determine what color the character should fade into,
    // so for example the character can fade to black instead of just turning invisible.
    [YarnCommand("obj_hide")]
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
    [YarnCommand("obj_show")]
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
    [YarnCommand("obj_move")]
    public IEnumerator MoveObject_Handler(GameObject stagePosition, float animationTime = 0.2f, bool waitForAnimation = false)
    {
        if(!stagePosition)
        {
            yield break;
        }

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

    [YarnCommand("obj_move_coord")]
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

    // Convenience method to check if the cutscene object is currently performing any animations.
    public virtual bool IsAnimating()
    {
        if(fadeInCoroutine != null)
        {
            return true;
        }

        if(fadeOutCoroutine != null)
        {
            return true;
        }

        if(movingCoroutine != null)
        {
            return true;
        }

        if(scalingCoroutine != null)
        {
            return true;
        }

        if(rotationCoroutine != null)
        {
            return true;
        }

        return false;
    }
}
