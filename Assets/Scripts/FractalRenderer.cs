using System.Collections.Generic;
using System.Data.SqlTypes;
using System.IO;
using UnityEngine;

namespace Fractal
{
	[RequireComponent(typeof(Camera))]
	public class FractalRenderer : MonoBehaviour
	{
		[SerializeField] bool showGUI = true;

		[Header("Rendering")]
		[SerializeField] ComputeShader fractalShader;
		[SerializeField] RenderTexture renderTexture;
		[SerializeField] int xDim = 1920;
		[SerializeField] int yDim = 1080;
		[SerializeField][Tooltip("The depth of the render texture (24 recommended)")] int zDim = 24;

		[Space]

		[Header("Movement")]
		[SerializeField] double zoom = 5f;
		[SerializeField] double zoomSpeed = 0.05f;
		/* This corresponds to the virtual zoom data (the one passed to the compute shader) */
		ZoomData[] zoomData = new ZoomData[1] { new ZoomData(5) };
		Vect2 prevMousePos;

		[Space]

		[Header("Fractal Generation")]
		[SerializeField] float Cr = -1f;
		[SerializeField] float Ci = 0f;
		[SerializeField] int maxIter = 20;

		[SerializeField] FractalType fractalType;

		[Space]

		[Header("Fractal Apperance")]
		[SerializeField] ColorProfile profile;
		int colorProfileSelection = 0;
		string[] colorProfileFiles;
		[SerializeField] TextAsset[] colorProfiles;

		[Space]

		[Header("Editor")]
		public bool hotReload = false;

		private void Start()
		{
			showGUI = true;

			CreateRenderTexture();
			LoadColorProfiles();
		}

		void LoadColorProfiles() 
		{
			List<string> colorProfileFiles_ = new List<string>();

			foreach(TextAsset colorProfile in colorProfiles) 	
			{
				colorProfileFiles_.Add(colorProfile.name);
			}

			colorProfileFiles = colorProfileFiles_.ToArray();
		}

		/* Create a new render texture */
		public void CreateRenderTexture()
		{
			renderTexture = new RenderTexture(xDim, yDim, zDim);
			renderTexture.enableRandomWrite = true;
			renderTexture.Create();
		}

		/* Show the fractal at the screen */
		private void OnRenderImage(RenderTexture src, RenderTexture dest)
		{
			GenerateFractal(dest);
		}

		/* Sends data to the compute shader and then show the render texture result at the screen */
		void GenerateFractal(RenderTexture dest)
		{
			// Generates a render texture if needed
			if (renderTexture == null)
				CreateRenderTexture();

			// Creates the zoom compute Buffer
			ComputeBuffer zoomBuffer = new ComputeBuffer(zoomData.Length, sizeof(double) * 3);
			zoomBuffer.SetData(zoomData);

			// Fill the compute shader with data
			fractalShader.SetBuffer((int)fractalType, "posData", zoomBuffer);
			fractalShader.SetTexture((int)fractalType, "Result", renderTexture);
			fractalShader.SetFloat("xRes", renderTexture.width);
			fractalShader.SetFloat("yRes", renderTexture.height);
			fractalShader.SetFloat("Cr", Cr);
			fractalShader.SetFloat("Ci", Ci);
			fractalShader.SetInt("maxIter", maxIter);

			fractalShader.SetFloats("rChannel", new float[4] { profile.rChannel.x, profile.rChannel.y, profile.rChannel.z, profile.rChannel.w });
			fractalShader.SetFloats("gChannel", new float[4] { profile.gChannel.x, profile.gChannel.y, profile.gChannel.z, profile.gChannel.w });
			fractalShader.SetFloats("bChannel", new float[4] { profile.bChannel.x, profile.bChannel.y, profile.bChannel.z, profile.bChannel.w });

			// Dispatch it
			fractalShader.Dispatch((int)fractalType, renderTexture.width / 8, renderTexture.height / 8, 1);

			// Show the result
			Graphics.Blit(renderTexture, dest);
		}

		private void Update()
		{
			/* Calculate Movement */
			Vect2 currentMousePos = GetMouseWorldPos();

			if (Input.GetKey(KeyCode.Mouse1))
			{
				zoomData[0].posX -= currentMousePos.x - prevMousePos.x;
				zoomData[0].posY -= fractalType != FractalType.BurningShip ? currentMousePos.y - prevMousePos.y : -currentMousePos.y + prevMousePos.y;
			}

			/* Calculates zoom */
			float mouseDelta = Input.GetAxis("Mouse ScrollWheel");

			if (mouseDelta > 0)
			{
				zoom *= 1 - zoomSpeed;
			}
			else if (mouseDelta < 0)
			{
				zoom *= 1 + zoomSpeed;
			}

			/* Checks for zoom reset */
			if (Input.GetKeyDown(KeyCode.Mouse2))
			{
				zoom = 5;
				zoomData[0].posX = 0;
				zoomData[0].posY = 0;
			}

			zoomData[0].zoom = zoom;

			prevMousePos = GetMouseWorldPos();

			if (Input.GetKeyDown(KeyCode.F1))
			{
				showGUI = !showGUI;
			}
		}

		/* Get the mouse position in the world */
		Vect2 GetMouseWorldPos()
		{
			Vector2 screenPos = (Vector2)Input.mousePosition; // Position relative to the screen
			Vector2 worldPos = new Vector2(screenPos.x - xDim / 2, screenPos.y - yDim / 2); // Position relative to unzoomed world
			Vect2 worldZoomedPos = new Vect2(worldPos.x * zoom / xDim + zoomData[0].posX, worldPos.y * zoom / yDim + zoomData[0].posY); // Position relative to world + zoom

			return worldZoomedPos;
		}

		private void OnGUI()
		{
			if (!showGUI) return;

			GUI.Box(new Rect(0, 0, 200, 600), "");

			GUILayout.Label("Simulation Data (F1 to Hide)");

			GUILayout.Space(10);

			GUILayout.BeginHorizontal();
			GUILayout.Label("Cr");
			Cr = GUILayout.HorizontalSlider(Cr, -5, 5);
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			GUILayout.Label("Ci");
			Ci = GUILayout.HorizontalSlider(Ci, -5, 5);
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			GUILayout.Label("Iterations");
			maxIter = System.Convert.ToInt32(GUILayout.TextField(maxIter.ToString()));
			GUILayout.EndHorizontal();

			GUILayout.Label($"PosX : {zoomData[0].posX}");
			GUILayout.Label($"PosY : {zoomData[0].posY}");
			GUILayout.Label($"Zoom : {zoomData[0].zoom}");

			GUILayout.Space(10);

			GUILayout.Label("Simulation Type");

			GUILayout.Space(10);

			if (GUILayout.Button("Julia"))
			{
				fractalType = FractalType.Julia;
			}

			if (GUILayout.Button("Mandelbrot"))
			{
				fractalType = FractalType.Mandelbrot;
			}

			if (GUILayout.Button("Burning Ship"))
			{
				fractalType = FractalType.BurningShip;
			}

			GUILayout.Space(10);

			GUILayout.Label("Color Profile");

			GUILayout.Space(10);

			colorProfileSelection = GUILayout.SelectionGrid(colorProfileSelection, colorProfileFiles, 1);

			GUILayout.Space(10);

			if (GUILayout.Button("Apply Color")) 
			{
				LoadColorProfile(colorProfileSelection);
			}
		}

		public void SaveColorProfile() 
		{
			string jsonColorProfile = JsonUtility.ToJson(profile);

			using (StreamWriter writer = new StreamWriter(Path.Combine(Application.persistentDataPath, "Profiles", "profile.json")))
			{
				writer.Write(jsonColorProfile);
			}
		}

		public void LoadColorProfile(int index) 
		{
			profile = JsonUtility.FromJson<ColorProfile>(colorProfiles[index].text);
		}

		/* Represents a color in the compute shader */
		[System.Serializable]
		public struct FractalColor
		{
			float r;
			float g;
			float b;

			public FractalColor(Color color)
			{
				r = color.r;
				g = color.g;
				b = color.b;
			}
		}

		[System.Serializable]
		public struct ZoomData
		{
			public double zoom;
			public double posX;
			public double posY;

			public ZoomData(double zoom)
			{
				this.zoom = zoom;
				posX = 0;
				posY = 0;
			}
		}

		[System.Serializable]
		public struct Vect2
		{
			public double x;
			public double y;

			public Vect2(double x, double y)
			{
				this.x = x;
				this.y = y;
			}
		}

		[System.Serializable]
		public struct ColorProfile 
		{
			public Vector4 rChannel;
			public Vector4 gChannel;
			public Vector4 bChannel;
		}

		public enum FractalType
		{
			Julia = 0,
			Mandelbrot,
			BurningShip
		}
	}
}
