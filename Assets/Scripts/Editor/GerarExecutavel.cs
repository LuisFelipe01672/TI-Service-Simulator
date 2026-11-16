using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Gera o jogo para Windows (um .exe que abre sem a Unity) em Builds/Windows.
// A pasta Builds/ está no .gitignore: o executável não vai para o repositório.
// O plugin Unity-MCP (ferramenta de desenvolvimento) fica de fora do jogo: a parte de runtime dele não compila
// no player (define UNITY_MCP_EXCLUDE_FROM_COMPILATION) e as DLLs que ele restaura em Assets/Plugins/NuGet
// são desmarcadas para Windows só durante o build.
public static class GerarExecutavel
{
    const string Pasta = "Builds/Windows";
    const string PastaDllsMcp = "Assets/Plugins/NuGet";

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
            extraScriptingDefines = new[] { "UNITY_MCP_EXCLUDE_FROM_COMPILATION" },
        };

        // Pasta de dados limpa: DLL que saiu do jogo não fica sobrando de um build anterior
        string dados = Path.Combine(Pasta, PlayerSettings.productName + "_Data");
        if (Directory.Exists(dados)) Directory.Delete(dados, true);

        var desligadas = DesligarDllsDoMcp();
        BuildReport relatorio;
        try { relatorio = BuildPipeline.BuildPlayer(opcoes); }
        finally { Religar(desligadas); }

        var r = relatorio.summary;
        if (r.result == BuildResult.Succeeded)
        {
            Debug.Log($"[Build] Pronto: {Path.GetFullPath(opcoes.locationPathName)} ({r.totalSize / (1024 * 1024)} MB, {r.totalTime.TotalSeconds:0} s)");
            if (!Application.isBatchMode) EditorUtility.RevealInFinder(opcoes.locationPathName);
        }
        else
            Debug.LogError($"[Build] Falhou: {r.result} ({r.totalErrors} erro(s)). Veja o Console.");
    }

    static List<PluginImporter> DesligarDllsDoMcp()
    {
        var lista = new List<PluginImporter>();
        if (!AssetDatabase.IsValidFolder(PastaDllsMcp)) return lista;
        foreach (var guid in AssetDatabase.FindAssets("", new[] { PastaDllsMcp }))
        {
            var imp = AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(guid)) as PluginImporter;
            if (imp == null || !(imp.GetCompatibleWithAnyPlatform() || imp.GetCompatibleWithPlatform(BuildTarget.StandaloneWindows64))) continue;
            imp.SetCompatibleWithAnyPlatform(false);
            imp.SetCompatibleWithEditor(true);
            imp.SetCompatibleWithPlatform(BuildTarget.StandaloneWindows64, false);
            imp.SaveAndReimport();
            lista.Add(imp);
        }
        if (lista.Count > 0) Debug.Log($"[Build] {lista.Count} DLL(s) do Unity-MCP fora do jogo neste build.");
        return lista;
    }

    static void Religar(List<PluginImporter> lista)
    {
        foreach (var imp in lista)
        {
            if (imp == null) continue;
            imp.SetCompatibleWithAnyPlatform(true);
            imp.SaveAndReimport();
        }
    }
}
