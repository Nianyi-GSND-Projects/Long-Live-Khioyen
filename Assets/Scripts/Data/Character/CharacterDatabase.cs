using System.Collections.Generic;
using UnityEngine;

namespace LongLiveKhioyen
{
    [CreateAssetMenu(menuName = "Long Live Khioyen/Database/Character Database")]
    public class CharacterDatabase : ScriptableObject
    {
        [SerializeField] private List<GameCharacter> characters = new List<GameCharacter>();
        
        private Dictionary<string, GameCharacter> _characterMap;

        private static CharacterDatabase _instance;
        public static CharacterDatabase Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<CharacterDatabase>("Data/CharacterDatabase");
                    if (_instance != null) _instance.Initialize();
                }
                return _instance;
            }
        }

        public void Initialize()
        {
            _characterMap = new Dictionary<string, GameCharacter>();
            foreach (var character in characters)
            {
                // 以 characterName 作为检索的 Key
                if (character != null && !string.IsNullOrEmpty(character.characterName))
                {
                    if (!_characterMap.ContainsKey(character.characterName))
                    {
                        _characterMap.Add(character.characterName, character);
                    }
                }
            }
            Debug.Log($"Character Database initialized with {_characterMap.Count} characters.");
        }

        public GameCharacter GetCharacter(string charName)
        {
            if (_characterMap == null) Initialize();

            if (_characterMap.TryGetValue(charName, out var character))
            {
                return character;
            }
            Debug.LogWarning($"[CharacterDatabase] Character '{charName}' not found in database!");
            return null;
        }
    }
}