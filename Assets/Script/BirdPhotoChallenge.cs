using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class BirdPhotoChallenge : MonoBehaviour
{
    [Serializable]
    public class BirdEntry
    {
        public string birdName;
        public BirdPhotoTarget target;
        public Image bookImage;
        public Sprite colorSprite;
        public Sprite graySprite;

        [NonSerialized] public bool photographed;
    }

    [Header("References")]
    public BirdWatchingCamera cameraController;
    public BirdBookUI bookUI;
    public Text progressLabel;
    public Text feedbackLabel;

    [Header("Level Goal")]
    [Min(1)]
    public int requiredTargetCount = 3;

    public BirdEntry[] birds = new BirdEntry[3];

    [Header("Photo Frame")]
    [Range(0f, 0.2f)]
    public float frameMargin = 0.02f;

    [Range(0.01f, 1f)]
    public float minimumScreenHeight = 0.12f;

    [Header("Focus Rules")]
    [Min(0.001f)]
    public float relativeFocusTolerance = 0.08f;

    [Min(0.001f)]
    public float minimumFocusTolerance = 0.15f;

    [Header("Occlusion")]
    public LayerMask obstacleLayers;

    [Header("Photo Timing")]
    [Min(0f)]
    public float photoCooldown = 0.3f;

    [Min(0f)]
    public float feedbackDuration = 2f;

    [Header("Scene Transition")]
    public bool loadNextSceneOnCompletion;

    public string nextSceneName = "Level1";

    [Min(0f)]
    public float transitionDelay = 2f;

    private readonly Vector3[] worldCorners = new Vector3[4];

    private int photographedCount;
    private float nextPhotoTime;
    private float feedbackEndTime;
    private bool initialized;

    public int PhotographedCount => photographedCount;

    public bool IsComplete =>
        initialized &&
        photographedCount >= requiredTargetCount;

    private void Start()
    {
        if (!ValidateConfiguration())
        {
            enabled = false;
            return;
        }

        photographedCount = 0;

        foreach (BirdEntry entry in birds)
        {
            entry.photographed = false;
            entry.bookImage.sprite = entry.colorSprite;
        }

        initialized = true;

        UpdateProgress();

        if (feedbackLabel != null)
        {
            feedbackLabel.text = "";
        }
    }

    private bool ValidateConfiguration()
    {
        if (cameraController == null ||
            cameraController.ViewCamera == null ||
            bookUI == null ||
            progressLabel == null)
        {
            Debug.LogError(
                "Assign the camera controller, book UI, and progress label.",
                this
            );

            return false;
        }

        if (cameraController.ViewCamera.orthographic)
        {
            Debug.LogError(
                "The photo challenge requires a perspective camera.",
                this
            );

            return false;
        }

        if (birds == null ||
            requiredTargetCount < 1 ||
            birds.Length < requiredTargetCount)
        {
            Debug.LogError(
                "Configure enough bird entries for the required target count.",
                this
            );

            return false;
        }

        for (int i = 0; i < birds.Length; i++)
        {
            BirdEntry entry = birds[i];

            if (entry == null ||
                entry.target == null ||
                entry.bookImage == null ||
                entry.colorSprite == null ||
                entry.graySprite == null)
            {
                Debug.LogError(
                    $"Bird entry {i} is missing a required reference.",
                    this
                );

                return false;
            }

            for (int j = 0; j < i; j++)
            {
                if (birds[j].target == entry.target ||
                    birds[j].bookImage == entry.bookImage)
                {
                    Debug.LogError(
                        "Each bird entry requires a unique target and book image.",
                        this
                    );

                    return false;
                }
            }
        }

        if (loadNextSceneOnCompletion &&
            (string.IsNullOrWhiteSpace(nextSceneName) ||
             !Application.CanStreamedLevelBeLoaded(nextSceneName)))
        {
            Debug.LogError(
                "Add the next scene to the active build profile scene list.",
                this
            );

            return false;
        }

        return true;
    }

    private void Update()
    {
        if (!initialized)
        {
            return;
        }

        if (feedbackLabel != null &&
            Time.unscaledTime >= feedbackEndTime)
        {
            feedbackLabel.text = "";
        }

        if (IsComplete ||
            bookUI.IsOpen ||
            !cameraController.isActiveAndEnabled ||
            Cursor.lockState != CursorLockMode.Locked)
        {
            return;
        }

        if (WasPhotoKeyPressed() &&
            Time.unscaledTime >= nextPhotoTime)
        {
            nextPhotoTime = Time.unscaledTime + photoCooldown;
            TakePhoto();
        }
    }

    private void TakePhoto()
    {
        int newBirdCount = 0;

        foreach (BirdEntry entry in birds)
        {
            if (photographedCount >= requiredTargetCount)
            {
                break;
            }

            if (entry.photographed ||
                !MeetsPhotoRequirements(entry.target))
            {
                continue;
            }

            entry.photographed = true;
            entry.bookImage.sprite = entry.graySprite;

            photographedCount++;
            newBirdCount++;

            Debug.Log(
                $"Bird photographed: {entry.birdName}",
                this
            );
        }

        UpdateProgress();

        if (IsComplete)
        {
            ShowFeedback("All required birds photographed!");

            if (loadNextSceneOnCompletion)
            {
                StartCoroutine(LoadNextScene());
            }
        }
        else if (newBirdCount > 0)
        {
            ShowFeedback($"New birds photographed: {newBirdCount}");
        }
        else
        {
            ShowFeedback(
                "Photo failed. Check framing, size, focus, and visibility."
            );
        }
    }

    private bool MeetsPhotoRequirements(BirdPhotoTarget target)
    {
        if (target == null ||
            !target.TryGetWorldCorners(worldCorners))
        {
            return false;
        }

        Camera camera = cameraController.ViewCamera;

        if ((camera.cullingMask & (1 << target.VisualLayer)) == 0)
        {
            return false;
        }

        float minY = float.PositiveInfinity;
        float maxY = float.NegativeInfinity;

        float magnification = Mathf.Max(
            1f,
            cameraController.Magnification
        );

        for (int i = 0; i < worldCorners.Length; i++)
        {
            Vector3 viewportPoint =
                camera.WorldToViewportPoint(worldCorners[i]);

            if (viewportPoint.z <= camera.nearClipPlane ||
                viewportPoint.z >= camera.farClipPlane)
            {
                return false;
            }

            if (viewportPoint.x < frameMargin ||
                viewportPoint.x > 1f - frameMargin ||
                viewportPoint.y < frameMargin ||
                viewportPoint.y > 1f - frameMargin)
            {
                return false;
            }

            minY = Mathf.Min(minY, viewportPoint.y);
            maxY = Mathf.Max(maxY, viewportPoint.y);

            float tolerance = Mathf.Max(
                minimumFocusTolerance,
                viewportPoint.z *
                relativeFocusTolerance /
                magnification
            );

            float focusError = Mathf.Abs(
                viewportPoint.z - cameraController.FocusDistance
            );

            if (focusError > tolerance)
            {
                return false;
            }
        }

        if (maxY - minY < minimumScreenHeight)
        {
            return false;
        }

        Vector3 center =
            (worldCorners[0] + worldCorners[2]) * 0.5f;

        if (IsBlocked(target, center))
        {
            return false;
        }

        for (int i = 0; i < worldCorners.Length; i++)
        {
            Vector3 samplePoint = Vector3.Lerp(
                center,
                worldCorners[i],
                0.85f
            );

            if (IsBlocked(target, samplePoint))
            {
                return false;
            }
        }

        return true;
    }

    private bool IsBlocked(BirdPhotoTarget target, Vector3 point)
    {
        Vector3 origin =
            cameraController.ViewCamera.transform.position;

        Vector3 direction = point - origin;
        float distance = direction.magnitude;

        if (distance <= 0.001f)
        {
            return false;
        }

        RaycastHit[] hits = Physics.RaycastAll(
            origin,
            direction / distance,
            distance,
            obstacleLayers,
            QueryTriggerInteraction.Ignore
        );

        foreach (RaycastHit hit in hits)
        {
            if (!target.OwnsTransform(hit.transform))
            {
                return true;
            }
        }

        return false;
    }

    private void UpdateProgress()
    {
        progressLabel.text =
            $"({photographedCount}/{requiredTargetCount})";
    }

    private void ShowFeedback(string message)
    {
        if (feedbackLabel == null)
        {
            return;
        }

        feedbackLabel.text = message;
        feedbackEndTime =
            Time.unscaledTime + feedbackDuration;
    }

    private IEnumerator LoadNextScene()
    {
        yield return new WaitForSecondsRealtime(transitionDelay);

        AsyncOperation operation =
            SceneManager.LoadSceneAsync(nextSceneName);

        if (operation == null)
        {
            Debug.LogError("The next scene could not be loaded.", this);
            yield break;
        }

        while (!operation.isDone)
        {
            yield return null;
        }
    }

    private bool WasPhotoKeyPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null &&
               Keyboard.current.spaceKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Space);
#endif
    }
}