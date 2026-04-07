using System.Collections;
using Global;
using UnityEngine;

namespace Systems
{
    public class DelayedDialogSystem : MonoBehaviour
    {
        [Header("Bell")]
        [SerializeField] private string bellDialogId;
        [SerializeField] private float bellDelay = 3f;

        private UISystem uiSystem;
        private Coroutine bellCoroutine;

        public void Link()
        {
            uiSystem = Linker.Instance.UISystem;
        }

        public void ScheduleBell()
        {
            if (string.IsNullOrEmpty(bellDialogId))
            {
                Debug.LogWarning("DelayedDialogSystem: bellDialogId is empty.");
                return;
            }

            if (bellCoroutine != null)
            {
                StopCoroutine(bellCoroutine);
            }

            bellCoroutine = StartCoroutine(BellRoutine());
        }

        private IEnumerator BellRoutine()
        {
            yield return new WaitForSeconds(bellDelay);

            if (uiSystem == null)
            {
                Debug.LogWarning("DelayedDialogSystem: UISystem is not linked.");
                bellCoroutine = null;
                yield break;
            }

            uiSystem.Execute(bellDialogId, null);
            bellCoroutine = null;
        }
    }
}