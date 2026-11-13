using UnityEngine;

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

        LuzTeto(casa, "Luz_Teto_Quarto", new Vector3(-2f, H - 0.15f, -1f), 1.3f, 6.5f, true);
        LuzTeto(casa, "Luz_Teto_Sala", new Vector3(2f, H - 0.15f, 0.5f), 1.2f, 6.5f, false);
        Sonda(casa, new Vector3(8f, H, 6f));

        // Caminho da calçada até a porta
        float zParede = CentroCasa.z + 3f + T;
        Caixa(casa, "Caminho", new Vector3(1.5f, 0.03f, (CalcadaSulZ1 + zParede) / 2f - CentroCasa.z),
            new Vector3(1.2f, 0.08f, CalcadaSulZ1 - zParede), mCalcada, 1.5f);
        Modelo("rugDoormat", casa, new Vector3(1.5f, 0.012f, 2.55f), 0, Colisao.Nenhuma);

        // Quarto (canto sudoeste)
        // 180°: a cabeceira encosta na parede sul, com o criado-mudo ao lado
        var cama = Modelo("bedDouble", casa, new Vector3(-2.7f, 0f, -1.75f), 180);
        var criado = Modelo("cabinetBedDrawerTable", casa, new Vector3(-1.05f, 0f, -2.65f), 0);
        Modelo("lampRoundTable", casa, new Vector3(-1.05f, Altura(criado), -2.65f), 0, Colisao.Nenhuma);
        Modelo("rugRounded", casa, new Vector3(-2.7f, 0.012f, 0.1f), 0, Colisao.Nenhuma);
        if (cama != null)
        {
            var c = cama.AddComponent<Cama>();
            c.distanciaInteracao = 2.0f;   // medido da base da cama até o centro do jogador
        }

        // Sala (canto sudeste): sofá olhando para a TV
        Modelo("loungeSofa", casa, new Vector3(2.4f, 0f, -2.45f), 0);
        var rack = Modelo("cabinetTelevision", casa, new Vector3(2.4f, 0f, -0.3f), 180);
        Modelo("televisionModern", casa, new Vector3(2.4f, Altura(rack), -0.3f), 180, Colisao.Nenhuma);
        Modelo("rugRectangle", casa, new Vector3(2.4f, 0.012f, -1.4f), 0, Colisao.Nenhuma);
        Modelo("pottedPlant", casa, new Vector3(3.55f, 0f, -2.6f), 0);

        // Cozinha (parede norte, a oeste da porta) e mesa
        Modelo("kitchenFridge", casa, new Vector3(-3.55f, 0f, 2.6f), 180);
        Modelo("kitchenCabinet", casa, new Vector3(-2.6f, 0f, 2.65f), 180);
        Modelo("kitchenSink", casa, new Vector3(-1.7f, 0f, 2.65f), 180);
        Modelo("kitchenStove", casa, new Vector3(-0.8f, 0f, 2.65f), 180);
        Modelo("tableRound", casa, new Vector3(-0.3f, 0f, 0.6f), 0);
        Modelo("chair", casa, new Vector3(-0.3f, 0f, 1.35f), 180);
        Modelo("chair", casa, new Vector3(-0.3f, 0f, -0.15f), 0);
        Modelo("coatRackStanding", casa, new Vector3(3.5f, 0f, 2.55f), 0);

        // Onde o jogador acorda (o DiaManager procura "SpawnCasa"): ao lado da cama, olhando para a porta
        Ponto("SpawnCasa", casa, new Vector3(0.2f, 1f, -1.2f), 0);
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
