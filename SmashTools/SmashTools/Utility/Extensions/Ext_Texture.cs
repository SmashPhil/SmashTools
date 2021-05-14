using System.Collections.Generic;
using UnityEngine;
using Verse;
using System;

namespace SmashTools
{
    [StaticConstructorOnStartup]
	public static class Ext_Texture
	{
        private static readonly Dictionary<Texture2D, Texture2D> wrapTexDictionary = new Dictionary<Texture2D, Texture2D>();
        private static readonly Dictionary<Pair<Texture2D, float>, Texture2D> rotatedTexDictionary = new Dictionary<Pair<Texture2D, float>, Texture2D>();
        private static RenderTexture previous;

        public static Texture2D WrapTexture(Texture2D source, TextureWrapMode wrapMode)
        {
            if (wrapTexDictionary.TryGetValue(source, out var wrappedTex))
            {
                return wrappedTex;
            }
            if (source.isReadable)
			{
                source.wrapMode = wrapMode;
                return source;
			}
            RenderTexture renderTex = ConvertToRenderTex(source);
            wrappedTex = new Texture2D(source.width, source.height)
            {
                wrapMode = wrapMode,
                name = source.name
            };
            
            wrappedTex.ReadPixels(new Rect(0, 0, renderTex.width, renderTex.height), 0, 0);
            wrappedTex.Apply();
            ReleaseMemory(renderTex);
            wrapTexDictionary.Add(source, wrappedTex);
            return wrappedTex;
        }

        public static RenderTexture ConvertToRenderTex(Texture2D source)
		{
            RenderTexture renderTex = RenderTexture.GetTemporary(
                source.width,
                source.height,
                0,
                RenderTextureFormat.Default,
                RenderTextureReadWrite.Linear);

            Graphics.Blit(source, renderTex);
            previous = RenderTexture.active;
            RenderTexture.active = renderTex;
            return renderTex;
        }

        public static Texture2D ConvertToReadableTex(Texture2D source)
		{
            RenderTexture renderTex = RenderTexture.GetTemporary(
                source.width,
                source.height,
                0,
                RenderTextureFormat.Default,
                RenderTextureReadWrite.Linear);

            Graphics.Blit(source, renderTex);
            previous = RenderTexture.active;
            RenderTexture.active = renderTex;

            Texture2D readableTex = new Texture2D(source.width, source.height)
            {
                name = source.name
            };

            readableTex.ReadPixels(new Rect(0, 0, renderTex.width, renderTex.height), 0, 0);
            readableTex.Apply();
            ReleaseMemory(renderTex);
            return readableTex;
        }

        public static void ReleaseMemory(RenderTexture renderTex)
		{
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(renderTex);
        }

        /// <summary>
        /// Rotate pixels of Texture2D by θ (Counter-Clockwise)
        /// </summary>
        /// <param name="tex"></param>
        /// <param name="angle"></param>
        public static Texture2D Rotate(this Texture2D tex, float angle)
        {
            if (angle != 90 && angle != 180 && angle != 270)
			{
                Log.Error($"Unable to rotate {tex.name} by angle=\"{angle}\". Angle must equal 90, 180, or 270.");
                return tex;
			}
            if (rotatedTexDictionary.TryGetValue(new Pair<Texture2D, float>(tex, angle), out Texture2D rotImage))
			{
                return rotImage;
			}
            if (tex.width != tex.height)
			{
                Log.Warning($"Rotating patterns with non-square dimensions may result in inaccurate conversions. Tex=\"{tex.name}\" ({tex.width},{tex.height})");
			}
            Texture2D readableTex;
            if (tex.isReadable)
            {
                readableTex = tex;
            }
			else
			{
                RenderTexture renderTex = ConvertToRenderTex(tex);
                readableTex = new Texture2D(tex.width, tex.height)
                {
                    name = tex.name
                };
                readableTex.ReadPixels(new Rect(0, 0, renderTex.width, renderTex.height), 0, 0);
                readableTex.Apply();
                ReleaseMemory(renderTex);
            }

            int width, height;
            int rWidth, rHeight;
            width = tex.width;
            height = tex.height;
            rWidth = readableTex.width;
            rHeight = readableTex.height;
            rotImage = new Texture2D(width, height)
            {
                name = tex.name
            };
            int x = 0, y = 0;
            Color32[] pix1 = rotImage.GetPixels32();
            Color32[] pix2 = readableTex.GetPixels32();
            
            Color32[] pix3 = RotateSquare(pix2, angle * Mathf.Deg2Rad, readableTex);
            for (int j = 0; j < rHeight; j++)
            {
                for (var i = 0; i < rWidth; i++)
                {
                    pix1[rotImage.width / 2 - rWidth / 2 + x + i + rotImage.width * (rotImage.height / 2 - rHeight / 2 + j + y)] = pix3[i + j * rWidth];
                }
            }
            rotImage.SetPixels32(pix1);
            rotImage.Apply();
            rotatedTexDictionary.Add(new Pair<Texture2D, float>(tex, angle), rotImage);
            return rotImage;
        }

        private static Color32[] RotateSquare(Color32[] arr, float phi, Texture2D originTexture)
        {
            int x;
            int y;
            int i;
            int j;
            float sn = Mathf.Sin(phi);
            float cs = Mathf.Cos(phi);
            Color32[] arr2 = originTexture.GetPixels32();
            int W = originTexture.width;
            int H = originTexture.height;
            int xc = W / 2;
            int yc = H / 2;
            for (j = 0; j < H; j++)
            {
                for (i = 0; i < W; i++)
                {
                    arr2[j * W + i] = new Color32(0, 0, 0, 0);
                    x = (int)(cs * (i - xc) + sn * (j - yc) + xc);
                    y = (int)(-sn * (i - xc) + cs * (j - yc) + yc);
                    if ((x > -1) && (x < W) && (y > -1) && (y < H))
                    {
                        arr2[j * W + i] = arr[y * W + x];
                    }
                }
            }
            return arr2;
        }
    }
}
