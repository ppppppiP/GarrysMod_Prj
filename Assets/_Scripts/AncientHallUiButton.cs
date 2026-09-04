using UnityEngine.EventSystems;
using UnityEngine.UI;

// Обычная экранная кнопка, но клавиша Jump/Space не вызывает её повторно после клика.
public sealed class AncientHallUiButton : Button
{
    public override void OnSubmit(BaseEventData eventData) { }
}
