using UnityEngine;

/// <summary>
/// Setup Wireframe rendering using Unity's GL.wireframe.
/// This was introduced because geometry shaders are not available on all platforms (especially Metal on Mac).
/// </summary>
public class WireframeRenderer : MonoBehaviour
{
    public Camera gridCam;
    public Shader shader;
    public Camera mainCamera;
    private bool _showingWireframe;

    private void Start()
    {
        gridCam.CopyFrom(mainCamera);
        gridCam.clearFlags = CameraClearFlags.Nothing;
        gridCam.depth = 0;
        gridCam.SetReplacementShader(shader, "RenderType");
    }

    public void SetShowWireframe(bool showWireFrame)
    {
        gridCam.enabled = showWireFrame;
        _showingWireframe = showWireFrame;
    }

    public void SetHideMain(bool hidden)
    {
        gridCam.enabled = hidden || _showingWireframe;
        mainCamera.enabled = !hidden;
        gridCam.clearFlags = hidden ? CameraClearFlags.SolidColor : CameraClearFlags.Nothing;
    }

    private void OnPreRender()
    {
        GL.wireframe = true;
    }

    private void OnPostRender()
    {
        GL.wireframe = false;
    }
}
