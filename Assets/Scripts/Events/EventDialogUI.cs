using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

namespace LongLiveKhioyen
{
    public class EventDialogUI : MonoBehaviour
    {
        public static EventDialogUI Instance { get; private set; }

        public static EventDialogUI GetOrCreateInstance()
        {
            if(Instance == null)
            {
                var prefab = Resources.Load<GameObject>("Prefabs/UI/Events/DialogPanel");
                if(prefab == null)
                {
                    Debug.LogError("无法加载 DialogPanel 的 prefab，实例化失败。");
                    return null;
                }
                Instance = Instantiate(prefab).GetComponent<EventDialogUI>();
            }
            return Instance;
        }

				[Header("UI References")]
        [SerializeField] private GameObject dialogPanel;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text contentText;
        [SerializeField] private Image portraitImage;
        [SerializeField] private Button nextButton;
        
        [Header("Blocking")]
        [SerializeField] private GameObject inputBlocker;
        
        private BattleEventDefinition _currentBattleEventDef;

        // 新增：对话队列
        private Queue<DialogData> _dialogQueue = new Queue<DialogData>();
        
        public bool IsActive => dialogPanel != null && dialogPanel.activeSelf;
        
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            
            Hide();
            
            
            if (nextButton != null)
            {
                nextButton.onClick.AddListener(OnNextClicked);
            }
        }

        // 新增：启动对话链
        public void StartDialogChain(DialogChainAction action)
        {
            if (BattleEventManager.Instance != null)
            {
                _currentBattleEventDef = BattleEventManager.Instance.CurrentEvent;
            }
            
            _dialogQueue.Clear();
            
            foreach (var data in action.dialogList)
            {
                _dialogQueue.Enqueue(data);
            }
            
            ShowNextDialog();
        }

        private void ShowNextDialog()
        {
            if (_dialogQueue.Count == 0)
            {
                Hide();
                return;
            }
            if (inputBlocker != null) inputBlocker.SetActive(true);

            DialogData currentDialog = _dialogQueue.Dequeue();
            
            if (dialogPanel != null) dialogPanel.SetActive(true);
            
            // 使用辅助属性获取最终显示内容
            if (nameText != null) nameText.text = currentDialog.GetDisplayName(_currentBattleEventDef);
            if (contentText != null) contentText.text = currentDialog.dialogText;
            
            if (portraitImage != null)
            {
                Sprite portrait = currentDialog.DisplayPortrait;
                if (portrait != null)
                {
                    portraitImage.sprite = portrait;
                    portraitImage.gameObject.SetActive(true);
                }
                else
                {
                    portraitImage.gameObject.SetActive(false);
                }
            }
        }

        private void OnNextClicked()
        {
            Debug.Log("Event Set");
            ShowNextDialog();
        }

        public System.Action onHidden;
        public void Hide()
        {
            if (dialogPanel != null) dialogPanel.SetActive(false);
            if (inputBlocker != null) inputBlocker.SetActive(false);
			      onHidden?.Invoke();
				}
    }
}