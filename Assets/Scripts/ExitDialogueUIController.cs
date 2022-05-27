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
        yield break;
    }

    public IEnumerator HideAnimationCoroutine()
    {
        yield break;
    }

    public void OnYesButtonClicked()
    {
        if(OnExitDialogueYesClicked != null)
        {
            OnExitDialogueYesClicked();
        }
    }

    public void OnNoButtonClicked()
    {
        if(OnExitDialogueNoClicked != null)
        {
            OnExitDialogueNoClicked();
        }
    }
}
