using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

namespace LongLiveKhioyen
{
	public class CostPreviewPanel : MonoBehaviour
	{
		void Update()
		{
			transform.position = Pointer.current.position.value;
		}

		[SerializeField] TMP_Text foodText, moneyText, knowledgeText;

		public void UpdateCostData(Economy cost)
		{
			foodText.text = cost.food.ToString();
			moneyText.text = cost.money.ToString();
			knowledgeText.text = cost.material.ToString();
		}
	}
}