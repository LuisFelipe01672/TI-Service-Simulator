using UnityEditor;
using UnityEngine;

// Liga os modelos 3D das ferramentas de diagnóstico (Sketchfab, CC-BY) ao jogo:
// cria/atualiza Resources/Ferramentas/ModelosFerramentas.asset apontando para os glTF.
static class ConfigurarFerramentas
{
    const string Caminho = "Assets/Resources/Ferramentas/ModelosFerramentas.asset";

    [MenuItem("TI Service/Ferramentas: ligar modelos 3D", false, 60)]
    static void Ligar()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources/Ferramentas")) AssetDatabase.CreateFolder("Assets/Resources", "Ferramentas");
        var banco = AssetDatabase.LoadAssetAtPath<ModelosFerramentas>(Caminho);
        if (banco == null)
        {
            banco = ScriptableObject.CreateInstance<ModelosFerramentas>();
            AssetDatabase.CreateAsset(banco, Caminho);
        }
        banco.multimetro = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Sketchfab/Multimetro/Multimetro.gltf");
        banco.pendrive = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Sketchfab/Pendrive/Pendrive.gltf");
        EditorUtility.SetDirty(banco);
        AssetDatabase.SaveAssets();
        Debug.Log($"[Ferramentas] Modelos ligados: multímetro {(banco.multimetro != null ? "ok" : "FALTANDO")}, pendrive {(banco.pendrive != null ? "ok" : "FALTANDO")}");
    }
}

// Diagnóstico: onde fica o visor do multímetro no modelo (pelas coordenadas de textura) e para onde ele olha
static class MedirVisorMultimetro
{
    [MenuItem("TI Service/Ferramentas: medir visor do multímetro (log)", false, 61)]
    static void Medir()
    {
        var go = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Sketchfab/Multimetro/Multimetro.gltf");
        foreach (var mf in go.GetComponentsInChildren<MeshFilter>(true))
        {
            var m = mf.sharedMesh;
            var uv = m.uv; var pos = m.vertices; var nor = m.normals;
            var raiz = go.transform;
            foreach (bool inverte in new[] { false, true })
            {
                Vector3 somaP = Vector3.zero, somaN = Vector3.zero; int n = 0;
                for (int i = 0; i < uv.Length; i++)
                {
                    float u = uv[i].x, v = inverte ? 1f - uv[i].y : uv[i].y;   // v medido de cima, como na imagem
                    if (u > 0.07f && u < 0.36f && v > 0.66f && v < 0.76f)
                    {
                        somaP += raiz.InverseTransformPoint(mf.transform.TransformPoint(pos[i]));
                        somaN += raiz.InverseTransformDirection(mf.transform.TransformDirection(nor[i]));
                        n++;
                    }
                }
                Debug.Log($"[Ferramentas] visor (v invertido={inverte}): {n} vértices, centro {(n > 0 ? (somaP / n).ToString("F3") : "-")}, normal {(n > 0 ? somaN.normalized.ToString("F2") : "-")}");
            }
            var b = m.bounds;
            Debug.Log($"[Ferramentas] malha {mf.name}: limites {b.min:F2} a {b.max:F2}, giro do nó {mf.transform.localRotation.eulerAngles:F0}, raiz→nó {raiz.InverseTransformDirection(mf.transform.up):F2}");
        }
    }
}
