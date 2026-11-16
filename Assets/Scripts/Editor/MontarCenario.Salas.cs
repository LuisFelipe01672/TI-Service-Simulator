using System.IO;
using UnityEditor;
using UnityEngine;

// Etapa 3: três clientes com sala própria — escritório de contabilidade (a sala "Cliente" de antes), clínica e
// loja. Cada sala tem a sua cara (layout, móveis, decoração e gente trabalhando) e pontos para os equipamentos:
// impressora, roteador e computador são um conjunto só na cena e vão para a sala do cliente quando o chamado é
// aceito (SalaCliente). Cada sala também guarda o caminho que o contato faz até cada equipamento.
// Coordenadas locais da sala: x de oeste (-) a leste (+), z de sul (-) a norte (+); a porta fica ao sul (x = 0).
// Giros (rotY) conferidos nas fotos: mesa de metal e armário com a frente em +z; cadeira de escritório, impressora,
// roteador e computador com a frente em -z; cadeira de jantar e personagens com a frente em +z.
public static partial class MontarCenario
{
    internal static Vector3 CentroClinica => new Vector3(40f, 0f, 14f);
    internal static Vector3 CentroLoja => new Vector3(40f, 0f, -14f);
    // Textura de piso cerâmico (ambientCG, CC0). Sem ela, o piso fica só com a cor.
    const string TexturaPisoCeramico = "Tiles002";   // porcelanato branco 60 x 60

    static Material mParedeClinica, mPisoClinica, mParedeLoja, mPisoLoja, mMdfBranco, mTelaPlanilha;

    static void CriarMateriaisSalas()
    {
        mParedeClinica = MatPBR("Parede_Clinica", "PaintedPlaster017", new Color(0.95f, 0.97f, 0.96f), 0.12f);
        mPisoClinica = MatPBR("Piso_Clinica", TexturaPisoCeramico, new Color(0.95f, 0.95f, 0.94f), 0.6f);
        mParedeLoja = MatPBR("Parede_Loja", "PaintedPlaster017", new Color(0.97f, 0.9f, 0.74f), 0.12f);
        mPisoLoja = MatPBR("Piso_Loja", TexturaPisoCeramico, new Color(0.8f, 0.78f, 0.75f), 0.55f);
        mMdfBranco = Mat("MDF_Branco", new Color(0.9f, 0.9f, 0.88f), 0.35f);
        mTelaPlanilha = Mat("Tela_Planilha", Color.white, 0.85f);
        var tex = TexturaPlanilha();
        mTelaPlanilha.mainTexture = tex;
        mTelaPlanilha.SetTexture("_EmissionMap", tex);
        Brilhar(mTelaPlanilha, Color.white * 0.75f);
    }

    // ---------------- Contabilidade (a sala "Cliente" de antes): complementos ----------------

    // Chamado pelo MontarCliente depois dos móveis e dos equipamentos de sempre
    static void CompletarContabilidade(Transform sala, Transform spawnEscritorio, float hMesa1)
    {
        // Estação 3, no meio da sala, com um colega digitando
        var mesa3 = ModeloReal(PH("metal_office_desk"), sala, new Vector3(1.6f, 0, 0.6f), LadoMesaMetal, RotMesa);
        float h3 = Altura(mesa3);
        ModeloReal(SF("Cadeira_Escritorio"), sala, new Vector3(1.6f, 0, -0.3f), 1.15f, RotCadeira);
        MonitorLigado(sala, new Vector3(1.5f, h3, 0.65f), 180);
        ModeloReal(PH("office_notepads"), sala, new Vector3(2.25f, h3, 0.45f), 0.3f, 10, Colisao.Nenhuma);
        ModeloReal(SF("Telefone_Escritorio"), sala, new Vector3(0.85f, h3, 0.5f), 0.22f, 200, Colisao.Nenhuma);
        Pessoa(sala, new Vector3(1.6f, 0, -0.32f), 0, PessoaTrabalhando.Atividade.Digitando, 2);

        // Colega da estação 2 digitando (o monitor dela agora está ligado)
        var pc2 = sala.Find("Computador_Mesa");
        if (pc2 != null) TelaAcesa(pc2.gameObject);
        Pessoa(sala, new Vector3(1.35f, 0, 2.48f), 0, PessoaTrabalhando.Atividade.Digitando, 1);
        ModeloReal(SF("Telefone_Escritorio"), sala, new Vector3(-3.45f, hMesa1, 3.4f), 0.22f, 160, Colisao.Nenhuma);

        // Arquivo morto: estante de aço com caixas de documentos, na parede sul (a oeste tem janela)
        var estante = ModeloReal(PH("steel_frame_shelves_03"), sala, new Vector3(-3.0f, 0, -3.68f), 1.8f, 0);
        CaixasNaEstante(sala, estante, true);
        ModeloReal(PH("wall_clock"), sala, new Vector3(-1.2f, 2.2f, -3.97f), 0.32f, 0, Colisao.Nenhuma);
        ModeloReal(PH("hanging_picture_frame_01"), sala, new Vector3(-4.97f, 1.55f, 2.9f), 0.7f, -90, Colisao.Nenhuma);

        // Pontos dos equipamentos (os mesmos lugares em que o PosicionarEquipamento deixou cada um)
        var aPc = Ponto("Ancora_Computador", sala, new Vector3(-2.8f, hMesa1, 3.55f), 0);
        var aImp = Ponto("Ancora_Impressora", sala, new Vector3(4.6f, AlturaDe(sala, "modern_wooden_cabinet"), 0.8f), 90);
        var aRot = Ponto("Ancora_Roteador", sala, new Vector3(-4.68f, AlturaDe(sala, "side_table_01"), -1.8f), -90);

        DadosSala(sala, TipoSala.Contabilidade, spawnEscritorio, new Vector2(0.9f, -0.9f),
            aImp, Caminho(sala, "Impressora", new Vector2(3.0f, -0.4f), new Vector2(3.75f, 1.55f)),
            aRot, Caminho(sala, "Roteador", new Vector2(-3.75f, -1.05f)),
            aPc, Caminho(sala, "Computador", new Vector2(-0.6f, 1.8f), new Vector2(-1.85f, 2.35f)));
    }

    // ---------------- Clínica ----------------

    static void MontarClinica(Transform spawnEscritorio)
    {
        var sala = Sala("Cliente_Clinica", CentroClinica, 10f, 8f, mPisoClinica, 2.4f, mParedeClinica,
            norte: new[] { Janela(1.3f, 1.4f) },
            sul: new[] { Porta(0f) },
            leste: new[] { Janela(-1.2f) },
            oeste: new[] { Janela(-1.7f) });
        // Clínica tem teto de gesso liso, branco
        var forro = sala.Find("Forro");
        if (forro != null)
            forro.GetComponent<MeshRenderer>().sharedMaterial = MatPBR("Teto_Gesso", "PaintedPlaster017", new Color(0.97f, 0.97f, 0.96f), 0.08f);

        LuzTeto(sala, "Luz_Teto_1", new Vector3(1.3f, H - 0.15f, 1.8f), 1.3f, 6.5f, true);
        LuzTeto(sala, "Luz_Teto_2", new Vector3(-2.8f, H - 0.15f, -1.6f), 1.2f, 6.5f, false);
        LuzTeto(sala, "Luz_Teto_3", new Vector3(2.8f, H - 0.15f, -2.2f), 1.1f, 6.5f, false);
        Sonda(sala, new Vector3(10f, H, 8f));
        foreach (var p in new[] { new Vector2(1.3f, 1.8f), new Vector2(-2.8f, -1.6f), new Vector2(2.8f, -2.2f) })
            ModeloReal(PH("modern_ceiling_lamp_01"), sala, new Vector3(p.x, H - 0.35f, p.y), 0.45f, 0, Colisao.Nenhuma);

        // Recepção: balcão alto de MDF branco com tampo de madeira; atrás, duas mesas com computador
        Caixa(sala, "Balcao", new Vector3(1.25f, 0.55f, 1.1f), new Vector3(4.3f, 1.1f, 0.12f), mMdfBranco, 1f);
        Caixa(sala, "Balcao_Tampo", new Vector3(1.25f, 1.12f, 1.17f), new Vector3(4.4f, 0.04f, 0.36f), mMadeira, 1.2f);
        Caixa(sala, "Balcao_Faixa", new Vector3(1.25f, 0.95f, 1.035f), new Vector3(4.3f, 0.12f, 0.012f), mMadeira, 1.2f, false);   // faixa de madeira na frente
        Caixa(sala, "Balcao_Rodape", new Vector3(1.25f, 0.04f, 1.03f), new Vector3(4.3f, 0.08f, 0.012f), Mat("Rodape_Escuro", new Color(0.18f, 0.18f, 0.19f), 0.3f), 1f, false);
        Caixa(sala, "Balcao_Lateral", new Vector3(3.4f, 0.55f, 1.45f), new Vector3(0.12f, 1.1f, 0.6f), mMdfBranco, 1f);

        var mesa1 = ModeloReal(PH("metal_office_desk"), sala, new Vector3(0.2f, 0, 1.95f), LadoMesaMetal, 0);
        float h = Altura(mesa1);
        ModeloReal(SF("Cadeira_Escritorio"), sala, new Vector3(0.2f, 0, 2.8f), 1.15f, 0);
        MonitorLigado(sala, new Vector3(0.3f, h, 1.9f), 0);
        ModeloReal(SF("Telefone_Escritorio"), sala, new Vector3(-0.55f, h, 1.85f), 0.22f, 20, Colisao.Nenhuma);
        Pessoa(sala, new Vector3(0.2f, 0, 2.82f), 180, PessoaTrabalhando.Atividade.Digitando, 0);

        ModeloReal(PH("metal_office_desk"), sala, new Vector3(2.4f, 0, 1.95f), LadoMesaMetal, 0);
        ModeloReal(SF("Cadeira_Escritorio"), sala, new Vector3(2.4f, 0, 2.85f), 1.15f, -10);   // a da recepcionista (o contato)
        ModeloReal(PH("office_notepads"), sala, new Vector3(3.05f, h, 2.05f), 0.3f, 170, Colisao.Nenhuma);
        Lixeira(sala, new Vector3(-0.95f, 0, 2.65f));
        var aPc = Ponto("Ancora_Computador", sala, new Vector3(2.4f, h, 1.9f), 180);   // tela para a recepcionista

        // Impressora num aparador no canto noroeste (fora da vista dos pacientes)
        var armario = ModeloReal(PH("modern_wooden_cabinet"), sala, new Vector3(-3.2f, 0, 3.62f), 2.4f, 180);
        var aImp = Ponto("Ancora_Impressora", sala, new Vector3(-3.4f, Altura(armario), 3.6f), 0);
        ModeloReal(PH("cardboard_box_01"), sala, new Vector3(-2.35f, Altura(armario), 3.65f), 0.32f, 8, Colisao.Nenhuma);   // resma de papel
        ModeloReal(PH("potted_plant_02"), sala, new Vector3(-4.5f, 0, 3.5f), 0.8f, 0);

        // Roteador numa mesinha na parede leste, atrás da recepção
        var mesinha = ModeloReal(PH("side_table_01"), sala, new Vector3(4.7f, 0, 2.9f), 0.55f, -90);
        var aRot = Ponto("Ancora_Roteador", sala, new Vector3(4.68f, Altura(mesinha), 2.9f), 90);

        // Sala de espera: cadeiras na parede oeste e na sul, revistas, planta, relógio e quadros
        float[] zOeste = { -2.6f, -1.95f, -1.3f, -0.65f };
        foreach (float z in zOeste) ModeloReal(PH("dining_chair_02"), sala, new Vector3(-4.55f, 0, z), 0.95f, 90);
        float[] xSul = { -3.8f, -3.15f, -2.5f };
        foreach (float x in xSul) ModeloReal(PH("dining_chair_02"), sala, new Vector3(x, 0, -3.55f), 0.95f, 0);
        ModeloReal(PH("potted_plant_01"), sala, new Vector3(-4.45f, 0, -3.5f), 0.9f, 0);
        var mesaCentro = ModeloReal(PH("modern_coffee_table_01"), sala, new Vector3(-3.3f, 0, -1.6f), 0.9f, 0);
        ModeloReal(PH("office_notepads"), sala, new Vector3(-3.3f, Altura(mesaCentro), -1.6f), 0.3f, 35, Colisao.Nenhuma);
        Pessoa(sala, new Vector3(-4.5f, 0, -1.95f), 90, PessoaTrabalhando.Atividade.ConversandoSentado, 2);
        Pessoa(sala, new Vector3(-3.15f, 0, -3.5f), 0, PessoaTrabalhando.Atividade.ConversandoSentado, 1);
        ModeloReal(PH("wall_clock"), sala, new Vector3(1.3f, 2.4f, 3.97f), 0.32f, RotRelogio, Colisao.Nenhuma);
        ModeloReal(PH("hanging_picture_frame_01"), sala, new Vector3(-4.97f, 1.6f, 1.2f), 0.7f, -90, Colisao.Nenhuma);
        ModeloReal(PH("hanging_picture_frame_01"), sala, new Vector3(4.97f, 1.6f, -2.8f), 0.7f, 90, Colisao.Nenhuma);
        ModeloReal(PH("potted_plant_04"), sala, new Vector3(-0.55f, 1.14f, 1.2f), 0.25f, 0, Colisao.Nenhuma);   // no balcão
        Lixeira(sala, new Vector3(-1.7f, 0, -3.7f));

        DadosSala(sala, TipoSala.Clinica, spawnEscritorio, new Vector2(1.3f, 0.35f),
            aImp, Caminho(sala, "Impressora", new Vector2(-1.6f, 0.4f), new Vector2(-1.75f, 2.85f), new Vector2(-2.3f, 3.05f)),
            aRot, Caminho(sala, "Roteador", new Vector2(3.95f, 0.45f), new Vector2(3.9f, 2.25f)),
            aPc, Caminho(sala, "Computador", new Vector2(3.95f, 0.45f), new Vector2(3.95f, 2.6f), new Vector2(3.35f, 2.85f)));
    }

    // ---------------- Loja (papelaria/variedades) ----------------

    static void MontarLoja(Transform spawnEscritorio)
    {
        // Vitrines dos dois lados da porta; os fundos (norte) sem janela: é a parede do estoque
        var vitrine = new Abertura { largura = 2.6f, baixo = 0.5f, alto = 2.3f };
        var sala = Sala("Cliente_Loja", CentroLoja, 10f, 8f, mPisoLoja, 2.4f, mParedeLoja,
            norte: null,
            sul: new[] { Mover(vitrine, -2.8f), Porta(0f), Mover(vitrine, 2.8f) },
            leste: new[] { Janela(2.6f) },
            oeste: null);
        var forro = sala.Find("Forro");
        if (forro != null)
            forro.GetComponent<MeshRenderer>().sharedMaterial = MatPBR("Teto_Gesso", "PaintedPlaster017", new Color(0.97f, 0.97f, 0.96f), 0.08f);

        LuzTeto(sala, "Luz_Teto_1", new Vector3(-2.5f, H - 0.15f, 0.6f), 1.3f, 6.5f, true);
        LuzTeto(sala, "Luz_Teto_2", new Vector3(2.5f, H - 0.15f, 0.6f), 1.2f, 6.5f, false);
        LuzTeto(sala, "Luz_Teto_3", new Vector3(0f, H - 0.15f, -2.6f), 1.1f, 6.5f, false);
        Sonda(sala, new Vector3(10f, H, 8f));
        // Luminárias tubulares de loja (calhas com lâmpada acesa)
        var mCalha = Mat("Calha_Luminaria", new Color(0.92f, 0.92f, 0.9f), 0.5f);
        var mTubo = Mat("Tubo_Aceso", new Color(1f, 0.98f, 0.94f), 0.5f);
        Brilhar(mTubo, new Color(1f, 0.97f, 0.9f) * 1.8f);
        foreach (var p in new[] { new Vector2(-2.5f, 0.6f), new Vector2(2.5f, 0.6f), new Vector2(0f, -2.6f), new Vector2(-2.5f, 2.8f), new Vector2(2.5f, 2.8f) })
        {
            Caixa(sala, "Calha", new Vector3(p.x, H - 0.03f, p.y), new Vector3(1.25f, 0.05f, 0.22f), mCalha, 1f, false, false);
            Caixa(sala, "Tubo", new Vector3(p.x, H - 0.065f, p.y), new Vector3(1.18f, 0.025f, 0.05f), mTubo, 1f, false, false);
        }

        // Prateleiras de aço na parede oeste, cheias de mercadoria; uma gôndola dupla no meio
        foreach (float z in new[] { -1.2f, 0.65f, 2.5f })
            CaixasNaEstante(sala, ModeloReal(PH("steel_frame_shelves_03"), sala, new Vector3(-4.68f, 0, z), 1.8f, 90), false);
        CaixasNaEstante(sala, ModeloReal(PH("steel_frame_shelves_03"), sala, new Vector3(-1.6f, 0, 0.9f), 1.8f, -90), false);
        CaixasNaEstante(sala, ModeloReal(PH("steel_frame_shelves_03"), sala, new Vector3(-1.04f, 0, 0.9f), 1.8f, 90), false);
        Pessoa(sala, new Vector3(-3.95f, 0, 0.4f), -90, PessoaTrabalhando.Atividade.EmPe, 0);   // repondo a prateleira
        // Mesa de promoção perto da vitrine, com mercadoria empilhada
        var promo = ModeloReal(PH("round_wooden_table_01"), sala, new Vector3(-1.3f, 0, -2.1f), 0.9f, 0);
        if (promo != null)
        {
            float topo = Altura(promo);
            var rndP = new System.Random(5);
            Material[] coresP = { Mat("Produto_Azul", new Color(0.18f, 0.35f, 0.65f), 0.35f), Mat("Produto_Vermelho", new Color(0.7f, 0.16f, 0.14f), 0.35f),
                                  Mat("Produto_Amarelo", new Color(0.92f, 0.74f, 0.18f), 0.35f), Mat("Produto_Verde", new Color(0.2f, 0.55f, 0.3f), 0.35f) };
            for (int i = 0; i < 7; i++)
            {
                float a = i * 0.9f, r = i == 0 ? 0f : 0.24f;
                var c = Caixa(promo.transform, "Produto", new Vector3(Mathf.Cos(a) * r, topo + 0.06f, Mathf.Sin(a) * r), new Vector3(0.14f, 0.12f, 0.1f), coresP[rndP.Next(coresP.Length)], 1f, false);
                c.transform.localRotation = Quaternion.Euler(0, (float)rndP.NextDouble() * 60f, 0);
            }
        }
        Pessoa(sala, new Vector3(-0.15f, 0, 1.3f), -90, PessoaTrabalhando.Atividade.EmPe, 2);   // cliente olhando a gôndola

        // Balcão do caixa (78 cm) com o computador virado para dentro; maquininha de cartão e cadeira
        Caixa(sala, "Balcao_Caixa", new Vector3(2.9f, 0.38f, -0.6f), new Vector3(0.6f, 0.76f, 2.4f), mMdfBranco, 1f);
        Caixa(sala, "Balcao_Caixa_Tampo", new Vector3(2.88f, 0.78f, -0.6f), new Vector3(0.72f, 0.04f, 2.5f), mMadeira, 1.2f);
        var aPc = Ponto("Ancora_Computador", sala, new Vector3(3.0f, 0.8f, -0.25f), -90);   // tela para o lado do caixa (leste)
        var mPreto = Mat("Plastico_Preto", new Color(0.04f, 0.04f, 0.045f), 0.55f);
        Caixa(sala, "Maquininha_Cartao", new Vector3(2.75f, 0.815f, -1.15f), new Vector3(0.075f, 0.03f, 0.16f), mPreto, 1f, false);
        ModeloReal(PH("office_notepads"), sala, new Vector3(2.95f, 0.8f, -1.55f), 0.3f, 80, Colisao.Nenhuma);
        ModeloReal(SF("Cadeira_Escritorio"), sala, new Vector3(3.95f, 0, -0.35f), 1.15f, 90);
        Lixeira(sala, new Vector3(3.6f, 0, -1.6f));

        // Fundos: aparador com a impressora (escritório da loja) e caixas de mercadoria chegando
        var armario = ModeloReal(PH("modern_wooden_cabinet"), sala, new Vector3(3.6f, 0, 3.62f), 2.4f, 180);
        var aImp = Ponto("Ancora_Impressora", sala, new Vector3(3.4f, Altura(armario), 3.6f), 0);
        var cx = ModeloReal(PH("cardboard_box_01"), sala, new Vector3(1.2f, 0, 3.5f), 0.55f, 12);
        ModeloReal(PH("cardboard_box_01"), sala, new Vector3(1.2f, Altura(cx), 3.5f), 0.45f, -10, Colisao.Nenhuma);
        ModeloReal(PH("cardboard_box_01"), sala, new Vector3(0.45f, 0, 3.55f), 0.5f, 30);

        // Roteador numa mesinha na parede leste, entre o caixa e os fundos
        var mesinha = ModeloReal(PH("side_table_01"), sala, new Vector3(4.7f, 0, 1.5f), 0.55f, -90);
        var aRot = Ponto("Ancora_Roteador", sala, new Vector3(4.68f, Altura(mesinha), 1.5f), 90);

        ModeloReal(PH("potted_plant_02"), sala, new Vector3(4.5f, 0, -3.5f), 0.8f, 0);
        ModeloReal(PH("wall_clock"), sala, new Vector3(4.97f, 2.2f, -1.0f), 0.32f, -90, Colisao.Nenhuma);

        DadosSala(sala, TipoSala.Loja, spawnEscritorio, new Vector2(1.6f, -1.7f),
            aImp, Caminho(sala, "Impressora", new Vector2(2.1f, 1.05f), new Vector2(2.75f, 2.65f)),
            aRot, Caminho(sala, "Roteador", new Vector2(2.1f, 1.05f), new Vector2(3.85f, 1.45f)),
            aPc, Caminho(sala, "Computador", new Vector2(2.1f, 1.05f), new Vector2(3.65f, 1.0f), new Vector2(3.8f, 0.4f)));
    }

    // ---------------- Peças comuns das salas ----------------

    // Dados da sala para o jogo: chegada pela porta sul, recepção, pontos e caminhos dos equipamentos e a porta de volta
    static void DadosSala(Transform sala, TipoSala tipo, Transform spawnEscritorio, Vector2 recepcao,
        Transform aImp, Transform[] cImp, Transform aRot, Transform[] cRot, Transform aPc, Transform[] cPc)
    {
        var dados = new GameObject("Dados_Sala");
        dados.transform.SetParent(sala, false);
        var s = dados.AddComponent<SalaCliente>();
        s.tipo = tipo;
        s.sala = sala;
        s.chegada = Ponto("Chegada", sala, new Vector3(0, 1f, -3.2f), 0);
        s.recepcao = Ponto("Recepcao", sala, new Vector3(recepcao.x, 0, recepcao.y), 180);
        s.ancoraImpressora = aImp; s.caminhoImpressora = cImp;
        s.ancoraRoteador = aRot; s.caminhoRoteador = cRot;
        s.ancoraComputador = aPc; s.caminhoComputador = cPc;

        var porta = new GameObject("Porta_VoltarEscritorio");
        porta.transform.SetParent(sala, false);
        porta.transform.localPosition = new Vector3(0, 1f, -3.75f);
        var tp = porta.AddComponent<Teleporte>();
        tp.destino = spawnEscritorio;
        tp.rotulo = "Voltar ao escritório";
        tp.distanciaInteracao = 1.8f;
    }

    static Transform[] Caminho(Transform sala, string nome, params Vector2[] pontos)
    {
        var pai = new GameObject("Caminho_" + nome).transform;
        pai.SetParent(sala, false);
        var lista = new Transform[pontos.Length];
        for (int i = 0; i < pontos.Length; i++) lista[i] = Ponto("P" + i, pai, new Vector3(pontos[i].x, 0, pontos[i].y), 0);
        return lista;
    }

    static void Pessoa(Transform sala, Vector3 pos, float rotY, PessoaTrabalhando.Atividade atividade, int personagem)
    {
        var p = Ponto("Pessoa_" + atividade, sala, pos, rotY);
        var pt = p.gameObject.AddComponent<PessoaTrabalhando>();
        pt.atividade = atividade;
        pt.personagem = personagem;
    }

    static float AlturaDe(Transform sala, string nomeModelo)
    {
        var t = sala.Find(nomeModelo);
        return t != null ? Altura(t.gameObject) : 0.7f;
    }

    // Monitor de mesa ligado (decoração: tela com planilha/agenda acesa); rotY 0 = tela para +z
    static GameObject MonitorLigado(Transform sala, Vector3 pos, float rotY)
    {
        var pc = ModeloReal(SF("Computador_Mesa"), sala, pos, 0.85f, rotY, Colisao.Nenhuma);
        if (pc != null) TelaAcesa(pc);
        return pc;
    }

    // Troca a tela do modelo por uma película preta com a planilha acesa por cima (a textura original tem marca real)
    static void TelaAcesa(GameObject pc)
    {
        var viva = pc.transform.Find("TelaPC");
        if (viva != null) Object.DestroyImmediate(viva.gameObject);
        var preta = pc.transform.Find("TelaDesligada");
        if (preta == null)
        {
            Object.DestroyImmediate(TelaNoMonitor(pc, out _).gameObject);
            preta = pc.transform.Find("TelaDesligada");
        }
        if (preta == null) return;
        var acesa = Object.Instantiate(preta.gameObject, preta.parent);
        acesa.name = "TelaAcesa";
        acesa.transform.position = preta.position - preta.forward * 0.0008f;
        acesa.transform.localScale = Vector3.Scale(preta.localScale, new Vector3(0.96f, 0.93f, 1f));
        acesa.GetComponent<Renderer>().sharedMaterial = mTelaPlanilha;
    }

    // Planilha simples desenhada em código (cabeçalho azul, linhas, algumas células coloridas): cara de tela de
    // sistema/planilha sem copiar a interface de nenhum programa real
    static Texture2D TexturaPlanilha()
    {
        const string caminho = "Assets/Textures/Gerados/Tela_Planilha.png";
        if (!AssetDatabase.IsValidFolder("Assets/Textures/Gerados")) AssetDatabase.CreateFolder("Assets/Textures", "Gerados");
        if (!File.Exists(caminho))
        {
            const int w = 256, hh = 160;
            var t = new Texture2D(w, hh, TextureFormat.RGB24, false);
            var rnd = new System.Random(7);
            for (int y = 0; y < hh; y++)
                for (int x = 0; x < w; x++)
                {
                    Color c = new Color(0.97f, 0.97f, 0.96f);
                    int yy = hh - 1 - y;
                    if (yy < 14) c = new Color(0.2f, 0.4f, 0.7f);                     // barra de título
                    else if (yy < 24) c = new Color(0.88f, 0.9f, 0.93f);              // menu
                    else if (x < 40) c = new Color(0.9f, 0.92f, 0.95f);               // lista lateral
                    if (yy >= 24 && x >= 40 && (yy - 24) % 10 == 0) c = new Color(0.78f, 0.8f, 0.83f);   // linhas
                    if (yy >= 24 && x >= 40 && (x - 40) % 43 == 0) c = new Color(0.78f, 0.8f, 0.83f);    // colunas
                    t.SetPixel(x, y, c);
                }
            // texto (tracinhos cinza) e algumas células marcadas
            for (int linha = 0; linha < 13; linha++)
                for (int col = 0; col < 5; col++)
                {
                    int x0 = 43 + col * 43, y0 = hh - 1 - (27 + linha * 10);
                    int comp = 12 + rnd.Next(24);
                    Color tinta = col == 4 && rnd.Next(4) == 0 ? new Color(0.75f, 0.2f, 0.2f) : new Color(0.35f, 0.37f, 0.4f);
                    for (int x = x0; x < x0 + comp && x < w; x++) { t.SetPixel(x, y0 - 3, tinta); t.SetPixel(x, y0 - 4, tinta); }
                    if (rnd.Next(9) == 0)
                        for (int y = y0 - 8; y < y0; y++) for (int x = x0 - 2; x < x0 + 41; x++) t.SetPixel(x, y, new Color(0.85f, 0.93f, 0.85f));
                }
            for (int linha = 0; linha < 12; linha++)
                for (int x = 6; x < 32; x++) { int y = hh - 1 - (30 + linha * 10); t.SetPixel(x, y, new Color(0.4f, 0.42f, 0.46f)); }
            t.Apply();
            File.WriteAllBytes(caminho, t.EncodeToPNG());
            Object.DestroyImmediate(t);
            AssetDatabase.ImportAsset(caminho);
        }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(caminho);
    }

    // Caixas nas prateleiras da estante de aço: caixas-arquivo de papelão (contabilidade) ou mercadoria colorida (loja).
    // As prateleiras do modelo (1,8 m de altura) ficam a 21, 59, 97 e 134 cm do chão.
    static readonly float[] AlturasPrateleira = { 0.215f, 0.59f, 0.97f, 1.345f };

    static void CaixasNaEstante(Transform sala, GameObject estante, bool arquivo)
    {
        if (estante == null) return;
        var pai = new GameObject(arquivo ? "Arquivo_Morto" : "Mercadoria").transform;
        pai.SetParent(estante.transform, false);
        var rnd = new System.Random(estante.transform.GetSiblingIndex() * 31 + (arquivo ? 1 : 2));
        Material[] cores =
        {
            Mat("Produto_Azul", new Color(0.18f, 0.35f, 0.65f), 0.35f), Mat("Produto_Vermelho", new Color(0.7f, 0.16f, 0.14f), 0.35f),
            Mat("Produto_Amarelo", new Color(0.92f, 0.74f, 0.18f), 0.35f), Mat("Produto_Verde", new Color(0.2f, 0.55f, 0.3f), 0.35f),
            Mat("Produto_Branco", new Color(0.92f, 0.92f, 0.9f), 0.35f), Mat("Produto_Laranja", new Color(0.9f, 0.45f, 0.12f), 0.35f),
        };
        var papelao = Mat("Caixa_Arquivo", new Color(0.62f, 0.48f, 0.32f), 0.15f);
        // Coordenadas locais da estante: a largura vai em x (−0,9 a 0,9 m) e a frente fica em +z
        foreach (float y in AlturasPrateleira)
        {
            float x = -0.82f;
            while (x < 0.78f)
            {
                float lw = arquivo ? 0.13f : 0.12f + (float)rnd.NextDouble() * 0.16f;
                float lh = arquivo ? 0.26f : 0.08f + (float)rnd.NextDouble() * 0.17f;
                float ld = arquivo ? 0.36f : 0.14f + (float)rnd.NextDouble() * 0.16f;
                if (x + lw > 0.82f) break;
                if (rnd.Next(arquivo ? 6 : 9) != 0)   // de vez em quando um vão
                {
                    var c = Caixa(pai, arquivo ? "Caixa_Arquivo" : "Produto", new Vector3(x + lw / 2f, y + lh / 2f + 0.003f, 0.02f),
                        new Vector3(lw, lh, ld), arquivo ? papelao : cores[rnd.Next(cores.Length)], 1f, false);
                    c.transform.localRotation = Quaternion.Euler(0, (float)rnd.NextDouble() * 6f - 3f, 0);
                }
                x += lw + 0.012f;
            }
        }
    }
}
