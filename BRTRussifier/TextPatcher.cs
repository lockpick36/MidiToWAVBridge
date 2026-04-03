using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BRTRussifier
{
    [HarmonyPatch(typeof(TextMeshProUGUI), "OnEnable")]
    class TMP_Master_Fixer_Patch
    {
        static void Postfix(TextMeshProUGUI __instance)
        {
            if (__instance == null) return;

            string sceneName = SceneManager.GetActiveScene().name;
            if (sceneName != "MainMenu" && sceneName != "Game") return;

            string objName = __instance.gameObject.name;

            // --- 1. ПРОВЕРКА НА ВЕРСИЮ ---
            if (objName.Equals("Version", System.StringComparison.OrdinalIgnoreCase))
            {
                ApplySafeSettings(__instance);
                UpdateTextFromLoc(__instance, "BRT_Version_Text");
            }
            // --- 2. ПРОВЕРКА НА ТИТРЫ (строим путь через IsInCredits) ---
            else if (sceneName == "MainMenu" && objName == "Text1" && IsInCredits(__instance.transform))
            {
                ApplySafeSettings(__instance);
                // Определяем, какая именно страница титров включилась, по имени родителя
                string parentName = __instance.transform.parent.name; // Это "Text"
                string grandParentName = __instance.transform.parent.parent.name; // Это "Credits_1", "Credits_2" или "Credits_3"

                if (grandParentName == "Credits_1") UpdateTextFromLoc(__instance, "BRT_Credits_1");
                else if (grandParentName == "Credits_2") UpdateTextFromLoc(__instance, "BRT_Credits_2");
                else if (grandParentName == "Credits_3") UpdateTextFromLoc(__instance, "BRT_Credits_3");
            }
            // --- 3. ПРОВЕРКА НА КНОПКИ/НАСТРОЙКИ ---
            else if (IsSettingElement(__instance.transform))
            {
                ApplySafeSettings(__instance);
            }
        }

        // Вспомогательный метод для подмены текста через твой JSON
        static void UpdateTextFromLoc(TextMeshProUGUI tmp, string key)
        {
            // Используем твой LocalizationManager
            string translated = Singleton<LocalizationManager>.Instance.GetLocalizedText(key);
            if (translated != key)
            {
                tmp.text = translated;
            }
        }

        // Фикс отображения: чтобы текст не исчезал и не сжимался
        static void ApplySafeSettings(TextMeshProUGUI tmp)
        {
            tmp.enableWordWrapping = false;
            tmp.overflowMode = TextOverflowModes.Overflow;
            tmp.enableAutoSizing = false;
            tmp.ForceMeshUpdate();
        }

        // Метод построения пути: проверяет, есть ли в родителях объект "CreditsBase"
        static bool IsInCredits(Transform t)
        {
            Transform current = t;
            while (current != null)
            {
                if (current.name == "CreditsBase") return true;
                current = current.parent;
            }
            return false;
        }

        // Проверка для элементов интерфейса
        static bool IsSettingElement(Transform t)
        {
            Transform current = t;
            while (current != null)
            {
                string name = current.name.ToLower();
                if (name.Contains("options") || name.Contains("graphics") ||
                    name.Contains("audio") || name.Contains("general") ||
                    name.Contains("button") || name.Contains("toggle") ||
                    name.Contains("controls"))
                {
                    return true;
                }
                current = current.parent;
            }
            return false;
        }
    }
}