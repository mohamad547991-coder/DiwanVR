// The earlier release removed URP variants using a hand-maintained keyword list.
// Those removals were not validated on a real Quest and can discard combinations
// needed by mobile, XR, lighting or project settings. Leave this experimental
// filter disabled by default and let Unity/URP manage its normal variant stripping.
// Re-enable only for a controlled, device-tested profiling experiment.
#if DIWAN_EXPERIMENTAL_SHADER_STRIPPING
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace DiwanVR.Editor
{
    // The Quest quality tier uses the forward mobile renderer, baked lighting,
    // one reflection probe and no SSAO, decals, cookies or DOTS renderers.
    // Keep stereo, lightmaps, material features and main-light shadow variants.
    public sealed class MobileShaderVariants : IPreprocessShaders
    {
        public int callbackOrder=>10000;
        private static readonly string[] unused={
            "FOG_LINEAR","FOG_EXP","FOG_EXP2","_SCREEN_SPACE_OCCLUSION",
            "_DBUFFER_MRT1","_DBUFFER_MRT2","_DBUFFER_MRT3","_FORWARD_PLUS",
            "_LIGHT_COOKIES","_LIGHT_LAYERS","_ADDITIONAL_LIGHT_SHADOWS",
            "DOTS_INSTANCING_ON","DYNAMICLIGHTMAP_ON","DEBUG_DISPLAY",
            "_REFLECTION_PROBE_BLENDING"
        };
        public void OnProcessShader(Shader shader,ShaderSnippetData snippet,IList<ShaderCompilerData> data)
        {
            if(EditorUserBuildSettings.activeBuildTarget!=BuildTarget.Android)return;
            if(!shader.name.StartsWith("Universal Render Pipeline/"))return;
            var keywords=new ShaderKeyword[unused.Length];
            for(int k=0;k<unused.Length;k++)keywords[k]=new ShaderKeyword(shader,unused[k]);
            for(int i=data.Count-1;i>=0;i--)
            {
                bool remove=false;
                foreach(var keyword in keywords)if(data[i].shaderKeywordSet.IsEnabled(keyword)){remove=true;break;}
                if(remove)data.RemoveAt(i);
            }
        }
    }
}

#endif
