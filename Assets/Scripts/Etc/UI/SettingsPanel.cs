using System;
using Terrain;
using UnityEngine;
using UnityEngine.UI;

namespace Etc.UI
{
    /// <summary>
    /// Create a Panel with multiple Slider, Boolean and Button inputs to adjust the shader preferences at play time.
    /// </summary>
    public class SettingsPanel : MonoBehaviour
    {
        public GameObject booleanPrefab, sliderPrefab, dividerPrefab, buttonPrefab;
        public Transform containerView;
        public WireframeRenderer wireframeRenderer;

        public Camera mainCamera, wireframeCamera;
        
        public Text fpsText;
        public Sun sun;

        public TerrainController terrainController;

        private SliderInput _sunSlider;
        private int _frame;
    
        // Start is called before the first frame update
        private void Start()
        {
            CreateDividerInput("Debug");
            CreateBooleanInput((toggle) => { fpsText.enabled = toggle;}, "Display Stats").SetValue(true);
            CreateBooleanInput((toggle) => { terrainController.ToggleKeyword("DISPLAY_SPLAT_MAP",toggle);}, "Display Splat Map").SetValue(false);
            CreateBooleanInput((toggle) => { terrainController.ToggleKeyword("DISPLAY_NORMAL",toggle);}, "Display Normals").SetValue(false);
            CreateBooleanInput((toggle) => { terrainController.ToggleKeyword("DISPLAY_SAMPLE_COUNT",toggle);}, "Display Sample Count").SetValue(false);
            CreateBooleanInput((toggle) => { terrainController.ToggleKeyword("FLAT_SHADING",toggle);}, "Flat Shading").SetValue(false);
            CreateBooleanInput((toggle) => { terrainController.ToggleKeyword("SHADE_WHITE",toggle);}, "Hide Color").SetValue(false);
            CreateBooleanInput((toggle) => { wireframeRenderer.SetShowWireframe(toggle);}, "Show Wireframe").SetValue(false);
            CreateBooleanInput((toggle) => { wireframeRenderer.SetHideMain(toggle);}, "Hide Mesh").SetValue(false);
            CreateDividerInput("Shader Features");
            CreateBooleanInput((toggle) => { terrainController.ToggleKeyword("ENABLE_SPLAT",toggle);}, "Splats").SetValue(true);
            CreateBooleanInput((toggle) => { terrainController.ToggleKeyword("SINGLE_BLEND",toggle);}, "Single Blend").SetValue(terrainController.singleBlend);
            CreateBooleanInput((toggle) => { terrainController.ToggleKeyword("DETAIL_NORMAL",toggle);}, "Detail Normals").SetValue(true);
            CreateDividerInput("Displacement");
            CreateSliderInput((value) => { terrainController.SetFloatValue("_DisplacementDistance", value);}, "Max Displacement Distance",0,30).SetValue(terrainController.displacementDistance);
            CreateSliderInput((value) => { terrainController.SetFloatValue("_DisplacementStrength", value);}, "Displacement Strength",0,1).SetValue(terrainController.displacementStrength);
            CreateDividerInput("Blending");
            CreateSliderInput((value) => { terrainController.SetFloatValue("_BlendOffset", value);}, "Blend Offset",0,0.576f).SetValue(terrainController.blendOffset);
            CreateSliderInput((value) => { terrainController.SetFloatValue("_BlendExponent", value);}, "Blend Exponent",1,16).SetValue(terrainController.blendExponent);
            CreateSliderInput((value) => { terrainController.SetFloatValue("_SplatHeightBlendStrength", value);}, "Splat Height Blend",0,1).SetValue(terrainController.splatHeightBlendStrength);
            CreateSliderInput((value) => { terrainController.SetFloatValue("_TriplanarHeightBlendStrength", value); },"Triplanar Height Blend", 0, 1).SetValue(terrainController.triplanarHeightBlendStrength);
            CreateDividerInput("Tessellation");
            CreateSliderInput((value) => { terrainController.SetFloatValue("_TessellationEdgeLength", value);}, "Max Edge Length",0,3).SetValue(terrainController.tessellationEdgeLength);
            CreateSliderInput((value) => { terrainController.SetFloatValue("_TessellationFalloffStartDistance", value);}, "Falloff Start Distance",0,300).SetValue(terrainController.tessellationFalloffStartDistance);
            CreateSliderInput((value) => { terrainController.SetFloatValue("_TessellationFalloffDistance", value);}, "Falloff Distance",0,300).SetValue(terrainController.tessellationFalloffDistance);
            CreateDividerInput("Lighting");
            CreateBooleanInput(
                (toggle) => { mainCamera.renderingPath = toggle ? RenderingPath.DeferredShading : RenderingPath.Forward;  wireframeCamera.renderingPath = toggle ? RenderingPath.DeferredShading : RenderingPath.Forward;}, "Deferred").SetValue(true);
            CreateBooleanInput((toggle) => { sun.SetPlaying(toggle);}, "Sun Movement").SetValue(false);
            _sunSlider = CreateSliderInput((value) => {sun.SetPosition(value);  }, "Sun Position", 0, 1);
            _sunSlider.SetValue(0.35f);
            CreateButtonInput(() => { sun.SetDusk(); }, "Sun Set Dusk");
            CreateButtonInput(() => { sun.SetDay(); }, "Sun Set Day");
        }

        private void Update()
        {
            _sunSlider.SetValue(sun.GetPosition());
        }

        // ReSharper disable once UnusedMethodReturnValue.Local Could be used to edit the Title later on...
        private DividerInput CreateDividerInput(string dividerName)
        {
            return Instantiate(dividerPrefab, Vector3.zero, Quaternion.identity, containerView).GetComponent<DividerInput>().Setup(dividerName);
        }
        private BooleanInput CreateBooleanInput(Action<bool> toggleAction, string toggleName)
        {
            return Instantiate(booleanPrefab, Vector3.zero, Quaternion.identity, containerView).GetComponent<BooleanInput>().Setup(toggleAction, toggleName);
        }
        // ReSharper disable once UnusedMethodReturnValue.Local Could be used to edit or disable the button later on...
        private ButtonInput CreateButtonInput(Action buttonAction, string buttonName)
        {
            return Instantiate(buttonPrefab, Vector3.zero, Quaternion.identity, containerView).GetComponent<ButtonInput>().Setup(buttonAction, buttonName);
        }
        private SliderInput CreateSliderInput(Action<float> changeAction, string sliderName, float min, float max)
        {
            return Instantiate(sliderPrefab, Vector3.zero, Quaternion.identity, containerView).GetComponent<SliderInput>().Setup(changeAction, sliderName, min, max);
        }
    }
}
