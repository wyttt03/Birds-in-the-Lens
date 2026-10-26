using UnityEngine;

public class LeafSway : MonoBehaviour
{
    [Header("摇摆设置")]
    [Tooltip("最大摇摆角度")]
    [SerializeField] private float angle = 4f;

    [Tooltip("每秒摇摆次数")]
    [Min(0f)]
    [SerializeField] private float frequency = 0.35f;

    [Tooltip("局部旋转轴。图片位于 XY 平面时通常使用 Z 轴")]
    [SerializeField] private Vector3 localAxis = Vector3.forward;

    [Tooltip("让不同树叶丛错开摇摆节奏")]
    [SerializeField] private bool randomStart = true;

    private Quaternion initialRotation;
    private float phase;
    private float elapsed;

    private void Awake()
    {
        initialRotation = transform.localRotation;
        phase = randomStart ? Random.Range(0f, Mathf.PI * 2f) : 0f;
    }

    private void OnEnable()
    {
        elapsed = 0f;
    }

    private void Update()
    {
        elapsed += Time.deltaTime;

        float t = elapsed * frequency * Mathf.PI * 2f + phase;

        // 随机摆动
        float wave =
            Mathf.Sin(t) * 0.8f +
            Mathf.Sin(t * 1.7f + phase) * 0.2f;

        Vector3 axis = localAxis.sqrMagnitude > 0.0001f
            ? localAxis.normalized
            : Vector3.forward;

        transform.localRotation =
            initialRotation * Quaternion.AngleAxis(wave * angle, axis);
    }

    private void OnDisable()
    {
        transform.localRotation = initialRotation;
    }
}
