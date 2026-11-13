using System.Collections;
using System.Collections.Generic;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Interface do jogo (substitui os antigos OnGUI): HUD, chamado ativo, avisos de interação,
// notificações, tablet com abas, painel de diagnóstico e menu de pausa.
// É criada sozinha em qualquer cena que tenha um MissionManager.
public partial class GameUI : MonoBehaviour
{
    public static GameUI Instance;

    enum Modal { Nenhum, Tablet, Manutencao, Pausa, Conversa, Resumo, Computador, Crimpagem, Bancada }
    Modal modal = Modal.Nenhum;
    public bool PainelAberto => modal != Modal.Nenhum;

    // HUD
    TextMeshProUGUI txtSaldo, txtReputacao, txtFase;
    RectTransform barraReputacao;
    Image barraReputacaoImg;
    RectTransform hud;
    float saldoMostrado;
    Tween tweenSaldo;

    // Chamado ativo
    RectTransform cardChamado;
    TextMeshProUGUI txtChCliente, txtChProblema, txtChEquip, txtChTempo, txtChPistas;
    RectTransform barraTempo;
    Image barraTempoImg;

    // Aviso de interação e notificações
    RectTransform prompt;
    TextMeshProUGUI txtPrompt;
    RectTransform areaToasts;

    // Modais
    RectTransform overlay;
    CanvasGroup overlayGrupo;
    RectTransform tablet, pausa;
    readonly List<RectTransform> janelas = new List<RectTransform>();   // todas as janelas modais
    RectTransform tabletConteudo;
    Button abaChamados, abaEquipe, abaLoja, abaEmpresa, abaContratos;
    int abaAtiva;
    TextMeshProUGUI txtTabletInfo;

    EventBinding<SaldoAlteradoEvent> bSaldo;
    EventBinding<ReputacaoAlteradaEvent> bRep;
    EventBinding<FaseDesbloqueadaEvent> bFase;
    EventBinding<ChamadoAceitoEvent> bAceito;
    EventBinding<ChamadoConcluidoEvent> bConcluido;
    EventBinding<AcaoErradaEvent> bErro;
    EventBinding<FuncionarioContratadoEvent> bContratado;

    // ---------- Criação automática ----------

    static bool registrado;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (!registrado)
        {
            SceneManager.sceneLoaded += (_, __) => CriarSePrecisar();
            registrado = true;
        }
        CriarSePrecisar();
    }

    static void CriarSePrecisar()
    {
        if (Instance != null) return;
        if (FindAnyObjectByType<MissionManager>() == null) return;
        new GameObject("GameUI").AddComponent<GameUI>();
    }

    // ---------- Ciclo de vida ----------

    void Awake()
    {
        Instance = this;
        Time.timeScale = 1f;
        Construir();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        Time.timeScale = 1f;
    }

    void OnEnable()
    {
        bSaldo = new EventBinding<SaldoAlteradoEvent>(AoMudarSaldo);
        bRep = new EventBinding<ReputacaoAlteradaEvent>(e => AtualizarReputacao(e.reputacao, true));
        bFase = new EventBinding<FaseDesbloqueadaEvent>(AoDesbloquearFase);
        bAceito = new EventBinding<ChamadoAceitoEvent>(e =>
        {
            ClienteNPC.Criar(e.chamado);
            Toast(e.chamado.EquipamentoConhecido
                ? $"Chamado aceito! Converse com <b>{e.chamado.contato}</b> ou vá direto ao(à) <b>{e.chamado.EquipamentoTexto()}</b>."
                : $"Chamado aceito! <b>{e.chamado.contato}</b> não soube explicar direito: converse para descobrir o problema.",
                UIFactory.Destaque, 3.5f);
        });
        bConcluido = new EventBinding<ChamadoConcluidoEvent>(AoConcluirChamado);
        bErro = new EventBinding<AcaoErradaEvent>(e =>
            Toast($"{e.acao} (-{e.penalidade:F0} reputação)", UIFactory.Perigo, 4f));
        bContratado = new EventBinding<FuncionarioContratadoEvent>(e =>
            { Toast($"{e.funcionario.nome} ({e.funcionario.NivelTexto()}) entrou na equipe!", UIFactory.Sucesso); AtualizarTablet(); });

        EventBus<SaldoAlteradoEvent>.Register(bSaldo);
        EventBus<ReputacaoAlteradaEvent>.Register(bRep);
        EventBus<FaseDesbloqueadaEvent>.Register(bFase);
        EventBus<ChamadoAceitoEvent>.Register(bAceito);
        EventBus<ChamadoConcluidoEvent>.Register(bConcluido);
        EventBus<AcaoErradaEvent>.Register(bErro);
        EventBus<FuncionarioContratadoEvent>.Register(bContratado);
        RegistrarEventosDia();
    }

    void OnDisable()
    {
        EventBus<SaldoAlteradoEvent>.Deregister(bSaldo);
        EventBus<ReputacaoAlteradaEvent>.Deregister(bRep);
        EventBus<FaseDesbloqueadaEvent>.Deregister(bFase);
        EventBus<ChamadoAceitoEvent>.Deregister(bAceito);
        EventBus<ChamadoConcluidoEvent>.Deregister(bConcluido);
        EventBus<AcaoErradaEvent>.Deregister(bErro);
        EventBus<FuncionarioContratadoEvent>.Deregister(bContratado);
        RemoverEventosDia();
    }

    void Start()
    {
        saldoMostrado = GameEconomy.Instance != null ? GameEconomy.Instance.saldo : 0f;
        txtSaldo.text = UIFactory.Dinheiro(saldoMostrado);
        AtualizarReputacao(ReputationManager.Instance != null ? ReputationManager.Instance.GetReputacao() : 50f, false);
        Toast("Aperte <b>Tab</b> para abrir o tablet e ver os chamados.", UIFactory.Destaque, 4f);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            if (modal == Modal.Tablet) FecharModal();
            else if (modal == Modal.Nenhum) AbrirTablet();
        }

        if (Input.GetKeyDown(KeyCode.Escape) && modal != Modal.Resumo)   // o resumo do dia só fecha pelo botão
        {
            if (modal != Modal.Nenhum) FecharModal();
            else AbrirPausa();
        }

        if (modal == Modal.Nenhum && Input.GetKeyDown(KeyCode.E))
            Interactable.MaisProximoEmAlcance()?.Interagir();

        AtualizarPrompt();
        AtualizarCardChamado();
        AtualizarRelogio();
        AtualizarComputador();
    }

    // ---------- Montagem da interface ----------

    void Construir()
    {
        var canvas = UIFactory.CriarCanvas("Canvas_Jogo", 10);
        canvas.transform.SetParent(transform, false);
        var raiz = (RectTransform)canvas.transform;

        // Mira
        var mira = UIFactory.Painel(raiz, "Mira", new Color(1, 1, 1, 0.8f));
        UIFactory.Ancorar(mira, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(6, 6));
        mira.pivot = new Vector2(0.5f, 0.5f);

        ConstruirHud(raiz);
        ConstruirCardChamado(raiz);

        // Aviso de interação
        prompt = UIFactory.Painel(raiz, "Prompt", UIFactory.Fundo);
        UIFactory.Ancorar(prompt, new Vector2(0.5f, 0f), new Vector2(0, 140), new Vector2(560, 56));
        txtPrompt = UIFactory.Texto(prompt, "", 24, UIFactory.CorTexto, FontStyles.Bold, TextAlignmentOptions.Center);
        UIFactory.Esticar(txtPrompt.rectTransform);
        prompt.gameObject.SetActive(false);

        // Notificações
        areaToasts = UIFactory.Vazio(raiz, "Notificacoes");
        UIFactory.Ancorar(areaToasts, new Vector2(0.5f, 1f), new Vector2(0, -30), new Vector2(760, 400));
        UIFactory.Vertical(areaToasts, 10, 0, TextAnchor.UpperCenter);

        // Fundo escuro dos painéis
        overlay = UIFactory.Painel(raiz, "Overlay", UIFactory.Overlay);
        UIFactory.Esticar(overlay);
        overlayGrupo = overlay.gameObject.AddComponent<CanvasGroup>();

        ConstruirTablet(overlay);
        ConstruirManutencao(overlay);
        ConstruirComputador(overlay);
        ConstruirPausa(overlay);
        ConstruirConversa(overlay);
        ConstruirResumo(overlay, raiz);
        overlay.gameObject.SetActive(false);
    }

    void ConstruirHud(RectTransform raiz)
    {
        hud = UIFactory.Painel(raiz, "HUD", UIFactory.Fundo);
        UIFactory.Ancorar(hud, new Vector2(0, 1), new Vector2(24, -24), new Vector2(400, 0));
        UIFactory.Vertical(hud, 6, 20);
        UIFactory.AjustarAoConteudo(hud);

        UIFactory.Texto(hud, "SALDO", 16, UIFactory.CorSuave, FontStyles.Bold);
        txtSaldo = UIFactory.Texto(hud, "R$ 0,00", 40, UIFactory.Sucesso, FontStyles.Bold);

        txtReputacao = UIFactory.Texto(hud, "Reputação 50/100", 20, UIFactory.CorTexto);
        var fundoBarra = UIFactory.Painel(hud, "BarraFundo", UIFactory.CartaoClaro);
        fundoBarra.gameObject.AddComponent<LayoutElement>().preferredHeight = 12;
        barraReputacao = UIFactory.Painel(fundoBarra, "Barra", UIFactory.Destaque);
        barraReputacaoImg = barraReputacao.GetComponent<Image>();
        barraReputacao.anchorMin = Vector2.zero;
        barraReputacao.anchorMax = new Vector2(0.5f, 1f);
        barraReputacao.offsetMin = barraReputacao.offsetMax = Vector2.zero;

        txtFase = UIFactory.Texto(hud, "Fase 1 · Técnico solo", 18, UIFactory.CorSuave);
        ConstruirRelogio(hud);
    }

    void ConstruirCardChamado(RectTransform raiz)
    {
        cardChamado = UIFactory.Painel(raiz, "ChamadoAtivo", UIFactory.Fundo);
        UIFactory.Ancorar(cardChamado, new Vector2(0, 1), new Vector2(24, -250), new Vector2(400, 0));
        UIFactory.Vertical(cardChamado, 6, 20);
        UIFactory.AjustarAoConteudo(cardChamado);

        UIFactory.Texto(cardChamado, "CHAMADO EM ATENDIMENTO", 16, UIFactory.Destaque, FontStyles.Bold);
        txtChCliente = UIFactory.Texto(cardChamado, "", 24, UIFactory.CorTexto, FontStyles.Bold);
        txtChProblema = UIFactory.Texto(cardChamado, "", 19, UIFactory.CorTexto);
        txtChEquip = UIFactory.Texto(cardChamado, "", 18, UIFactory.CorSuave);
        txtChPistas = UIFactory.Texto(cardChamado, "", 17, UIFactory.CorSuave);
        txtChTempo = UIFactory.Texto(cardChamado, "", 20, UIFactory.CorTexto, FontStyles.Bold);
        var fundoBarra = UIFactory.Painel(cardChamado, "TempoFundo", UIFactory.CartaoClaro);
        fundoBarra.gameObject.AddComponent<LayoutElement>().preferredHeight = 8;
        barraTempo = UIFactory.Painel(fundoBarra, "Tempo", UIFactory.Sucesso);
        barraTempoImg = barraTempo.GetComponent<Image>();
        barraTempo.anchorMin = Vector2.zero;
        barraTempo.anchorMax = Vector2.one;
        barraTempo.offsetMin = barraTempo.offsetMax = Vector2.zero;

        cardChamado.gameObject.SetActive(false);
    }

    RectTransform JanelaModal(RectTransform pai, string nome, Vector2 tamanho)
    {
        var janela = UIFactory.Painel(pai, nome, UIFactory.Fundo);
        UIFactory.Ancorar(janela, new Vector2(0.5f, 0.5f), Vector2.zero, tamanho);
        janela.pivot = new Vector2(0.5f, 0.5f);
        janela.gameObject.SetActive(false);
        janelas.Add(janela);
        return janela;
    }

    void ConstruirTablet(RectTransform pai)
    {
        tablet = JanelaModal(pai, "Tablet", new Vector2(1040, 720));

        // Cabeçalho
        var cab = UIFactory.Vazio(tablet, "Cabecalho");
        cab.anchorMin = new Vector2(0, 1); cab.anchorMax = new Vector2(1, 1);
        cab.pivot = new Vector2(0.5f, 1);
        cab.offsetMin = new Vector2(32, -84); cab.offsetMax = new Vector2(-32, -24);
        UIFactory.Horizontal(cab, 16);
        var titulo = UIFactory.Texto(cab, "TABLET · TI Service", 34, UIFactory.CorTexto, FontStyles.Bold);
        titulo.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
        txtTabletInfo = UIFactory.Texto(cab, "", 20, UIFactory.CorSuave, FontStyles.Normal, TextAlignmentOptions.Right);
        txtTabletInfo.gameObject.AddComponent<LayoutElement>().preferredWidth = 360;
        UIFactory.Botao(cab, "X", FecharModal, UIFactory.CartaoClaro, 56, 56, 26);

        // Abas
        var abas = UIFactory.Vazio(tablet, "Abas");
        abas.anchorMin = new Vector2(0, 1); abas.anchorMax = new Vector2(1, 1);
        abas.pivot = new Vector2(0.5f, 1);
        abas.offsetMin = new Vector2(32, -152); abas.offsetMax = new Vector2(-32, -100);
        UIFactory.Horizontal(abas, 10);
        abaChamados = UIFactory.Botao(abas, "Chamados", () => TrocarAba(0), UIFactory.Destaque, 170, 52, 22);
        abaEquipe = UIFactory.Botao(abas, "Equipe", () => TrocarAba(1), UIFactory.CartaoClaro, 150, 52, 22);
        abaLoja = UIFactory.Botao(abas, "Loja e mochila", () => TrocarAba(2), UIFactory.CartaoClaro, 210, 52, 22);
        abaEmpresa = UIFactory.Botao(abas, "Empresa", () => TrocarAba(3), UIFactory.CartaoClaro, 160, 52, 22);
        abaContratos = UIFactory.Botao(abas, "Contratos", () => TrocarAba(4), UIFactory.CartaoClaro, 180, 52, 22);

        // Conteúdo com rolagem
        var area = UIFactory.Vazio(tablet, "Area");
        area.anchorMin = Vector2.zero; area.anchorMax = Vector2.one;
        area.offsetMin = new Vector2(32, 28); area.offsetMax = new Vector2(-32, -170);
        var lista = UIFactory.ListaComRolagem(area, "Lista");
        UIFactory.Esticar(lista.parent.parent as RectTransform);
        tabletConteudo = lista;
    }

    void ConstruirPausa(RectTransform pai)
    {
        pausa = JanelaModal(pai, "Pausa", new Vector2(460, 0));
        UIFactory.Vertical(pausa, 14, 36, TextAnchor.UpperCenter);
        UIFactory.AjustarAoConteudo(pausa);

        UIFactory.Texto(pausa, "Pausado", 40, UIFactory.CorTexto, FontStyles.Bold, TextAlignmentOptions.Center);
        UIFactory.Botao(pausa, "Continuar", FecharModal, UIFactory.Destaque, 380, 56);
        UIFactory.Botao(pausa, "Salvar jogo", () =>
        {
            SaveManager.Instance?.Salvar();
            Toast("Jogo salvo!", UIFactory.Sucesso);
        }, UIFactory.CartaoClaro, 380, 56);
        UIFactory.Botao(pausa, "Salvar e voltar ao menu", () =>
        {
            SaveManager.Instance?.Salvar();
            Time.timeScale = 1f;
            SceneManager.LoadScene(0);
        }, UIFactory.CartaoClaro, 380, 56);
        UIFactory.Botao(pausa, "Sair do jogo", () =>
        {
            SaveManager.Instance?.Salvar();
            Application.Quit();
        }, UIFactory.Perigo, 380, 56);
    }

    // ---------- HUD ----------

    void AoMudarSaldo(SaldoAlteradoEvent e)
    {
        if (tweenSaldo.isAlive) tweenSaldo.Stop();
        float de = saldoMostrado;
        tweenSaldo = Tween.Custom(startValue: de, endValue: e.saldo, duration: 0.8f,
            onValueChange: v => { saldoMostrado = v; txtSaldo.text = UIFactory.Dinheiro(v); },
            ease: Ease.OutCubic);
        Pulsar(hud);
        // Não mostra "+R$" quando o valor veio do save carregado no início da cena
        if (e.delta > 0.01f && Time.timeSinceLevelLoad > 1f) Toast($"+{UIFactory.Dinheiro(e.delta)}", UIFactory.Sucesso, 1.8f);
    }

    void AtualizarReputacao(float rep, bool animar)
    {
        txtReputacao.text = $"Reputação {rep:F0}/100";
        float alvo = Mathf.Clamp01(rep / 100f);
        barraReputacaoImg.color = rep < 30f ? UIFactory.Perigo : rep < 60f ? UIFactory.Alerta : UIFactory.Sucesso;

        int fase = ReputationManager.Instance != null ? ReputationManager.Instance.FaseAtual : 1;
        txtFase.text = fase == 1 ? "Fase 1 · Técnico solo" :
                       fase == 2 ? "Fase 2 · Pequena equipe" : "Fase 3 · Empresa de TI";

        if (!animar) { barraReputacao.anchorMax = new Vector2(alvo, 1f); return; }
        float de = barraReputacao.anchorMax.x;
        Tween.Custom(startValue: de, endValue: alvo, duration: 0.5f,
            onValueChange: v => { if (barraReputacao != null) barraReputacao.anchorMax = new Vector2(v, 1f); },
            ease: Ease.OutCubic);
    }

    void AoDesbloquearFase(FaseDesbloqueadaEvent e)
    {
        string texto = e.fase == 2
            ? "<size=30><b>FASE 2 DESBLOQUEADA!</b></size>\nAgora você pode contratar funcionários (aba Equipe do tablet)."
            : "<size=30><b>FASE 3 DESBLOQUEADA!</b></size>\nClientes grandes e chamados difíceis vão aparecer.";
        Toast(texto, UIFactory.Alerta, 5f);
        AtualizarReputacao(ReputationManager.Instance.GetReputacao(), true);
    }

    void AoConcluirChamado(ChamadoConcluidoEvent e)
    {
        if (e.porFuncionario)
            Toast(e.cometeuErro
                ? $"Sua equipe errou no chamado de {e.chamado.nomeCliente}. Reputação caiu."
                : $"Sua equipe resolveu o chamado de {e.chamado.nomeCliente}!",
                e.cometeuErro ? UIFactory.Perigo : UIFactory.Sucesso);
        else
            Toast($"Chamado de <b>{e.chamado.nomeCliente}</b> resolvido em {Tempo(e.tempoGasto)}!" +
                  (e.diagnosticoCerteiro ? "\n<color=#FFD54A>Diagnóstico certeiro! +1 reputação</color>" : ""),
                  UIFactory.Sucesso, 3.5f);
        AtualizarTablet();
    }

    void AtualizarCardChamado()
    {
        var ch = MissionManager.Instance != null ? MissionManager.Instance.chamadoAtivo : null;
        bool ativo = ch != null;
        if (cardChamado.gameObject.activeSelf != ativo) cardChamado.gameObject.SetActive(ativo);
        if (!ativo) return;

        float restante = MissionManager.Instance.TempoRestante;
        float frac = Mathf.Clamp01(restante / ch.tempoLimite);
        txtChCliente.text = $"{ch.nomeCliente}  <size=70%><color={ch.CorPersonalidade()}>{ch.PersonalidadeTexto().ToUpper()}</color></size>";
        txtChProblema.text = ch.DescricaoVisivel;
        txtChEquip.text = ch.EquipamentoConhecido
            ? $"Problema no(a) {ch.EquipamentoTexto()}  ·  Fale com {ch.contato}"
            : $"Converse com {ch.contato} para entender o problema";
        txtChPistas.text = ch.anotacoes.Count == 0 ? "Nenhuma pista anotada ainda"
            : $"{ch.anotacoes.Count} pista(s) anotada(s)  ·  veja no tablet (Tab)";
        txtChTempo.text = restante > 0 ? $"Prazo: {Tempo(restante)}" : $"ATRASADO {Tempo(-restante)}";
        txtChTempo.color = restante <= 0 ? UIFactory.Perigo : frac < 0.25f ? UIFactory.Alerta : UIFactory.CorTexto;
        barraTempo.anchorMax = new Vector2(frac, 1f);
        barraTempoImg.color = frac < 0.25f ? UIFactory.Perigo : frac < 0.5f ? UIFactory.Alerta : UIFactory.Sucesso;
    }

    void AtualizarPrompt()
    {
        string texto = null;
        if (modal == Modal.Nenhum)
            texto = Interactable.MaisProximoEmAlcance()?.TextoPrompt;
        bool mostrar = texto != null;
        if (prompt.gameObject.activeSelf != mostrar) prompt.gameObject.SetActive(mostrar);
        if (mostrar) txtPrompt.text = texto;
    }

    // ---------- Notificações ----------

    public void Toast(string texto, Color cor, float duracao = 2.5f)
    {
        var card = UIFactory.Painel(areaToasts, "Toast", UIFactory.Fundo);
        UIFactory.Horizontal(card, 0, 0);
        card.GetComponent<HorizontalLayoutGroup>().padding = new RectOffset(22, 22, 14, 14);
        card.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = true;
        var faixa = UIFactory.Painel(card, "Faixa", cor);
        var le = faixa.gameObject.AddComponent<LayoutElement>();
        le.preferredWidth = 6; le.minWidth = 6;
        var t = UIFactory.Texto(card, texto, 22, UIFactory.CorTexto);
        t.margin = new Vector4(16, 0, 0, 0);
        t.gameObject.AddComponent<LayoutElement>().preferredWidth = 680;
        var grupo = card.gameObject.AddComponent<CanvasGroup>();
        grupo.alpha = 0f;
        StartCoroutine(VidaToast(card, grupo, duracao));
    }

    IEnumerator VidaToast(RectTransform card, CanvasGroup grupo, float duracao)
    {
        Tween.Custom(startValue: 0f, endValue: 1f, duration: 0.25f,
            onValueChange: v => { if (grupo != null) grupo.alpha = v; }, useUnscaledTime: true);
        yield return new WaitForSecondsRealtime(duracao);
        Tween.Custom(startValue: 1f, endValue: 0f, duration: 0.4f,
            onValueChange: v => { if (grupo != null) grupo.alpha = v; }, useUnscaledTime: true);
        yield return new WaitForSecondsRealtime(0.45f);
        if (card != null) Destroy(card.gameObject);
    }

    void Pulsar(RectTransform alvo)
    {
        if (alvo == null) return;
        Tween.Custom(startValue: 1.06f, endValue: 1f, duration: 0.35f,
            onValueChange: v => { if (alvo != null) alvo.localScale = Vector3.one * v; }, ease: Ease.OutCubic);
    }

    // ---------- Modais ----------

    void AbrirModal(Modal tipo, RectTransform janela)
    {
        foreach (var j in janelas) if (j != null) j.gameObject.SetActive(false);

        modal = tipo;
        overlay.gameObject.SetActive(true);
        janela.gameObject.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        overlayGrupo.alpha = 0f;
        Tween.Custom(startValue: 0f, endValue: 1f, duration: 0.18f,
            onValueChange: v => { if (overlayGrupo != null) overlayGrupo.alpha = v; }, useUnscaledTime: true);
        Tween.Custom(startValue: 0.92f, endValue: 1f, duration: 0.25f,
            onValueChange: v => { if (janela != null) janela.localScale = Vector3.one * v; },
            ease: Ease.OutBack, useUnscaledTime: true);

        Time.timeScale = tipo == Modal.Pausa ? 0f : 1f;
    }

    public void FecharModal()
    {
        if (rotinaSegurar != null) { StopCoroutine(rotinaSegurar); rotinaSegurar = null; }
        segurando = false;
        // Desistir da crimpagem volta para a janela de onde ela foi aberta
        if (modal == Modal.Crimpagem && aoCancelarCrimpagem != null) { CancelarCrimpagem(); return; }
        crimpAtual = null;
        modal = Modal.Nenhum;
        equipamentoAtual = null;
        npcConversa = null;
        overlay.gameObject.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        Time.timeScale = 1f;
    }

    public void AbrirPausa() => AbrirModal(Modal.Pausa, pausa);

    public void AbrirTablet()
    {
        AbrirModal(Modal.Tablet, tablet);
        FuncionarioManager.Instance?.GerarCandidatosSeVazio();
        AtualizarTablet();
    }

    void TrocarAba(int aba)
    {
        int fase = ReputationManager.Instance != null ? ReputationManager.Instance.FaseAtual : 1;
        if (aba == 1 && fase < 2)
        {
            Toast("A aba Equipe abre na <b>Fase 2</b> (30 de reputação).", UIFactory.Alerta);
            return;
        }
        abaAtiva = aba;
        AtualizarTablet();
    }

    void AtualizarTablet()
    {
        if (tablet == null || !tablet.gameObject.activeSelf) return;

        int fase = ReputationManager.Instance != null ? ReputationManager.Instance.FaseAtual : 1;
        if (fase < 2 && abaAtiva == 1) abaAtiva = 0;
        abaChamados.GetComponent<Image>().color = abaAtiva == 0 ? UIFactory.Destaque : UIFactory.CartaoClaro;
        abaEquipe.GetComponent<Image>().color = abaAtiva == 1 ? UIFactory.Destaque : UIFactory.CartaoClaro;
        abaLoja.GetComponent<Image>().color = abaAtiva == 2 ? UIFactory.Destaque : UIFactory.CartaoClaro;
        abaEmpresa.GetComponent<Image>().color = abaAtiva == 3 ? UIFactory.Destaque : UIFactory.CartaoClaro;
        abaContratos.GetComponent<Image>().color = abaAtiva == 4 ? UIFactory.Destaque : UIFactory.CartaoClaro;
        abaEquipe.GetComponentInChildren<TextMeshProUGUI>().text = fase >= 2 ? "Equipe" : "Equipe (Fase 2)";

        float saldo = GameEconomy.Instance != null ? GameEconomy.Instance.saldo : 0f;
        txtTabletInfo.text = $"{UIFactory.Dinheiro(saldo)}  ·  Fase {fase}";

        UIFactory.Limpar(tabletConteudo);
        if (abaAtiva == 0) MontarAbaChamados();
        else if (abaAtiva == 1) MontarAbaEquipe();
        else if (abaAtiva == 2) MontarAbaLoja();
        else if (abaAtiva == 3) MontarAbaEmpresa();
        else MontarAbaContratos();
    }

    RectTransform Cartao(Color? cor = null)
    {
        var c = UIFactory.Painel(tabletConteudo, "Cartao", cor ?? UIFactory.Cartao);
        UIFactory.Vertical(c, 8, 20);
        return c;
    }

    RectTransform LinhaBotoes(RectTransform pai)
    {
        var linha = UIFactory.Vazio(pai, "Botoes");
        UIFactory.Horizontal(linha, 12);
        return linha;
    }

    void Secao(string titulo)
    {
        UIFactory.Texto(tabletConteudo, titulo, 18, UIFactory.CorSuave, FontStyles.Bold);
    }

    void MontarAbaChamados()
    {
        var mm = MissionManager.Instance;
        if (mm == null) return;

        if (mm.chamadoAtivo != null)
        {
            var ch = mm.chamadoAtivo;
            Secao("EM ATENDIMENTO");
            var c = Cartao(UIFactory.CartaoClaro);
            UIFactory.Texto(c, $"<b>{ch.nomeCliente}</b>   <size=18><color={ch.CorPersonalidade()}>{ch.PersonalidadeTexto().ToUpper()}</color></size>", 26, UIFactory.CorTexto);
            UIFactory.Texto(c, ch.DescricaoVisivel, 21, UIFactory.CorTexto);
            UIFactory.Texto(c, ch.EquipamentoConhecido
                ? $"Converse com {ch.contato} (E) para pegar pistas e vá até o(a) {ch.EquipamentoTexto()}. Conserte e confira se funcionou (imprimir, ping...)."
                : $"{ch.contato} não soube explicar. Converse com ele(a) (E) para descobrir o equipamento.", 19, UIFactory.CorSuave);
            foreach (var nota in ch.anotacoes)
                UIFactory.Texto(c, "• " + nota, 18, UIFactory.CorSuave);
            var linha = LinhaBotoes(c);
            UIFactory.Botao(linha, "Desistir (-20 reputação)", () =>
            {
                mm.AbandonarChamado();
                Toast("Você desistiu do chamado. O cliente não gostou nada.", UIFactory.Perigo);
                AtualizarTablet();
            }, UIFactory.Perigo, 320, 48, 20);
        }

        bool foraDoExpediente = DiaManager.Instance != null && DiaManager.Instance.ForaDoExpediente;
        if (foraDoExpediente)
            UIFactory.Texto(tabletConteudo, "<color=#F5B83F>Fora do expediente:</color> os clientes voltam a ligar amanhã às 8h. Saia pela porta do escritório para encerrar o dia.",
                20, UIFactory.CorTexto);

        Secao("CHAMADOS ABERTOS");
        if (mm.chamadosDisponiveis.Count == 0)
        {
            UIFactory.Texto(tabletConteudo, "Nenhum chamado no momento.", 21, UIFactory.CorSuave);
            return;
        }

        var equipeLivre = FuncionarioManager.Instance != null &&
                          FuncionarioManager.Instance.equipe.Exists(f => !f.ocupado);

        foreach (var ch in new List<Chamado>(mm.chamadosDisponiveis))
        {
            var chamado = ch;
            var c = Cartao();
            var corDif = chamado.dificuldade == DificuldadeChamado.Facil ? "#3CCB7F" :
                         chamado.dificuldade == DificuldadeChamado.Medio ? "#F5B83F" : "#F2545B";
            UIFactory.Texto(c, $"<b>{chamado.nomeCliente}</b>   <color={corDif}><size=18>{chamado.DificuldadeTexto().ToUpper()}</size></color>" +
                $"   <color={chamado.CorPersonalidade()}><size=18>{chamado.PersonalidadeTexto().ToUpper()}</size></color>",
                26, UIFactory.CorTexto);
            UIFactory.Texto(c, $"\"{chamado.DescricaoVisivel}\"  <color=#9AA6B8>— {chamado.contato}</color>", 21, UIFactory.CorTexto);
            UIFactory.Texto(c, (chamado.contratoId > 0 ? "<color=#3FA9F5>Incluso no contrato de locação</color>" : $"Paga {UIFactory.Dinheiro(chamado.recompensa)}") +
                $"  ·  Prazo {Tempo(chamado.tempoLimite)}  ·  {chamado.EquipamentoVisivel}" +
                (chamado.contratoId > 0 ? "" : chamado.exigeNota ? "  ·  <color=#F5B83F>exige nota fiscal</color>" : "  ·  aceita recibo"),
                19, UIFactory.CorSuave);
            string motivoBloqueio = null;
            bool liberado = EmpresaManager.Instance == null || EmpresaManager.Instance.PodeAtender(chamado, out motivoBloqueio);
            if (!liberado) UIFactory.Texto(c, $"<color=#F2545B>{motivoBloqueio}</color>", 18, UIFactory.CorTexto);

            var linha = LinhaBotoes(c);
            var atender = UIFactory.Botao(linha, "Atender", () =>
            {
                mm.AceitarChamado(chamado);
                FecharModal();
            }, UIFactory.Destaque, 180, 48, 22);
            atender.interactable = mm.chamadoAtivo == null && !foraDoExpediente && liberado;

            if (equipeLivre && !foraDoExpediente)
            {
                UIFactory.Botao(linha, "Delegar para a equipe", () =>
                {
                    mm.chamadosDisponiveis.Remove(chamado);
                    FuncionarioManager.Instance.DelegarChamado(chamado);
                    Toast($"Chamado de {chamado.nomeCliente} enviado para a equipe.", UIFactory.Destaque);
                    if (mm.chamadosDisponiveis.Count < 2) mm.GerarChamados();
                    AtualizarTablet();
                }, UIFactory.CartaoClaro, 280, 48, 22);
            }
        }
    }

    void MontarAbaEquipe()
    {
        var fm = FuncionarioManager.Instance;
        if (fm == null) return;

        Secao($"SUA EQUIPE ({fm.equipe.Count})");
        if (fm.equipe.Count == 0)
            UIFactory.Texto(tabletConteudo, "Ninguém contratado ainda. Funcionários resolvem chamados sozinhos, mas podem errar!", 21, UIFactory.CorSuave);

        foreach (var f in fm.equipe)
        {
            var c = Cartao();
            string status = f.ocupado ? "<color=#F5B83F>Atendendo um chamado</color>" : "<color=#3CCB7F>Livre</color>";
            UIFactory.Texto(c, $"<b>{f.nome}</b>  ·  {f.NivelTexto()}", 24, UIFactory.CorTexto);
            UIFactory.Texto(c, $"{status}  ·  Acerta {f.ChanceAcerto() * 100:F0}%  ·  {UIFactory.Dinheiro(f.salarioMensal)}/mês", 19, UIFactory.CorSuave);
        }

        Secao("CANDIDATOS");
        if (fm.candidatos.Count == 0)
            UIFactory.Texto(tabletConteudo, "Nenhum candidato agora. Volte mais tarde.", 21, UIFactory.CorSuave);

        foreach (var cand in new List<Funcionario>(fm.candidatos))
        {
            var candidato = cand;
            var c = Cartao();
            UIFactory.Texto(c, $"<b>{candidato.nome}</b>  ·  {candidato.NivelTexto()}", 24, UIFactory.CorTexto);
            UIFactory.Texto(c, $"Acerta {candidato.ChanceAcerto() * 100:F0}% dos chamados  ·  Salário {UIFactory.Dinheiro(candidato.salarioMensal)}/mês",
                19, UIFactory.CorSuave);
            var linha = LinhaBotoes(c);
            UIFactory.Botao(linha, "Contratar", () =>
            {
                if (fm.ContratarCandidato(candidato)) AtualizarTablet();
            }, UIFactory.Sucesso, 200, 48, 22);
        }
    }

    static string Tempo(float segundos)
    {
        int s = Mathf.Max(0, Mathf.CeilToInt(segundos));
        return $"{s / 60}:{s % 60:00}";
    }
}
