using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace LongLiveKhioyen
{
    public class TutorialUI : MonoBehaviour
    {
        public static TutorialUI Instance { get; private set; }

        [Header("UI References")]
        public GameObject panel;
        public TMP_Text titleText;
        public TMP_Text topContentText;
        public Image tutorialImage;
        public TMP_Text bottomContentText;
        public GameObject imageContainer;
        [Header("Buttons")]
        public Button prevButton;
        public Button nextButton;
        public TMP_Text nextButtonText; // 用于修改 "Next" -> "Close"

        public bool IsActive => panel != null && panel.activeSelf;

        private TutorialDefinitionSO _currentTutorial;
        private int _currentIndex;

        private void Awake()
        {
            Instance = this;
            if (panel != null) panel.SetActive(false);
            
            if (prevButton != null) prevButton.onClick.AddListener(OnPrevClicked);
            if (nextButton != null) nextButton.onClick.AddListener(OnNextClicked);
        }

        public void Show(TutorialDefinitionSO tutorial)
        {
            if (tutorial == null || tutorial.pages.Count == 0) return;

            _currentTutorial = tutorial;
            _currentIndex = 0;
            
            if (panel != null) panel.SetActive(true);
            UpdatePage();
        }

        private void UpdatePage()
        {
            if (_currentTutorial == null) return;
    
            TutorialPage page = _currentTutorial.pages[_currentIndex];

            // 1. 标题
            if (titleText != null) titleText.text = page.title;

            // 2. 上部文本
            if (topContentText != null)
            {
                bool hasText = !string.IsNullOrEmpty(page.topText);
                topContentText.text = page.topText;
                topContentText.gameObject.SetActive(hasText); // [关键] 没字就隐藏
            }

            // 3. 图片
            bool hasImage = page.image != null;
        
            if (tutorialImage != null)
            {
                tutorialImage.sprite = page.image;
            }

            // [关键] 隐藏容器
            if (imageContainer != null)
            {
                imageContainer.SetActive(hasImage);
            }
            else if (tutorialImage != null)
            {
                // Fallback: 如果没绑容器，直接隐藏图片
                tutorialImage.gameObject.SetActive(hasImage);
            }

            // 4. 下部文本
            if (bottomContentText != null)
            {
                bool hasText = !string.IsNullOrEmpty(page.bottomText);
                bottomContentText.text = page.bottomText;
                bottomContentText.gameObject.SetActive(hasText); // [关键] 没字就隐藏
            }

            // 2. 更新按钮状态
            if (prevButton != null)
            {
                prevButton.interactable = _currentIndex > 0;
                prevButton.gameObject.SetActive(_currentTutorial.pages.Count > 1); // 如果只有一页，隐藏上一页按钮？或者只是禁用
            }

            if (nextButton != null)
            {
                bool isLastPage = _currentIndex >= _currentTutorial.pages.Count - 1;
                if (nextButtonText != null)
                {
                    nextButtonText.text = isLastPage ? "Close" : "Next";
                }
            }
            LayoutRebuilder.ForceRebuildLayoutImmediate(panel.GetComponent<RectTransform>());
        }

        private void OnPrevClicked()
        {
            if (_currentIndex > 0)
            {
                _currentIndex--;
                UpdatePage();
            }
        }

        private void OnNextClicked()
        {
            if (_currentIndex < _currentTutorial.pages.Count - 1)
            {
                _currentIndex++;
                UpdatePage();
            }
            else
            {
                Close();
            }
        }

        public void Close()
        {
            if (panel != null) panel.SetActive(false);
            _currentTutorial = null;
        }
    }
}
