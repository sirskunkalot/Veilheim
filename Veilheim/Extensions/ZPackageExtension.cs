// Veilheim

using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using Ionic.Zlib;
using Steamworks;

namespace Veilheim.Extensions
{
    public static class ZPackageExtension
    {
        /// <summary>
        /// Read ZPackage from file
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public static ZPackage ReadFromFile(string filename)
        {
            ZPackage package;
            using (FileStream fs = File.OpenRead(filename))
            {
                using (BinaryReader br = new BinaryReader(fs))
                {
                    int count = br.ReadInt32();
                    package = new ZPackage(br.ReadBytes(count));
                }
            }

            return package;
        }

        /// <summary>
        /// Write ZPackage to file
        /// </summary>
        /// <param name="package"></param>
        /// <param name="filename"></param>
        public static void WriteToFile(this ZPackage package, string filename)
        {
            using (FileStream fs = File.Create(filename))
            {
                using (BinaryWriter bw = new BinaryWriter(fs))
                {
                    byte[] data = package.GetArray();
                    bw.Write(data.Length);
                    bw.Write(data);
                }
            }
        }
    }
}
