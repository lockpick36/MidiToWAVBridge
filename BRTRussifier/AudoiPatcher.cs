using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace BRTRussifier
{
    // Token: 0x02000005 RID: 5
    [HarmonyPatch(typeof(AudioSource), "PlayOneShot", new Type[]
    {
        typeof(AudioClip),
        typeof(float)
    })]
    internal class AudioSource_PlayOneShot_Patch
    {
        // Token: 0x06000005 RID: 5 RVA: 0x00002094 File Offset: 0x00000294

        private static void Prefix(ref AudioClip clip)
        {
            bool flag = clip == null;
            if (!flag)
            {
                AudioClip audioClip;
                bool flag2 = BRTRussifierPlugin.AudioCache.TryGetValue(clip.name, out audioClip);
                if (flag2)
                {
                    clip = audioClip;
                }
            }
        }
    }
    [HarmonyPatch(typeof(AudioSource), "set_clip")]
    internal class AudioSource_SetClip_Patch
    {
        // Token: 0x06000007 RID: 7 RVA: 0x000020D8 File Offset: 0x000002D8
        private static void Prefix(ref AudioClip value)
        {
            bool flag = value == null;
            if (!flag)
            {
                AudioClip audioClip;
                bool flag2 = BRTRussifierPlugin.AudioCache.TryGetValue(value.name, out audioClip);
                if (flag2)
                {
                    value = audioClip;
                }
            }
        }
    }
}
