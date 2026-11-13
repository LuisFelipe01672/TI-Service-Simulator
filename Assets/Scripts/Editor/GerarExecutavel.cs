using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Gera o jogo para Windows (um .exe que abre sem a Unity) em Builds/Windows.
// A pasta Builds/ está no .gitignore: o executável não vai para o repositório.
public static class GerarExecutavel
{
    const string Pasta = "Builds/Windows";

    [MenuItem("TI Service/4. Gerar executável (Windows)", false, 1100)]
    public static void Gerar()
    {
        var cenas = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        if (cenas.Length == 0)
        {
            EditorUtility.DisplayDialog("Gerar executável", "Nenhuma cena marcada no Build Profiles.", "OK");
            return;
        }

        var opcoes = new BuildPlayerOptions
        {
            scenes = cenas,
            locationPathName = Path.Combine(Pasta, PlayerSettings.productName + ".exe"),
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None,
        };

        var relatorio = BuildPipeline.BuildPlayer(opcoes);
        var r = relatorio.summary;
        if (r.result == BuildResult.Succeeded)
        {
            Debug.Log($"[Build] Pronto: {Path.GetFullPath(opcoes.locationPathName)} ({r.totalSize / (1024 * 1024)} MB, {r.totalTime.TotalSeconds:0} s)");
            if (!Application.isBatchMode) EditorUtility.RevealInFinder(opcoes.locationPathName);
        }
        else
            Debug.LogError($"[Build] Falhou: {r.result} ({r.totalErrors} erro(s)). Veja o Console.");
    }
}
