using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Prompt : MonoBehaviour
{
    private GameObject MainCam;
    private Interact InteractionScript;
    [Tooltip("Hover prompt")]
    public string prompt = "Prompt";

    void Start()
    {
        MainCam = GameObject.FindWithTag("MainCamera");
        if (MainCam == null)
        {
            MainCam = GameObject.FindFirstObjectByType<Camera>().gameObject;
        }
        InteractionScript = MainCam.GetComponent<Interact>();
    }

    public void Hovering()
    {
        InteractionScript.message = prompt;
    }
}
