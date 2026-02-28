using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace LongLiveKhioyen
{
	public class PolisSelectionHighlightRenderFeature : ScriptableRendererFeature
	{
		[System.Serializable]
		public class Settings
		{
			public Material overrideMaterial;
			public int overrideMaterialPassIndex = 0;
			public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
		}

		class Pass : ScriptableRenderPass
		{
			readonly ProfilingSampler profilingSampler = new("Polis Selection Highlight");
			readonly List<Renderer> rendererCache = new();
			readonly List<Material> materialCache = new();

			Material overrideMaterial;
			int overrideMaterialPassIndex;

			public void Setup(Material material, int materialPassIndex)
			{
				overrideMaterial = material;
				overrideMaterialPassIndex = Mathf.Max(0, materialPassIndex);
			}

			public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
			{
				if(overrideMaterial == null)
					return;

				if(renderingData.cameraData.cameraType != CameraType.Game)
					return;

				var polis = Polis.Instance;
				if(polis == null || polis.Selected == null)
					return;

				// ISelectable 不是组件，先手动转成 MonoBehaviour 才能继续取渲染器。
				if(polis.Selected is not MonoBehaviour selectedBehaviour || selectedBehaviour == null)
					return;

				rendererCache.Clear();
				selectedBehaviour.GetComponentsInChildren(true, rendererCache);
				if(rendererCache.Count == 0)
					return;

				var cmd = CommandBufferPool.Get();
				using(new ProfilingScope(cmd, profilingSampler))
				{
					foreach(var renderer in rendererCache)
					{
						if(renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy)
							continue;

						renderer.GetSharedMaterials(materialCache);
						if(materialCache.Count == 0)
							continue;

						for(int subMeshIndex = 0; subMeshIndex < materialCache.Count; ++subMeshIndex)
							cmd.DrawRenderer(renderer, overrideMaterial, subMeshIndex, overrideMaterialPassIndex);
					}
				}

				context.ExecuteCommandBuffer(cmd);
				CommandBufferPool.Release(cmd);
			}
		}

		[SerializeField] Settings settings = new();
		Pass pass;

		public override void Create()
		{
			pass = new Pass
			{
				renderPassEvent = settings.renderPassEvent
			};
		}

		public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
		{
			if(settings.overrideMaterial == null)
				return;

			pass.renderPassEvent = settings.renderPassEvent;
			pass.Setup(settings.overrideMaterial, settings.overrideMaterialPassIndex);
			renderer.EnqueuePass(pass);
		}
	}
}
