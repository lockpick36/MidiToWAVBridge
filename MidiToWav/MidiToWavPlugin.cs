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
        // Centralized asset manager to store and retrieve AudioClips by their MIDI names
        public static AssetManager WavAssets = new AssetManager();

        void Awake()
        {
            // Initialize asset loading sequence
            LoadWavs();
            
            // Apply all Harmony patches to intercept game methods
            new Harmony("brt.lockpick36.miditowav").PatchAll();
            
            Logger.LogInfo("WavReplacer 1.2.0: 'Aggressive Elevator' mode activated.");
        }

        void LoadWavs()
        {
            // Resolve the physical path of the mod folder
            string modPath = AssetLoader.GetModPath(this);
            if (!Directory.Exists(modPath)) return;

            // Recursively scan for .wav files and register them into the AssetManager
            // File names must match the internal MIDI names (e.g., "Elevator.wav")
            foreach (string filePath in Directory.GetFiles(modPath, "*.wav", SearchOption.AllDirectories))
            {
                WavAssets.Add<AudioClip>(Path.GetFileNameWithoutExtension(filePath), AssetLoader.AudioClipFromFile(filePath));
            }
        }
    }

    /// <summary>
    /// Core synchronization controller attached to every MidiFilePlayer instance.
    /// Manages the transition between MIDI triggers and WAV playback.
    /// </summary>
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

            // If a replacement WAV exists for the current MIDI track
            if (custom != null)
            {
                if (midiPlayer.MPTK_IsPlaying)
                {
                    // Trigger playback if the MIDI started playing or the track has changed
                    if (!wasPlaying || lastMidiName != currentMidi)
                    {
                        audioSource.clip = custom;
                        audioSource.loop = midiPlayer.MPTK_Loop;
                        
                        // Special handling for title screen music persistence
                        audioSource.time = (currentMidi == "titleFixed" && wasPlaying) ? audioSource.time : 0;
                        
                        audioSource.Play();
                        lastMidiName = currentMidi;
                        wasPlaying = true;
                    }

                    // ACTIVE VOLUME MIRRORING:
                    // We steal the volume level from the MIDI player and apply it to our AudioSource.
                    // Then we mute the MIDI player (Volume = 0) so only the WAV is audible.
                    if (midiPlayer.MPTK_Volume > 0f)
                    {
                        audioSource.volume = midiPlayer.MPTK_Volume;
                        midiPlayer.MPTK_Volume = 0f;
                    }
                }
                else if (wasPlaying && currentMidi != "titleFixed")
                {
                    // Stop WAV if the MIDI player has stopped (except for specific persistent tracks)
                    audioSource.Stop();
                    wasPlaying = false;
                    lastMidiName = "";
                }
            }
            else if (audioSource.isPlaying)
            {
                // Fallback: stop audio if no replacement is found for the active track
                audioSource.Stop();
                wasPlaying = false;
            }
        }
    }

    /// <summary>
    /// Intercepts the initialization of any MIDI player in the game.
    /// </summary>
    [HarmonyPatch(typeof(MidiFilePlayer), "Awake")]
    internal class PatchMidiAwake
    {
        private static void Postfix(MidiFilePlayer __instance)
        {
            // Attach our proxy controller to the MIDI player's GameObject if not already present
            if (__instance.gameObject.GetComponentInChildren<WavSyncController>() == null)
            {
                GameObject proxy = new GameObject("WavReplacer_Proxy");
                proxy.transform.SetParent(__instance.transform);
                
                var controller = proxy.AddComponent<WavSyncController>();
                controller.midiPlayer = __instance;
                
                // Configure the AudioSource for high-fidelity 2D playback
                controller.audioSource = proxy.AddComponent<AudioSource>();
                controller.audioSource.playOnAwake = false;
                controller.audioSource.spatialBlend = 0f; // Ensure music is global (non-spatial)
                
                // Essential for transitions: prevents audio from cutting out during scene loading
                controller.audioSource.ignoreListenerPause = true; 
            }
        }
    }

    // ELEVATOR FIX 1: Force-start music during the earliest phase of ElevatorScreen initialization
    [HarmonyPatch(typeof(ElevatorScreen), "Initialize")]
    internal class PatchElevatorEarlyStart
    {
        private static void Prefix()
        {
            if (MusicManager.Instance != null)
            {
                // Manually trigger the MIDI engine to ensure our SyncController catches the event early
                MusicManager.Instance.PlayMidi("Elevator", true);
                Debug.Log("WavReplacer: Elevator music forced in Initialize Prefix");
            }
        }
    }

    // ELEVATOR FIX 2: Ensure music continues or restarts during the Results screen
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
}
