﻿using System;
using System.Collections.Generic;
using UnityEngine;

namespace SSTUTools.Module
{
    class SSTUCustomRadialDecoupler : PartModule, IPartMassModifier, IPartCostModifier
    {

        [KSPField]
        public float heightIncrement = 1f;

        [KSPField]
        public float diameterIncrement = 0.625f;
        
        //this is used to determine actual resultant scale from input for radius
        //should match the model default scale geometry being used...
        [KSPField]
        public float modelDiameter = 2.5f;

        [KSPField]
        public float surfaceNodeX = -0.1f;

        /// <summary>
        /// The volume of resources that the part contains at its default model scale (e.g. modelRadius listed above)
        /// </summary>
        [KSPField]
        public float resourceVolume = 0.125f;

        /// <summary>
        /// The thrust of the engine module at default model scale
        /// </summary>
        [KSPField]
        public float engineThrust = 600f;

        /// <summary>
        /// Should thrust scale on square, cube, or some other power?  Default is cubic to match the fuel quantity
        /// </summary>
        [KSPField]
        public float thrustScalePower = 2;

        [KSPField]
        public float minHeight = 0.5f;

        [KSPField]
        public float maxHeight = 100f;

        [KSPField]
        public float minDiameter = 0.625f;

        [KSPField]
        public float maxDiameter = 10f;

        [KSPField]
        public String topMountName = "SC-RBDC-MountUpper";

        [KSPField]
        public String bottomMountName = "SC-RBDC-MountLower";

        [KSPField]
        public String scaleTransform = "SC-RBDC-Scalar";

        [KSPField(isPersistant = true, guiName = "Height", guiActiveEditor = true),
         UI_FloatEdit(sigFigs = 3, suppressEditorShipModified =true)]
        public float height = 2f;

        [KSPField(isPersistant = true, guiName = "Diameter", guiActiveEditor = true),
         UI_FloatEdit(sigFigs = 3, suppressEditorShipModified = true)]
        public float diameter = 1.25f;

        [KSPField(guiName = "Raw Thrust", guiActive = true, guiActiveEditor = true)]
        public float guiEngineThrust = 0f;

        [KSPField(isPersistant = true)]
        public bool initializedResources = false;
        
        [KSPField]
        public String techLimitSet = "Default";
                
        private float prevHeight;
        private float prevDiameter;

        private Transform topMountTransform;
        private Transform bottomMountTransform;
        private Transform scalarTransform;
        
        private FuelTypeData fuelType;

        // tech limit values are updated every time the part is initialized in the editor; ignored otherwise
        private float techLimitMaxDiameter;

        public void onHeightUpdated(BaseField field, object obj)
        {
            if(prevHeight!= height)
            {
                prevHeight = height;
                setHeightFromEditor(height, true);
            }
        }

        public void onDiameterUpdated(BaseField field, object obj)
        {
            if (prevDiameter != diameter)
            {
                prevDiameter = diameter;
                setDiameterFromEditor(diameter, true);
            }
        }

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            ConfigNode node = SSTUStockInterop.getPartModuleConfig(part, this);
            TechLimit.updateTechLimits(techLimitSet, out techLimitMaxDiameter);
            if (diameter > techLimitMaxDiameter) { diameter = techLimitMaxDiameter; }
            fuelType = new FuelTypeData(node.GetNode("FUELTYPE"));
            float max = techLimitMaxDiameter < maxDiameter ? techLimitMaxDiameter : maxDiameter;
            this.updateUIFloatEditControl("height", minHeight, maxHeight, heightIncrement*2, heightIncrement, heightIncrement*0.05f, true, height);
            this.updateUIFloatEditControl("diameter", max, maxDiameter, diameterIncrement*2, diameterIncrement, diameterIncrement*0.05f, true, diameter);
            locateTransforms();
            updateModelScales();
            updateModelPositions();
            updateAttachNodes(false);

            //can just check if part has -any- resources? if it does, then don't touch it... if it does not, then put some there (as long as it is not the prefab...)
            if (!initializedResources && (HighLogic.LoadedSceneIsFlight || HighLogic.LoadedSceneIsEditor))
            {
                initializedResources = true;
                updatePartResources();
            }
            updateEditorFields();
            Fields["height"].uiControlEditor.onFieldChanged = onHeightUpdated;
            Fields["diameter"].uiControlEditor.onFieldChanged = onDiameterUpdated;
        }

        public float GetModuleMass(float defaultMass, ModifierStagingSituation sit)
        {
            float scale = getScale();
            float modifiedMass = defaultMass * Mathf.Pow(scale, 3);
            return -defaultMass + modifiedMass;
        }

        public float GetModuleCost(float defaultCost, ModifierStagingSituation sit)
        {
            float scale = getScale();
            float modifiedCost = defaultCost * Mathf.Pow(scale, 3);
            float currentVolume = resourceVolume * Mathf.Pow(getCurrentModelScale(), thrustScalePower);            
            modifiedCost += fuelType.getResourceCost(currentVolume);
            return -defaultCost + modifiedCost;
        }

        public ModifierChangeWhen GetModuleMassChangeWhen() { return ModifierChangeWhen.CONSTANTLY; }
        public ModifierChangeWhen GetModuleCostChangeWhen() { return ModifierChangeWhen.CONSTANTLY; }

        public void Start()
        {
            updateEngineThrust();
        }

        public override string GetInfo()
        {
            return "This part has configurable diameter, height, and ejection force.  Includes separation motors for the attached payload.  Motor thrust and resource volume scale with part size.";
        }
        
        private void setHeightFromEditor(float newHeight, bool updateSymmetry)
        {
            if (newHeight > maxHeight) { newHeight = maxHeight; }
            if (newHeight < minHeight) { newHeight = minHeight; }
            height = newHeight;
            updateEditorFields();
            updateModule();
            if (updateSymmetry)
            {
                foreach (Part p in part.symmetryCounterparts)
                {
                    p.GetComponent<SSTUCustomRadialDecoupler>().setHeightFromEditor(newHeight, false);
                }
            }
        }

        private void setDiameterFromEditor(float newDiameter, bool updateSymmetry)
        {
            if (newDiameter > maxDiameter) { newDiameter = maxDiameter; }
            if (newDiameter < minDiameter) { newDiameter = minDiameter; }
            if (newDiameter > techLimitMaxDiameter) { newDiameter = techLimitMaxDiameter; }
            diameter = newDiameter;
            updateEditorFields();
            updatePartResources();
            updateEngineThrust();
            updateModule();
            if (updateSymmetry)
            {
                foreach (Part p in part.symmetryCounterparts)
                {
                    p.GetComponent<SSTUCustomRadialDecoupler>().setDiameterFromEditor(newDiameter, false);
                }
            }
        }

        private void updateTransformYPos(Transform t, float pos)
        {            
            Vector3 partSpacePosition = part.transform.InverseTransformPoint(t.position);
            partSpacePosition.y = pos;
            t.position = part.transform.TransformPoint(partSpacePosition);
        }

        private void setModelScale(Transform t, float scale)
        {
            t.localScale = new Vector3(scale, scale, scale); ;
        }

        //restores the editor field values for radius/height
        private void updateEditorFields()
        {
            prevHeight = height;
            prevDiameter = diameter;
        }

        private void updateModule()
        {
            updateModelScales();
            updateModelPositions();
            updateAttachNodes(true);
        }

        private void updateModelPositions()
        {
            float half = height * 0.5f;
            updateTransformYPos(topMountTransform, half);
            updateTransformYPos(bottomMountTransform, -half);
        }

        private void updateModelScales()
        {
            setModelScale(scalarTransform, getScale());
        }

        private float getScale()
        {
            return diameter / modelDiameter;
        }

        private void locateTransforms()
        {
            topMountTransform = part.transform.FindRecursive(topMountName);
            bottomMountTransform = part.transform.FindRecursive(bottomMountName);
            scalarTransform = part.transform.FindRecursive(scaleTransform);
        }

        private void updateAttachNodes(bool userInput)
        {
            AttachNode surface = part.srfAttachNode;
            if (surface != null)
            {
                Vector3 pos = new Vector3(surfaceNodeX * getScale(), 0, 0);
                SSTUAttachNodeUtils.updateAttachNodePosition(part, surface, pos, surface.orientation, userInput);
            }
        }

        private void updateEngineThrust()
        {
            float currentScale = getCurrentModelScale();
            float thrustScalar = Mathf.Pow(currentScale, thrustScalePower);
            float maxThrust = engineThrust * thrustScalar;
            guiEngineThrust = maxThrust;
            ConfigNode updateNode = new ConfigNode("MODULE");
            updateNode.AddValue("maxThrust", maxThrust);
            ModuleEngines[] engines = part.GetComponents<ModuleEngines>();
            foreach (ModuleEngines engine in engines)
            {
                engine.maxThrust = maxThrust;
                engine.Load(updateNode);
            }
        }

        private void updatePartResources()
        {
            float resourceScalar = Mathf.Pow(getCurrentModelScale(), thrustScalePower);
            float currentVolume = resourceVolume * resourceScalar;
            if (SSTUModInterop.isRFInstalled())
            {
                SSTUModInterop.onPartFuelVolumeUpdate(part, currentVolume);
            }
            else
            {
                SSTUResourceList res = fuelType.getResourceList(currentVolume);
                res.setResourcesToPart(part, HighLogic.LoadedSceneIsEditor);
            }
        }
        
        private float getCurrentModelScale()
        {
            return diameter / modelDiameter;
        }
    }

}
