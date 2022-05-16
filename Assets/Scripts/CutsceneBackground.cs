using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Yarn.Unity;

public class CutsceneBackground : MonoBehaviour
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

    void Awake()
    {
        // Set the image component and configure it for proper display.
        image = GetComponent<Image>();
        image.preserveAspect = true;

        // The cache should be loaded before the game ever gets here but it doesn't hurt to check.
        if(WebAssetCache.Instance.status == WebAssetCache.WebCacheStatus.ReadyToUse)
        {
            List<WebAssetCache.LoadedImageAsset> assets = WebAssetCache.Instance.GetLoadedAssetsByCategory("backgrounds");

            if(assets.Count <= 0)
            {
                Debug.LogWarningFormat("No loaded assets returned by cache.");
            }

            foreach(WebAssetCache.LoadedImageAsset asset in assets)
            {
                Sprite sprite = Sprite.Create(asset.texture, new Rect(0.0f, 0.0f, asset.texture.width, asset.texture.height), new Vector2(0.0f, 0.0f), 100.0f);
                backgroundSprites.Add(asset.name, sprite);

                Debug.LogFormat("Loaded background sprite '{0}'.", asset.name);
            }
        }
        else
        {
            Debug.LogErrorFormat("Attempted to load conversation background when cache was not fully loaded.");
        }
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
    public void ChangeBackgroundImage(string name)
    {
        if(backgroundSprites.ContainsKey(name))
        {
            image.sprite = backgroundSprites[name];
        }
        else
        {
            Debug.LogErrorFormat("Cutscene Background: Background sprite named '{0}' does not exist.", name);
        }
    }

    public IEnumerator ChangeBackgroundImage(string name, float animationTime = 0.0f)
    {
        if(!backgroundSprites.ContainsKey(name))
        {
            Debug.LogFormat("Cutscene Background: Cannot change image to '{0}' because no image by that name exists.", name);
            imageChangeCoroutine = null;
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
        alternateImage.sprite = backgroundSprites[name];

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
        imageChangeCoroutine = StartCoroutine(ChangeBackgroundImage(name, animationTime));
    }

    public void StopImageChangeAnimation()
    {
        StopCoroutine(imageChangeCoroutine);
        imageChangeCoroutine = null;
    }

    // Change the background image's color to the one given.
    public void ChangeBackgroundColor(Color color)
    {
        image.color = color;
    }

    // The above handler coroutines will share the same background animation coroutine, because essentially changing color and alpha
    // is the exact same thing. We have separate handlers because we need separate Yarn commands that handle variations of the same functionality.
    public IEnumerator ChangeBackgroundColor(Color newColor, float animationTime = 0.0f)
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
        colorChangeCoroutine = StartCoroutine(ChangeBackgroundColor(newColor, animationTime));
    }

    public void StopColorChangeAnimation()
    {
        StopCoroutine(colorChangeCoroutine);
        colorChangeCoroutine = null;
    }
}
