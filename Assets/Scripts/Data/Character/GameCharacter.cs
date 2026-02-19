using UnityEngine;

namespace LongLiveKhioyen
{
    [CreateAssetMenu(menuName = "Long Live Khioyen/Game Character")]
    public class GameCharacter : ScriptableObject
    {
        public string characterName;
        public Sprite portrait;
        [TextArea] public string description;
    }
}