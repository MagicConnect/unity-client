using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.UI;

public class LoadingScreenManager : MonoBehaviour
{
    public GameObject image;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void TestDisplayImage()
    {
        Texture2D displayTexture = WebAssetCache.Instance.GetTexture2D("assets/art/accessories/Magic_Emblem.webp");

        if(displayTexture != null)
        {
            Sprite sprite = Sprite.Create(displayTexture, new Rect(0.0f, 0.0f, displayTexture.width, displayTexture.height), new Vector2(0.0f, 0.0f), 100.0f);
            image.GetComponent<Image>().sprite = sprite;
        }
    }
}
