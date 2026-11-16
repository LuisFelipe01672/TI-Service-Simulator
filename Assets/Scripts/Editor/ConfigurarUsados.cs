using UnityEditor;
using UnityEngine;

// Etapa 6: liga os modelos 3D que o projeto já tem (impressora, roteador, switch) aos equipamentos usados do lote:
// cria/atualiza Resources/Usados/ModelosUsados.asset.
static class ConfigurarUsados
{
    const string Caminho = "Assets/Resources/Usados/ModelosUsados.asset";

    [MenuItem("TI Service/Usados: ligar modelos 3D", false, 62)]
    static void Ligar()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources/Usados")) AssetDatabase.CreateFolder("Assets/Resources", "Usados");
        var banco = AssetDatabase.LoadAssetAtPath<ModelosUsados>(Caminho);
        if (banco == null)
        {
            banco = ScriptableObject.CreateInstance<ModelosUsados>();
            AssetDatabase.CreateAsset(banco, Caminho);
        }
        banco.impressora = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Sketchfab/Impressora_MFP_Compacta.fbx");
        banco.roteador = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Sketchfab/Roteador/Roteador.gltf");
        banco.switchRede = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Sketchfab/Switch_5_Portas/Switch_5_Portas.gltf");
        EditorUtility.SetDirty(banco);
        AssetDatabase.SaveAssets();
        Debug.Log($"[Usados] Modelos ligados: impressora {(banco.impressora != null ? "ok" : "FALTANDO")}, roteador {(banco.roteador != null ? "ok" : "FALTANDO")}, switch {(banco.switchRede != null ? "ok" : "FALTANDO")}");
    }
}
