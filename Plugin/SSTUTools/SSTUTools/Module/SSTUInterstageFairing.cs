using System;
using System.Collections.Generic;
using UnityEngine;
using KSPShaderTools;

namespace SSTUTools
{
    public class SSTUInterstageFairing : PartModule, IMultipleDragCube, IPartCostModifier, IPartMassModifier, IRecolorable
    {

        #region KSP MODULE fields
        //config fields for various transform and node names

        //reference to the model for this part, used for the fairing base model/mesh.  Panels are added around/to this base model.
        [KSPField]
        public String modelName = "SSTU/Assets/SC-GEN-FR";

        /// <summary>
        /// The diameter of the mounting point of the model
        /// </summary>
        [KSPField]
        public float defaultModelDiameter = 5f;
        
        /// <summary>
        /// At the models default diameter, what is the diameter of the bottom of the fairing?
        /// </summary>
        [KSPField]
        public float defaultFairingDiameter = 5f;

        [KSPField]
        public float defaultBaseVolume = 1f;
                
        [KSPField]
        public float internalNodePosition;

        [KSPField]
        public float bottomNodePosition;
        
        [KSPField]
        public String topNodeName = "top";

        [KSPField]
        public String bottomNodeName = "bottom";

        [KSPField]
        public String internalNodeName = "internal";

        [KSPField]
        public int topDecouplerModuleIndex = 1;

        [KSPField]
        public int internalDecouplerModuleIndex = 2;

        //how many sections should the fairing have radially?
        [KSPField]
        public int numberOfPanels = 4;
        
        [KSPField]
        public float wallThickness = 0.025f;
        
        [KSPField]
        public int cylinderSides = 24;

        //maximum height
        [KSPField]
        public float maxHeight = 15.0f;

        //minimum height
        [KSPField]
        public float minHeight = 1.0f;

        [KSPField]
        public float minDiameter = 0.625f;

        [KSPField]
        public float maxDiameter = 10f;

        //how far should the panels be rotated for the 'deployed' animation
        [KSPField]
        public float deployedRotation = 60f;

        //how many degrees per second should the fairings rotate while deploy animation is playing?
        [KSPField]
        public float animationSpeed = 5f;

        [KSPField]
        public float costPerBaseVolume = 1500f;

        [KSPField]
        public float costPerPanelArea = 50f;

        [KSPField]
        public float massPerBaseCubicMeter = 0.5f;

        [KSPField]
        public float massPerPanelArea = 0.025f;

        [KSPField]
        public float topDiameterIncrement = 0.625f;

        [KSPField]
        public float bottomDiameterIncrement = 0.625f;

        [KSPField]
        public float heightIncrement = 1;

        [KSPField]
        public String uvMap = "NodeFairing";

        [KSPField(isPersistant = true, guiName = "Texture", guiActiveEditor = true),
         UI_ChooseOption(suppressEditorShipModified = true)]
        public String currentTextureSet = String.Empty;

        /// <summary>
        /// Top diameter of the -fairing-
        /// </summary>
        [KSPField(isPersistant = true, guiActiveEditor = true, guiName = "Top Diam"),
         UI_FloatEdit(sigFigs = 3, suppressEditorShipModified = true)]
        public float topDiameter = 2.5f;
        
        /// <summary>
        /// Bottom diameter of the -model-; this is not necessarily the bottom diameter of the fairing
        /// </summary>
        [KSPField(isPersistant = true, guiActiveEditor = true, guiName = "Base Diam"),
         UI_FloatEdit(sigFigs = 3, suppressEditorShipModified = true)]
        public float bottomDiameter = 2.5f;

        //stored current height of the panels, used to recreate mesh on part reload, may be set in config to set the default starting height
        [KSPField(isPersistant = true, guiActiveEditor = true, guiName = "Tot. Height"),
         UI_FloatEdit(sigFigs = 3, suppressEditorShipModified = true)]
        public float currentHeight = 1.0f;

        //if top radius !=bottom radius, this will create a 'split' panel at this position, for a straight-up-then-tapered/flared fairing
        [KSPField(isPersistant = true, guiActiveEditor = true, guiName = "Taper Height"),
         UI_FloatEdit(sigFigs = 3, suppressEditorShipModified = true)]
        public float currentStraightHeight = 0f;

        [KSPField(guiName = "Jettison Panels On Deploy", isPersistant = true, guiActive = true, guiActiveEditor = true),
         UI_Toggle(enabledText = "Enabled", disabledText = "Disabled", suppressEditorShipModified = true)]
        public bool jettisonPanels = false;

        [KSPField(guiName = "Toggle Deployment", guiActiveEditor = true, isPersistant = true),
         UI_Toggle(enabledText = "Open", disabledText = "Closed", suppressEditorShipModified = true)]
        public bool editorDeployed = false;

        [KSPField(isPersistant =true, guiName ="Editor Transparency", guiActiveEditor = true),
         UI_Toggle(enabledText ="On", disabledText ="Off", suppressEditorShipModified = true)]
        public bool editorTransparency = true;

        [KSPField(guiName = "Colliders", guiActiveEditor = true, isPersistant =true),
         UI_Toggle(enabledText = "Enabled", disabledText = "Disabled", suppressEditorShipModified = true)]
        public bool generateColliders = false;

        [KSPField(guiName = "Fairing Cost", guiActiveEditor = true)]
        public float fairingCost;

        [KSPField(guiName = "Fairing Mass", guiActiveEditor = true)]
        public float fairingMass;

        [KSPField(isPersistant = true)]
        public bool panelsJettisoned = false;

        //are planels deployed and upper node decoupled?
        //toggled to true as soon as deploy action is activated
        [KSPField(isPersistant = true)]
        public bool deployed = false;

        //is inner node decoupled?
        //toggled to true as soon as inner node is decoupled, only available after deployed=true
        [KSPField(isPersistant = true)]
        public bool decoupled = false;

        //deployment animation persistence field
        [KSPField(isPersistant = true)]
        public float currentRotation = 0.0f;

        [KSPField(isPersistant = true)]
        public bool animating = false;

        [KSPField(isPersistant = true)]
        public string customColorData = string.Empty;

        [KSPField(isPersistant = true)]
        public bool initializedColors = false;

        [Persistent]
        public string configNodeData = string.Empty;

        #endregion

        #region private working variables

        private bool initialized;

        private InterstageFairingContainer fairingBase;        

        //material used for procedural fairing, created from the texture references above
        private Material fairingMaterial;

        private RecoloringHandler recolorHandler;

        //lerp between the two cubes depending upon deployed state
        //re-render the cubes on fairing rebuild
        private DragCube closedCube;
        private DragCube openCube;

        #endregion

        #region KSP GUI Actions

        [KSPEvent(name = "deployEvent", guiName = "Deploy Panels", guiActive = true)]
        public void deployEvent()
        {
            this.actionWithSymmetry(m => 
            {
                m.onDeployEvent();
            });
        }

        [KSPEvent(name = "decoupleEvent", guiName = "Decouple Inner Node", guiActive = true)]
        public void decoupleEvent()
        {
            this.actionWithSymmetry(m => 
            {
                m.onDecoupleEvent();
            });
        }

        [KSPAction("Deploy and Release Top Node")]
        public void deployAction(KSPActionParam param)
        {
            onDeployEvent();
        }

        [KSPAction("Decouple Inner Node")]
        public void decoupleAction(KSPActionParam param)
        {
            onDecoupleEvent();
        }

        public void onTextureUpdated(BaseField field, object obj)
        {
            this.actionWithSymmetry(m =>
            {
                m.currentTextureSet = currentTextureSet;
                m.updateTextureSet(!SSTUGameSettings.persistRecolor());
            });
        }

        public void onEditorDeployUpdated(BaseField field, object obj)
        {
            this.actionWithSymmetry(m => 
            {
                m.setPanelRotations(m.currentRotation);
            });
        }

        public void onCollidersUpdated(BaseField field, object obj)
        {
            this.actionWithSymmetry(m =>
            {
                m.generateColliders = generateColliders;
                if (m.fairingBase.generateColliders != m.generateColliders)
                {
                    m.fairingBase.generateColliders = m.generateColliders;
                    m.rebuildFairing(true);
                }
            });
        }

        public void onTransparencyUpdated(BaseField field, object obj)
        {
            this.actionWithSymmetry(m => 
            {
                m.editorTransparency = editorTransparency;
                m.fairingBase.setOpacity(m.editorTransparency ? 0.25f : 1);
            });
        }

        public void onTopDiameterUpdated(BaseField field, object obj)
        {
            this.actionWithSymmetry(m => 
            {
                m.topDiameter = topDiameter;
                m.rebuildFairing(true);
                m.updateShieldStatus();
            });
        }

        public void onBottomDiameterUpdated(BaseField field, object obj)
        {
            this.actionWithSymmetry(m => 
            {
                m.bottomDiameter = bottomDiameter;
                m.rebuildFairing(true);
                m.updateShieldStatus();
            });
        }

        public void onHeightUpdated(BaseField field, object obj)
        {
            this.actionWithSymmetry(m =>
            {
                m.currentHeight = currentHeight;
                m.validateStraightHeight();
                m.rebuildFairing(true);
                m.updateShieldStatus();
            });
        }

        public void onStraightUpdated(BaseField field, object obj)
        {
            this.actionWithSymmetry(m =>
            {
                m.currentStraightHeight = currentStraightHeight;
                m.validateStraightHeight();
                m.rebuildFairing(true);
                m.updateShieldStatus();
            });
        }

        #endregion

        #region KSP overrides

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            if (string.IsNullOrEmpty(configNodeData)) { configNodeData = node.ToString(); }
            if (node.HasValue("customColor1"))
            {
                Color c1 = node.GetColorFromFloatCSV("customColor1");
                Color c2 = node.GetColorFromFloatCSV("customColor2");
                Color c3 = node.GetColorFromFloatCSV("customColor3");
                string colorData = c1.r + "," + c1.g + "," + c1.b + "," + c1.a + ",0;";
                colorData = colorData + c2.r + "," + c2.g + "," + c2.b + "," + c2.a + ",0;";
                colorData = colorData + c3.r + "," + c3.g + "," + c3.b + "," + c3.a + ",0";
                customColorData = colorData;
            }
            initialize();
        }

        public override void OnStart(PartModule.StartState state)
        {
            base.OnStart(state);            
            initialize();
            this.updateUIFloatEditControl(nameof(topDiameter), minDiameter, maxDiameter, topDiameterIncrement*2, topDiameterIncrement, topDiameterIncrement*0.05f, true, topDiameter);
            this.updateUIFloatEditControl(nameof(bottomDiameter), minDiameter, maxDiameter, bottomDiameterIncrement*2, bottomDiameterIncrement, bottomDiameterIncrement*0.05f, true, bottomDiameter);
            this.updateUIFloatEditControl(nameof(currentHeight), minHeight, maxHeight, heightIncrement*2, heightIncrement, heightIncrement*0.05f, true, currentHeight);
            this.updateUIFloatEditControl(nameof(currentStraightHeight), 0, maxHeight, heightIncrement*2, heightIncrement, heightIncrement*0.05f, true, currentStraightHeight);
            Fields[nameof(topDiameter)].uiControlEditor.onFieldChanged = onTopDiameterUpdated;
            Fields[nameof(bottomDiameter)].uiControlEditor.onFieldChanged = onBottomDiameterUpdated;
            Fields[nameof(currentHeight)].uiControlEditor.onFieldChanged = onHeightUpdated;
            Fields[nameof(currentStraightHeight)].uiControlEditor.onFieldChanged = onStraightUpdated;
            Fields[nameof(editorTransparency)].uiControlEditor.onFieldChanged = onTransparencyUpdated;
            Fields[nameof(generateColliders)].uiControlEditor.onFieldChanged = onCollidersUpdated;
            Fields[nameof(editorDeployed)].uiControlEditor.onFieldChanged = onEditorDeployUpdated;
            Fields[nameof(currentTextureSet)].uiControlEditor.onFieldChanged = onTextureUpdated;
            GameEvents.onEditorShipModified.Add(new EventData<ShipConstruct>.OnEvent(onEditorShipModified));
        }

        public void OnDestroy()
        {
            GameEvents.onEditorShipModified.Remove(new EventData<ShipConstruct>.OnEvent(onEditorShipModified));
        }

        public void onEditorShipModified(ShipConstruct ship)
        {
            if (!HighLogic.LoadedSceneIsEditor)
            {
                return;
            }
            fairingBase.setOpacity(editorTransparency ? 0.25f : 1);
        }

        public override void OnActive()
        {
            base.OnActive();
            if (!deployed)
            {
                onDeployEvent();
            }
            else if (!decoupled)
            {
                onDecoupleEvent();
            }            
        }

        public override string GetInfo()
        {
            return "This part has configurable top diameter, bottom diameter, height, and taper start height.  Includes functions to decouple both top and inner payloads and airstream protection for inner payload.";
        }
        
        public void FixedUpdate()
        {
            if (HighLogic.LoadedSceneIsFlight && animating)
            {
                updateAnimation();
            }
        }

        //IMultipleDragCube override
        public string[] GetDragCubeNames()
        {
            return new string[]
            {
                "Open",
                "Closed"
            };
        }

        //IMultipleDragCube override
        public void AssumeDragCubePosition(string name)
        {
            if ("Open".Equals(name))
            {
                setPanelRotations(deployedRotation);
            }
            else
            {
                setPanelRotations(0);
            }
        }

        //IMultipleDragCube override
        public bool UsesProceduralDragCubes() { return false; }

        //IMultipleDragCube override
        public bool IsMultipleCubesActive
        {
            get
            {
                return true;
            }
        }

        //IPartCostModifier override
        public float GetModuleCost(float cost, ModifierStagingSituation sit) { return -cost + fairingCost; }

        //IPartMassModifier override
        public float GetModuleMass(float mass, ModifierStagingSituation sit) { return -mass + fairingMass; }
        public ModifierChangeWhen GetModuleMassChangeWhen() { return ModifierChangeWhen.CONSTANTLY; }
        public ModifierChangeWhen GetModuleCostChangeWhen() { return ModifierChangeWhen.CONSTANTLY; }

        //IRecolorable override
        public string[] getSectionNames()
        {
            return new string[] { "Decoupler" };
        }

        //IRecolorable override
        public RecoloringData[] getSectionColors(string name)
        {
            return recolorHandler.getColorData();
        }

        //IRecolorable override
        public void setSectionColors(string name, RecoloringData[] colors)
        {
            recolorHandler.setColorData(colors);
            updateTextureSet(false);
        }

        //IRecolorable override
        public TextureSet getSectionTexture(string section)
        {
            return KSPShaderLoader.getTextureSet(currentTextureSet);
        }

        #endregion

        #region private action handling methods

        private void onDeployEvent()
        {
            if (!deployed)
            {
                animating = true;
                deployed = true;
                decoupleByModule(topDecouplerModuleIndex);
                updateShieldStatus();
                updateGuiState();
            }
        }

        private void onDecoupleEvent()
        {
            if (deployed && !decoupled)
            {
                decoupled = true;
                decoupleByModule(internalDecouplerModuleIndex);
                updateGuiState();
            }
        }

        private void decoupleByModule(int index)
        {
            ModuleDecouple d = part.Modules[index] as ModuleDecouple;
            if (d == null)
            {
                MonoBehaviour.print("ERROR: No decoupler found at module index: " + index);
            }
            else if (!d.isDecoupled)
            {
                d.Decouple();
            }
        }

        #endregion

        #region fairing data update methods

        private void setPanelRotations(float rotation)
        {
            if (fairingBase != null && !panelsJettisoned)
            {
                rotation = HighLogic.LoadedSceneIsEditor && editorDeployed ? deployedRotation : rotation;
                fairingBase.setPanelRotations(rotation);
            }
        }

        private void setPanelOpacity(float val)
        {
            if (fairingBase != null && !panelsJettisoned) { fairingBase.setOpacity(val); }
        }

        private void updateAnimation()
        {
            float delta = TimeWarp.fixedDeltaTime * animationSpeed;
            float previousAngle = currentRotation;
            currentRotation += delta;
            if (currentRotation >= deployedRotation)
            {
                currentRotation = deployedRotation;
                setPanelRotations(currentRotation);
                animating = false;
                updateShieldStatus();
                if (jettisonPanels)
                {
                    jettisonFairingPanels();
                }
            }
            else
            {
                setPanelRotations(currentRotation);
            }
            updateDragCube();
        }

        private void updateDragCube()
        {
            if (!HighLogic.LoadedSceneIsFlight && !HighLogic.LoadedSceneIsEditor) { return; }//don't touch them in the editor
            float percentDeployed = currentRotation / deployedRotation;
            part.DragCubes.SetCubeWeight("Open", percentDeployed);
            part.DragCubes.SetCubeWeight("Closed", 1f - percentDeployed);
        }

        private void enableEditorColliders(bool val)
        {
            fairingBase.enableEditorCollider(val);
        }

        private void jettisonFairingPanels()
        {
            panelsJettisoned = true;
            fairingBase.jettisonPanels(part, 10, Vector3.forward, 0.1f);
        }
                
        #endregion

        #region fairing rebuild methods            

        private void rebuildFairing(bool userInput)
        {
            Transform model = part.transform.FindModel(modelName);
            if (model != null)
            {
                float scale = getCurrentScale();
                model.transform.localScale = new Vector3(scale, scale, scale);
            }
            createPanels();          
            updateFairingMassAndCost();
            updateNodePositions(userInput);
            updateShieldStatus();
            enableEditorColliders(HighLogic.LoadedSceneIsEditor);
            setPanelRotations(currentRotation);
            SSTUModInterop.onPartGeometryUpdate(part, false);
            recreateDragCubes();
        }

        //create procedural panel sections for the current part configuration (radialSection count), with orientation set from base panel orientation
        private void createPanels()
        {
            if (!panelsJettisoned)
            {
                float modelFairingScale = defaultFairingDiameter / defaultModelDiameter;
                float bottomRadius = bottomDiameter * modelFairingScale * 0.5f;
                float topRadius = topDiameter * 0.5f;

                fairingBase.clearProfile();
                fairingBase.addRing(0, bottomRadius);
                if (topRadius != bottomRadius && currentStraightHeight < currentHeight)
                {
                    fairingBase.addRing(currentStraightHeight, bottomRadius);
                }
                fairingBase.addRing(currentHeight, topDiameter * 0.5f);
                fairingBase.generateColliders = this.generateColliders;
                fairingBase.generateFairing();
                fairingBase.setMaterial(fairingMaterial);
                if (HighLogic.LoadedSceneIsEditor && editorTransparency) { setPanelOpacity(0.25f); }
                else { setPanelOpacity(1.0f); }
                updateTextureSet(false);
            }
        }

        private void recreateDragCubes()
        {
            if (!HighLogic.LoadedSceneIsFlight && !HighLogic.LoadedSceneIsEditor) { return; }//don't touch them in the prefab
            if (part.partInfo == null) { return; }
            setPanelRotations(deployedRotation);
            this.openCube = DragCubeSystem.Instance.RenderProceduralDragCube(part);
            setPanelRotations(0);
            this.closedCube = DragCubeSystem.Instance.RenderProceduralDragCube(part);
            this.closedCube.Name = "Closed";
            this.openCube.Name = "Open";
            part.DragCubes.ClearCubes();
            part.DragCubes.Cubes.Add(closedCube);
            part.DragCubes.Cubes.Add(openCube);
            part.DragCubes.ResetCubeWeights();
            updateDragCube();
        }

        private void updateFairingMassAndCost()
        {
            float baseScale = bottomDiameter / defaultModelDiameter;
            float baseVolume = baseScale * baseScale * baseScale * defaultBaseVolume;
            float avgDiameter = (bottomDiameter + topDiameter) * 0.5f;
            float panelArea = avgDiameter * Mathf.PI * currentHeight;//circumference * height = area

            float baseCost = costPerBaseVolume * baseVolume;
            float panelCost = costPerPanelArea * panelArea;
            float baseMass = massPerBaseCubicMeter * baseVolume;
            float panelMass = massPerPanelArea * panelArea;

            fairingCost = panelCost + baseCost;
            fairingMass = panelMass + baseMass;
        }
              
        #endregion        

        #region private helper methods

        private void initialize()
        {
            if (initialized) { return; }
            initialized = true;

            recolorHandler = new RecoloringHandler(Fields[nameof(customColorData)]);

            ConfigNode node = SSTUConfigNodeUtils.parseConfigNode(configNodeData);

            ConfigNode[] textureNodes = node.GetNodes("TEXTURESET");
            string[] names = TextureSet.getTextureSetNames(textureNodes);
            string[] titles = TextureSet.getTextureSetTitles(textureNodes);
            TextureSet t = KSPShaderLoader.getTextureSet(currentTextureSet);
            if (t == null)
            {
                currentTextureSet = names[0];
                t = KSPShaderLoader.getTextureSet(currentTextureSet);
                initializedColors = false;
            }
            if (!initializedColors)
            {
                initializedColors = true;
                recolorHandler.setColorData(t.maskColors);
            }
            this.updateUIChooseOptionControl(nameof(currentTextureSet), names, titles, true, currentTextureSet);
            fairingMaterial = t.textureData[0].createMaterial("SSTUFairingMaterial");
            
            Transform tr = part.transform.FindRecursive("model").FindOrCreate("PetalAdapterRoot");
            fairingBase = new InterstageFairingContainer(tr.gameObject, cylinderSides, numberOfPanels, wallThickness);
            UVMap uvs = UVMap.GetUVMapGlobal(uvMap);
            fairingBase.outsideUV = uvs.getArea("outside");
            fairingBase.insideUV = uvs.getArea("inside");
            fairingBase.edgesUV = uvs.getArea("edges");

            validateStraightHeight();
            rebuildFairing(false);//will create fairing using default / previously saved fairing configuration
            setPanelRotations(currentRotation);
            if (panelsJettisoned)
            {
                fairingBase.destroyFairing();
            }
            updateGuiState();
        }

        private void validateStraightHeight()
        {
            if (currentStraightHeight > currentHeight)
            {
                currentStraightHeight = currentHeight;
                this.updateUIFloatEditControl(nameof(currentStraightHeight), currentStraightHeight);
            }
        }

        private void updateNodePositions(bool userInput)
        {
            float scale = getCurrentScale();
            float topY = currentHeight;
            float innerY = internalNodePosition * scale;
            float bottomY = bottomNodePosition * scale;
            Vector3 bottomNodePOs = new Vector3(0, bottomY, 0);
            Vector3 innerNodePos = new Vector3(0, innerY, 0);
            Vector3 topNodePos = new Vector3(0, topY, 0);

            AttachNode node = part.FindAttachNode(bottomNodeName);
            if (node != null)
            {
                SSTUAttachNodeUtils.updateAttachNodePosition(part, node, bottomNodePOs, node.orientation, userInput);
            }
            node = part.FindAttachNode(internalNodeName);
            if (node != null)
            {
                SSTUAttachNodeUtils.updateAttachNodePosition(part, node, innerNodePos, node.orientation, userInput);
            }
            node = part.FindAttachNode(topNodeName);
            if (node != null)
            {
                SSTUAttachNodeUtils.updateAttachNodePosition(part, node, topNodePos, node.orientation, userInput);
            }
        }

        private float getCurrentScale()
        {
            return bottomDiameter / defaultModelDiameter;
        }

        private void updateGuiState()
        {
            Events[nameof(deployEvent)].active = !deployed && !decoupled;//only available if not previously deployed or decoupled
            Events[nameof(decoupleEvent)].active = deployed && !decoupled;//only available if deployed but not decoupled
            Actions[nameof(deployAction)].active = !deployed && !decoupled;//only available if not previously deployed or decoupled
            Actions[nameof(decoupleAction)].active = deployed && !decoupled;//only available if deployed but not decoupled			
        }
        
        private void updateShieldStatus()
        {
            SSTUAirstreamShield shield = part.GetComponent<SSTUAirstreamShield>();
            if (shield != null)
            {
                string name = "InterstageFairingShield" + "" + part.Modules.IndexOf(this);
                if (!deployed)
                {
                    float top = currentHeight; ;
                    float bottom = 0f;
                    float topRad = topDiameter*0.5f;
                    float botRad = bottomDiameter*0.5f;
                    bool useTop = true;
                    bool useBottom = false;
                    shield.addShieldArea(name, topRad, botRad, top, bottom, useTop, useBottom);
                }
                else
                {
                    shield.removeShieldArea(name);
                }
            }
        }

        private void updateTextureSet(bool useDefaults)
        {
            TextureSet s = KSPShaderLoader.getTextureSet(currentTextureSet);
            RecoloringData[] colors = useDefaults ? s.maskColors : getSectionColors(string.Empty);
            fairingBase.enableTextureSet(currentTextureSet, colors);
            if (useDefaults)
            {
                recolorHandler.setColorData(colors);
            }
            SSTUModInterop.onPartTextureUpdated(part);
        }

        #endregion

        private class InterstageFairingContainer : FairingContainer
        {
            //this collider sits at the top of the fairing so that the payload properly snaps into position
            private GameObject editorCollider;
            public float editorColliderHeight = 0.1f;
            
            public InterstageFairingContainer(GameObject root, int cylinderFaces, int numberOfPanels, float thickness) : base(root, cylinderFaces, numberOfPanels, thickness)
            {

            }           

            public void enableEditorCollider(bool enabled)
            {
                if (editorCollider != null)
                {
                    GameObject.Destroy(editorCollider);
                }
                if (enabled)
                {
                    float maxHeight = getHeight();
                    Vector3 offset = new Vector3(0, maxHeight - editorColliderHeight, 0);

                    CylinderMeshGenerator cmg = new CylinderMeshGenerator(offset, 12, editorColliderHeight, getBottomRadius(), getTopRadius(), 0, 0);
                    Mesh mesh = cmg.generateMesh();
                    editorCollider = new GameObject("PetalAdapterEditorCollider");
                    MeshFilter mf = editorCollider.AddComponent<MeshFilter>();
                    mf.mesh = mesh;
                    MeshCollider mc = editorCollider.AddComponent<MeshCollider>();
                    mc.convex = true;
                    mc.enabled = true;
                    editorCollider.transform.NestToParent(rootObject.transform);
                }
            }
        }
    }
}

