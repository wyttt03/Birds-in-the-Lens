using UnityEngine;
using UnityEngine.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class BirdBookUI : MonoBehaviour
{
    [Header("Book UI")]
    [SerializeField] private Button bookButton;
    [SerializeField] private Image buttonIcon;
    [SerializeField] private Text buttonLabel;
    [SerializeField] private GameObject bookPages;

    [Header("Audio")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip openClip;
    [SerializeField] private AudioClip closeClip;

    [Header("Button Sprites")]
    [SerializeField] private Sprite closedBookSprite;
    [SerializeField] private Sprite openBookSprite;

    [Header("Gameplay Controls")]
    [Tooltip("Assign gameplay input scripts that must stop while the book is open.")]
    [SerializeField] private MonoBehaviour[] gameplayControls;

    private bool isOpen;
    private bool initialized;
    private bool[] previousControlStates;

    private CursorLockMode previousCursorLockState;
    private bool previousCursorVisibility;

    public bool IsOpen => isOpen;

    private void Awake()
    {
        if (bookButton == null || bookPages == null)
        {
            Debug.LogError(
                "Assign the book button and book pages.",
                this
            );

            enabled = false;
            return;
        }

        previousControlStates = new bool[
            gameplayControls != null ? gameplayControls.Length : 0
        ];

        isOpen = false;
        bookPages.SetActive(false);
        UpdateButtonAppearance();

        bookButton.onClick.AddListener(ToggleBook);
        initialized = true;
    }

    private void Update()
    {
        if (initialized && WasBookKeyPressed())
        {
            ToggleBook();
        }
    }

    public void ToggleBook()
    {
        if (!initialized || !isActiveAndEnabled)
        {
            return;
        }

        if (isOpen)
        {
            CloseBook();
            PlaySfx(closeClip);
        }
        else
        {
            OpenBook();
            PlaySfx(openClip);
        }
    }

    private void PlaySfx(AudioClip clip)
    {
        if (sfxSource != null && clip != null)
        {
            sfxSource.PlayOneShot(clip);
        }
    }

    private void OpenBook()
    {
        if (isOpen)
        {
            return;
        }

        // Save cursor state before disabling gameplay scripts.
        previousCursorLockState = Cursor.lockState;
        previousCursorVisibility = Cursor.visible;

        isOpen = true;

        for (int i = 0; i < previousControlStates.Length; i++)
        {
            MonoBehaviour control = gameplayControls[i];

            if (control == null || control == this)
            {
                continue;
            }

            previousControlStates[i] = control.enabled;
            control.enabled = false;
        }

        bookPages.SetActive(true);
        UpdateButtonAppearance();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void CloseBook()
    {
        if (!isOpen)
        {
            return;
        }

        isOpen = false;

        if (bookPages != null)
        {
            bookPages.SetActive(false);
        }

        for (int i = 0; i < previousControlStates.Length; i++)
        {
            MonoBehaviour control = gameplayControls[i];

            if (control == null || control == this)
            {
                continue;
            }

            control.enabled = previousControlStates[i];
        }

        UpdateButtonAppearance();

        Cursor.lockState = previousCursorLockState;
        Cursor.visible = previousCursorVisibility;
    }

    private void UpdateButtonAppearance()
    {
        Sprite currentSprite = isOpen
            ? openBookSprite
            : closedBookSprite;

        if (buttonIcon != null)
        {
            buttonIcon.sprite = currentSprite;
            buttonIcon.enabled = currentSprite != null;
        }

        if (buttonLabel != null)
        {
            buttonLabel.text = isOpen
                ? "Close Book [B]"
                : "Open Book [B]";
        }
    }

    private bool WasBookKeyPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null &&
               Keyboard.current.bKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.B);
#endif
    }

    private void OnDisable()
    {
        if (initialized)
        {
            CloseBook();
        }
    }

    private void OnDestroy()
    {
        if (initialized && bookButton != null)
        {
            bookButton.onClick.RemoveListener(ToggleBook);
        }
    }
}