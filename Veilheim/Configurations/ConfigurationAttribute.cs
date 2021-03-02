using System;

namespace Veilheim.Configurations
{
    [AttributeUsage(AttributeTargets.Property)]
    public class ConfigurationAttribute : Attribute
    {
        public ConfigurationAttribute(string comment, ActivationTime activationTime, string sinceVersion = "0.0.1")
        {
            Comment = comment;
            SinceVersion = sinceVersion;
            ActivationTime = activationTime;
        }

        public string Comment { get; set; }
        public string SinceVersion { get; set; }
        public ActivationTime ActivationTime { get; set; }
    }
}