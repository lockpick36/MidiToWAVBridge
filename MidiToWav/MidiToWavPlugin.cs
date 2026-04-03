using BepInEx;
using HarmonyLib;
using MidiPlayerTK;
using MTM101BaldAPI.AssetTools;
using System.IO;
using System.Collections;
using UnityEngine;

namespace MidiToWav
{
    [BepInPlugin("brt.lockpick36.miditowav", "Midi To Wav Replacer", "1.2.0")]
    public class WavReplacerPlugin : BaseUnityPlugin
    {
        public static AssetManager WavAssets = new AssetManager();

        void Awake()
        {
            LoadWavs();
            new Harmony("brt.lockpick36.miditowav").PatchAll();
            Logger.LogInfo("WavReplacer 1.2.0: Режим 'Агрессивный лифт' активирован.");
        }

        void LoadWavs()
        {
            string modPath = AssetLoader.GetModPath(this);
            if (!Directory.Exists(modPath)) return;
            foreach (string filePath in Directory.GetFiles(modPath, "*.wav", SearchOption.AllDirectories))
            {
                WavAssets.Add<AudioClip>(Path.GetFileNameWithoutExtension(filePath), AssetLoader.AudioClipFromFile(filePath));
            }
        }
    }

    public class WavSyncController : MonoBehaviour
    {
        public MidiFilePlayer midiPlayer;
        public AudioSource audioSource;
        private string lastMidiName = "";
        private bool wasPlaying = false;

        void Update()
        {
            if (midiPlayer == null || audioSource == null) return;

            string currentMidi = midiPlayer.MPTK_MidiName;
            AudioClip custom = WavReplacerPlugin.WavAssets.Get<AudioClip>(currentMidi);

            if (custom != null)
            {
                if (midiPlayer.MPTK_IsPlaying)
                {
                    if (!wasPlaying || lastMidiName != currentMidi)
                    {
                        audioSource.clip = custom;
                        audioSource.loop = midiPlayer.MPTK_Loop;
                        audioSource.time = (currentMidi == "titleFixed" && wasPlaying) ? audioSource.time : 0;
                        audioSource.Play();
                        lastMidiName = currentMidi;
                        wasPlaying = true;
                    }

                    if (midiPlayer.MPTK_Volume > 0f)
                    {
                        audioSource.volume = midiPlayer.MPTK_Volume;
                        midiPlayer.MPTK_Volume = 0f;
                    }
                }
                else if (wasPlaying && currentMidi != "titleFixed")
                {
                    audioSource.Stop();
                    wasPlaying = false;
                    lastMidiName = "";
                }
            }
            else if (audioSource.isPlaying)
            {
                audioSource.Stop();
                wasPlaying = false;
            }
        }
    }

    [HarmonyPatch(typeof(MidiFilePlayer), "Awake")]
    internal class PatchMidiAwake
    {
        private static void Postfix(MidiFilePlayer __instance)
        {
            if (__instance.gameObject.GetComponentInChildren<WavSyncController>() == null)
            {
                GameObject proxy = new GameObject("WavReplacer_Proxy");
                proxy.transform.SetParent(__instance.transform);
                var controller = proxy.AddComponent<WavSyncController>();
                controller.midiPlayer = __instance;
                controller.audioSource = proxy.AddComponent<AudioSource>();
                controller.audioSource.playOnAwake = false;
                controller.audioSource.spatialBlend = 0f;
                controller.audioSource.ignoreListenerPause = true; // Важно для загрузочных экранов
            }
        }
    }

    // ИСПРАВЛЕНИЕ ЛИФТА 1: Запуск музыки в самом начале Initialize
    [HarmonyPatch(typeof(ElevatorScreen), "Initialize")]
    internal class PatchElevatorEarlyStart
    {
        private static void Prefix()
        {
            if (MusicManager.Instance != null)
            {
                // Принудительно запускаем "Elevator" прямо сейчас
                MusicManager.Instance.PlayMidi("Elevator", true);
                Debug.Log("WavReplacer: Музыка лифта вызвана в Initialize Prefix");
            }
        }
    }

    // ИСПРАВЛЕНИЕ ЛИФТА 2: Защита от прерывания в Results
    [HarmonyPatch(typeof(ElevatorScreen), "Results")]
    internal class PatchElevatorResultsFix
    {
        private static void Prefix()
        {
            if (MusicManager.Instance != null && !MusicManager.Instance.MidiPlayer.MPTK_IsPlaying)
            {
                MusicManager.Instance.PlayMidi("Elevator", true);
            }
        }
    }

    // ИСПРАВЛЕНИЕ ЛИФТА 3: Убираем оригинальный вызов PlayMidi из ZoomIntro, чтобы он не перезапускал музыку
    // Мы используем Transpiler или просто подавляем повторный вызов через проверку IsPlaying в коде синхронизации
}