using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace LongLiveKhioyen
{
	public partial class Polis : MonoBehaviour
	{
		static Polis instance;
		public static Polis Instance => instance;

		#region Life cycle
		void Awake()
		{
			instance = this;
		}

		void Start()
		{
			if(Data == null)
			{
				Debug.LogWarning("No polis data assigned, cannot initialize polis.");
				return;
			}

			// Orientation
			transform.rotation = Quaternion.Euler(0, Data.orientation, 0);
			gameObject.isStatic = true;

			// Construction
			ConstructGround();
			ConstructWalls();
			SpawnBuildingsFromData();

			// Population
			onTasksChanged += () => RequiredPopulation = CalculateRequiredPopulation();
			RequiredPopulation = CalculateRequiredPopulation();
			onBuildingsChanged += () => PopulationCap = CalculatePopulationCap();
			PopulationCap = CalculatePopulationCap();

			// Time
			float passedTime = GameInstance.Instance.GameTime - LastTime;
			PassTime(passedTime);
			GameInstance.Instance.onGameTimeAdvanced += PassTime;

			// Navmesh
			navMeshSurface.RemoveData();
			navMeshSurface.BuildNavMesh();

			// DEBUG: Plop NPCs
			var npcTemplate = Resources.Load<GameObject>("Prefabs/Polis/Characters/NPC-dummy");
			for(int i = 0; i < 100; ++i)
			{
				var position = Utilities.GetRandomPositionOnHavMesh(navMeshSurface);
				var npc = Instantiate(npcTemplate);
				npc.transform.SetParent(transform, false);
				npc.transform.position = position;
			}

			// Mode
			SwitchToMode(Mode.Mayor);
			IsInConstructModal = false;
			AnchorPosition = MapToWorld((Vector2)Size * .5f);
		}

		void OnDestroy()
		{
			instance = null;
			if(GameInstance.Instance)
				GameInstance.Instance.onGameTimeAdvanced -= PassTime;
		}

		void Update()
		{
			float dt = Time.deltaTime;
			if(dt > 0)
				GameInstance.Instance.AdvanceTime(dt);
		}
		#endregion

		#region Data
		public PolisData Data => GameInstance.Instance.LastPolis;
		Vector2Int Size => Data.size;
		#endregion

		#region Population
		public System.Action onPopulationDataChanged;

		public int Population
		{
			get => Data.population;
			private set
			{
				Data.population = Mathf.Min(value, PopulationCap);
				onPopulationDataChanged?.Invoke();
			}
		}

		int requiredPopulation;
		public int RequiredPopulation
		{
			get => requiredPopulation;
			private set
			{
				requiredPopulation = value;
				onPopulationDataChanged?.Invoke();
			}
		}

		int CalculateRequiredPopulation()
		{
			if(Data.Tasks.Count == 0)
				return 0;
			return Data.Tasks.Select(t => t.requiredPopulation).Aggregate((a, b) => a + b);
		}

		public int FreePopulation
			=> Population - RequiredPopulation;

		public float Efficiency
		{
			get
			{
				int required = RequiredPopulation;
				if(required <= Population)
					return 1f;
				return (float)Population / required;
			}
		}

		int populationCap;
		public int PopulationCap
		{
			get => populationCap;
			private set
			{
				populationCap = value;
				Population = Mathf.Min(Population, PopulationCap);  // 此行自动触发更新事件。
			}
		}

		/// <summary>
		/// 计算城池人口上限。
		/// </summary>
		/// <remarks>
		/// 应该根据民居与水井的数量及分布计算，但牢宋还没给出具体算法，此处先用 民居数量*10 占位。
		/// </remarks>
		int CalculatePopulationCap()
		{
			return QueryBuildingsByTag("dwelling").Length * 10;
		}
		#endregion

		#region Economy
		public Economy Economy
		{
			get => Data.economy;
			set
			{
				Data.economy = value;
				onEconomyChanged?.Invoke();
			}
		}

		public System.Action onEconomyChanged;

		public bool CheckResourceAffordance(Economy cost)
		{
			return cost <= Economy;
		}

		public bool TryCostResource(Economy cost, bool actuallyCost = true)
		{
			if(!CheckResourceAffordance(cost))
				return false;
			if(actuallyCost)
				Economy -= cost;
			return true;
		}
		#endregion

		#region Time
		float LastTime
		{
			get => Data.lastTime;
			set => Data.lastTime = value;
		}

		void PassTime(float amount)
		{
			if(Efficiency == 0)
			{
				// 效率为 0（人口也为 0）时无法执行任何任务，可安全度过时间。
				PassTime_Simple(amount);
				return;
			}

			while(amount > 0)
			{
				if(Tasks.Count == 0)
				{
					PassTime_Simple(amount);
					return;
				}
				float a = Mathf.Min(amount, Tasks[0].remainingTime / Efficiency);
				PassTime_Simple(a);
				amount -= a;
			}
		}

		void PassTime_Simple(float amount)
		{
			foreach(var task in Tasks)
				task.remainingTime -= amount * Efficiency;
			var toBeExecuted = Tasks.Where(t => t.remainingTime <= 0).ToArray();
			foreach(var task in toBeExecuted)
			{
				ExecuteTask(task);
				RemoveTask(task);
			}
			LastTime += amount;
		}
		#endregion

		#region Tasks
		IReadOnlyList<PolisTask> Tasks => Data.Tasks;

		public System.Action onTasksChanged;

		public void AddTask(PolisTask task)
		{
			Data.AddTask(task);
			onTasksChanged.Invoke();
		}

		public void RemoveTask(PolisTask task)
		{
			Data.RemoveTask(task);
			onTasksChanged.Invoke();
		}

		void ExecuteTask(PolisTask task)
		{
			switch(task.type)
			{
				case PolisTaskType.construction:
					ExecuteConstructionTask(task);
					break;
				case PolisTaskType.monthPassed:
					ExecuteMonthPassedTask(task);
					break;
				default: throw new System.NotSupportedException();
			}
		}

		void ExecuteConstructionTask(PolisTask task)
		{
			int x = int.Parse(task.parameters[0]), y = int.Parse(task.parameters[1]);
			var site = GetBuildingAt(x, y) as ConstructionSite;
			if(site == null)
			{
				Debug.LogError($"No construction site at ({x}, {y}).");
				return;
			}
			FinishConstruction(site);
		}

		void ExecuteMonthPassedTask(PolisTask task)
		{
			int startingMonth = int.Parse(task.parameters[0]);
			Debug.Log($"Month passed in polis \"{Data.id}\". Starting month: {startingMonth}");
			// TODO
		}
		#endregion

		#region Selection
		public ISelectable selected;

		public System.Action<ISelectable> onSelectionChanged;

		public ISelectable Selected
		{
			get => selected;
			set
			{
				selected?.OnDeselect();
				selected = value;
				selected?.OnSelect();
				onSelectionChanged?.Invoke(selected);
			}
		}
		#endregion
	}
}
