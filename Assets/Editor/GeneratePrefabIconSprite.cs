// Editor/GeneratePrefabIconSprite.cs
// Renders a transparent 256x256 icon for the selected prefab and imports it as a Sprite named {prefabName}-icn.png
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class GeneratePrefabIconSprite
{
	[MenuItem("Tools/Generate Prefab Icon Sprite (256x256)")]
	public static void GenerateForSelection()
	{
		var objs = Selection.objects;
		if (objs == null || objs.Length == 0)
		{
			EditorUtility.DisplayDialog("No Selection", "Select a prefab asset in the Project window.", "OK");
			return;
		}

		foreach (var obj in objs)
		{
			var path = AssetDatabase.GetAssetPath(obj);
			if (string.IsNullOrEmpty(path) || Path.GetExtension(path).ToLower() != ".prefab")
			{
				Debug.LogWarning($"Skipped: {obj.name} (not a prefab asset)");
				continue;
			}

			try
			{
				GenerateForPrefabPath(path);
			}
			catch (System.Exception ex)
			{
				Debug.LogError($"Failed to generate icon for '{path}': {ex}");
			}
		}
	}

	private static void GenerateForPrefabPath(string prefabAssetPath)
	{
		// Create a Preview Scene so we don't pollute the current scene.
		var previewScene = EditorSceneManager.NewPreviewScene();
		GameObject instance = null;
		Camera cam = null;
		Light keyLight = null;

		try
		{
			var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabAssetPath);
			if (prefab == null)
				throw new System.Exception("Could not load prefab at " + prefabAssetPath);

			// Instantiate into preview scene
			instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
			SceneManager.MoveGameObjectToScene(instance, previewScene);
			instance.transform.position = Vector3.zero;
			instance.transform.rotation = Quaternion.identity;

			// Compute render bounds
			var bounds = CalculateRenderableBounds(instance);
			if (bounds.size == Vector3.zero)
				bounds = new Bounds(Vector3.zero, Vector3.one * 0.5f); // fallback minimal bounds

			// Camera setup (orthographic for consistent scale), 3/4 angle
			cam = new GameObject("IconCam").AddComponent<Camera>();
			SceneManager.MoveGameObjectToScene(cam.gameObject, previewScene);
			cam.clearFlags = CameraClearFlags.SolidColor;
			cam.backgroundColor = new Color(0, 0, 0, 0); // transparent
			cam.orthographic = true;
			cam.nearClipPlane = 0.01f;
			cam.farClipPlane = 1000f;
			cam.allowHDR = false;
			cam.allowMSAA = false;

			// Place camera
			var size = bounds.extents;
			float halfMax = Mathf.Max(size.x, size.y, size.z);
			cam.orthographicSize = halfMax * 1.2f; // padding
			Vector3 viewDir = (new Vector3(1f, 1f, -1f)).normalized; // nice 3/4 view
			float dist = halfMax * 4f; // ensure in front
			cam.transform.position = bounds.center - viewDir * dist;
			cam.transform.rotation = Quaternion.LookRotation(viewDir, Vector3.up);

			// Simple key light to avoid flat shading
			keyLight = new GameObject("KeyLight").AddComponent<Light>();
			SceneManager.MoveGameObjectToScene(keyLight.gameObject, previewScene);
			keyLight.type = LightType.Directional;
			keyLight.intensity = 1.2f;
			keyLight.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

			// Set layer to Default so camera sees it
			SetLayerRecursive(instance, 0);
			cam.cullingMask = 1 << 0;

			// Render to RT
			const int TEX_SIZE = 256;
			var rt = new RenderTexture(TEX_SIZE, TEX_SIZE, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
			rt.antiAliasing = 1;
			var prevActive = RenderTexture.active;
			var prevTarget = cam.targetTexture;

			Texture2D tex = null;
			try
			{
				cam.targetTexture = rt;
				RenderTexture.active = rt;
				GL.Clear(true, true, new Color(0, 0, 0, 0));
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

			// Write PNG next to prefab
			var dir = Path.GetDirectoryName(prefabAssetPath).Replace("\\", "/");
			var baseName = Path.GetFileNameWithoutExtension(prefabAssetPath);
			var pngPath = $"{dir}/{baseName}-icn.png";

			var pngBytes = ImageConversion.EncodeToPNG(tex);
			File.WriteAllBytes(pngPath, pngBytes);
			Object.DestroyImmediate(tex);

			AssetDatabase.ImportAsset(pngPath, ImportAssetOptions.ForceUpdate);

			// Set importer to Sprite with transparency
			var importer = AssetImporter.GetAtPath(pngPath) as TextureImporter;
			if (importer != null)
			{
				importer.textureType = TextureImporterType.Sprite;
				importer.spriteImportMode = SpriteImportMode.Single;
				importer.alphaIsTransparency = true;
				importer.mipmapEnabled = false;
				importer.sRGBTexture = true;
				importer.filterMode = FilterMode.Bilinear;
				importer.textureCompression = TextureImporterCompression.Uncompressed; // icons look crisp
				importer.spritePixelsPerUnit = 256; // 1 unit == full icon; tweak if desired
#if UNITY_2022_1_OR_NEWER
				importer.alphaSource = TextureImporterAlphaSource.FromInput;
#endif
				EditorUtility.SetDirty(importer);
				importer.SaveAndReimport();
			}

			Debug.Log($"Generated icon sprite: {pngPath}");
		}
		finally
		{
			// Cleanup preview scene objects
			if (cam) Object.DestroyImmediate(cam.gameObject);
			if (keyLight) Object.DestroyImmediate(keyLight.gameObject);
			if (instance) Object.DestroyImmediate(instance);
			EditorSceneManager.ClosePreviewScene(previewScene);
		}
	}

	private static Bounds CalculateRenderableBounds(GameObject root)
	{
		var renderers = root.GetComponentsInChildren<Renderer>();
		Bounds b = new Bounds(root.transform.position, Vector3.zero);
		bool hasRenderer = false;

		foreach (var r in renderers)
		{
			if (!hasRenderer)
			{
				b = r.bounds;
				hasRenderer = true;
			}
			else
			{
				b.Encapsulate(r.bounds);
			}
		}

		// Fallback to colliders if no renderers
		if (!hasRenderer)
		{
			var colliders = root.GetComponentsInChildren<Collider>();
			foreach (var c in colliders)
			{
				if (!hasRenderer)
				{
					b = c.bounds;
					hasRenderer = true;
				}
				else
				{
					b.Encapsulate(c.bounds);
				}
			}
		}

		// As a final guard, ensure non-zero size
		if (!hasRenderer)
			b = new Bounds(Vector3.zero, Vector3.one * 0.5f);

		return b;
	}

	private static void SetLayerRecursive(GameObject go, int layer)
	{
		go.layer = layer;
		foreach (Transform t in go.transform)
			SetLayerRecursive(t.gameObject, layer);
	}

	[MenuItem("Tools/Generate Prefab Icon Sprite (256x256)", true)]
	private static bool ValidateGenerateForSelection()
	{
		if (Selection.objects == null || Selection.objects.Length == 0) return false;
		foreach (var obj in Selection.objects)
		{
			var path = AssetDatabase.GetAssetPath(obj);
			if (!string.IsNullOrEmpty(path) && Path.GetExtension(path).ToLower() == ".prefab")
				return true;
		}
		return false;
	}
}
