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
			GameInstance.Instance.ExecuteWhenInitialized(Initialize);
		}

		void Initialize()
		{
			if(Data == null)
				player.gameObject.SetActive(false);

			InitializeUi();

			// Mode
			SwitchToMode(Mode.Mayor);
			IsInConstructModal = false;

			// Orientation
			transform.rotation = Quaternion.Euler(0, Data.orientation, 0);
			gameObject.isStatic = true;

			// Constructions
			ConstructGround();
			ConstructWalls();
			InitializeBuildings();

			// Initialize Navmesh
			navMeshSurface.RemoveData();
			navMeshSurface.BuildNavMesh();

			// Center view
			AnchorPosition = MapToWorld((Vector2)Size * .5f);

			// Time
			float passedTime = GameInstance.Instance.GameTime - LastTime;
			PassTime(passedTime);
			GameInstance.Instance.onGameTimeAdvanced += PassTime;
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
		PolisData Data => GameInstance.Instance.LastPolis;

		public string Id => Data.id;
		public Vector2Int Size => Data.size;
		#endregion

		#region Population
		public System.Action onPopulationChanged;

		public int Population { get; private set; } = 10;  // Debug
		public int BusyPopulation { get; private set; } = 5;
		public int PopulationCap { get; private set; } = 12;
		#endregion

		#region Economy
		public Economy Economy
		{
			get => Data.economy;
			set => Data.economy = value;
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
			{
				Economy -= cost;
				onEconomyChanged?.Invoke();
			}
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
			while(amount > 0)
			{
				if(Tasks.Count == 0)
				{
					PassTime_Simple(amount);
					return;
				}
				float a = Mathf.Min(amount, Tasks[0].remainingTime);
				PassTime_Simple(a);
				amount -= a;
			}
		}

		void PassTime_Simple(float amount)
		{
			foreach(var task in Tasks)
				task.remainingTime -= amount;
			var toBeExecuted = Tasks.Where(t => t.remainingTime <= 0).ToArray();
			foreach(var task in toBeExecuted)
			{
				ExecuteTask(task);
				Tasks.Remove(task);
			}
			LastTime += amount;
		}
		#endregion

		#region Tasks
		IList<PolisTask> Tasks => Data.Tasks;

		public void AddTask(PolisTask task)
		{
			Data.AddTask(task);
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
			var building = GetBuildingAt(x, y);
			if(building == null)
			{
				Debug.LogError($"No building at ({x}, {y}).");
				return;
			}
			building.UnderConstruction = false;
		}

		void ExecuteMonthPassedTask(PolisTask task)
		{
			int startingMonth = int.Parse(task.parameters[0]);
			Debug.Log($"Month passed in polis \"{Data.id}\". Starting month: {startingMonth}");
			// TODO
		}
		#endregion

		#region Selection
		readonly List<ISelectable> selection = new();
		public IList<ISelectable> Selection
		{
			get => selection;
			set
			{
				foreach(var s in selection)
					s.OnDeselect();

				selection.Clear();
				selection.AddRange(value);

				foreach(var s in selection)
					s.OnSelect();
			}
		}
		#endregion
	}
}
