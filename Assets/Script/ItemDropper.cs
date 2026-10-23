using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Spawns falling items. Call Drop() from a UnityEvent (for example a bird's
/// On Take Off), and/or turn on random drops for a tree.
/// </summary>
public class ItemDropper : MonoBehaviour
{
    [Header("Ground")]
    [Tooltip("An empty object placed on the ground under the tree. Items land at its height.")]
    [SerializeField] private Transform groundPoint;
    [Header("Items")]
    [Tooltip("Item prefabs (with FallingItem). One is picked at random per drop.")]
    [SerializeField] private GameObject[] itemPrefabs;

    [Tooltip("Where items appear. Leave empty to use this object.")]
    [SerializeField] private Transform spawnPoint;

    [Tooltip("Items appear at a random spot inside this box around the spawn point.")]
    [SerializeField] private Vector3 spawnAreaSize = new Vector3(1f, 0.3f, 0.3f);

    [Tooltip("How many items fall each time Drop() is called.")]
    [SerializeField] private Vector2Int countRange = new Vector2Int(1, 3);

    [Header("Random Drops")]
    [SerializeField] private bool randomDrops = false;

    [Tooltip("Seconds between random drops, picked at random between the two values.")]
    [SerializeField] private Vector2 randomIntervalRange = new Vector2(8f, 20f);

    [Header("Feedback Events")]
    public UnityEvent onDropped;

    private float nextRandomDropTime;

    private void Start()
    {
        ScheduleNextRandomDrop();
    }

    private void Update()
    {
        if (randomDrops && Time.time >= nextRandomDropTime)
        {
            Drop();
            ScheduleNextRandomDrop();
        }
    }

    /// <summary>Drops a few items. Safe to call from a UnityEvent.</summary>
    public void Drop()
    {
        if (itemPrefabs == null || itemPrefabs.Length == 0)
        {
            return;
        }

        Transform origin = spawnPoint != null ? spawnPoint : transform;
        int count = Random.Range(countRange.x, countRange.y + 1);

        for (int i = 0; i < count; i++)
        {
            GameObject prefab = itemPrefabs[Random.Range(0, itemPrefabs.Length)];

            if (prefab == null)
            {
                continue;
            }

            Vector3 offset = new Vector3(
                Random.Range(-0.5f, 0.5f) * spawnAreaSize.x,
                Random.Range(-0.5f, 0.5f) * spawnAreaSize.y,
                Random.Range(-0.5f, 0.5f) * spawnAreaSize.z);

            // Face the same way as the spawn point, so the item lies in the same layer as the tree.
            GameObject item = Instantiate(
                prefab,
                origin.position + offset,
                Quaternion.Euler(0f, origin.eulerAngles.y, 0f));
            // Tell the item where the ground is.
            if (groundPoint != null)
            {
                FallingItem falling = item.GetComponent<FallingItem>();

                if (falling != null)
                {
                    falling.SetGroundHeight(groundPoint.position.y);
                }
            }
        }

        onDropped?.Invoke();
    }

    private void ScheduleNextRandomDrop()
    {
        nextRandomDropTime = Time.time + Random.Range(randomIntervalRange.x, randomIntervalRange.y);
    }
}