using UnityEngine;

// Etapa 10: o access point do projeto de Wi-Fi é criado em código (como o rack e o notebook: não está na cena
// montada). O objeto fica no centro do piso da sala do cliente, com o giro da sala; as peças (caixa do kit no chão,
// forro, AP no teto) são filhos dele (EquipamentoPecas.WiFi). Coordenadas da sala: x de -5 a 5, z de -4 a 4
// (todas as salas de cliente têm 10 x 8 m, ver MontarCenario.Salas).
public static class AccessPointCliente
{
    public const float LarguraSala = 10f, ProfundidadeSala = 8f;
    public const float FolgaParede = 0.6f;          // o AP não vai colado na parede (o suporte precisa de espaço)

    static Equipamento atual;
    public static float AlturaForro { get; private set; } = 2.8f;   // altura do teto em relação ao piso
    public static int Versao { get; private set; }

    public static Equipamento Garantir()
    {
        if (atual != null) return atual;
        foreach (var e in Object.FindObjectsByType<Equipamento>())
            if (e.tipoEquipamento == TipoEquipamento.AccessPoint) return atual = e;
        var go = new GameObject("AccessPoint_Cliente");
        var eq = go.AddComponent<Equipamento>();
        eq.tipoEquipamento = TipoEquipamento.AccessPoint;
        eq.distanciaInteracao = 0.01f;   // só pelas peças (mirar + E): o centro da sala não abre nada
        go.AddComponent<EquipamentoPecas>();
        return atual = eq;
    }

    public static Transform Transform => atual != null ? atual.transform : null;

    // No centro do piso da sala, com o giro dela; mede o pé-direito pelo forro
    public static void Posicionar(SalaCliente sala)
    {
        var ap = Garantir();
        if (sala == null) return;
        Transform s = sala.sala != null ? sala.sala : sala.transform;
        ap.transform.SetPositionAndRotation(s.position, s.rotation);
        AlturaForro = 2.8f;
        foreach (var h in Physics.RaycastAll(s.position + Vector3.up * 0.5f, Vector3.up, 6f, ~0, QueryTriggerInteraction.Ignore))
            if (h.collider.name == "Forro") { AlturaForro = Mathf.Min(AlturaForro, h.point.y - s.position.y); }
        AlturaForro = Mathf.Clamp(AlturaForro, 2.3f, 3.5f);
        Versao++;
        Debug.Log($"[WiFi] Access point preparado na sala {sala.Nome} (forro a {AlturaForro:0.00} m).");
    }

    // ---------- Pontos da sala ----------

    // Ponto da sala (coordenadas locais) para o mundo
    public static Vector3 Mundo(float x, float y, float z)
    {
        var t = Transform;
        return t != null ? t.TransformPoint(new Vector3(x, y, z)) : new Vector3(x, y, z);
    }

    public static Vector3 Local(Vector3 mundo)
    {
        var t = Transform;
        return t != null ? t.InverseTransformPoint(mundo) : mundo;
    }

    // O ponto mirado no forro vira o lugar do AP (afastado das paredes)
    public static Vector3 AjustarNoForro(Vector3 mundo)
    {
        Vector3 l = Local(mundo);
        l.x = Mathf.Clamp(l.x, -LarguraSala / 2f + FolgaParede, LarguraSala / 2f - FolgaParede);
        l.z = Mathf.Clamp(l.z, -ProfundidadeSala / 2f + FolgaParede, ProfundidadeSala / 2f - FolgaParede);
        l.y = AlturaForro;
        return Mundo(l.x, l.y, l.z);
    }

    // A escada está aberta embaixo deste ponto? (fica no porta-malas do carro; aberta no chão com E)
    public const float DistanciaEscada = 1.4f;
    public static CargaFisica Escada()
    {
        foreach (var c in Object.FindObjectsByType<CargaFisica>())
            if (c.item.tipo == TipoCarga.Escada) return c;
        return null;
    }

    public static bool EscadaEmbaixo(Vector3 pontoTeto)
    {
        var c = Escada();
        if (c == null || CargaFisica.NaMao == c || c.noPortaMalas) return false;
        Vector3 d = c.transform.position - pontoTeto;
        d.y = 0f;
        return d.magnitude <= DistanciaEscada && Mathf.Abs(c.transform.position.y - (pontoTeto.y - AlturaForro)) < 0.6f;
    }

    // Onde fica a caixa do kit (no chão, ao lado do rack)
    public static Vector3 PosicaoCaixa()
    {
        var r = RackCliente.Transform;
        if (r != null) return r.TransformPoint(new Vector3(0.55f, 0f, -0.35f));
        return Mundo(0f, 0f, 0f);
    }
}

// Etapa 10: modelo de sinal do Wi-Fi (simplificado, mas com a cara do real): perda pela distância
// (40 dB no primeiro metro + 10·n·log10(d), n = 4 numa sala com gente e móveis) e perda pelos móveis no caminho
// (3 dB, 6 dB os de metal). Fontes: o roteador da operadora (antenas internas, numa mesinha baixa) e o AP no teto.
public static class SinalWiFi
{
    public const float TxRoteador = 9f, TxAp = 12f, Expoente = 4f;
    public const float AlturaMedicao = 1.2f;          // altura do celular na mão
    public const float SemSinal = -95f;

    public static float Perda(float distancia) => 40f + 10f * Expoente * Mathf.Log10(Mathf.Max(0.5f, distancia));

    public static float Calcular(float tx, float distancia, float perdaObstaculos) => tx - Perda(distancia) - perdaObstaculos;

    // Sinal (dBm) no ponto p, a melhor das fontes ligadas
    public static float Em(Vector3 p, EstadoEquipamentos e)
    {
        if (e == null) return SemSinal;
        float melhor = SemSinal;
        if (e.WiFiNoRoteador)
        {
            Transform rot = Roteador();
            if (rot != null)
            {
                Vector3 o = rot.position + Vector3.up * 0.12f;
                melhor = Mathf.Max(melhor, Calcular(TxRoteador, Vector3.Distance(o, p), Obstaculos(o, p)));
            }
        }
        if (e.wifiServico && e.ApLigado)
        {
            Vector3 o = e.apPos - Vector3.up * 0.06f;
            melhor = Mathf.Max(melhor, Calcular(TxAp, Vector3.Distance(o, p), Obstaculos(o, p)));
        }
        return melhor;
    }

    static Transform rotCache;
    static Transform Roteador()
    {
        if (rotCache != null) return rotCache;
        foreach (var eq in Object.FindObjectsByType<Equipamento>())
            if (eq.tipoEquipamento == TipoEquipamento.Roteador) return rotCache = eq.transform;
        return null;
    }

    static readonly RaycastHit[] hits = new RaycastHit[24];
    static readonly System.Collections.Generic.HashSet<Transform> vistos = new System.Collections.Generic.HashSet<Transform>();

    // Móveis entre a fonte e o ponto (cada móvel conta uma vez); paredes, piso e forro da própria sala não contam
    public static float Obstaculos(Vector3 de, Vector3 ate)
    {
        Vector3 d = ate - de;
        float dist = d.magnitude;
        if (dist < 0.05f) return 0f;
        int n = Physics.RaycastNonAlloc(de, d / dist, hits, dist, ~0, QueryTriggerInteraction.Ignore);
        vistos.Clear();
        float perda = 0f;
        for (int i = 0; i < n; i++)
        {
            var c = hits[i].collider;
            if (c == null || c is CharacterController || c.CompareTag("Player")) continue;
            if (c.GetComponentInParent<Equipamento>() != null) continue;
            string nome = c.name;
            if (nome == "Piso" || nome == "Forro" || nome.StartsWith("Parede")) continue;
            Transform raiz = c.attachedRigidbody != null ? c.attachedRigidbody.transform : c.transform;
            if (!vistos.Add(raiz)) continue;
            string l = (raiz.name + " " + nome).ToLowerInvariant();
            perda += l.Contains("metal") || l.Contains("steel") || l.Contains("shelves") || l.Contains("cabinet") || l.Contains("geladeira") ? 6f : 3f;
        }
        return Mathf.Min(perda, 18f);
    }

    public static string Qualidade(float dbm) =>
        dbm >= -55f ? "excelente" : dbm >= EstadoEquipamentos.MetaDbm ? "bom" : dbm >= -75f ? "fraco" : dbm > SemSinal + 1f ? "muito fraco" : "sem sinal";

    // Cor do mapa de calor
    public static Color Cor(float dbm) =>
        dbm >= -55f ? new Color(0.13f, 0.7f, 0.33f) :
        dbm >= EstadoEquipamentos.MetaDbm ? new Color(0.55f, 0.85f, 0.3f) :
        dbm >= -75f ? new Color(0.98f, 0.62f, 0.15f) : new Color(0.9f, 0.2f, 0.18f);
}
