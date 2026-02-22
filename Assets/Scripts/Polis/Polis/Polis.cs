using UnityEngine;

namespace LongLiveKhioyen
{
	public partial class Polis : MonoBehaviour
	{
		static Polis instance;
		public static Polis Instance => instance;

		public PolisData Data => GameInstance.Instance.LastPolis;
		[SerializeField] PolisUi ui;

		#region 生命周期
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

			// 默认切到市长模式
			SwitchToMode(Mode.Mayor);
			IsInConstructModal = false;  // 编辑时可能没关建造面板，手动关一下

			InitializeConstruction();
			InitializeBuilding();
			InitializeTime();

			// 度过累积的时间
			float passedTime = GameInstance.Instance.Data.time.ElapsedGameTime - Data.LastTime.ElapsedGameTime;
			Data.PassTime(passedTime);
			GameInstance.Instance.onGameTimeAdvanced += Data.PassTime;
		}

		void OnDestroy()
		{
			if(GameInstance.Instance)
				GameInstance.Instance.onGameTimeAdvanced -= Data.PassTime;

			FinalizeConstruction();
			FinalizeBuilding();
			FinalizeTime();
		}

		void Update()
		{
			float dt = Time.deltaTime;
			UpdateTime(dt);
		}
		#endregion

		#region 选中
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

		#region 网格
		public Vector2 WorldToMap(Vector3 world)
		{
			var cell = grid.LocalToCellInterpolated(grid.WorldToLocal(world));
			return new(cell.x, cell.z);
		}
		public Vector2Int WorldToMapInt(Vector3 world)
			=> Vector2Int.FloorToInt(WorldToMap(world));
		public Vector3 MapToWorld(Vector2 map)
			=> grid.LocalToWorld(MapToLocal(map));
		public Vector3 MapToLocal(Vector2 map)
			=> grid.CellToLocalInterpolated(new(map.x, 0, map.y));

		public Vector3 ClampToMap(Vector3 pos)
		{
			pos = WorldToMap(pos);
			pos.x = Mathf.Clamp(pos.x, 0, Data.size.x);
			pos.y = Mathf.Clamp(pos.y, 0, Data.size.y);
			return MapToWorld(pos);
		}
		#endregion
	}
}
