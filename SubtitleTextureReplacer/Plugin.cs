using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using MTM101BaldAPI.OptionsAPI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Subtitles
{
    [BepInPlugin("brt.lockpick36.replacersubtitletexture", "Replacer Subtitle Texture", "1.1.0")]
    [BepInProcess("BALDI.exe")]
    public class Subtitles : BaseUnityPlugin
    {
        public static Subtitles Instance;
        public static Sprite customSprite;
        public static string texturesPath;
        public static string modFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        public static string resourcesPath = Path.Combine(modFolder, "Resources"); // Папка с фонами

        // Конфиги
        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<string> selectedTexture;
        public static List<string> textureFiles = new List<string>(); // Список путей к файлам
        public static ConfigEntry<string> currentTexture; // Текущая выбранная текстура в конфиге
        public static ConfigEntry<float> transparency; // Прозрачность

        private void Awake()
        {
            Instance = this;

            // Настройка путей
            texturesPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "textures");
            if (!Directory.Exists(texturesPath)) Directory.CreateDirectory(texturesPath);

            // Регистрация настроек
            modEnabled = Config.Bind("General", "Enabled", true, "Enable custom subtitle texture");
            selectedTexture = Config.Bind("Texture", "TextureName", "subtitle_bg.png", "Current PNG file");
            transparency = Config.Bind("Texture", "Transparency", 0.9f, "Alpha");

            LoadTexture();

            var harmony = new Harmony("brt.lockpick36.replacersubtitletexture");
            harmony.PatchAll();

            // ПРИНУДИТЕЛЬНАЯ РЕГИСТРАЦИЯ (независимо от внешних API)
            // Мы подписываемся на статический обработчик твоего внутреннего CustomOptionsCore
            CustomOptionsCore.OnMenuInitialize += (menu, handler) =>
            {
                handler.AddCategory<SubtitleOptionsPage>("Subtitles");
                Logger.LogInfo("Subtitle Category Forced Registration Success!");
            };

            Logger.LogInfo("Plugin Awake Finished");
        }

        public void LoadTexture()
        {
            string path = Path.Combine(texturesPath, selectedTexture.Value);
            if (!File.Exists(path)) return;

            try
            {
                byte[] fileData = File.ReadAllBytes(path);
                Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (ImageConversion.LoadImage(tex, fileData))
                {
                    tex.filterMode = FilterMode.Point;
                    Color[] pixels = tex.GetPixels();
                    for (int i = 0; i < pixels.Length; i++) pixels[i].a *= transparency.Value;
                    tex.SetPixels(pixels);
                    tex.Apply();

                    customSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
                }
            }
            catch (Exception e) { Logger.LogError(e.Message); }
        }

        public static void ApplyTo(SubtitleController controller)
        {
            if (!modEnabled.Value || controller?.bg == null || customSprite == null) return;
            controller.bg.sprite = customSprite;
            controller.bg.color = Color.white;
            controller.bg.type = Image.Type.Simple;
        }
        public static Sprite LoadCustomSprite(string fileName)
        {
            string path = Path.Combine(Subtitles.resourcesPath, fileName);
            if (!File.Exists(path)) return null;

            byte[] data = File.ReadAllBytes(path);
            Texture2D tex = new Texture2D(2, 2);
            tex.LoadImage(data);
            tex.filterMode = FilterMode.Point;

            // ВАЖНО: Последний аргумент (PPU) ставим 1.0f
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 1.0f);
        }
    }

    // Патчи
    [HarmonyPatch(typeof(SubtitleController), "Initialize")]
    class PatchInit { static void Postfix(SubtitleController __instance) => Subtitles.ApplyTo(__instance); }

    [HarmonyPatch(typeof(SubtitleController), "Update")]
    class PatchUpdate
    {
        static void Postfix(SubtitleController __instance)
        {
            if (__instance.bg != null && __instance.bg.sprite != Subtitles.customSprite) Subtitles.ApplyTo(__instance);
        }
    }

}