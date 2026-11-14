using System.IO;
using UnityEditor;
using UnityEngine;

// Ferramenta de desenvolvimento: executa um item de menu da Unity escrito num arquivo, sem precisar clicar.
// Escreva o caminho do menu (ex.: "TI Service/Teste gráfico/1. Montar cômodo de prova") em
// Temp/comando_unity.txt; o editor executa e apaga o arquivo. Resultado vai para o Console/Editor.log.
[InitializeOnLoad]
static class ComandoExterno
{
    static readonly string Arquivo = Path.Combine(Path.GetDirectoryName(Application.dataPath), "Temp", "comando_unity.txt");
    static double proxima;

    static ComandoExterno() => EditorApplication.update += Verificar;

    static void Verificar()
    {
        if (EditorApplication.timeSinceStartup < proxima) return;
        proxima = EditorApplication.timeSinceStartup + 1.0;
        if (!File.Exists(Arquivo) || EditorApplication.isCompiling || EditorApplication.isUpdating) return;

        string menu;
        try { menu = File.ReadAllText(Arquivo).Trim(); File.Delete(Arquivo); }
        catch { return; }
        if (string.IsNullOrEmpty(menu)) return;

        bool ok = EditorApplication.ExecuteMenuItem(menu);
        Debug.Log($"[ComandoExterno] '{menu}' → {(ok ? "executado" : "menu não encontrado")}");
    }
}
