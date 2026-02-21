using UnityEngine;
using System.Collections.Generic;

namespace LongLiveKhioyen
{
    [CreateAssetMenu(menuName = "Long Live Khioyen/Events/Dialog Database")]
    public class DialogDatabase : ScriptableObject
    {
        public static DialogDatabase Instance => Resources.Load<DialogDatabase>("Data/DialogDatabase");

        public List<DialogChainAction> dialogs = new List<DialogChainAction>();

        public DialogChainAction GetDialog(int id)
        {
            return dialogs.Find(d => d.id == id);
        }
    }
}