using HarmonyLib;
using UnityEngine;
using System;

namespace TexturePackPortBBCR
{
    [HarmonyPatch(typeof(Application), "CallLogCallback")] // Патчим самый глубокий уровень логов Unity
    class UnityErrorFilter_Patch
    {
        static bool Prefix(string logString, LogType type)
        {
            // Если это ошибка и она содержит текст про нечитаемую текстуру
            if (type == LogType.Error && logString.Contains("is not readable"))
            {
                // Если в строке упоминаются твои текстуры или просто общая ошибка доступа к памяти
                if (logString.Contains("texture memory can not be accessed"))
                {
                    // return false отменяет вывод этого конкретного сообщения в консоль BepInEx
                    return false;
                }
            }
            return true; // Все остальные логи пропускаем
        }
    }
}