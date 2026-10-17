using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InspectNote : MonoBehaviour
{
    [Header("Lightswitch that toggles light")]
    private bool showNote;
    private bool canInteract = true;
    [Tooltip("Page AudioSource")]
    public AudioSource Source;
    [Tooltip("0 paper shuffle in, 1 paper shuffle out")]
    public AudioClip[] clips;
    [Tooltip("Page Renderer")]
    public Renderer pageRend;
    private Color originColor;
    private bool over = false;
    [Tooltip("Color when hovered over (looked at)")]
    public Color targetColor = Color.yellow;
    private GameObject MainCam;
    private Interact InteractionScript;
    [Tooltip("Hover prompt - 0 Read Page, 1 n/a")]
    public string[] prompts = { "Read Page", "" };
    [Tooltip("The Canvas UI HUD image for Note, ideally centered and large")]
    public Image noteUI;
    [Tooltip("The Page sprite")]
    public Sprite targetImage;
    private PlayerMovement playerScript;

    // Start is called before the first frame update
    void Start()
    {
        MainCam = GameObject.FindWithTag("MainCamera");
        if (MainCam == null)
        {
            MainCam = GameObject.FindFirstObjectByType<Camera>().gameObject;
        }
        InteractionScript = MainCam.GetComponent<Interact>();
        originColor = pageRend.material.color;
        playerScript = FindFirstObjectByType<PlayerMovement>();
        noteUI.transform.parent.gameObject.SetActive(false);
    }

    public void Hovering()
    {
        over = true;
        StartCoroutine(Fadeout());
        if (!showNote)
            InteractionScript.message = prompts[0];
        else if (showNote)
            InteractionScript.message = prompts[1];
    }

    public void Interacting()
    {
        if (canInteract)
        {
            showNote = !showNote;
            if (Source.isPlaying)
            {
                Source.Stop();
            }
            Source.pitch = Random.Range(.9f, 1.3f);
            if (!showNote)
            {
                Source.clip = clips[0];
                InteractionScript.message = prompts[1];
                StartCoroutine(SetNote());
            }
            else if (showNote)
            {
                Source.clip = clips[1];
                InteractionScript.message = prompts[0];
                StartCoroutine(SetNote());
            }
            Source.Play();
            canInteract = false;
        }
    }

    void FixedUpdate()
    {
        if (over)
        {
            pageRend.material.color = Color.Lerp(pageRend.material.color, targetColor, Time.deltaTime * 4);
        }
        else if (!over)
        {
            pageRend.material.color = Color.Lerp(pageRend.material.color, originColor, Time.deltaTime * 2);
        }
    }

    private IEnumerator Fadeout()
    {
        yield return new WaitForSeconds(1);
        over = false;
    }

    private IEnumerator SetNote()
    {
        if (showNote)
        {
            noteUI.sprite = targetImage;
        }
        else
        {
            noteUI.sprite = null;
        }
        noteUI.transform.parent.gameObject.SetActive(showNote);
        //disable player movement when inspecting note
        MouseLook[] foundLookScripts = FindObjectsByType<MouseLook>(FindObjectsSortMode.None);
        if (foundLookScripts.Length >= 2 && playerScript != null)
        {
            foreach(MouseLook lookScript in foundLookScripts)
            {
                lookScript.working = !showNote;
            }
            playerScript.SetWorking(!showNote);
        }
        if(Source.clip != null)
        {
            yield return new WaitForSeconds(Source.clip.length);
        }
        else
        {
            yield return new WaitForSeconds(.35f);
        }
        canInteract = true;
    }
}
