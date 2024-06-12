using System.Collections.Generic;
using UnityEngine;
using Verse;
using static System.Net.Mime.MediaTypeNames;
using Object = UnityEngine.Object;

namespace SmashTools
{
    [StaticConstructorOnStartup]
    public static class Ext_Texture
    {
        private static RenderTexture previous;

        public static bool TryReplaceInContentFinder<T>(string itemPath, T item) where T : Object
		{
            List<ModContentPack> runningModsListForReading = LoadedModManager.RunningModsListForReading;
            for (int i = runningModsListForReading.Count - 1; i >= 0; i--)
            {
                ModContentPack modContentPack = runningModsListForReading[i];
                T itemToDestroy = modContentPack.GetContentHolder<T>().Get(itemPath);
                if (itemToDestroy != null && itemToDestroy != item)
                {
                    Object.Destroy(itemToDestroy);
                    InjectIntoContentFinder(modContentPack, itemPath, item);
                    return true;
                }
            }
            return false;
        }

        private static void InjectIntoContentFinder<T>(ModContentPack modContentPack, string itemPath, T item) where T : class
		{
            modContentPack.GetContentHolder<T>().contentList[itemPath] = item;
        }

        public static Texture2D CreateReadableTexture(Texture2D source, TextureWrapMode? wrapMode = null)
        {
			RenderTexture renderTex = ConvertToRenderTex(source);
            Texture2D readableTexture = new Texture2D(source.width, source.height)
            {
                name = source.name,
                wrapMode = wrapMode ?? source.wrapMode,
			};
			readableTexture.ReadPixels(new Rect(0, 0, renderTex.width, renderTex.height), 0, 0);
			readableTexture.Apply();
			ReleaseMemory(renderTex);

            return readableTexture;
		}

        /// <summary>
        /// Change <seealso cref="TextureWrapMode"/> of <paramref name="source"/> to <paramref name="wrapMode"/>
        /// </summary>
        /// <param name="source"></param>
        /// <param name="wrapMode"></param>
        /// <returns></returns>
        public static Texture2D WrapTexture(Texture2D source, TextureWrapMode wrapMode)
        {
            if (source.isReadable)
            {
                source.wrapMode = wrapMode;
                return source;
            }
            RenderTexture renderTex = ConvertToRenderTex(source);
            Texture2D wrappedTex = CreateReadableTexture(source, wrapMode);

			wrappedTex.ReadPixels(new Rect(0, 0, renderTex.width, renderTex.height), 0, 0);
            wrappedTex.Apply(true, true);
            ReleaseMemory(renderTex);
            return wrappedTex;
        }

        /// <summary>
        /// Convert <paramref name="source"/> into a <seealso cref="RenderTexture"/>
        /// </summary>
        /// <remarks>
        /// Due to how this extension is used, it does not release the memory of the <seealso cref="RenderTexture"/>. Do not forget to release its memory when you are done using it!
        /// </remarks>
        /// <param name="source"></param>
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

        public static void ReleaseMemory(RenderTexture renderTex)
        {
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(renderTex);
        }

        /// <summary>
        /// Rotate pixels of <paramref name="source"/> by <paramref name="angle"/> (Counter-Clockwise)
        /// </summary>
        /// <param name="source"></param>
        /// <param name="angle"></param>
        public static Texture2D Rotate(this Texture2D source, float angle)
        {
            if (angle != 90 && angle != 180 && angle != 270)
            {
                Log.Error($"Unable to rotate {source.name} by angle=\"{angle}\". Angle must equal 90, 180, or 270.");
                return source;
            }
            if (source.width != source.height)
            {
                Log.Warning($"Rotating patterns with non-square dimensions may result in inaccurate conversions. Tex=\"{source.name}\" ({source.width},{source.height})");
            }
            Texture2D readableTex;
            if (source.isReadable)
            {
                readableTex = source;
            }
            else
            {
                readableTex = CreateReadableTexture(source);
            }

            int width, height;
            int rWidth, rHeight;
            width = source.width;
            height = source.height;
            rWidth = readableTex.width;
            rHeight = readableTex.height;
            Texture2D rotImage = new Texture2D(width, height)
            {
                name = source.name
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
            rotImage.Apply(true, true);
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
