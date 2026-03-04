using UnityEngine;
using System.Collections;

namespace LongLiveKhioyen
{
	/// <summary>
	/// 用于引导开局流程的控制器。流程走完后自毁。
	/// </summary>
	public abstract class GameController : MonoBehaviour
	{
		protected GameInstance Game => GameInstance.Instance;
		protected GameData GameData => Game.Data;
		protected Polis Polis => Polis.Instance;
		protected PolisData PolisData => PolisData.Current;
		protected Battle Battle => Battle.Instance;

		protected IEnumerator Start()
		{
			DontDestroyOnLoad(gameObject);

			yield return new WaitUntil(() => Game != null);  // 等待游戏创建完成。
			yield return Routine();
			Destroy(gameObject);
		}

		protected abstract IEnumerator Routine();
	}
}
