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

    // Coordenadas do mundo. O escritório (6 x 6 m) fica na origem, com a porta ao sul (z = -3).
    const float CalcadaNorteZ0 = -3.15f, CalcadaNorteZ1 = -5.5f;
    const float AsfaltoZ1 = -12.5f, CalcadaSulZ1 = -15f;
    const float RuaX = 34f;                  // a rua vai de -34 a +34 em x
    static readonly Vector3 CentroCasa = new Vector3(0f, 0f, -19.5f);

    static void MontarRuaECasaDentro()
    {
        CriarMateriaisRua();
        MontarRua();
        portasAbrem = true;
        MontarCasa();
        portasAbrem = false;
        MontarPredios();
    }

    static void CriarMateriaisRua()
    {
        mAsfalto = Mat("Asfalto", new Color(0.17f, 0.17f, 0.18f), 0.15f);
        mCalcada = Mat("Calcada", new Color(0.62f, 0.61f, 0.58f), 0.1f);
        mFaixa = Mat("Faixa_Branca", new Color(0.92f, 0.92f, 0.9f), 0.2f);
        mParedeCasa = MatPBR("Parede_Casa", "PaintedPlaster017", new Color(0.96f, 0.88f, 0.76f), 0.12f);
        mLampadaApagada = Mat("Lampada_Apagada", new Color(0.8f, 0.8f, 0.75f), 0.6f);
        mLampadaAcesa = Mat("Lampada_Acesa", new Color(1f, 0.92f, 0.75f), 0.6f);
        Brilhar(mLampadaAcesa, new Color(1f, 0.85f, 0.6f) * 2.2f);
        mJanelaPredio = Mat("Janela_Predio", new Color(0.12f, 0.16f, 0.2f), 0.9f);
    }

    // ---------------- Rua ----------------

    static void MontarRua()
    {
        var rua = new GameObject("Rua").transform;
        rua.SetParent(raiz, false);

        float largura = RuaX * 2f;
        float zAsfalto = (CalcadaNorteZ1 + AsfaltoZ1) / 2f, profAsfalto = CalcadaNorteZ1 - AsfaltoZ1;

        // Calçadas 12 cm acima do asfalto (o meio-fio é o degrau)
        Caixa(rua, "Calcada_Norte", new Vector3(0f, 0.05f, (CalcadaNorteZ0 + CalcadaNorteZ1) / 2f),
            new Vector3(largura, 0.14f, CalcadaNorteZ0 - CalcadaNorteZ1), mCalcada, 1.5f);
        Caixa(rua, "Asfalto", new Vector3(0f, -0.01f, zAsfalto), new Vector3(largura, 0.04f, profAsfalto), mAsfalto, 3f);
        Caixa(rua, "Calcada_Sul", new Vector3(0f, 0.05f, (AsfaltoZ1 + CalcadaSulZ1) / 2f),
            new Vector3(largura, 0.14f, AsfaltoZ1 - CalcadaSulZ1), mCalcada, 1.5f);

        // Faixa central tracejada (sem a parte da faixa de pedestre)
        for (float x = -RuaX + 1.5f; x < RuaX - 1f; x += 3f)
            if (Mathf.Abs(x) > 2.5f)
                Caixa(rua, "Faixa_Central", new Vector3(x, 0.012f, zAsfalto), new Vector3(1.5f, 0.005f, 0.12f), mFaixa, 1f, false, false);

        // Faixa de pedestre ligando a porta do escritório à casa
        for (float z = CalcadaNorteZ1 - 0.4f; z > AsfaltoZ1 + 0.3f; z -= 0.8f)
            Caixa(rua, "Faixa_Pedestre", new Vector3(0f, 0.012f, z), new Vector3(3f, 0.005f, 0.4f), mFaixa, 1f, false, false);

        // Postes (acendem no fim da tarde)
        Poste(rua, new Vector3(-7f, 0.12f, CalcadaNorteZ1 + 0.3f), -1f);
        Poste(rua, new Vector3(7f, 0.12f, CalcadaNorteZ1 + 0.3f), -1f);
        Poste(rua, new Vector3(-20f, 0.12f, CalcadaNorteZ1 + 0.3f), -1f);
        Poste(rua, new Vector3(3.5f, 0.12f, AsfaltoZ1 - 0.3f), 1f);
        Poste(rua, new Vector3(16f, 0.12f, AsfaltoZ1 - 0.3f), 1f);
    }

    // 'lado' = para onde o braço aponta em z (-1 = para o sul, sobre a rua)
    static void Poste(Transform pai, Vector3 base_, float lado)
    {
        var p = new GameObject("Poste").transform;
        p.SetParent(pai, false);
        p.localPosition = base_;

        const float altura = 4.6f;
        Caixa(p, "Haste", new Vector3(0f, altura / 2f, 0f), new Vector3(0.12f, altura, 0.12f), mAluminio, 1f);
        Caixa(p, "Braco", new Vector3(0f, altura - 0.05f, lado * 0.6f), new Vector3(0.07f, 0.07f, 1.2f), mAluminio, 1f, false);
        var lampada = Caixa(p, "Luminaria", new Vector3(0f, altura - 0.12f, lado * 1.1f), new Vector3(0.28f, 0.08f, 0.45f), mLampadaAcesa, 1f, false, false);

        var go = new GameObject("Luz");
        go.transform.SetParent(p, false);
        go.transform.localPosition = new Vector3(0f, altura - 0.35f, lado * 1.1f);
        var luz = go.AddComponent<Light>();
        luz.type = LightType.Point;
        luz.color = new Color(1f, 0.85f, 0.62f);
        luz.intensity = 1.8f;
        luz.range = 13f;
        luz.shadows = LightShadows.None;

        var poste = go.AddComponent<PosteLuz>();
        poste.lampada = lampada.GetComponent<Renderer>();
        poste.acesa = mLampadaAcesa;
        poste.apagada = mLampadaApagada;
    }

    // ---------------- Casa do jogador ----------------

    static void MontarCasa()
    {
        // 8 x 6 m, porta virada para a rua (norte), um cômodo com quarto, sala e cozinha
        var casa = Sala("Casa", CentroCasa, 8f, 6f, mPisoMadeira, 1.6f, mParedeCasa,
            norte: new[] { Janela(-1.7f), Porta(1.5f) },
            sul: new[] { Janela(1.8f) },
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
        Modelo("rugDoormat", casa, new Vector3(1.5f, 0.012f, 2.55f), 0, Colisao.Nenhuma);

        MontarQuarto(casa);
        MontarHomeOffice(casa);
        Persianas(casa);

        // Sala (canto sudeste): sofá olhando para a TV — Kenney por enquanto (Fase 1B)
        Modelo("loungeSofa", casa, new Vector3(2.4f, 0f, -2.45f), 0);
        var rack = Modelo("cabinetTelevision", casa, new Vector3(2.4f, 0f, -0.3f), 180);
        Modelo("televisionModern", casa, new Vector3(2.4f, Altura(rack), -0.3f), 180, Colisao.Nenhuma);
        Modelo("rugRectangle", casa, new Vector3(2.4f, 0.012f, -1.4f), 0, Colisao.Nenhuma);

        // Cozinha na parede leste, entre a janela e a porta — Kenney por enquanto (Fase 1B)
        Modelo("kitchenFridge", casa, new Vector3(3.55f, 0f, 2.55f), -90);
        Modelo("kitchenSink", casa, new Vector3(3.6f, 0f, 1.7f), -90);
        Modelo("kitchenStove", casa, new Vector3(3.6f, 0f, 1.0f), -90);
        MontarCafeteira(casa);
        Modelo("coatRackStanding", casa, new Vector3(2.45f, 0f, 2.6f), 0);

        // Onde o jogador acorda (o DiaManager procura "SpawnCasa"): ao lado da cama, olhando para a porta
        Ponto("SpawnCasa", casa, new Vector3(0.2f, 1f, -1.2f), 0);
    }

    // ---------------- Quarto e home office (modelos realistas, Fase 1A) ----------------
    // Casa: x de -4 a 4 (oeste → leste), z de -3 a 3 (sul → norte, a rua fica ao norte).

    const string PastaPolyHaven = "Assets/Models/PolyHaven/";
    const string PastaSketchfab = "Assets/Models/Sketchfab/";
    static string PH(string id) => PastaPolyHaven + id + "/" + id + ".gltf";
    static string SF(string nome) => PastaSketchfab + nome + ".glb";

    static void MontarQuarto(Transform casa)
    {
        // Cama com a cabeceira na parede sul
        var cama = ModeloReal(SF("Cama_Casal"), casa, new Vector3(-2.35f, 0f, -1.95f), 2.1f, RotCama);
        if (cama != null) cama.AddComponent<Cama>().distanciaInteracao = 2.0f;   // da base da cama ao jogador

        var criadoEsq = ModeloReal(PH("ClassicNightstand_01"), casa, new Vector3(-3.55f, 0f, -2.72f), 0.5f, 0);
        var criadoDir = ModeloReal(PH("ClassicNightstand_01"), casa, new Vector3(-1.15f, 0f, -2.72f), 0.5f, 0);
        ModeloReal(PH("alarm_clock_01"), casa, new Vector3(-1.15f, Altura(criadoDir), -2.7f), 0.12f, 160, Colisao.Nenhuma);
        ModeloReal(PH("standing_picture_frame_01"), casa, new Vector3(-3.5f, Altura(criadoEsq), -2.78f), 0.2f, 15, Colisao.Nenhuma);
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
        var mesa = ModeloReal(PH("metal_office_desk"), casa, new Vector3(-3.0f, 0f, 2.55f), 1.5f, RotMesa);
        float tampo = Altura(mesa);
        ModeloReal(SF("Cadeira_Escritorio"), casa, new Vector3(-3.0f, 0f, 1.75f), 1.15f, RotCadeira);
        var pc = ModeloReal(SF("Computador_Mesa"), casa, new Vector3(-3.1f, tampo, 2.6f), 0.85f, 180, Colisao.Nenhuma);
        if (pc != null) ConfigurarPcDeCasa(pc, casa);
        ModeloReal(PH("desk_lamp_arm_01"), casa, new Vector3(-3.42f, tampo, 2.72f), 0.5f, 135, Colisao.Nenhuma);
        ModeloReal(PH("office_notepads"), casa, new Vector3(-2.45f, tampo, 2.35f), 0.3f, 20, Colisao.Nenhuma);
        ModeloReal(PH("potted_plant_04"), casa, new Vector3(-2.35f, tampo, 2.75f), 0.25f, 0, Colisao.Nenhuma);
        var relogio = ModeloReal(PH("wall_clock"), casa, new Vector3(-3.0f, 2.15f, 2.97f), 0.32f, RotRelogio, Colisao.Nenhuma);
        if (relogio != null) relogio.AddComponent<RelogioAnalogico>().frente = -casa.forward;   // mostrador para o sul
        ModeloReal(SF("Mochila"), casa, new Vector3(-2.2f, 0f, 2.4f), 0.5f, 200);

        // Estante de aço com as ferramentas, na parede norte entre a janela e a porta
        var estante = ModeloReal(PH("steel_frame_shelves_03"), casa, new Vector3(-0.35f, 0f, 2.68f), 1.8f, RotEstante);
        ModeloReal(PH("metal_toolbox"), casa, new Vector3(-0.35f, 0f, 2.25f), 0.5f, 10);
        ModeloReal(PH("cardboard_box_01"), casa, new Vector3(0.25f, 0f, 2.45f), 0.45f, 25);
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

    // Balcão sob a janela leste com a cafeteira (café = energia) e a xícara que aparece quando fica pronto
    static void MontarCafeteira(Transform casa)
    {
        var balcao = Modelo("kitchenCabinet", casa, new Vector3(3.6f, 0f, 0.2f), -90);
        float topo = Altura(balcao);
        var maquina = Modelo("kitchenCoffeeMachine", casa, new Vector3(3.68f, topo, 0.05f), -90, Colisao.Nenhuma);
        if (maquina == null) return;

        var mXicara = Mat("Xicara_Branca", new Color(0.96f, 0.95f, 0.92f), 0.7f);
        var mCafe = Mat("Cafe", new Color(0.18f, 0.1f, 0.05f), 0.85f);
        var xicara = new GameObject("Xicara").transform;
        xicara.SetParent(casa, false);
        xicara.localPosition = new Vector3(3.55f, topo, 0.42f);
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
        Renderer monitor = pc.GetComponentsInChildren<Renderer>()
            .FirstOrDefault(r => r.transform.parent != null && r.transform.parent.name.Contains("Computer"));
        if (monitor == null) monitor = pc.GetComponentsInChildren<Renderer>().OrderByDescending(r => r.bounds.size.y).First();
        var b = monitor.bounds;   // mundo; o monitor está virado para o sul (-Z da casa)
        Vector3 fora = -casa.forward;

        var tela = new GameObject("TelaPC").transform;
        tela.SetParent(pc.transform, true);
        tela.position = new Vector3(b.center.x, b.center.y + b.size.y * 0.03f, b.min.z - 0.004f);
        tela.rotation = Quaternion.LookRotation(fora);
        var t = tela.gameObject.AddComponent<TelaComputador>();
        t.tamanho = new Vector2(b.size.x * 0.9f, b.size.y * 0.8f);

        var pose = new GameObject("PoseSentado").transform;
        pose.SetParent(pc.transform, true);
        pose.position = new Vector3(b.center.x + 0.05f, b.center.y + 0.05f, b.min.z - 0.62f);
        pose.rotation = Quaternion.LookRotation(tela.position - pose.position);

        var uso = pc.AddComponent<ComputadorCasa>();
        uso.poseSentado = pose;
        Debug.Log($"[Cenário] PC de casa: monitor {monitor.transform.parent?.name} {b.size.x:0.00} x {b.size.y:0.00} m");
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
        Colisao colisao = Colisao.Caixa, int manterMetade = 0)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(caminho);
        if (prefab == null) { Debug.LogError("[Cenário] Modelo não encontrado: " + caminho); return null; }

        var suporte = new GameObject(System.IO.Path.GetFileNameWithoutExtension(caminho)).transform;
        var modelo = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        modelo.transform.SetParent(suporte, false);

        var b = Limites(modelo);
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

    // ---------------- Vizinhança ----------------

    static void MontarPredios()
    {
        var viz = new GameObject("Vizinhanca").transform;
        viz.SetParent(raiz, false);

        // Lado do escritório (frente para o sul) e lado da casa (frente para o norte)
        Predio(viz, new Vector3(-11f, 0f, 1.5f), new Vector3(9f, 7f, 7f), new Color(0.78f, 0.74f, 0.68f), -1f);
        Predio(viz, new Vector3(11f, 0f, 1f), new Vector3(8f, 4.5f, 6f), new Color(0.68f, 0.74f, 0.8f), -1f);
        Predio(viz, new Vector3(-23f, 0f, 2f), new Vector3(10f, 10f, 8f), new Color(0.72f, 0.7f, 0.74f), -1f);
        Predio(viz, new Vector3(-11f, 0f, -21f), new Vector3(8f, 4f, 9f), new Color(0.82f, 0.78f, 0.7f), 1f);
        Predio(viz, new Vector3(11f, 0f, -21.5f), new Vector3(9f, 6f, 10f), new Color(0.74f, 0.8f, 0.76f), 1f);
        Predio(viz, new Vector3(24f, 0f, -21f), new Vector3(10f, 9f, 9f), new Color(0.7f, 0.72f, 0.78f), 1f);
    }

    // Bloco com janelas na fachada virada para a rua ('frente' = sinal de z da fachada)
    static void Predio(Transform pai, Vector3 base_, Vector3 tam, Color cor, float frente)
    {
        var p = new GameObject("Predio").transform;
        p.SetParent(pai, false);
        p.localPosition = base_;
        var mat = Mat("Predio_" + Mathf.RoundToInt(cor.r * 100) + "_" + Mathf.RoundToInt(cor.b * 100), cor, 0.1f);
        Caixa(p, "Bloco", new Vector3(0f, tam.y / 2f, 0f), tam, mat, 2.5f);

        float zFachada = frente * (tam.z / 2f + 0.01f);
        int andares = Mathf.Max(1, Mathf.FloorToInt(tam.y / 3f));
        int colunas = Mathf.Max(1, Mathf.FloorToInt(tam.x / 2.2f));
        for (int a = 0; a < andares; a++)
            for (int c = 0; c < colunas; c++)
            {
                float x = -tam.x / 2f + (c + 0.5f) * tam.x / colunas;
                float y = 1.6f + a * 3f;
                if (y + 0.6f > tam.y) continue;
                Caixa(p, "Janela", new Vector3(x, y, zFachada), new Vector3(1.1f, 1.2f, 0.02f), mJanelaPredio, 1f, false, false);
            }
    }
}
