using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using Object = UnityEngine.Object;
using System.Text.RegularExpressions;

namespace Lens.Framework.Tools.FillTransparentPixels {
    public class FillTransparentPixels {
        [MenuItem("Assets/FillTransparentPixels", false, 103)]
        public static void Main() {
            Object[] objs = Selection.GetFiltered(typeof(Object), SelectionMode.Assets); //过滤选中的对象,这里只筛选Assets类型的
            foreach (Object obj in objs) { //遍历筛选过后选中的内容
                string path = AssetDatabase.GetAssetPath(obj); //获取obj的路径
                if (string.IsNullOrEmpty(path) == true) {
                    return;
                }
                Debug.Log("<color=#00ff00>Format Texture</color>, Selected Path: " + path);
                if (path.EndsWith(".png") || path.EndsWith(".jpg")) {
                    //选择的路径包含了.png字段
                    FormatTexture(path);
                }
            }
        }

        private static void FormatTexture(string path) {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter; //获取TextureImporter
            if (importer == null) { //不是图片资源就报错,找不到TextureImporter
                Debug.LogError("选择了非图片的资源, 资源路径 = " + path);
                return;
            }
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 1;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.isReadable = true;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.SetPlatformTextureSettings(CreateImporterSetting("Android", 2048, TextureImporterFormat.ASTC_4x4));
            importer.SetPlatformTextureSettings(CreateImporterSetting("iPhone", 2048, TextureImporterFormat.ASTC_4x4));
            importer.SetPlatformTextureSettings(CreateImporterSetting("Standalone", 2048, TextureImporterFormat.DXT5));
            importer.SaveAndReimport();
            Texture2D texture = AssetDatabase.LoadAssetAtPath(path, typeof(Texture2D)) as Texture2D;
            Texture2D formatTexture = null;
            TextureClamper(texture, out formatTexture);
            AtlasWrite(formatTexture, path);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate); //导入数据
        }

        public static TextureImporterPlatformSettings CreateImporterSetting(string name, int maxSize, TextureImporterFormat format, int compressionQuality = 50, bool allowsAlphaSplitting = false, TextureImporterCompression tc = TextureImporterCompression.Uncompressed) {
            //Debug.Log(format);
            TextureImporterPlatformSettings tips = new TextureImporterPlatformSettings();
            tips.overridden = true;
            tips.name = name;
            tips.maxTextureSize = maxSize;
            tips.format = format;
            tips.textureCompression = tc;
            tips.allowsAlphaSplitting = allowsAlphaSplitting;
            tips.compressionQuality = compressionQuality;
            return tips;
        }

        public static void TextureClamper(Texture2D sourceTexture, out Texture2D formatTexture) {
            int sourceWidth = sourceTexture.width; //贴图宽高
            int sourceHeight = sourceTexture.height;
            Color32[] sourcePixels = sourceTexture.GetPixels32();
            int targetWidth = sourceWidth + 2;
            int targetHeight = sourceHeight + 2;
            Color32[] targetPixels = new Color32[targetWidth * targetHeight]; //像素数组
            Texture2D targetTexture = new Texture2D(targetWidth, targetHeight);
            for (int i = 0; i < sourceHeight; i++) {
                for (int j = 0; j < sourceWidth; j++) {
                    targetPixels[(i + 1) * targetWidth + (j + 1)] = sourcePixels[i * sourceWidth + j]; //这一步将源贴图的像素映射到了目标生成贴图的最中心,即外围包裹2 pixel
                }
            }
            //上下左右四周各补上空白像素
            //左边缘
            Color32 clearColor = new Color32(1, 1, 1, 0);
            for (int v = 0; v < sourceHeight; v++) {
                for (int k = 0; k < 1; k++) {
                    targetPixels[(v + 1) * targetWidth + k] = clearColor;
                }
            }
            //右边缘
            for (int v = 0; v < sourceHeight; v++) {
                for (int k = 0; k < 1; k++) {
                    targetPixels[(v + 1) * targetWidth + (sourceWidth + 1 + k)] = clearColor;
                }
            }
            //上边缘
            for (int h = 0; h < sourceWidth; h++) {
                for (int k = 0; k < 1; k++) {
                    targetPixels[(sourceHeight + 1 + k) * targetWidth + 1 + h] = clearColor;
                }
            }
            //下边缘
            for (int h = 0; h < sourceWidth; h++) {
                for (int k = 0; k < 1; k++) {
                    targetPixels[k * targetWidth + 1 + h] = clearColor;
                }
            }
            targetTexture.SetPixels32(targetPixels); //为贴图设置像素信息,自动将一维的像素数组转化成二维的贴图信息数组
            targetTexture.Apply(); //实际应用任何先前的 SetPixel 和 SetPixels 更改,将贴图数据进行应用
            formatTexture = targetTexture; //返回targetTexture贴图数据
        }

        public static void AtlasWrite(Texture2D atlas, string path) {
            byte[] pngData = atlas.EncodeToPNG();
            string pngPath = Application.dataPath + path.Replace("Assets", "");
            File.WriteAllBytes(pngPath, pngData);
            LogAtlasSize(atlas, path);
        }

        public const int ATLAS_MAX_SIZE = 2048;
        public const int FAVOR_ATLAS_SIZE = 2048;
        private static void LogAtlasSize(Texture2D atlas, string path) {
            if (atlas.width > FAVOR_ATLAS_SIZE || atlas.height > FAVOR_ATLAS_SIZE) {
                Debug.Log(string.Format("<color=#ff0000>【警告】图集宽度或高度超过2048像素： {0} </color>", path));
            } else {
                Debug.Log(string.Format("<color=#13ffe7>图集 {0} 尺寸为： {1}x{2}</color>", path, atlas.width, atlas.height));
            }
        }
    }
}