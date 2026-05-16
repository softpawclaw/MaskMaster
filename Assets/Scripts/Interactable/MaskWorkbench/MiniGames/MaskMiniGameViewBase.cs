using UnityEngine;

namespace Interactable.MaskWorkbench.MiniGames
{
    public abstract class MaskMiniGameViewBase : MonoBehaviour
    {
        public virtual void Init(MaskMiniGameConfig config, MaskMiniGameRequest request) { }
        public abstract void SetCursor(float center01, float size01);
        public virtual void Show() => gameObject.SetActive(true);
        public virtual void Hide() => gameObject.SetActive(false);
    }
}
