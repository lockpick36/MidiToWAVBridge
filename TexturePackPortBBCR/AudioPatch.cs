using HarmonyLib;
using UnityEngine;

namespace TexturePackPortBBCR
{
    // Патчим основной метод проигрывания звуков
    [HarmonyPatch(typeof(AudioSource), "PlayOneShot", typeof(AudioClip), typeof(float))]
    class AudioSource_PlayOneShot_Patch
    {
        static void Prefix(ref AudioClip clip)
        {
            if (clip == null) return;

            // Если в нашем кэше есть звук с таким же именем, как у игры
            if (TexturePackPlugin.AudioCache.TryGetValue(clip.name, out AudioClip customClip))
            {
                // Подменяем оригинал на наш файл перед проигрыванием
                clip = customClip;
            }
        }
    }

    // Патчим установку клипа в компонент (например, для фоновой музыки или длинных фраз)
    [HarmonyPatch(typeof(AudioSource), "set_clip")]
    class AudioSource_SetClip_Patch
    {
        static void Prefix(ref AudioClip value)
        {
            if (value == null) return;

            if (TexturePackPlugin.AudioCache.TryGetValue(value.name, out AudioClip customClip))
            {
                value = customClip;
            }
        }
    }
}