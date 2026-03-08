// Assets/Scripts/Battle/UI/UnitResultEntry.cs

using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace LongLiveKhioyen
{
    public class UnitResultEntry : MonoBehaviour
    {
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI hpText;
        public TextMeshProUGUI moraleText;
        public TextMeshProUGUI statusText;

        public void Initialize(BattalionStatus status, bool isDead, bool isRetreated)
        {
            nameText.text = status.battalionName;
            hpText.text = $"HP: {status.currentSolider}/{status.MaxSolider}";
            moraleText.text = $"Morale: {status.currentMorale}/{status.MaxMorale}";

            if (isDead)
            {
                statusText.text = "Annihilated";
                statusText.color = Color.red;
            }
            else if (isRetreated)
            {
                statusText.text = "Evacuated";
                statusText.color = Color.blue;
            }
            else
            {
                statusText.text = "Alive";
                statusText.color = Color.green;
            }
        }
    }
}