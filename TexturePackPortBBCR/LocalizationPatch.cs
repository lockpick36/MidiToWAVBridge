using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace TexturePackPortBBCR
{
    [HarmonyPatch(typeof(LocalizationManager), "LoadLocalizedText")]
    class LocalizationManager_LoadPath_Patch
    {
        static void Prefix(ref string fileName)
        {
            if (fileName == "Subtitles_En.json")
            {
                // Ищем Subtitles.json в каждом паке
                string root = TexturePackPlugin.PacksRoot;
                if (!Directory.Exists(root)) return;

                foreach (var packDir in Directory.GetDirectories(root))
                {
                    string customPath = Path.Combine(packDir, "Localization", "Subtitles.json");
                    if (File.Exists(customPath))
                    {
                        string streamingPath = Application.streamingAssetsPath;
                        Uri fullPathUri = new Uri(customPath);
                        Uri streamingUri = new Uri(streamingPath + Path.DirectorySeparatorChar);

                        fileName = streamingUri.MakeRelativeUri(fullPathUri).ToString().Replace('/', Path.DirectorySeparatorChar);
                        Debug.Log($"[TexturePack] Перенаправление на пак: {customPath}");
                        break; // Берем первый найденный
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(LocalizationManager), "GetLocalizedText", new Type[] { typeof(string), typeof(bool) })]
    class LocalizationManager_GetLocalizedText_Patch
    {
        static bool Prefix(string key, ref string __result, Dictionary<string, string> ___localizedText)
        {
            if (___localizedText != null && ___localizedText.ContainsKey(key))
            {
                __result = ___localizedText[key];
                return false;
            }
            return true;
        }
    }
}