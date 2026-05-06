using Enums;
using UnityEngine;

namespace Interactable.MaskWorkbench
{
    public class MaskWorkpieceSocketView : MonoBehaviour
    {
        [SerializeField] private MaskSocket socket = MaskSocket.None;
        [SerializeField] private Transform selectionAnchor;
        [SerializeField] private GameObject emptyVisual;
        [SerializeField] private GameObject selectedVisual;
        [SerializeField] private GameObject plannedVisual;

        public MaskSocket Socket => socket;
        public Transform SelectionAnchor => selectionAnchor != null ? selectionAnchor : transform;

        private void Reset()
        {
            selectionAnchor = transform;
        }

        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }

        public void Refresh(bool selected, bool planned)
        {
            if (emptyVisual != null)
                emptyVisual.SetActive(!planned);

            if (selectedVisual != null)
                selectedVisual.SetActive(selected);

            if (plannedVisual != null)
                plannedVisual.SetActive(planned);
        }
    }
}
