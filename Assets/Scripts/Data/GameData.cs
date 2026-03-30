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
			public Sprite mapMask;
		}
		public WorldData2D data2D;

		public struct EnvironmentParams
		{
			public float difficulty;
			public float tree;
			public float water;

			public override readonly string ToString() => $"(difficulty={difficulty}, tree={tree}, water={water})";
		}

		/// <param name="position">未被缩放过的游戏世界坐标。</param>
		public EnvironmentParams GetEnviromentParams(Vector2 position)
		{
			var mm = data2D.mapMask;
			if(!mm)
				return default;
			var mt = mm.texture;

			Vector2 pixelCoord = mm.pivot + position * mm.pixelsPerUnit;

			var rect = mm.rect;
			pixelCoord.x = Mathf.Clamp(pixelCoord.x, rect.xMin, rect.xMax);
			pixelCoord.y = Mathf.Clamp(pixelCoord.y, rect.yMin, rect.yMax);
			
			pixelCoord.x = Mathf.Clamp(pixelCoord.x, 0, mt.width - 1);
			pixelCoord.y = Mathf.Clamp(pixelCoord.y, 0, mt.height - 1);

			var color = mt.GetPixel(Mathf.RoundToInt(pixelCoord.x), Mathf.RoundToInt(pixelCoord.y));
			return new()
			{
				difficulty = color.r,
				tree = color.g,
				water = color.b,
			};
		}
	}
}
