using System;
using System.Linq;
using TMPro;
using UnityEngine;

namespace MTM101BaldAPI
{
    public class MTM101BaldiDevAPI : MonoBehaviour
    {
        public static AssetMan AssetMan = new AssetMan();
    }

    public class AssetMan
    {
        public T Get<T>(string name) where T : UnityEngine.Object
        {
            T asset = Resources.FindObjectsOfTypeAll<T>().FirstOrDefault(x => x.name == name);
            if (asset != null) return asset;

            if (typeof(T) == typeof(Sprite) && name.Contains("Arrow"))
            {
                var allSprites = Resources.FindObjectsOfTypeAll<Sprite>()
                                          .Where(x => x.name.Contains("MenuArrowSheet"))
                                          .OrderBy(x => x.name).ToList();

                if (allSprites.Count >= 4)
                {
                    // ИНВЕРТИРУЕМ: 
                    // 0 и 1 обычно ТУСКЛЫЕ (Normal)
                    // 2 и 3 обычно ЯРКИЕ (Highlight)

                    if (name.Contains("Left"))
                    {
                        // Если в имени есть Highlight — берем яркий (2), иначе тусклый (0)
                        return (name.Contains("Highlight") ? allSprites[2] : allSprites[0]) as T;
                    }

                    if (name.Contains("Right"))
                    {
                        // Если в имени есть Highlight — берем яркий (3), иначе тусклый (1)
                        return (name.Contains("Highlight") ? allSprites[3] : allSprites[1]) as T;
                    }
                }
            }
            return Resources.FindObjectsOfTypeAll<T>().FirstOrDefault(x => x.name.Contains(name)) as T;
        }
    }
}