using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Etc
{
	/// <summary>
	/// Used to run Benchmarks where the camera flies over the terrain.
	/// </summary>
	public class StatDisplay : MonoBehaviour
	{
		public Text text;
		private float _deltaTime;
		
		private float _peakMs;

		private int _ticksSinceLastPeak;

		private int _currentFrame;
		private int _totalCountedFrames;
		private float _totalCpuMs;
		private float _totalFps;
		
		private float _startTime;

		private void Start()
		{
			Application.targetFrameRate = -1;
			text.text = "";
			_peakMs = float.MaxValue;
			_startTime = Time.time;
		}

		private void Update()
		{
			var activeScene = SceneManager.GetActiveScene();
			var sceneName = activeScene.name;
			_currentFrame++;
			_deltaTime += (Time.unscaledDeltaTime - _deltaTime) * 0.1f;

			_ticksSinceLastPeak++;
		
			var milliseconds = _deltaTime * 1000.0f;
			var fps = 1.0f / _deltaTime;
			var averageCpu = "0.0000ms, 0.00fps average";
			var duration = "Duration: "+(Time.time - _startTime).ToString("0.000")+"s";
			if (_currentFrame < 200)
			{
				_peakMs = 0;
			}
			else
			{
				_totalCountedFrames++;
				_totalCpuMs += milliseconds;
				_totalFps += fps;
				averageCpu = (_totalCpuMs / _totalCountedFrames).ToString("0.0000") + "ms, " +(_totalFps / _totalCountedFrames).ToString("0.00")+ "fps average";
			}
			if (milliseconds > _peakMs)
			{
				_ticksSinceLastPeak = 0;
				_peakMs = milliseconds;
			}
			if (_ticksSinceLastPeak > 60)
			{
				_peakMs = milliseconds;
			}
			
			text.text = "Scene: " + sceneName + "\nFrame: " + _currentFrame + "\n" + duration + "\n\nTiming:\n" +
			            milliseconds.ToString("0.0000") + "ms, " + fps.ToString("0.00") + "fps\n" +
			            _peakMs.ToString("0.00") + "ms peak\n" +
			            averageCpu;
		}
	}
}