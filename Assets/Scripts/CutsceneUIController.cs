using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CutsceneUIController : MonoBehaviour
{
    public GameObject pauseScreenOverlay;

    public ExitDialogueUIController exitDialogue;

    public bool isGamePaused = false;

    // When pausing the cutscene, the timescale is temporarily set to 0. When unpausing the game,
    // the original timescale needs to be restored. If a custom timescale was being used, that
    // value needs to be saved here or else it will be lost upon resuming the cutscene.
    private float previousTimescale = 1.0f;

    public Coroutine pauseAnimation;

    public float pauseAnimationSpeed = 20.0f;

    // Start is called before the first frame update
    void Start()
    {
        SubscribeToEvents();
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
            canvasGroup.alpha = 1.0f;

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
            canvasGroup.alpha = 0.0f;

            while(canvasGroup.alpha < 1.0f)
            {
                canvasGroup.alpha += pauseAnimationSpeed * Time.unscaledDeltaTime;

                yield return null;
            }
        }

        pauseAnimation = null;
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
        exitDialogue.ShowDialogue();
    }

    public void OnExitDialogueNoClicked()
    {
        exitDialogue.HideDialogue();
    }
}
