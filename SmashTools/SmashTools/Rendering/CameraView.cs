using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using HarmonyLib;
using Verse;
using RimWorld;

namespace SmashTools
{
	[StaticConstructorOnStartup]
	public static class CameraView
	{
		private const float DefaultCameraSize = 24;
		private const float PageKeyZoomRate = 4;
		private const float ZoomScaleFromAltDenominator = 35;
		private const float CameraViewerZoomRate = 0.55f;

		private static MethodInfo GetSunShadowsViewRect_MethodInfo;

		private static Camera camera;
		private static RenderTexture renderTexture;
		private static float orthographicSize;
		private static CameraMapConfig cameraConfig = new CameraMapConfig_Normal();

		private static int lastViewRectGetFrame = -1;
		private static CellRect lastViewRect;
		private static float desiredSize = 24f;

		private static Vector3 rootPos;
		private static float rootSize;

		public static Vector3 RootPos { get => rootPos; set => rootPos = value; }

		public static float OrthographicSize { get => orthographicSize; set => orthographicSize = value; }

		public static bool InUse { get; private set; }
		private static bool Patched { get; set; }
		private static bool LockedToMainCamera { get; set; } = false;

		public static CellRect CurrentViewRect
		{
			get
			{
				if (Time.frameCount != lastViewRectGetFrame)
				{
					lastViewRect = default;
					float num = UI.screenWidth / (float)UI.screenHeight;
					Vector3 currentRealPosition = camera.transform.position;
					lastViewRect.minX = Mathf.FloorToInt(currentRealPosition.x - rootSize * num - 1f);
					lastViewRect.maxX = Mathf.CeilToInt(currentRealPosition.x + rootSize * num);
					lastViewRect.minZ = Mathf.FloorToInt(currentRealPosition.z - rootSize - 1f);
					lastViewRect.maxZ = Mathf.CeilToInt(currentRealPosition.z + rootSize);
					lastViewRectGetFrame = Time.frameCount;
				}
				return lastViewRect;
			}
		}

		public static CellRect VisibleSections(Map map, CellRect viewRect)
		{
			CellRect sunShadowsViewRect = (CellRect)GetSunShadowsViewRect_MethodInfo.Invoke(map.mapDrawer, new object[] { viewRect });
			sunShadowsViewRect.ClipInsideMap(map);
			IntVec2 intVec = SectionCoordsAt(sunShadowsViewRect.BottomLeft);
			IntVec2 intVec2 = SectionCoordsAt(sunShadowsViewRect.TopRight);
			if (intVec2.x < intVec.x || intVec2.z < intVec.z)
			{
				return CellRect.Empty;
			}
			return CellRect.FromLimits(intVec.x, intVec.z, intVec2.x, intVec2.z);
		}

		private static IntVec2 SectionCoordsAt(IntVec3 loc)
		{
			return new IntVec2(Mathf.FloorToInt((loc.x / 17)), Mathf.FloorToInt((loc.z / 17)));
		}

		public static void HandleZoom()
		{
			float zoom = 0;
			if (Event.current.type == EventType.ScrollWheel)
			{
				zoom -= Event.current.delta.y * CameraViewerZoomRate;
			}
			if (KeyBindingDefOf.MapZoom_In.KeyDownEvent)
			{
				zoom += PageKeyZoomRate;
			}
			if (KeyBindingDefOf.MapZoom_Out.KeyDownEvent)
			{
				zoom -= PageKeyZoomRate;
			}
			desiredSize -= zoom * cameraConfig.zoomSpeed * rootSize / ZoomScaleFromAltDenominator;
			desiredSize = Mathf.Clamp(desiredSize, cameraConfig.sizeRange.min, cameraConfig.sizeRange.max);
		}

		public static void Update(Vector3 position)
		{
			if (InUse)
			{
				rootPos = new Vector3(position.x, rootPos.y, position.z);
				rootPos.y = 15f + (rootSize - cameraConfig.sizeRange.min) / (cameraConfig.sizeRange.max - cameraConfig.sizeRange.min) * 50f;

				camera.orthographicSize = orthographicSize;
				camera.transform.position = rootPos;

				if (LockedToMainCamera)
				{
					Find.Camera.transform.position = camera.transform.position;
					Find.Camera.orthographicSize = orthographicSize;
				}
			}
		}
		
		public static bool RenderAt(Rect rect)
		{
			if (!InUse)
			{
				return false;
			}
			try
			{
				GUI.DrawTexture(rect, renderTexture);
			}
			catch (Exception ex)
			{
				Log.ErrorOnce($"Exception thrown in CameraView. Exception = {ex}", "CameraView_RT".GetHashCode());
				return false;
			}
			return true;
		}

		public static void DrawMapGridInView()
		{
			foreach (IntVec3 cell in CurrentViewRect)
			{
				GenDraw.DrawLineBetween(cell.ToVector3(), cell.ToVector3() + new Vector3(1, 0, 0));
				GenDraw.DrawLineBetween(cell.ToVector3(), cell.ToVector3() + new Vector3(0, 0, 1));
			}
		}

		public static void Close()
		{
			InUse = false;
			if (camera && camera.gameObject)
			{
				GameObject.Destroy(renderTexture);
				GameObject.Destroy(camera.gameObject);

				camera = null;
				renderTexture = null;
			}
		}

		public static void ResetSize()
		{
			desiredSize = DefaultCameraSize;
			rootSize = desiredSize;
		}

		/// <param name="zoom">Defaults to max zoom on PC</param>
		public static void Start(float orthographicSize = 11, CameraMapConfig cameraConfig = null)
		{
			Start(new IntVec2(512, 512), orthographicSize: orthographicSize, cameraConfig: cameraConfig);
		}

		public static void Start(IntVec2 size, float orthographicSize = 11, RenderTextureFormat renderTextureFormat = RenderTextureFormat.ARGBFloat, CameraMapConfig cameraConfig = null)
		{
			InUse = true;
			try
			{
				if (cameraConfig == null)
				{
					cameraConfig = (CameraMapConfig)Activator.CreateInstance(typeof(CameraMapConfig_Normal));
				}
				CameraView.cameraConfig = cameraConfig;
				CameraView.camera = CreateCamera(orthographicSize);
				CameraView.renderTexture = CreateRenderTexture(size, renderTextureFormat);
				CameraView.camera.targetTexture = renderTexture;

				CameraView.camera.transform.position = Find.Camera.transform.position;
				CameraView.camera.transform.rotation = Find.Camera.transform.rotation;

				ResetSize();
				PatchOcclusionCulling();
			}
			catch (Exception ex)
			{
				Close();
				throw ex;
			}
		}

		internal static Camera CreateCamera(float orthographicSize)
		{
			GameObject cameraObject = new GameObject("CameraView_GameObject");
			cameraObject.SetActive(true);

			CameraView.orthographicSize = orthographicSize;

			Camera camera = cameraObject.AddComponent<Camera>();
			camera.orthographic = true;
			camera.cameraType = CameraType.Game;
			camera.orthographicSize = orthographicSize;
			return camera;
		}

		internal static RenderTexture CreateRenderTexture(IntVec2 size, RenderTextureFormat renderTextureFormat)
		{
			RenderTexture renderTexture = new RenderTexture(size.x, size.z, 16, SupportedFormat(renderTextureFormat));
			renderTexture.name = "CameraView_RT";
			renderTexture.Create();
			return renderTexture;
		}

		private static RenderTextureFormat SupportedFormat(RenderTextureFormat renderTextureFormat)
		{
			if (SystemInfo.SupportsRenderTextureFormat(renderTextureFormat))
			{
				return renderTextureFormat;
			}
			if (renderTextureFormat == RenderTextureFormat.R8 && SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.RG16))
			{
				return RenderTextureFormat.RG16;
			}
			if ((renderTextureFormat == RenderTextureFormat.R8 || renderTextureFormat == RenderTextureFormat.RG16) && SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGB32))
			{
				return RenderTextureFormat.ARGB32;
			}
			if ((renderTextureFormat == RenderTextureFormat.R8 || renderTextureFormat == RenderTextureFormat.RHalf || renderTextureFormat == RenderTextureFormat.RFloat) && SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.RGFloat))
			{
				return RenderTextureFormat.RGFloat;
			}
			if ((renderTextureFormat == RenderTextureFormat.R8 || renderTextureFormat == RenderTextureFormat.RHalf || renderTextureFormat == RenderTextureFormat.RFloat || renderTextureFormat == RenderTextureFormat.RGFloat) && SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBFloat))
			{
				return RenderTextureFormat.ARGBFloat;
			}
			return renderTextureFormat;
		}

		private static void PatchOcclusionCulling()
		{
			if (!Patched)
			{
				Patched = true;
				try
				{
					GetSunShadowsViewRect_MethodInfo = AccessTools.Method(typeof(MapDrawer), "GetSunShadowsViewRect");
					if (GetSunShadowsViewRect_MethodInfo == null)
					{
						throw new NullReferenceException("MethodInfo fields");
					}
					Messages.Message("Patching CameraView to exclude from occlusion culling.", MessageTypeDefOf.NeutralEvent);
					ProjectSetup.Harmony.Patch(original: AccessTools.Method(typeof(DynamicDrawManager), nameof(DynamicDrawManager.DrawDynamicThings)),
						transpiler: new HarmonyMethod(typeof(CameraView),
						nameof(CameraView.CameraViewRenderInRectTranspiler_Thing_Runtime)));
					ProjectSetup.Harmony.Patch(original: AccessTools.Method(typeof(CellRenderer), nameof(CellRenderer.RenderSpot), parameters: new Type[] { typeof(Vector3), typeof(Material), typeof(float) }),
						transpiler: new HarmonyMethod(typeof(CameraView),
						nameof(CameraView.CameraViewRenderInRectTranspiler_TerrainVector3_Runtime)));
					ProjectSetup.Harmony.Patch(original: AccessTools.Method(typeof(CellRenderer), nameof(CellRenderer.RenderCell), parameters: new Type[] { typeof(IntVec3), typeof(Material) }),
						transpiler: new HarmonyMethod(typeof(CameraView),
						nameof(CameraView.CameraViewRenderInRectTranspiler_TerrainIntVec3_Runtime)));
					ProjectSetup.Harmony.Patch(original: AccessTools.Method(typeof(MapDrawer), nameof(MapDrawer.DrawMapMesh)),
						postfix: new HarmonyMethod(typeof(CameraView),
						nameof(CameraView.CameraViewRenderInRect_MapMesh_Runtime)));
					//-- TODO --
					//GenView.ShouldSpawnMotesAt
				}
				catch (Exception ex)
				{
					LockedToMainCamera = true;
					Log.Error($"Failed to patch CameraView rect for occlusion culling. Animations will not work outside main camera rect, locking main camera to viewer.\nException={ex}");
				}
			}
		}

		private static IEnumerable<CodeInstruction> CameraViewRenderInRectTranspiler_Thing_Runtime(IEnumerable<CodeInstruction> instructions)
		{
			List<CodeInstruction> instructionsList = instructions.ToList();

			for (int i = 0; i < instructionsList.Count; i++)
			{
				CodeInstruction instruction = instructionsList[i];

				if (instruction.Calls(AccessTools.Method(typeof(CellRect), nameof(CellRect.Contains))))
				{
					yield return new CodeInstruction(opcode: OpCodes.Pop); //position
					yield return new CodeInstruction(opcode: OpCodes.Pop); //CellRect address
					yield return new CodeInstruction(opcode: OpCodes.Ldloc_1); //CellRect
					yield return new CodeInstruction(opcode: OpCodes.Ldloc_S, operand: 5); //thing
					yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Method(typeof(CameraView), nameof(CameraOrCameraViewContainPosition_Thing)));
					instruction = instructionsList[++i]; //CALL : CellRect.Contains
				}

				yield return instruction;
			}
		}

		private static IEnumerable<CodeInstruction> CameraViewRenderInRectTranspiler_TerrainVector3_Runtime(IEnumerable<CodeInstruction> instructions)
		{
			List<CodeInstruction> instructionsList = instructions.ToList();

			for (int i = 0; i < instructionsList.Count; i++)
			{
				CodeInstruction instruction = instructionsList[i];

				if (instruction.Calls(AccessTools.Method(typeof(CellRect), nameof(CellRect.Contains))))
				{
					yield return new CodeInstruction(opcode: OpCodes.Pop); //position
					yield return new CodeInstruction(opcode: OpCodes.Pop); //CellRect address
					yield return new CodeInstruction(opcode: OpCodes.Ldsfld, AccessTools.Field(typeof(CellRenderer), "viewRect")); //CellRect
					yield return new CodeInstruction(opcode: OpCodes.Ldarg_0); //loc
					yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Method(typeof(CameraView), nameof(CameraOrCameraViewContainPosition_Vector3)));
					instruction = instructionsList[++i]; //CALL : CellRect.Contains
				}

				yield return instruction;
			}
		}

		private static IEnumerable<CodeInstruction> CameraViewRenderInRectTranspiler_TerrainIntVec3_Runtime(IEnumerable<CodeInstruction> instructions)
		{
			List<CodeInstruction> instructionsList = instructions.ToList();

			for (int i = 0; i < instructionsList.Count; i++)
			{
				CodeInstruction instruction = instructionsList[i];

				if (instruction.Calls(AccessTools.Method(typeof(CellRect), nameof(CellRect.Contains))))
				{
					yield return new CodeInstruction(opcode: OpCodes.Pop); //viewRect
					yield return new CodeInstruction(opcode: OpCodes.Pop); //cell
					yield return new CodeInstruction(opcode: OpCodes.Ldsfld, AccessTools.Field(typeof(CellRenderer), "viewRect")); //CellRect
					yield return new CodeInstruction(opcode: OpCodes.Ldarg_0); //cell
					yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Method(typeof(CameraView), nameof(CameraOrCameraViewContainPosition_IntVec3)));
					instruction = instructionsList[++i]; //CALL : CellRect.Contains
				}

				yield return instruction;
			}
		}

		private static void CameraViewRenderInRect_MapMesh_Runtime(MapDrawer __instance, Map ___map, Section[,] ___sections)
		{
			if (InUse)
			{
				CellRect currentViewRect = VisibleSections(___map, CurrentViewRect);
				CellRect visibleSections = VisibleSections(___map, Find.CameraDriver.CurrentViewRect);
				foreach (IntVec3 intVec in currentViewRect.CellsNoOverlap(visibleSections))
				{
					Section section = ___sections[intVec.x, intVec.z];
					section.DrawSection(false);
				}
			}
		}

		private static bool CameraOrCameraViewContainPosition_Thing(CellRect cellRect, Thing thing)
		{
			if (InUse)
			{
				return cellRect.Contains(thing.Position) || CurrentViewRect.Contains(thing.DrawPos.ToIntVec3());
			}
			return cellRect.Contains(thing.Position);
		}

		private static bool CameraOrCameraViewContainPosition_IntVec3(CellRect cellRect, IntVec3 cell)
		{
			return CameraOrCameraViewContainPosition_Vector3(cellRect, cell.ToVector3ShiftedWithAltitude(AltitudeLayer.MetaOverlays));
		}

		private static bool CameraOrCameraViewContainPosition_Vector3(CellRect cellRect, Vector3 loc)
		{
			IntVec3 cell = loc.ToIntVec3();
			if (InUse)
			{
				return cellRect.Contains(cell) || CurrentViewRect.Contains(cell);
			}
			return cellRect.Contains(cell);
		}
	}
}