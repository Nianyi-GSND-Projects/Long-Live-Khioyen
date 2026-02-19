using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using System;

namespace LongLiveKhioyen
{
	public class ExpeditionPanel : MonoBehaviour
	{
		#region 生命周期
		protected void Start()
		{
			Refresh();

			PolisData.Current.onGarrisonChanged += Refresh;
		}

		protected void OnDestroy()
		{
			PolisData.Current.onGarrisonChanged -= Refresh;
		}

		void Refresh()
		{
			RefreshCommanders();
			RefreshSelecteds();
			RefreshDepartButton();
		}
		#endregion

		#region 选中状态
		readonly HashSet<GameCommander> selectedCommanders = new();

		IEnumerable<(GameCommander, FancyListItem)> ListOut(Transform root, IEnumerable<GameCommander> commanders)
		{
			root.ClearChildren();
			foreach(var commander in commanders)
			{
				FancyListItem item = FancyListItem.Instantiate(root);
				item.ApplyCommander(commander);
				yield return (commander, item);
			}
		}

		IEnumerable<(GameCommander, FancyListItem)> ListOut(Component root, bool selected)
		{
			return ListOut(root.transform, PolisData.Current.GetGarrisonedCommanders().Where(c => selectedCommanders.Contains(c) == selected));
		}

		void Select(GameCommander commander)
		{
			selectedCommanders.Add(commander);
			OnSelectionChanged();
		}

		void Deselect(GameCommander commander)
		{
			selectedCommanders.Remove(commander);
			OnSelectionChanged();
		}

		void OnSelectionChanged()
		{
			Refresh();
		}
		#endregion

		#region 左侧指挥官列表
		[SerializeField] LayoutGroup commandersLayoutGroup;

		void RefreshCommanders()
		{
			foreach(var (commander, item) in ListOut(commandersLayoutGroup, false))
				item.onClick += () => Select(commander);
		}
		#endregion

		#region 右侧选中列表
		[SerializeField] LayoutGroup selectedLayoutGroup;

		void RefreshSelecteds()
		{
			foreach(var (commander, item) in ListOut(selectedLayoutGroup, true))
				item.onClick += () => Deselect(commander);
		}
		#endregion

		#region 出城
		[SerializeField] Button departButton;

		void RefreshDepartButton()
		{
			departButton.interactable = selectedCommanders.Count > 0;
		}

		public void Depart()
		{
			GameInstance.Instance.DepartFromPolis(selectedCommanders.ToArray());
		}
		#endregion
	}
}
