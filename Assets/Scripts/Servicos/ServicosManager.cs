using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[System.Serializable]
public class ContratoServico
{
    public int id;
    public TipoServico tipo;
    public string empresa;
    public int unidades;
    public int meses;
    public float mensal;
    public float implantacao;
    public int diaAssinatura;
    public int diaImplantacao = -1;
    public int diaFim;
    public bool ativo = true;
    public float satisfacao = 75f;

    // Backup
    public bool backupOk = true;
    public int diasBackupFalhando;
    public int mesUltimoTeste = -99;

    public bool Implantado => diaImplantacao > 0;
}

[System.Serializable]
public class LeadServico
{
    public int id;
    public TipoServico tipo;
    public string empresa;
    public int unidades;
    public float disposicao;
    public int expiraNoDia;
}

// Alerta da central de monitoramento: algo para resolver à distância (ou com visita) antes de virar problema
[System.Serializable]
public class Alerta
{
    public int id;
    public int contratoId;
    public string empresa;
    public string titulo;
    public string detalhe;
    public int minutos;          // tempo para resolver
    public bool visita;          // precisa ir até o cliente
    public int criadoNoDia;
    public bool critico;
    public string acao;          // efeito especial ao resolver ("teste-backup", "reexecutar-backup", "troca-equipamento"...)
}

// Serviços recorrentes (suporte gerenciado, e-mail/Office, backup, bot, locação de notebook e mini PC)
// e a central de monitoramento que gera os alertas do dia a dia.
public class ServicosManager : MonoBehaviour
{
    public static ServicosManager Instance;

    public List<ContratoServico> contratos = new List<ContratoServico>();
    public List<LeadServico> leads = new List<LeadServico>();
    public List<Alerta> alertas = new List<Alerta>();
    public int proximoId = 100000;   // ids separados dos contratos de impressora
    public int ultimoMesFaturado = -1;

    // Estoque de notebooks e mini PCs: comprados (chegam no dia seguinte), prontos (preparados na bancada)
    public int notebooksNovos, notebooksProntos, miniPcsNovos, miniPcsProntos;
    public List<int> chegadasNotebook = new List<int>();   // dia em que cada um chega
    public List<int> chegadasMiniPc = new List<int>();

    [Header("Ajustes")]
    public float chanceLeadPorDia = 0.25f;
    public float chanceRansomwarePorDia = 0.004f;   // por cliente com suporte

    EventBinding<DiaIniciadoEvent> bDia;

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
        new GameObject("ServicosManager").AddComponent<ServicosManager>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (dadosSalvos != null) { Carregar(dadosSalvos); dadosSalvos = null; }
    }

    void OnDestroy() { if (Instance == this) Instance = null; }

    void OnEnable()
    {
        bDia = new EventBinding<DiaIniciadoEvent>(e => NovoDia(e.dia));
        EventBus<DiaIniciadoEvent>.Register(bDia);
    }

    void OnDisable() => EventBus<DiaIniciadoEvent>.Deregister(bDia);

    static int Hoje => DiaManager.Instance != null ? DiaManager.Instance.dia : 1;
    static void Avisar(string texto, Color cor, float dur = 3.5f) => GameUI.Instance?.Toast(texto, cor, dur);

    // ---------- Liberação por porte da empresa ----------

    public bool Liberado(TipoServico t, out string motivo)
    {
        var emp = EmpresaManager.Instance;
        motivo = null;
        if (emp == null) return true;
        if (!emp.TemCnpj) { motivo = "Precisa de CNPJ (abra o MEI na aba Empresa)."; return false; }
        return Parcerias.LiberaServico(t, out motivo);
    }

    public float Mrr
    {
        get { float t = 0f; foreach (var c in contratos) if (c.ativo && c.Implantado) t += c.mensal; return t; }
    }

    public ContratoServico Contrato(int id) => contratos.Find(c => c.id == id);
    public bool PossuiContrato(int id) => Contrato(id) != null;

    // ---------- Leads e propostas ----------

    void GerarLead()
    {
        var possiveis = new List<TipoServico>();
        foreach (var s in CatalogoServicos.Servicos) if (!s.soPorProjeto && Liberado(s.tipo, out _)) possiveis.Add(s.tipo);
        if (possiveis.Count == 0) return;
        var tipo = possiveis[Random.Range(0, possiveis.Count)];
        int unidades;
        switch (tipo)
        {
            case TipoServico.Suporte: unidades = Random.Range(3, 26); break;
            case TipoServico.Email: unidades = Random.Range(3, 31); break;
            case TipoServico.Backup: unidades = new[] { 50, 100, 200, 300, 500, 700 }[Random.Range(0, 6)]; break;
            case TipoServico.Bot: unidades = 1; break;
            case TipoServico.Notebook: unidades = Random.Range(1, 9); break;
            case TipoServico.Erp: unidades = Random.Range(3, 21); break;
            default: unidades = Random.Range(1, 11); break;
        }
        var l = new LeadServico
        {
            id = proximoId++, tipo = tipo, empresa = CatalogoServicos.SortearEmpresa(), unidades = unidades,
            disposicao = Random.Range(0.85f, 1.25f), expiraNoDia = Hoje + Random.Range(3, 7)
        };
        leads.Add(l);
        Avisar($"Novo contato: <b>{l.empresa}</b> quer {CatalogoServicos.Info(tipo).nome.ToLower()} ({DescreverUnidades(tipo, unidades)}).", UIFactory.Destaque, 4f);
    }

    public static string DescreverUnidades(TipoServico t, int u)
    {
        var info = CatalogoServicos.Info(t);
        if (t == TipoServico.Backup) return $"{u} GB";
        if (t == TipoServico.Bot) return "1 bot";
        return $"{u} {info.unidade}{(u > 1 ? "s" : "")}";
    }

    // Envia proposta com desconto (0 a 0,15). Devolve o resultado.
    public string EnviarProposta(LeadServico l, int prazoIndice, float desconto)
    {
        if (!Liberado(l.tipo, out string motivo)) return motivo;
        var info = CatalogoServicos.Info(l.tipo);
        int meses = info.porPrazo ? new[] { 12, 24, 36 }[Mathf.Clamp(prazoIndice, 0, 2)] : 12;
        float tabela = CatalogoServicos.Mensal(l.tipo, l.unidades, meses);
        float oferta = Mathf.Round(tabela * (1f - Mathf.Clamp(desconto, 0f, 0.3f)) * 100f) / 100f;
        float rep = ReputationManager.Instance != null ? ReputationManager.Instance.GetReputacao() : 30f;
        float limite = tabela * l.disposicao * (1f + (rep - 50f) / 500f);
        var rival = Concorrencia.Sortear();
        float concorrente = Concorrencia.Oferta(rival, tabela);
        leads.Remove(l);

        if (oferta > limite)
        {
            Concorrencia.Registrar(rival, false);
            return $"<b>{l.empresa}</b> achou caro ({UIFactory.Dinheiro(oferta)}/mês) e fechou com a {rival.nome}.";
        }
        if (oferta > concorrente * (Concorrencia.VantagemJogador(rep) + FocoEmpresa.Vantagem(FocoEmpresa.AreaDoServico(l.tipo))))
        {
            Concorrencia.Registrar(rival, false);
            return $"<b>{l.empresa}</b> fechou com a {rival.nome} por {UIFactory.Dinheiro(concorrente)}/mês (a sua saiu {UIFactory.Dinheiro(oferta)}).";
        }
        Concorrencia.Registrar(rival, true);

        var c = new ContratoServico
        {
            id = proximoId++, tipo = l.tipo, empresa = l.empresa, unidades = l.unidades, meses = meses,
            mensal = oferta, implantacao = CatalogoServicos.Implantacao(l.tipo, l.unidades),
            diaAssinatura = Hoje, diaFim = Hoje + meses * 30
        };
        contratos.Add(c);
        return $"Fechado: <b>{l.empresa}</b> — {info.nome}, {UIFactory.Dinheiro(oferta)}/mês! Agora faça a implantação (aba Contratos).";
    }

    // Contrato fechado fora do fluxo de proposta (licitação): ainda precisa ser implantado
    public ContratoServico CriarContrato(TipoServico t, string empresa, int unidades, int meses, float mensal, float implantacao)
    {
        var c = new ContratoServico
        {
            id = proximoId++, tipo = t, empresa = empresa, unidades = unidades, meses = meses, mensal = mensal,
            implantacao = implantacao, diaAssinatura = Hoje, diaFim = Hoje + meses * 30
        };
        contratos.Add(c);
        return c;
    }

    // Contrato que nasce já implantado (firewall entregue por um projeto)
    public ContratoServico CriarContratoImplantado(TipoServico t, string empresa, int unidades, int meses, float mensal)
    {
        var c = new ContratoServico
        {
            id = proximoId++, tipo = t, empresa = empresa, unidades = unidades, meses = meses, mensal = mensal,
            diaAssinatura = Hoje, diaImplantacao = Hoje, diaFim = Hoje + meses * 30
        };
        contratos.Add(c);
        return c;
    }

    // ---------- Equipamentos (notebook / mini PC) ----------

    public string ComprarEquipamento(TipoServico t)
    {
        var info = CatalogoServicos.Info(t);
        if (!info.exigeEquipamento) return "Esse serviço não usa equipamento.";
        if (!Liberado(t, out string motivo)) return motivo;
        float preco = PrecoEquipamento(t);
        var eco = GameEconomy.Instance;
        if (eco != null && eco.saldo < preco) return $"Saldo insuficiente: {UIFactory.Dinheiro(preco)}.";
        eco?.Gastar(preco);
        (t == TipoServico.Notebook ? chegadasNotebook : chegadasMiniPc).Add(Hoje + 1);
        return $"{info.unidade} comprado por {UIFactory.Dinheiro(preco)}. Chega amanhã; prepare na bancada antes de entregar.";
    }

    // Preço de compra com o desconto do nível de parceria com o distribuidor
    public static float PrecoEquipamento(TipoServico t) => Mathf.Round(CatalogoServicos.Info(t).custoEquipamento * (1f - Parcerias.DescontoServico(t)));

    public int Novos(TipoServico t) => t == TipoServico.Notebook ? notebooksNovos : miniPcsNovos;
    public int Prontos(TipoServico t) => t == TipoServico.Notebook ? notebooksProntos : miniPcsProntos;

    // Chamado pela bancada depois do checklist de preparação
    public bool PrepararUnidade(TipoServico t)
    {
        if (t == TipoServico.Notebook) { if (notebooksNovos <= 0) return false; notebooksNovos--; notebooksProntos++; return true; }
        if (miniPcsNovos <= 0) return false;
        miniPcsNovos--; miniPcsProntos++;
        return true;
    }

    // ---------- Implantação e alertas (gastam tempo do dia) ----------

    public static bool GastarTempo(float minutos, out string motivo)
    {
        motivo = null;
        var d = DiaManager.Instance;
        if (MissionManager.Instance != null && MissionManager.Instance.chamadoAtivo != null)
        { motivo = "Termine o chamado em atendimento primeiro."; return false; }
        if (d == null) return true;
        if (d.diaEncerrado || d.ForaDoExpediente) { motivo = "Fora do expediente: deixe para amanhã."; return false; }
        d.minutoAtual += minutos;
        return true;
    }

    public static int MinutosImplantacao(ContratoServico c)
    {
        switch (c.tipo)
        {
            case TipoServico.Suporte: return 30 + 10 * c.unidades;
            case TipoServico.Email: return 20 + 15 * c.unidades;
            case TipoServico.Backup: return 120;
            case TipoServico.Bot: return 180;
            case TipoServico.Erp: return 240;
            default: return 60;   // entrega
        }
    }

    public string Implantar(ContratoServico c)
    {
        if (c.Implantado) return "Já está implantado.";
        var info = CatalogoServicos.Info(c.tipo);
        if (info.exigeEquipamento && Prontos(c.tipo) < c.unidades)
            return $"Você precisa de {c.unidades} {info.unidade}(s) prontos (preparados na bancada). Tem {Prontos(c.tipo)}.";
        if (!GastarTempo(MinutosImplantacao(c), out string motivo)) return motivo;
        if (c.tipo == TipoServico.Notebook) notebooksProntos -= c.unidades;
        if (c.tipo == TipoServico.MiniPc) miniPcsProntos -= c.unidades;
        c.diaImplantacao = Hoje;
        Cobrar(c, c.implantacao, "Implantação: " + info.nome);
        return $"{info.nome} implantado na <b>{c.empresa}</b>. Implantação cobrada: {UIFactory.Dinheiro(c.implantacao)}.";
    }

    public string ResolverAlerta(Alerta a)
    {
        if (!alertas.Contains(a)) return "Esse alerta já foi resolvido.";
        var c = Contrato(a.contratoId);
        if (a.acao == "troca-equipamento" && c != null && Prontos(c.tipo) <= 0)
            return $"Você não tem {CatalogoServicos.Info(c.tipo).unidade} pronto para trocar. Prepare um na bancada.";
        if (!GastarTempo(Mathf.Round(a.minutos * FocoEmpresa.MultMinutosAlerta), out string motivo)) return motivo;
        alertas.Remove(a);
        if (c == null) return "Resolvido.";

        switch (a.acao)
        {
            case "reexecutar-backup": c.backupOk = true; c.diasBackupFalhando = 0; break;
            case "teste-backup": c.mesUltimoTeste = EmpresaManager.MesDoDia(Hoje); break;
            case "nova-conta": c.unidades++; c.mensal += CatalogoServicos.PrecoUnidade(c.tipo, c.unidades, c.meses); break;
            case "troca-equipamento":
                if (c.tipo == TipoServico.Notebook) { notebooksProntos--; notebooksNovos++; } else { miniPcsProntos--; miniPcsNovos++; }
                GameEconomy.Instance?.Gastar(200f);   // conserto do equipamento que voltou
                break;
        }
        c.satisfacao = Mathf.Min(100f, c.satisfacao + (Hoje == a.criadoNoDia ? 4f : 2f));
        return $"Resolvido: {a.titulo} ({c.empresa})." + (a.acao == "troca-equipamento" ? " O equipamento com defeito voltou para conserto (R$ 200) e precisa ser preparado de novo." : "");
    }

    // Chamado de contrato de serviço encerrado (o MissionManager avisa todos os gerenciadores; cada um checa o id)
    public void ChamadoContratoEncerrado(int contratoId, bool resolvido, bool rapido)
    {
        var c = Contrato(contratoId);
        if (c == null) return;
        c.satisfacao = resolvido ? Mathf.Min(100f, c.satisfacao + (rapido ? 8f : 3f)) : Mathf.Max(0f, c.satisfacao - 10f);
    }

    void NovoAlerta(ContratoServico c, string titulo, string detalhe, int minutos, bool visita = false, bool critico = false, string acao = null)
    {
        if (alertas.Exists(a => a.contratoId == c.id && a.titulo == titulo)) return;
        alertas.Add(new Alerta
        {
            id = proximoId++, contratoId = c.id, empresa = c.empresa, titulo = titulo, detalhe = detalhe,
            minutos = minutos, visita = visita, criadoNoDia = Hoje, critico = critico, acao = acao
        });
    }

    // ---------- Dia a dia ----------

    void NovoDia(int dia)
    {
        Chegadas(chegadasNotebook, ref notebooksNovos, "notebook");
        Chegadas(chegadasMiniPc, ref miniPcsNovos, "mini PC");

        leads.RemoveAll(l => l.expiraNoDia < dia);
        float multFoco = Mathf.Max(FocoEmpresa.MultLeads(AreaNegocio.Gerenciados), FocoEmpresa.MultLeads(AreaNegocio.Software));
        if (Random.value < chanceLeadPorDia * Concorrencia.FatorDemanda() * multFoco && leads.Count < 4)
        {
            var emp = EmpresaManager.Instance;
            if (emp == null || emp.TemCnpj) GerarLead();
        }

        var data = DiaManager.DataDoDia(dia);
        bool util = data.DayOfWeek != System.DayOfWeek.Saturday && data.DayOfWeek != System.DayOfWeek.Sunday;

        foreach (var c in contratos)
        {
            if (!c.ativo || !c.Implantado) continue;
            if (util) GerarAlertas(c, dia);
        }

        // Alertas esquecidos: o cliente percebe o problema
        foreach (var a in new List<Alerta>(alertas))
        {
            var c = Contrato(a.contratoId);
            if (c == null || !c.ativo) { alertas.Remove(a); continue; }
            int idade = dia - a.criadoNoDia;
            if (idade >= 2) c.satisfacao = Mathf.Max(0f, c.satisfacao - (a.critico ? 8f : 3f));
            if (idade == 4)
            {
                Avisar($"<b>{c.empresa}</b> reclamou: \"{a.titulo}\" está sem solução há dias.", UIFactory.Perigo, 4f);
                // No suporte gerenciado, o problema esquecido vira chamado presencial (incluso no contrato)
                if (c.tipo == TipoServico.Suporte) { alertas.Remove(a); MissionManager.Instance?.AdicionarChamadoDeContrato(c.id, c.empresa, DificuldadeChamado.Medio); }
            }
        }

        // Backup falhando direto: dias sem cópia
        foreach (var c in contratos)
            if (c.ativo && c.tipo == TipoServico.Backup && !c.backupOk) c.diasBackupFalhando++;

        // Ransomware: quem tem suporte gerenciado pode ser atacado; o backup decide o final
        foreach (var c in new List<ContratoServico>(contratos))
            if (c.ativo && c.Implantado && c.tipo == TipoServico.Suporte && util && Random.value < chanceRansomwarePorDia)
                Ransomware(c);

        // Fechamento do mês
        int mesAtual = EmpresaManager.MesDoDia(dia);
        while (ultimoMesFaturado < mesAtual - 1)
        {
            ultimoMesFaturado++;
            Faturar(ultimoMesFaturado);
        }

        foreach (var c in contratos)
        {
            if (!c.ativo) continue;
            if (c.satisfacao < 20f) Encerrar(c, $"<b>{c.empresa}</b> cancelou {CatalogoServicos.Info(c.tipo).nome.ToLower()}: estava muito insatisfeita.");
            else if (dia >= c.diaFim)
            {
                if (c.satisfacao >= 60f) { c.diaFim = dia + c.meses * 30; Avisar($"<b>{c.empresa}</b> renovou {CatalogoServicos.Info(c.tipo).nome.ToLower()}!", UIFactory.Sucesso); }
                else Encerrar(c, $"<b>{c.empresa}</b> não renovou {CatalogoServicos.Info(c.tipo).nome.ToLower()}.");
            }
        }
    }

    void Chegadas(List<int> lista, ref int novos, string nome)
    {
        int chegou = lista.RemoveAll(d => d <= Hoje);
        if (chegou > 0)
        {
            novos += chegou;
            Avisar($"Chegaram {chegou} {nome}(s). Prepare na bancada antes de entregar.", UIFactory.Sucesso);
        }
    }

    void GerarAlertas(ContratoServico c, int dia)
    {
        float r = Random.value;
        switch (c.tipo)
        {
            case TipoServico.Suporte:
                if (r < 0.03f * c.unidades)
                {
                    int q = Random.Range(0, 4);
                    if (q == 0) NovoAlerta(c, "Disco quase cheio", "Uma máquina está com 95% do disco ocupado.", 20);
                    else if (q == 1) NovoAlerta(c, "Antivírus bloqueou uma ameaça", "Arquivo suspeito em quarentena; verificar a máquina.", 25, critico: true);
                    else if (q == 2) NovoAlerta(c, "Máquina sem atualizações", "Uma máquina não atualiza o Windows há 30 dias.", 30);
                    else NovoAlerta(c, "Computador sem comunicar", "Uma máquina parou de responder ao monitoramento.", 60, visita: true);
                }
                break;
            case TipoServico.Email:
                if (r < 0.02f * c.unidades)
                {
                    int q = Random.Range(0, 3);
                    if (q == 0) NovoAlerta(c, "Usuário esqueceu a senha", "Resetar a senha e reconfigurar a verificação em duas etapas (MFA).", 10);
                    else if (q == 1) NovoAlerta(c, "Caixa de e-mail cheia", "Arquivar mensagens antigas e liberar espaço.", 20);
                    else NovoAlerta(c, "Novo funcionário: criar conta", "Criar usuário, atribuir licença e configurar o Outlook (a conta entra na mensalidade).", 30, acao: "nova-conta");
                }
                break;
            case TipoServico.Backup:
                if (c.backupOk && r < 0.06f)
                {
                    c.backupOk = false;
                    NovoAlerta(c, "Backup da noite falhou", "O job de backup não terminou. Verificar e executar de novo.", 15, critico: true, acao: "reexecutar-backup");
                }
                int mes = EmpresaManager.MesDoDia(dia);
                if (c.mesUltimoTeste < mes && DiaManager.DataDoDia(dia).Day >= 20)
                    NovoAlerta(c, "Teste de restauração do mês", "Restaurar alguns arquivos para garantir que o backup funciona.", 30, acao: "teste-backup");
                break;
            case TipoServico.Firewall:
                if (r < 0.05f)
                {
                    int q = Random.Range(0, 4);
                    if (q == 0) NovoAlerta(c, "Ataque bloqueado", "Muitas tentativas de acesso de fora. Revisar o log e bloquear os IPs.", 20, critico: true);
                    else if (q == 1) NovoAlerta(c, "VPN não conecta", "Funcionário em home office não consegue conectar na VPN.", 25);
                    else if (q == 2) NovoAlerta(c, "Liberar site bloqueado", "O filtro de conteúdo bloqueou um site que o cliente usa.", 10);
                    else NovoAlerta(c, "Atualizar firmware do firewall", "Atualização de segurança disponível. Agendar e aplicar.", 40);
                }
                break;
            case TipoServico.Erp:
                if (r < ProdutosProprios.ChanceBug(ProdutosProprios.Erp))
                {
                    int q = Random.Range(0, 3);
                    if (q == 0) NovoAlerta(c, "Bug no sistema", "Erro ao emitir nota fiscal no ERP. Corrigir e avisar o cliente.", 45, critico: true);
                    else if (q == 1) NovoAlerta(c, "Cliente pediu relatório novo", "Montar um relatório de vendas por vendedor.", 40);
                    else NovoAlerta(c, "Treinar usuário novo", "Funcionário novo do cliente precisa aprender o sistema.", 30);
                }
                break;
            case TipoServico.Bot:
                if (r < 0.03f) NovoAlerta(c, "Cliente quer mudar o menu do bot", "Ajustar o fluxo de atendimento no editor.", 30);
                break;
            default:
                if (r < 0.006f * c.unidades)
                    NovoAlerta(c, $"{CatalogoServicos.Info(c.tipo).unidade} com defeito", "Levar uma unidade pronta e trocar no cliente.", 60, visita: true, critico: true, acao: "troca-equipamento");
                break;
        }
    }

    void Ransomware(ContratoServico suporte)
    {
        var backup = contratos.Find(x => x.ativo && x.Implantado && x.tipo == TipoServico.Backup && x.empresa == suporte.empresa);
        int mes = EmpresaManager.MesDoDia(Hoje);
        if (backup != null && backup.backupOk && backup.mesUltimoTeste >= mes - 1)
        {
            float horas = 4f * CatalogoServicos.HoraTecnica;
            Cobrar(suporte, horas, "Recuperação após ataque de ransomware");
            suporte.satisfacao = Mathf.Min(100f, suporte.satisfacao + 15f);
            backup.satisfacao = Mathf.Min(100f, backup.satisfacao + 15f);
            ReputationManager.Instance?.AlterarReputacao(3f);
            Conquistas.Desbloquear("heroi");
            Avisar($"<b>Ransomware na {suporte.empresa}!</b> Você restaurou tudo do backup testado. Herói do dia (+3 reputação, 4 h técnicas cobradas).", UIFactory.Sucesso, 6f);
        }
        else if (backup != null)
        {
            suporte.satisfacao -= 25f; backup.satisfacao -= 30f;
            ReputationManager.Instance?.AlterarReputacao(-3f);
            Avisar($"<b>Ransomware na {suporte.empresa}!</b> O backup estava falhando ou sem teste: parte dos dados se perdeu (-3 reputação).", UIFactory.Perigo, 6f);
        }
        else
        {
            suporte.satisfacao -= 35f;
            ReputationManager.Instance?.AlterarReputacao(-2f);
            Avisar($"<b>Ransomware na {suporte.empresa}!</b> Sem backup, os arquivos foram perdidos (-2 reputação). Ofereça backup aos seus clientes.", UIFactory.Perigo, 6f);
            if (Liberado(TipoServico.Backup, out _))
                leads.Add(new LeadServico { id = proximoId++, tipo = TipoServico.Backup, empresa = suporte.empresa, unidades = 200, disposicao = 1.3f, expiraNoDia = Hoje + 7 });
        }
    }

    void Faturar(int mes)
    {
        float custos = 0f;
        foreach (var c in contratos)
        {
            if (!c.ativo || !c.Implantado) continue;
            var inicioMes = EmpresaManager.PrimeiroDiaDoMes(mes);
            int diaInicio = EmpresaManager.DiaDaData(inicioMes);
            int dias = System.DateTime.DaysInMonth(inicioMes.Year, inicioMes.Month);
            float fracao = Mathf.Clamp01((diaInicio + dias - Mathf.Max(c.diaImplantacao, diaInicio)) / (float)dias);
            Cobrar(c, Mathf.Round(c.mensal * fracao * 100f) / 100f, CatalogoServicos.Info(c.tipo).nome);
            custos += CatalogoServicos.CustoMensal(c.tipo, c.unidades) * fracao * (1f - Parcerias.DescontoServico(c.tipo));
            if (c.tipo == TipoServico.Bot) custos += Random.Range(0, 1500) * 0.043f;   // mensagens além das 1.000 grátis
        }
        if (custos > 0.01f)
        {
            GameEconomy.Instance?.Gastar(Mathf.Round(custos * 100f) / 100f);
            Avisar($"Fornecedores de licenças e nuvem cobrados: {UIFactory.Dinheiro(custos)}.", UIFactory.Alerta);
        }
    }

    void Cobrar(ContratoServico c, float valor, string servico)
    {
        if (valor <= 0f) return;
        var ch = new Chamado(c.empresa, servico, TipoChamado.Rede, DificuldadeChamado.Facil, valor, 0f) { exigeNota = true };
        if (EmpresaManager.Instance != null) EmpresaManager.Instance.RegistrarServico(ch, valor, true);
        else GameEconomy.Instance?.ReceberPagamento(valor);
    }

    void Encerrar(ContratoServico c, string msg)
    {
        c.ativo = false;
        alertas.RemoveAll(a => a.contratoId == c.id);
        if (c.tipo == TipoServico.Notebook) notebooksNovos += c.unidades;   // equipamentos voltam para revisão
        if (c.tipo == TipoServico.MiniPc) miniPcsNovos += c.unidades;
        Avisar(msg, UIFactory.Perigo, 5f);
    }

    // ---------- Save ----------

    [System.Serializable]
    public class Dados
    {
        public List<ContratoServico> contratos = new List<ContratoServico>();
        public List<LeadServico> leads = new List<LeadServico>();
        public List<Alerta> alertas = new List<Alerta>();
        public int proximoId = 100000, ultimoMesFaturado = -1;
        public int notebooksNovos, notebooksProntos, miniPcsNovos, miniPcsProntos;
        public List<int> chegadasNotebook = new List<int>(), chegadasMiniPc = new List<int>();
    }

    public static Dados dadosSalvos;

    public Dados Exportar() => new Dados
    {
        contratos = new List<ContratoServico>(contratos), leads = new List<LeadServico>(leads), alertas = new List<Alerta>(alertas),
        proximoId = proximoId, ultimoMesFaturado = ultimoMesFaturado,
        notebooksNovos = notebooksNovos, notebooksProntos = notebooksProntos, miniPcsNovos = miniPcsNovos, miniPcsProntos = miniPcsProntos,
        chegadasNotebook = new List<int>(chegadasNotebook), chegadasMiniPc = new List<int>(chegadasMiniPc)
    };

    public void Carregar(Dados d)
    {
        if (d == null) { NovoJogo(); return; }
        contratos = d.contratos ?? new List<ContratoServico>();
        leads = d.leads ?? new List<LeadServico>();
        alertas = d.alertas ?? new List<Alerta>();
        proximoId = Mathf.Max(100000, d.proximoId); ultimoMesFaturado = d.ultimoMesFaturado;
        notebooksNovos = d.notebooksNovos; notebooksProntos = d.notebooksProntos;
        miniPcsNovos = d.miniPcsNovos; miniPcsProntos = d.miniPcsProntos;
        chegadasNotebook = d.chegadasNotebook ?? new List<int>(); chegadasMiniPc = d.chegadasMiniPc ?? new List<int>();
    }

    public void NovoJogo()
    {
        contratos.Clear(); leads.Clear(); alertas.Clear(); proximoId = 100000; ultimoMesFaturado = -1;
        notebooksNovos = notebooksProntos = miniPcsNovos = miniPcsProntos = 0;
        chegadasNotebook.Clear(); chegadasMiniPc.Clear();
    }
}
