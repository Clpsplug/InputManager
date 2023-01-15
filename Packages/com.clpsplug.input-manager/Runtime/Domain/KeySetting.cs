using System;
using System.Collections.Generic;
using UnityEngine;

namespace InputManager.Domain
{
    /// <summary>
    /// Extend this class to create your own Key Settings.
    /// Ideally, there should be one extended class for a set of key group.
    /// The default keymap is assigned using the Input System's "Input Asset."
    /// </summary>
    /// <typeparam name="TEnum"></typeparam>
    public abstract class KeySetting<TEnum> : ScriptableObject where TEnum : Enum
    {
        /// <summary>
        /// Input asset file name to look at, without the extension.
        /// The asset MUST be in your Resources/Input Assets folder.
        /// </summary>
        public string inputAssetName;

        /// <summary>
        /// Name of your input map.
        /// </summary>
        public string inputMapName;

        /// <summary>
        /// Set true to enable key config for this set.
        /// </summary>
        public bool overrideable;

        /// <summary>
        /// Association between the key enum for code and the action name in the Input Asset.
        /// </summary>
        public List<KeyEnumPair<TEnum>> keySettings;

        public bool IsEmpty()
        {
            return keySettings.Count == 0;
        }
    }

    /// <summary>
    /// Key config item for <see cref="KeySetting{TEnum}"/>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [Serializable]
    public class KeyEnumPair<T> where T : Enum
    {
        /// <summary>
        /// Enum member to use in the code
        /// </summary>
        public T enumKey;

        /// <summary>
        /// Input System's action name
        /// </summary>
        public string actionName;
    }
}