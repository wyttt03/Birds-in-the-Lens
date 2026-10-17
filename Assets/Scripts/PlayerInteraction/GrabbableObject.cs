using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class GrabbableObject : MonoBehaviour {
    [HideInInspector]
    public bool beingCarried = false;
    private Rigidbody rigid;
    [Header("For Picking Up / Inspecting Things - ensure collider, rigidbody, and trigger")]
    [Tooltip("MainCamera -> Empty Game Object, a bit in front")]
    public Transform HoldPos;
    [Tooltip("200 Strong, 50 weak")]
    public float throwSpeed = 200;
    [Tooltip("0 pickup, 1 throw, 2 crash soft, 3 crash hard")]
    public AudioClip[] clips;
    private AudioSource source;
    [Tooltip("Primary renderer of this object")]
    public Renderer rend;
    private Color originColor;
    private bool over = false;
    [Tooltip("Color when hovered over (looked at)")]
    public Color targetColor = Color.yellow;
    private GameObject MainCam;
    private Interact InteractionScript;
    [Tooltip("Grab Object, Throw Object OR n/a")]
    public string[] prompts = { "Grab Object", "" };
    private bool touched = false;
    private float lowPitchRange = .97f;
    private float highPitchRange = 1.5f;
    private float velocityClipSplit = 5f;
    private bool rotating = false;
    private Quaternion rotateBy;
    private Vector3 originPos;
    private Quaternion originRot;
    private Vector3 originScale;
    [Tooltip("if object is returned to original position, snaps into place")]
    public bool canReturn = true;
    private bool objectReset = true;
    private float distToOrigin;
    //mute collision sounds on start (so things can fall down and set silently)
    private bool muteCollSound = true;

    // Use this for initialization
    void Start () {
        rigid = gameObject.GetComponent<Rigidbody>();
        MainCam = GameObject.FindWithTag("MainCamera");
        if (MainCam == null)
        {
            MainCam = GameObject.FindFirstObjectByType<Camera>().gameObject;
        }
        InteractionScript = MainCam.GetComponent<Interact>();
        source = gameObject.GetComponent<AudioSource>();
        originColor = rend.material.color;
        StartCoroutine(SetOriginTrans());
    }

    public void Hovering()
    {
        over = true;
        StartCoroutine(Fadeout());
        if (!beingCarried)
            InteractionScript.message = prompts[0];
    }
    public void Interacting()
    {
        beingCarried = !beingCarried;
        if (!beingCarried)
        {
            rigid.isKinematic = false;
            transform.parent = null;
            rigid.AddForce(MainCam.transform.forward * throwSpeed);  
            source.pitch = 1;
            source.clip = clips[1];//Throw
            source.Play();
            rotating = false;
            MouseLook[] foundLookScripts = FindObjectsByType<MouseLook>(FindObjectsSortMode.None);
            for (int i = 0; i < foundLookScripts.Length; i++)
            {
                foundLookScripts[i].working = true;
            }
            touched = false;
        }
        else if (beingCarried)
        {
            rigid.isKinematic = true;
            transform.position = HoldPos.position;
            transform.parent = MainCam.transform;
            source.pitch = 1;
            source.clip = clips[0];//Pickup
            source.Play();
            objectReset = false;
        }
        InteractionScript.message = prompts[1];
        InteractionScript.CrosshairUI.SetActive(false);
    }

    public void RelativeRotate(float rotateLeftRight, float rotateUpDown)
    {
        float sensitivity = 5f;
        //Gets the world vector space for cameras up vector 
        Vector3 relativeUp = MainCam.transform.TransformDirection(Vector3.up);
        //Gets world vector for space cameras right vector
        Vector3 relativeRight = MainCam.transform.TransformDirection(Vector3.right);

        //Turns relativeUp vector from world to objects local space
        Vector3 objectRelativeUp = transform.InverseTransformDirection(relativeUp);
        //Turns relativeRight vector from world to object local space
        Vector3 objectRelaviveRight = transform.InverseTransformDirection(relativeRight);

        rotateBy = Quaternion.AngleAxis(rotateLeftRight / gameObject.transform.localScale.x * sensitivity, objectRelativeUp)
            * Quaternion.AngleAxis(-rotateUpDown / gameObject.transform.localScale.x * sensitivity, objectRelaviveRight);
    }

    // Update is called once per frame
    void FixedUpdate()
    {

        if (rotating)
        {
            gameObject.transform.localRotation *= rotateBy;
        }

        if (!beingCarried)
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
        else
        {
            rend.material.color = originColor;
        }
        
        if (touched)
        {
            rigid.isKinematic = false;
            transform.parent = null;
            beingCarried = false;
            rotating = false;
            MouseLook[] foundLookScripts = FindObjectsByType<MouseLook>(FindObjectsSortMode.None);
            for (int i = 0; i < foundLookScripts.Length; i++)
            {
                foundLookScripts[i].working = true;
            }
            touched = false;
        }
    }

    void Update()
    {
        if (beingCarried)
        {
            if (Input.GetKeyDown(KeyCode.Mouse1) && !rotating)
            {
                MouseLook[] foundLookScripts = FindObjectsByType<MouseLook>(FindObjectsSortMode.None);
                for (int i = 0; i < foundLookScripts.Length; i++)
                {
                    foundLookScripts[i].working = false;
                }
                rotating = true;
            }
            if (Input.GetKeyDown(KeyCode.Mouse1))
            {
                RelativeRotate(-Input.GetAxis("Mouse X") * 5, -Input.GetAxis("Mouse Y") * 5);
            }
            if (Input.GetKeyUp(KeyCode.Mouse1) && rotating)
            {
                MouseLook[] foundLookScripts = FindObjectsByType<MouseLook>(FindObjectsSortMode.None);
                for (int i = 0; i < foundLookScripts.Length; i++)
                {
                    foundLookScripts[i].working = true;
                }
                rotating = false;
            } 
        }
        else
        {
            if (canReturn)
            {
                if (!objectReset)
                {
                    distToOrigin = Vector3.Distance(originPos, transform.position);
                    if (distToOrigin <= .25f)
                    {
                        //disable physics
                        rigid.isKinematic = true;
                        transform.position = originPos;
                        transform.rotation = originRot;
                        transform.localScale = originScale;
                        rigid.isKinematic = false;
                        objectReset = true;
                    }
                }
            }
        }

    }

    void OnTriggerEnter(Collider coll)
    {
        if (!beingCarried)
        {
            source.Stop();
            source.pitch = Random.Range(lowPitchRange, highPitchRange);
            float hitVol = Random.Range(.7f,1);
            if (!muteCollSound)
            {
                if (rigid.linearVelocity.magnitude < velocityClipSplit)
                    source.PlayOneShot(clips[2], hitVol);//Soft Collision
                else
                    source.PlayOneShot(clips[3], hitVol);//Hard Collision
            }
            touched = true;
        }
    }
    
    private IEnumerator Fadeout()
    {
        yield return new WaitForSeconds(1);
        over = false;
    }

    private IEnumerator SetOriginTrans()
    {
        yield return new WaitForSeconds(.25f);
        originPos = transform.position;
        originRot = transform.rotation;
        originScale = transform.localScale;
        yield return new WaitForSeconds(.75f);
        muteCollSound = false;
    }
}
