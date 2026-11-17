using System.Collections.Generic;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Carro do técnico (Etapa 9C): hatch popular de 3 portas (modelo CC-BY "Milano '95", sem marca), montado pelo
// MontarCenario.Carro. Dirige-se de verdade, em 1ª pessoa, com WheelCollider (tração dianteira e câmbio automático):
//   W acelera · S freia e, parado, engata a ré · A/D volante · Espaço freio de mão · L faróis · H buzina · E descer.
// Entrar e sair com E na porta do motorista: a porta abre, o técnico senta (ou desce) e a porta bate.
// Combustível: o tanque gasta com a distância; a distância conta na escala do relógio do jogo (1 s = 1 min, então
// 1 m no bairro = 60 m na cidade). O hodômetro soma os km e, durante um chamado, os km e o tempo de deslocamento
// vão para a OS (cobrados no fechamento: CatalogoServicos.ValorKm). Posto: Cidade/PostoCombustivel.cs.
public class CarroTecnico : MonoBehaviour
{
    public static CarroTecnico Instance { get; private set; }
    public static bool NoCarro => Instance != null && Instance.dentro;

    // ---- Constantes do carro (um 1.0 popular) ----
    public const float EscalaDistancia = 60f;        // 1 m no bairro = 60 m na cidade (mesma escala do relógio)
    public const float KmPorLitro = 11f;
    public const float Tanque = 48f;                 // litros
    public const float LitrosJogoNovo = 20f;
    public const float Reserva = 5f;                 // luz da reserva acende abaixo disso
    const float VelMaxima = 60f / 3.6f;              // limitador: bairro residencial
    const float VelMaxRe = 15f / 3.6f;
    const float TorqueMotor = 520f;                  // por roda dianteira
    const float TorqueFreio = 2600f;
    const float TorqueFreioMao = 4500f;
    const float GiroMaximo = 33f;                    // graus nas rodas (manobra)
    const float VoltasVolante = 1.4f;                // de batente a batente: ~3 voltas no total → ±1,4 volta

    // ---- Montado pelo editor (MontarCenario.Carro) ----
    public WheelCollider[] rodas = new WheelCollider[4];      // FE, FD, TE, TD
    public Transform[] rodasVisuais = new Transform[4];
    public Transform volante;                // pivô: o eixo da coluna é o forward dele
    public Quaternion volanteBase = Quaternion.identity;
    public Transform porta, tampa;           // porta do motorista (dobradiça na frente) e tampa traseira (dobradiça em cima)
    public Vector3 portaAberta = new Vector3(0f, 62f, 0f), tampaAberta = new Vector3(78f, 0f, 0f);
    public Vector3 centroMassa = new Vector3(0f, 0.36f, 0.12f);   // baixo e um pouco à frente (motor na frente)
    public Transform pontoAssento, pontoSaida;
    public Light[] farois;
    public Renderer[] lampadasFarol, lampadasFreio, lampadasRe;

    // ---- Estado ----
    public float litros = LitrosJogoNovo;
    public float kmTotal;
    public bool faroisLigados;
    bool dentro, animando, motorLigado, freioDeMao = true;
    int marcha = 1;                          // 1 = D, -1 = R
    float acel, freio, giroInput, giroAtual;
    public float Velocidade { get; private set; }       // m/s na direção da frente (negativa = ré)
    public float KmH => Mathf.Abs(Velocidade) * 3.6f;
    public bool PortaAberta { get; private set; }
    public bool TampaAberta { get; private set; }
    public bool Animando => animando;

    // Entrada vinda de fora (piloto automático dos atalhos de teste); null = teclado
    public static System.Func<CarroTecnico, (float acel, float freio, float giro, bool freioMao)> PilotoAutomatico;

    Rigidbody rb;
    Transform jogador, cam;
    PlayerController pc;
    CharacterController cc;
    float alturaRaiz = 1f, olharYaw, olharPitch;
    AudioSource somMotor;
    MaterialPropertyBlock bloco;
    RectTransform hud;
    Light luzCabine;
    TextMeshProUGUI hudTexto, hudDicas;

    // ---------------- Ciclo ----------------

    void Awake()
    {
        Instance = this;
        rb = GetComponent<Rigidbody>();
        if (rb != null) { rb.automaticCenterOfMass = false; rb.centerOfMass = centroMassa; }
        bloco = new MaterialPropertyBlock();
        somMotor = gameObject.AddComponent<AudioSource>();
        somMotor.clip = SomManager.ClipDe("motor");
        somMotor.loop = true;
        somMotor.spatialBlend = 0.6f;
        somMotor.minDistance = 2f;
        somMotor.maxDistance = 30f;
        somMotor.volume = 0f;
        CriarHud();
        // Luz do dia entrando pelas janelas: o interior em sombra ficava preto (só acende com alguém dentro)
        luzCabine = new GameObject("Luz_Cabine").AddComponent<Light>();
        luzCabine.transform.SetParent(transform, false);
        luzCabine.transform.localPosition = new Vector3(0f, 1.25f, 0.1f);
        luzCabine.type = LightType.Point;
        luzCabine.range = 2.2f;
        luzCabine.intensity = 1.0f;
        luzCabine.color = new Color(0.95f, 0.97f, 1f);
        luzCabine.shadows = LightShadows.None;
        luzCabine.enabled = false;
        EstacionarFisica(true);
        Luzes();
    }

    void Start()
    {
        if (dadosSalvos != null) { Carregar(dadosSalvos); dadosSalvos = null; }
    }

    void OnDestroy() { if (Instance == this) Instance = null; }

    // Parado sem ninguém dentro: o corpo fica cinemático (não escorrega, não gasta física)
    void EstacionarFisica(bool parado)
    {
        if (rb == null) return;
        if (parado)
        {
            if (!rb.isKinematic) { rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero; }
            rb.isKinematic = true;
        }
        else rb.isKinematic = false;
    }

    // ---------------- Entrar e sair ----------------

    public string PodeEntrar()
    {
        if (animando) return "";
        if (CargaFisica.NaMao != null || UsadosFisicos.NaMao != null)
            return "Suas mãos estão ocupadas: guarde o que está segurando no porta-malas (ou largue no chão) antes de dirigir.";
        if (TampaAberta) return "O porta-malas está aberto: feche a tampa antes de sair com o carro.";
        return null;
    }

    public void Entrar()
    {
        string motivo = PodeEntrar();
        if (motivo != null) { if (motivo.Length > 0) GameUI.Instance?.Toast(motivo, UIFactory.Alerta, 3.5f); return; }
        var p = GameObject.FindWithTag("Player");
        if (p == null) return;
        jogador = p.transform;
        pc = p.GetComponent<PlayerController>();
        cc = p.GetComponent<CharacterController>();
        var c = p.GetComponentInChildren<Camera>();
        if (c == null) return;
        cam = c.transform;
        if (Physics.Raycast(jogador.position, Vector3.down, out var h, 3f, ~0, QueryTriggerInteraction.Ignore))
            alturaRaiz = Mathf.Clamp(jogador.position.y - h.point.y, 0.5f, 1.6f);

        animando = true;
        if (pc != null) pc.enabled = false;
        if (cc != null) cc.enabled = false;
        Vector3 olhoNoJogador = cam.position - jogador.position;
        Vector3 fora = pontoSaida.position + Vector3.up * alturaRaiz;
        Quaternion paraCarro = Quaternion.LookRotation(Vector3.ProjectOnPlane(transform.forward, Vector3.up));
        Vector3 sentado = pontoAssento.position - olhoNoJogador;

        SomManager.TocarEm("porta_carro", porta.position, 0.5f, 1.25f);   // trinco
        Sequence.Create()
            .Group(Tween.Position(jogador, fora, 0.35f, Ease.OutQuad))
            .Group(Tween.Rotation(jogador, paraCarro, 0.35f))
            .Chain(Tween.LocalRotation(porta, Quaternion.Euler(portaAberta), 0.45f, Ease.OutCubic))
            .Chain(Tween.Position(jogador, sentado, 0.6f, Ease.InOutQuad))
            .Group(Tween.LocalRotation(cam, Quaternion.Euler(8f, 0f, 0f), 0.6f))
            .Chain(Tween.LocalRotation(porta, Quaternion.identity, 0.3f, Ease.InQuad))
            .ChainCallback(this, s =>
            {
                SomManager.TocarEm("porta_carro", s.porta.position, 0.9f);
                s.jogador.SetParent(s.pontoAssento, true);
                s.olharYaw = 0f; s.olharPitch = 8f;
                s.dentro = true;
                s.animando = false;
                s.marcha = 1;
                s.EstacionarFisica(false);
                s.LigarMotor();
                s.hud.gameObject.SetActive(true);
                Debug.Log($"[Carro] Entrou no carro. Combustível {s.litros:0.0} L, hodômetro {s.kmTotal:0.0} km.");
            });
    }

    public void Sair()
    {
        if (!dentro || animando) return;
        if (KmH > 3f) { GameUI.Instance?.Toast("Pare o carro antes de descer.", UIFactory.Alerta, 2f); return; }
        animando = true;
        dentro = false;
        freioDeMao = true;
        acel = freio = 0f;
        DesligarMotor();
        hud.gameObject.SetActive(false);
        jogador.SetParent(null, true);
        // Não desce no meio de um poste ou parede: procura um lugar livre ao lado da porta
        Vector3 fora = pontoSaida.position + Vector3.up * alturaRaiz;
        for (int i = 0; i < 6 && Physics.CheckCapsule(fora + Vector3.down * 0.4f, fora + Vector3.up * 0.4f, 0.3f, ~0, QueryTriggerInteraction.Ignore); i++)
            fora += (i % 2 == 0 ? transform.forward : -transform.forward) * (0.5f * (i + 1));
        Quaternion paraFora = Quaternion.LookRotation(Vector3.ProjectOnPlane(-transform.right, Vector3.up));

        SomManager.TocarEm("porta_carro", porta.position, 0.5f, 1.25f);
        Sequence.Create()
            .Chain(Tween.LocalRotation(porta, Quaternion.Euler(portaAberta), 0.4f, Ease.OutCubic))
            .Chain(Tween.Position(jogador, fora, 0.6f, Ease.InOutQuad))
            .Group(Tween.Rotation(jogador, paraFora, 0.6f))
            .Group(Tween.LocalRotation(cam, Quaternion.identity, 0.6f))
            .Chain(Tween.LocalRotation(porta, Quaternion.identity, 0.3f, Ease.InQuad))
            .ChainCallback(this, s =>
            {
                SomManager.TocarEm("porta_carro", s.porta.position, 0.9f);
                if (s.cc != null) s.cc.enabled = true;
                if (s.pc != null) s.pc.enabled = true;
                s.animando = false;
                s.EstacionarFisica(true);
                s.Luzes();
                Debug.Log($"[Carro] Desceu do carro em {s.transform.position:F1} ({Bairro.RuaMaisProxima(s.transform.position)}).");
                s.AoDescer?.Invoke();
            });
    }

    public System.Action AoDescer;

    void LigarMotor()
    {
        if (litros <= 0f) { GameUI.Instance?.Toast("O motor não pega: o tanque está vazio. Traga um galão do posto.", UIFactory.Perigo, 4f); return; }
        motorLigado = true;
        SomManager.TocarEm("partida", transform.position, 0.8f);
        somMotor.PlayDelayed(0.6f);
    }

    void DesligarMotor()
    {
        motorLigado = false;
        somMotor.Stop();
    }

    // ---------------- Porta-malas (tampa traseira) ----------------

    public void AbrirTampa(bool abrir)
    {
        if (TampaAberta == abrir || tampa == null) return;
        TampaAberta = abrir;
        PortaMalas.Instance?.AtualizarConteudo();
        Tween.StopAll(tampa);
        if (abrir) Tween.LocalRotation(tampa, Quaternion.Euler(tampaAberta), 0.9f, Ease.OutCubic);
        else Tween.LocalRotation(tampa, Quaternion.identity, 0.5f, Ease.InQuad).OnComplete(this, s => SomManager.TocarEm("porta_malas", s.tampa.position, 0.9f));
        SomManager.TocarEm(abrir ? "clique" : "mexer", tampa.position, 0.6f, abrir ? 0.7f : 0.8f);
    }

    // ---------------- Direção ----------------

    void Update()
    {
        if (!dentro || animando) return;
        bool painel = GameUI.Instance != null && GameUI.Instance.PainelAberto;

        if (PilotoAutomatico != null)
        {
            var e = PilotoAutomatico(this);
            acel = e.acel; freio = e.freio; giroInput = e.giro; freioDeMao = e.freioMao;
        }
        else if (!painel)
        {
            float w = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow) ? 1f : 0f;
            float s = Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow) ? 1f : 0f;
            giroInput = (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow) ? 1f : 0f) - (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow) ? 1f : 0f);
            // Câmbio automático: S freia; parado, engata a ré. Na ré, W freia; parado, volta para o D.
            if (marcha > 0)
            {
                acel = w; freio = s;
                if (s > 0f && w == 0f && Velocidade < 0.4f) marcha = -1;
            }
            else
            {
                acel = s; freio = w;
                if (w > 0f && s == 0f && Velocidade > -0.4f) marcha = 1;
            }
            if (Input.GetKeyDown(KeyCode.Space)) { freioDeMao = !freioDeMao; SomManager.TocarEm("freio_mao", transform.position, 0.5f); }
            if ((w > 0f || s > 0f) && freioDeMao && KmH < 1f) { freioDeMao = false; SomManager.TocarEm("freio_mao", transform.position, 0.4f, 1.2f); }
            if (Input.GetKeyDown(KeyCode.L)) { faroisLigados = !faroisLigados; SomManager.Tocar("clique", 0.5f); }
            if (Input.GetKeyDown(KeyCode.H)) SomManager.TocarEm("buzina", transform.position, 1f);
            if (Input.GetKeyDown(KeyCode.E)) { Sair(); return; }
            OlharEmVolta();
        }
        else { acel = 0f; freio = 0.3f; giroInput = 0f; }

        if (!motorLigado) acel = 0f;
        giroAtual = Mathf.MoveTowards(giroAtual, giroInput, Time.deltaTime * (giroInput == 0f ? 3f : 2.2f));
        if (volante != null)
            volante.localRotation = volanteBase * Quaternion.Euler(0f, 0f, -giroAtual * VoltasVolante * 360f);

        // Som do motor: rotação sobe com a velocidade e com o pé no acelerador
        if (somMotor.isPlaying)
        {
            float rot = 0.9f + Mathf.Repeat(KmH, 22f) / 22f * 0.9f * Mathf.Clamp01(KmH / 6f) + acel * 0.35f;
            somMotor.pitch = Mathf.Lerp(somMotor.pitch, rot, Time.deltaTime * 4f);
            somMotor.volume = 0.35f * Configuracoes.VolumeEfeitos;
        }
        Luzes();
        AtualizarHud();
    }

    // Olhar em volta sentado (retrovisor, janela): mouse gira a cabeça com limite
    void OlharEmVolta()
    {
        float sens = Configuracoes.Sensibilidade;
        olharYaw = Mathf.Clamp(olharYaw + Input.GetAxis("Mouse X") * sens, -120f, 120f);
        olharPitch = Mathf.Clamp(olharPitch - Input.GetAxis("Mouse Y") * sens * (Configuracoes.InverterY ? -1f : 1f), -50f, 60f);
        jogador.localRotation = Quaternion.Euler(0f, olharYaw, 0f);
        cam.localRotation = Quaternion.Euler(olharPitch, 0f, 0f);
    }

    void FixedUpdate()
    {
        if (rb == null || rb.isKinematic) { Velocidade = 0f; return; }
        Velocidade = Vector3.Dot(rb.linearVelocity, transform.forward);
        bool semGasolina = litros <= 0f;
        if (semGasolina && motorLigado) { DesligarMotor(); GameUI.Instance?.Toast("O motor apagou: <b>acabou a gasolina</b>. Busque um galão no posto.", UIFactory.Perigo, 5f); }

        float vel = Mathf.Abs(Velocidade);
        float limite = marcha > 0 ? VelMaxima : VelMaxRe;
        float torque = motorLigado ? acel * TorqueMotor * marcha * Mathf.Clamp01((limite - vel) / 1.5f) : 0f;
        float esterco = giroAtual * Mathf.Lerp(GiroMaximo, 11f, vel / VelMaxima);
        float freioTotal = freio * TorqueFreio;
        if (!dentro || (acel == 0f && freio == 0f && vel < 0.25f)) freioTotal = Mathf.Max(freioTotal, 400f);   // segura parado
        for (int i = 0; i < 4; i++)
        {
            var r = rodas[i];
            if (r == null) continue;
            bool frente = i < 2;
            r.motorTorque = frente ? torque : 0f;
            r.steerAngle = frente ? esterco : 0f;
            r.brakeTorque = freioTotal + (!frente && freioDeMao ? TorqueFreioMao : 0f);
            r.GetWorldPose(out var pos, out var rot);
            if (rodasVisuais[i] != null) rodasVisuais[i].SetPositionAndRotation(pos, rot);
        }
        AntiRolagem(0, 1);
        AntiRolagem(2, 3);
        rb.AddForce(-transform.up * vel * vel * 3f);   // um pouco de pressão aerodinâmica (estabilidade)

        // Distância, combustível, hodômetro e o deslocamento do chamado
        if (vel > 0.2f && rodas[0] != null && rodas[0].isGrounded)
        {
            float km = vel * Time.fixedDeltaTime * EscalaDistancia / 1000f;
            kmTotal += km;
            litros = Mathf.Max(0f, litros - km / KmPorLitro * (acel > 0.5f ? 1.1f : 1f));
            var os = MissionManager.Instance != null ? MissionManager.Instance.chamadoAtivo?.os : null;
            if (os != null)
            {
                os.kmRodados += km;
                os.minutosDeslocamento += Time.fixedDeltaTime * (DiaManager.Instance != null ? DiaManager.Instance.minutosPorSegundo : 1f);
            }
            if (litros < Reserva && litros + km / KmPorLitro >= Reserva)
                GameUI.Instance?.Toast("Acendeu a <b>luz da reserva</b>: passe no posto (Rua dos Jacarandás, 340).", UIFactory.Alerta, 5f);
        }
    }

    // Barra estabilizadora: transfere força de uma roda para a outra do mesmo eixo nas curvas
    void AntiRolagem(int a, int b)
    {
        var ra = rodas[a]; var rb2 = rodas[b];
        if (ra == null || rb2 == null) return;
        const float rigidez = 9000f;
        float ca = 1f, cb = 1f;
        bool ga = ra.GetGroundHit(out var ha), gb = rb2.GetGroundHit(out var hb);
        if (ga) ca = (-ra.transform.InverseTransformPoint(ha.point).y - ra.radius) / ra.suspensionDistance;
        if (gb) cb = (-rb2.transform.InverseTransformPoint(hb.point).y - rb2.radius) / rb2.suspensionDistance;
        float f = (ca - cb) * rigidez;
        if (ga) rb.AddForceAtPosition(ra.transform.up * -f, ra.transform.position);
        if (gb) rb.AddForceAtPosition(rb2.transform.up * f, rb2.transform.position);
    }

    // ---------------- Luzes ----------------

    void Luzes()
    {
        bool farol = faroisLigados && (dentro || animando);
        if (luzCabine != null) luzCabine.enabled = dentro && (DiaManager.Instance == null || !DiaManager.Instance.EhNoite);
        foreach (var l in farois) if (l != null) l.enabled = farol;
        Emitir(lampadasFarol, farol ? new Color(1f, 0.95f, 0.8f) * 2.2f : Color.black);
        Emitir(lampadasFreio, dentro && (freio > 0.05f || (marcha < 0 && acel > 0f)) ? new Color(1f, 0.05f, 0.03f) * 3f
                              : farol ? new Color(0.6f, 0.02f, 0.01f) : Color.black);
        Emitir(lampadasRe, dentro && marcha < 0 ? Color.white * 2f : Color.black);
    }

    void Emitir(Renderer[] rs, Color c)
    {
        if (rs == null) return;
        foreach (var r in rs)
        {
            if (r == null) continue;
            r.GetPropertyBlock(bloco);
            bloco.SetColor("_EmissionColor", c);
            r.SetPropertyBlock(bloco);
        }
    }

    // ---------------- Painel (HUD) ----------------

    void CriarHud()
    {
        var canvas = UIFactory.CriarCanvas("Carro_Canvas", 6);
        canvas.transform.SetParent(transform, false);
        hud = UIFactory.Painel(canvas.transform, "Painel", new Color(0.05f, 0.06f, 0.08f, 0.78f));
        UIFactory.Ancorar(hud, new Vector2(0.5f, 0f), new Vector2(0f, 24f), new Vector2(760f, 92f));
        hud.GetComponent<Image>().raycastTarget = false;
        hudTexto = UIFactory.Texto(hud, "", 26, UIFactory.CorTexto, FontStyles.Normal, TextAlignmentOptions.Center);
        hudTexto.rectTransform.anchorMin = new Vector2(0f, 0.4f); hudTexto.rectTransform.anchorMax = Vector2.one;
        hudTexto.rectTransform.offsetMin = hudTexto.rectTransform.offsetMax = Vector2.zero;
        hudTexto.raycastTarget = false;
        hudDicas = UIFactory.Texto(hud, "W/S acelerar e frear (parado: ré) · A/D volante · Espaço freio de mão · L faróis · H buzina · E descer",
            15, UIFactory.CorSuave, FontStyles.Normal, TextAlignmentOptions.Center);
        hudDicas.rectTransform.anchorMin = Vector2.zero; hudDicas.rectTransform.anchorMax = new Vector2(1f, 0.4f);
        hudDicas.rectTransform.offsetMin = hudDicas.rectTransform.offsetMax = Vector2.zero;
        hudDicas.raycastTarget = false;
        hud.gameObject.SetActive(false);
    }

    void AtualizarHud()
    {
        int barras = Mathf.RoundToInt(litros / Tanque * 10f);
        string tanque = new string('|', barras) + "<color=#4A5260>" + new string('|', 10 - barras) + "</color>";
        string cor = litros < Reserva ? "#F2545B" : "#E8ECF2";
        hudTexto.text = $"<b>{KmH:0}</b> km/h   <b>{(marcha > 0 ? "D" : "<color=#F5B83F>R</color>")}</b>" +
                        (freioDeMao ? "   <color=#F2545B>(P) freio de mão</color>" : "") +
                        (faroisLigados ? "   <color=#3FA9F5>faróis</color>" : "") +
                        $"   <color={cor}>combustível [{tanque}] {litros:0.0} L</color>" +
                        $"   <color=#9AA6B8>{kmTotal:0} km</color>";
    }

    // ---------------- Combustível ----------------

    public float FaltaParaEncher => Mathf.Max(0f, Tanque - litros);

    public void Abastecer(float l)
    {
        litros = Mathf.Min(Tanque, litros + Mathf.Max(0f, l));
    }

    // ---------------- Save ----------------

    [System.Serializable]
    public class Dados
    {
        public bool salvo;
        public Vector3 posicao;
        public float giro;
        public float litros = LitrosJogoNovo;
        public float km;
        public bool farois;
        public bool tampaAberta;
        public List<ItemCarga> carga = new List<ItemCarga>();
    }

    public static Dados dadosSalvos;

    public static Dados Exportar()
    {
        var c = Instance;
        if (c == null) return dadosSalvos;
        return new Dados
        {
            salvo = true,
            posicao = c.transform.position,
            giro = c.transform.eulerAngles.y,
            litros = c.litros,
            km = c.kmTotal,
            farois = c.faroisLigados,
            carga = PortaMalas.Instance != null ? PortaMalas.Instance.Exportar() : new List<ItemCarga>(),
        };
    }

    public static void CarregarSalvo(Dados d)
    {
        if (Instance != null) Instance.Carregar(d);
        else dadosSalvos = d;
    }

    // Save antigo (sem carro) ou jogo novo: carro na vaga de casa, com 20 L
    void Carregar(Dados d)
    {
        if (dentro) return;
        if (d == null || !d.salvo)
        {
            var v = Bairro.VagaDe(TipoEndereco.Casa);
            Posicionar(v.centro, v.giro);
            litros = d != null && d.litros > 0f ? d.litros : LitrosJogoNovo;
            kmTotal = 0f;
            faroisLigados = false;
            PortaMalas.Instance?.Carregar(null);
            return;
        }
        Posicionar(d.posicao, d.giro);
        litros = Mathf.Clamp(d.litros, 0f, Tanque);
        kmTotal = Mathf.Max(0f, d.km);
        faroisLigados = d.farois;
        PortaMalas.Instance?.Carregar(d.carga);
        Luzes();
        Debug.Log($"[Carro] Carregado em {d.posicao:F1}: {litros:0.0} L, {kmTotal:0.0} km, {d.carga?.Count ?? 0} volume(s) no porta-malas.");
    }

    public void Posicionar(Vector3 pos, float giro)
    {
        EstacionarFisica(true);
        transform.SetPositionAndRotation(new Vector3(pos.x, Mathf.Max(pos.y, 0f) + 0.02f, pos.z), Quaternion.Euler(0f, giro, 0f));
        Physics.SyncTransforms();
        for (int i = 0; i < 4; i++)
            if (rodas[i] != null && rodasVisuais[i] != null)
            {
                rodas[i].GetWorldPose(out var p, out var r);
                rodasVisuais[i].SetPositionAndRotation(p, r);
            }
    }

    public static void NovoJogo() => CarregarSalvo(new Dados());
}
