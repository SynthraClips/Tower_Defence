using UnityEngine;
using TMPro;

public class SpeedControl : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI label; // e.g., a TMP text on the button to show "1x / 2x / 3x"

    [Header("Speeds")]
    public float[] speeds = new float[] { 1f, 2f, 3f };

    private int index = 0;
    private float baseFixedDeltaTime;

    private void Awake()
    {
        baseFixedDeltaTime = Time.fixedDeltaTime;
        ApplySpeed();
    }

    public void CycleSpeed()
    {
        index = (index + 1) % speeds.Length;
        ApplySpeed();
    }

    private void ApplySpeed()
    {
        float s = speeds[index];
        Time.timeScale = s;
        // Keep physics stable relative to timescale:
        Time.fixedDeltaTime = baseFixedDeltaTime * Time.timeScale;

        if (label) label.text = $"{s:0.#}x";
    }
}
