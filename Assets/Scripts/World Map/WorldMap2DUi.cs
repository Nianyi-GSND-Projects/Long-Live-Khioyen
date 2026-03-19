using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace LongLiveKhioyen
{
	public class WorldMap2DUi : MonoBehaviour
	{
		[SerializeField] WorldMap2D worldMap;
		ArmyStatus Army => GameInstance.Instance.ActiveArmy;

		#region Life cycle
		protected void Update()
		{
			RefreshFoodSlider();
		}
		#endregion

		#region Food
		[SerializeField] Slider foodSlider;
		[SerializeField] TMP_Text foodText;

		void RefreshFoodSlider()
		{
			float foodValue = 0;
			if(Army.initialFood != 0)
				foodValue = Army.carriedFood / Army.initialFood;
			foodSlider.value = foodValue;
			foodText.text = $"{Mathf.FloorToInt(Army.carriedFood)}";
		}
		#endregion
	}
}
