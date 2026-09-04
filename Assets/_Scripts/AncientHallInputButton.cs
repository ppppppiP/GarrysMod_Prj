using UnityEngine;
using UnityEngine.EventSystems;

public sealed class AncientHallInputButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public enum InputKind { Left, Right, Jump }
    [InspectorName("Действие кнопки")] public InputKind kind;
    [InspectorName("Контроллер готового персонажа")] public PlayerController player;

    private void Awake() { if (player == null) player = FindFirstObjectByType<PlayerController>(); }
    public void OnPointerDown(PointerEventData eventData)
    {
        if (player == null) return;
        if (kind == InputKind.Left) player.SetMobileHorizontal(-1f);
        else if (kind == InputKind.Right) player.SetMobileHorizontal(1f);
        else player.RequestMobileJump();
    }
    public void OnPointerUp(PointerEventData eventData)
    {
        if (player != null && kind != InputKind.Jump) player.SetMobileHorizontal(0f);
    }
}
