using TMPro;
using UnityEngine;

// Etapa 10: medição do Wi-Fi (site survey) com o celular. O técnico liga a medição no app Wi-Fi e anda pela sala:
// cada quadrado de 1 m por onde passa é medido e aparece colorido no chão (verde bom, laranja fraco, vermelho
// muito fraco), como o mapa de calor do app. Medida a maior parte da sala (75% dos quadrados livres), o mapa vai
// para a OS (antes de instalar o AP é o "antes"; com o AP no teto, o "depois").
public class SurveyWiFi : MonoBehaviour
{
    public static SurveyWiFi Instance { get; private set; }
    public static bool Medindo => Instance != null && Instance.ativo;

    public const int Colunas = 10, Linhas = 8;      // quadrados de 1 m (salas de 10 x 8 m)

    bool ativo;
    float[] valores = new float[Colunas * Linhas];
    bool[] livres = new bool[Colunas * Linhas];
    int totalLivres, medidos;
    Transform raizLadrilhos, jogador;
    Renderer[] ladrilhos = new Renderer[Colunas * Linhas];
    Material[] matLadrilho = new Material[Colunas * Linhas];
    RectTransform painel;
    TextMeshProUGUI texto;
    float esconderEm = -1f, proximoTexto;

    public float[] Valores => valores;
    public bool[] Livres => livres;
    public float Progresso => totalLivres == 0 ? 0f : medidos / (float)totalLivres;

    public static SurveyWiFi Garantir()
    {
        if (Instance != null) return Instance;
        return new GameObject("SurveyWiFi").AddComponent<SurveyWiFi>();
    }

    void Awake()
    {
        Instance = this;
        var canvas = UIFactory.CriarCanvas("Survey_Canvas", 6);
        canvas.transform.SetParent(transform, false);
        painel = UIFactory.Painel(canvas.transform, "Survey", new Color(0.06f, 0.08f, 0.1f, 0.85f));
        UIFactory.Ancorar(painel, new Vector2(0f, 0f), new Vector2(24f, 160f), new Vector2(600f, 110f));
        texto = UIFactory.Texto(painel, "", 22, UIFactory.CorTexto);
        UIFactory.Esticar(texto.rectTransform, 12);
        painel.gameObject.SetActive(false);
    }

    void OnDestroy() { if (Instance == this) Instance = null; }

    static int Indice(int c, int l) => l * Colunas + c;
    public static Vector2 CentroLocal(int c, int l) => new Vector2(-AccessPointCliente.LarguraSala / 2f + 0.5f + c, -AccessPointCliente.ProfundidadeSala / 2f + 0.5f + l);

    // ---------- Começar / parar ----------

    public string Comecar()
    {
        var mm = MissionManager.Instance;
        var e = mm?.Estado;
        if (e == null || SalaCliente.Atual == null) return "A medição é feita na sala do cliente, durante um atendimento.";
        jogador = GameObject.FindWithTag("Player")?.transform;
        if (jogador == null || AccessPointCliente.Transform == null) return "Sem o técnico na cena.";
        if (!DentroDaSala(jogador.position, out _, out _)) return "Entre na sala do cliente para medir.";
        MapearLivres();
        for (int i = 0; i < valores.Length; i++) valores[i] = float.NaN;
        medidos = 0;
        ativo = true;
        esconderEm = -1f;
        CriarLadrilhos();
        painel.gameObject.SetActive(true);
        Debug.Log($"[WiFi] Medição começou: {totalLivres} quadrados livres na sala {SalaCliente.Atual.Nome}.");
        return $"Medição ligada: ande devagar pela sala, passando pelos cantos (o chão mostra o sinal de cada metro). Precisa medir {CoberturaTexto()} da sala.";
    }

    public void Parar(bool esconder = true)
    {
        ativo = false;
        if (painel != null) painel.gameObject.SetActive(false);
        if (esconder && raizLadrilhos != null) raizLadrilhos.gameObject.SetActive(false);
    }

    static string CoberturaTexto() => $"{EstadoEquipamentos.CoberturaSurvey * 100f:0}%";

    bool DentroDaSala(Vector3 mundo, out int c, out int l)
    {
        Vector3 p = AccessPointCliente.Local(mundo);
        c = Mathf.FloorToInt(p.x + AccessPointCliente.LarguraSala / 2f);
        l = Mathf.FloorToInt(p.z + AccessPointCliente.ProfundidadeSala / 2f);
        return c >= 0 && c < Colunas && l >= 0 && l < Linhas && p.y > -0.5f && p.y < 2.5f;
    }

    // Quadrado livre: dá para ficar em pé ali (sem móvel entre 0,5 e 1,4 m de altura)
    static readonly Collider[] cols = new Collider[16];
    void MapearLivres()
    {
        totalLivres = 0;
        var t = AccessPointCliente.Transform;
        for (int l = 0; l < Linhas; l++)
            for (int c = 0; c < Colunas; c++)
            {
                Vector2 cl = CentroLocal(c, l);
                Vector3 centro = t.TransformPoint(new Vector3(cl.x, 0.95f, cl.y));
                int n = Physics.OverlapBoxNonAlloc(centro, new Vector3(0.22f, 0.42f, 0.22f), cols, t.rotation, ~0, QueryTriggerInteraction.Ignore);
                bool livre = true;
                for (int i = 0; i < n; i++)
                {
                    var col = cols[i];
                    if (col is CharacterController || col.CompareTag("Player") || col.GetComponentInParent<PlayerController>() != null) continue;
                    if (col.GetComponentInParent<Animator>() != null) continue;   // gente sai do caminho
                    livre = false; break;
                }
                livres[Indice(c, l)] = livre;
                if (livre) totalLivres++;
            }
    }

    // ---------- Ladrilhos no chão ----------

    void CriarLadrilhos()
    {
        var t = AccessPointCliente.Transform;
        if (raizLadrilhos == null)
        {
            raizLadrilhos = new GameObject("MapaDeCalor").transform;
            var sh = Shader.Find("Sprites/Default");
            for (int l = 0; l < Linhas; l++)
                for (int c = 0; c < Colunas; c++)
                {
                    int i = Indice(c, l);
                    var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    q.name = $"Ladrilho_{c}_{l}";
                    Destroy(q.GetComponent<Collider>());
                    q.transform.SetParent(raizLadrilhos, false);
                    Vector2 cl = CentroLocal(c, l);
                    q.transform.localPosition = new Vector3(cl.x, 0.015f, cl.y);
                    q.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                    q.transform.localScale = new Vector3(0.92f, 0.92f, 1f);
                    var r = q.GetComponent<Renderer>();
                    r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    r.receiveShadows = false;
                    matLadrilho[i] = new Material(sh) { color = new Color(0f, 0f, 0f, 0f) };
                    r.sharedMaterial = matLadrilho[i];
                    ladrilhos[i] = r;
                }
        }
        raizLadrilhos.SetPositionAndRotation(t.position, t.rotation);
        raizLadrilhos.gameObject.SetActive(true);
        for (int i = 0; i < ladrilhos.Length; i++) ladrilhos[i].enabled = false;
    }

    // ---------- A cada quadro ----------

    void Update()
    {
        if (!ativo)
        {
            if (esconderEm > 0f && Time.time > esconderEm && raizLadrilhos != null) { raizLadrilhos.gameObject.SetActive(false); esconderEm = -1f; }
            return;
        }
        var e = MissionManager.Instance?.Estado;
        if (e == null || jogador == null) { Parar(); return; }

        bool dentro = DentroDaSala(jogador.position, out int c, out int l);
        float aqui = SinalWiFi.Em(new Vector3(jogador.position.x, AccessPointCliente.Mundo(0, 0, 0).y + SinalWiFi.AlturaMedicao, jogador.position.z), e);
        if (dentro)
        {
            int i = Indice(c, l);
            if (float.IsNaN(valores[i]))
            {
                if (!livres[i]) { livres[i] = true; totalLivres++; }   // o técnico chegou lá: é livre
                Vector2 cl = CentroLocal(c, l);
                Vector3 ponto = AccessPointCliente.Mundo(cl.x, SinalWiFi.AlturaMedicao, cl.y);
                valores[i] = SinalWiFi.Em(ponto, e);
                medidos++;
                var cor = SinalWiFi.Cor(valores[i]);
                cor.a = 0.45f;
                matLadrilho[i].color = cor;
                ladrilhos[i].enabled = true;
                SomManager.Tocar("clique", 0.15f);
            }
        }

        if (Time.time >= proximoTexto)
        {
            proximoTexto = Time.time + 0.2f;
            texto.text = $"<b>Wi-Fi · medindo a sala</b>   aqui: <b>{aqui:0} dBm</b> ({SinalWiFi.Qualidade(aqui)})\n" +
                         $"{medidos} de {totalLivres} quadrados ({Progresso * 100f:0}% de {CoberturaTexto()})  ·  meta: {EstadoEquipamentos.MetaDbm:0} dBm ou mais" +
                         (dentro ? "" : "\n<color=#F5B83F>Fora da sala do cliente</color>");
        }

        if (Progresso >= EstadoEquipamentos.CoberturaSurvey) Concluir(e);
    }

    void Concluir(EstadoEquipamentos e)
    {
        float pior = 0f;
        int fracos = 0;
        foreach (float v in valores)
        {
            if (float.IsNaN(v)) continue;
            pior = Mathf.Min(pior, v);
            if (v < EstadoEquipamentos.MetaDbm) fracos++;
        }
        var copia = (float[])valores.Clone();
        bool depois = e.apInstalado;
        string r = e.RegistrarSurvey(copia, pior, fracos, medidos);
        var tex = Imagem(copia, e);
        if (depois) e.fotoDepois = tex; else e.fotoAntes = tex;
        Parar(false);
        esconderEm = Time.time + 15f;
        MissionManager.Instance?.RegistrarAcao($"Medição do Wi-Fi ({(depois ? "depois" : "antes")}): pior ponto {pior:0} dBm, {fracos} abaixo da meta");
        MissionManager.Instance?.Verificar();
        GameUI.Instance?.Toast(r + " O mapa de calor foi para a OS.", fracos == 0 ? UIFactory.Sucesso : UIFactory.Alerta, 6f);
        Debug.Log("[WiFi] " + r);
    }

    // Atalho de teste: mede todos os quadrados livres sem andar
    public string MedirTudo()
    {
        string r = Comecar();
        if (!ativo) return r;
        var e = MissionManager.Instance.Estado;
        for (int l = 0; l < Linhas; l++)
            for (int c = 0; c < Colunas; c++)
            {
                int i = Indice(c, l);
                if (!livres[i]) continue;
                Vector2 cl = CentroLocal(c, l);
                valores[i] = SinalWiFi.Em(AccessPointCliente.Mundo(cl.x, SinalWiFi.AlturaMedicao, cl.y), e);
                medidos++;
                var cor = SinalWiFi.Cor(valores[i]); cor.a = 0.45f;
                matLadrilho[i].color = cor;
                ladrilhos[i].enabled = true;
            }
        Concluir(e);
        return "medido";
    }

    // ---------- Mapa de calor (imagem para a OS) ----------

    public static Texture2D Imagem(float[] v, EstadoEquipamentos e)
    {
        const int px = 32;
        var tex = new Texture2D(Colunas * px, Linhas * px, TextureFormat.RGB24, false) { name = "MapaDeCalor", filterMode = FilterMode.Point };
        var fundo = new Color(0.16f, 0.17f, 0.19f);
        var cores = new Color[tex.width * tex.height];
        for (int l = 0; l < Linhas; l++)
            for (int c = 0; c < Colunas; c++)
            {
                float val = v != null ? v[Indice(c, l)] : float.NaN;
                Color cor = float.IsNaN(val) ? fundo : SinalWiFi.Cor(val);
                for (int y = 0; y < px; y++)
                    for (int x = 0; x < px; x++)
                    {
                        bool borda = x == 0 || y == 0;
                        cores[(l * px + y) * tex.width + c * px + x] = borda ? cor * 0.75f : cor;
                    }
            }
        tex.SetPixels(cores);
        // Fontes: o roteador (branco) e o AP (azul)
        var rot = Object.FindObjectsByType<Equipamento>();
        foreach (var eq in rot)
            if (eq.tipoEquipamento == TipoEquipamento.Roteador) Marcar(tex, AccessPointCliente.Local(eq.transform.position), Color.white, px);
        if (e != null && e.apInstalado) Marcar(tex, AccessPointCliente.Local(e.apPos), new Color(0.25f, 0.6f, 1f), px);
        tex.Apply();
        return tex;
    }

    static void Marcar(Texture2D tex, Vector3 local, Color cor, int px)
    {
        int cx = Mathf.RoundToInt((local.x + AccessPointCliente.LarguraSala / 2f) * px);
        int cy = Mathf.RoundToInt((local.z + AccessPointCliente.ProfundidadeSala / 2f) * px);
        for (int y = -5; y <= 5; y++)
            for (int x = -5; x <= 5; x++)
                if (x * x + y * y <= 25)
                {
                    int X = Mathf.Clamp(cx + x, 0, tex.width - 1), Y = Mathf.Clamp(cy + y, 0, tex.height - 1);
                    tex.SetPixel(X, Y, x * x + y * y >= 16 ? Color.black : cor);
                }
    }
}
