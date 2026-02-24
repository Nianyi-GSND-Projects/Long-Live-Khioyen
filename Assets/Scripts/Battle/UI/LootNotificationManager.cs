using UnityEngine;
using System.Collections.Generic;

namespace LongLiveKhioyen
{
    public class LootNotificationManager : MonoBehaviour
    {
        public static LootNotificationManager Instance { get; private set; }

        [Header("Settings")]
        public GameObject toastPrefab;
        public Transform container; // Vertical Layout Group

        private void Awake()
        {
            Instance = this;
        }

        public void ShowMessage(string message)
        {
            if (toastPrefab == null || container == null) return;

            GameObject go = Instantiate(toastPrefab, container);
            go.transform.SetAsLastSibling();

            LootToastUI toast = go.GetComponent<LootToastUI>();
            if (toast != null)
            {
                toast.Initialize(message);
            }
        }
    }
}