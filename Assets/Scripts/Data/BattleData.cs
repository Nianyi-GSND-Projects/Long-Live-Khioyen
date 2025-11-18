using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LongLiveKhioyen
{
	public enum BattleType
	{
		Seige,
		Defend,
		Encounter
	}
	[Serializable]
	public class BattleData
	{
		public string id;
		public string name;
		public Vector2 position;
		public BattleType battleType;

		public Vector2Int battleSize;
		[Range(0, 359)] public float encounterOrientation;
		public List<ReserveTeam> playerReserveTeams;
		public List<BattalionCompilation> PlayerBattalions;
		public List<BattalionCompilation> EnemyBattalions;
		public List<BattalionCompilation> FriendlyBattalions;
		public string playerCommanderId;

		public BattleData()
		{
			battleType = BattleType.Encounter;
			battleSize = new Vector2Int(10, 10);
			playerReserveTeams = new ();
			PlayerBattalions = new ();
			EnemyBattalions = new();
			FriendlyBattalions = new();
		}
	}
	
	public class BattleResult
	{
		public List<string> Loot;

		public void CollectLoot(BattleData battleData)
		{
			Loot.Clear();
			foreach (var BattalionCompilation in battleData.PlayerBattalions)
			foreach (var Item in BattalionCompilation.currentInventory)
			{
				Loot.Add(Item);
			}
		}
	}

	
	
	public class BattalionCompilation
	{
		public int battalionId;
		public Vector2Int position;
		
		
		public bool selected;
		public bool actionDone;
		
		public List<string> currentInventory;
		//TODO：道具堆叠的定义
		
		public BattalionDefinition battalionDefinition;
		public BattalionCommander battalionCommander;
		
		public int currentSoliders;
		public int currentMurale;
		public int currentTraining;

		public int currentMovement;

		public bool ActionEnd = false;
		
		public BattalionCompilation()
		{
			battalionId = 0;
			currentInventory = new List<string>();
		}
		
	}

	public class ReserveTeam
	{
		public BattalionDefinition battalionDefinition;
		public BattalionCommander battalionCommander;

		public int currentSoliders;
		public int currentMurale;
		public int currentTraining;
		
		public bool placed = false;
	}
	
}
