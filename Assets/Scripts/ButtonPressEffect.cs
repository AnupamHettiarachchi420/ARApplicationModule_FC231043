using UnityEngine;
using UnityEngine.EventSystems;

public class ButtonPressEffect : MonoBehaviour,
    IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [SerializeField] private float pressedScale = 0.92f;
    private Vector3 normalScale;

    private void Awake() { normalScale = transform.localScale; }

    public void OnPointerDown(PointerEventData e) { transform.localScale = normalScale * pressedScale; }
    public void OnPointerUp(PointerEventData e) { transform.localScale = normalScale; }
    public void OnPointerExit(PointerEventData e) { transform.localScale = normalScale; }
}