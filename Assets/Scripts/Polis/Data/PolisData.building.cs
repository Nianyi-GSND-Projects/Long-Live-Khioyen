using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using NaughtyAttributes;

namespace LongLiveKhioyen
{
	public partial class PolisData
	{
		[ShowIf("type", PolisType.Controlled)] public List<BuildingPlacement> buildings;

		#region 事件
		public Action onBuildingsChanged;
		public Action<BuildingPlacement> onConstructionSitePlaced;
		public Action<BuildingPlacement> onBuildingConstructed;
		public Action<BuildingPlacement> onBuildingChanged;
		public Action<BuildingPlacement> onBuildingRemoved;
		#endregion

		#region 公有方法
		public void ConstructBuilding(string id, Vector2Int mapPosition, int orientation)
		{
			if(!GameManager.FindBuildingDefinitionById(id, out var definition))
				return;
			BuildingPlacement building = new(id, mapPosition, orientation, true);
			buildings.Add(building);

			PolisTask task = new(
				PolisTaskType.buildingConstructed,
				definition.constructionTime,
				definition.constructionPopulation,
				mapPosition.x.ToString(),
				mapPosition.y.ToString()
			);
			AddTask(task);

			onConstructionSitePlaced?.Invoke(building);
			OnAnyBuildingChanged();
		}

		void FinishConstructionSite(Vector2Int pos)
		{
			var building = GetBuildingAt(pos);
			building.underConstruction = false;

			onBuildingConstructed?.Invoke(building);
			CheckAndExecuteOnConstructionCompletedTasks(building);
			onBuildingChanged?.Invoke(building);
			OnAnyBuildingChanged();
		}

		public void RelocateBuilding(BuildingPlacement building, Vector2Int newPosition, int newOrientation)
		{
			building.position = newPosition;
			building.orientation = newOrientation;

			onBuildingChanged?.Invoke(building);
			OnAnyBuildingChanged();
		}

		public void RemoveBuilding(BuildingPlacement building)
		{
			buildings.Remove(building);

			onBuildingRemoved?.Invoke(building);
			OnAnyBuildingChanged();
		}
		#endregion

		#region 任务
		void ExecuteBuildingConstructedTask(PolisTask task)
		{
			int x = int.Parse(task.parameters[0]), y = int.Parse(task.parameters[1]);
			FinishConstructionSite(new(x, y));
		}

		[SerializeField] List<KeyValuePair<string, PolisTask>> scheduledOnConstructionCompletedTasks = new();

		void ExecuteScheduleOnConstructionCompleted(PolisTask task)
		{
			string tag = task.parameters[0];
			string taskType = task.parameters[1];
			string[] taskParams = task.parameters.Skip(2).ToArray();
			scheduledOnConstructionCompletedTasks.Add(new(tag, new(taskType, 0, taskParams)));
		}

		/// <summary>在建造完成时，检查是否有建造完成事件可触发，并触发之。</summary>
		void CheckAndExecuteOnConstructionCompletedTasks(BuildingPlacement placement)
		{
			var targetTasks = scheduledOnConstructionCompletedTasks
				.Where(pair => placement.Definition.tags.Contains(pair.Key))
				.Select(pair => pair.Value)
				.ToArray();
			scheduledOnConstructionCompletedTasks.RemoveAll(pair => targetTasks.Any(task => task == pair.Value));
			foreach(var task in targetTasks)
				AddTask(task);
		}
		#endregion

		#region 辅助
		void OnAnyBuildingChanged()
		{
			onBuildingsChanged?.Invoke();
			NotifyPossiblePopulationChange();
		}

		public BuildingPlacement[] QueryBuildingsByTag(params string[] tags)
		{
			return buildings
				.Where(b => tags.Any(t => b.Definition.tags.Contains(t)))
				.ToArray();
		}

		public BuildingPlacement GetBuildingAt(Vector2Int position)
		{
			foreach(var placement in buildings)
			{
				if(YieldBuildingOccupancy(placement).Any(grid => grid == position))
					return placement;
			}
			return null;
		}

		public IEnumerable<Vector2Int> YieldBuildingOccupancy(BuildingPlacement placement)
		{
			// GPT gen

			// Local helper to rotate a grid vector by k quarter turns around +Y (same as Transform.Rotate(0, k*90, 0)).
			// Mapping follows Unity's left-handed transform convention:
			//   0: (x, y) -> ( x,  y)
			//   1: (x, y) -> ( y, -x)
			//   2: (x, y) -> (-x, -y)
			//   3: (x, y) -> (-y,  x)
			static Vector2Int Rot90(Vector2Int v, int quarterTurns)
			{
				quarterTurns = ((quarterTurns % 4) + 4) % 4; // normalize to {0,1,2,3}
				return quarterTurns switch
				{
					0 => v,
					1 => new Vector2Int(v.y, -v.x),
					2 => new Vector2Int(-v.x, -v.y),
					3 => new Vector2Int(-v.y, v.x),
					_ => v // unreachable
				};
			}

			var definition = placement.Definition;
			var size = definition.size;          // rectangle size in cells at orientation=0
			var pivot = definition.pivot;         // rotation pivot in local (orientation=0) cell coords
			var origin = placement.position;       // world/grid coords where the pivot is placed
			int rot = placement.orientation & 3;

			// Enumerate every cell of the footprint (orientation=0 local space),
			// rotate the offset around the pivot, then translate to world/grid space.
			for(int ly = 0; ly < size.y; ly++)
			{
				for(int lx = 0; lx < size.x; lx++)
				{
					// Local cell (lx, ly) relative to pivot
					var deltaLocal = new Vector2Int(lx - pivot.x, ly - pivot.y);

					// Rotate around pivot and translate by 'origin'
					var deltaWorld = Rot90(deltaLocal, rot);
					yield return origin + deltaWorld;
				}
			}
		}

		public bool ValidateBuildingPlacement(BuildingPlacement placement)
		{
			var targetGrids = YieldBuildingOccupancy(placement).ToArray();
			foreach(var pos in targetGrids)
			{
				if(!IsValidMapPosition(pos))
					return false;
			}
			foreach(var building in buildings)
			{
				if(YieldBuildingOccupancy(building).Any(targetGrids.Contains))
					return false;
			}
			return true;
		}

		public bool IsValidMapPosition(Vector2Int pos)
		{
			return pos.x >= 0 && pos.y >= 0 && pos.x < size.x && pos.y < size.y;
		}
		#endregion
	}

	[Serializable]
	public class BuildingPlacement
	{
		public string id;  // The building ID stored in the definition sheet.
		public Vector2Int position;
		[Range(0, 3)] public int orientation;  // By 90 degrees.

		public bool underConstruction;

		public BuildingPlacement(string id, Vector2Int position, int orientation, bool underConstruction = false)
		{
			this.id = id;
			this.position = position;
			this.orientation = orientation;
			this.underConstruction = underConstruction;
		}

		public BuildingDefinition Definition
		{
			get
			{
				GameManager.FindBuildingDefinitionById(id, out var definition);
				return definition;
			}
		}
	}
}
