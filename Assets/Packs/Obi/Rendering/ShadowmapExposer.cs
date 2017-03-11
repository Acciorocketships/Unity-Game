using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace Obi{
public class ShadowmapExposer : MonoBehaviour
{
     CommandBuffer m_afterShadowPass = null;
	 public ObiParticleRenderer meshRenderer;
 
     // Use this for initialization
     void Start ()
     {
          m_afterShadowPass = new CommandBuffer();
          m_afterShadowPass.name = "Shadowmap Expose";
 
          //The name of the shadowmap for this light will be "MyShadowMap"
          m_afterShadowPass.SetGlobalTexture ("_MyShadowMap", new RenderTargetIdentifier(BuiltinRenderTextureType.CurrentActive));
		 
          Light light = GetComponent<Light>();
          if (light)
          {
               //add command buffer right after the shadowmap has been renderered
               light.AddCommandBuffer (UnityEngine.Rendering.LightEvent.AfterShadowMap, m_afterShadowPass);
          }
 
     }
}
}
