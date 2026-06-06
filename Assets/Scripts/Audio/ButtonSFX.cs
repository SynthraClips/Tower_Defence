using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class ButtonSFX : MonoBehaviour
{
    [SerializeField] private SFX sfx = SFX.Click;
    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(PlayClickSfx);
    }

    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(PlayClickSfx);
        }
    }

    private void PlayClickSfx()
    {
        AudioManager.Instance?.PlaySFX(sfx);
    }
}
