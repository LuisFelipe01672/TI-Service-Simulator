using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Copia CREDITOS.md (raiz do projeto) para Assets/Resources/Creditos.md, que a tela de créditos lê no jogo.
// Roda ao abrir/compilar o projeto e antes de cada build — edite só o CREDITOS.md da raiz.
[InitializeOnLoad]
public class SincronizarCreditos : IPreprocessBuildWithReport
{
    const string Origem = "CREDITOS.md";
    const string Destino = "Assets/Resources/Creditos.md";

    static SincronizarCreditos() => EditorApplication.delayCall += Sincronizar;

    public int callbackOrder => 0;
    public void OnPreprocessBuild(BuildReport report) => Sincronizar();

    [MenuItem("TI Service/Sincronizar créditos", priority = 1200)]
    public static void Sincronizar()
    {
        var raiz = Path.GetDirectoryName(Application.dataPath);
        var origem = Path.Combine(raiz, Origem);
        var destino = Path.Combine(raiz, Destino);
        if (!File.Exists(origem)) return;

        var texto = File.ReadAllText(origem);
        if (File.Exists(destino) && File.ReadAllText(destino) == texto) return;

        Directory.CreateDirectory(Path.GetDirectoryName(destino));
        File.WriteAllText(destino, texto);
        AssetDatabase.ImportAsset(Destino);
        Debug.Log("[Créditos] Resources/Creditos.md atualizado a partir do CREDITOS.md");
    }
}
