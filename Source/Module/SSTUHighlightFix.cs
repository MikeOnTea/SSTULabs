﻿using UnityEngine;
using System.Collections.Generic;

namespace SSTUTools
{
    public class SSTUHighlightFix : PartModule
    {

        [KSPField]
        public string transformName = "HighlightingHackObject";

        private Transform dummyTransform;

        private Renderer[] cachedRenderList;

        private MaterialPropertyBlock mpb;

        private static int colorID;
        private static int falloffID;

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            MonoBehaviour.print("Starting highlighting fixer for part: " + part.name);
            dummyTransform = part.transform.FindRecursive(transformName);
            if (dummyTransform == null)//should only be null on the prefab part
            {
                GameObject newObj = new GameObject(transformName);
                newObj.transform.name = transformName;
                newObj.transform.NestToParent(part.transform.FindRecursive("model"));

                Renderer render = newObj.AddComponent<MeshRenderer>();//add a new render
                render.material = SSTUUtils.loadMaterial(null, null);//with an empty dummy material, also it doesn't actually have any mesh
                dummyTransform = newObj.transform;//cache reference to it for use for updating
            }
            if (HighLogic.LoadedSceneIsEditor)
            {
                GameEvents.onEditorShipModified.Add(new EventData<ShipConstruct>.OnEvent(onEditorVesselModified));
            }
        }

        public void Start()
        {
            colorID = HighLogic.ShaderPropertyID_RimColor;
            falloffID = HighLogic.ShaderPropertyID_RimFalloff;
            mpb = new MaterialPropertyBlock();
            updateRenderCache();
        }

        /// <summary>
        /// Event callback for when vessel is modified in the editor.  Used in this case to update the cached render list (my modular parts always call onEditorVesselModified when their models are changed, so this is a good enough catch for them)
        /// </summary>
        /// <param name="ship"></param>
        public void onEditorVesselModified(ShipConstruct ship)
        {
            if (!HighLogic.LoadedSceneIsEditor) { return; }
            updateRenderCache();
        }

        public void LateUpdate()
        {
            if (HighLogic.LoadedSceneIsEditor)
            {
                Color color = dummyTransform.renderer.material.GetColor(colorID);
                float falloff = dummyTransform.renderer.material.GetFloat(falloffID);

                mpb.SetColor(colorID, color);
                mpb.SetFloat(falloffID, falloff);
                bool updateCache = false;
                int len = cachedRenderList.Length;
                for (int i = 0; i < len; i++)
                {
                    //somehow we got a nulled out render, object was likely deleted -- update the cached list, will be correct for the next tick/update cycle
                    if (cachedRenderList[i] == null)
                    {
                        updateCache = true;
                        continue;
                    }
                    cachedRenderList[i].SetPropertyBlock(mpb);
                }
                if (updateCache)
                {
                    updateRenderCache();
                }
            }
        }

        private void updateRenderCache()
        {
            cachedRenderList = null;
            Renderer[] renders = part.transform.GetComponentsInChildren<Renderer>(true);
            List<Renderer> rendersToCache = new List<Renderer>();
            int len = renders.Length;
            for (int i = 0; i < len; i++)
            {
                //skip the dummy renderer; though it honestly should not matter if it is in the list or not, as we are pulling the current vals from it before setting anything
                if (renders[i].transform != dummyTransform)
                {
                    rendersToCache.Add(renders[i]);
                }
            }
            cachedRenderList = rendersToCache.ToArray();
        }

    }
}