using UnityEngine;
using System.Collections;

namespace LongLiveKhioyen
{
    [CreateAssetMenu(menuName = "Long Live Khioyen/Events/Actions/Show Tutorial")]
    public class ShowTutorialAction : GameEventAction
    {
        [Header("Content")]
        public TutorialDefinitionSO tutorialDef;

        public override void Execute()
        {
            if (TutorialUI.Instance != null && tutorialDef != null)
            {
                TutorialUI.Instance.Show(tutorialDef);
            }
        }

        public override IEnumerator ExecuteCoroutine()
        {
            if (TutorialUI.Instance != null && tutorialDef != null)
            {
                TutorialUI.Instance.Show(tutorialDef);
                
                // 阻塞直到关闭
                while (TutorialUI.Instance.IsActive)
                {
                    yield return null;
                }
            }
        }
    }
}