using System.Collections.Generic;
using UnityEngine;

public partial class MissionManager : MonoBehaviour
{
    public static MissionManager Instance;

    [Header("Chamados")]
    public List<Chamado> chamadosDisponiveis = new List<Chamado>();
    // NonSerialized: sem isso o Unity cria um chamado "vazio" pelo Inspector e o jogo acha que tem chamado ativo
    [System.NonSerialized] public Chamado chamadoAtivo;
    public int maxChamadosSimultaneos = 3;

    [Header("Retrabalho")]
    [Tooltip("Reputação perdida quando o cliente liga dizendo que o problema voltou")]
    public float penalidadeRetorno = 3f;

    [Header("Investigação")]
    [Tooltip("Minutos do jogo gastos em cada pergunta ao cliente")]
    public float custoPergunta = 5f;
    [Tooltip("Perguntas que um cliente apressado aguenta antes de se irritar")]
    public int pacienciaApressado = 2;

    [Header("Locais")]
    public Transform[] locaisClientes;

    // Cada nome leva a uma sala (SalaCliente.TipoDoCliente): contabilidade, clínica ou loja
    static readonly string[] clientesFase1 = { "Contabilidade Alfa", "Escritório Beta", "Clínica Gama", "Papelaria Kappa" };
    static readonly string[] clientesFase2 = { "Indústria Delta", "Construtora Épsilon", "Farmácia Zeta" };
    static readonly string[] clientesFase3 = { "Banco Eta", "Hospital Theta", "Multinacional Iota" };

    private Transform jogador;

    // ---------- Ordens de serviço, avaliações e retornos (vão para o save) ----------

    [System.Serializable]
    public class Dados
    {
        public int proximaOS = 1;
        public int proximoRetorno = 1;
        public float somaEstrelas;
        public int avaliacoes;
        public bool tutorialFeito;    // o chamado guiado do primeiro dia já passou (saves antigos: true na migração)
        public List<RetornoPendente> retornos = new List<RetornoPendente>();
        public List<OrdemServico> historico = new List<OrdemServico>();   // últimas OS fechadas
    }
    public static Dados dadosSalvos;
    Dados dados = new Dados();
    const int MaxHistorico = 15;

    public List<RetornoPendente> Retornos => dados.retornos;
    public List<OrdemServico> HistoricoOS => dados.historico;
    public int Avaliacoes => dados.avaliacoes;
    public float MediaEstrelas => dados.avaliacoes > 0 ? dados.somaEstrelas / dados.avaliacoes : 0f;

    public Dados Exportar() => dados;
    public void Carregar(Dados d) { dados = d ?? new Dados(); dados.retornos ??= new List<RetornoPendente>(); dados.historico ??= new List<OrdemServico>(); }
    public void NovoJogo() => dados = new Dados();

    // ---------- Tempo (horas do jogo) ----------

    // Minutos do jogo desde que o técnico aceitou o chamado
    public float TempoDecorrido => chamadoAtivo != null ? SLA.Agora() - chamadoAtivo.aceitoEm : 0f;
    // Minutos do jogo até estourar o SLA (negativo = atrasado)
    public float TempoRestante => chamadoAtivo != null ? SLA.Restante(chamadoAtivo, SLA.Agora()) : 0f;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
        if (dadosSalvos != null) { Carregar(dadosSalvos); dadosSalvos = null; }
    }

    void Start()
    {
        jogador = GameObject.FindWithTag("Player").transform;
        GerarChamados();
    }

    public void GerarChamados()
    {
        // Chamados de contrato e retornos em garantia continuam na lista; só os avulsos são trocados
        chamadosDisponiveis.RemoveAll(c => c.contratoId <= 0 && !c.retrabalho);
        AbrirRetornosDoDia();
        AbrirChamadoGuiado();

        int fase = ReputationManager.Instance != null ? ReputationManager.Instance.FaseAtual : 1;
        int quantidade = Mathf.Min(fase == 1 ? 3 : fase == 2 ? 4 : 5, maxChamadosSimultaneos);
        // Fim de semana: sábado tem pouco movimento, domingo quase nada
        var d = DiaManager.Instance;
        if (d != null && d.Data.DayOfWeek == System.DayOfWeek.Saturday) quantidade = Mathf.Min(quantidade, 2);
        if (d != null && d.Data.DayOfWeek == System.DayOfWeek.Sunday) quantidade = 1;

        for (int i = 0; i < quantidade; i++)
            chamadosDisponiveis.Add(CriarChamadoPorFase(fase));
        ContratosManager.Instance?.GarantirChamadosServico();

        Debug.Log($"[MissionManager] {chamadosDisponiveis.Count} chamados gerados para fase {fase}.");
    }

    Chamado CriarChamadoPorFase(int fase)
    {
        DificuldadeChamado dif;
        if      (fase == 1) dif = DificuldadeChamado.Facil;
        else if (fase == 2) dif = Random.value < 0.5f ? DificuldadeChamado.Facil : DificuldadeChamado.Medio;
        else                dif = Random.value < 0.4f ? DificuldadeChamado.Medio : DificuldadeChamado.Dificil;

        Problema p = CatalogoProblemas.SortearDisponivel(dif, CatalogoProblemas.PodeFazerCheckup(Mochila.Instance));
        Personalidade jeito = CatalogoProblemas.SortearPersonalidade(fase);

        string[] clientes = fase == 1 ? clientesFase1 : fase == 2 ? clientesFase2 : clientesFase3;

        // Prazo: SLA em horas do jogo pela prioridade (desde a abertura do chamado)
        var prioridade = SLA.Definir(dif, jeito, false, false);
        float sla = SLA.Horas(prioridade) * 60f;
        // Chamado avulso: horas técnicas estimadas × hora técnica + deslocamento
        float horas = dif == DificuldadeChamado.Facil ? 1f : dif == DificuldadeChamado.Medio ? 2f : 3f;
        float recompensa  = (horas * CatalogoServicos.HoraTecnica + CatalogoServicos.Deslocamento) * EmpresaManager.MultPrecoRegime;
        recompensa  = Mathf.Round(recompensa * Chamado.MultPagamento(jeito) / 5f) * 5f;

        var chamado = new Chamado(clientes[Random.Range(0, clientes.Length)], p.descricao, TipoDo(p), dif, recompensa, sla);
        chamado.equipamento = p.equipamento;
        chamado.solucao = p.solucao;
        chamado.problema = p;
        chamado.personalidade = jeito;
        chamado.prioridade = prioridade;
        chamado.abertoEm = SLA.Agora();
        chamado.contato = CatalogoProblemas.SortearContato();
        // Quanto maior o cliente, mais exige nota fiscal
        chamado.exigeNota = Random.value < (fase == 1 ? 0.25f : fase == 2 ? 0.6f : 0.9f);
        return chamado;
    }

    static TipoChamado TipoDo(Problema p) => RegistroEquipamentos.De(p.equipamento).TipoChamado;

    // Defeito num equipamento alugado: vira um chamado incluso no contrato (não paga, mas o cliente espera rapidez)
    // dif Fácil = problemas de impressora; Médio = problemas de computador
    public void AdicionarChamadoDeContrato(int contratoId, string empresa, DificuldadeChamado dif = DificuldadeChamado.Facil, string solucao = null)
    {
        var p = CatalogoProblemas.SortearDisponivel(dif, false);   // contrato: defeito, não check-up
        // defeito já conhecido (ex.: peça que passou da vida útil)
        if (solucao != null)
            foreach (var lista in new[] { CatalogoProblemas.Faceis, CatalogoProblemas.Medios, CatalogoProblemas.Dificeis })
                foreach (var x in lista) if (x.solucao == solucao && x.equipamento == TipoEquipamento.Impressora) p = x;
        var prioridade = SLA.Definir(dif, Personalidade.Tranquilo, true, false);
        var ch = new Chamado(empresa, p.descricao, TipoDo(p), dif, 0f, SLA.Horas(prioridade) * 60f)
        {
            equipamento = p.equipamento, solucao = p.solucao, problema = p, contratoId = contratoId,
            personalidade = Personalidade.Tranquilo, contato = CatalogoProblemas.SortearContato(),
            prioridade = prioridade, abertoEm = SLA.Agora()
        };
        chamadosDisponiveis.Insert(0, ch);
    }

    // ---------- Locação física (Etapa 5): instalação e visita mensal viram chamados no cliente ----------

    public Chamado ChamadoDeServico(int contratoId, ServicoLocacao tipo)
    {
        if (chamadoAtivo != null && chamadoAtivo.contratoId == contratoId && chamadoAtivo.servicoLocacao == tipo) return chamadoAtivo;
        return chamadosDisponiveis.Find(c => c.contratoId == contratoId && c.servicoLocacao == tipo);
    }

    public Chamado AdicionarChamadoServicoLocacao(ContratoLocacao c, ServicoLocacao tipo)
    {
        if (c == null || tipo == ServicoLocacao.Nenhum) return null;
        bool instalacao = tipo == ServicoLocacao.Instalacao;
        var p = instalacao ? CatalogoProblemas.InstalacaoLocacao : CatalogoProblemas.VisitaLocacao;
        var prioridade = PrioridadeSLA.Baixa;   // serviço agendado: prazo longo
        float valor = instalacao ? CatalogoLocacao.Implantacao + CatalogoLocacao.Frete : 0f;
        var ch = new Chamado(c.empresa, p.descricao, TipoChamado.Impressora, DificuldadeChamado.Facil, valor, SLA.Horas(prioridade) * 60f)
        {
            equipamento = p.equipamento, solucao = p.solucao, problema = p, contratoId = c.id, servicoLocacao = tipo,
            personalidade = Personalidade.Tranquilo, contato = CatalogoProblemas.SortearContato(),
            prioridade = prioridade, abertoEm = SLA.Agora(), exigeNota = true
        };
        ch.anotacoes.Add(instalacao
            ? "Instalação da locação: a impressora vai na caixa. Monte, fixe o IP no painel, adicione no computador e teste."
            : "Visita mensal: ler o contador no visor, ver o toner e a unidade de imagem. Leve toner e cilindro.");
        chamadosDisponiveis.Insert(0, ch);
        Debug.Log($"[MissionManager] Chamado de locação: {(instalacao ? "instalação" : "visita mensal")} na {c.empresa}.");
        return ch;
    }

    public void AceitarChamado(Chamado chamado)
    {
        var empresa = EmpresaManager.Instance;
        if (empresa != null && !empresa.PodeAtender(chamado, out string motivo))
        {
            GameUI.Instance?.Toast(motivo, UIFactory.Perigo, 4f);
            return;
        }
        chamadoAtivo = chamado;
        chamadosDisponiveis.Remove(chamado);
        chamado.estado = new EstadoEquipamentos();
        if (chamado.problema != null) CatalogoProblemas.Quebrar(chamado.problema, chamado.estado);
        if (chamado.tutorial) chamado.estado.tonerDoCliente = true;   // o cliente já comprou o toner
        if (chamado.servicoLocacao != ServicoLocacao.Nenhum) ContratosManager.Instance?.PrepararEstado(chamado);
        if (chamado.projetoId > 0) ProjetosManager.Instance?.PrepararEstado(chamado);
        Sujeira.LimparTudo();
        chamado.aceitoEm = SLA.Agora();
        chamado.aguardandoAssinatura = false;
        chamado.os = NovaOS(chamado);
        // Os equipamentos vão para a sala deste cliente antes do resto (sujeira, cliente e OS usam a posição deles)
        SalaCliente.Para(chamado.nomeCliente)?.ReceberEquipamentos();
        EventBus<ChamadoAceitoEvent>.Raise(new ChamadoAceitoEvent { chamado = chamado });
        Debug.Log($"[MissionManager] Chamado aceito: {chamado.nomeCliente} — {chamado.descricaoProblema} | OS {chamado.os.numero} | SLA {SLA.Texto(chamado.prioridade)} até {SLA.HoraDoPrazo(chamado, SLA.Agora())}");
        IrParaCliente();
    }

    // A OS nasce no aceite com o que o cliente relatou; o resto se preenche com o trabalho
    OrdemServico NovaOS(Chamado ch)
    {
        return new OrdemServico
        {
            numero = dados.proximaOS++,
            cliente = ch.nomeCliente,
            contato = ch.contato,
            equipamento = ch.EquipamentoConhecido ? ch.EquipamentoTexto() : "a definir",
            relato = ch.DescricaoVisivel,
            abertura = TextoDataHora(ch.abertoEm),
            prioridade = $"{SLA.Texto(ch.prioridade)} (SLA {SLA.Horas(ch.prioridade):0}h)",
            retrabalho = ch.retrabalho,
        };
    }

    // "Seg 02/03 08:00" a partir do minuto absoluto do jogo
    public static string TextoDataHora(float minutoAbs)
    {
        int dia = Mathf.FloorToInt(minutoAbs / 1440f) + 1;
        int m = Mathf.FloorToInt(minutoAbs - (dia - 1) * 1440f);
        return $"{DiaManager.DataDoDia(dia):dd/MM} {m / 60:00}:{m % 60:00}";
    }

    // ---------- Manutenção ----------

    public EstadoEquipamentos Estado => chamadoAtivo?.estado;

    // Onde fica o equipamento do chamado (para espalhar sujeira, por exemplo)
    public Vector3 PosicaoEquipamentoAtual
    {
        get
        {
            if (chamadoAtivo != null)
                foreach (var eq in FindObjectsByType<Equipamento>())
                    if (eq.tipoEquipamento == chamadoAtivo.equipamento) return eq.transform.position;
            return jogador != null ? jogador.position : Vector3.zero;
        }
    }

    // O jogador conferiu algo (imprimiu, pingou, reiniciou...). Se o problema sumiu, o cliente confere e
    // falta só fechar a OS com ele (checklist e assinatura).
    public bool Verificar()
    {
        var ch = chamadoAtivo;
        if (ch == null || ch.problema == null || ch.estado == null) return false;
        if (!CatalogoProblemas.Resolvido(ch.problema, ch.estado)) return false;
        if (ch.aguardandoAssinatura) return true;
        ch.aguardandoAssinatura = true;
        bool checkup = CatalogoProblemas.EhCheckup(ch.problema);
        if (ch.os != null)
        {
            ch.os.equipamento = ch.EquipamentoTexto();
            ch.os.diagnostico = CatalogoProblemas.EhServicoProjeto(ch.problema) ? DiagnosticoProjeto(ch)
                : CatalogoProblemas.EhServicoLocacao(ch.problema) ? DiagnosticoLocacao(ch)
                : checkup
                ? $"Check-up completo do computador ({ch.estado.idadePcAnos} anos): {ch.estado.Recomendacoes().Count} recomendação(ões) no laudo."
                : $"Defeito no(a) {ch.EquipamentoTexto().ToLowerInvariant()}. Solução: {ch.problema.solucao.ToLowerInvariant()}.";
            AtualizarLaudo(ch);
        }
        GameUI.Instance?.Toast(checkup
            ? $"<b>Laudo completo!</b> Todas as peças foram medidas.\nFeche tudo e fale com {ch.contato} para entregar o laudo e assinar a OS."
            : $"<b>{ch.contato}</b> conferiu: \"Funcionou! Muito obrigado!\"\n" +
              $"Arrume o local e fale com {ch.contato} para conferir o checklist e assinar a OS.", UIFactory.Sucesso, 5f);
        Debug.Log($"[MissionManager] Defeito resolvido (OS {ch.os?.numero}): falta a assinatura do cliente.");
        return true;
    }

    // Instalação/visita da locação: o que foi feito (a OS mostra; refeito na assinatura, depois da reserva)
    static string DiagnosticoLocacao(Chamado ch) =>
        ch.servicoLocacao == ServicoLocacao.Instalacao
            ? $"Impressora alugada instalada: tirada da caixa, toner inicial, papel, rede com IP fixo {ch.estado.ipImpressora}, adicionada no computador e testada."
            : "Visita mensal: contador lido no visor" + (ch.estado.visitaTonerBaixo ? ", toner trocado" : "") +
              (ch.estado.trocasCilindro > 0 ? ", unidade de imagem trocada (preventiva)" : "") +
              (ch.estado.reservasDeixadas > 0 ? $", {ch.estado.reservasDeixadas} toner(s) reserva deixado(s) no cliente" : "") + ".";

    // Medições das ferramentas viram o laudo da OS (no check-up é o próprio serviço; nos outros, o teste de prova)
    static void AtualizarLaudo(Chamado ch)
    {
        if (ch?.os == null || ch.estado == null) return;
        ch.os.laudo = new List<string>(ch.estado.medicoes);
        ch.os.recomendacoes = ch.estado.Recomendacoes();
    }

    // Uma ação que causou estrago (resetar sem precisar, desligar sem salvar...)
    public void RegistrarErro(string motivo, float reputacao)
    {
        if (chamadoAtivo == null) return;
        float penalidade = reputacao * chamadoAtivo.MultErro;
        chamadoAtivo.tentativasErradas++;
        chamadoAtivo.os?.RegistrarAcao("(!) " + motivo);
        ReputationManager.Instance?.AlterarReputacao(-penalidade);
        EventBus<AcaoErradaEvent>.Raise(new AcaoErradaEvent { acao = motivo, penalidade = penalidade });
    }

    // Peças usadas no cliente (toner, cabo) entram na conta dele e na OS
    public void CobrarPeca(float valor, string nome = null)
    {
        if (chamadoAtivo == null) return;
        chamadoAtivo.pecasCobradas += valor;
        chamadoAtivo.os?.RegistrarPeca(nome, valor);
    }

    public void RegistrarAcao(string descricao = null)
    {
        if (chamadoAtivo == null) return;
        chamadoAtivo.acoesFeitas++;
        chamadoAtivo.os?.RegistrarAcao(descricao);
    }

    // ---------- Conversa com o cliente ----------

    // Gasta minutos do relógio do jogo (perguntar, instalar o sistema...): o SLA corre junto
    public void ConsumirTempo(float minutos)
    {
        if (chamadoAtivo != null && DiaManager.Instance != null) DiaManager.Instance.minutoAtual += minutos;
    }

    // Faz uma pergunta ao cliente do chamado ativo e devolve a fala dele
    public string Perguntar(Pergunta pergunta)
    {
        var ch = chamadoAtivo;
        if (ch == null || ch.problema == null) return "...";
        if (ch.perguntasFeitas.Contains(pergunta)) return ch.problema.Resposta(pergunta, ch.Mente);

        ConsumirTempo(custoPergunta);
        ch.conversou = true;
        ch.perguntasFeitas.Add(pergunta);

        string resposta = ch.problema.Resposta(pergunta, ch.Mente);
        ch.anotacoes.Add($"<b>{ch.contato}</b> ({CatalogoProblemas.RotuloPergunta(pergunta)}): {resposta}");
        ch.os?.RegistrarAcao($"Perguntou ao cliente: {CatalogoProblemas.RotuloPergunta(pergunta)}");

        if (ch.personalidade == Personalidade.Apressado && ch.perguntasFeitas.Count > pacienciaApressado)
        {
            ReputationManager.Instance?.AlterarReputacao(-1f);
            resposta += $"\n<color=#F5B83F>{CatalogoProblemas.Impaciencia()}</color>  <size=80%>(-1 reputação)</size>";
        }
        return resposta;
    }

    // ---------- Fechamento: checklist, assinatura e avaliação ----------

    // O que o cliente confere agora (a janela da OS mostra antes de pedir a assinatura)
    public List<ItemChecklist> ChecklistAtual() =>
        chamadoAtivo == null ? new List<ItemChecklist>() :
        AvaliacaoCliente.Checklist(chamadoAtivo.problema, chamadoAtivo.estado, Sujeira.Todas.Count);

    // Chamado depois que o cliente assinou a prancheta (animação na ClienteNPC)
    public void FecharOSAssinada()
    {
        if (chamadoAtivo == null || !chamadoAtivo.aguardandoAssinatura) return;
        ConcluirChamado(false);
    }

    public void ConcluirChamado(bool cometeuErro = false)
    {
        if (chamadoAtivo == null) return;
        var ch = chamadoAtivo;
        float agora = SLA.Agora();
        float trabalho = Mathf.Max(0f, agora - ch.aceitoEm);
        float atraso = Mathf.Max(0f, -SLA.Restante(ch, agora));

        // O cliente confere o checklist e avalia
        var checklist = AvaliacaoCliente.Checklist(ch.problema, ch.estado, Sujeira.Todas.Count);
        int falhas = AvaliacaoCliente.Falhas(checklist);
        int estrelas = cometeuErro ? 1 : AvaliacaoCliente.Estrelas(atraso, falhas, ch.tentativasErradas, ch.personalidade, ch.retrabalho);
        string motivo = null;
        if (cometeuErro) motivo = "o serviço deu errado";
        else if (atraso > 0f) motivo = "demorou mais do que o combinado";
        else foreach (var it in checklist) if (!it.ok) { motivo = it.falha; break; }
        if (motivo == null && ch.tentativasErradas > 0) motivo = "fez estrago durante o serviço";
        if (motivo == null && ch.retrabalho) motivo = "tive que chamar de novo pelo mesmo problema";
        string comentario = AvaliacaoCliente.Comentario(estrelas, motivo, Random.Range(0, 3));

        ReputationManager.Instance?.AvaliarEstrelas(estrelas, ch.MultReputacao);
        dados.somaEstrelas += estrelas;
        dados.tutorialFeito = true;
        dados.avaliacoes++;

        // Pagamento: retorno em garantia não cobra o serviço de novo (só peças novas)
        float servico = ch.retrabalho ? 0f : cometeuErro ? ch.recompensa * 0.3f : ch.recompensa;
        // Etapa 9C: km rodados com o carro no chamado avulso entram na conta (o cliente paga o deslocamento)
        if (ch.os != null && !ch.retrabalho && ch.contratoId == 0 && ch.projetoId == 0 && ch.servicoLocacao == ServicoLocacao.Nenhum && ch.os.ValorDeslocamento > 0f)
        {
            servico += ch.os.ValorDeslocamento;
            Debug.Log($"[Carro] Deslocamento do chamado: {ch.os.kmRodados:0.0} km = R$ {ch.os.ValorDeslocamento:0} ({ch.os.minutosDeslocamento:0} min dirigindo).");
        }
        float valor = servico + ch.pecasCobradas;
        if (ch.projetoId > 0)
        {
            // Etapa 8: visita de um projeto de rede — o valor é o do projeto (entrada e entrega), não da visita
            string r = ProjetosManager.Instance?.VisitaConcluida(ch);
            if (r != null) GameUI.Instance?.Toast(r, UIFactory.Sucesso, 6f);
            servico = 0f;
        }
        else if (ch.servicoLocacao != ServicoLocacao.Nenhum)
        {
            // Instalação/visita da locação: a implantação é cobrada pelo contrato; toner e cilindro são da empresa
            string r = ContratosManager.Instance?.ServicoLocacaoConcluido(ch, atraso <= 0f);
            if (r != null) GameUI.Instance?.Toast(r, UIFactory.Sucesso, 5f);
            servico = 0f;
        }
        else if (ch.contratoId > 0)
        {
            // Incluso no contrato: não cobra o serviço; peças podem ser cobradas
            bool noPrazo = atraso <= 0f;
            ContratosManager.Instance?.ChamadoContratoEncerrado(ch.contratoId, true, noPrazo);
            ServicosManager.Instance?.ChamadoContratoEncerrado(ch.contratoId, true, noPrazo);
            if (ch.pecasCobradas > 0f && EmpresaManager.Instance != null) EmpresaManager.Instance.RegistrarServico(ch, ch.pecasCobradas, false);
        }
        else if (valor <= 0f)
            GameUI.Instance?.Toast("Retorno em garantia: sem cobrança.", UIFactory.Destaque, 3f);
        else if (EmpresaManager.Instance != null)
            GameUI.Instance?.Toast(EmpresaManager.Instance.RegistrarServico(ch, valor, false), UIFactory.Destaque, 4f);
        else
            GameEconomy.Instance?.ReceberPagamento(valor);

        // Sujeira esquecida no chão: já custou estrela; o cliente reclama e alguém limpa
        if (Sujeira.Todas.Count > 0)
        {
            GameUI.Instance?.Toast($"{ch.contato} reclamou da sujeira que você deixou no chão.", UIFactory.Perigo, 4f);
            Sujeira.LimparTudo();
        }

        // Bônus: investigou (conversou) e resolveu sem fazer estrago, dentro do prazo
        bool certeiro = !cometeuErro && ch.tentativasErradas == 0 && atraso <= 0f && ch.perguntasFeitas.Count > 0;
        if (certeiro) ReputationManager.Instance?.AlterarReputacao(1f);

        // A causa ficou? O cliente volta a chamar em 1 a 3 dias (o técnico só descobre quando ele ligar)
        string causa = CatalogoProblemas.CausaRaizPendente(ch.problema, ch.estado);
        if (ch.retrabalho) dados.retornos.RemoveAll(r => r.id == ch.retornoId);
        if (causa != null) AgendarRetorno(ch, causa);

        // Fecha a OS
        if (ch.os != null)
        {
            ch.os.minutosTrabalho = trabalho;
            ch.os.noPrazo = atraso <= 0f;
            ch.os.assinada = true;
            ch.os.estrelas = estrelas;
            ch.os.comentario = comentario;
            ch.os.valorServico = servico;
            ch.os.causaPendente = causa ?? "";
            AtualizarLaudo(ch);
            if (ch.estado != null && CatalogoProblemas.EhServicoLocacao(ch.problema)) ch.os.diagnostico = DiagnosticoLocacao(ch);
            if (ch.estado != null && CatalogoProblemas.EhServicoProjeto(ch.problema))
            {
                ch.os.diagnostico = DiagnosticoProjeto(ch);
                ch.os.fotoAntes = ch.estado.fotoAntes;
                ch.os.fotoDepois = ch.estado.fotoDepois;
                if (ch.estado.fotoAntes != null || ch.estado.fotoDepois != null)
                    ch.os.fotos = (ch.estado.fotoAntes != null ? "antes" : "") + (ch.estado.fotoAntes != null && ch.estado.fotoDepois != null ? " e " : "") +
                                  (ch.estado.fotoDepois != null ? "depois" : "");
            }
            if (string.IsNullOrEmpty(ch.os.diagnostico) && ch.problema != null)
                ch.os.diagnostico = $"Solução: {ch.problema.solucao.ToLowerInvariant()}.";
            dados.historico.Insert(0, ch.os);
            if (dados.historico.Count > MaxHistorico) dados.historico.RemoveRange(MaxHistorico, dados.historico.Count - MaxHistorico);
        }

        Debug.Log($"[MissionManager] OS {ch.os?.numero} fechada: {estrelas} estrela(s), {OrdemServico.HorasTexto(trabalho)} de trabalho, " +
                  $"atraso {atraso:F0} min, checklist {checklist.Count - falhas}/{checklist.Count}, causa pendente: {causa ?? "nenhuma"} | \"{comentario}\"");

        ch.concluido = true;
        ch.aguardandoAssinatura = false;
        EventBus<ChamadoConcluidoEvent>.Raise(new ChamadoConcluidoEvent
        {
            chamado = ch, tempoGasto = trabalho, cometeuErro = cometeuErro, porFuncionario = false,
            diagnosticoCerteiro = certeiro, estrelas = estrelas, comentario = comentario
        });
        chamadoAtivo = null;

        if (chamadosDisponiveis.Count < 2)
            GerarChamados();
    }

    // Desistir do chamado: o cliente fica insatisfeito
    public void AbandonarChamado()
    {
        if (chamadoAtivo == null) return;
        Debug.Log($"[MissionManager] Chamado abandonado: {chamadoAtivo.nomeCliente}");
        chamadoAtivo.falhou = true;
        if (chamadoAtivo.servicoLocacao != ServicoLocacao.Nenhum)
            ContratosManager.Instance?.ServicoLocacaoAbandonado(chamadoAtivo);
        else if (chamadoAtivo.contratoId > 0)
        {
            ContratosManager.Instance?.ChamadoContratoEncerrado(chamadoAtivo.contratoId, false, false);
            ServicosManager.Instance?.ChamadoContratoEncerrado(chamadoAtivo.contratoId, false, false);
        }
        // Abandonar um retorno em garantia encerra a garantia (e pega ainda pior)
        if (chamadoAtivo.retrabalho) dados.retornos.RemoveAll(r => r.id == chamadoAtivo.retornoId);
        ReputationManager.Instance?.AlterarReputacao(-ReputationManager.Instance.punicaoErro * (chamadoAtivo.retrabalho ? 1.5f : 1f));
        chamadoAtivo = null;
        GPS.Instance?.Desligar();
        ContratosManager.Instance?.GarantirChamadosServico();   // instalação/visita continua pendente
        // projeto de rede: o que foi feito nesta visita se perde (o cliente desconfia); o projeto continua
        if (chamadosDisponiveis.Count < 2)
            GerarChamados();
    }

    // ---------- Primeiro dia (chamado guiado) ----------

    public bool TutorialPendente => !dados.tutorialFeito && Conquistas.chamados == 0;

    void AbrirChamadoGuiado()
    {
        if (!TutorialPendente || chamadosDisponiveis.Exists(c => c.tutorial) || (chamadoAtivo != null && chamadoAtivo.tutorial)) return;
        var p = CatalogoProblemas.Buscar(TipoEquipamento.Impressora, TutorialPrimeiroDia.Solucao);
        if (p == null) return;
        float recompensa = Mathf.Round((CatalogoServicos.HoraTecnica + CatalogoServicos.Deslocamento) * EmpresaManager.MultPrecoRegime / 5f) * 5f;
        var ch = new Chamado("Escritório Beta", p.descricao, TipoChamado.Impressora, DificuldadeChamado.Facil, recompensa, SLA.Horas(PrioridadeSLA.Baixa) * 60f)
        {
            equipamento = p.equipamento, solucao = p.solucao, problema = p, tutorial = true,
            personalidade = Personalidade.Tranquilo, contato = "Dona Lúcia", exigeNota = false,
            prioridade = PrioridadeSLA.Baixa, abertoEm = SLA.Agora()
        };
        ch.anotacoes.Add("Primeiro chamado: siga o guia no card do chamado (canto esquerdo).");
        chamadosDisponiveis.Insert(0, ch);
    }

    // ---------- Retrabalho ----------

    void AgendarRetorno(Chamado ch, string causa)
    {
        int hoje = DiaManager.Instance != null ? DiaManager.Instance.dia : 1;
        var r = new RetornoPendente
        {
            id = dados.proximoRetorno++,
            cliente = ch.nomeCliente, contato = ch.contato,
            equipamento = ch.problema != null ? ch.problema.equipamento : ch.equipamento,
            solucao = ch.problema != null ? ch.problema.solucao : ch.solucao, dificuldade = ch.dificuldade,
            personalidade = ch.personalidade, exigeNota = ch.exigeNota,
            diaRetorno = hoje + Random.Range(1, 4),
            motivo = causa, numeroOS = ch.os != null ? ch.os.numero : 0
        };
        dados.retornos.Add(r);
        Debug.Log($"[MissionManager] Causa não resolvida: {ch.nomeCliente} vai chamar de novo no dia {r.diaRetorno} ({causa}).");
    }

    // Retornos que vencem hoje (ou antes) viram chamados urgentes em garantia
    void AbrirRetornosDoDia()
    {
        int hoje = DiaManager.Instance != null ? DiaManager.Instance.dia : 1;
        foreach (var r in dados.retornos)
        {
            if (r.diaRetorno > hoje) continue;
            if (chamadosDisponiveis.Exists(c => c.retrabalho && c.retornoId == r.id)) continue;
            if (chamadoAtivo != null && chamadoAtivo.retrabalho && chamadoAtivo.retornoId == r.id) continue;
            var ch = CriarRetorno(r);
            if (ch == null) continue;
            chamadosDisponiveis.Insert(0, ch);
            if (!r.avisado)
            {
                r.avisado = true;
                ReputationManager.Instance?.AlterarReputacao(-penalidadeRetorno);
                GameUI.Instance?.Toast($"<b>{r.cliente}</b> ligou: \"{r.motivo}!\" Retorno em garantia, urgente " +
                                       $"(-{penalidadeRetorno:F0} reputação).", UIFactory.Perigo, 6f);
            }
        }
    }

    public Chamado CriarRetorno(RetornoPendente r)
    {
        var p = CatalogoProblemas.Buscar(r.equipamento, r.solucao);
        if (p == null) return null;
        var ch = new Chamado(r.cliente, $"Voltou o mesmo problema! {p.descricao}", TipoDo(p), r.dificuldade, 0f, SLA.Horas(PrioridadeSLA.Urgente) * 60f)
        {
            equipamento = p.equipamento, solucao = p.solucao, problema = p,
            personalidade = r.personalidade, contato = r.contato, exigeNota = false,
            prioridade = PrioridadeSLA.Urgente, abertoEm = SLA.Agora(),
            retrabalho = true, retornoId = r.id
        };
        ch.anotacoes.Add($"Retorno da OS {r.numeroOS}: {r.motivo}.");
        return ch;
    }

    // Etapa 9B: sem teleporte. O endereço do cliente vai para o GPS do celular e o técnico vai andando.
    // Atalhos de teste (editor) ainda levam direto à porta: ver TeleporteDeTeste.
    public static bool TeleporteDeTeste;

    void IrParaCliente()
    {
        var sala = chamadoAtivo != null ? SalaCliente.Para(chamadoAtivo.nomeCliente) : null;
        if (TeleporteDeTeste)
        {
            TeleportarParaCliente(sala);
            return;
        }
        if (sala != null && GPS.Instance != null)
        {
            var e = Bairro.De(Bairro.DoTipoSala(sala.tipo));
            e.nome = chamadoAtivo.nomeCliente;   // o GPS mostra o nome do cliente, no endereço da sala dele
            GPS.Instance.Definir(e);
            return;
        }
        TeleportarParaCliente(sala);   // cena antiga, sem o bairro: o jeito de antes
    }

    void TeleportarParaCliente(SalaCliente sala)
    {
        if (jogador == null)
            jogador = GameObject.FindWithTag("Player").transform;

        // Cada cliente tem a sua sala (contabilidade, clínica ou loja); cena antiga: o local único
        Transform destino = sala != null && sala.chegada != null ? sala.chegada
            : locaisClientes != null && locaisClientes.Length > 0 ? locaisClientes[0] : null;
        if (destino == null) return;
        Teleporte.MoverJogador(jogador, destino); // posição e direção do olhar
        Debug.Log($"[MissionManager] Teleportado para: {(sala != null ? sala.Nome : "local do cliente")} {destino.position}");
    }
}
