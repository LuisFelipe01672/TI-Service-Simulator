using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Menu "TI Service" no topo do Unity com tarefas de configuração do projeto.
public static class ConfigurarProjeto
{
    const string GraphyPrefab  = "Packages/com.tayx.graphy/Prefab/[Graphy].prefab";
    const string ConsolePrefab = "Packages/com.yasirkula.ingamedebugconsole/Plugins/IngameDebugConsole/IngameDebugConsole.prefab";
    const string TmpEssentials = "Packages/com.unity.ugui/Package Resources/TMP Essential Resources.unitypackage";

    [MenuItem("TI Service/1. Importar fontes do TextMeshPro")]
    public static void ImportarTmp()
    {
        if (AssetDatabase.IsValidFolder("Assets/TextMesh Pro"))
        {
            Debug.Log("[Configurar] TextMeshPro já está importado.");
            return;
        }
        AssetDatabase.ImportPackage(TmpEssentials, false);
        Debug.Log("[Configurar] TMP Essential Resources importado.");
    }

    [MenuItem("TI Service/2. Adicionar ferramentas de debug na cena aberta")]
    public static void AdicionarDebug()
    {
        bool mudou = false;
        mudou |= Adicionar(GraphyPrefab, "[Graphy]");
        mudou |= Adicionar(ConsolePrefab, "IngameDebugConsole");

        mudou |= DeixarGraphyCompacto();

        if (mudou)
        {
            var cena = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(cena);
            EditorSceneManager.SaveScene(cena);
            Debug.Log($"[Configurar] Ferramentas de debug adicionadas em {cena.name} e cena salva.");
        }
    }

    // Mostra só o FPS em texto (Ctrl+H esconde/mostra, Ctrl+G troca o modo)
    static bool DeixarGraphyCompacto()
    {
        var graphy = GameObject.Find("[Graphy]");
        if (graphy == null) return false;
        var manager = graphy.GetComponent("GraphyManager");
        if (manager == null) return false;

        var so = new SerializedObject(manager);
        bool mudou = false;
        mudou |= Definir(so, "m_fpsModuleState", 1);      // TEXT
        mudou |= Definir(so, "m_ramModuleState", 4);      // OFF
        mudou |= Definir(so, "m_audioModuleState", 4);    // OFF
        mudou |= Definir(so, "m_advancedModuleState", 4); // OFF
        so.ApplyModifiedProperties();
        if (mudou) Debug.Log("[Configurar] Graphy configurado para mostrar só o FPS.");
        return mudou;
    }

    static bool Definir(SerializedObject so, string campo, int valor)
    {
        var p = so.FindProperty(campo);
        if (p == null || p.enumValueIndex == valor) return false;
        p.enumValueIndex = valor;
        return true;
    }

    static bool Adicionar(string caminho, string nome)
    {
        if (GameObject.Find(nome) != null)
        {
            Debug.Log($"[Configurar] {nome} já está na cena.");
            return false;
        }
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(caminho);
        if (prefab == null)
        {
            Debug.LogError($"[Configurar] Prefab não encontrado: {caminho}");
            return false;
        }
        var obj = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        obj.name = nome;
        Undo.RegisterCreatedObjectUndo(obj, "Adicionar " + nome);
        return true;
    }
}
