// LocalizationData.cs
using System;

namespace BRTRussifier
{
    [Serializable]
    public class LocalizationData
    {
        public LocalizationItem[] items;
    }

    [Serializable]
    public class LocalizationItem
    {
        public string key;
        public string value;
    }
}