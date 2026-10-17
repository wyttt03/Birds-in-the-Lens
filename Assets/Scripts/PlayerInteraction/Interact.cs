using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Interact : MonoBehaviour
{

    private Vector3 fwd;
    [HideInInspector]
    public bool hover = false;
    private bool alreadyHovered = false;
    private bool alreadyHovered2 = false;
    [Header("General Interaction Variables")]
    public GameObject InteractionUI;
    public GameObject CrosshairUI;
    private Animation anim;
    private TextMeshProUGUI dispText;
    private float dist = 1000;
    //[System.NonSerialized]
    public string message = "";

    [System.NonSerialized]
    public GameObject currentObj = null;
    private GameObject storedIntObj;

    void Start()
    {
        anim = InteractionUI.GetComponent<Animation>();
        dispText = InteractionUI.GetComponent<TextMeshProUGUI>();
        dispText.text = "";
    }

    // Update is called once per frame
    void Update()
    {
        fwd = transform.TransformDirection(Vector3.forward);
        RaycastHit hit;
        if (Physics.Raycast(transform.position, fwd, out hit, 300))
        {
            currentObj = hit.collider.gameObject;
            if (currentObj.tag == "Interactable")
            {
                storedIntObj = currentObj;
                dist = Vector3.Distance(hit.transform.position, this.transform.position);
                if (dist < 3)
                {
                    storedIntObj.transform.SendMessage("Hovering", hit.point, SendMessageOptions.DontRequireReceiver);
                    dispText.text = message;
                    if (!alreadyHovered)
                    {
                        anim.Play("An_InteractTextPopup");
                        CrosshairUI.SetActive(true);
                        alreadyHovered2 = false;
                        alreadyHovered = true;
                    }
                    hover = true;
                    if (Input.GetKeyDown(KeyCode.Mouse0))
                    {
                        hit.transform.SendMessage("Interacting", SendMessageOptions.DontRequireReceiver);
                    }
                    if (Input.GetKeyDown(KeyCode.Mouse1))
                    {
                        hit.transform.SendMessage("Looking", SendMessageOptions.DontRequireReceiver);
                    }
                }
            }
            else if (hit.transform.tag != "Interactable")
            {
                CrosshairUI.SetActive(false);
                dispText.text = "";
                hover = false;
                alreadyHovered = false;
                if (!alreadyHovered2)
                {
                    anim.Play("An_InteractTextPopout");
                    alreadyHovered2 = true;
                }
                if(storedIntObj != null)
                {
                    storedIntObj.transform.SendMessage("UnHover", SendMessageOptions.DontRequireReceiver);
                    storedIntObj = null;
                }
            }
        }
        else
        {
            hover = false;
            dispText.text = "";
            CrosshairUI.SetActive(false);
            storedIntObj = null;
        }
    }
}
