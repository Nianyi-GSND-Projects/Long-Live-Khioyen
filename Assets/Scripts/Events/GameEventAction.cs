using UnityEngine;

namespace LongLiveKhioyen
{
    public abstract class GameEventAction : ScriptableObject
    {
        [TextArea] public string description;
        public abstract void Execute();
    }
}