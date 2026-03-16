using System;
using System.Collections.Generic;
using System.Linq;

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
			public string terrainAddress;
			public string skyboxAddress;
		}
		public WorldData3D data3D;

		[Serializable]
		public struct WorldData2D
		{
			public float scale;
			public string mapAddress;
		}
		public WorldData2D data2D;
	}
}
