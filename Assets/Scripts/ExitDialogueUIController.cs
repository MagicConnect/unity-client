using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

public class ExitDialogueUIController : MonoBehaviour
{
    public event Action OnExitDialogueYesClicked;
    public event Action OnExitDialogueNoClicked;

    public Coroutine exitAnimation;

    public CanvasGroup canvasGroup;

    public float animationSpeed = 20.0f;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ShowDialogue()
    {
        gameObject.SetActive(true);

        if(exitAnimation != null)
        {
            StopCoroutine(exitAnimation);
        }

        exitAnimation = StartCoroutine(ShowAnimationCoroutine());
    }

    public void HideDialogue()
    {
        if(exitAnimation != null)
        {
            StopCoroutine(exitAnimation);
        }

        exitAnimation = StartCoroutine(HideAnimationCoroutine());
    }

    public IEnumerator ShowAnimationCoroutine()
    {
        canvasGroup.blocksRaycasts = true;

        while (canvasGroup.alpha < 1.0f)
        {
            canvasGroup.alpha += animationSpeed * Time.unscaledDeltaTime;

            yield return null;
        }

        exitAnimation = null;
    }

    public IEnumerator HideAnimationCoroutine()
    {
        canvasGroup.blocksRaycasts = false;

        while (canvasGroup.alpha > 0.0f)
        {
            canvasGroup.alpha -= animationSpeed * Time.unscaledDeltaTime;

            yield return null;
        }

        gameObject.SetActive(false);
        exitAnimation = null;
    }

    public void OnYesButtonClicked()
    {
        if(OnExitDialogueYesClicked != null)
        {
            OnExitDialogueYesClicked();
        }

        Application.Quit();
    }

    public void OnNoButtonClicked()
    {
        if(OnExitDialogueNoClicked != null)
        {
            OnExitDialogueNoClicked();
        }
    }
}
