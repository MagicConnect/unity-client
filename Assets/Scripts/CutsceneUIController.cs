using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Yarn.Unity;

public class CutsceneUIController : MonoBehaviour
{
    public GameObject pauseScreenOverlay;

    public ExitDialogueUIController exitDialogue;

    public GameObject skipCutsceneDialogue;

    public CutsceneManager cutsceneManager;

    public CustomLineView lineView;

    // The little icon for the auto-advance toggle button.
    public Image autoAdvanceArrow;

    public bool isGamePaused = false;

    public bool isSkipDialogueActive = false;

    // When pausing the cutscene, the timescale is temporarily set to 0. When unpausing the game,
    // the original timescale needs to be restored. If a custom timescale was being used, that
    // value needs to be saved here or else it will be lost upon resuming the cutscene.
    private float previousTimescale = 1.0f;

    public Coroutine pauseAnimation;

    public Coroutine skipDialogueAnimation;

    public float pauseAnimationSpeed = 20.0f;

    public float skipAnimationSpeed = 20.0f;

    public float presetVerySlow = 10.0f;
    public float presetSlow = 20.0f;
    public float presetMedium = 30.0f;
    public float presetFast = 40.0f;
    public float presetVeryFast = 50.0f;

    // Start is called before the first frame update
    void Start()
    {
        SubscribeToEvents();
        UpdateAutoAdvanceButton();
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Escape))
        {
            if(isGamePaused)
            {
                UnpauseCutscene();
            }
            else
            {
                PauseCutscene();
            }
        }
    }

    void OnDestroy()
    {
        UnsubFromEvents();
    }

    private void SubscribeToEvents()
    {
        exitDialogue.OnExitDialogueNoClicked += OnExitDialogueNoClicked;
    }

    private void UnsubFromEvents()
    {
        exitDialogue.OnExitDialogueNoClicked -= OnExitDialogueNoClicked;
    }

    public void PauseCutscene()
    {
        previousTimescale = Time.timeScale;
        Time.timeScale = 0.0f;
        isGamePaused = true;

        if(pauseAnimation != null)
        {
            StopCoroutine(pauseAnimation);
        }

        pauseAnimation = StartCoroutine(PauseAnimationCoroutine());
    }

    public void UnpauseCutscene()
    {
        Time.timeScale = previousTimescale;
        isGamePaused = false;

        if(pauseAnimation != null)
        {
            StopCoroutine(pauseAnimation);
        }

        pauseAnimation = StartCoroutine(PauseAnimationCoroutine());
    }

    public IEnumerator PauseAnimationCoroutine()
    {
        CanvasGroup canvasGroup = pauseScreenOverlay.GetComponent<CanvasGroup>();

        if(!isGamePaused)
        {
            canvasGroup.blocksRaycasts = false;

            while(canvasGroup.alpha > 0.0f)
            {
                canvasGroup.alpha -= pauseAnimationSpeed * Time.unscaledDeltaTime;

                yield return null;
            }

            pauseScreenOverlay.SetActive(false);
        }
        else
        {
            pauseScreenOverlay.SetActive(true);
            canvasGroup.blocksRaycasts = true;

            while(canvasGroup.alpha < 1.0f)
            {
                canvasGroup.alpha += pauseAnimationSpeed * Time.unscaledDeltaTime;

                yield return null;
            }
        }

        pauseAnimation = null;
    }

    public void ShowSkipDialogue()
    {
        isSkipDialogueActive = true;

        if(skipDialogueAnimation != null)
        {
            StopCoroutine(skipDialogueAnimation);
        }

        skipDialogueAnimation = StartCoroutine(SkipAnimationCoroutine());
    }

    public void HideSkipDialogue()
    {
        isSkipDialogueActive = false;

        if(skipDialogueAnimation != null)
        {
            StopCoroutine(skipDialogueAnimation);
        }

        skipDialogueAnimation = StartCoroutine(SkipAnimationCoroutine());
    }

    public IEnumerator SkipAnimationCoroutine()
    {
        CanvasGroup canvasGroup = skipCutsceneDialogue.GetComponent<CanvasGroup>();

        if(!isSkipDialogueActive)
        {
            canvasGroup.blocksRaycasts = false;

            while(canvasGroup.alpha > 0.0f)
            {
                canvasGroup.alpha -= skipAnimationSpeed * Time.unscaledDeltaTime;

                yield return null;
            }

            skipCutsceneDialogue.SetActive(false);
        }
        else
        {
            skipCutsceneDialogue.SetActive(true);
            canvasGroup.blocksRaycasts = true;

            while(canvasGroup.alpha < 1.0f)
            {
                canvasGroup.alpha += skipAnimationSpeed * Time.unscaledDeltaTime;

                yield return null;
            }
        }

        skipDialogueAnimation = null;
    }

    public void OnPauseButtonClicked()
    {
        if(isGamePaused)
        {
            UnpauseCutscene();
        }
        else
        {
            PauseCutscene();
        }
    }

    public void OnExitButtonClicked()
    {
        Debug.LogFormat("CutsceneUIController: OnExitButtonClicked().");
        exitDialogue.ShowDialogue();
    }

    public void OnExitDialogueNoClicked()
    {
        exitDialogue.HideDialogue();
    }

    public void OnSkipCutsceneButtonClicked()
    {
        ShowSkipDialogue();
    }

    public void OnSkipCutsceneYesClicked()
    {
        HideSkipDialogue();
        cutsceneManager.EndCutscene();
    }

    public void OnSkipCutsceneNoClicked()
    {
        HideSkipDialogue();
    }

    public void OnAutoAdvanceButtonClicked()
    {
        lineView.autoAdvance = !lineView.autoAdvance;
        UpdateAutoAdvanceButton();
    }

    // Method for other objects to change the text auto-advance setting. Refreshes the auto-advance button after the change.
    public void SetAutoAdvanceEnabled(bool value)
    {
        lineView.autoAdvance = value;
        UpdateAutoAdvanceButton();
    }

    // Change the auto-advance button icon to represent the current setting.
    public void UpdateAutoAdvanceButton()
    {
        if(lineView.autoAdvance)
        {
            autoAdvanceArrow.color = Color.white;
        }
        else
        {
            autoAdvanceArrow.color = Color.gray;
        }
    }

    public void SetAutoHoldTime(float seconds)
    {
        // Make sure the hold time isn't set to a negative number. That wouldn't make any sense.
        if(seconds >= 0.0f)
        {
            lineView.holdTime = seconds;
        }
        else
        {
            lineView.holdTime = 0.0f;
        }
    }

    public void SetTypewriterEffectEnabled(bool value)
    {
        lineView.useTypewriterEffect = value;
    }

    public void SetTextSpeed(float charactersPerSecond)
    {
        // Make sure the characters per second isn't 0 or a negative number.
        if(charactersPerSecond >= 1.0f)
        {
            lineView.typewriterEffectSpeed = charactersPerSecond;
        }
        else
        {
            lineView.typewriterEffectSpeed = 1.0f;
        }
    }

    public void SetTextSpeed(string preset)
    {
        switch(preset)
        {
            case "very_slow": 
                lineView.typewriterEffectSpeed = presetVerySlow;
                break;
            case "slow": 
                lineView.typewriterEffectSpeed = presetSlow;
                break;
            case "medium": 
                lineView.typewriterEffectSpeed = presetMedium;
                break;
            case "fast": 
                lineView.typewriterEffectSpeed = presetFast;
                break;
            case "very_fast": 
                lineView.typewriterEffectSpeed = presetVeryFast;
                break;
            default:
                Debug.LogErrorFormat("Cutscene UI Controller: No typewriter speed preset for '{0}' exists.", preset); 
                break;
        }
    }
}
