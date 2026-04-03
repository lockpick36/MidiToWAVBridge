using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace BRTRussifier
{

    [HarmonyPatch(typeof(LocalizationManager), "LoadLocalizedText")]
    class LocalizationManager_LoadPath_Patch
    {
        public static HashSet<string> ManualTranslations = new HashSet<string>();

        // ВАЖНО: Добавляем в параметры ___localizedText, чтобы Harmony его нашел
        static void Prefix(ref string fileName, ref Dictionary<string, string> ___localizedText)
        {
            if (fileName == "Subtitles_En.json")
            {
                string customPath = Path.Combine(BepInEx.Paths.PluginPath, "BRTRussifier", "Localization", "Subtitles_Ru.json");

                if (File.Exists(customPath))
                {
                    // 1. Читаем и парсим файл (это создает ту самую loadedData)
                    string jsonContent = File.ReadAllText(customPath);
                    LocalizationData loadedData = JsonUtility.FromJson<LocalizationData>(jsonContent);

                    // 2. Убеждаемся, что словарь в игре инициализирован
                    if (___localizedText == null) ___localizedText = new Dictionary<string, string>();

                    // 3. Заполняем данными
                    if (loadedData != null && loadedData.items != null)
                    {
                        foreach (LocalizationItem item in loadedData.items)
                        {
                            ___localizedText[item.key] = item.value;
                            // ЗАПОМИНАЕМ: этот текст — наш, его НЕЛЬЗЯ переводить авто-переводчиком
                            ManualTranslations.Add(item.value);
                        }
                        Debug.Log($"[BRT] Загружено {loadedData.items.Length} ручных субтитров.");
                    }

                    // 4. Перенаправляем путь (старая логика)
                    string streamingPath = Application.streamingAssetsPath;
                    Uri fullPathUri = new Uri(customPath);
                    Uri streamingUri = new Uri(streamingPath + Path.DirectorySeparatorChar);
                    fileName = streamingUri.MakeRelativeUri(fullPathUri).ToString().Replace('/', Path.DirectorySeparatorChar);
                }
            }
        }
    }

    [HarmonyPatch(typeof(LocalizationManager), "GetLocalizedText", new Type[] { typeof(string), typeof(bool) })]
    class LocalizationManager_GetLocalizedText_Patch
    {
        static bool Prefix(string key, ref string __result, Dictionary<string, string> ___localizedText)
        {
            if (___localizedText != null && ___localizedText.TryGetValue(key, out string translated))
            {
                __result = translated;
                return false;
            }
            return true;
        }
    }
}