using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;
using PP = UnityEngine.Rendering.PostProcessing;

// Monta o cenário com aparência realista:
// - salas com paredes de verdade (2,80 m), porta e janelas com vidro, rodapé e forro com luminárias;
// - materiais PBR do ambientCG (CC0) em Assets/Textures/ambientCG;
// - móveis do Kenney Furniture Kit (CC0), com madeira texturizada;
// - luz do sol entrando pelas janelas, luzes de teto, reflexos e pós-processamento.
// Pode ser rodado de novo: apaga o "Cenario" antigo e monta outro.
public static partial class MontarCenario
{
    const string PastaModelos = "Assets/Models/Kenney/Furniture/";
    const string PastaMateriais = "Assets/Materials/Cenario";
    const string PastaTexturas = "Assets/Textures/ambientCG/";
    const string CaminhoPerfil = "Assets/Settings/PosProcessamento.asset";
    const string CaminhoRecursosPP = "Packages/com.unity.postprocessing/PostProcessing/PostProcessResources.asset";

    const float Escala = 2f;     // o kit do Kenney é pequeno (mesa com 38 cm): 2x deixa no tamanho do jogador
    const float H = 2.8f;        // pé-direito
    const float T = 0.15f;       // espessura das paredes
    const bool MadeiraNosMoveis = true;

    enum Colisao { Nenhuma, Caixa, Malha }

    struct Abertura
    {
        public float centro, largura, baixo, alto;
        public bool porta;
    }

    static Abertura Janela(float centro, float largura = 1.2f) =>
        new Abertura { centro = centro, largura = largura, baixo = 0.9f, alto = 2.1f };

    static Abertura Porta(float centro) =>
        new Abertura { centro = centro, largura = 1.0f, baixo = 0f, alto = 2.1f, porta = true };

    static float correcaoFbx = 1f;
    static Transform raiz;
    static readonly HashSet<string> materiaisKenney = new HashSet<string>();

    // Materiais do cenário
    static Material mPisoMadeira, mCarpete, mParedeEscritorio, mParedeCliente, mForro, mMadeira,
                    mRodape, mAluminio, mVidro, mMetal, mGrama;

    [MenuItem("TI Service/3. Montar cenário (escritório, cliente, rua e casa)")]
    public static void Montar()
    {
        if (GameObject.FindWithTag("Player") == null || Object.FindAnyObjectByType<MissionManager>() == null)
        {
            EditorUtility.DisplayDialog("Montar cenário", "Abra a SampleScene antes de montar o cenário.", "OK");
            return;
        }
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PastaModelos + "wall.fbx") == null)
        {
            EditorUtility.DisplayDialog("Montar cenário", "Modelos do Kenney não encontrados em " + PastaModelos, "OK");
            return;
        }

        CalibrarEscalaFbx();
        CriarMateriais();
        materiaisKenney.Clear();

        var antigo = GameObject.Find("Cenario");
        if (antigo != null) Undo.DestroyObjectImmediate(antigo);
        raiz = new GameObject("Cenario").transform;
        Undo.RegisterCreatedObjectUndo(raiz.gameObject, "Montar cenário");

        // Esconde o cenário antigo (não apaga, para poder voltar se quiser)
        foreach (var nome in new[] { "Parede_Frente", "Parede_Tras", "Parede_Esquerda", "Parede_Direita", "Mesa", "Pé_Mesa", "Plane" })
        {
            var go = GameObject.Find(nome);
            if (go != null) { Undo.RecordObject(go, "Esconder cenário antigo"); go.SetActive(false); }
        }

        // Gramado em volta das salas (é o que se vê pelas janelas)
        Caixa(raiz, "Gramado", new Vector3(20f, -0.07f, 0f), new Vector3(160f, 0.1f, 80f), mGrama, 4f);

        portasAbrem = true;            // escritório: a porta abre e dá para a rua
        var spawn = MontarEscritorio();
        portasAbrem = false;           // cliente: a porta leva de volta (Teleporte)
        MontarCliente(spawn);
        MontarRuaECasaDentro();
        AjustarJogador(spawn);
        Iluminacao();

        Debug.Log("[Cenário] Materiais dos móveis do Kenney: " + string.Join(", ", materiaisKenney.OrderBy(n => n)));

        var cena = EditorSceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(cena);
        EditorSceneManager.SaveScene(cena);
        AssetDatabase.SaveAssets();
        Debug.Log("[Cenário] Escritório, cliente, rua e casa montados. Cena salva.");
    }

    // ---------------- Escritório do jogador ----------------

    static Transform MontarEscritorio()
    {
        // Quartinho de 6 x 6 m, piso de madeira, porta ao sul e janelas
        var sala = Sala("Escritorio", new Vector3(0, 0, 0), 6f, 6f, mPisoMadeira, 1.6f, mParedeEscritorio,
            norte: new[] { Janela(-1.6f), Janela(1.6f) },
            sul: new[] { Porta(0f) },
            leste: new[] { Janela(-1.3f) },
            oeste: new[] { Janela(-1.4f) });

        LuzTeto(sala, "Luz_Teto", new Vector3(0, H - 0.15f, 0.4f), 1.5f, 7.5f, true);
        Sonda(sala, new Vector3(6f, H, 6f));

        Modelo("rugRectangle", sala, new Vector3(0, 0.012f, 0.2f), 0, Colisao.Nenhuma);
        var mesa = Modelo("desk", sala, new Vector3(0, 0, 2.35f), 180);
        float alturaMesa = Altura(mesa);
        Modelo("chairDesk", sala, new Vector3(0, 0, 1.45f), 180);
        Modelo("plantSmall1", sala, new Vector3(0.55f, alturaMesa, 2.45f), 0, Colisao.Nenhuma);
        Modelo("trashcan", sala, new Vector3(1.05f, 0, 2.5f), 0);

        PosicionarEquipamento("Notebook", sala, new Vector3(-0.1f, alturaMesa, 2.3f), 180,
            v => Modelo("laptop", v, Vector3.zero, 0, Colisao.Nenhuma));

        var estante = Modelo("bookcaseOpen", sala, new Vector3(-2.7f, 0, 0.6f), 90);
        Modelo("books", sala, new Vector3(-2.7f, Altura(estante), 0.6f), 90, Colisao.Nenhuma);
        Modelo("lampSquareFloor", sala, new Vector3(-2.5f, 0, 2.5f), 0);

        var caixa = Modelo("cardboardBoxClosed", sala, new Vector3(2.45f, 0, 2.45f), 10);
        Modelo("cardboardBoxClosed", sala, new Vector3(2.4f, Altura(caixa), 2.45f), -15);
        Modelo("cardboardBoxOpen", sala, new Vector3(1.85f, 0, 2.55f), 25);

        var aparador = Modelo("sideTable", sala, new Vector3(2.7f, 0, 0.4f), -90);
        Modelo("kitchenCoffeeMachine", sala, new Vector3(2.7f, Altura(aparador), 0.4f), -90, Colisao.Nenhuma);
        Modelo("pottedPlant", sala, new Vector3(2.45f, 0, -2.45f), 0);
        Modelo("coatRackStanding", sala, new Vector3(-2.45f, 0, -2.45f), 0);

        return Ponto("SpawnEscritorio", sala, new Vector3(0, 1f, -1.2f), 0);
    }

    // ---------------- Escritório do cliente ----------------

    static void MontarCliente(Transform spawnEscritorio)
    {
        // Escritório de 10 x 8 m longe do quartinho, com carpete
        var sala = Sala("Cliente", new Vector3(40, 0, 0), 10f, 8f, mCarpete, 1.5f, mParedeCliente,
            norte: new[] { Janela(-0.4f, 1.4f), Janela(3.3f, 1.4f) },
            sul: new[] { Porta(0f) },
            leste: new[] { Janela(-1.8f, 1.4f) },
            oeste: new[] { Janela(-0.4f) });

        LuzTeto(sala, "Luz_Teto_1", new Vector3(-2.5f, H - 0.15f, 1.6f), 1.3f, 6.5f, true);
        LuzTeto(sala, "Luz_Teto_2", new Vector3(2.5f, H - 0.15f, 1.6f), 1.3f, 6.5f, false);
        LuzTeto(sala, "Luz_Teto_3", new Vector3(0f, H - 0.15f, -2.2f), 1.2f, 6.5f, false);
        Sonda(sala, new Vector3(10f, H, 8f));

        Modelo("rugRectangle", sala, new Vector3(0, 0.012f, 0.5f), 90, Colisao.Nenhuma);

        // Estação 1: o Computador do chamado
        var mesa1 = Modelo("desk", sala, new Vector3(-2.8f, 0, 3.35f), 180);
        float h = Altura(mesa1);
        Modelo("chairDesk", sala, new Vector3(-2.8f, 0, 2.45f), 180);
        // O monitor da Kenney tem a tela em -z: rotação 0 deixa a tela virada para a cadeira
        PosicionarEquipamento("Computador", sala, new Vector3(-2.8f, h, 3.45f), 0,
            v => Modelo("computerScreen", v, Vector3.zero, 0, Colisao.Nenhuma));
        Modelo("computerKeyboard", sala, new Vector3(-2.8f, h, 3.05f), 180, Colisao.Nenhuma);
        Modelo("computerMouse", sala, new Vector3(-2.35f, h, 3.05f), 180, Colisao.Nenhuma);

        // Estação 2: só decoração
        var mesa2 = Modelo("desk", sala, new Vector3(1.2f, 0, 3.35f), 180);
        Modelo("chairDesk", sala, new Vector3(1.2f, 0, 2.45f), 200);
        Modelo("computerScreen", sala, new Vector3(1.2f, Altura(mesa2), 3.45f), 0, Colisao.Nenhuma);
        Modelo("computerKeyboard", sala, new Vector3(1.2f, Altura(mesa2), 3.05f), 180, Colisao.Nenhuma);

        // Impressora em cima de um armário, na parede leste
        var armario = Modelo("kitchenCabinet", sala, new Vector3(4.5f, 0, 0.8f), -90);
        // O visual da impressora tem a frente em -z: 90° deixa a frente virada para a sala
        PosicionarEquipamento("Impressora", sala, new Vector3(4.45f, Altura(armario), 0.8f), 90, VisualImpressora);

        // Roteador em cima de um aparador, na parede oeste
        var aparador = Modelo("sideTable", sala, new Vector3(-4.55f, 0, -1.8f), 90);
        PosicionarEquipamento("Roteador", sala, new Vector3(-4.5f, Altura(aparador), -1.8f), -90, VisualRoteador);

        // Decoração
        Modelo("bookcaseClosedWide", sala, new Vector3(-4.6f, 0, 1.6f), 90);
        Modelo("loungeSofa", sala, new Vector3(2.6f, 0, -3.45f), 0);
        Modelo("tableCoffee", sala, new Vector3(2.6f, 0, -2.4f), 0);
        Modelo("pottedPlant", sala, new Vector3(4.45f, 0, -3.45f), 0);
        Modelo("pottedPlant", sala, new Vector3(-4.45f, 0, 3.45f), 0);
        Modelo("lampSquareFloor", sala, new Vector3(4.5f, 0, 3.5f), 0);
        Modelo("trashcan", sala, new Vector3(-1.5f, 0, 3.5f), 0);
        Modelo("coatRackStanding", sala, new Vector3(-4.45f, 0, -3.45f), 0);

        // Onde o jogador chega ao aceitar um chamado (objeto já usado pelo MissionManager)
        var chegada = GameObject.Find("ClienteLocation");
        if (chegada != null)
        {
            Undo.RecordObject(chegada.transform, "Mover chegada");
            chegada.transform.position = sala.TransformPoint(new Vector3(0, 1f, -2.6f));
            chegada.transform.rotation = Quaternion.Euler(0, 0, 0);
        }

        // Porta de volta para o escritório
        var porta = new GameObject("Porta_VoltarEscritorio");
        porta.transform.SetParent(sala, false);
        porta.transform.localPosition = new Vector3(0, 1f, -3.75f);
        var tp = porta.AddComponent<Teleporte>();
        tp.destino = spawnEscritorio;
        tp.rotulo = "Voltar ao escritório";
        tp.distanciaInteracao = 1.8f;
    }

    // ---------------- Equipamentos montados com formas simples ----------------

    static void VisualImpressora(Transform v)
    {
        var corpo = Mat("Impressora_Corpo", new Color(0.86f, 0.87f, 0.89f), 0.35f);
        var escuro = Mat("Impressora_Escuro", new Color(0.18f, 0.2f, 0.23f), 0.4f);
        var papel = Mat("Papel", Color.white, 0.1f);
        var tela = Mat("LED_Verde", new Color(0.2f, 0.9f, 0.45f), 0.6f);
        Brilhar(tela, new Color(0.2f, 0.9f, 0.45f) * 1.5f);
        Cubo(v, "Corpo", new Vector3(0, 0.15f, 0), new Vector3(0.52f, 0.3f, 0.42f), corpo);
        Cubo(v, "Tampa", new Vector3(0, 0.31f, 0.05f), new Vector3(0.52f, 0.02f, 0.32f), escuro);
        Cubo(v, "PapelEntrada", new Vector3(0, 0.36f, 0.14f), new Vector3(0.3f, 0.08f, 0.02f), papel);
        Cubo(v, "Bandeja", new Vector3(0, 0.12f, -0.27f), new Vector3(0.36f, 0.015f, 0.14f), escuro);
        Cubo(v, "PapelSaida", new Vector3(0, 0.13f, -0.27f), new Vector3(0.28f, 0.01f, 0.12f), papel);
        Cubo(v, "Painel", new Vector3(0.18f, 0.305f, -0.14f), new Vector3(0.1f, 0.012f, 0.06f), tela);
    }

    static void VisualRoteador(Transform v)
    {
        var preto = Mat("Roteador_Preto", new Color(0.08f, 0.09f, 0.11f), 0.55f);
        var led = Mat("LED_Verde", new Color(0.2f, 0.9f, 0.45f), 0.6f);
        Cubo(v, "Corpo", new Vector3(0, 0.035f, 0), new Vector3(0.34f, 0.07f, 0.22f), preto);
        Cubo(v, "AntenaEsq", new Vector3(-0.13f, 0.19f, 0.09f), new Vector3(0.02f, 0.26f, 0.02f), preto);
        Cubo(v, "AntenaDir", new Vector3(0.13f, 0.19f, 0.09f), new Vector3(0.02f, 0.26f, 0.02f), preto);
        for (int i = 0; i < 4; i++)
            Cubo(v, "LED" + i, new Vector3(-0.09f + i * 0.06f, 0.045f, -0.112f), new Vector3(0.022f, 0.012f, 0.006f), led);
    }

    // ---------------- Construção das salas ----------------

    // Sala com o interior de 'largura' (x) por 'profundidade' (z); as aberturas usam coordenadas da sala:
    // norte/sul pelo x, leste/oeste pelo z.
    static Transform Sala(string nome, Vector3 centro, float largura, float profundidade, Material piso, float metrosPiso,
        Material parede, Abertura[] norte, Abertura[] sul, Abertura[] leste, Abertura[] oeste)
    {
        var sala = new GameObject(nome).transform;
        sala.SetParent(raiz, false);
        sala.position = centro;

        float w = largura / 2f, d = profundidade / 2f;

        Caixa(sala, "Piso", new Vector3(0, -0.05f, 0), new Vector3(largura + 2 * T, 0.1f, profundidade + 2 * T), piso, metrosPiso);
        Caixa(sala, "Forro", new Vector3(0, H + 0.05f, 0), new Vector3(largura + 2 * T, 0.1f, profundidade + 2 * T), mForro, 4f);

        // Cada parede tem o eixo x local ao longo dela e o z local apontando para fora da sala
        Parede(sala, "Parede_Norte", new Vector3(0, 0, d), 0, largura + 2 * T, largura, parede,
            norte?.Select(a => Mover(a, a.centro)));
        Parede(sala, "Parede_Sul", new Vector3(0, 0, -d), 180, largura + 2 * T, largura, parede,
            sul?.Select(a => Mover(a, -a.centro)));
        Parede(sala, "Parede_Leste", new Vector3(w, 0, 0), 90, profundidade, profundidade, parede,
            leste?.Select(a => Mover(a, -a.centro)));
        Parede(sala, "Parede_Oeste", new Vector3(-w, 0, 0), -90, profundidade, profundidade, parede,
            oeste?.Select(a => Mover(a, a.centro)));
        return sala;
    }

    static Abertura Mover(Abertura a, float novoCentro) { a.centro = novoCentro; return a; }

    static void Parede(Transform sala, string nome, Vector3 pos, float rotY, float comprimento, float interior,
        Material mat, IEnumerable<Abertura> aberturas)
    {
        var p = new GameObject(nome).transform;
        p.SetParent(sala, false);
        p.localPosition = pos;
        p.localRotation = Quaternion.Euler(0, rotY, 0);

        var lista = (aberturas ?? Enumerable.Empty<Abertura>()).OrderBy(a => a.centro).ToList();

        void Bloco(float x0, float x1, float y0, float y1)
        {
            if (x1 - x0 < 0.001f || y1 - y0 < 0.001f) return;
            Caixa(p, "Bloco", new Vector3((x0 + x1) / 2f, (y0 + y1) / 2f, T / 2f), new Vector3(x1 - x0, y1 - y0, T), mat, 2.5f);
        }

        float x = -comprimento / 2f;
        foreach (var a in lista)
        {
            float a0 = a.centro - a.largura / 2f, a1 = a.centro + a.largura / 2f;
            Bloco(x, a0, 0, H);
            Bloco(a0, a1, 0, a.baixo);
            Bloco(a0, a1, a.alto, H);
            if (a.porta) MontarPorta(p, a); else MontarJanela(p, a);
            x = a1;
        }
        Bloco(x, comprimento / 2f, 0, H);

        // Rodapé ao longo do lado de dentro, interrompido nas portas
        float r = -interior / 2f;
        foreach (var a in lista.Where(a => a.porta))
        {
            float a0 = a.centro - a.largura / 2f;
            if (a0 > r) Caixa(p, "Rodape", new Vector3((r + a0) / 2f, 0.045f, -0.008f), new Vector3(a0 - r, 0.09f, 0.016f), mRodape, 1f, false);
            r = a.centro + a.largura / 2f;
        }
        if (interior / 2f > r)
            Caixa(p, "Rodape", new Vector3((r + interior / 2f) / 2f, 0.045f, -0.008f), new Vector3(interior / 2f - r, 0.09f, 0.016f), mRodape, 1f, false);
    }

    static void MontarJanela(Transform p, Abertura a)
    {
        float c = a.centro, l = a.largura, b = a.baixo, t = a.alto, meio = (b + t) / 2f, alt = t - b;
        const float f = 0.05f, prof = 0.07f;
        var j = new GameObject("Janela").transform;
        j.SetParent(p, false);

        Caixa(j, "Vidro", new Vector3(c, meio, T / 2f), new Vector3(l, alt, 0.01f), mVidro, 1f, true, false);
        Caixa(j, "Moldura_Topo", new Vector3(c, t - f / 2f, T / 2f), new Vector3(l, f, prof), mAluminio, 1f, false);
        Caixa(j, "Moldura_Base", new Vector3(c, b + f / 2f, T / 2f), new Vector3(l, f, prof), mAluminio, 1f, false);
        Caixa(j, "Moldura_Esq", new Vector3(c - l / 2f + f / 2f, meio, T / 2f), new Vector3(f, alt, prof), mAluminio, 1f, false);
        Caixa(j, "Moldura_Dir", new Vector3(c + l / 2f - f / 2f, meio, T / 2f), new Vector3(f, alt, prof), mAluminio, 1f, false);
        Caixa(j, "Moldura_Meio", new Vector3(c, meio, T / 2f), new Vector3(0.035f, alt, 0.06f), mAluminio, 1f, false);
        // Peitoril: avança um pouco para dentro da sala
        Caixa(j, "Peitoril", new Vector3(c, b - 0.015f, 0f), new Vector3(l + 0.12f, 0.03f, 0.22f), mRodape, 1f, false);
    }

    static void MontarPorta(Transform p, Abertura a)
    {
        float c = a.centro, l = a.largura, t = a.alto;
        const float bt = 0.06f;
        var j = new GameObject("Porta").transform;
        j.SetParent(p, false);

        Caixa(j, "Batente_Esq", new Vector3(c - l / 2f + bt / 2f, t / 2f, T / 2f), new Vector3(bt, t, T + 0.03f), mRodape, 1f, false);
        Caixa(j, "Batente_Dir", new Vector3(c + l / 2f - bt / 2f, t / 2f, T / 2f), new Vector3(bt, t, T + 0.03f), mRodape, 1f, false);
        Caixa(j, "Batente_Topo", new Vector3(c, t - bt / 2f, T / 2f), new Vector3(l, bt, T + 0.03f), mRodape, 1f, false);

        // Folha, maçanetas e espelhos ficam dentro da dobradiça (lado esquerdo), para a porta poder abrir
        var dobradica = new GameObject("Dobradica").transform;
        dobradica.SetParent(j, false);
        dobradica.localPosition = new Vector3(c - l / 2f + bt, 0f, T / 2f);
        var folha = new List<GameObject>
        {
            Caixa(j, "Folha", new Vector3(c, (t - bt) / 2f + 0.005f, T / 2f), new Vector3(l - 2 * bt - 0.01f, t - bt - 0.01f, 0.045f), mMadeira, 1.2f)
        };
        float xm = c + l / 2f - bt - 0.12f;
        folha.Add(Caixa(j, "Macaneta_Dentro", new Vector3(xm, 1.0f, T / 2f - 0.045f), new Vector3(0.13f, 0.022f, 0.022f), mMetal, 1f, false));
        folha.Add(Caixa(j, "Macaneta_Fora", new Vector3(xm, 1.0f, T / 2f + 0.045f), new Vector3(0.13f, 0.022f, 0.022f), mMetal, 1f, false));
        folha.Add(Caixa(j, "Espelho_Dentro", new Vector3(xm + 0.05f, 1.0f, T / 2f - 0.026f), new Vector3(0.04f, 0.14f, 0.008f), mMetal, 1f, false));
        folha.Add(Caixa(j, "Espelho_Fora", new Vector3(xm + 0.05f, 1.0f, T / 2f + 0.026f), new Vector3(0.04f, 0.14f, 0.008f), mMetal, 1f, false));
        foreach (var go in folha) go.transform.SetParent(dobradica, true);

        if (portasAbrem)
        {
            // O ponto de interação fica no meio da porta (o grupo "Porta" está no centro da parede)
            var ponto = new GameObject("Interacao").transform;
            ponto.SetParent(j, false);
            ponto.localPosition = new Vector3(c, 1f, T / 2f);
            var porta = ponto.gameObject.AddComponent<PortaAbrivel>();
            porta.dobradica = dobradica;
            porta.distanciaInteracao = 1.6f;
        }
    }

    // Salas montadas com esta opção ligada ganham portas que abrem com E (escritório e casa)
    static bool portasAbrem;

    static void LuzTeto(Transform sala, string nome, Vector3 pos, float intensidade, float alcance, bool sombra)
    {
        var go = new GameObject(nome);
        go.transform.SetParent(sala, false);
        go.transform.localPosition = pos;
        var luz = go.AddComponent<Light>();
        luz.type = LightType.Point;
        luz.color = new Color(1f, 0.95f, 0.86f);
        luz.intensity = intensidade;
        luz.range = alcance;
        luz.shadows = sombra ? LightShadows.Soft : LightShadows.None;
        luz.shadowStrength = 0.7f;
        luz.renderMode = LightRenderMode.ForcePixel;
    }

    // Reflexos do ambiente na sala (deixa piso, vidro e metal mais convincentes)
    static void Sonda(Transform sala, Vector3 tamanho)
    {
        var go = new GameObject("Reflexos");
        go.transform.SetParent(sala, false);
        go.transform.localPosition = new Vector3(0, H / 2f, 0);
        var rp = go.AddComponent<ReflectionProbe>();
        rp.mode = ReflectionProbeMode.Realtime;
        rp.refreshMode = ReflectionProbeRefreshMode.OnAwake;
        rp.timeSlicingMode = ReflectionProbeTimeSlicingMode.NoTimeSlicing;
        rp.resolution = 128;
        rp.size = tamanho;
        rp.boxProjection = true;
        rp.intensity = 0.8f;
    }

    // ---------------- Luz, câmera e pós-processamento ----------------

    static void Iluminacao()
    {
        // Sol: entra pelas janelas do lado norte
        foreach (var luz in Object.FindObjectsByType<Light>())
        {
            if (luz.type != LightType.Directional) continue;
            Undo.RecordObject(luz, "Sol");
            Undo.RecordObject(luz.transform, "Sol");
            luz.transform.rotation = Quaternion.Euler(38f, 200f, 0f);
            luz.color = new Color(1f, 0.95f, 0.86f);
            luz.intensity = 1.25f;
            luz.shadows = LightShadows.Soft;
            luz.shadowStrength = 0.9f;
            luz.shadowBias = 0.03f;
            luz.shadowNormalBias = 0.3f;
        }

        RenderSettings.ambientMode = AmbientMode.Skybox;
        RenderSettings.ambientIntensity = 0.65f;
        RenderSettings.reflectionIntensity = 0.8f;
        RenderSettings.fog = false;

        QualitySettings.shadowDistance = 30f;
        QualitySettings.shadowResolution = ShadowResolution.High;
        QualitySettings.shadowCascades = 2;
        QualitySettings.pixelLightCount = 8;
        QualitySettings.anisotropicFiltering = AnisotropicFiltering.Enable;

        var jogador = GameObject.FindWithTag("Player");
        var cam = jogador != null ? jogador.GetComponentInChildren<Camera>() : Camera.main;
        if (cam == null) { Debug.LogWarning("[Cenário] Câmera do jogador não encontrada."); return; }
        Undo.RecordObject(cam, "Câmera");
        cam.allowHDR = true;
        cam.nearClipPlane = 0.05f;

        PosProcessamento(cam);
    }

    static void PosProcessamento(Camera cam)
    {
        var recursos = AssetDatabase.LoadAssetAtPath<PP.PostProcessResources>(CaminhoRecursosPP);
        if (recursos == null)
        {
            Debug.LogWarning("[Cenário] Pacote Post Processing não encontrado — pulando o pós-processamento.");
            return;
        }

        var camada = cam.GetComponent<PP.PostProcessLayer>();
        if (camada == null) camada = Undo.AddComponent<PP.PostProcessLayer>(cam.gameObject);
        camada.Init(recursos);
        camada.volumeTrigger = cam.transform;
        camada.volumeLayer = 1 << 0;   // Default
        camada.antialiasingMode = PP.PostProcessLayer.Antialiasing.FastApproximateAntialiasing;
        EditorUtility.SetDirty(camada);

        if (!AssetDatabase.IsValidFolder("Assets/Settings")) AssetDatabase.CreateFolder("Assets", "Settings");
        AssetDatabase.DeleteAsset(CaminhoPerfil);
        var perfil = ScriptableObject.CreateInstance<PP.PostProcessProfile>();
        AssetDatabase.CreateAsset(perfil, CaminhoPerfil);

        var ao = perfil.AddSettings<PP.AmbientOcclusion>();
        ao.enabled.Override(true);
        ao.mode.Override(PP.AmbientOcclusionMode.MultiScaleVolumetricObscurance);
        ao.intensity.Override(0.55f);

        var bloom = perfil.AddSettings<PP.Bloom>();
        bloom.enabled.Override(true);
        bloom.intensity.Override(1.2f);
        bloom.threshold.Override(1.1f);

        var cor = perfil.AddSettings<PP.ColorGrading>();
        cor.enabled.Override(true);
        cor.gradingMode.Override(PP.GradingMode.HighDefinitionRange);
        cor.tonemapper.Override(PP.Tonemapper.ACES);
        cor.postExposure.Override(0.45f);
        cor.temperature.Override(4f);
        cor.saturation.Override(6f);
        cor.contrast.Override(6f);

        var vinheta = perfil.AddSettings<PP.Vignette>();
        vinheta.enabled.Override(true);
        vinheta.intensity.Override(0.22f);
        vinheta.smoothness.Override(0.45f);

        foreach (var efeito in perfil.settings) AssetDatabase.AddObjectToAsset(efeito, perfil);
        EditorUtility.SetDirty(perfil);
        AssetDatabase.SaveAssets();

        var vol = new GameObject("PosProcessamento");
        vol.transform.SetParent(raiz, false);
        vol.layer = 0;
        var volume = vol.AddComponent<PP.PostProcessVolume>();
        volume.isGlobal = true;
        volume.priority = 1;
        volume.sharedProfile = perfil;
    }

    // ---------------- Materiais ----------------

    static void CriarMateriais()
    {
        mPisoMadeira = MatPBR("Piso_Madeira", "WoodFloor051", Color.white, 0.45f);
        mCarpete = MatPBR("Piso_Carpete", "Carpet016", new Color(0.92f, 0.92f, 0.95f), 0.05f);
        mParedeEscritorio = MatPBR("Parede_Escritorio", "PaintedPlaster017", new Color(0.95f, 0.92f, 0.86f), 0.12f);
        mParedeCliente = MatPBR("Parede_Cliente", "PaintedPlaster017", new Color(0.88f, 0.91f, 0.94f), 0.12f);
        mForro = MatPBR("Forro", "OfficeCeiling005", Color.white, 0.2f);
        var emissao = Textura("OfficeCeiling005", "Emission", false);
        if (emissao != null) { mForro.SetTexture("_EmissionMap", emissao); Brilhar(mForro, Color.white * 1.6f); }
        mMadeira = MatPBR("Madeira", "Wood049", Color.white, 0.4f);

        mRodape = Mat("Rodape", new Color(0.93f, 0.93f, 0.91f), 0.45f);
        mAluminio = Mat("Aluminio", new Color(0.25f, 0.26f, 0.28f), 0.65f, 0.7f);
        mMetal = Mat("Metal_Escovado", new Color(0.75f, 0.75f, 0.76f), 0.7f, 1f);
        mGrama = Mat("Grama", new Color(0.29f, 0.4f, 0.2f), 0.05f);
        mVidro = MatVidro();
        AssetDatabase.SaveAssets();
    }

    static Texture2D Textura(string id, string mapa, bool normal)
    {
        string caminho = $"{PastaTexturas}{id}/{id}_1K-JPG_{mapa}.jpg";
        var imp = AssetImporter.GetAtPath(caminho) as TextureImporter;
        if (imp == null) return null;

        bool mudou = false;
        bool corReal = mapa == "Color" || mapa == "Emission";
        if (normal && imp.textureType != TextureImporterType.NormalMap) { imp.textureType = TextureImporterType.NormalMap; mudou = true; }
        if (!normal && imp.sRGBTexture != corReal) { imp.sRGBTexture = corReal; mudou = true; }
        if (imp.anisoLevel < 4) { imp.anisoLevel = 4; mudou = true; }
        if (mudou) imp.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(caminho);
    }

    static Material MatPBR(string nome, string id, Color tom, float brilho)
    {
        var m = CarregarOuCriar(nome);
        m.color = tom;
        m.SetFloat("_Glossiness", brilho);
        m.SetFloat("_Metallic", 0f);
        m.mainTextureScale = Vector2.one;

        var cor = Textura(id, "Color", false);
        if (cor != null) m.mainTexture = cor;
        else Debug.LogWarning($"[Cenário] Textura {id} não encontrada — usando só a cor.");

        var normal = Textura(id, "NormalGL", true);
        if (normal != null)
        {
            m.SetTexture("_BumpMap", normal);
            m.SetFloat("_BumpScale", 1f);
            m.EnableKeyword("_NORMALMAP");
        }

        var ao = Textura(id, "AmbientOcclusion", false);
        if (ao != null) { m.SetTexture("_OcclusionMap", ao); m.SetFloat("_OcclusionStrength", 1f); }

        EditorUtility.SetDirty(m);
        return m;
    }

    static void Brilhar(Material m, Color cor)
    {
        m.EnableKeyword("_EMISSION");
        m.SetColor("_EmissionColor", cor);
        m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
        EditorUtility.SetDirty(m);
    }

    static Material MatVidro()
    {
        var m = CarregarOuCriar("Vidro");
        m.SetFloat("_Mode", 3);   // Transparent
        m.SetOverrideTag("RenderType", "Transparent");
        m.SetInt("_SrcBlend", (int)BlendMode.One);
        m.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
        m.SetInt("_ZWrite", 0);
        m.DisableKeyword("_ALPHATEST_ON");
        m.DisableKeyword("_ALPHABLEND_ON");
        m.EnableKeyword("_ALPHAPREMULTIPLY_ON");
        m.renderQueue = (int)RenderQueue.Transparent;
        m.color = new Color(0.75f, 0.85f, 0.9f, 0.12f);
        m.SetFloat("_Glossiness", 0.95f);
        m.SetFloat("_Metallic", 0f);
        EditorUtility.SetDirty(m);
        return m;
    }

    static Material CarregarOuCriar(string nome)
    {
        if (!AssetDatabase.IsValidFolder("Assets/Materials")) AssetDatabase.CreateFolder("Assets", "Materials");
        if (!AssetDatabase.IsValidFolder(PastaMateriais)) AssetDatabase.CreateFolder("Assets/Materials", "Cenario");
        string caminho = PastaMateriais + "/" + nome + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(caminho);
        if (m == null)
        {
            m = new Material(Shader.Find("Standard"));
            AssetDatabase.CreateAsset(m, caminho);
        }
        return m;
    }

    static Material Mat(string nome, Color cor, float brilho, float metal = 0f)
    {
        var m = CarregarOuCriar(nome);
        m.color = cor;
        m.SetFloat("_Glossiness", brilho);
        m.SetFloat("_Metallic", metal);
        EditorUtility.SetDirty(m);
        return m;
    }

    // ---------------- Peças ----------------

    // Caixa com UVs em metros (a textura não estica: 'metros' é o tamanho de uma repetição).
    // As UVs usam a posição dentro do pai, então blocos vizinhos da mesma parede ficam alinhados.
    static GameObject Caixa(Transform pai, string nome, Vector3 centro, Vector3 tam, Material mat, float metros,
        bool colisor = true, bool sombra = true)
    {
        var go = new GameObject(nome);
        go.transform.SetParent(pai, false);
        go.transform.localPosition = centro;

        var verts = new List<Vector3>();
        var normais = new List<Vector3>();
        var uvs = new List<Vector2>();
        var tris = new List<int>();
        var faces = new[]
        {
            (n: Vector3.right,   v: Vector3.up),
            (n: Vector3.left,    v: Vector3.up),
            (n: Vector3.forward, v: Vector3.up),
            (n: Vector3.back,    v: Vector3.up),
            (n: Vector3.up,      v: Vector3.forward),
            (n: Vector3.down,    v: Vector3.forward),
        };
        Vector3 metade = tam / 2f;
        float Comp(Vector3 eixo) => Mathf.Abs(Vector3.Dot(eixo, metade));

        foreach (var (n, v) in faces)
        {
            Vector3 u = Vector3.Cross(n, v);
            Vector3 cn = n * Comp(n), cu = u * Comp(u), cv = v * Comp(v);
            int i0 = verts.Count;
            foreach (var p in new[] { cn - cu - cv, cn - cu + cv, cn + cu + cv, cn + cu - cv })
            {
                verts.Add(p);
                normais.Add(n);
                Vector3 mundo = centro + p;
                uvs.Add(new Vector2(Vector3.Dot(mundo, u), Vector3.Dot(mundo, v)) / metros);
            }
            tris.AddRange(new[] { i0, i0 + 1, i0 + 2, i0, i0 + 2, i0 + 3 });
        }

        var malha = new Mesh { name = nome };
        malha.SetVertices(verts);
        malha.SetNormals(normais);
        malha.SetUVs(0, uvs);
        malha.SetTriangles(tris, 0);
        malha.RecalculateTangents();
        malha.RecalculateBounds();

        go.AddComponent<MeshFilter>().sharedMesh = malha;
        var mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = mat;
        mr.shadowCastingMode = sombra ? ShadowCastingMode.On : ShadowCastingMode.Off;
        if (colisor) go.AddComponent<BoxCollider>().size = tam;
        return go;
    }

    // Coloca um modelo do Kenney com a base apoiada em 'pos' (coordenadas locais do pai) e centralizado
    static GameObject Modelo(string nome, Transform pai, Vector3 pos, float rotY = 0, Colisao colisao = Colisao.Caixa)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PastaModelos + nome + ".fbx");
        if (prefab == null) { Debug.LogError("[Cenário] Modelo não encontrado: " + nome); return null; }

        var suporte = new GameObject(nome).transform;  // começa na origem para medir o modelo
        var modelo = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        modelo.transform.SetParent(suporte, false);
        modelo.transform.localScale *= Escala * correcaoFbx;

        foreach (var r in modelo.GetComponentsInChildren<Renderer>())
        {
            var mats = r.sharedMaterials;
            bool trocou = false;
            for (int i = 0; i < mats.Length; i++)
            {
                if (mats[i] == null) continue;
                materiaisKenney.Add(mats[i].name);
                if (MadeiraNosMoveis && mats[i].name.ToLowerInvariant().Contains("wood"))
                {
                    mats[i] = mMadeira;
                    trocou = true;
                }
            }
            if (trocou) r.sharedMaterials = mats;
        }

        var b = Limites(modelo);
        modelo.transform.localPosition -= new Vector3(b.center.x, b.min.y, b.center.z);

        if (colisao == Colisao.Caixa)
        {
            var bc = suporte.gameObject.AddComponent<BoxCollider>();
            bc.center = new Vector3(0, b.size.y / 2f, 0);
            bc.size = b.size;
        }
        else if (colisao == Colisao.Malha)
        {
            foreach (var mf in modelo.GetComponentsInChildren<MeshFilter>())
                mf.gameObject.AddComponent<MeshCollider>().sharedMesh = mf.sharedMesh;
        }

        suporte.SetParent(pai, false);
        suporte.localPosition = pos;
        suporte.localRotation = Quaternion.Euler(0, rotY, 0);
        return suporte.gameObject;
    }

    // Move um objeto que já existe na cena (com Equipamento/Notebook) e troca o cubo pelo visual novo
    static void PosicionarEquipamento(string nome, Transform pai, Vector3 posLocal, float rotY, Action<Transform> montarVisual)
    {
        var go = GameObject.Find(nome);
        if (go == null) { Debug.LogWarning("[Cenário] Objeto não encontrado na cena: " + nome); return; }
        Undo.RecordObject(go.transform, "Posicionar " + nome);

        var t = go.transform;
        t.position = Vector3.zero;
        t.rotation = Quaternion.identity;
        t.localScale = Vector3.one;

        var mr = go.GetComponent<MeshRenderer>();
        if (mr != null) { Undo.RecordObject(mr, "Esconder cubo"); mr.enabled = false; }

        var antigo = t.Find("Visual");
        if (antigo != null) Object.DestroyImmediate(antigo.gameObject);
        var visual = new GameObject("Visual").transform;
        visual.SetParent(t, false);
        montarVisual(visual);

        var b = Limites(visual.gameObject);
        var bc = go.GetComponent<BoxCollider>();
        if (bc == null) bc = Undo.AddComponent<BoxCollider>(go);
        Undo.RecordObject(bc, "Ajustar colisor");
        bc.center = b.center;
        bc.size = b.size;

        t.position = pai.TransformPoint(posLocal);
        t.rotation = Quaternion.Euler(0, pai.eulerAngles.y + rotY, 0);
    }

    static Transform Ponto(string nome, Transform pai, Vector3 posLocal, float rotY)
    {
        var p = new GameObject(nome).transform;
        p.SetParent(pai, false);
        p.localPosition = posLocal;
        p.localRotation = Quaternion.Euler(0, rotY, 0);
        return p;
    }

    static void AjustarJogador(Transform spawn)
    {
        var jogador = GameObject.FindWithTag("Player");
        Undo.RecordObject(jogador.transform, "Mover jogador");
        jogador.transform.position = spawn.position;
        jogador.transform.rotation = spawn.rotation;

        var cc = jogador.GetComponent<CharacterController>();
        if (cc != null)
        {
            Undo.RecordObject(cc, "Ajustar jogador");
            cc.radius = 0.35f; // cabe entre os móveis
            cc.height = 1.8f;  // passa nas portas (2,10 m) mesmo vindo da calçada, 12 cm mais alta
            cc.center = Vector3.zero;
        }
    }

    static GameObject Cubo(Transform pai, string nome, Vector3 posLocal, Vector3 tamanho, Material mat)
    {
        var c = GameObject.CreatePrimitive(PrimitiveType.Cube);
        c.name = nome;
        Object.DestroyImmediate(c.GetComponent<Collider>());
        c.transform.SetParent(pai, false);
        c.transform.localPosition = posLocal;
        c.transform.localScale = tamanho;
        c.GetComponent<Renderer>().sharedMaterial = mat;
        return c;
    }

    // Os FBX podem vir em centímetros ou metros: mede a parede (1 unidade de largura) e corrige
    static void CalibrarEscalaFbx()
    {
        correcaoFbx = 1f;
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PastaModelos + "wall.fbx");
        var tmp = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        var b = Limites(tmp);
        float largura = Mathf.Max(b.size.x, b.size.z);
        Object.DestroyImmediate(tmp);
        if (largura > 0.0001f) correcaoFbx = 1f / largura;
        Debug.Log($"[Cenário] Largura da parede no FBX: {largura:F3} → correção {correcaoFbx:F3}");
    }

    static Bounds Limites(GameObject go)
    {
        var rs = go.GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.zero);
        var b = rs[0].bounds;
        for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
        return b;
    }

    static float Altura(GameObject go) => go == null ? 0f : Limites(go).max.y - go.transform.position.y;
}
