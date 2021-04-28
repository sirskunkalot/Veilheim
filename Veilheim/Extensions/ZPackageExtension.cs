// Veilheim
// a Valheim mod
// 
// File:    ZPackageExtension.cs
// Project: Veilheim

using System.IO;

namespace Veilheim.Extensions
{
    public static class ZPackageExtension
    {
        /// <summary>
        ///     Read ZPackage from file
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public static ZPackage ReadFromFile(string filename)
        {
            ZPackage package;
            using (var fs = File.OpenRead(filename))
            {
                using (var br = new BinaryReader(fs))
                {
                    var count = br.ReadInt32();
                    package = new ZPackage(br.ReadBytes(count));
                }
            }

            return package;
        }

        /// <summary>
        ///     Write ZPackage to file
        /// </summary>
        /// <param name="package"></param>
        /// <param name="filename"></param>
        public static void WriteToFile(this ZPackage package, string filename)
        {
            if (!Directory.Exists(Path.GetDirectoryName(filename)))
            {
                Jotunn.Logger.LogInfo($"Creating path {Path.GetDirectoryName(filename)}");
                Directory.CreateDirectory(Path.GetDirectoryName(filename));
            }

            using (var fs = File.Create(filename))
            {
                using (var bw = new BinaryWriter(fs))
                {
                    var data = package.GetArray();
                    bw.Write(data.Length);
                    bw.Write(data);
                }
            }
        }
    }
}