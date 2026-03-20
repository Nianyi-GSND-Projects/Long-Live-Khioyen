using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;
using Nianyi.UnityPack;
using TMPro;

namespace LongLiveKhioyen
{
	public interface ITooltipSource
	{
		public string GetTooltipText();

		public float Delay { get; }
	}

	public class TooltipManager : MonoBehaviour
	{
		Vector2 MousePosition => Mouse.current.position.value;

		#region Unity 生命周期
		protected void Update()
		{
			var es = EventSystem.current;
			if(!es)
			{
				SetHoveringGo(null);
				return;
			}

			List<RaycastResult> hits = new();
			es.RaycastAll(new(es) { position = MousePosition }, hits);
			if(hits.Count < 1)
			{
				SetHoveringGo(null);
				return;
			}

			SetHoveringGo(hits[0].gameObject);
		}

		protected void LateUpdate()
		{
			if(tooltip)
			{
				tooltip.transform.position = MousePosition;
			}
		}
		#endregion

		#region 核心
		ITooltipSource currentSource;
		GameObject tooltip;

		void ShowTooltip()
		{
			HideTooltip();
			if((currentSource as MonoBehaviour) == null)
				return;

			string text = currentSource.GetTooltipText();
			if(text == null)
				return;

			Canvas canvas = FindObjectOfType<Canvas>();
			if(canvas == null)
				return;

			tooltip = HierarchyUtility.InstantiatePrefabFromResource("Prefabs/UI/Tooltip");
			tooltip.transform.SetParent(canvas.transform, false);
			tooltip.GetComponentInChildren<TMP_Text>(true).text = text;
		}

		void HideTooltip()
		{
			if(tooltip == null)
				return;
			Destroy(tooltip);
			tooltip = null;
		}
		#endregion

		#region 时序
		GameObject hoveringGo;
		void SetHoveringGo(GameObject value)
		{
			if(value == null)
				value = null;
			if(Equals(hoveringGo, value))  // 若 hoveringGo 已失效，将其转为真正的 null
				return;
			hoveringGo = value;

			if(hoveringGo == null)
			{
				SetSource(null);
				return;
			}
			SetSource(hoveringGo.GetComponentInParent<ITooltipSource>());
		}

		void SetSource(ITooltipSource source)
		{
			if(source == currentSource)
				return;
			currentSource = source;
			if(currentSource == null)
			{
				StopCountdown();
				return;
			}
			RestartCountdown();
		}

		Coroutine coroutine;
		void StopCountdown()
		{
			HideTooltip();
			if(coroutine == null)
				return;
			StopCoroutine(coroutine);
			coroutine = null;
		}

		void RestartCountdown()
		{
			StopCountdown();
			coroutine = StartCoroutine(ShowTooltipDelayed());
		}

		IEnumerator ShowTooltipDelayed()
		{
			HideTooltip();
			yield return new WaitForSecondsRealtime(currentSource.Delay);
			ShowTooltip();
		}
		#endregion

		#region 静态接口
		public static Tooltip SetTooltip(GameObject go, string text)
		{
			if(go.TryGetComponent<ITooltipSource>(out var existing))
				Destroy(existing as MonoBehaviour);
			var tooltip = go.AddComponent<Tooltip>();
			tooltip.TooltipText = text;
			return tooltip;
		}
		#endregion
	}
}
