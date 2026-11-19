using UnityEngine;

// Etapa 10E-3: sete clientes novos com interior espalhados pela cidade (Bairro.Predios). Todas as salas têm 10 x 8 m
// (o access point e o rack supõem isso) e a porta no meio da parede sul local; as do lado sul da rua giram 180° no fim
// (com pontos e caminhos), como a loja. Cada uma parte de um dos três tipos (escritório, clínica, loja), com outro
// layout, cor, piso, luminárias, móveis e decoração. Os equipamentos do chamado vão para os pontos "Ancora_*" e o
// contato percorre os "Caminho_*" (DadosSala). Paredes do roteador sem janela: o rack e os pontos de rede vão nela.
// Giros (ver MontarCenario.Salas.cs): mesa de metal com a frente (lado de quem senta) em +z; cadeira de escritório
// com a frente em -z; computador do chamado com a tela em -z (180 = tela em +z); MonitorLigado 0 = tela em +z;
// impressora e roteador com a frente em -z; aparador, mesinha, estante, sofá, relógio e cadeira de jantar com a frente
// em +z; quadro com a frente em -z.
public static partial class MontarCenario
{
    static void MontarClientesNovos(Transform spawn)
    {
        CriarMateriaisBairro();   // a placa interna da farmácia usa o fundo das placas (criado de novo no MontarBairro)
        MontarAdvocacia(spawn);
        MontarImobiliaria(spawn);
        MontarEscola(spawn);
        MontarLaboratorio(spawn);
        MontarVeterinaria(spawn);
        MontarFarmacia(spawn);
        MontarOtica(spawn);
    }

    // ---------------- Peças comuns ----------------

    static Transform SalaDoPredio(TipoEndereco t, Material piso, float metrosPiso, Material parede,
        Abertura[] norte, Abertura[] sul, Abertura[] leste, Abertura[] oeste, bool gesso = true)
    {
        var p = Bairro.Predio(t);
        var sala = Sala(p.objeto, p.centro, PredioCliente.Largura, PredioCliente.Profundidade, piso, metrosPiso, parede,
            norte: norte, sul: sul, leste: leste, oeste: oeste);
        if (gesso)
        {
            var forro = sala.Find("Forro");
            if (forro != null)
                forro.GetComponent<MeshRenderer>().sharedMaterial = MatPBR("Teto_Gesso", "PaintedPlaster017", new Color(0.97f, 0.97f, 0.96f), 0.08f);
        }
        return sala;
    }

    // Prédio do lado sul da rua: gira a sala inteira (com pontos e caminhos) para a porta dar na calçada
    static void Girar(Transform sala, TipoEndereco t) => sala.rotation = Quaternion.Euler(0f, Bairro.Predio(t).giro, 0f);

    // Três lâmpadas (a primeira com sombra: no máximo uma por cômodo) e a sonda de reflexos
    static void TresLuzes(Transform sala, Vector2 a, Vector2 b, Vector2 c, float forca = 1.25f)
    {
        LuzTeto(sala, "Luz_Teto_1", new Vector3(a.x, H - 0.15f, a.y), forca + 0.05f, 6.5f, true);
        LuzTeto(sala, "Luz_Teto_2", new Vector3(b.x, H - 0.15f, b.y), forca, 6.5f, false);
        LuzTeto(sala, "Luz_Teto_3", new Vector3(c.x, H - 0.15f, c.y), forca - 0.1f, 6.5f, false);
        Sonda(sala, new Vector3(PredioCliente.Largura, H, PredioCliente.Profundidade));
    }

    // Calhas com lâmpada tubular acesa (loja, escola, farmácia)
    static void Calhas(Transform sala, params Vector2[] pos)
    {
        var mCalha = Mat("Calha_Luminaria", new Color(0.92f, 0.92f, 0.9f), 0.5f);
        var mTubo = Mat("Tubo_Aceso", new Color(1f, 0.98f, 0.94f), 0.5f);
        Brilhar(mTubo, new Color(1f, 0.97f, 0.9f) * 1.8f);
        foreach (var p in pos)
        {
            Caixa(sala, "Calha", new Vector3(p.x, H - 0.03f, p.y), new Vector3(1.25f, 0.05f, 0.22f), mCalha, 1f, false, false);
            Caixa(sala, "Tubo", new Vector3(p.x, H - 0.065f, p.y), new Vector3(1.18f, 0.025f, 0.05f), mTubo, 1f, false, false);
        }
    }

    // Painéis de LED de embutir 62 x 62 (imobiliária, laboratório)
    static void PaineisLed(Transform sala, params Vector2[] pos)
    {
        var mAro = Mat("Painel_Aro", new Color(0.9f, 0.9f, 0.9f), 0.4f);
        var mLuz = Mat("Painel_Aceso", new Color(1f, 1f, 0.98f), 0.3f);
        Brilhar(mLuz, new Color(1f, 0.98f, 0.94f) * 1.6f);
        foreach (var p in pos)
        {
            Caixa(sala, "Painel_LED", new Vector3(p.x, H - 0.012f, p.y), new Vector3(0.64f, 0.024f, 0.64f), mAro, 1f, false, false);
            Caixa(sala, "Painel_Luz", new Vector3(p.x, H - 0.026f, p.y), new Vector3(0.58f, 0.006f, 0.58f), mLuz, 1f, false, false);
        }
    }

    static void Pendentes(Transform sala, params Vector2[] pos)
    {
        foreach (var p in pos)
            ModeloReal(PH("modern_ceiling_lamp_01"), sala, new Vector3(p.x, H - 0.35f, p.y), 0.45f, 0, Colisao.Nenhuma);
    }

    // Balcão alto de recepção (MDF com tampo de madeira), de x0 a x1 na linha z (frente para -z)
    static void BalcaoAlto(Transform sala, float x0, float x1, float z, Material corpo, Material tampo, bool lateralOeste)
    {
        float cx = (x0 + x1) / 2f, w = x1 - x0;
        Caixa(sala, "Balcao", new Vector3(cx, 0.55f, z), new Vector3(w, 1.1f, 0.12f), corpo, 1f);
        Caixa(sala, "Balcao_Tampo", new Vector3(cx, 1.12f, z + 0.07f), new Vector3(w + 0.1f, 0.04f, 0.36f), tampo, 1.2f);
        Caixa(sala, "Balcao_Faixa", new Vector3(cx, 0.95f, z - 0.065f), new Vector3(w, 0.12f, 0.012f), tampo, 1.2f, false);
        Caixa(sala, "Balcao_Rodape", new Vector3(cx, 0.04f, z - 0.07f), new Vector3(w, 0.08f, 0.012f), Mat("Rodape_Escuro", new Color(0.18f, 0.18f, 0.19f), 0.3f), 1f, false);
        float xl = lateralOeste ? x0 : x1;
        Caixa(sala, "Balcao_Lateral", new Vector3(xl, 0.55f, z + 0.35f), new Vector3(0.12f, 1.1f, 0.6f), corpo, 1f);
    }

    // Mesa de metal com cadeira, monitor ligado e (opcional) alguém digitando. 'norte' = quem senta fica ao norte.
    static float Estacao(Transform sala, float x, float z, bool norte, bool monitor, int pessoa)
    {
        var mesa = ModeloReal(PH("metal_office_desk"), sala, new Vector3(x, 0, z), LadoMesaMetal, norte ? 0f : 180f);
        float h = Altura(mesa);
        float zc = norte ? z + 0.87f : z - 0.87f;
        ModeloReal(SF("Cadeira_Escritorio"), sala, new Vector3(x, 0, zc), 1.15f, norte ? 0f : 180f);
        if (monitor) MonitorLigado(sala, new Vector3(x + 0.1f, h, z - (norte ? 0.05f : -0.05f)), norte ? 0f : 180f);
        if (pessoa >= 0) Pessoa(sala, new Vector3(x, 0, zc + (norte ? 0.02f : -0.02f)), norte ? 180f : 0f, PessoaTrabalhando.Atividade.Digitando, pessoa);
        return h;
    }

    static GameObject Aparador(Transform sala, Vector3 pos, float rotY) => ModeloReal(PH("modern_wooden_cabinet"), sala, pos, 2.4f, rotY);
    static GameObject Mesinha(Transform sala, Vector3 pos, float rotY) => ModeloReal(PH("side_table_01"), sala, pos, 0.55f, rotY);

    // Fotos coloridas presas num painel (quadro de imóveis, mural da escola): 'fundo' de w x h na parede norte
    static void Mural(Transform sala, string nome, Vector3 centro, float w, float h, Material fundo, int colunas, int linhas, int semente)
    {
        var m = new GameObject(nome).transform;
        m.SetParent(sala, false);
        m.localPosition = centro;
        Caixa(m, "Fundo", Vector3.zero, new Vector3(w, h, 0.025f), fundo, 1f, false, false);
        Material[] cores =
        {
            Mat("Foto_Ceu", new Color(0.55f, 0.72f, 0.88f), 0.3f), Mat("Foto_Grama", new Color(0.45f, 0.65f, 0.35f), 0.3f),
            Mat("Foto_Telhado", new Color(0.7f, 0.38f, 0.28f), 0.3f), Mat("Foto_Parede", new Color(0.93f, 0.86f, 0.7f), 0.3f),
            Mat("Produto_Branco", new Color(0.92f, 0.92f, 0.9f), 0.35f),
        };
        var rnd = new System.Random(semente);
        float cw = w / colunas, ch = h / linhas;
        for (int i = 0; i < colunas; i++)
            for (int j = 0; j < linhas; j++)
            {
                var pos = new Vector3(-w / 2f + cw * (i + 0.5f), -h / 2f + ch * (j + 0.5f), -0.018f);
                Caixa(m, "Foto", pos, new Vector3(cw * 0.78f, ch * 0.72f, 0.008f), cores[rnd.Next(cores.Length)], 1f, false, false);
            }
    }

    // ---------------- Escritório de advocacia (tipo escritório; Av. Paineiras, lado norte) ----------------

    static void MontarAdvocacia(Transform spawn)
    {
        var mParede = MatPBR("Parede_Advocacia", "PaintedPlaster017", new Color(0.93f, 0.88f, 0.8f), 0.12f);
        var sala = SalaDoPredio(TipoEndereco.Advocacia, mPisoMadeira, 1.5f, mParede,
            norte: new[] { Janela(0.5f, 1.4f), Janela(3.4f, 1.2f) },
            sul: new[] { Janela(-3f, 1.4f), Porta(0f) },
            leste: null,
            oeste: new[] { Janela(-2.4f) });
        TresLuzes(sala, new Vector2(0.5f, 2.0f), new Vector2(-2.4f, -1.0f), new Vector2(2.6f, -1.4f));
        Pendentes(sala, new Vector2(0.5f, 2.0f), new Vector2(-2.4f, -1.0f), new Vector2(2.6f, -1.4f));

        // Mesa do advogado ao fundo, de frente para a porta; o computador do chamado fica nela
        var mesa = ModeloReal(PH("metal_office_desk"), sala, new Vector3(0.5f, 0, 2.6f), LadoMesaMetal, 0);
        float h = Altura(mesa);
        ModeloReal(SF("Cadeira_Escritorio"), sala, new Vector3(0.5f, 0, 3.45f), 1.15f, 0);
        var aPc = Ponto("Ancora_Computador", sala, new Vector3(0.5f, h, 2.55f), 180);
        ModeloReal(PH("desk_lamp_arm_01"), sala, new Vector3(1.25f, h, 2.75f), 0.5f, 160, Colisao.Nenhuma);
        ModeloReal(PH("office_notepads"), sala, new Vector3(-0.15f, h, 2.45f), 0.3f, 10, Colisao.Nenhuma);
        ModeloReal(SF("Telefone_Escritorio"), sala, new Vector3(-0.3f, h, 2.8f), 0.22f, 200, Colisao.Nenhuma);
        foreach (float x in new[] { 0f, 1.0f }) ModeloReal(PH("dining_chair_02"), sala, new Vector3(x, 0, 1.65f), 0.95f, 0);

        // Estantes com processos (caixas-arquivo) na parede oeste e mesa de reunião redonda
        CaixasNaEstante(sala, ModeloReal(PH("steel_frame_shelves_03"), sala, new Vector3(-4.68f, 0, 0.4f), 1.8f, 90), true);
        CaixasNaEstante(sala, ModeloReal(PH("steel_frame_shelves_03"), sala, new Vector3(-4.68f, 0, 2.3f), 1.8f, 90), true);
        var reuniao = ModeloReal(PH("round_wooden_table_01"), sala, new Vector3(-2.3f, 0, -1.0f), 1.1f, 0);
        ModeloReal(PH("office_notepads"), sala, new Vector3(-2.2f, Altura(reuniao), -1.05f), 0.3f, 40, Colisao.Nenhuma);
        ModeloReal(PH("dining_chair_02"), sala, new Vector3(-2.3f, 0, -0.25f), 0.95f, 180);
        ModeloReal(PH("dining_chair_02"), sala, new Vector3(-3.05f, 0, -1.0f), 0.95f, 90);
        ModeloReal(PH("dining_chair_02"), sala, new Vector3(-1.55f, 0, -1.0f), 0.95f, -90);
        Pessoa(sala, new Vector3(-3.05f, 0, -1.0f), 90, PessoaTrabalhando.Atividade.ConversandoSentado, 2);   // cliente esperando

        // Secretária perto da porta, virada para a frente
        float hs = Estacao(sala, 2.4f, -1.6f, true, true, 0);
        ModeloReal(SF("Telefone_Escritorio"), sala, new Vector3(1.75f, hs, -1.7f), 0.22f, 20, Colisao.Nenhuma);
        Lixeira(sala, new Vector3(3.6f, 0, -1.0f));

        // Impressora num aparador no canto noroeste; roteador numa mesinha na parede leste (sem janela: rack)
        var armario = Aparador(sala, new Vector3(-3.2f, 0, 3.62f), 180);
        var aImp = Ponto("Ancora_Impressora", sala, new Vector3(-3.4f, Altura(armario), 3.6f), 0);
        ModeloReal(PH("cardboard_box_01"), sala, new Vector3(-2.35f, Altura(armario), 3.65f), 0.32f, 8, Colisao.Nenhuma);
        var mesinha = Mesinha(sala, new Vector3(4.7f, 0, -1.5f), -90);
        var aRot = Ponto("Ancora_Roteador", sala, new Vector3(4.68f, Altura(mesinha), -1.5f), 90);

        ModeloReal(PH("potted_plant_02"), sala, new Vector3(4.5f, 0, 3.5f), 0.8f, 0);
        ModeloReal(PH("potted_plant_01"), sala, new Vector3(-4.45f, 0, -3.5f), 0.9f, 0);
        ModeloReal(PH("wall_clock"), sala, new Vector3(2.5f, 2.2f, -3.97f), 0.32f, 0, Colisao.Nenhuma);
        ModeloReal(PH("hanging_picture_frame_01"), sala, new Vector3(-1.7f, 1.6f, 3.97f), 0.7f, 0, Colisao.Nenhuma);
        ModeloReal(PH("hanging_picture_frame_01"), sala, new Vector3(4.97f, 1.6f, 2.6f), 0.7f, 90, Colisao.Nenhuma);

        DadosSala(sala, TipoSala.Contabilidade, spawn, new Vector2(1.2f, -2.6f),
            aImp, Caminho(sala, "Impressora", new Vector2(-0.7f, 0.8f), new Vector2(-2.4f, 2.75f)),
            aRot, Caminho(sala, "Roteador", new Vector2(3.95f, -2.6f), new Vector2(3.95f, -1.0f)),
            aPc, Caminho(sala, "Computador", new Vector2(0.2f, -0.4f), new Vector2(-0.75f, 1.2f), new Vector2(-0.75f, 3.0f), new Vector2(-0.35f, 3.35f)));
        Girar(sala, TipoEndereco.Advocacia);
    }

    // ---------------- Imobiliária (tipo escritório; Av. Paineiras, lado sul, girada) ----------------

    static void MontarImobiliaria(Transform spawn)
    {
        var mParede = MatPBR("Parede_Imobiliaria", "PaintedPlaster017", new Color(0.86f, 0.9f, 0.93f), 0.12f);
        var mPiso = MatPBR("Piso_Imobiliaria", "Tiles107", new Color(0.86f, 0.86f, 0.88f), 0.55f);
        var vitrine = new Abertura { largura = 2.4f, baixo = 0.5f, alto = 2.3f };
        var sala = SalaDoPredio(TipoEndereco.Imobiliaria, mPiso, 2f, mParede,
            norte: new[] { Janela(-2.6f, 1.4f) },
            sul: new[] { Mover(vitrine, -2.9f), Porta(0f), Mover(vitrine, 2.9f) },
            leste: new[] { Janela(1.6f) },
            oeste: null);
        TresLuzes(sala, new Vector2(-2.6f, 1.0f), new Vector2(2.6f, 1.0f), new Vector2(0f, -2.2f));
        PaineisLed(sala, new Vector2(-2.6f, 1.0f), new Vector2(2.6f, 1.0f), new Vector2(0f, -2.2f), new Vector2(0f, 1.0f), new Vector2(-2.6f, -2.2f), new Vector2(2.6f, -2.2f));

        // Três corretores numa fila de mesas, de frente para a vitrine; o computador do chamado é o do meio
        Estacao(sala, -2.6f, 1.4f, true, true, 0);
        float h = Estacao(sala, 0f, 1.4f, true, false, -1);
        var aPc = Ponto("Ancora_Computador", sala, new Vector3(0f, h, 1.35f), 180);
        ModeloReal(PH("office_notepads"), sala, new Vector3(0.65f, h, 1.25f), 0.3f, -12, Colisao.Nenhuma);
        Estacao(sala, 2.6f, 1.4f, true, true, 1);
        ModeloReal(SF("Telefone_Escritorio"), sala, new Vector3(3.3f, h, 1.3f), 0.22f, 160, Colisao.Nenhuma);
        foreach (float x in new[] { -2.6f, 0f, 2.6f }) ModeloReal(PH("dining_chair_02"), sala, new Vector3(x, 0, 0.55f), 0.95f, 0);
        Pessoa(sala, new Vector3(-2.6f, 0, 0.55f), 0, PessoaTrabalhando.Atividade.ConversandoSentado, 2);   // cliente com o corretor

        // Quadro de imóveis à venda na parede do fundo
        Mural(sala, "Quadro_Imoveis", new Vector3(1.6f, 1.55f, 3.985f), 3.0f, 1.2f, Mat("Cortica", new Color(0.72f, 0.58f, 0.4f), 0.2f), 5, 3, 11);
        ModeloReal(PH("wall_clock"), sala, new Vector3(4.97f, 2.25f, -2.8f), 0.32f, -90, Colisao.Nenhuma);

        // Espera junto à vitrine: sofá e mesinha de centro com revistas
        ModeloReal(PH("sofa_02"), sala, new Vector3(-2.9f, 0, -3.5f), 1.81f, RotSofa);
        var centro = ModeloReal(PH("modern_coffee_table_01"), sala, new Vector3(-2.9f, 0, -2.5f), 1.0f, 90);
        ModeloReal(PH("office_notepads"), sala, new Vector3(-2.9f, Altura(centro), -2.5f), 0.3f, 60, Colisao.Nenhuma);
        ModeloReal(PH("potted_plant_02"), sala, new Vector3(4.5f, 0, -3.5f), 0.8f, 0);
        ModeloReal(PH("potted_plant_01"), sala, new Vector3(4.45f, 0, 3.5f), 0.9f, 0);

        // Impressora na parede leste; roteador na oeste (sem janela: rack)
        var armario = Aparador(sala, new Vector3(4.64f, 0, -1.6f), -90);
        var aImp = Ponto("Ancora_Impressora", sala, new Vector3(4.6f, Altura(armario), -1.6f), 90);
        var mesinha = Mesinha(sala, new Vector3(-4.7f, 0, 1.9f), 90);
        var aRot = Ponto("Ancora_Roteador", sala, new Vector3(-4.68f, Altura(mesinha), 1.9f), -90);
        Lixeira(sala, new Vector3(-1.2f, 0, 2.0f));

        DadosSala(sala, TipoSala.Contabilidade, spawn, new Vector2(1.4f, -1.6f),
            aImp, Caminho(sala, "Impressora", new Vector2(3.85f, -1.6f)),
            aRot, Caminho(sala, "Roteador", new Vector2(-1.2f, -0.4f), new Vector2(-3.95f, -0.3f), new Vector2(-3.95f, 1.35f)),
            aPc, Caminho(sala, "Computador", new Vector2(1.3f, 0f), new Vector2(1.3f, 2.6f), new Vector2(0.6f, 2.6f)));
        Girar(sala, TipoEndereco.Imobiliaria);
    }

    // ---------------- Escola de cursos (tipo escritório; Rua dos Flamboyants, lado norte) ----------------

    static void MontarEscola(Transform spawn)
    {
        var mParede = MatPBR("Parede_Escola", "PaintedPlaster017", new Color(0.97f, 0.95f, 0.86f), 0.12f);
        var mPiso = MatPBR("Piso_Escola", "Tiles107", new Color(0.78f, 0.84f, 0.8f), 0.45f);
        var sala = SalaDoPredio(TipoEndereco.Escola, mPiso, 2f, mParede,
            norte: null,
            sul: new[] { Janela(-3f, 1.4f), Porta(0f) },
            leste: new[] { Janela(-1.6f, 1.4f), Janela(1.8f, 1.4f) },
            oeste: null, gesso: false);
        TresLuzes(sala, new Vector2(-1.6f, 0.6f), new Vector2(1.8f, 0.6f), new Vector2(0f, 2.9f));
        Calhas(sala, new Vector2(-1.6f, 0.6f), new Vector2(1.8f, 0.6f), new Vector2(0f, 2.9f), new Vector2(-1.6f, -2.2f), new Vector2(1.8f, -2.2f));

        // Lousa branca na parede norte, com moldura e aparador de pincel
        var mBranco = Mat("Lousa_Branca", new Color(0.96f, 0.97f, 0.97f), 0.75f);
        Caixa(sala, "Lousa", new Vector3(-0.6f, 1.45f, 3.982f), new Vector3(3.2f, 1.2f, 0.03f), mBranco, 1f, false, false);
        Caixa(sala, "Lousa_Moldura_C", new Vector3(-0.6f, 2.065f, 3.975f), new Vector3(3.26f, 0.03f, 0.045f), mAluminio, 1f, false, false);
        Caixa(sala, "Lousa_Moldura_B", new Vector3(-0.6f, 0.835f, 3.96f), new Vector3(3.26f, 0.03f, 0.08f), mAluminio, 1f, false, false);
        Caixa(sala, "Lousa_Moldura_E", new Vector3(-2.215f, 1.45f, 3.975f), new Vector3(0.03f, 1.26f, 0.045f), mAluminio, 1f, false, false);
        Caixa(sala, "Lousa_Moldura_D", new Vector3(1.015f, 1.45f, 3.975f), new Vector3(0.03f, 1.26f, 0.045f), mAluminio, 1f, false, false);
        var mTinta = Mat("Pincel_Azul", new Color(0.15f, 0.25f, 0.6f), 0.4f);
        for (int i = 0; i < 4; i++)   // "matéria" escrita na lousa (riscos)
            Caixa(sala, "Escrita", new Vector3(-1.6f + i * 0.12f, 1.85f - i * 0.18f, 3.965f), new Vector3(1.2f - i * 0.2f, 0.025f, 0.003f), mTinta, 1f, false, false);
        Caixa(sala, "Pincel", new Vector3(0.2f, 0.86f, 3.94f), new Vector3(0.13f, 0.02f, 0.02f), mTinta, 1f, false, false);
        ModeloReal(PH("wall_clock"), sala, new Vector3(-0.6f, 2.45f, 3.97f), 0.3f, RotRelogio, Colisao.Nenhuma);
        CaixasNaEstante(sala, ModeloReal(PH("steel_frame_shelves_03"), sala, new Vector3(-3.5f, 0, 3.68f), 1.8f, 180), false);   // livros

        // Laboratório de informática: duas fileiras de bancadas viradas para a lousa, dois micros em cada
        int pessoa = 0;
        foreach (float z in new[] { 1.4f, -0.6f })
            foreach (float x in new[] { -2.2f, 1.0f })
            {
                var mesa = ModeloReal(PH("metal_office_desk"), sala, new Vector3(x, 0, z), LadoMesaMetal, 180);
                float hm = Altura(mesa);
                foreach (float dx in new[] { -0.45f, 0.45f })
                {
                    MonitorLigado(sala, new Vector3(x + dx, hm, z + 0.05f), 180);
                    ModeloReal(SF("Cadeira_Escritorio"), sala, new Vector3(x + dx, 0, z - 0.85f), 1.0f, 180);
                    if ((dx < 0f) == (z > 0f) && pessoa < 3)
                        Pessoa(sala, new Vector3(x + dx, 0, z - 0.87f), 0, PessoaTrabalhando.Atividade.Digitando, pessoa++);
                }
            }

        // Mesa do professor no canto nordeste, de frente para a turma: o computador do chamado
        var mesaProf = ModeloReal(PH("metal_office_desk"), sala, new Vector3(3.0f, 0, 2.9f), LadoMesaMetal, 0);
        float h = Altura(mesaProf);
        ModeloReal(SF("Cadeira_Escritorio"), sala, new Vector3(3.0f, 0, 3.55f), 1.15f, 0);
        var aPc = Ponto("Ancora_Computador", sala, new Vector3(3.0f, h, 2.85f), 180);
        ModeloReal(PH("office_notepads"), sala, new Vector3(2.3f, h, 2.75f), 0.3f, 15, Colisao.Nenhuma);

        // Impressora num aparador na parede sul; roteador na parede oeste (sem janela: rack)
        var armario = Aparador(sala, new Vector3(3.4f, 0, -3.62f), 0);
        var aImp = Ponto("Ancora_Impressora", sala, new Vector3(3.4f, Altura(armario), -3.6f), 180);
        ModeloReal(PH("cardboard_box_01"), sala, new Vector3(2.5f, Altura(armario), -3.65f), 0.32f, -6, Colisao.Nenhuma);
        var mesinha = Mesinha(sala, new Vector3(-4.7f, 0, -2.2f), 90);
        var aRot = Ponto("Ancora_Roteador", sala, new Vector3(-4.68f, Altura(mesinha), -2.2f), -90);
        Lixeira(sala, new Vector3(4.6f, 0, 2.2f));
        ModeloReal(PH("potted_plant_02"), sala, new Vector3(-4.5f, 0, 3.5f), 0.8f, 0);

        DadosSala(sala, TipoSala.Contabilidade, spawn, new Vector2(1.2f, -2.7f),
            aImp, Caminho(sala, "Impressora", new Vector2(2.5f, -2.9f)),
            aRot, Caminho(sala, "Roteador", new Vector2(-1.0f, -2.7f), new Vector2(-3.9f, -2.4f)),
            aPc, Caminho(sala, "Computador", new Vector2(4.1f, -2.4f), new Vector2(4.15f, 2.2f), new Vector2(4.2f, 3.1f)));
        Girar(sala, TipoEndereco.Escola);
    }

    // ---------------- Laboratório de análises (tipo clínica; Rua das Palmeiras, lado norte) ----------------

    static void MontarLaboratorio(Transform spawn)
    {
        var mParede = MatPBR("Parede_Laboratorio", "PaintedPlaster017", new Color(0.92f, 0.97f, 0.95f), 0.12f);
        var mPiso = MatPBR("Piso_Laboratorio", "Tiles107", new Color(0.93f, 0.93f, 0.93f), 0.6f);
        var sala = SalaDoPredio(TipoEndereco.Laboratorio, mPiso, 2f, mParede,
            norte: new[] { Janela(0.4f, 1.4f) },
            sul: new[] { Porta(0f), Janela(2.8f, 1.4f) },
            leste: new[] { Janela(-1.5f) },
            oeste: null);
        // a lâmpada do lado oeste fica na frente das cabines (atrás delas a cortina ficava no escuro)
        TresLuzes(sala, new Vector2(1.6f, 2.0f), new Vector2(-2.6f, 0.6f), new Vector2(1.2f, -2.2f));
        PaineisLed(sala, new Vector2(1.6f, 2.0f), new Vector2(-2.6f, 0.6f), new Vector2(1.2f, -2.2f), new Vector2(-2.6f, -1.8f), new Vector2(-4.0f, 2.8f), new Vector2(-2.1f, 2.8f));

        // Recepção: balcão alto (o espelho do da clínica) e duas mesas atrás
        var mVerdeAgua = Mat("MDF_VerdeAgua", new Color(0.62f, 0.83f, 0.8f), 0.35f);
        BalcaoAlto(sala, -0.6f, 3.8f, 0.6f, mMdfBranco, mVerdeAgua, true);
        Estacao(sala, 0.4f, 1.45f, true, true, 0);
        float h = Estacao(sala, 2.8f, 1.45f, true, false, -1);
        var aPc = Ponto("Ancora_Computador", sala, new Vector3(2.8f, h, 1.4f), 180);
        ModeloReal(PH("office_notepads"), sala, new Vector3(3.4f, h, 1.55f), 0.3f, 170, Colisao.Nenhuma);
        ModeloReal(PH("potted_plant_04"), sala, new Vector3(3.3f, 1.14f, 0.6f), 0.25f, 0, Colisao.Nenhuma);
        Lixeira(sala, new Vector3(-0.25f, 0, 2.6f));

        // Duas cabines de coleta no canto noroeste: divisória, cadeira, mesinha e cortina meio aberta
        var mDivisoria = Mat("Divisoria_Branca", new Color(0.93f, 0.94f, 0.94f), 0.4f);
        var mCortina = Mat("Cortina_Azul", new Color(0.55f, 0.72f, 0.85f), 0.15f);
        Caixa(sala, "Divisoria_Meio", new Vector3(-3.05f, 1.0f, 2.8f), new Vector3(0.05f, 2.0f, 2.4f), mDivisoria, 1f);
        Caixa(sala, "Divisoria_Leste", new Vector3(-1.2f, 1.0f, 2.8f), new Vector3(0.05f, 2.0f, 2.4f), mDivisoria, 1f);
        foreach (float x in new[] { -4.0f, -2.1f })
        {
            ModeloReal(PH("dining_chair_02"), sala, new Vector3(x, 0, 3.2f), 0.95f, 180);
            Mesinha(sala, new Vector3(x + 0.6f, 0, 3.6f), 180);
            Caixa(sala, "Cortina", new Vector3(x - 0.55f, 1.15f, 1.62f), new Vector3(0.7f, 1.9f, 0.03f), mCortina, 1f, false);
        }
        Pessoa(sala, new Vector3(-4.0f, 0, 3.2f), 180, PessoaTrabalhando.Atividade.ConversandoSentado, 2);   // paciente na coleta

        // Espera: cadeiras na parede sul e na leste
        foreach (float x in new[] { -3.8f, -3.15f, -2.5f }) ModeloReal(PH("dining_chair_02"), sala, new Vector3(x, 0, -3.55f), 0.95f, 0);
        foreach (float z in new[] { -2.6f, -1.95f, -1.3f }) ModeloReal(PH("dining_chair_02"), sala, new Vector3(4.55f, 0, z), 0.95f, -90);
        Pessoa(sala, new Vector3(4.5f, 0, -1.95f), -90, PessoaTrabalhando.Atividade.ConversandoSentado, 1);
        ModeloReal(PH("wall_clock"), sala, new Vector3(-1.5f, 2.2f, -3.97f), 0.32f, 0, Colisao.Nenhuma);
        ModeloReal(PH("hanging_picture_frame_01"), sala, new Vector3(4.97f, 1.6f, 2.0f), 0.7f, 90, Colisao.Nenhuma);
        ModeloReal(PH("potted_plant_01"), sala, new Vector3(4.45f, 0, -3.5f), 0.9f, 0);

        // Impressora atrás da recepção (canto nordeste); roteador na parede oeste (sem janela: rack)
        var armario = Aparador(sala, new Vector3(3.6f, 0, 3.62f), 180);
        var aImp = Ponto("Ancora_Impressora", sala, new Vector3(3.4f, Altura(armario), 3.6f), 0);
        var mesinha = Mesinha(sala, new Vector3(-4.7f, 0, -1.0f), 90);
        var aRot = Ponto("Ancora_Roteador", sala, new Vector3(-4.68f, Altura(mesinha), -1.0f), -90);

        DadosSala(sala, TipoSala.Clinica, spawn, new Vector2(1.8f, -0.3f),
            aImp, Caminho(sala, "Impressora", new Vector2(4.3f, -0.2f), new Vector2(4.3f, 2.9f)),
            aRot, Caminho(sala, "Roteador", new Vector2(-1.1f, -0.6f), new Vector2(-3.95f, -0.6f)),
            aPc, Caminho(sala, "Computador", new Vector2(4.3f, -0.2f), new Vector2(4.3f, 2.4f), new Vector2(3.6f, 2.5f)));
        Girar(sala, TipoEndereco.Laboratorio);
    }

    // ---------------- Clínica veterinária e pet shop (tipo clínica; Rua das Sibipirunas, lado sul, girada) ----------------

    static void MontarVeterinaria(Transform spawn)
    {
        var mParede = MatPBR("Parede_Veterinaria", "PaintedPlaster017", new Color(0.86f, 0.93f, 0.97f), 0.12f);
        var mPiso = MatPBR("Piso_Veterinaria", "Tiles002", new Color(0.88f, 0.88f, 0.84f), 0.55f);
        var vitrine = new Abertura { largura = 2.4f, baixo = 0.5f, alto = 2.3f };
        var sala = SalaDoPredio(TipoEndereco.Veterinaria, mPiso, 2.4f, mParede,
            norte: null,
            sul: new[] { Mover(vitrine, -2.9f), Porta(0f), Mover(vitrine, 2.9f) },
            leste: new[] { Janela(2.6f) },
            oeste: null);
        TresLuzes(sala, new Vector2(-2.5f, 0.6f), new Vector2(2.5f, 0.6f), new Vector2(0f, -2.6f));
        Calhas(sala, new Vector2(-2.5f, 0.6f), new Vector2(2.5f, 0.6f), new Vector2(0f, -2.6f), new Vector2(0f, 2.8f));

        // Pet shop: prateleiras de ração e acessórios na parede oeste, sacos de ração empilhados no chão
        foreach (float z in new[] { -1.8f, 0.1f, 2.0f })
            CaixasNaEstante(sala, ModeloReal(PH("steel_frame_shelves_03"), sala, new Vector3(-4.68f, 0, z), 1.8f, 90), false);
        Material[] sacos = { Mat("Saco_Racao_Vermelho", new Color(0.72f, 0.2f, 0.16f), 0.3f), Mat("Saco_Racao_Verde", new Color(0.25f, 0.5f, 0.3f), 0.3f),
                             Mat("Saco_Racao_Amarelo", new Color(0.9f, 0.72f, 0.2f), 0.3f) };
        for (int i = 0; i < 6; i++)
        {
            var c = Caixa(sala, "Saco_Racao", new Vector3(-3.55f + (i % 3) * 0.5f, 0.2f + (i / 3) * 0.16f, -3.35f + (i / 3) * 0.05f),
                new Vector3(0.45f, 0.16f, 0.62f), sacos[i % 3], 1f, i < 3);
            c.transform.localRotation = Quaternion.Euler(0, (i * 13) % 9 - 4, 0);
        }

        // Consultório ao fundo: mesa de inox para os bichos, armário de remédios e o veterinário
        var mInox = Mat("Inox", new Color(0.78f, 0.79f, 0.8f), 0.75f, 1f);
        Caixa(sala, "Mesa_Inox", new Vector3(0.2f, 0.88f, 2.7f), new Vector3(1.3f, 0.05f, 0.65f), mInox, 1f);
        Caixa(sala, "Mesa_Inox_Prateleira", new Vector3(0.2f, 0.25f, 2.7f), new Vector3(1.2f, 0.03f, 0.58f), mInox, 1f, false);
        foreach (var d in new[] { new Vector2(-0.55f, -0.27f), new Vector2(0.55f, -0.27f), new Vector2(-0.55f, 0.27f), new Vector2(0.55f, 0.27f) })
            Caixa(sala, "Mesa_Inox_Pe", new Vector3(0.2f + d.x, 0.43f, 2.7f + d.y), new Vector3(0.04f, 0.86f, 0.04f), mInox, 1f, false);
        Pessoa(sala, new Vector3(0.2f, 0, 3.35f), 180, PessoaTrabalhando.Atividade.EmPe, 0);
        CaixasNaEstante(sala, ModeloReal(PH("steel_frame_shelves_03"), sala, new Vector3(-1.9f, 0, 3.68f), 1.8f, 180), false);
        ModeloReal(PH("hanging_picture_frame_01"), sala, new Vector3(-3.9f, 1.6f, 3.97f), 0.7f, 0, Colisao.Nenhuma);

        // Caixa da recepção perto da porta (como o da loja), com o computador do chamado
        Caixa(sala, "Balcao_Caixa", new Vector3(2.9f, 0.38f, -0.6f), new Vector3(0.6f, 0.76f, 2.4f), Mat("MDF_AzulClaro", new Color(0.55f, 0.72f, 0.86f), 0.35f), 1f);
        Caixa(sala, "Balcao_Caixa_Tampo", new Vector3(2.88f, 0.78f, -0.6f), new Vector3(0.72f, 0.04f, 2.5f), mMdfBranco, 1.2f);
        var aPc = Ponto("Ancora_Computador", sala, new Vector3(3.0f, 0.8f, -0.25f), -90);
        Caixa(sala, "Maquininha_Cartao", new Vector3(2.75f, 0.815f, -1.15f), new Vector3(0.075f, 0.03f, 0.16f), Mat("Plastico_Preto", new Color(0.04f, 0.04f, 0.045f), 0.55f), 1f, false);
        ModeloReal(SF("Cadeira_Escritorio"), sala, new Vector3(3.95f, 0, -0.35f), 1.15f, 90);

        // Espera junto à vitrine, com um cliente
        foreach (float x in new[] { -2.2f, -1.55f }) ModeloReal(PH("dining_chair_02"), sala, new Vector3(x, 0, -3.5f), 0.95f, 0);
        Pessoa(sala, new Vector3(-1.55f, 0, -3.5f), 0, PessoaTrabalhando.Atividade.ConversandoSentado, 1);
        ModeloReal(PH("wall_clock"), sala, new Vector3(4.97f, 2.2f, 0.6f), 0.32f, -90, Colisao.Nenhuma);
        ModeloReal(PH("potted_plant_02"), sala, new Vector3(-0.9f, 0, -3.5f), 0.8f, 0);

        // Impressora no canto nordeste; roteador na parede leste, ao sul do caixa (rack na parede sem janela)
        var armario = Aparador(sala, new Vector3(3.6f, 0, 3.62f), 180);
        var aImp = Ponto("Ancora_Impressora", sala, new Vector3(3.4f, Altura(armario), 3.6f), 0);
        var mesinha = Mesinha(sala, new Vector3(4.7f, 0, -2.6f), -90);
        var aRot = Ponto("Ancora_Roteador", sala, new Vector3(4.68f, Altura(mesinha), -2.6f), 90);

        DadosSala(sala, TipoSala.Clinica, spawn, new Vector2(1.6f, -1.7f),
            aImp, Caminho(sala, "Impressora", new Vector2(2.1f, 1.05f), new Vector2(2.75f, 2.65f)),
            aRot, Caminho(sala, "Roteador", new Vector2(2.2f, -2.7f), new Vector2(3.9f, -2.7f)),
            aPc, Caminho(sala, "Computador", new Vector2(2.1f, 1.05f), new Vector2(3.65f, 1.0f), new Vector2(3.8f, 0.4f)));
        Girar(sala, TipoEndereco.Veterinaria);
    }

    // ---------------- Farmácia (tipo loja; Rua das Acácias, lado norte) ----------------

    static void MontarFarmacia(Transform spawn)
    {
        var mParede = MatPBR("Parede_Farmacia", "PaintedPlaster017", new Color(0.97f, 0.98f, 0.97f), 0.12f);
        var mPiso = MatPBR("Piso_Farmacia", "Tiles002", new Color(0.97f, 0.97f, 0.97f), 0.65f);
        var vitrine = new Abertura { largura = 2.6f, baixo = 0.5f, alto = 2.3f };
        var sala = SalaDoPredio(TipoEndereco.Farmacia, mPiso, 2.4f, mParede,
            norte: null,
            sul: new[] { Mover(vitrine, -2.8f), Porta(0f), Mover(vitrine, 2.8f) },
            leste: null,
            oeste: new[] { Janela(3.0f, 1.0f) });
        TresLuzes(sala, new Vector2(-2.4f, 0f), new Vector2(2.0f, 1.5f), new Vector2(0f, -2.6f), 1.3f);
        Calhas(sala, new Vector2(-2.4f, 0f), new Vector2(2.0f, 1.5f), new Vector2(0f, -2.6f), new Vector2(-2.4f, 2.4f), new Vector2(2.4f, -1.2f), new Vector2(-0.2f, 1.5f));

        // Gôndolas de perfumaria e higiene na parede oeste e uma dupla no meio
        foreach (float z in new[] { -2.0f, -0.1f, 1.8f })
            CaixasNaEstante(sala, ModeloReal(PH("steel_frame_shelves_03"), sala, new Vector3(-4.68f, 0, z), 1.8f, 90), false);
        CaixasNaEstante(sala, ModeloReal(PH("steel_frame_shelves_03"), sala, new Vector3(-1.9f, 0, -0.7f), 1.8f, -90), false);
        CaixasNaEstante(sala, ModeloReal(PH("steel_frame_shelves_03"), sala, new Vector3(-1.34f, 0, -0.7f), 1.8f, 90), false);
        Pessoa(sala, new Vector3(-3.95f, 0, -0.8f), -90, PessoaTrabalhando.Atividade.EmPe, 2);   // cliente na gôndola

        // Balcão dos remédios ao fundo, com faixa verde; atrás, estantes de remédios e o farmacêutico
        var mVerde = Mat("Verde_Farmacia", new Color(0.12f, 0.55f, 0.32f), 0.4f);
        Caixa(sala, "Balcao_Remedios", new Vector3(1.0f, 0.45f, 2.0f), new Vector3(4.4f, 0.9f, 0.6f), mMdfBranco, 1f);
        Caixa(sala, "Balcao_Remedios_Tampo", new Vector3(1.0f, 0.92f, 2.0f), new Vector3(4.5f, 0.04f, 0.66f), Mat("Tampo_Cinza", new Color(0.72f, 0.73f, 0.74f), 0.55f), 1f);
        Caixa(sala, "Balcao_Remedios_Faixa", new Vector3(1.0f, 0.62f, 1.694f), new Vector3(4.4f, 0.22f, 0.012f), mVerde, 1f, false);
        var aPc = Ponto("Ancora_Computador", sala, new Vector3(2.6f, 0.94f, 2.05f), 180);
        ModeloReal(SF("Cadeira_Escritorio"), sala, new Vector3(2.6f, 0, 2.85f), 1.15f, 0);
        Pessoa(sala, new Vector3(0.6f, 0, 2.75f), 180, PessoaTrabalhando.Atividade.EmPe, 0);
        CaixasNaEstante(sala, ModeloReal(PH("steel_frame_shelves_03"), sala, new Vector3(-0.4f, 0, 3.68f), 1.8f, 180), false);
        CaixasNaEstante(sala, ModeloReal(PH("steel_frame_shelves_03"), sala, new Vector3(1.45f, 0, 3.68f), 1.8f, 180), false);
        Placa(sala, new Vector3(0.6f, 2.45f, 3.0f), Quaternion.identity, 2.4f, 0.4f, "MEDICAMENTOS", null);
        ModeloReal(PH("office_notepads"), sala, new Vector3(0.2f, 0.94f, 1.95f), 0.3f, 100, Colisao.Nenhuma);

        // Cantinho da pressão perto da vitrine: cadeira e o aparelho numa mesinha
        ModeloReal(PH("dining_chair_02"), sala, new Vector3(2.4f, 0, -3.45f), 0.95f, 0);
        var mesaPressao = Mesinha(sala, new Vector3(3.0f, 0, -3.55f), 0);
        Caixa(sala, "Aparelho_Pressao", new Vector3(3.0f, Altura(mesaPressao) + 0.05f, -3.55f), new Vector3(0.16f, 0.1f, 0.12f), mMdfBranco, 1f, false);
        Caixa(sala, "Cestinhas", new Vector3(-0.9f, 0.15f, -3.4f), new Vector3(0.45f, 0.3f, 0.32f), Mat("Cesta_Vermelha", new Color(0.75f, 0.12f, 0.12f), 0.4f), 1f);

        // Impressora e roteador na parede leste (sem janela: rack ao sul)
        var armario = Aparador(sala, new Vector3(4.64f, 0, 1.4f), -90);
        var aImp = Ponto("Ancora_Impressora", sala, new Vector3(4.6f, Altura(armario), 1.4f), 90);
        var mesinha = Mesinha(sala, new Vector3(4.7f, 0, -2.2f), -90);
        var aRot = Ponto("Ancora_Roteador", sala, new Vector3(4.68f, Altura(mesinha), -2.2f), 90);
        Lixeira(sala, new Vector3(3.6f, 0, 3.6f));

        DadosSala(sala, TipoSala.Loja, spawn, new Vector2(2.0f, -1.6f),
            aImp, Caminho(sala, "Impressora", new Vector2(4.0f, -0.6f), new Vector2(3.95f, 0.8f)),
            aRot, Caminho(sala, "Roteador", new Vector2(3.95f, -1.65f)),
            aPc, Caminho(sala, "Computador", new Vector2(4.0f, -0.6f), new Vector2(4.0f, 2.75f), new Vector2(3.35f, 2.8f)));
        Girar(sala, TipoEndereco.Farmacia);
    }

    // ---------------- Ótica (tipo loja; Rua das Acácias, lado sul, girada) ----------------

    static void MontarOtica(Transform spawn)
    {
        var mParede = MatPBR("Parede_Otica", "PaintedPlaster017", new Color(0.93f, 0.9f, 0.86f), 0.12f);
        var vitrine = new Abertura { largura = 2.6f, baixo = 0.5f, alto = 2.3f };
        var sala = SalaDoPredio(TipoEndereco.Otica, mPisoMadeira, 1.5f, mParede,
            norte: null,
            sul: new[] { Mover(vitrine, -2.8f), Porta(0f), Mover(vitrine, 2.8f) },
            leste: null,
            oeste: null);
        TresLuzes(sala, new Vector2(1.6f, 0.2f), new Vector2(-2.0f, 1.6f), new Vector2(0f, -2.4f));
        Pendentes(sala, new Vector2(1.6f, -0.7f), new Vector2(1.6f, 1.0f), new Vector2(-2.0f, 1.6f), new Vector2(0f, -2.4f));

        // Parede leste em painel de madeira com prateleiras de armações
        Caixa(sala, "Painel_Madeira", new Vector3(4.985f, 1.4f, -0.5f), new Vector3(0.03f, 2.6f, 6.4f), mMadeira, 1.2f, false);
        Material[] armacoes = { Mat("Armacao_Preta", new Color(0.05f, 0.05f, 0.06f), 0.6f), Mat("Armacao_Tartaruga", new Color(0.35f, 0.2f, 0.1f), 0.6f),
                                Mat("Armacao_Vermelha", new Color(0.6f, 0.1f, 0.12f), 0.6f), Mat("Armacao_Azul", new Color(0.12f, 0.22f, 0.5f), 0.6f), mAluminio };
        var mVidroPrat = Mat("Prateleira_Vidro", new Color(0.85f, 0.9f, 0.9f), 0.9f);
        var rnd = new System.Random(48);
        foreach (float y in new[] { 1.0f, 1.32f, 1.64f, 1.96f })
        {
            Caixa(sala, "Prateleira_Oculos", new Vector3(4.87f, y, -0.5f), new Vector3(0.2f, 0.015f, 5.6f), mVidroPrat, 1f, false, false);
            for (float z = -3.1f; z < 2.1f; z += 0.26f)
                Caixa(sala, "Oculos", new Vector3(4.86f, y + 0.03f, z), new Vector3(0.05f, 0.045f, 0.14f), armacoes[rnd.Next(armacoes.Length)], 1f, false, false);
        }
        Caixa(sala, "Espelho", new Vector3(4.96f, 1.5f, 2.75f), new Vector3(0.02f, 1.1f, 0.5f), Mat("Espelho", new Color(0.8f, 0.85f, 0.88f), 0.97f, 1f), 1f, false);

        // Dois balcões-vitrine com tampo de vidro e cadeiras para quem prova os óculos
        var mEscuro = Mat("MDF_Grafite", new Color(0.2f, 0.21f, 0.23f), 0.4f);
        foreach (float z in new[] { -0.7f, 1.0f })
        {
            Caixa(sala, "Balcao_Vitrine", new Vector3(1.6f, 0.47f, z), new Vector3(1.8f, 0.94f, 0.55f), mEscuro, 1f);
            Caixa(sala, "Balcao_Vitrine_Vidro", new Vector3(1.6f, 0.955f, z), new Vector3(1.82f, 0.02f, 0.57f), mVidro, 1f, false, false);
            for (int i = 0; i < 5; i++)
                Caixa(sala, "Oculos", new Vector3(0.95f + i * 0.32f, 0.965f, z), new Vector3(0.14f, 0.02f, 0.05f), armacoes[(i + (int)(z * 3)) & 3], 1f, false, false);
            ModeloReal(PH("dining_chair_02"), sala, new Vector3(1.6f, 0, z - 0.65f), 0.95f, 0);
        }
        Pessoa(sala, new Vector3(2.95f, 0, -0.7f), -90, PessoaTrabalhando.Atividade.EmPe, 0);   // vendedora
        Pessoa(sala, new Vector3(1.6f, 0, -1.35f), 0, PessoaTrabalhando.Atividade.ConversandoSentado, 2);   // cliente provando

        // Cantinho do exame de vista: tabela de letras na parede norte e a cadeira virada para ela
        var mPapel = Mat("Papel", Color.white, 0.1f);
        var mPreto = Mat("Plastico_Preto", new Color(0.04f, 0.04f, 0.045f), 0.55f);
        Caixa(sala, "Tabela_Letras", new Vector3(-3.6f, 1.5f, 3.985f), new Vector3(0.5f, 0.75f, 0.02f), mPapel, 1f, false, false);
        for (int i = 0; i < 6; i++)
        {
            float lw = 0.36f - i * 0.05f;
            Caixa(sala, "Letras", new Vector3(-3.6f, 1.76f - i * 0.1f, 3.973f), new Vector3(lw, 0.05f - i * 0.006f, 0.004f), mPreto, 1f, false, false);
        }
        ModeloReal(SF("Cadeira_Escritorio"), sala, new Vector3(-3.6f, 0, 0.9f), 1.15f, 180);
        var mesaExame = Mesinha(sala, new Vector3(-2.8f, 0, 1.2f), 0);
        Caixa(sala, "Aparelho_Exame", new Vector3(-2.8f, Altura(mesaExame) + 0.1f, 1.2f), new Vector3(0.28f, 0.2f, 0.18f), mPreto, 1f, false);

        // Mesa do atendimento no fundo (computador do chamado) e a impressora num aparador ao lado
        var mesa = ModeloReal(PH("metal_office_desk"), sala, new Vector3(2.6f, 0, 3.2f), LadoMesaMetal, 180);
        float h = Altura(mesa);
        ModeloReal(SF("Cadeira_Escritorio"), sala, new Vector3(2.6f, 0, 2.3f), 1.15f, 180);
        var aPc = Ponto("Ancora_Computador", sala, new Vector3(2.6f, h, 3.25f), 0);
        ModeloReal(PH("office_notepads"), sala, new Vector3(3.3f, h, 3.0f), 0.3f, -20, Colisao.Nenhuma);
        var armario = Aparador(sala, new Vector3(0.0f, 0, 3.62f), 180);
        var aImp = Ponto("Ancora_Impressora", sala, new Vector3(-0.2f, Altura(armario), 3.6f), 0);

        // Espera junto à vitrine e roteador na parede oeste (sem janela: rack)
        ModeloReal(PH("sofa_02"), sala, new Vector3(-2.9f, 0, -3.5f), 1.81f, RotSofa);
        ModeloReal(PH("modern_coffee_table_01"), sala, new Vector3(-2.9f, 0, -2.55f), 1.0f, 90);
        ModeloReal(PH("potted_plant_01"), sala, new Vector3(4.4f, 0, 3.55f), 0.9f, 0);
        var mesinha = Mesinha(sala, new Vector3(-4.7f, 0, -0.6f), 90);
        var aRot = Ponto("Ancora_Roteador", sala, new Vector3(-4.68f, Altura(mesinha), -0.6f), -90);
        Lixeira(sala, new Vector3(3.7f, 0, 2.6f));

        DadosSala(sala, TipoSala.Loja, spawn, new Vector2(0.6f, -2.3f),
            aImp, Caminho(sala, "Impressora", new Vector2(-0.3f, -1.2f), new Vector2(-0.4f, 2.75f)),
            aRot, Caminho(sala, "Roteador", new Vector2(-0.3f, -1.2f), new Vector2(-3.9f, -1.1f)),
            aPc, Caminho(sala, "Computador", new Vector2(-0.3f, -1.2f), new Vector2(-0.3f, 2.2f), new Vector2(1.5f, 2.35f)));
        Girar(sala, TipoEndereco.Otica);
    }
}
