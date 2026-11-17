using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Configura as texturas que os modelos .gltf (Sketchfab, Poly Haven) usam como arquivos separados:
/// comprimidas (DXT/BC7), tamanho máximo por tipo de objeto e sRGB só nas texturas de cor
/// (baseColor, emissive). Normal, metallicRoughness e occlusion são dados: sRGB desligado.
/// Os GLB embutiam as imagens e o glTFast as mandava sem compressão (build de 631 MB);
/// por isso cada modelo virou uma pasta com .gltf + .bin + imagens.
/// </summary>
public class TexturasModelos : AssetPostprocessor
{
    const string Raiz = "Assets/Models/";

    // Objetos pequenos ou vistos de longe: 1024 basta. O resto (móveis, impressora, PC): 2048.
    static readonly string[] Pequenos =
    {
        "Cartucho_Toner", "Telefone_Escritorio", "Notebook", "Switch_5_Portas", "Roteador", "Mochila",
        "Micro_Ondas", "Patch_Panel", "Switch_Rack_24", "Multimetro", "Pendrive",
        // prédios do bairro (Etapa 9): vistos da rua, de longe
        "Casa_Comum_Brasileira", "Casa_Terrea", "Sobrado_Porta_Enrolar", "Sobrado_Tijolo", "Predio_Hospital", "Orelhao",
        "Escada_Aluminio",   // Etapa 9C (o carro fica em 2048: o painel é visto de perto)
    };

    void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith(Raiz)) return;
        if (assetPath.StartsWith(Raiz + "Mixamo/"))
        {
            // Personagens (fora do Git): a cor fica em 2048 (rosto de perto); normal/brilho/especular em 1024
            var impM = (TextureImporter)assetImporter;
            impM.maxTextureSize = Path.GetFileNameWithoutExtension(assetPath).EndsWith("_Diffuse") ? 2048 : 1024;
            if (impM.textureCompression == TextureImporterCompression.Uncompressed)
                impM.textureCompression = TextureImporterCompression.Compressed;
            return;
        }
        var gltf = AcharGltf(assetPath);
        if (gltf == null) return;
        var papel = PapelDaTextura(gltf, assetPath);
        if (papel == Papel.Nenhum) return;

        var imp = (TextureImporter)assetImporter;
        imp.textureType = TextureImporterType.Default;
        imp.textureShape = TextureImporterShape.Texture2D;
        imp.sRGBTexture = papel == Papel.Cor;
        imp.mipmapEnabled = true;
        imp.isReadable = false;
        imp.textureCompression = papel == Papel.Normal ? TextureImporterCompression.CompressedHQ
                                                       : TextureImporterCompression.Compressed;
        imp.maxTextureSize = Pequenos.Any(p => assetPath.Contains("/" + p + "/")) ? 1024 : 2048;
        imp.anisoLevel = 2;
    }

    enum Papel { Nenhum, Cor, Normal, Dado }

    static string AcharGltf(string caminhoTextura)
    {
        var pasta = Path.GetDirectoryName(caminhoTextura);
        for (int i = 0; i < 2 && !string.IsNullOrEmpty(pasta); i++)
        {
            var achados = Directory.GetFiles(pasta, "*.gltf");
            if (achados.Length > 0) return achados[0].Replace('\\', '/');
            pasta = Path.GetDirectoryName(pasta);
        }
        return null;
    }

    static Papel PapelDaTextura(string caminhoGltf, string caminhoTextura)
    {
        Gltf g;
        try { g = JsonUtility.FromJson<Gltf>(File.ReadAllText(caminhoGltf)); }
        catch { return Papel.Nenhum; }
        if (g?.images == null || g.textures == null) return Papel.Nenhum;

        var pastaGltf = Path.GetDirectoryName(caminhoGltf).Replace('\\', '/');
        int imagem = -1;
        for (int i = 0; i < g.images.Length; i++)
        {
            var uri = g.images[i].uri;
            if (string.IsNullOrEmpty(uri)) continue;
            var completo = (pastaGltf + "/" + System.Uri.UnescapeDataString(uri)).Replace("/./", "/");
            if (completo == caminhoTextura) { imagem = i; break; }
        }
        if (imagem < 0) return Papel.Nenhum;

        var texturas = new HashSet<int>();
        for (int t = 0; t < g.textures.Length; t++) if (g.textures[t].source == imagem) texturas.Add(t);

        bool cor = false, normal = false, dado = false;
        if (g.materials != null)
            foreach (var m in g.materials)
            {
                if (Usa(m.pbrMetallicRoughness?.baseColorTexture, texturas) || Usa(m.emissiveTexture, texturas)) cor = true;
                var sg = m.extensions?.KHR_materials_pbrSpecularGlossiness;
                if (sg != null && (Usa(sg.diffuseTexture, texturas) || Usa(sg.specularGlossinessTexture, texturas))) cor = true;
                if (Usa(m.normalTexture, texturas)) normal = true;
                if (Usa(m.occlusionTexture, texturas) || Usa(m.pbrMetallicRoughness?.metallicRoughnessTexture, texturas)) dado = true;
            }
        if (cor) return Papel.Cor;
        if (normal) return Papel.Normal;
        if (dado) return Papel.Dado;
        return Papel.Cor; // usada por extensão desconhecida: trata como cor (o padrão da Unity)
    }

    static bool Usa(InfoTextura info, HashSet<int> texturas) => info != null && info.index >= 0 && texturas.Contains(info.index);

    [MenuItem("TI Service/Ferramentas/Reimportar texturas dos modelos")]
    public static void Reimportar()
    {
        var pastas = new[] { "Assets/Models/Sketchfab", "Assets/Models/PolyHaven", "Assets/Models/Mixamo" }.Where(AssetDatabase.IsValidFolder).ToArray();
        var caminhos = AssetDatabase.FindAssets("t:Texture2D", pastas)
            .Select(AssetDatabase.GUIDToAssetPath).Distinct().ToList();
        AssetDatabase.StartAssetEditing();
        try { foreach (var c in caminhos) AssetDatabase.ImportAsset(c, ImportAssetOptions.ForceUpdate); }
        finally { AssetDatabase.StopAssetEditing(); }
        Debug.Log($"[Texturas] {caminhos.Count} texturas de modelos reimportadas.");
    }

    // ---- JSON mínimo do glTF (JsonUtility ignora o que não está aqui) ----
    [System.Serializable] class Gltf { public Imagem[] images; public Textura[] textures; public Material[] materials; }
    [System.Serializable] class Imagem { public string uri; }
    [System.Serializable] class Textura { public int source = -1; }
    [System.Serializable] class InfoTextura { public int index = -1; }
    [System.Serializable] class Pbr { public InfoTextura baseColorTexture; public InfoTextura metallicRoughnessTexture; }
    [System.Serializable] class SpecGloss { public InfoTextura diffuseTexture; public InfoTextura specularGlossinessTexture; }
    [System.Serializable] class Extensoes { public SpecGloss KHR_materials_pbrSpecularGlossiness; }
    [System.Serializable] class Material
    {
        public Pbr pbrMetallicRoughness; public InfoTextura normalTexture; public InfoTextura occlusionTexture;
        public InfoTextura emissiveTexture; public Extensoes extensions;
    }
}
