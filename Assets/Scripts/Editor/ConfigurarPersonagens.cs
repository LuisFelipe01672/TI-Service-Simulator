using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

// Personagens do Mixamo (Assets/Models/Mixamo):
// - na importação, todos viram esqueleto humanoide e as animações (Anim_*.fbx) ficam em loop;
// - o menu cria o controlador de animação e o BancoPersonagens em Resources/Personagens.
public class ConfigurarPersonagens : AssetPostprocessor
{
    const string Pasta = "Assets/Models/Mixamo/";
    const string PastaSaida = "Assets/Resources/Personagens";

    void OnPreprocessModel()
    {
        if (!assetPath.StartsWith(Pasta)) return;
        var imp = (ModelImporter)assetImporter;
        imp.animationType = ModelImporterAnimationType.Human;
        bool animacao = Path.GetFileName(assetPath).StartsWith("Anim_");
        imp.importAnimation = animacao;
        imp.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        if (animacao) imp.materialImportMode = ModelImporterMaterialImportMode.None;
    }

    void OnPreprocessAnimation()
    {
        if (!assetPath.StartsWith(Pasta) || !Path.GetFileName(assetPath).StartsWith("Anim_")) return;
        var imp = (ModelImporter)assetImporter;
        var clips = imp.defaultClipAnimations;
        foreach (var c in clips)
        {
            c.loopTime = true;
            c.lockRootRotation = true;      // o corpo não gira sozinho
            c.lockRootHeightY = true;       // nem sobe/desce
            c.lockRootPositionXZ = true;    // nem anda (quem move é o jogo)
            c.keepOriginalOrientation = true;
            c.keepOriginalPositionY = true;
            c.keepOriginalPositionXZ = true;
        }
        imp.clipAnimations = clips;
    }

    [MenuItem("TI Service/5. Configurar personagens (Mixamo)", false, 1101)]
    public static void Configurar()
    {
        // Reimporta com as regras acima (caso os arquivos tenham entrado antes deste script)
        foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { Pasta.TrimEnd('/') }))
            AssetDatabase.ImportAsset(AssetDatabase.GUIDToAssetPath(guid), ImportAssetOptions.ForceUpdate);

        // As texturas vêm embutidas no FBX: extrai para uma pasta e reimporta, assim os materiais as encontram
        string pastaTex = Pasta + "Texturas";
        if (!AssetDatabase.IsValidFolder(pastaTex)) AssetDatabase.CreateFolder(Pasta.TrimEnd('/'), "Texturas");
        foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { Pasta.TrimEnd('/') }))
        {
            string caminho = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetFileName(caminho).StartsWith("Anim_")) continue;
            var imp = AssetImporter.GetAtPath(caminho) as ModelImporter;
            if (imp == null) continue;
            imp.ExtractTextures(pastaTex);
        }
        AssetDatabase.Refresh();
        foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { Pasta.TrimEnd('/') }))
        {
            string caminho = AssetDatabase.GUIDToAssetPath(guid);
            if (!Path.GetFileName(caminho).StartsWith("Anim_"))
                AssetDatabase.ImportAsset(caminho, ImportAssetOptions.ForceUpdate);
        }

        if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder(PastaSaida)) AssetDatabase.CreateFolder("Assets/Resources", "Personagens");

        // Controlador: um estado por animação, começando em "Parado"
        string caminhoCtrl = PastaSaida + "/Personagens.controller";
        AssetDatabase.DeleteAsset(caminhoCtrl);
        var ctrl = AnimatorController.CreateAnimatorControllerAtPath(caminhoCtrl);
        var sm = ctrl.layers[0].stateMachine;
        var estados = new (string estado, string arquivo)[]
        {
            (BancoPersonagens.Parado, "Anim_Parado"),
            (BancoPersonagens.Andando, "Anim_Andando"),
            (BancoPersonagens.Digitando, "Anim_Digitando"),
            (BancoPersonagens.ConversandoSentado, "Anim_ConversandoSentado"),
            (BancoPersonagens.ConversandoEmPe, "Anim_ConversandoEmPe"),
        };
        foreach (var (estado, arquivo) in estados)
        {
            var clip = Clip(Pasta + arquivo + ".fbx");
            if (clip == null) { Debug.LogWarning($"[Personagens] Animação não encontrada: {arquivo}"); continue; }
            var st = sm.AddState(estado);
            st.motion = clip;
            if (estado == BancoPersonagens.Parado) sm.defaultState = st;
        }

        // Banco: modelos (o de roupa formal primeiro, para o VIP)
        string[] ordem = { "Leonard", "Megan", "Remy" };
        var modelos = ordem.Select(n => AssetDatabase.LoadAssetAtPath<GameObject>(Pasta + n + ".fbx")).Where(m => m != null).ToArray();
        string caminhoBanco = PastaSaida + "/BancoPersonagens.asset";
        var banco = AssetDatabase.LoadAssetAtPath<BancoPersonagens>(caminhoBanco);
        if (banco == null)
        {
            banco = ScriptableObject.CreateInstance<BancoPersonagens>();
            AssetDatabase.CreateAsset(banco, caminhoBanco);
        }
        banco.modelos = modelos;
        banco.controlador = ctrl;
        EditorUtility.SetDirty(banco);
        AssetDatabase.SaveAssets();
        Debug.Log($"[Personagens] {modelos.Length} personagem(ns) e {sm.states.Length} animação(ões) configurados em {PastaSaida}.");
    }

    static AnimationClip Clip(string caminho) =>
        AssetDatabase.LoadAllAssetsAtPath(caminho).OfType<AnimationClip>().FirstOrDefault(c => !c.name.StartsWith("__preview__"));
}
