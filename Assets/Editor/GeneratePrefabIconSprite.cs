// Editor/GeneratePrefabIconSprite.cs
// Renders a transparent 256x256 sprite icon next to each selected prefab: {PrefabName}-icn.png
using System.IO;
using UnityEditor;
using UnityEngine;

public static class GeneratePrefabIconSprite
{
	private const int TEX_SIZE = 256;
	private const int CAPTURE_LAYER = 31; // private layer index (doesn't need to be named in Tags & Layers)

	[MenuItem("Tools/Generate Prefab Icon Sprite (256x256)")]
	public static void GenerateForSelection()
	{
		var objs = Selection.objects;
		if (objs == null || objs.Length == 0)
		{
			EditorUtility.DisplayDialog("No Selection", "Select one or more prefab assets in the Project window.", "OK");
			return;
		}

		foreach (var obj in objs)
		{
			var path = AssetDatabase.GetAssetPath(obj);
			if (string.IsNullOrEmpty(path) || Path.GetExtension(path).ToLower() != ".prefab")
			{
				Debug.LogWarning($"Skipped (not a prefab asset): {obj.name}");
				continue;
			}

			try { GenerateForPrefabPath(path); }
			catch (System.Exception ex) { Debug.LogError($"Failed to generate icon for '{path}': {ex}"); }
		}
	}

	[MenuItem("Tools/Generate Prefab Icon Sprite (256x256)", true)]
	private static bool ValidateGenerateForSelection()
	{
		if (Selection.objects == null) return false;
		foreach (var obj in Selection.objects)
		{
			var path = AssetDatabase.GetAssetPath(obj);
			if (!string.IsNullOrEmpty(path) && Path.GetExtension(path).ToLower() == ".prefab")
				return true;
		}
		return false;
	}

	private static void GenerateForPrefabPath(string prefabAssetPath)
	{
		// Hidden root so we don't dirty the scene; will be destroyed immediately.
		var root = new GameObject("__IconCaptureRoot__");
		root.hideFlags = HideFlags.HideAndDontSave | HideFlags.NotEditable;

		GameObject instance = null;
		Camera cam = null;
		Light key = null;

		// Use a private layer so our camera only sees what we spawn here.
		void SetLayerRecursive(GameObject go, int layer)
		{
			go.layer = layer;
			foreach (Transform t in go.transform) SetLayerRecursive(t.gameObject, layer);
		}

		try
		{
			var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabAssetPath);
			if (!prefab) throw new System.Exception("Prefab could not be loaded.");

			instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
			if (!instance) throw new System.Exception("Prefab instantiation failed.");

			instance.transform.SetParent(root.transform, false);
			instance.transform.position = Vector3.zero;
			instance.transform.rotation = Quaternion.identity;

			SetLayerRecursive(instance, CAPTURE_LAYER);

			// Compute bounds from renderers (Skinned + Mesh, etc.)
			var bounds = CalculateBounds(instance);
			if (bounds.size.sqrMagnitude <= Mathf.Epsilon)
				bounds = new Bounds(Vector3.zero, Vector3.one * 0.5f);

			// Camera (orthographic, transparent background)
			var camGO = new GameObject("IconCam");
			camGO.hideFlags = HideFlags.HideAndDontSave | HideFlags.NotEditable;
			camGO.transform.SetParent(root.transform, false);

			cam = camGO.AddComponent<Camera>();
			cam.clearFlags = CameraClearFlags.SolidColor;
			cam.backgroundColor = new Color(0, 0, 0, 0);
			cam.orthographic = true;
			cam.nearClipPlane = 0.01f;
			cam.farClipPlane = 1000f;
			cam.allowHDR = false;
			cam.allowMSAA = false;
			cam.cullingMask = 1 << CAPTURE_LAYER;

			float halfMax = Mathf.Max(bounds.extents.x, bounds.extents.y, bounds.extents.z);
			cam.orthographicSize = halfMax * 1.2f;

			// Camera looks straight down from above
			cam.transform.position = bounds.center + Vector3.up * (halfMax * 4f);
			cam.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

			// Rotate the prefab 45° around Y for isometric style
			instance.transform.rotation = Quaternion.Euler(0f, 45f, 0f);

			// Simple directional light (shadows off to avoid artifacts)
			var lightGO = new GameObject("KeyLight");
			lightGO.hideFlags = HideFlags.HideAndDontSave | HideFlags.NotEditable;
			lightGO.transform.SetParent(root.transform, false);
			key = lightGO.AddComponent<Light>();
			key.type = LightType.Directional;
			key.intensity = 1.2f;
			key.shadows = LightShadows.None;
			key.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

			// Render
			var rt = new RenderTexture(TEX_SIZE, TEX_SIZE, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB)
			{ antiAliasing = 1 };
			var prevActive = RenderTexture.active;
			var prevTarget = cam.targetTexture;

			Texture2D tex = null;
			try
			{
				cam.targetTexture = rt;
				RenderTexture.active = rt;
				GL.Clear(true, true, new Color(0, 0, 0, 0));

				// In URP/HDRP, Camera.Render is okay for a simple offscreen pass in-editor.
				// If you still get blanks, see notes below to switch to Handles.DrawCamera.
				cam.Render();

				tex = new Texture2D(TEX_SIZE, TEX_SIZE, TextureFormat.RGBA32, false, false);
				tex.ReadPixels(new Rect(0, 0, TEX_SIZE, TEX_SIZE), 0, 0);
				tex.Apply(false, false);
			}
			finally
			{
				cam.targetTexture = prevTarget;
				RenderTexture.active = prevActive;
				rt.Release();
				Object.DestroyImmediate(rt);
			}

			// Save next to prefab
			var dir = Path.GetDirectoryName(prefabAssetPath).Replace("\\", "/");
			var baseName = Path.GetFileNameWithoutExtension(prefabAssetPath);
			var pngPath = $"{dir}/{baseName}-icn.png";
			File.WriteAllBytes(pngPath, ImageConversion.EncodeToPNG(tex));
			Object.DestroyImmediate(tex);

			AssetDatabase.ImportAsset(pngPath, ImportAssetOptions.ForceUpdate);

			// Import as Sprite
			var ti = (TextureImporter)AssetImporter.GetAtPath(pngPath);
			if (ti != null)
			{
				ti.textureType = TextureImporterType.Sprite;
				ti.spriteImportMode = SpriteImportMode.Single;
				ti.alphaIsTransparency = true;
				ti.mipmapEnabled = false;
				ti.textureCompression = TextureImporterCompression.Uncompressed;
				ti.filterMode = FilterMode.Bilinear;
				ti.sRGBTexture = true;
#if UNITY_2022_1_OR_NEWER
				ti.alphaSource = TextureImporterAlphaSource.FromInput;
#endif
				ti.spritePixelsPerUnit = TEX_SIZE; // 1 unit == full icon
				ti.SaveAndReimport();
			}

			Debug.Log($"Generated icon sprite: {pngPath}");
		}
		finally
		{
			if (root) Object.DestroyImmediate(root);
		}
	}

	private static Bounds CalculateBounds(GameObject go)
	{
		var renderers = go.GetComponentsInChildren<Renderer>(true);
		Bounds b = new Bounds(go.transform.position, Vector3.zero);
		bool has = false;
		foreach (var r in renderers)
		{
			// Skip non-visible or disabled renderers
			if (!r.enabled) continue;
			if (!has) { b = r.bounds; has = true; } else b.Encapsulate(r.bounds);
		}

		if (!has)
		{
			var colliders = go.GetComponentsInChildren<Collider>(true);
			foreach (var c in colliders)
			{
				if (!has) { b = c.bounds; has = true; } else b.Encapsulate(c.bounds);
			}
		}

		if (!has) b = new Bounds(Vector3.zero, Vector3.one * 0.5f);
		return b;
	}
}
