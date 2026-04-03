using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

namespace BRTRussifier
{
    [BepInPlugin("BRT.lockpick36.Russifier", "BRT Russifier", "1.5.0")]
    public class BRTRussifierPlugin : BaseUnityPlugin
    {
        public static BRTRussifierPlugin Instance;
        internal static ManualLogSource Log;
        public static Dictionary<string, AudioClip> AudioCache = new Dictionary<string, AudioClip>();

        internal static string ModPath => Path.Combine(Paths.PluginPath, "BRTRussifier");
        internal static string TexturesPath => Path.Combine(ModPath, "Textures");
        internal static string AudioPath => Path.Combine(ModPath, "Audio");
        internal static string TranslationFilePath => Path.Combine(ModPath, "translation_memory_ru.txt");

        private void Awake()
        {
            Instance = this;
            Log = base.Logger;

            if (!Directory.Exists(TexturesPath)) Directory.CreateDirectory(TexturesPath);
            if (!Directory.Exists(AudioPath)) Directory.CreateDirectory(AudioPath);

            GameObject core = new GameObject("BRT_Core_Systems");
            DontDestroyOnLoad(core);

            SceneManager.sceneLoaded += (s, m) => LoadTextures();
            new Harmony("BRT.lockpick36.Russifier").PatchAll();

            Log.LogInfo("BRT Russifier v1.5.0: Загружен (Ollama/Gemma Mode).");
        }

        private void Start() => StartCoroutine(PreloadAllAudio());

        private void LoadTextures()
        {
            if (!Directory.Exists(TexturesPath)) return;
            string[] files = Directory.GetFiles(TexturesPath, "*.png");
            Texture2D[] allTextures = Resources.FindObjectsOfTypeAll<Texture2D>();

            foreach (string path in files)
            {
                string fileName = Path.GetFileNameWithoutExtension(path);
                Texture2D target = allTextures.FirstOrDefault(t => t != null && t.name == fileName);
                if (target != null)
                {
                    try
                    {
                        byte[] data = File.ReadAllBytes(path);
                        if (ImageConversion.LoadImage(target, data))
                        {
                            target.filterMode = FilterMode.Point;
                            target.Apply(true);
                        }
                    }
                    catch (Exception e) { Log.LogError($"Texture Error: {e.Message}"); }
                }
            }
        }

        private IEnumerator PreloadAllAudio()
        {
            if (!Directory.Exists(AudioPath)) yield break;
            foreach (string path in Directory.GetFiles(AudioPath, "*.wav"))
            {
                string name = Path.GetFileNameWithoutExtension(path);
                using (WWW www = new WWW("file://" + path))
                {
                    yield return www;
                    if (string.IsNullOrEmpty(www.error))
                    {
                        AudioClip clip = www.GetAudioClip(false, false, AudioType.WAV);
                        if (clip != null)
                        {
                            clip.name = name;
                            AudioCache[name] = clip;
                        }
                    }
                }
            }
        }
    }
}