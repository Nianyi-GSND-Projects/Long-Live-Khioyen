using UnityEngine;
using System.Collections.Generic;

namespace LongLiveKhioyen
{
    [System.Serializable]
    public class TutorialPage
    {
        public string title;
        
        [TextArea(3, 5)] 
        public string topText;
        
        public Sprite image;
        
        [TextArea(3, 5)] 
        public string bottomText;
    }

    [CreateAssetMenu(menuName = "Long Live Khioyen/Tutorial/Tutorial Definition")]
    public class TutorialDefinitionSO : ScriptableObject
    {
        public List<TutorialPage> pages = new List<TutorialPage>();
    }
}