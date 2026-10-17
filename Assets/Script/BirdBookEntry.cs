using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// One handbook entry. Shows the color sprite until its species is
/// photographed, then swaps to the gray sprite.
/// Attach to the Image inside the handbook pages.
/// </summary>
public class BirdBookEntry : MonoBehaviour
{
    [SerializeField] private string speciesId = "bird_01";
    [SerializeField] private Image image;
    [SerializeField] private Sprite colorSprite;
    [SerializeField] private Sprite graySprite;

    private void Awake()
    {
        if (image == null)
        {
            image = GetComponent<Image>();
        }
    }

    // The book pages are hidden until opened, so refresh every time they appear.
    private void OnEnable()
    {
        Refresh();
    }

    private void Refresh()
    {
        if (image == null)
        {
            return;
        }

        image.sprite = PhotoCapture.IsSpeciesCaptured(speciesId)
            ? graySprite
            : colorSprite;
    }
}