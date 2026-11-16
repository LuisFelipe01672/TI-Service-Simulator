using UnityEditor;
using UnityEngine;

// Banheiro da casa (Etapa 3, pendência da Etapa 2): 2,6 x 1,7 m atrás da parede sul, com porta entre a cama e o sofá.
// Vaso com caixa acoplada e pia de coluna vêm do Sketchfab (CC-BY, ver CREDITOS.md); o box de vidro com perfil de
// alumínio e o chuveiro elétrico (com o eletroduto até o teto e o registro) são montados aqui — é o chuveiro de casa
// brasileira, que os modelos prontos não têm. Paredes e piso de azulejo.
// Coordenadas locais do banheiro: x de -1,3 (oeste) a 1,3 (leste), z de -0,85 (sul, fundo) a 0,85 (norte, porta).
public static partial class MontarCenario
{
    // A porta fica na parede sul da casa (x = 0,4 da casa); o vão do banheiro fica logo atrás, na parede dele
    static Abertura PortaBanheiro => new Abertura { centro = 0.4f, largura = 0.8f, baixo = 0f, alto = 2.1f, porta = true };
    static readonly Vector3 CentroBanheiro = CentroCasa + new Vector3(0.4f, 0f, -3f - 2f * T - 0.85f);
    const float LarguraBanheiro = 2.6f, ProfBanheiro = 1.7f;
    const string TexturaAzulejo = "Tiles107";        // azulejo branco

    static void MontarBanheiro()
    {
        var mAzulejo = MatPBR("Azulejo_Banheiro", TexturaAzulejo, new Color(0.96f, 0.97f, 0.97f), 0.75f);
        var mPiso = MatPBR("Piso_Banheiro", TexturaPisoCeramico, new Color(0.78f, 0.77f, 0.75f), 0.6f);
        var sala = Sala("Banheiro", CentroBanheiro, LarguraBanheiro, ProfBanheiro, mPiso, 0.4f, mAzulejo,
            norte: new[] { new Abertura { centro = 0f, largura = 0.8f, alto = 2.1f, porta = true, vao = true } },
            sul: new[] { new Abertura { centro = 0.15f, largura = 0.6f, baixo = 1.6f, alto = 2.05f } },   // basculante
            leste: null, oeste: null);
        // Por fora o banheiro tem a mesma pintura da casa; por dentro, azulejo até o teto
        foreach (var r in sala.GetComponentsInChildren<MeshRenderer>())
            if (r.name == "Bloco_Fora") r.sharedMaterial = mParedeCasa;
        var forro = sala.Find("Forro");
        if (forro != null) forro.GetComponent<MeshRenderer>().sharedMaterial = MatPBR("Teto_Casa", "PaintedPlaster017", new Color(0.97f, 0.97f, 0.96f), 0.08f);

        LuzTeto(sala, "Luz_Teto", new Vector3(-0.3f, H - 0.15f, 0f), 0.9f, 4f, false);
        Sonda(sala, new Vector3(LarguraBanheiro, H, ProfBanheiro));
        ModeloReal(PH("modern_ceiling_lamp_01"), sala, new Vector3(-0.3f, H - 0.35f, 0f), 0.4f, 0, Colisao.Nenhuma);

        var mCromado = Mat("Cromado", new Color(0.82f, 0.83f, 0.85f), 0.92f, 1f);
        var mBranco = Mat("Plastico_Branco", new Color(0.95f, 0.95f, 0.94f), 0.55f);

        // ---- Vaso sanitário com caixa acoplada, encostado no fundo (lado oeste), virado para a porta
        if (Existe(SF("Vaso_Sanitario")))
            ModeloReal(SF("Vaso_Sanitario"), sala, new Vector3(-0.8f, 0, -0.85f + 0.36f), 0.78f, RotVaso);
        // porta-papel na parede oeste, ao lado do vaso
        Caixa(sala, "PortaPapel_Base", new Vector3(-1.29f, 0.72f, -0.35f), new Vector3(0.02f, 0.06f, 0.06f), mCromado, 1f, false);
        var rolo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Object.DestroyImmediate(rolo.GetComponent<Collider>());
        rolo.name = "Rolo_Papel";
        rolo.transform.SetParent(sala, false);
        rolo.transform.localPosition = new Vector3(-1.21f, 0.66f, -0.35f);
        rolo.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        rolo.transform.localScale = new Vector3(0.11f, 0.05f, 0.11f);
        rolo.GetComponent<Renderer>().sharedMaterial = Mat("Papel_Higienico", new Color(0.97f, 0.97f, 0.95f), 0.1f);

        // ---- Pia de coluna na parede oeste, com espelho em cima
        if (Existe(SF("Pia_Coluna")))
            ModeloReal(SF("Pia_Coluna"), sala, new Vector3(-1.3f + 0.24f, 0, 0.38f), 0.86f, RotPia);
        var mEspelho = Mat("Espelho", new Color(0.86f, 0.88f, 0.9f), 0.97f, 1f);
        Caixa(sala, "Espelho", new Vector3(-1.295f, 1.5f, 0.38f), new Vector3(0.01f, 0.62f, 0.46f), mEspelho, 1f, false, false);
        Caixa(sala, "Saboneteira", new Vector3(-1.25f, 0.98f, 0.66f), new Vector3(0.09f, 0.02f, 0.12f), mCromado, 1f, false);
        // toalheiro de rosto na parede norte, ao lado da pia, com a toalha
        Caixa(sala, "Toalheiro", new Vector3(-0.85f, 1.3f, 0.8f), new Vector3(0.4f, 0.02f, 0.02f), mCromado, 1f, false);
        Caixa(sala, "Toalha_Rosto", new Vector3(-0.85f, 1.12f, 0.795f), new Vector3(0.32f, 0.36f, 0.025f), Mat("Toalha_Azul", new Color(0.25f, 0.42f, 0.6f), 0.05f), 1f, false);

        // ---- Box: piso do chuveiro, ralo, dois painéis de vidro de correr no perfil de alumínio
        const float xBox = 0.45f;
        Caixa(sala, "Ralo", new Vector3(0.95f, 0.003f, -0.45f), new Vector3(0.1f, 0.004f, 0.1f), mCromado, 1f, false, false);
        var mPerfil = Mat("Perfil_Box", new Color(0.78f, 0.79f, 0.8f), 0.7f, 0.9f);
        var mVidroBox = Mat("Vidro_Box", new Color(0.86f, 0.92f, 0.94f), 0.95f);
        TornarTransparente(mVidroBox, 0.16f);
        Caixa(sala, "Box_Trilho_Baixo", new Vector3(xBox + 0.02f, 0.015f, 0f), new Vector3(0.05f, 0.03f, ProfBanheiro), mPerfil, 1f, false);
        Caixa(sala, "Box_Trilho_Cima", new Vector3(xBox + 0.02f, 1.92f, 0f), new Vector3(0.05f, 0.04f, ProfBanheiro), mPerfil, 1f, false);
        Caixa(sala, "Box_Vidro_Fixo", new Vector3(xBox, 0.96f, -0.4f), new Vector3(0.008f, 1.86f, 0.9f), mVidroBox, 1f, true, false);
        Caixa(sala, "Box_Vidro_Correr", new Vector3(xBox + 0.04f, 0.96f, 0.12f), new Vector3(0.008f, 1.86f, 0.62f), mVidroBox, 1f, true, false);
        Caixa(sala, "Box_Puxador", new Vector3(xBox + 0.065f, 1.05f, 0.4f), new Vector3(0.02f, 0.3f, 0.02f), mPerfil, 1f, false);
        foreach (float z in new[] { -0.84f, 0.84f })
            Caixa(sala, "Box_Perfil", new Vector3(xBox + 0.02f, 0.96f, z), new Vector3(0.04f, 1.9f, 0.02f), mPerfil, 1f, false);

        // ---- Chuveiro elétrico na parede leste: braço, corpo branco com o crivo, eletroduto até o teto e registro
        float xP = LarguraBanheiro / 2f;
        Caixa(sala, "Chuveiro_Braco", new Vector3(xP - 0.09f, 2.14f, -0.2f), new Vector3(0.18f, 0.025f, 0.025f), mCromado, 1f, false);
        Caixa(sala, "Chuveiro_Corpo", new Vector3(xP - 0.2f, 2.07f, -0.2f), new Vector3(0.1f, 0.16f, 0.11f), mBranco, 1f, false);
        var crivo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Object.DestroyImmediate(crivo.GetComponent<Collider>());
        crivo.name = "Chuveiro_Crivo";
        crivo.transform.SetParent(sala, false);
        crivo.transform.localPosition = new Vector3(xP - 0.2f, 1.975f, -0.2f);
        crivo.transform.localScale = new Vector3(0.15f, 0.015f, 0.15f);
        crivo.GetComponent<Renderer>().sharedMaterial = mBranco;
        Caixa(sala, "Chuveiro_Fio", new Vector3(xP - 0.012f, (2.16f + H) / 2f, -0.2f), new Vector3(0.022f, H - 2.16f, 0.022f), mBranco, 1f, false);
        Caixa(sala, "Registro_Base", new Vector3(xP - 0.01f, 1.15f, -0.2f), new Vector3(0.02f, 0.07f, 0.07f), mCromado, 1f, false);
        Caixa(sala, "Registro_Manopla", new Vector3(xP - 0.05f, 1.15f, -0.2f), new Vector3(0.06f, 0.025f, 0.08f), mCromado, 1f, false);
        Caixa(sala, "Prateleira_Box", new Vector3(xP - 0.06f, 1.25f, 0.5f), new Vector3(0.12f, 0.02f, 0.3f), mCromado, 1f, false);
        var mFrasco = Mat("Frasco_Shampoo", new Color(0.35f, 0.55f, 0.75f), 0.6f);
        Caixa(sala, "Shampoo", new Vector3(xP - 0.06f, 1.34f, 0.42f), new Vector3(0.05f, 0.16f, 0.04f), mFrasco, 1f, false);
        Caixa(sala, "Sabonete_Liquido", new Vector3(xP - 0.06f, 1.32f, 0.55f), new Vector3(0.05f, 0.12f, 0.04f), Mat("Frasco_Branco", new Color(0.92f, 0.9f, 0.86f), 0.6f), 1f, false);

        // Tapete na saída do box e toalha de banho pendurada no box
        Caixa(sala, "Tapete_Banheiro", new Vector3(0.05f, 0.005f, 0.42f), new Vector3(0.45f, 0.01f, 0.65f), Mat("Tapete_Banheiro", new Color(0.3f, 0.45f, 0.62f), 0.02f), 1f, false);
        Caixa(sala, "Toalha_Banho", new Vector3(xBox - 0.02f, 1.6f, -0.45f), new Vector3(0.04f, 0.6f, 0.5f), Mat("Toalha_Branca", new Color(0.93f, 0.92f, 0.88f), 0.03f), 1f, false);
        Lixeira(sala, new Vector3(-0.3f, 0, -0.65f));
    }

    // Giro dos modelos do banheiro para ficarem de frente para dentro (conferido nas fotos)
    const float RotVaso = 0f, RotPia = 90f;

    static bool Existe(string caminho) => AssetDatabase.LoadAssetAtPath<GameObject>(caminho) != null;

    static void TornarTransparente(Material m, float alfa)
    {
        m.SetFloat("_Mode", 3);
        m.SetOverrideTag("RenderType", "Transparent");
        m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
        m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        m.SetInt("_ZWrite", 0);
        m.DisableKeyword("_ALPHATEST_ON");
        m.DisableKeyword("_ALPHABLEND_ON");
        m.EnableKeyword("_ALPHAPREMULTIPLY_ON");
        m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        var c = m.color; c.a = alfa; m.color = c;
        EditorUtility.SetDirty(m);
    }
}
