// Veilheim
// a Valheim mod
// 
// File:    AssetManager.cs
// Project: Veilheim

using UnityEngine;

namespace Veilheim.AssetManagers
{
    /// <summary>
    /// The base class for all the library's various Managers
    /// </summary>
    public abstract class AssetManager : MonoBehaviour
    {
        /// <summary>
        /// Initialize manager class after all manager scripts have been added to the root game object
        /// </summary>
        internal virtual void Init() { }
    }
}
