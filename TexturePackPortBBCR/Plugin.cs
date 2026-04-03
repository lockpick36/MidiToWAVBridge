using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TexturePackPortBBCR
{
    [BepInPlugin("BRT.lockpick36.TexturePackSystem", "Texture Pack System", "1.1.0")]
    public class TexturePackPlugin : BaseUnityPlugin
    {
        internal static ManualLogSource Log;
        // Корень паков: BepInEx/plugins/TexturePacks
        internal static string PacksRoot => Path.Combine(Paths.PluginPath, "TexturePacks");
        public static TexturePackPlugin Instance { get; private set; }

        internal static Dictionary<string, AudioClip> AudioCache = new Dictionary<string, AudioClip>();
        private Harmony _harmony;

        private void Awake()
        {
            Log = Logger;
            Instance = this;
            _harmony = new Harmony("BRT.lockpick36.TexturePackSystem");

            // Авто-создание папки, если её нет
            if (!Directory.Exists(PacksRoot))
            {
                Directory.CreateDirectory(PacksRoot);
            }

            SceneManager.sceneLoaded += OnSceneLoaded;
            _harmony.PatchAll();

            Log.LogInfo("Система паков инициализирована.");
        }

        private void Start()
        {
            // Запускаем предзагрузку звуков из всех ВКЛЮЧЕННЫХ паков
            StartCoroutine(PreloadAllAudio());
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // Используем задержку, чтобы текстуры успели появиться в памяти
            StartCoroutine(LoadTexturesDeferred());
        }

        private IEnumerator LoadTexturesDeferred()
        {
            yield return new WaitForEndOfFrame();
            LoadAllReplacements();
        }

        public void LoadAllReplacements()
        {
            var activePacks = GetActivePacks();
            if (activePacks.Count == 0) return;

            var allTextures = Resources.FindObjectsOfTypeAll<Texture2D>();

            // Проходим по пакам в порядке приоритета (1, 2, 3...)
            foreach (var packPath in activePacks)
            {
                string texturesPath = Path.Combine(packPath, "Textures");
                if (!Directory.Exists(texturesPath)) continue;

                var files = Directory.GetFiles(texturesPath, "*.png");
                foreach (var file in files)
                {
                    ApplyTextureFromFile(file, allTextures);
                }
            }
        }

        // Логика получения активных паков из твоего примера с JSON
        public List<string> GetActivePacks()
        {
            if (!Directory.Exists(PacksRoot)) return new List<string>();

            var packDirs = Directory.GetDirectories(PacksRoot)
                                    .OrderBy(d => Path.GetFileName(d))
                                    .ToList();

            List<string> activePacks = new List<string>();

            foreach (var path in packDirs)
            {
                string jsonPath = Path.Combine(path, "Pack.json");
                if (!File.Exists(jsonPath)) continue;

                try
                {
                    string jsonContent = File.ReadAllText(jsonPath);
                    LocalizationData data = JsonUtility.FromJson<LocalizationData>(jsonContent);

                    var workItem = data.items?.FirstOrDefault(i =>
                        i.key.Equals("PackWork", StringComparison.OrdinalIgnoreCase));

                    if (workItem != null && workItem.value.Trim().Equals("true", StringComparison.OrdinalIgnoreCase))
                    {
                        activePacks.Add(path);
                    }
                }
                catch (Exception e) { Log.LogError($"Ошибка JSON в {Path.GetFileName(path)}: {e.Message}"); }
            }
            return activePacks;
        }

        private void ApplyTextureFromFile(string file, Texture2D[] engineTextures)
        {
            var fileName = Path.GetFileNameWithoutExtension(file);
            var targetTexture = engineTextures.FirstOrDefault(t => t != null && t.name == fileName);
            if (targetTexture == null) return;

            try
            {
                byte[] fileData = File.ReadAllBytes(file);
                if (targetTexture.LoadImage(fileData))
                {
                    targetTexture.filterMode = FilterMode.Point;
                    targetTexture.Apply(true);
                }
            }
            catch (Exception e) { Log.LogError($"[Textures] Ошибка {fileName}: {e.Message}"); }
        }

        private IEnumerator PreloadAllAudio()
        {
            // Очищаем кэш перед загрузкой (если нужно)
            AudioCache.Clear();
            var activePacks = GetActivePacks();

            foreach (var packPath in activePacks)
            {
                string audioPath = Path.Combine(packPath, "Audio");
                if (!Directory.Exists(audioPath)) continue;

                foreach (var file in Directory.GetFiles(audioPath, "*.wav"))
                {
                    string clipName = Path.GetFileNameWithoutExtension(file);
                    using (WWW www = new WWW("file://" + file))
                    {
                        yield return www;
                        if (string.IsNullOrEmpty(www.error))
                        {
                            AudioClip clip = www.GetAudioClip(false, false, AudioType.WAV);
                            if (clip != null)
                            {
                                clip.name = clipName;
                                // Если в разных паках один звук, "победит" тот, что в паке с большим номером
                                AudioCache[clipName] = clip;
                            }
                        }
                    }
                }
            }
            Log.LogInfo($"[Audio] Загружено: {AudioCache.Count} клипов.");
        }
    }
}