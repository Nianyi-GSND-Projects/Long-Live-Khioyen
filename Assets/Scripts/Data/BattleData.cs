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
		public List<Battalion> PlayerBattalions;
		public List<Battalion> EnemyBattalions;
		public List<Battalion> FriendlyBattalions;
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
			foreach (var Battalion in battleData.PlayerBattalions)
			foreach (var Item in Battalion.inventory)
			{
				Loot.Add(Item);
			}
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
