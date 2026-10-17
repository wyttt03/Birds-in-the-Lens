using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class NormalButton : MonoBehaviour
{
    private GameObject MainCam;
    private Interact InteractionScript;

    private bool canInteract = true;
    private bool over = false;
    public string prompt = "Press Button";

    public AudioClip clip;
    public AudioSource Source;

    public UnityEvent OnPress;

    public float PressDelay = 0.5f;

    public Renderer rend;
    public Color targetColor = Color.yellow;
    private Color originColor;

    public Animation pressAnim;

    void Start()
    {
        MainCam = GameObject.FindWithTag("MainCamera");
        if (MainCam == null)
        {
            MainCam = GameObject.FindFirstObjectByType<Camera>().gameObject;
        }
        InteractionScript = MainCam.GetComponent<Interact>();
        originColor = rend.material.color;
    }

    public void Hovering()
    {
        over = true;
        StartCoroutine(Fadeout());
        InteractionScript.message = prompt;
    }

    public void Interacting()
    {
        if (canInteract)
        {
            InteractionScript.message = "";
            StartCoroutine(PressButton());

            Source.clip = clip;
            Source.Play();

            canInteract = false;
        }
    }

    void FixedUpdate()
    {
        if (over)
        {
            rend.material.color = Color.Lerp(rend.material.color, targetColor, Time.deltaTime * 4);
        }
        else if (!over)
        {
            rend.material.color = Color.Lerp(rend.material.color, originColor, Time.deltaTime * 2);
        }
    }

    private IEnumerator PressButton()
    {
        OnPress.Invoke();
        pressAnim.Play("An_ButtonPress");

        yield return new WaitForSeconds(PressDelay);
        canInteract = true;
    }

    private IEnumerator Fadeout()
    {
        yield return new WaitForSeconds(1);
        over = false;
    }
}
