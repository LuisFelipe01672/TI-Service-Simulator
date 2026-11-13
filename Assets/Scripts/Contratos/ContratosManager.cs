using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Um equipamento da empresa (patrimônio): fica no estoque até ser instalado num cliente
[System.Serializable]
public class EquipamentoPatrimonio
{
    public int id;
    public string tipoId;
    public int condicao;          // índice em CatalogoLocacao.Condicoes
    public float custoPago;
    public int diaCompra;
    public int diaChegada;        // chega do fornecedor nesse dia
    public int contratoId = -1;   // -1 = no estoque
    public float desgaste;        // 0 a 1 (equipamento velho dá mais defeito)
    public TipoEquipamentoLocacao Tipo => CatalogoLocacao.Tipo(tipoId);
}

// Uma empresa interessada em alugar impressora
[System.Serializable]
public class Lead
{
    public int id;
    public string empresa;
    public int paginasMes;
    public float fracCor;         // fração das páginas em cor
    public float disposicao;      // quanto aceita pagar em relação ao "preço justo" (0,9 a 1,3)
    public bool grafica;          // gráfica: volume altíssimo (as tabelas reais excluem gráficas)
    public int expiraNoDia;
}

// Um contrato de locação de impressora
[System.Serializable]
public class ContratoLocacao
{
    public int id;
    public string empresa;
    public int equipamentoId;
    public int planoIndice;
    public int meses;
    public float mensal;          // já com desconto do prazo
    public int franquia;
    public float excedentePB, excedenteCor;
    public int paginasMes;
    public float fracCor;
    public int fornecedorSuprimento;

    public int diaAssinatura;
    public int diaInstalacao = -1;     // -1 = ainda não instalado
    public int diaFim;
    public bool ativo = true;

    public float satisfacao = 75f;      // 0 a 100
    public float suprimento = 1f;       // nível de tinta/toner (0 a 1)
    public int diasSemSuprimento;
    public int paginasNoMes;            // contador do mês corrente
    public int contadorTotal;
    public int mesLeitura = -99;        // último mês em que o contador foi lido
    public int paginasLidas;            // páginas do mês registradas na leitura
    public bool chamadoAberto;

    public bool Instalado => diaInstalacao > 0;
}

// Contratos de locação de impressoras (outsourcing de impressão):
// leads → proposta (equipamento, plano, prazo) → disputa com concorrente → instalação →
// todo mês: fixo + excedente (só se o contador foi lido) → suprimentos, defeitos, satisfação → renovação ou cancelamento.
public class ContratosManager : MonoBehaviour
{
    public static ContratosManager Instance;

    public List<EquipamentoPatrimonio> patrimonio = new List<EquipamentoPatrimonio>();
    public List<Lead> leads = new List<Lead>();
    public List<ContratoLocacao> contratos = new List<ContratoLocacao>();
    public int proximoId = 1;
    public int ultimoMesFaturado = -1;

    [Header("Ajustes")]
    public float chanceLeadPorDia = 0.45f;
    public float minutosVisita = 90f;
    public float minutosInstalacao = 120f;
    public float chanceDefeitoBase = 0.004f;   // por dia, por equipamento

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
        new GameObject("ContratosManager").AddComponent<ContratosManager>();
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

    public bool Liberado => EmpresaManager.Instance == null || EmpresaManager.Instance.TemCnpj;

    // Receita recorrente mensal (fixo dos contratos ativos e instalados)
    public float Mrr
    {
        get { float t = 0f; foreach (var c in contratos) if (c.ativo && c.Instalado) t += c.mensal; return t; }
    }

    public EquipamentoPatrimonio Equip(int id) => patrimonio.Find(e => e.id == id);

    // ---------- Compras ----------

    public string ComprarEquipamento(string tipoId, int condicao)
    {
        var t = CatalogoLocacao.Tipo(tipoId);
        var c = CatalogoLocacao.Condicao(condicao);
        float preco = Mathf.Round(t.custoCompra * c.multCusto);
        var eco = GameEconomy.Instance;
        if (eco != null && eco.saldo < preco) return $"Saldo insuficiente: {UIFactory.Dinheiro(preco)}.";
        eco?.Gastar(preco);
        var e = new EquipamentoPatrimonio
        {
            id = proximoId++, tipoId = tipoId, condicao = condicao, custoPago = preco,
            diaCompra = Hoje, diaChegada = Hoje + 1,
            desgaste = condicao == 0 ? 0f : condicao == 1 ? 0.35f : 0.55f
        };
        patrimonio.Add(e);
        return $"{t.nome} ({c.nome}) comprada por {UIFactory.Dinheiro(preco)}. Chega amanhã.";
    }

    public List<EquipamentoPatrimonio> EmEstoque(string tipoId) =>
        patrimonio.FindAll(e => e.tipoId == tipoId && e.contratoId < 0 && e.diaChegada <= Hoje);

    // ---------- Leads e propostas ----------

    void GerarLead()
    {
        bool grafica = Random.value < 0.08f;
        var l = new Lead
        {
            id = proximoId++,
            empresa = grafica ? "Gráfica Expressa" : CatalogoLocacao.SortearEmpresa(),
            paginasMes = grafica ? Random.Range(8000, 15000) : Mathf.RoundToInt(Random.Range(300f, 5000f) / 50f) * 50,
            fracCor = grafica ? Random.Range(0.4f, 0.7f) : Random.value < 0.35f ? 0f : Random.Range(0.05f, 0.35f),
            disposicao = Random.Range(0.9f, 1.3f),
            grafica = grafica,
            expiraNoDia = Hoje + Random.Range(3, 7)
        };
        leads.Add(l);
        Avisar($"Novo contato comercial: <b>{l.empresa}</b> quer alugar impressora (~{l.paginasMes} páginas/mês).", UIFactory.Destaque, 4f);
    }

    // Plano "justo" para o perfil: o mais barato que um vendedor honesto ofereceria
    public static float PrecoJusto(Lead l)
    {
        float melhor = float.MaxValue;
        foreach (var t in CatalogoLocacao.Equipamentos)
        {
            if (l.fracCor > 0.02f && !t.colorida) continue;
            foreach (var p in t.planos) melhor = Mathf.Min(melhor, CatalogoLocacao.ContaEsperada(p, 36, l.paginasMes, l.fracCor));
        }
        return melhor;
    }

    // Avaliação antes de enviar (mostrada ao jogador)
    public string AvaliarProposta(Lead l, string tipoId, int plano, int meses, int fornecedor, out float contaMes, out float custoMes)
    {
        var t = CatalogoLocacao.Tipo(tipoId);
        var p = t.planos[Mathf.Clamp(plano, 0, t.planos.Length - 1)];
        contaMes = CatalogoLocacao.ContaEsperada(p, meses, l.paginasMes, l.fracCor);
        custoMes = l.paginasMes * CatalogoLocacao.CustoPorPagina(t, l.fracCor, CatalogoLocacao.Fornecedor(fornecedor));
        if (l.fracCor > 0.02f && !t.colorida) return "Este cliente imprime colorido: uma impressora preto e branco não serve.";
        if (l.paginasMes > t.paginasRecomendadasMes * 1.5f) return "Volume bem acima do recomendado: o equipamento vai desgastar e quebrar mais.";
        return null;
    }

    // Envia a proposta. Devolve o resultado (o cliente compara com um concorrente).
    public string EnviarProposta(Lead l, string tipoId, int plano, int meses, int fornecedor)
    {
        if (!Liberado) return "Locação exige CNPJ: abra o MEI na aba Empresa.";
        var t = CatalogoLocacao.Tipo(tipoId);
        if (l.fracCor > 0.02f && !t.colorida) return "O cliente recusou: ele precisa imprimir colorido.";
        var estoque = EmEstoque(tipoId);
        if (estoque.Count == 0) return $"Você não tem {t.nome} em estoque. Compre uma antes de fechar.";

        var p = t.planos[Mathf.Clamp(plano, 0, t.planos.Length - 1)];
        float conta = CatalogoLocacao.ContaEsperada(p, meses, l.paginasMes, l.fracCor);
        float justo = PrecoJusto(l);
        float rep = ReputationManager.Instance != null ? ReputationManager.Instance.GetReputacao() : 30f;

        // O cliente topa até o preço justo × disposição (reputação ajuda um pouco);
        // o concorrente oferece algo perto do preço justo e o cliente escolhe o melhor custo-benefício.
        float limite = justo * l.disposicao * (1f + (rep - 50f) / 500f);
        float concorrente = justo * Random.Range(0.92f, 1.2f);
        string nomeConc = CatalogoLocacao.SortearConcorrente();
        leads.Remove(l);

        if (conta > limite)
            return $"<b>{l.empresa}</b> achou caro ({UIFactory.Dinheiro(conta)}/mês) e fechou com a {nomeConc} por {UIFactory.Dinheiro(concorrente)}/mês.";
        if (conta > concorrente * (1f + (rep - 50f) / 200f))
            return $"<b>{l.empresa}</b> fechou com a {nomeConc}: a proposta deles saiu {UIFactory.Dinheiro(concorrente)}/mês contra {UIFactory.Dinheiro(conta)} sua.";

        var e = estoque[0];
        var c = new ContratoLocacao
        {
            id = proximoId++, empresa = l.empresa, equipamentoId = e.id, planoIndice = plano, meses = meses,
            mensal = CatalogoLocacao.MensalComDesconto(p, meses), franquia = p.franquia,
            excedentePB = p.excedentePB, excedenteCor = p.excedenteCor,
            paginasMes = l.paginasMes, fracCor = l.fracCor, fornecedorSuprimento = fornecedor,
            diaAssinatura = Hoje, diaFim = Hoje + meses * 30
        };
        e.contratoId = c.id;
        contratos.Add(c);
        return $"Contrato fechado com <b>{l.empresa}</b>: {UIFactory.Dinheiro(c.mensal)}/mês por {meses} meses! Agora instale o equipamento (aba Contratos).";
    }

    // ---------- Visitas ----------

    // Passa o tempo da visita; devolve false se não dá (fora do expediente ou com chamado aberto)
    bool GastarTempo(float minutos, out string motivo)
    {
        motivo = null;
        var d = DiaManager.Instance;
        if (MissionManager.Instance != null && MissionManager.Instance.chamadoAtivo != null)
        { motivo = "Termine o chamado em atendimento antes de sair para uma visita."; return false; }
        if (d == null) return true;
        if (d.diaEncerrado || d.ForaDoExpediente) { motivo = "Fora do expediente: visite amanhã."; return false; }
        d.minutoAtual += minutos;
        return true;
    }

    public string Instalar(ContratoLocacao c)
    {
        if (c.Instalado) return "Já está instalado.";
        if (!GastarTempo(minutosInstalacao, out string motivo)) return motivo;
        c.diaInstalacao = Hoje;
        c.suprimento = 1f;
        // Implantação e frete são cobrados uma vez
        CobrarCliente(c, CatalogoLocacao.Implantacao + CatalogoLocacao.Frete, "Implantação e frete");
        return $"Equipamento instalado na <b>{c.empresa}</b>. Implantação + frete cobrados ({UIFactory.Dinheiro(CatalogoLocacao.Implantacao + CatalogoLocacao.Frete)}).";
    }

    // Visita de rotina: lê o contador e repõe tinta/toner (o custo do suprimento sai na hora)
    public string Visitar(ContratoLocacao c)
    {
        if (!c.Instalado) return "Instale o equipamento primeiro.";
        if (!GastarTempo(minutosVisita, out string motivo)) return motivo;
        var e = Equip(c.equipamentoId);
        var t = e != null ? e.Tipo : CatalogoLocacao.Equipamentos[0];
        int mes = EmpresaManager.MesDoDia(Hoje);

        c.mesLeitura = mes;
        c.paginasLidas = c.paginasNoMes;

        float faltando = 1f - c.suprimento;
        float custo = Mathf.Round(faltando * t.rendimentoRefil * CatalogoLocacao.CustoPorPagina(t, c.fracCor, CatalogoLocacao.Fornecedor(c.fornecedorSuprimento)) * 100f) / 100f;
        if (custo > 0f) GameEconomy.Instance?.Gastar(custo);
        c.suprimento = 1f;
        c.diasSemSuprimento = 0;
        c.satisfacao = Mathf.Min(100f, c.satisfacao + 3f);
        return $"Visita na <b>{c.empresa}</b>: contador em {c.contadorTotal} ({c.paginasNoMes} páginas este mês)." +
               (custo > 0f ? $" Suprimento reposto: {UIFactory.Dinheiro(custo)}." : "");
    }

    // ---------- Dia a dia ----------

    void NovoDia(int dia)
    {
        // Equipamentos que chegaram
        foreach (var e in patrimonio)
            if (e.diaChegada == dia) Avisar($"Chegou do fornecedor: {e.Tipo.nome} ({CatalogoLocacao.Condicao(e.condicao).nome}).", UIFactory.Sucesso);

        leads.RemoveAll(l => l.expiraNoDia < dia);
        if (Liberado && Random.value < chanceLeadPorDia && leads.Count < 4) GerarLead();

        foreach (var c in contratos)
        {
            if (!c.ativo || !c.Instalado) continue;
            SimularDia(c);
        }

        // Fechamento do mês: fatura todos os contratos
        int mesAtual = EmpresaManager.MesDoDia(dia);
        while (ultimoMesFaturado < mesAtual - 1)
        {
            ultimoMesFaturado++;
            Faturar(ultimoMesFaturado);
        }

        // Fim de contrato: renova ou vai embora
        foreach (var c in contratos)
        {
            if (!c.ativo || dia < c.diaFim) continue;
            if (c.satisfacao >= 60f)
            {
                c.diaFim = dia + c.meses * 30;
                Avisar($"<b>{c.empresa}</b> renovou o contrato por mais {c.meses} meses!", UIFactory.Sucesso, 4f);
            }
            else Encerrar(c, $"<b>{c.empresa}</b> não renovou o contrato (satisfação {c.satisfacao:F0}).");
        }
    }

    void SimularDia(ContratoLocacao c)
    {
        var e = Equip(c.equipamentoId);
        var t = e != null ? e.Tipo : CatalogoLocacao.Equipamentos[0];

        // Páginas do dia (dias úteis), com variação
        var data = DiaManager.DataDoDia(Hoje);
        bool util = data.DayOfWeek != System.DayOfWeek.Saturday && data.DayOfWeek != System.DayOfWeek.Sunday;
        int paginas = util ? Mathf.RoundToInt(c.paginasMes / 22f * Random.Range(0.6f, 1.4f)) : 0;

        if (c.suprimento <= 0f) paginas = 0;   // sem tinta não imprime
        c.paginasNoMes += paginas;
        c.contadorTotal += paginas;
        c.suprimento = Mathf.Max(0f, c.suprimento - paginas / (float)t.rendimentoRefil);

        if (c.suprimento <= 0f)
        {
            c.diasSemSuprimento++;
            c.satisfacao = Mathf.Max(0f, c.satisfacao - 6f);
            if (c.diasSemSuprimento == 1) Avisar($"<b>{c.empresa}</b> está sem tinta/toner! Faça uma visita.", UIFactory.Perigo, 4f);
        }
        else if (c.suprimento < 0.15f && c.suprimento + paginas / (float)t.rendimentoRefil >= 0.15f)
            Avisar($"Tinta/toner baixo na <b>{c.empresa}</b> (abaixo de 15%).", UIFactory.Alerta, 3.5f);

        // Desgaste e defeito
        if (e != null)
        {
            float sobrecarga = Mathf.Max(1f, c.paginasMes / (float)t.paginasRecomendadasMes);
            e.desgaste = Mathf.Min(1f, e.desgaste + paginas / 400000f * sobrecarga);
            float chance = chanceDefeitoBase * CatalogoLocacao.Condicao(e.condicao).multDefeito *
                           CatalogoLocacao.Fornecedor(c.fornecedorSuprimento).multDefeito * sobrecarga * (1f + e.desgaste * 2f);
            if (!c.chamadoAberto && paginas > 0 && Random.value < chance) AbrirChamadoContrato(c);
        }

        if (c.chamadoAberto) c.satisfacao = Mathf.Max(0f, c.satisfacao - 3f);

        if (c.satisfacao < 20f) Encerrar(c, $"<b>{c.empresa}</b> cancelou o contrato: estava muito insatisfeita.");
    }

    void AbrirChamadoContrato(ContratoLocacao c)
    {
        c.chamadoAberto = true;
        MissionManager.Instance?.AdicionarChamadoDeContrato(c.id, c.empresa);
        Avisar($"Defeito na impressora alugada da <b>{c.empresa}</b>: chamado aberto (incluso no contrato).", UIFactory.Alerta, 4f);
    }

    // Chamado de contrato encerrado (chamado pelo MissionManager): resolvido ou abandonado
    public void ChamadoContratoEncerrado(int contratoId, bool resolvido, bool rapido)
    {
        var c = contratos.Find(x => x.id == contratoId);
        if (c == null) return;
        c.chamadoAberto = false;
        c.satisfacao = resolvido ? Mathf.Min(100f, c.satisfacao + (rapido ? 8f : 3f)) : Mathf.Max(0f, c.satisfacao - 10f);
    }

    void Faturar(int mes)
    {
        foreach (var c in contratos)
        {
            if (!c.ativo || !c.Instalado) continue;
            // Primeiro mês proporcional aos dias instalado
            var inicioMes = EmpresaManager.PrimeiroDiaDoMes(mes);
            int diaInicioMes = EmpresaManager.DiaDaData(inicioMes);
            int diasNoMes = System.DateTime.DaysInMonth(inicioMes.Year, inicioMes.Month);
            float fracao = Mathf.Clamp01((diaInicioMes + diasNoMes - Mathf.Max(c.diaInstalacao, diaInicioMes)) / (float)diasNoMes);
            float valor = c.mensal * fracao;

            // Excedente só entra se o contador foi lido no mês
            int excedentes = Mathf.Max(0, c.paginasNoMes - c.franquia);
            float excedenteValor = excedentes * ((1f - c.fracCor) * c.excedentePB + c.fracCor * c.excedenteCor);
            if (c.mesLeitura == mes) valor += excedenteValor;
            else if (excedenteValor > 1f)
                Avisar($"Você não leu o contador da <b>{c.empresa}</b>: perdeu {UIFactory.Dinheiro(excedenteValor)} de excedente.", UIFactory.Perigo, 4.5f);

            CobrarCliente(c, Mathf.Round(valor * 100f) / 100f, "Locação de impressora");
            c.paginasNoMes = 0;
        }
    }

    void CobrarCliente(ContratoLocacao c, float valor, string servico)
    {
        if (valor <= 0f) return;
        var ch = new Chamado(c.empresa, servico, TipoChamado.Impressora, DificuldadeChamado.Facil, valor, 0f) { exigeNota = true };
        if (EmpresaManager.Instance != null) EmpresaManager.Instance.RegistrarServico(ch, valor, true);
        else GameEconomy.Instance?.ReceberPagamento(valor);
    }

    void Encerrar(ContratoLocacao c, string mensagem)
    {
        c.ativo = false;
        var e = Equip(c.equipamentoId);
        if (e != null) e.contratoId = -1;   // volta para o estoque
        Avisar(mensagem, UIFactory.Perigo, 5f);
    }

    // ---------- Save ----------

    [System.Serializable]
    public class Dados
    {
        public List<EquipamentoPatrimonio> patrimonio = new List<EquipamentoPatrimonio>();
        public List<Lead> leads = new List<Lead>();
        public List<ContratoLocacao> contratos = new List<ContratoLocacao>();
        public int proximoId = 1;
        public int ultimoMesFaturado = -1;
    }

    public static Dados dadosSalvos;

    public Dados Exportar() => new Dados
    {
        patrimonio = new List<EquipamentoPatrimonio>(patrimonio), leads = new List<Lead>(leads),
        contratos = new List<ContratoLocacao>(contratos), proximoId = proximoId, ultimoMesFaturado = ultimoMesFaturado
    };

    public void Carregar(Dados d)
    {
        if (d == null) { NovoJogo(); return; }
        patrimonio = d.patrimonio ?? new List<EquipamentoPatrimonio>();
        leads = d.leads ?? new List<Lead>();
        contratos = d.contratos ?? new List<ContratoLocacao>();
        proximoId = Mathf.Max(1, d.proximoId);
        ultimoMesFaturado = d.ultimoMesFaturado;
    }

    public void NovoJogo()
    {
        patrimonio.Clear(); leads.Clear(); contratos.Clear(); proximoId = 1; ultimoMesFaturado = -1;
    }
}
