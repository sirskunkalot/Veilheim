using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Compilation;

namespace Veilheim.Editor
{
    public class BuildUtils
    {
        /// <summary>
        /// Absolute path to Solution folder
        /// </summary>
        public static readonly string k_SolutionDir = Path.Combine(Application.dataPath, "..", "..");

        public static readonly string k_LibDir = Path.Combine(k_SolutionDir, "libraries\\Unity");

        public static readonly string k_AssetDir = Path.Combine(k_SolutionDir, "Veilheim\\Assets");

        private static readonly string[] k_IgnoredAssemblies = {
            "UnityEngine.TestRunner",
            "UnityEngine.UI",
            "Unity.TextMeshPro",
            "Unity.Timeline"
        };

        /// <summary>
        /// Runs:
        /// <see cref="BuildRuntimeAssemblies" />,
        /// <see cref="BuildAssetBundles" />
        /// </summary>
        [MenuItem("Veilheim/Build All")]
        public static void BuildAll()
        {
            BuildRuntimeAssemblies();
            BuildAssetBundles();
        }

        /// <summary>
        /// Builds the required dlls and puts them in <see cref="k_LibDir" />.
        /// </summary>
        [MenuItem("Veilheim/Build Runtime Assemblies")]
        public static void BuildRuntimeAssemblies()
        {
            Assembly[] playerAssemblies = CompilationPipeline
                .GetAssemblies(AssembliesType.PlayerWithoutTestAssemblies)
                .Where(x => !k_IgnoredAssemblies.Contains(x.name))
                .ToArray();

            string stageDirName = "Build";
            string projectRootDir = Path.Combine(Application.dataPath, "..");
            string stagePath = Path.Combine(projectRootDir, stageDirName);

            var options = new BuildPlayerOptions();
            options.locationPathName = Path.Combine(stagePath, "Veilheim.exe");
            options.options = BuildOptions.BuildScriptsOnly;
            options.target = EditorUserBuildSettings.activeBuildTarget;
            BuildPipeline.BuildPlayer(options);

            //string distDir = CreateDistDirectory();
            string distDir = k_LibDir;
            foreach (var assembly in playerAssemblies)
            {
                File.Copy(
                    Path.Combine(stagePath, "Veilheim_Data", "Managed", assembly.name + ".dll"),
                    Path.Combine(distDir, assembly.name + ".dll"),
                    true
                );
            }
        }

        /// <summary>
        /// Builds asset bundles and puts them in <see cref="k_AssetDir" />.
        /// </summary>
        [MenuItem("Veilheim/Build Asset Bundles")]
        public static void BuildAssetBundles()
        {
            string stageDirName = "AssetBundles";
            string projectRootDir = Path.Combine(Application.dataPath, "..");
            string stagePath = Path.Combine(projectRootDir, stageDirName);

            BuildPipeline.BuildAssetBundles(stagePath, BuildAssetBundleOptions.None, EditorUserBuildSettings.activeBuildTarget);

            //string distDir = CreateDistDirectory();
            string distDir = k_AssetDir;
            foreach (var file in Directory.EnumerateFiles(stagePath).Where(x => !x.EndsWith(".manifest") && !x.EndsWith("AssetBundles")))
            {
                string fileName = Path.GetFileName(file);
                File.Copy(file, Path.Combine(distDir, fileName), true);
            }
        }
    }
}