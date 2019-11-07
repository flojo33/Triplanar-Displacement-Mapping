using Etc;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Creates a custom editor for the camera Screen Capture script.
/// </summary>
[CustomEditor(typeof(CameraScreenCapture))]
public class CameraScreenCaptureEditor : Editor 
{
	public override void OnInspectorGUI()
	{
		CameraScreenCapture cameraScreenCapture = (CameraScreenCapture)target;
		var path = cameraScreenCapture.path;
		if (!path.EndsWith("/"))
		{
			path += "/";
		}
		var nextName = path + cameraScreenCapture.filename + cameraScreenCapture.currentTake + ".png";
		GUILayout.Label("Current Take: "+cameraScreenCapture.currentTake);
		if(GUILayout.Button("Reset Take"))
		{
			cameraScreenCapture.currentTake = 1;
		}

		GUILayout.Label("Path:");
		var newPath = GUILayout.TextField(cameraScreenCapture.path).Replace("\\","/");
		while (newPath != newPath.Replace("//", "/"))
		{
			newPath = newPath.Replace("//", "/");
		}
		if (cameraScreenCapture.path != newPath)
		{
			cameraScreenCapture.currentTake = 1;
			cameraScreenCapture.path = newPath;
		}
		
		GUILayout.Label("Filename:");
		var newName = GUILayout.TextField(cameraScreenCapture.filename);
		if (cameraScreenCapture.filename != newName)
		{
			cameraScreenCapture.currentTake = 1;
			cameraScreenCapture.filename = newName;
		}
		
		GUILayout.Label("Next Filename: "+nextName);
		if(GUILayout.Button("Take screenshot"))
		{
			cameraScreenCapture.TakeScreenshot();
		}
	}
}