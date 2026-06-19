using UnityEditor;
using UnityEditor.Build;

namespace ObstacleTower.Editor
{
    public static class BuildUtility
    {
        // UNITY_POST_PROCESSING_STACK_V2 is no longer needed — URP 17 has built-in post-processing.
        private const string BaseSymbols = "";
        private const string EvaluationSymbol = "OTCEVALUATION";

        [MenuItem("Obstacle Tower/Automated Build")]
        public static void BuildGame()
        {
            var path = EditorUtility.SaveFolderPanel("Choose Location of Builds", "", "");
            if (path == "")
            {
                return;
            }
            _makeBuild(path, BaseSymbols, BuildTarget.StandaloneWindows);
            _makeBuild(path, BaseSymbols, BuildTarget.StandaloneOSX);
            _makeBuild(path, BaseSymbols, BuildTarget.StandaloneLinux64);
        }

        private static void _makeBuild(
            string path,
            string symbols,
            BuildTarget target
        )
        {
            var levels = new[] {"Assets/ObstacleTower/Scenes/Procedural.unity"};

            var fullPath = path + "/ObstacleTower/obstacletower";

            if (target == BuildTarget.StandaloneWindows)
            {
                fullPath += ".exe";
            }

            if (target == BuildTarget.StandaloneLinux64)
            {
                fullPath += ".x86_64";
            }

            // SetScriptingDefineSymbolsForGroup is deprecated in Unity 6; use NamedBuildTarget instead.
            PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.Standalone, symbols);
            BuildPipeline.BuildPlayer(levels, fullPath, target, BuildOptions.None);
        }
    }
}
