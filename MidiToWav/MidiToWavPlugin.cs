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
        // Using a static asset manager to keep track of our loaded clips across the entire session
        public static AssetManager WavAssets = new AssetManager();

        void Awake()
        {
            // Initializing our custom sound bank
            LoadExternalSounds();

            var harmony = new Harmony("brt.lockpick36.miditowav");
            harmony.PatchAll();

            Logger.LogInfo("WavReplacer: Build 1.2.0 loaded. 'Aggressive Elevator' logic is hot.");
        }

        void LoadExternalSounds()
        {
            string path = AssetLoader.GetModPath(this);
            if (!Directory.Exists(path))
            {
                Logger.LogWarning("WavReplacer: Mod directory not found. Music replacement will not work.");
                return;
            }

            // Scanning for wav files. We use Path.GetFileNameWithoutExtension as the key 
            // to match internal MIDI track names (e.g., 'Elevator', 'School', etc.)
            foreach (string file in Directory.GetFiles(path, "*.wav", SearchOption.AllDirectories))
            {
                WavAssets.Add<AudioClip>(Path.GetFileNameWithoutExtension(file), AssetLoader.AudioClipFromFile(file));
                // Logger.LogDebug($"[WavReplacer] Registered: {Path.GetFileName(file)}");
            }
        }
    }

    public class WavSyncController : MonoBehaviour
    {
        public MidiFilePlayer m_Player;
        public AudioSource m_Source;

        private string _lastTrack = "";
        private bool _isCurrentlyPlaying = false;

        void Update()
        {
            // Safeguard for scene transitions
            if (m_Player == null || m_Source == null) return;

            string activeMidi = m_Player.MPTK_MidiName;
            AudioClip customClip = WavReplacerPlugin.WavAssets.Get<AudioClip>(activeMidi);

            // Handle only if we have a replacement for this specific MIDI
            if (customClip != null)
            {
                if (m_Player.MPTK_IsPlaying)
                {
                    // If track changed or MIDI just started playing
                    if (!_isCurrentlyPlaying || _lastTrack != activeMidi)
                    {
                        m_Source.clip = customClip;
                        m_Source.loop = m_Player.MPTK_Loop;

                        // Patch for title music - prevents resetting when menu reloads
                        m_Source.time = (activeMidi == "titleFixed" && _isCurrentlyPlaying) ? m_Source.time : 0;

                        m_Source.Play();
                        _lastTrack = activeMidi;
                        _isCurrentlyPlaying = true;
                    }

                    // SILENT TAKEOVER LOGIC:
                    // We steal the volume from the MPTK player and apply it to our AudioSource.
                    // Then we zero out the original to prevent the "Double Sound" effect.
                    if (m_Player.MPTK_Volume > 0f)
                    {
                        m_Source.volume = m_Player.MPTK_Volume;
                        m_Player.MPTK_Volume = 0f;
                    }
                }
                else if (_isCurrentlyPlaying && activeMidi != "titleFixed")
                {
                    m_Source.Stop();
                    _isCurrentlyPlaying = false;
                    _lastTrack = "";
                }
            }
            else if (m_Source.isPlaying)
            {
                // Stop WAV if game switched to a MIDI we don't have a replacement for
                m_Source.Stop();
                _isCurrentlyPlaying = false;
            }
        }
    }

    [HarmonyPatch(typeof(MidiFilePlayer), "Awake")]
    internal class MidiAwakePatch
    {
        private static void Postfix(MidiFilePlayer __instance)
        {
            // Injecting our proxy controller into the MPTK object.
            // This ensures every MIDI source in the game is "listened" to by our mod.
            if (__instance.gameObject.GetComponentInChildren<WavSyncController>() == null)
            {
                GameObject proxyObj = new GameObject("WavReplacer_AudioBridge");
                proxyObj.transform.SetParent(__instance.transform);

                var sync = proxyObj.AddComponent<WavSyncController>();
                sync.m_Player = __instance;

                sync.m_Source = proxyObj.AddComponent<AudioSource>();
                sync.m_Source.playOnAwake = false;
                sync.m_Source.spatialBlend = 0f; // Global 2D sound for music

                // Crucial for 0.14.X: prevents music from being killed by the listener pause during loading
                sync.m_Source.ignoreListenerPause = true;
            }
        }
    }

    // --- ELEVATOR FIXES (The "Aggressive" suite) ---

    [HarmonyPatch(typeof(ElevatorScreen), "Initialize")]
    internal class ElevatorEarlyStartPatch
    {
        // Force the music to kick in early during Initialize. 
        // Original code often delays this, leading to awkward silence.
        private static void Prefix()
        {
            if (MusicManager.Instance != null)
            {
                MusicManager.Instance.PlayMidi("Elevator", true);
                // Debug.Log("WavReplacer: Forced Elevator start in Initialize.Prefix");
            }
        }
    }

    [HarmonyPatch(typeof(ElevatorScreen), "Results")]
    internal class ElevatorResultsPersistencePatch
    {
        // Make sure the music doesn't cut out when results are displayed.
        private static void Prefix()
        {
            if (MusicManager.Instance != null && !MusicManager.Instance.MidiPlayer.MPTK_IsPlaying)
            {
                MusicManager.Instance.PlayMidi("Elevator", true);
            }
        }
    }
}
