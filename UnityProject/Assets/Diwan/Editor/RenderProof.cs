using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace DiwanVR.Editor
{
    internal static class RenderProof
    {
        // Render into our own target synchronously. This does not depend on
        // Game view focus, the editor repaint, or screenshot frame timing.
        public static bool Capture(Camera source,string path,out string detail)
        {
            if(!source){detail="No camera";return false;}
            GameObject go=null;RenderTexture target=null;Texture2D image=null;
            var previous=RenderTexture.active;
            try
            {
                go=new GameObject("ValidationRenderCamera"){hideFlags=HideFlags.HideAndDontSave};
                var camera=go.AddComponent<Camera>();camera.CopyFrom(source);camera.enabled=false;
                camera.transform.SetPositionAndRotation(source.transform.position,source.transform.rotation);
                camera.stereoTargetEye=StereoTargetEyeMask.None;
                var data=camera.GetUniversalAdditionalCameraData();data.allowXRRendering=false;data.renderPostProcessing=false;
                target=new RenderTexture(960,600,24,RenderTextureFormat.ARGB32){antiAliasing=1};target.Create();
                RenderTexture.active=target;GL.Clear(true,true,Color.magenta);RenderTexture.active=previous;
                if(GraphicsSettings.currentRenderPipeline)
                {
                    var request=new UniversalRenderPipeline.SingleCameraRequest{destination=target};
                    if(!RenderPipeline.SupportsRenderRequest(camera,request))throw new InvalidOperationException("Pipeline cannot render a validation frame.");
                    RenderPipeline.SubmitRenderRequest(camera,request);
                }
                else {camera.targetTexture=target;camera.Render();camera.targetTexture=null;}
                RenderTexture.active=target;
                image=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
                image.ReadPixels(new Rect(0,0,target.width,target.height),0,0);image.Apply();
                Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllBytes(path,image.EncodeToPNG());
                return HasSceneDetail(image.GetPixels32(),out detail);
            }
            catch(Exception error){detail=error.ToString();return false;}
            finally
            {
                RenderTexture.active=previous;
                if(image)UnityEngine.Object.DestroyImmediate(image);
                if(target){target.Release();UnityEngine.Object.DestroyImmediate(target);}
                if(go)UnityEngine.Object.DestroyImmediate(go);
            }
        }

        internal static bool HasSceneDetail(Color32[] pixels,out string detail)
        {
            if(pixels==null||pixels.Length==0){detail="Empty image";return false;}
            var histogram=new Dictionary<int,int>();int dominant=0;
            foreach(var p in pixels)
            {
                int key=(p.r>>4)*256+(p.g>>4)*16+(p.b>>4);
                histogram.TryGetValue(key,out int count);histogram[key]=++count;dominant=Math.Max(dominant,count);
            }
            float fraction=(float)dominant/pixels.Length;
            detail=$"Color bins: {histogram.Count}; largest color region: {fraction:P1}. Desktop image only; headset untested.";
            // Reject blank/white/cyan/error frames; this is a smoke test, not
            // a claim that stereo rendering, every object or text is correct.
            return histogram.Count>=24 && fraction<.85f;
        }
    }
}
