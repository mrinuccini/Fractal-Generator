using UnityEditor;
using UnityEngine;

namespace Fractal
{
	[CustomEditor(typeof(FractalRenderer))]
	public class FractalRendererEditor : Editor
	{
		public override void OnInspectorGUI()
		{
			if (DrawDefaultInspector())
			{
				if (((FractalRenderer)target).hotReload)
				{
					((FractalRenderer)target).CreateRenderTexture();
				}
			}

			if (GUILayout.Button("Reload Fractal"))
			{
				((FractalRenderer)target).CreateRenderTexture();
			}

			if (GUILayout.Button("Save Color Profile")) 
			{
				((FractalRenderer)target).SaveColorProfile();
			}
		}
	}
}
