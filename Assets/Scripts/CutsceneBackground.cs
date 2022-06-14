using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Yarn.Unity;

public class CutsceneBackground : CutsceneObject
{
    // This holds all the possible background sprites that the writer can choose between, identified by
    // the name of the background in the asset manifest.
    public Dictionary<string, Sprite> backgroundSprites = new Dictionary<string, Sprite>();

    // The image component of this background object.
    private Image image;

    public Image defaultImage;

    public Image alternateImage;

    // Because of overlapping animations, it'll be useful to know what the actual desired color of the image is,
    // rather than the one it is currently animated to.
    public Color trueColor;

    // TODO: Evaluate whether we want these coroutines to be exposed to other classes. It may not really matter since
    // Unity allows anyone anywhere to start and stop coroutines on a gameobject, but at least it might indicate to a developer
    // that they should use some controlled method of getting the animation state, stopping animations, etc.
    public Coroutine colorChangeCoroutine {get; private set;}

    public Coroutine imageChangeCoroutine {get; private set;}

    // Until we have a pool for sprites and other assets, we need a reference to the cutscene manager where all the loaded
    // sprites are stored.
    public CutsceneManager cutsceneManager;

    void Awake()
    {
        // Set the image component and configure it for proper display.
        //image = GetComponent<Image>();
        //image.preserveAspect = true;

        cutsceneManager = GameObject.Find("Cutscene Manager").GetComponent<CutsceneManager>();
        rectTransform = GetComponent<RectTransform>();

        defaultImage.preserveAspect = true;
        alternateImage.preserveAspect = true;

        /*
        // The cache should be loaded before the game ever gets here but it doesn't hurt to check.
        if(WebAssetCache.Instance.status == WebAssetCache.WebCacheStatus.ReadyToUse)
        {
            List<WebAssetCache.LoadedImageAsset> assets = WebAssetCache.Instance.GetLoadedAssetsByCategory("backgrounds");

            if(assets.Count <= 0)
            {
                Debug.LogWarningFormat("Cutscene Background: No loaded assets returned by cache.");
            }

            foreach(WebAssetCache.LoadedImageAsset asset in assets)
            {
                Sprite sprite = Sprite.Create(asset.texture, new Rect(0.0f, 0.0f, asset.texture.width, asset.texture.height), new Vector2(0.0f, 0.0f), 100.0f);
                backgroundSprites.Add(asset.name, sprite);

                Debug.LogFormat("Cutscene Background: Loaded background sprite '{0}'.", asset.name);
            }
        }
        else
        {
            Debug.LogErrorFormat("Cutscene Background: Attempted to load background when cache was not fully loaded.");
        }
        */
    }

    // Start is called before the first frame update
    void Start()
    {
        trueColor = defaultImage.color;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // Change the background image to a loaded sprite of the given name.
    //[YarnCommand("set_background_image")]
    public void SetImage(string name)
    {
        // Get the sprite from the pool (cutscene manager).
        Sprite newSprite = cutsceneManager.GetSprite(name);
        if(newSprite)
        {
            // I don't think the alternate image needs to be changed at all.
            defaultImage.sprite = newSprite;
        }
        else
        {
            // If the sprite reference is null then it wasn't possible to get one using the given asset name.
            Debug.LogErrorFormat("Cutscene Background ({0}): Cannot change image because there is no sprite with the name '{1}'.", gameObject.name, name);
            return;
        }
    }

    //[YarnCommand("change_background_image")]
    public IEnumerator AnimateImageChange_Handler(string name, float animationTime = 0.0f, bool waitForAnimation = false)
    {
        if(imageChangeCoroutine != null)
        {
            StopCoroutine(imageChangeCoroutine);
        }

        imageChangeCoroutine = StartCoroutine(ChangeImage(name, animationTime));

        if(waitForAnimation)
        {
            yield return new WaitUntil(() => imageChangeCoroutine == null);
        }
    }

    public IEnumerator ChangeImage(string name, float animationTime = 0.0f)
    {
        // Get the sprite from the pool (cutscene manager).
        Sprite newSprite = cutsceneManager.GetSprite(name);
        if(!newSprite)
        {
            // If the sprite reference is null then it wasn't possible to get one using the given asset name.
            Debug.LogErrorFormat("Cutscene Background ({0}): Cannot change image because there is no sprite with the name '{1}'.");
            yield break;
        }

        // TODO: Because animations can be canceled, we may need a more robust way of setting colors over time.
        // Otherwise, this might consider the alpha transparency of the partially animated background to be part of the original color
        // to keep track of. Maybe reset the alpha in the start/stop animation methods?
        Debug.LogFormat("Cutscene Background: Changing image to '{0}' over {1} seconds.", name, animationTime);
        float timePassed = 0.0f;
        //Color oldColor = defaultImage.color;

        // Enable the alternate image so it can be seen.
        alternateImage.enabled = true;

        // Just to make sure, we want the image we're switching to inherit the color properties of the current image.
        alternateImage.color = trueColor;

        // Most importantly, set the alternate image to have the new sprite.
        alternateImage.sprite = newSprite;

        while(timePassed <= animationTime)
        {
            float progress = 0.0f;

            if(animationTime <= 0.0f)
            {
                progress = 1.0f;
            }
            else
            {
                progress = timePassed / animationTime;
            }

            //defaultImage.color = new Color(oldColor.r, oldColor.g, oldColor.b, Mathf.Lerp(1.0f, 0.0f, progress));
            defaultImage.color = new Color(trueColor.r, trueColor.g, trueColor.b, Mathf.Lerp(trueColor.a, 0.0f, progress));

            timePassed += Time.deltaTime;

            // If there's still time in the animation, advance to the next frame. Otherwise, we should allow the coroutine to end.
            if(timePassed < animationTime)
            {
                yield return null;
            }
        }

        // In case there are any issues with timing, make sure the desired result is reached.
        defaultImage.color = new Color(trueColor.r, trueColor.g, trueColor.b, 0.0f);

        // Go ahead and hide the default image to reduce performance issues with transparencies.
        defaultImage.enabled = false;

        // Switch the images so the alternative image is now the default, and is now rendered in front of the old image.
        Image temp = defaultImage;
        defaultImage = alternateImage;
        alternateImage = temp;
        defaultImage.gameObject.transform.SetAsLastSibling();

        imageChangeCoroutine = null;
        Debug.LogFormat("Cutscene Background: Completed image change animation.");
    }

    public void StartImageChangeAnimation(string name, float animationTime = 0.0f)
    {
        imageChangeCoroutine = StartCoroutine(ChangeImage(name, animationTime));
    }

    public void StopImageChangeAnimation()
    {
        StopCoroutine(imageChangeCoroutine);
        imageChangeCoroutine = null;
    }

    // Change the background image's color to the one given.
    //[YarnCommand("set_background_color")]
    public void SetColor(float r, float g, float b, float a = 1.0f)
    {
        // TODO: If the color is changed while an image or color change animation is occurring it could cause problems.
        // Or it could be fine. Keep this method in mind if there are any bugs.
        Color color = new Color(r, g, b, a);
        trueColor = color;

        // If there is no animation currently ongoing, change the image's color as well. Otherwise, this is probably being
        // handled by the animation coroutine.
        if(colorChangeCoroutine == null)
        {
            defaultImage.color = color;
        }
    }

    //[YarnCommand("set_background_alpha")]
    public void SetAlpha(float a)
    {
        trueColor.a = a;

        if(colorChangeCoroutine == null)
        {
            Color color = new Color(trueColor.r, trueColor.g, trueColor.b, a);
            defaultImage.color = color;
        }
    }

    //[YarnCommand("change_background_alpha")]
    public IEnumerator ChangeAlpha_Handler(float a, float animationTime = 0.0f, bool waitForAnimation = false)
    {
        if(colorChangeCoroutine != null)
        {
            StopCoroutine(colorChangeCoroutine);
        }

        colorChangeCoroutine = StartCoroutine(ChangeColor(new Color(trueColor.r, trueColor.g, trueColor.b, a), animationTime));

        if(waitForAnimation)
        {
            yield return new WaitUntil(() => colorChangeCoroutine == null);
        }
    }

    //[YarnCommand("change_background_color")]
    public IEnumerator ChangeColor_Handler(float r, float g, float b, float a = 1.0f, float animationTime = 0.0f, bool waitForAnimation = false)
    {
        if(colorChangeCoroutine != null)
        {
            StopCoroutine(colorChangeCoroutine);
        }

        colorChangeCoroutine = StartCoroutine(ChangeColor(new Color(r, g, b, a), animationTime));

        if(waitForAnimation)
        {
            yield return new WaitUntil(() => colorChangeCoroutine == null);
        }
    }

    // The above handler coroutines will share the same background animation coroutine, because essentially changing color and alpha
    // is the exact same thing. We have separate handlers because we need separate Yarn commands that handle variations of the same functionality.
    public IEnumerator ChangeColor(Color newColor, float animationTime = 0.0f)
    {
        Debug.LogFormat("Cutscene Background: Changing background color to {0} over {1} seconds.", newColor, animationTime);

        float timePassed = 0.0f;
        Color oldColor = defaultImage.color;
        trueColor = newColor;

        while(timePassed <= animationTime)
        {
            float progress = 0.0f;

            // Make sure not to divide by 0.
            if(animationTime <= 0.0f)
            {
                progress = 1.0f;
            }
            else
            {
                progress = timePassed / animationTime;
            }

            defaultImage.color = Color.Lerp(oldColor, newColor, progress);
            alternateImage.color = Color.Lerp(oldColor, newColor, progress);

            timePassed += Time.deltaTime;

            if(timePassed < animationTime)
            {
                yield return null;
            }
        }

        // After the animation completes make sure we arrive at the desired color, in case the timing wasn't exact.
        defaultImage.color = newColor;
        alternateImage.color = newColor;

        colorChangeCoroutine = null;
        Debug.LogFormat("Cutscene Background: Color change animation completed.");
    }

    public void StartColorChangeAnimation(Color newColor, float animationTime = 0.0f)
    {
        colorChangeCoroutine = StartCoroutine(ChangeColor(newColor, animationTime));
    }

    public void StopColorChangeAnimation()
    {
        StopCoroutine(colorChangeCoroutine);
        colorChangeCoroutine = null;
    }

    public void HideBackground()
    {
        defaultImage.enabled = false;
        alternateImage.enabled = false;
    }

    //[YarnCommand("show_background")]
    public override void HideObject()
    {
        defaultImage.enabled = false;
        alternateImage.enabled = false;
    }

    public void ShowBackground()
    {
        defaultImage.enabled = true;
        alternateImage.enabled = true;
    }

    //[YarnCommand("hide_background")]
    public override void ShowObject()
    {
        defaultImage.enabled = true;
        alternateImage.enabled = true;
    }
}
