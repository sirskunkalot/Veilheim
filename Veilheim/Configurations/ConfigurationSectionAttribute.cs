using System;

namespace Veilheim.Configurations
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ConfigurationSectionAttribute : Attribute
    {
        public string Comment { get; set; }
        public string SinceVersion { get; set; }

        public ConfigurationSectionAttribute(string comment, string sinceVersion = "0.0.1")
        {
            Comment = comment;
            SinceVersion = sinceVersion;
        }
    }
}