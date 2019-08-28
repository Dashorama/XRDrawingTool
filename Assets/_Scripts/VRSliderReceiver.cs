using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Slider))]
public class VRSliderReceiver : MonoBehaviour, IPointerClickHandler
{
    Slider slider;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (slider == null)
            slider = GetComponent<Slider>();
        slider.OnDrag(eventData);
    }
}
