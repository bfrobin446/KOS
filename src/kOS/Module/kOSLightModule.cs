using kOS.Safe.Utilities;
using UnityEngine;

namespace kOS.Module
{
    internal class kOSLightModule : PartModule
    {
        [KSPField(isPersistant = true, guiName = "Required Power for Lights", guiActive = true)]
        public float resourceAmount = 0.001f;

        [KSPField(isPersistant = true, guiActive = true, guiName = "Light R")]
        [UI_FloatRange(maxValue = 1, minValue = 0, scene = UI_Scene.Flight, stepIncrement = 0.01f)]
        protected float red = 1;

        [KSPField(isPersistant = true, guiActive = true, guiName = "Light G")]
        [UI_FloatRange(maxValue = 1, minValue = 0, scene = UI_Scene.Flight, stepIncrement = 0.01f)]
        protected float green = 1;

        [KSPField(isPersistant = true, guiActive = true, guiName = "Light B")]
        [UI_FloatRange(maxValue = 1, minValue = 0, scene = UI_Scene.Flight, stepIncrement = 0.01f)]
        protected float blue = 1;

        [KSPField(isPersistant = true, guiActive = true, guiName = "Power Starved")]
        private bool powerStarved = false;

        [KSPField(isPersistant = false, guiName = "Last requested power", guiActive = true)]
        public float lastResource = 0.2f;

        [KSPField(isPersistant = false, guiActive = false)]
        public string animationName = "";
        
        [KSPField(isPersistant = false, guiActive = false)]
        public string pulseWidth = "";

        [KSPField(isPersistant = false, guiActive = false)]
        public string gapWidth = "";

        private double lastTime = double.MaxValue;

        private readonly Color powerOffColor = new Color(0, 0, 0, 0);

        private ModuleLight lightModule;
        private Light[] lights;
        private Renderer[] renderers;
        private Animation[] animations;
        private bool lastLightModuleIsOn = false;
        
        public override void OnLoad(ConfigNode node)
        {
            updateReferences();
        }

        public void Update()
        {
            if (HighLogic.LoadedScene == GameScenes.EDITOR)
            {
                if (lightModule != null)
                {
                    red = lightModule.lightR;
                    green = lightModule.lightG;
                    blue = lightModule.lightB;
                }
                else
                {
                    updateReferences();
                }
            }
            updateColor();
        }

        public void FixedUpdate()
        {
            if (HighLogic.LoadedScene == GameScenes.FLIGHT)
            {
                if (lightModule != null && lightModule.isOn)
                {
                    processElectricity();
                }
                else
                {
                    lastTime = double.MaxValue;
                }
            }
        }

        public override string GetInfo()
        {
            string format =
                "Configurable color.\n" +
                "Maximum power consumption: {0}EC/s";
            return string.Format(format, resourceAmount);
        }

        public void updateReferences()
        {
            lightModule = part.GetComponent<ModuleLight>();
            lightModule.resourceAmount = 0;
            lightModule.Fields["resourceAmount"].guiActive = false;
            lightModule.useResources = false;
            lights = part.FindModelComponents<Light>();
            renderers = part.FindModelComponents<Renderer>();
            animations = part.FindModelAnimators();
        }

        private void processElectricity()
        {
            double currentTime = Planetarium.GetUniversalTime();
            double dt = currentTime - lastTime;
            if (dt > 0)
            {
                double request = (red + green + blue) / 3 * resourceAmount * dt;
                lastResource = (float)request;
                double received = part.RequestResource("ElectricCharge", request);
                if (received / request > 0.5)
                {
                    powerStarved = false;
                }
                else
                {
                    powerStarved = true;
                }
            }
            lastTime = currentTime;
        }

        private void updateColor()
        {
            if (lightModule != null)
            {
                Color currentColor = powerStarved || !lightModule.isOn ? powerOffColor : new Color(red, green, blue, 1);
                if (lights != null)
                {
                    foreach (Light lgt in lights)
                    {
                        lgt.color = currentColor;
                    }
                }
                if (renderers != null)
                {
                    foreach (Renderer rnd in renderers)
                    {
                        rnd.material.SetColor("_EmissiveColor", currentColor);
                    }
                }
                if (animations != null && !string.IsNullOrEmpty(animationName))
                {
                    foreach (var animation in animations)
                    {
                        if (!animation.isPlaying && (lightModule.isOn & !lastLightModuleIsOn) && !powerStarved) // <<-- extra check
                        {
                            animation.Play(animationName);
                        }
                        else if (animation.isPlaying && (!lightModule.isOn || powerStarved))
                        {
                            animation.Stop();
                        }
                    }
                    lastLightModuleIsOn = lightModule.isOn;
                }
            }
        }
    }
}
