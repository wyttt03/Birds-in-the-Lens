using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Marks an object as a photographable target (a bird or a human).
/// Attach to the target object. Uses a Renderer's bounds as its frame.
/// </summary>
public class PhotoTarget : MonoBehaviour
{
    /// <summary>All active targets in the scene.</summary>
    public static readonly List<PhotoTarget> All = new List<PhotoTarget>();

    [SerializeField] private string speciesId = "bird_01";

    [Tooltip("Leave empty to use the first Renderer found on this object or its children.")]
    [SerializeField] private Renderer boundsRenderer;

    [Tooltip("Shrinks the bounds, useful when the sprite has transparent padding.")]
    [Range(0.3f, 1f)]
    [SerializeField] private float boundsScale = 0.8f;

    public string SpeciesId => speciesId;
    public bool IsCaptured { get; private set; }

    private void Awake()
    {
        if (boundsRenderer == null)
        {
            boundsRenderer = GetComponentInChildren<Renderer>();
        }
    }

    private void OnEnable()
    {
        All.Add(this);
    }

    private void OnDisable()
    {
        All.Remove(this);
    }

    public void MarkCaptured()
    {
        IsCaptured = true;
    }

    public bool TryGetBounds(out Bounds bounds)
    {
        if (boundsRenderer == null)
        {
            bounds = default;
            return false;
        }

        Bounds source = boundsRenderer.bounds;
        bounds = new Bounds(source.center, source.size * boundsScale);
        return true;
    }
}