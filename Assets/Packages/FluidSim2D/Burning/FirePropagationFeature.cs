using System;
using System.Collections.Generic;
using FluidSim2DProject;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class FirePropagationFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class BlitSettings
    {
        public Material blitMaterial;
        public LayerMask layerMask;
        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
    } 
    public BlitSettings settings = new BlitSettings();

    BlitToRenderTexturePass blitPass;

    public override void Create()
    {
        if (settings.blitMaterial == null)
        {
            settings.blitMaterial = new Material(Shader.Find("Custom/FireProjection"));
        }

        var projector = FindObjectOfType<CustomProjector>();
        if (projector == null) throw new Exception("PROGECTOR NOT FOUND");
        //projector._material = settings.blitMaterial;
        var fluidSim = FindObjectOfType<FluidSim>();
        settings.blitMaterial.SetTexture("_FireProj",fluidSim.visualizationMat.GetTexture("_ProjectionTexture"));
        Debug.Log($"using projector: {projector.name}");
        blitPass = new BlitToRenderTexturePass(settings.blitMaterial, settings.layerMask);
        blitPass.renderPassEvent = settings.renderPassEvent;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (settings.blitMaterial != null)
        {
            renderer.EnqueuePass(blitPass);
        }
    }

    class BlitToRenderTexturePass : ScriptableRenderPass
    {
        private Material blitMaterial;
        private LayerMask layerMask;
        private List<BurningBehaviour> burningBehaviours = new List<BurningBehaviour>();

        public BlitToRenderTexturePass(Material blitMaterial, LayerMask layerMask)
        {
            this.blitMaterial = blitMaterial;
            this.layerMask = layerMask;
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            // Find all BurningBehaviour components on objects in the specified layer
            burningBehaviours.Clear();
            foreach (var behaviour in GameObject.FindObjectsOfType<BurningBehaviour>())
            {
                if (((1 << behaviour.gameObject.layer) & layerMask) != 0)
                {
                    burningBehaviours.Add(behaviour);
                }
            }

            // Prepare Command Buffer
            CommandBuffer cmd = CommandBufferPool.Get("FireProjection");
            SetMatrix();

            foreach (var behaviour in burningBehaviours)
            {
                /*if (behaviour.renderTexture != null)
                {
                    // Set the Render Target to the object's renderTexture
                    cmd.SetRenderTarget(behaviour.renderTexture);
                    //cmd.ClearRenderTarget(true, true, Color.clear); should not clear

                    // Blit the sprite through the material into the renderTexture
                    //cmd.Blit(behaviour.renderTexture, behaviour.renderTexture, blitMaterial);
                    
                    //cmd.DrawRenderer(behaviour.meshRenderer, blitMaterial);
                    cmd.DrawMesh(behaviour.meshFilter.sharedMesh,behaviour.orthographicCamera.worldToCameraMatrix,blitMaterial);
                }*/
            }

            // Execute and release Command Buffer
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
        
        private void SetMatrix()
        {
            //blitMaterial?.SetMatrix("_ProjectionMatrix", Camera.main.projectionMatrix);
            //blitMaterial?.SetMatrix("_ViewMatrix", Camera.main.worldToCameraMatrix) ;
            // Create a new camera projection matrix that matches the render texture's aspect ratio
            /*var renderTextureWidth = behaviour.renderTexture.width;
            var renderTextureHeight = behaviour.renderTexture.height;
            float aspectRatio = (float)renderTextureWidth / renderTextureHeight;

            Matrix4x4 projectionMatrix = Matrix4x4.Perspective(
                Camera.main.fieldOfView,
                aspectRatio,
                Camera.main.nearClipPlane,
                Camera.main.farClipPlane
            );

            Matrix4x4 viewMatrix = Camera.main.worldToCameraMatrix;
            Matrix4x4 viewProjectionMatrix = projectionMatrix * viewMatrix;

            blitMaterial.SetMatrix("_ViewProjectionMatrix", viewProjectionMatrix);*/
            
            blitMaterial.SetMatrix("_ProjectionMatrix", Camera.main.projectionMatrix);
            blitMaterial.SetMatrix("_ViewMatrix", Camera.main.worldToCameraMatrix) ;
        }
    }
}