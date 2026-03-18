using System;
using System.Collections.Generic;
using System.Linq;
using NaughtyAttributes;
using UnityEngine;

namespace LongLiveKhioyen
{
	[Serializable]
	public class GameData
	{
		public WorldData world;
		public string lastPolis;
		public string mainPolis;
		public List<PolisData> poleis = new();
		/// <summary>以月份为单位。</summary>
		public GameTime time;

		public PolisData GetPolis(string id)
		{
			return poleis.FirstOrDefault(p => p.id == id);
		}
	}

	[Serializable]
	public class WorldData
	{
		[Serializable]
		public struct WorldData3D
		{
			public float scale;
		}
		public WorldData3D data3D;

		[Serializable]
		public struct WorldData2D
		{
			public float scale;
			public Sprite mapImage;
		}
		public WorldData2D data2D;
	}
}
