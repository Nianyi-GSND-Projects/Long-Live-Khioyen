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

            // 1. 更新内容
            if (titleText != null) titleText.text = page.title;
            if (topContentText != null) topContentText.text = page.topText;
            if (bottomContentText != null) bottomContentText.text = page.bottomText;
            
            if (tutorialImage != null)
            {
                tutorialImage.sprite = page.image;
                tutorialImage.gameObject.SetActive(page.image != null);
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
