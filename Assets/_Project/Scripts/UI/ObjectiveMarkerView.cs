using UnityEngine;
using UnityEngine.UI;

namespace EscapeUNIFRANZ.UI
{
    public sealed class ObjectiveMarkerView : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private Image icon;
        [SerializeField] private RectTransform arrow;

        public void Render(Vector2 anchoredPosition, bool offScreen, float angle)
        {
            if (root == null || icon == null || arrow == null)
            {
                Hide();
                return;
            }

            ((RectTransform)root.transform).anchoredPosition = anchoredPosition;
            arrow.gameObject.SetActive(offScreen);
            if (offScreen)
            {
                arrow.localRotation = Quaternion.Euler(0f, 0f, angle);
            }

            root.SetActive(true);
        }

        public void Hide()
        {
            root?.SetActive(false);
        }
    }
}
