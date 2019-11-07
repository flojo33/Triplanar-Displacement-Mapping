using System.IO;
using UnityEngine;

namespace Etc
{
	/// <summary>
	/// Class for creating screenshots while in game or in the editor.
	/// </summary>
	public class CameraScreenCapture : MonoBehaviour
	{
		public int currentTake = 1;
		public string filename;
		public string path;

		private void Update()
		{
			if (Input.GetKeyDown(KeyCode.LeftAlt))
			{
				TakeScreenshot();
			}
		}
		
		public void TakeScreenshot()
		{
			var cameraComponent = GetComponent<Camera>();
			
			var renderTexture = new RenderTexture(cameraComponent.pixelWidth, cameraComponent.pixelHeight,32);
			cameraComponent.targetTexture = renderTexture;
			cameraComponent.Render();
			
			Texture2D tex = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGBA32, false);
			RenderTexture.active = renderTexture;
			tex.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
			tex.Apply();

			if (!Directory.Exists(path))
			{
				Directory.CreateDirectory(path);
			}
			Debug.Log(path);
			File.WriteAllBytes(
				path+"//"+filename+"_"+currentTake+".png",
				tex.EncodeToPNG());
			
			cameraComponent.targetTexture = null;
			
			currentTake++;
		}
	}
}