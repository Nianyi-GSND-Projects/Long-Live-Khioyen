using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Localization.Settings;
using System.Collections.Generic;

namespace LongLiveKhioyen
{
	public class SettingsPanel : MonoBehaviour
	{
		#region Life cycle
		protected void Awake()
		{
			InitializeLocaleList();
		}
		#endregion

		#region Locale
		public VerticalLayoutGroup localeList;

		void InitializeLocaleList()
		{
			GameObject template = Resources.Load<GameObject>("Prefabs/UI/Pause/Locale Option");
			foreach(var locale in LocalizationSettings.AvailableLocales.Locales)
			{
				var option = Instantiate(template).GetComponent<LocaleOption>();
				option.transform.SetParent(localeList.transform, false);

				option.Locale = locale;
			}
			localeList.CalculateLayoutInputVertical();
		}
		#endregion
	}
}
