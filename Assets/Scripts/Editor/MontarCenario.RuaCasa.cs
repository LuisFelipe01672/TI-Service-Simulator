using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// Parte do MontarCenario: rua na frente do escritório (calçadas, asfalto, faixa de pedestre e postes),
// a casa do jogador do outro lado da rua (quarto, sala e cozinha num só cômodo) e prédios vizinhos.
// O jogador sai pela porta do escritório, atravessa a rua, entra em casa e dorme na cama (Cama).
public static partial class MontarCenario
{
    static Material mAsfalto, mCalcada, mFaixa, mParedeCasa, mLampadaAcesa, mLampadaApagada, mJanelaPredio;

    // Coordenadas do mundo: vêm da planta do bairro (Cidade/Bairro.cs)
    const float CalcadaSulZ1 = Bairro.AcaciasCalcadaSulZ1;
    internal static Vector3 CentroCasa => Bairro.CentroCasa;

    static void MontarRuaECasaDentro()
    {
        CriarMateriaisRua();
        MontarRua();
        portasAbrem = true;
        MontarCasa();
        MontarBanheiro();
        MontarBairro();
        MontarCarroEPosto();   // Etapa 9C
        MontarTransito();      // Etapa 9D
    }

    static void CriarMateriaisRua()
    {
        mAsfalto = MatPBR("Asfalto", "Asphalt031", new Color(0.8f, 0.8f, 0.8f), 0.15f);
        mCalcada = MatPBR("Calcada", "Concrete034", new Color(0.86f, 0.85f, 0.82f), 0.1f);
        mFaixa = Mat("Faixa_Branca", new Color(0.92f, 0.92f, 0.9f), 0.2f);
        mParedeCasa = MatPBR("Parede_Casa", "PaintedPlaster017", new Color(0.96f, 0.88f, 0.76f), 0.12f);
        mLampadaApagada = Mat("Lampada_Apagada", new Color(0.8f, 0.8f, 0.75f), 0.6f);
        mLampadaAcesa = Mat("Lampada_Acesa", new Color(1f, 0.92f, 0.75f), 0.6f);
        Brilhar(mLampadaAcesa, new Color(1f, 0.85f, 0.6f) * 2.2f);
        mJanelaPredio = Mat("Janela_Predio", new Color(0.12f, 0.16f, 0.2f), 0.9f);
    }

    // ---------------- Casa do jogador ----------------

    static void MontarCasa()
    {
        // 8 x 6 m, porta virada para a rua (norte), um cômodo com quarto, sala e cozinha
        var casa = Sala("Casa", CentroCasa, 8f, 6f, mPisoMadeira, 1.6f, mParedeCasa,
            norte: new[] { Janela(-1.7f), Porta(1.5f) },
            sul: new[] { PortaBanheiro, Janela(1.8f) },   // porta do banheiro (Etapa 3), entre a cama e o sofá
            leste: new[] { Janela(0.3f) },
            oeste: new[] { Janela(0.6f) });

        // Casa tem teto de gesso liso (o forro de placas é de escritório)
        var forro = casa.Find("Forro");
        if (forro != null)
            forro.GetComponent<MeshRenderer>().sharedMaterial =
                MatPBR("Teto_Casa", "PaintedPlaster017", new Color(0.97f, 0.97f, 0.96f), 0.08f);

        LuzTeto(casa, "Luz_Teto_Quarto", new Vector3(-2f, H - 0.15f, -1f), 1.3f, 6.5f, true);
        LuzTeto(casa, "Luz_Teto_Sala", new Vector3(2f, H - 0.15f, 0.5f), 1.2f, 6.5f, false);
        Sonda(casa, new Vector3(8f, H, 6f));

        // Caminho da calçada até a porta
        float zParede = CentroCasa.z + 3f + T;
        Caixa(casa, "Caminho", new Vector3(1.5f, 0.03f, (CalcadaSulZ1 + zParede) / 2f - CentroCasa.z),
            new Vector3(1.2f, 0.08f, CalcadaSulZ1 - zParede), mCalcada, 1.5f);
        Caixa(casa, "Capacho", new Vector3(1.5f, 0.006f, 2.62f), new Vector3(0.75f, 0.012f, 0.45f), Mat("Capacho", new Color(0.16f, 0.13f, 0.1f), 0.05f), 1f, false);

        MontarQuarto(casa);
        MontarHomeOffice(casa);
        Persianas(casa);

        MontarSala(casa);

        MontarCozinha(casa);

        // Encomendas (Fase 2): chegam como caixas na frente de casa, ao lado da porta (EncomendaFisica)
        Ponto("PontoEntrega", casa, new Vector3(2.55f, 0f, 3.55f), 180);
        CriarPrefabCaixa();

        // Onde o jogador acorda (o DiaManager procura "SpawnCasa"): ao lado da cama, olhando para a porta
        Ponto("SpawnCasa", casa, new Vector3(0.2f, 1f, -1.2f), 0);
    }

    // ---------------- Quarto e home office (modelos realistas, Fase 1A) ----------------
    // Casa: x de -4 a 4 (oeste → leste), z de -3 a 3 (sul → norte, a rua fica ao norte).

    const string PastaPolyHaven = "Assets/Models/PolyHaven/";
    const string PastaSketchfab = "Assets/Models/Sketchfab/";
    static string PH(string id) => PastaPolyHaven + id + "/" + id + ".gltf";
    static string SF(string nome) => PastaSketchfab + nome + "/" + nome + ".gltf";

    static void MontarQuarto(Transform casa)
    {
        // Cama com a cabeceira na parede sul
        var cama = ModeloReal(SF("Cama_Casal"), casa, new Vector3(-2.35f, 0f, -1.95f), 2.1f, RotCama);
        if (cama != null) cama.AddComponent<Cama>().distanciaInteracao = 2.0f;   // da base da cama ao jogador

        var criadoEsq = ModeloReal(PH("ClassicNightstand_01"), casa, new Vector3(-3.55f, 0f, -2.72f), 0.5f, 0);
        var criadoDir = ModeloReal(PH("ClassicNightstand_01"), casa, new Vector3(-1.15f, 0f, -2.72f), 0.5f, 0);
        ModeloReal(PH("alarm_clock_01"), casa, new Vector3(-1.15f, Altura(criadoDir), -2.7f), 0.12f, 160, Colisao.Nenhuma);
        ModeloReal(PH("standing_picture_frame_01"), casa, new Vector3(-3.4f, Altura(criadoEsq), -2.8f), 0.2f, 15, Colisao.Nenhuma);
        CriarAbajur(casa, new Vector3(-3.66f, Altura(criadoEsq), -2.68f));
        ModeloReal(PH("hanging_picture_frame_01"), casa, new Vector3(-2.35f, 1.35f, -2.97f), 0.7f, 180, Colisao.Nenhuma);

        // Tapete aos pés da cama
        Caixa(casa, "Tapete_Quarto", new Vector3(-2.35f, 0.006f, -0.45f), new Vector3(2.0f, 0.012f, 1.2f), mCarpete, 1.5f, false);

        // Guarda-roupa branco na parede oeste, entre a cama e a janela
        // o arquivo traz dois guarda-roupas (fechado e aberto): fica só o fechado
        ModeloReal(SF("Guarda_Roupa"), casa, new Vector3(-3.68f, 0f, -0.75f), 1.9f, RotGuardaRoupa,
            manterMetade: +1);
        ModeloReal(PH("potted_plant_02"), casa, new Vector3(-0.55f, 0f, -2.6f), 0.9f, 0);
    }

    static void MontarHomeOffice(Transform casa)
    {
        // Mesa no canto noroeste, de frente para a parede norte, com a janela à direita
        var mesa = ModeloReal(PH("metal_office_desk"), casa, new Vector3(-3.0f, 0f, 2.53f), LadoMesaMetal, RotMesa);
        float tampo = Altura(mesa);
        ModeloReal(SF("Cadeira_Escritorio"), casa, new Vector3(-3.0f, 0f, 1.75f), 1.15f, RotCadeira);
        var pc = ModeloReal(SF("Computador_Mesa"), casa, new Vector3(-3.1f, tampo, 2.6f), 0.85f, 180, Colisao.Nenhuma);
        if (pc != null) ConfigurarPcDeCasa(pc, casa);
        ModeloReal(PH("desk_lamp_arm_01"), casa, new Vector3(-3.42f, tampo, 2.72f), 0.5f, 135, Colisao.Nenhuma);
        ModeloReal(PH("office_notepads"), casa, new Vector3(-2.45f, tampo, 2.35f), 0.3f, 20, Colisao.Nenhuma);
        ModeloReal(PH("potted_plant_04"), casa, new Vector3(-2.35f, tampo, 2.75f), 0.25f, 0, Colisao.Nenhuma);
        var relogio = ModeloReal(PH("wall_clock"), casa, new Vector3(-3.0f, 2.15f, 2.97f), 0.32f, RotRelogio, Colisao.Nenhuma);
        if (relogio != null) relogio.AddComponent<RelogioAnalogico>().frente = -casa.forward;   // mostrador para o sul
        var mochila = ModeloReal(SF("Mochila"), casa, new Vector3(-2.2f, 0f, 2.4f), 0.5f, 200);
        if (mochila != null) mochila.AddComponent<MochilaFisica>();   // pegar antes de sair de casa

        // Estante de aço com as ferramentas, na parede norte entre a janela e a porta
        var estante = ModeloReal(PH("steel_frame_shelves_03"), casa, new Vector3(-0.35f, 0f, 2.68f), 1.8f, RotEstante);
        ModeloReal(PH("metal_toolbox"), casa, new Vector3(-0.35f, 0f, 2.25f), 0.5f, 10);
        ModeloReal(PH("cardboard_box_01"), casa, new Vector3(-1.0f, 0f, 2.3f), 0.45f, 25);
        if (estante != null) ModeloReal(PH("cardboard_box_01"), casa, new Vector3(-0.35f, Altura(estante) * 0.55f, 2.7f), 0.4f, -8, Colisao.Nenhuma);

        // Luminárias de teto (só o visual; as luzes são as LuzTeto)
        var lustreQuarto = ModeloReal(PH("modern_ceiling_lamp_01"), casa, new Vector3(-2f, H - 0.35f, -1f), 0.45f, 0, Colisao.Nenhuma);
        var lustreSala = ModeloReal(PH("modern_ceiling_lamp_01"), casa, new Vector3(2f, H - 0.35f, 0.5f), 0.45f, 0, Colisao.Nenhuma);

        // Interruptores: quarto (parede sul, ao lado do criado-mudo) e sala (ao lado da porta)
        CriarInterruptor(casa, "Interruptor_Quarto", "do quarto", new Vector3(-0.6f, 1.15f, -3f), 0f,
            casa.Find("Luz_Teto_Quarto"), lustreQuarto);
        CriarInterruptor(casa, "Interruptor_Sala", "da sala", new Vector3(0.85f, 1.15f, 3f), 180f,
            casa.Find("Luz_Teto_Sala"), lustreSala);
    }

    // Sala (canto sudeste, Fase 1B): sofá de couro na parede sul olhando para a TV no rack (que divide os ambientes),
    // mesa de centro com tapete, planta; mesa redonda de jantar com duas cadeiras perto da cozinha
    const float RotSofa = 0f, RotRack = 180f;
    static void MontarSala(Transform casa)
    {
        ModeloReal(PH("sofa_02"), casa, new Vector3(2.4f, 0f, -2.5f), 2.0f, RotSofa);
        Caixa(casa, "Tapete_Sala", new Vector3(2.4f, 0.006f, -1.45f), new Vector3(2.2f, 0.012f, 1.5f), mCarpete, 1.5f, false);
        ModeloReal(PH("modern_coffee_table_01"), casa, new Vector3(2.4f, 0.012f, -1.5f), 1.0f, 0);
        ModeloReal(PH("throw_pillows_01"), casa, new Vector3(2.0f, 0.45f, -2.55f), 0.5f, 0, Colisao.Nenhuma);
        ModeloReal(PH("potted_plant_01"), casa, new Vector3(3.6f, 0f, -2.6f), 0.9f, 0);

        var rack = ModeloReal(PH("modern_wooden_cabinet"), casa, new Vector3(2.4f, 0f, -0.25f), 1.6f, RotRack);
        CriarTV(casa, new Vector3(2.4f, Altura(rack), -0.2f), 180f);

        // Jantar: mesa redonda e duas cadeiras viradas para ela
        // mesa pequena encostada perto da cozinha, longe do caminho da porta (x 1,0–2,0)
        ModeloReal(PH("round_wooden_table_01"), casa, new Vector3(2.55f, 0f, 0.75f), 0.75f, 0);
        ModeloReal(PH("dining_chair_02"), casa, new Vector3(2.0f, 0f, 0.75f), 0.95f, 90);
        ModeloReal(PH("dining_chair_02"), casa, new Vector3(2.55f, 0f, 1.32f), 0.95f, 180);
    }

    // TV de tela plana 50": painel preto brilhante (desligada reflete o ambiente), moldura fina e pé central.
    // 'rotY' 180 = tela virada para o sul (sofá).
    static void CriarTV(Transform casa, Vector3 base_, float rotY)
    {
        var mPainel = Mat("TV_Tela", new Color(0.02f, 0.02f, 0.025f), 0.92f, 0.1f);
        var mCorpo = Mat("TV_Corpo", new Color(0.06f, 0.06f, 0.07f), 0.45f);
        var tv = new GameObject("TV").transform;
        tv.SetParent(casa, false);
        tv.localPosition = base_;
        tv.localRotation = Quaternion.Euler(0, rotY, 0);
        const float w = 1.12f, h = 0.65f;
        Caixa(tv, "Pe", new Vector3(0, 0.008f, 0), new Vector3(0.36f, 0.016f, 0.2f), mCorpo, 1f, false);
        Caixa(tv, "Haste", new Vector3(0, 0.06f, 0.02f), new Vector3(0.06f, 0.1f, 0.03f), mCorpo, 1f, false);
        Caixa(tv, "Corpo", new Vector3(0, 0.1f + h / 2f, 0.015f), new Vector3(w, h, 0.035f), mCorpo, 1f, false);
        Caixa(tv, "Tela", new Vector3(0, 0.1f + h / 2f + 0.005f, -0.004f), new Vector3(w - 0.02f, h - 0.03f, 0.004f), mPainel, 1f, false);
    }

    // Prefab da caixa de papelão realista (Poly Haven) com 1 m em cada eixo e base em y = 0, em Resources, para o
    // jogo criar encomendas e a caixa de cabo em qualquer tamanho (EncomendaFisica.CriarCaixa)
    static void CriarPrefabCaixa()
    {
        const string pasta = "Assets/Resources/Encomendas";
        if (!AssetDatabase.IsValidFolder(pasta)) AssetDatabase.CreateFolder("Assets/Resources", "Encomendas");
        var modelo = AssetDatabase.LoadAssetAtPath<GameObject>(PH("cardboard_box_01"));
        if (modelo == null) { Debug.LogWarning("[Cenário] cardboard_box_01 não encontrado."); return; }

        var raiz = new GameObject("Caixa");
        var inst = (GameObject)PrefabUtility.InstantiatePrefab(modelo);
        PrefabUtility.UnpackPrefabInstance(inst, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        inst.transform.SetParent(raiz.transform, false);
        var b = Limites(inst);
        inst.transform.localScale = Vector3.Scale(inst.transform.localScale, new Vector3(1f / b.size.x, 1f / b.size.y, 1f / b.size.z));
        b = Limites(inst);
        inst.transform.localPosition -= new Vector3(b.center.x, b.min.y, b.center.z);
        foreach (var c in raiz.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
        PrefabUtility.SaveAsPrefabAsset(raiz, pasta + "/Caixa.prefab");
        Object.DestroyImmediate(raiz);
    }

    // Balcão sob a janela leste com a cafeteira (café = energia) e a xícara que aparece quando fica pronto
    // Cozinha realista na parede leste (Fase 1B): bancada compacta (pia, cooktop e forno) sob a janela, geladeira
    // ao lado da porta, micro-ondas e cafeteira espresso em cima da bancada. Frentes viradas para oeste (-X).
    const float RotCozinha = -90f;
    static void MontarCozinha(Transform casa)
    {
        var bancada = ModeloReal(SF("Cozinha_Compacta"), casa, new Vector3(3.68f, 0f, 0.95f), 1.61f, RotCozinha);
        if (bancada != null) TornarOpaco(bancada);   // o modelo veio com o material inteiro em modo transparente
        float topo = bancada != null ? Altura(bancada) * 0.9f : 0.9f;   // tampo (sem o espelho de trás)

        var geladeira = ModeloReal(SF("Eletrodomesticos"), casa, new Vector3(3.55f, 0f, 2.45f), 2.02f, RotCozinha,
            soAPeca: "Refridgerator");
        if (geladeira != null) geladeira.AddComponent<Geladeira>();   // comer recupera energia

        // micro-ondas em cima da geladeira (cozinha pequena), cafeteira na ponta da bancada perto dela
        float topoGeladeira = geladeira != null ? Altura(geladeira) : 2f;
        ModeloReal(SF("Micro_Ondas"), casa, new Vector3(3.62f, topoGeladeira, 2.45f), 0.48f, RotCozinha, Colisao.Nenhuma);
        MontarCafeteira(casa, new Vector3(3.78f, topo, 1.62f), topo);
    }

    static void MontarCafeteira(Transform casa, Vector3 pos, float topo)
    {
        var maquina = ModeloReal(SF("Eletrodomesticos"), casa, pos, 0.32f, RotCozinha, Colisao.Nenhuma, soAPeca: "Coffeemachine");
        if (maquina == null) return;

        var mXicara = Mat("Xicara_Branca", new Color(0.96f, 0.95f, 0.92f), 0.7f);
        var mCafe = Mat("Cafe", new Color(0.18f, 0.1f, 0.05f), 0.85f);
        var xicara = new GameObject("Xicara").transform;
        xicara.SetParent(casa, false);
        xicara.localPosition = new Vector3(3.55f, topo, pos.z - 0.2f);
        var corpo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Object.DestroyImmediate(corpo.GetComponent<Collider>());
        corpo.transform.SetParent(xicara, false);
        corpo.transform.localPosition = new Vector3(0, 0.045f, 0);
        corpo.transform.localScale = new Vector3(0.08f, 0.045f, 0.08f);
        corpo.GetComponent<Renderer>().sharedMaterial = mXicara;
        var cafe = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Object.DestroyImmediate(cafe.GetComponent<Collider>());
        cafe.transform.SetParent(xicara, false);
        cafe.transform.localPosition = new Vector3(0, 0.086f, 0);
        cafe.transform.localScale = new Vector3(0.068f, 0.002f, 0.068f);
        cafe.GetComponent<Renderer>().sharedMaterial = mCafe;
        xicara.gameObject.SetActive(false);

        var c = maquina.AddComponent<Cafeteira>();
        c.xicara = xicara;
    }

    // Espelho branco de interruptor com a tecla; 'parede' é o ponto na face interna da parede,
    // rotY gira o espelho para ficar de frente para dentro do cômodo.
    static void CriarInterruptor(Transform casa, string nome, string comodo, Vector3 parede, float rotY,
        Transform luz, GameObject lustre)
    {
        var mBranco = Mat("Interruptor_Branco", new Color(0.95f, 0.95f, 0.93f), 0.35f);
        var go = new GameObject(nome);
        go.transform.SetParent(casa, false);
        go.transform.localPosition = parede;
        go.transform.localRotation = Quaternion.Euler(0, rotY, 0);
        Caixa(go.transform, "Espelho", new Vector3(0, 0, 0.004f), new Vector3(0.08f, 0.12f, 0.008f), mBranco, 1f, false, false);
        var tecla = new GameObject("Tecla").transform;
        tecla.SetParent(go.transform, false);
        tecla.localPosition = new Vector3(0, 0, 0.009f);
        tecla.localRotation = Quaternion.Euler(-12f, 0, 0);
        Caixa(tecla, "Tecla_Visual", Vector3.zero, new Vector3(0.032f, 0.055f, 0.01f), mBranco, 1f, false, false);

        var i = go.AddComponent<Interruptor>();
        i.comodo = comodo;
        i.tecla = tecla;
        if (luz != null && luz.GetComponent<Light>() != null) i.luzes.Add(luz.GetComponent<Light>());
        if (lustre != null) i.lampadas.AddRange(lustre.GetComponentsInChildren<Renderer>());
    }

    // PC de casa: tela viva do jogo sobre o monitor e o ponto de vista de quem está sentado na cadeira
    static void ConfigurarPcDeCasa(GameObject pc, Transform casa)
    {
        var tela = TelaNoMonitor(pc, out var b);
        var pose = new GameObject("PoseSentado").transform;
        pose.SetParent(pc.transform, true);
        pose.position = b.center + Vector3.up * 0.05f + pc.transform.right * -0.05f + tela.forward * 0.62f;
        pose.rotation = Quaternion.LookRotation(tela.position - pose.position);

        var uso = pc.AddComponent<ComputadorCasa>();
        uso.poseSentado = pose;
    }

    // Monitor do modelo Computador_Mesa: acha a malha da tela e põe por cima a tela viva do jogo (TelaComputador).
    // A tela fica virada para a frente do suporte do modelo (pc.transform.forward). 'b' = limites do monitor (mundo).
    // Modelos glTF que vieram com material em modo transparente sem precisar (a peça fica "fantasma"):
    // troca por uma cópia opaca salva em Assets/Materials/Cenario (o material original do GLB não pode ser editado)
    internal static void TornarOpaco(GameObject modelo)
    {
        foreach (var r in modelo.GetComponentsInChildren<Renderer>(true))
        {
            var mats = r.sharedMaterials;
            bool mudou = false;
            for (int i = 0; i < mats.Length; i++)
            {
                var m = mats[i];
                if (m == null || m.renderQueue < 2450) continue;
                var copia = CarregarOuCriar(m.name.Replace(" ", "_") + "_Opaco");
                copia.shader = m.shader;
                copia.CopyPropertiesFromMaterial(m);
                foreach (var k in copia.shaderKeywords)
                    if (k.Contains("ALPHA") || k.Contains("TRANSPARENT") || k.Contains("BLEND")) copia.DisableKeyword(k);
                if (copia.HasProperty("_SrcBlend")) copia.SetFloat("_SrcBlend", 1f);
                if (copia.HasProperty("_DstBlend")) copia.SetFloat("_DstBlend", 0f);
                if (copia.HasProperty("_ZWrite")) copia.SetFloat("_ZWrite", 1f);
                if (copia.HasProperty("_Mode")) copia.SetFloat("_Mode", 0f);
                if (copia.HasProperty("baseColorFactor")) { var c = copia.GetColor("baseColorFactor"); c.a = 1f; copia.SetColor("baseColorFactor", c); }
                copia.SetOverrideTag("RenderType", "Opaque");
                copia.renderQueue = 2000;
                EditorUtility.SetDirty(copia);
                mats[i] = copia;
                mudou = true;
            }
            if (mudou) r.sharedMaterials = mats;
        }
    }

    internal static Transform TelaNoMonitor(GameObject pc, out Bounds b)
    {
        Renderer monitor = pc.GetComponentsInChildren<Renderer>()
            .FirstOrDefault(r => r.transform.parent != null && r.transform.parent.name.Contains("Computer"));
        if (monitor == null) monitor = pc.GetComponentsInChildren<Renderer>().OrderByDescending(r => r.bounds.size.y).First();
        b = monitor.bounds;
        Vector3 fora = pc.transform.forward;
        float profundidade = Mathf.Abs(Vector3.Dot(b.extents, new Vector3(Mathf.Abs(fora.x), Mathf.Abs(fora.y), Mathf.Abs(fora.z))));
        float largura = Mathf.Abs(Vector3.Dot(b.size, new Vector3(Mathf.Abs(pc.transform.right.x), 0, Mathf.Abs(pc.transform.right.z))));

        var tela = new GameObject("TelaPC").transform;
        tela.SetParent(pc.transform, true);
        tela.position = b.center + Vector3.up * b.size.y * 0.03f + fora * (profundidade + 0.004f);
        tela.rotation = Quaternion.LookRotation(fora);
        var t = tela.gameObject.AddComponent<TelaComputador>();
        t.tamanho = new Vector2(largura * 0.93f, b.size.y * 0.84f);

        // Película preta sempre visível: com o PC desligado (tela viva escondida) a tela fica preta,
        // e a textura original do modelo (com marca real) nunca aparece
        var preta = GameObject.CreatePrimitive(PrimitiveType.Quad);
        preta.name = "TelaDesligada";
        Object.DestroyImmediate(preta.GetComponent<Collider>());
        preta.transform.SetParent(pc.transform, true);
        preta.transform.position = b.center + fora * (profundidade + 0.0025f);
        preta.transform.rotation = Quaternion.LookRotation(-fora);   // o lado visível do Quad olha para -Z local
        preta.transform.localScale = Vector3.one;
        preta.transform.localScale = new Vector3(largura * 0.985f / preta.transform.lossyScale.x, b.size.y * 0.93f / preta.transform.lossyScale.y, 1f);
        preta.GetComponent<Renderer>().sharedMaterial = Mat("Tela_Desligada", new Color(0.015f, 0.015f, 0.02f), 0.85f);
        Debug.Log($"[Cenário] Tela no monitor de {pc.name}: {largura:0.00} x {b.size.y:0.00} m");
        return tela;
    }

    // Persiana horizontal por dentro de cada janela (mesmas posições do Sala(...) da casa), feita de lâminas.
    // Fica meio aberta (lâminas a 35°), deixando a luz do sol entrar em faixas.
    static void Persianas(Transform casa)
    {
        var mLamina = Mat("Persiana_Aluminio", new Color(0.9f, 0.9f, 0.88f), 0.55f, 0.3f);
        void Uma(string nome, Vector3 centro, float rotY)
        {
            var p = new GameObject(nome).transform;
            p.SetParent(casa, false);
            p.localPosition = centro;
            p.localRotation = Quaternion.Euler(0, rotY, 0);
            const float largura = 1.3f, topo = 2.2f - 1.5f, baixo = 0.95f - 1.5f, passo = 0.045f;
            Caixa(p, "Trilho", new Vector3(0, topo + 0.02f, 0), new Vector3(largura + 0.04f, 0.04f, 0.05f), mLamina, 1f, false);
            int n = 0;
            for (float y = topo - 0.03f; y > baixo; y -= passo, n++)
            {
                var l = Caixa(p, "Lamina", new Vector3(0, y, 0), new Vector3(largura, 0.003f, 0.04f), mLamina, 1f, false, true);
                l.transform.localRotation = Quaternion.Euler(35f, 0, 0);
            }
            Caixa(p, "Base", new Vector3(0, baixo, 0), new Vector3(largura, 0.012f, 0.045f), mLamina, 1f, false);
            Caixa(p, "Cordao", new Vector3(largura / 2f - 0.06f, (topo + baixo) / 2f - 0.15f, 0.03f),
                new Vector3(0.006f, topo - baixo, 0.006f), mLamina, 1f, false, false);
        }
        // a janela vai de 0,9 a 2,1 m; a persiana fica 6 cm para dentro da parede
        Uma("Persiana_Norte", new Vector3(-1.7f, 1.5f, 3f - 0.06f), 0);
        Uma("Persiana_Sul", new Vector3(1.8f, 1.5f, -3f + 0.06f), 180);
        Uma("Persiana_Leste", new Vector3(4f - 0.06f, 1.5f, 0.3f), 90);
        Uma("Persiana_Oeste", new Vector3(-4f + 0.06f, 1.5f, 0.6f), -90);
    }

    // Giro de cada modelo para ficar de frente para dentro da casa (conferido nas fotos do menu Teste gráfico)
    const float RotCama = 0f, RotGuardaRoupa = 90f, RotMesa = 180f, RotCadeira = 180f, RotRelogio = 180f,
                RotEstante = 180f;

    // Modelo glTF/GLB realista: mantém o giro original do arquivo, ajusta o maior lado para 'maiorLado'
    // (metros), apoia a base em 'pos' (local do pai), centraliza e gira 'rotY'.
    // manterMetade: -1/+1 apaga as peças do outro lado do eixo horizontal mais comprido (arquivos com dois modelos)
    static GameObject ModeloReal(string caminho, Transform pai, Vector3 pos, float maiorLado, float rotY,
        Colisao colisao = Colisao.Caixa, int manterMetade = 0, string soAPeca = null)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(caminho);
        if (prefab == null) { Debug.LogError("[Cenário] Modelo não encontrado: " + caminho); return null; }

        var suporte = new GameObject(System.IO.Path.GetFileNameWithoutExtension(caminho)).transform;
        var modelo = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        modelo.transform.SetParent(suporte, false);

        var b = Limites(modelo);
        // soAPeca: arquivos com vários objetos (ex.: conjunto de eletrodomésticos) — fica só o que tem esse nome
        if (!string.IsNullOrEmpty(soAPeca))
        {
            PrefabUtility.UnpackPrefabInstance(modelo, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            foreach (var r in modelo.GetComponentsInChildren<Renderer>())
            {
                bool fica = false;
                for (var t = r.transform; t != null && t != modelo.transform; t = t.parent) if (t.name.Contains(soAPeca)) { fica = true; break; }
                if (!fica) Object.DestroyImmediate(r.gameObject);
            }
            b = Limites(modelo);
        }
        if (manterMetade != 0)
        {
            PrefabUtility.UnpackPrefabInstance(modelo, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            bool eixoX = b.size.x >= b.size.z;
            foreach (var r in modelo.GetComponentsInChildren<Renderer>())
            {
                float d = eixoX ? r.bounds.center.x - b.center.x : r.bounds.center.z - b.center.z;
                if (d * manterMetade < 0) Object.DestroyImmediate(r.gameObject);
            }
            b = Limites(modelo);
        }
        float lado = Mathf.Max(b.size.x, b.size.y, b.size.z);
        if (lado > 0.0001f) modelo.transform.localScale *= maiorLado / lado;
        b = Limites(modelo);
        modelo.transform.localPosition -= new Vector3(b.center.x, b.min.y, b.center.z);

        foreach (var r in modelo.GetComponentsInChildren<Renderer>())
            r.shadowCastingMode = ShadowCastingMode.On;

        if (colisao == Colisao.Caixa)
        {
            var bc = suporte.gameObject.AddComponent<BoxCollider>();
            bc.center = new Vector3(0, b.size.y / 2f, 0);
            bc.size = b.size;
        }

        suporte.SetParent(pai, false);
        suporte.localPosition = pos;
        suporte.localRotation = Quaternion.Euler(0, rotY, 0);
        return suporte.gameObject;
    }
}
