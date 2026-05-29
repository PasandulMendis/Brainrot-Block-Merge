using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MenuButtonSound : MonoBehaviour, IPointerClickHandler
{
    private Button btn;

    void Awake()
    {
        btn = GetComponent<Button>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (btn != null && !btn.interactable) return;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayMenuSound();
        }
    }
}