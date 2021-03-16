// Veilheim
// a Valheim mod
// 
// File:    SectionStatusChangeEventArgs.cs
// Project: Veilheim

using System;

namespace Veilheim.Configurations
{
    public class SectionStatusChangeEventArgs : EventArgs
    {
        public SectionStatusChangeEventArgs(bool enabled)
        {
            Enabled = enabled;
        }

        public bool Enabled { get; set; }
    }
}