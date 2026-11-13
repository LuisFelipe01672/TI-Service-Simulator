using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum Regime { Autonomo, MEI, ME }

// Um serviço feito que o cliente ainda vai pagar
[System.Serializable]
public class Recebivel
{
    public string cliente;
    public string servico;
    public float valor;
    public int diaServico;
    public int diaPrevisto;        // quando o dinheiro cai (PIX = mesmo dia, boleto = alguns dias)
    public string forma;           // "PIX" ou "Boleto"
    public bool exigeNota;         // cliente empresa: só paga com nota fiscal
    public bool documentoEmitido;  // nota fiscal (CNPJ) ou recibo (CPF)
    public bool pago;
}

// Uma guia de imposto ou conta (DAS, carnê-leão, INSS, contador)
[System.Serializable]
public class Guia
{
    public string nome;
    public float valor;
    public int vencimento;         // dia do jogo
    public bool paga;
    public string competencia;     // "03/2026"
}

// A "empresa" do jogador: começa trabalhando como pessoa física (CPF) e pode abrir MEI e depois ME.
// Valores de 2026 (Brasil): salário mínimo R$ 1.621; DAS MEI serviços R$ 86,05 (vence dia 20); limite MEI R$ 81 mil/ano;
// Simples Nacional Anexo III a partir de 6%; carnê-leão com isenção até R$ 5 mil/mês; INSS do autônomo 11% do mínimo.
// Multa por atraso: 0,33% ao dia (máx. 20%) + juros de 1% ao mês.
public class EmpresaManager : MonoBehaviour
{
    public static EmpresaManager Instance;

    // ---------- Valores ----------
    public const float SalarioMinimo = 1621f;
    public const float DasMei = 86.05f;
    public const float LimiteMei = 81000f;
    public const float InssAutonomo = SalarioMinimo * 0.11f;   // plano simplificado
    public const float CustoAberturaMe = 800f;                 // Junta, alvará e certificado digital (aprox.)
    public const int DiasAberturaMe = 5;
    public const float HonorariosContador = 450f;              // mensalidade típica de contador (aprox.)

    // ---------- Estado ----------
    public Regime regime = Regime.Autonomo;
    public int meProntaNoDia = -1;            // abertura de ME em andamento
    public List<float> faturamentoMes = new List<float>();   // índice = mês desde mar/2026
    public List<Recebivel> recebiveis = new List<Recebivel>();
    public List<Guia> guias = new List<Guia>();
    public int ultimoMesFechado = -1;
    public int notasEmitidas;

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
        new GameObject("EmpresaManager").AddComponent<EmpresaManager>();
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

    // ---------- Calendário ----------

    static int DiaAtual => DiaManager.Instance != null ? DiaManager.Instance.dia : 1;

    // Mês contado a partir de março/2026 (0 = mar/2026)
    public static int MesDoDia(int dia)
    {
        var d = DiaManager.DataDoDia(dia);
        return (d.Year - 2026) * 12 + d.Month - 3;
    }

    public static System.DateTime PrimeiroDiaDoMes(int mes) => new System.DateTime(2026, 3, 1).AddMonths(mes);

    // Dia do jogo que cai na data informada (ou o primeiro depois dela)
    public static int DiaDaData(System.DateTime data) => Mathf.Max(1, (int)(data - DiaManager.Inicio).TotalDays + 1);

    public string NomeRegime =>
        regime == Regime.Autonomo ? "Autônomo (CPF)" : regime == Regime.MEI ? "MEI — Técnico de manutenção de computador" : "Microempresa (Simples Nacional)";

    public bool TemCnpj => regime != Regime.Autonomo;

    // Quanto o cliente aceita pagar por hora: técnico autônomo cobra menos que empresa formal
    public static float MultPrecoRegime => Instance == null ? 1f : Instance.regime == Regime.Autonomo ? 0.65f : Instance.regime == Regime.MEI ? 0.85f : 1f;

    public float FaturamentoNoMes(int mes) => mes >= 0 && mes < faturamentoMes.Count ? faturamentoMes[mes] : 0f;

    // Faturamento do ano-calendário atual
    public float FaturamentoAno
    {
        get
        {
            int mesAtual = MesDoDia(DiaAtual);
            int ano = PrimeiroDiaDoMes(mesAtual).Year;
            float total = 0f;
            for (int m = 0; m <= mesAtual; m++) if (PrimeiroDiaDoMes(m).Year == ano) total += FaturamentoNoMes(m);
            return total;
        }
    }

    // Limite do MEI é proporcional aos meses de atividade no primeiro ano (R$ 6.750 por mês)
    public float LimiteMeiNoAno => LimiteMei;

    // ---------- Regras de negócio ----------

    // Empresas com dívida de impostos vencida há mais de 60 dias ficam irregulares e não emitem nota
    public bool Irregular => guias.Exists(g => !g.paga && DiaAtual - g.vencimento > 60);

    public int LimiteFuncionarios => regime == Regime.Autonomo ? 0 : regime == Regime.MEI ? 1 : 99;

    // Encargos sobre o salário (FGTS, INSS patronal...)
    public float MultEncargos => regime == Regime.MEI ? 1.11f : regime == Regime.ME ? 1.08f : 1f;

    public bool PodeAtender(Chamado ch, out string motivo)
    {
        motivo = null;
        if (ch.exigeNota && !TemCnpj) { motivo = "Este cliente é empresa e só contrata com nota fiscal. Abra um CNPJ (aba Empresa)."; return false; }
        if (ch.exigeNota && Irregular) { motivo = "Seu CNPJ está irregular (impostos atrasados há mais de 60 dias): não dá para emitir nota."; return false; }
        return true;
    }

    // Chamado concluído: registra o faturamento e a forma de pagamento. Devolve um texto para o jogador.
    public string RegistrarServico(Chamado ch, float valor, bool porFuncionario)
    {
        int dia = DiaAtual;
        int mes = MesDoDia(dia);
        while (faturamentoMes.Count <= mes) faturamentoMes.Add(0f);
        faturamentoMes[mes] += valor;

        var r = new Recebivel
        {
            cliente = ch.nomeCliente,
            servico = ch.descricaoProblema,
            valor = valor,
            diaServico = dia,
            exigeNota = ch.exigeNota,
        };
        if (ch.exigeNota) { r.forma = "Boleto"; r.diaPrevisto = dia + Random.Range(2, 6); }
        else if (Random.value < 0.65f) { r.forma = "PIX"; r.diaPrevisto = dia; }
        else { r.forma = "Boleto"; r.diaPrevisto = dia + Random.Range(1, 4); }
        // Uns clientes atrasam o pagamento
        if (Random.value < 0.08f) r.diaPrevisto += Random.Range(3, 8);
        recebiveis.Add(r);

        VerificarLimiteMei();
        ProcessarRecebimentos();

        if (r.pago) return $"{ch.nomeCliente} pagou {UIFactory.Dinheiro(valor)} no PIX.";
        if (r.exigeNota) return $"Emita a nota fiscal para {ch.nomeCliente} (aba Empresa): o boleto de {UIFactory.Dinheiro(valor)} só sai com a nota.";
        return $"{ch.nomeCliente} vai pagar {UIFactory.Dinheiro(valor)} por boleto em {r.diaPrevisto - dia} dia(s).";
    }

    // Nota fiscal (CNPJ) ou recibo (CPF)
    public string EmitirDocumento(Recebivel r)
    {
        if (r.documentoEmitido) return "Documento já emitido.";
        if (r.exigeNota && !TemCnpj) return "Sem CNPJ não dá para emitir nota fiscal.";
        if (TemCnpj && Irregular) return "CNPJ irregular: pague os impostos atrasados para voltar a emitir notas.";
        r.documentoEmitido = true;
        if (TemCnpj) notasEmitidas++;
        ProcessarRecebimentos();
        return TemCnpj ? $"NFS-e nº {notasEmitidas:000} emitida para {r.cliente} ({UIFactory.Dinheiro(r.valor)})."
                       : $"Recibo emitido para {r.cliente} ({UIFactory.Dinheiro(r.valor)}).";
    }

    void ProcessarRecebimentos()
    {
        int dia = DiaAtual;
        foreach (var r in recebiveis)
        {
            if (r.pago || dia < r.diaPrevisto) continue;
            if (r.exigeNota && !r.documentoEmitido) continue;   // empresa não paga sem nota
            r.pago = true;
            GameEconomy.Instance?.ReceberPagamento(r.valor);
            GameUI.Instance?.Toast($"Pagamento recebido: {r.cliente} ({r.forma}) +{UIFactory.Dinheiro(r.valor)}", UIFactory.Sucesso, 2.5f);
        }
        recebiveis.RemoveAll(x => x.pago && dia - x.diaServico > 30);
    }

    public float AReceber
    {
        get { float t = 0f; foreach (var r in recebiveis) if (!r.pago) t += r.valor; return t; }
    }

    // ---------- Abrir empresa ----------

    public string AbrirMei()
    {
        if (regime != Regime.Autonomo) return "Você já tem CNPJ.";
        if (FaturamentoAno > LimiteMei) return "Seu faturamento no ano já passou do limite do MEI. Abra uma ME.";
        regime = Regime.MEI;
        return "MEI aberto no Portal do Empreendedor! Ocupação: Técnico(a) de manutenção de computador (CNAE 9511-8/00). DAS de R$ 86,05 todo dia 20.";
    }

    public string AbrirMe()
    {
        if (regime == Regime.ME) return "Você já é uma Microempresa.";
        if (meProntaNoDia > 0) return "A abertura da sua ME já está em andamento.";
        var eco = GameEconomy.Instance;
        if (eco != null && eco.saldo < CustoAberturaMe) return $"Precisa de {UIFactory.Dinheiro(CustoAberturaMe)} (Junta Comercial, alvará e certificado digital).";
        eco?.Gastar(CustoAberturaMe, LivroCaixa.Taxas);
        meProntaNoDia = DiaAtual + DiasAberturaMe;
        return $"Contador contratado e papelada enviada. Sua ME fica pronta em {DiasAberturaMe} dias.";
    }

    void VerificarLimiteMei()
    {
        if (regime != Regime.MEI) return;
        float fat = FaturamentoAno;
        if (fat > LimiteMei * 1.2f)
        {
            // Passou mais de 20%: desenquadramento na hora, com imposto sobre o excesso
            regime = Regime.ME;
            float excesso = fat - LimiteMei;
            AdicionarGuia("DAS complementar (excesso do MEI)", excesso * 0.06f, DiaAtual + 30, MesDoDia(DiaAtual));
            GameUI.Instance?.Toast("<b>Você estourou o limite do MEI em mais de 20%!</b>\nVirou Microempresa na hora e vai pagar imposto sobre o excesso.", UIFactory.Perigo, 6f);
        }
        else if (fat > LimiteMei)
            GameUI.Instance?.Toast("Atenção: você passou do limite de R$ 81 mil do MEI. Em janeiro vira Microempresa.", UIFactory.Alerta, 4f);
    }

    // ---------- Impostos ----------

    public void AdicionarGuia(string nome, float valor, int vencimento, int mes)
    {
        if (valor < 0.01f) return;
        guias.Add(new Guia { nome = nome, valor = Mathf.Round(valor * 100f) / 100f, vencimento = vencimento, competencia = PrimeiroDiaDoMes(mes).ToString("MM/yyyy") });
    }

    // Valor com multa (0,33% ao dia, até 20%) e juros (1% ao mês) se estiver atrasada
    public float ValorAtual(Guia g)
    {
        int atraso = DiaAtual - g.vencimento;
        if (atraso <= 0) return g.valor;
        float multa = Mathf.Min(0.2f, 0.0033f * atraso);
        float juros = 0.01f * Mathf.CeilToInt(atraso / 30f);
        return Mathf.Round(g.valor * (1f + multa + juros) * 100f) / 100f;
    }

    public string PagarGuia(Guia g)
    {
        if (g.paga) return "Já está paga.";
        float valor = ValorAtual(g);
        var eco = GameEconomy.Instance;
        if (eco != null && eco.saldo < valor) return "Saldo insuficiente.";
        eco?.Gastar(valor, LivroCaixa.CategoriaDaGuia(g.nome));
        g.paga = true;
        return $"{g.nome} ({g.competencia}) paga: {UIFactory.Dinheiro(valor)}.";
    }

    // Imposto de renda mensal do autônomo (carnê-leão), regras de 2026:
    // base = rendimento - deduções (INSS) ou o desconto simplificado de R$ 607,20, o que for maior;
    // depois, a redução da Lei 15.270/2025 zera o imposto até R$ 5 mil e diminui até R$ 7.350.
    public static float CarneLeao(float rendimento, float deducoes = 0f)
    {
        float baseMensal = rendimento - Mathf.Max(deducoes, 607.20f);
        float ir;
        if (baseMensal <= 2428.80f) ir = 0f;
        else if (baseMensal <= 2826.65f) ir = baseMensal * 0.075f - 182.16f;
        else if (baseMensal <= 3751.05f) ir = baseMensal * 0.15f - 394.16f;
        else if (baseMensal <= 4664.68f) ir = baseMensal * 0.225f - 675.49f;
        else ir = baseMensal * 0.275f - 908.73f;

        float reducao = rendimento <= 5000f ? 312.89f : rendimento <= 7350f ? 978.62f - 0.133145f * rendimento : 0f;
        return Mathf.Max(0f, ir - Mathf.Max(0f, reducao));
    }

    // Simples Nacional, Anexo III: alíquota efetiva pela receita dos últimos 12 meses
    public static float AliquotaAnexoIII(float rbt12)
    {
        float nominal, deduzir;
        if (rbt12 <= 180000f) { nominal = 0.06f; deduzir = 0f; }
        else if (rbt12 <= 360000f) { nominal = 0.112f; deduzir = 9360f; }
        else if (rbt12 <= 720000f) { nominal = 0.135f; deduzir = 17640f; }
        else if (rbt12 <= 1800000f) { nominal = 0.16f; deduzir = 35640f; }
        else if (rbt12 <= 3600000f) { nominal = 0.21f; deduzir = 125640f; }
        else { nominal = 0.33f; deduzir = 648000f; }
        if (rbt12 <= 0f) return nominal;
        return (rbt12 * nominal - deduzir) / rbt12;
    }

    float Rbt12(int mes)
    {
        float total = 0f; int meses = 0;
        for (int m = Mathf.Max(0, mes - 12); m < mes; m++) { total += FaturamentoNoMes(m); meses++; }
        if (meses == 0) return FaturamentoNoMes(mes) * 12f;
        return total / meses * 12f;   // empresa nova: proporcionaliza
    }

    // Fecha o mês que acabou: gera as guias que vencem no mês seguinte
    void FecharMes(int mes)
    {
        float fat = FaturamentoNoMes(mes);
        var proximo = PrimeiroDiaDoMes(mes + 1);
        int dia15 = DiaDaData(proximo.AddDays(14));
        int dia20 = DiaDaData(proximo.AddDays(19));
        int fimMes = DiaDaData(proximo.AddMonths(1).AddDays(-1));

        switch (regime)
        {
            case Regime.Autonomo:
                if (fat > 0f)
                {
                    AdicionarGuia("INSS autônomo (GPS)", InssAutonomo, dia15, mes);
                    AdicionarGuia("Carnê-leão (IR)", CarneLeao(fat, InssAutonomo), fimMes, mes);
                }
                break;
            case Regime.MEI:
                AdicionarGuia("DAS MEI", DasMei, dia20, mes);
                break;
            case Regime.ME:
                AdicionarGuia("DAS Simples Nacional", fat * AliquotaAnexoIII(Rbt12(mes)), dia20, mes);
                AdicionarGuia("Honorários do contador", HonorariosContador, DiaDaData(proximo.AddDays(9)), mes);
                break;
        }

        // Folha de pagamento: salários + encargos, no 5º dia útil (aprox. dia 7) do mês seguinte
        var fm = FuncionarioManager.Instance;
        if (fm != null && fm.equipe.Count > 0)
        {
            float salarios = 0f;
            foreach (var f in fm.equipe) salarios += f.salarioMensal;
            AdicionarGuia("Folha de pagamento (salários)", salarios, DiaDaData(proximo.AddDays(6)), mes);
            AdicionarGuia("FGTS (8%)", salarios * 0.08f, DiaDaData(proximo.AddDays(19)), mes);
            if (regime == Regime.MEI) AdicionarGuia("INSS patronal do MEI (3%)", salarios * 0.03f, dia20, mes);
            // 13º salário: em dezembro (gerado no fechamento de novembro)
            if (PrimeiroDiaDoMes(mes).Month == 11)
                AdicionarGuia("13º salário", salarios, DiaDaData(proximo.AddDays(19)), mes);
        }

        // Virada do ano: MEI que passou do limite vira ME
        if (proximo.Month == 1 && regime == Regime.MEI)
        {
            float fatAno = 0f;
            for (int m = 0; m <= mes; m++) if (PrimeiroDiaDoMes(m).Year == proximo.Year - 1) fatAno += FaturamentoNoMes(m);
            if (fatAno > LimiteMei)
            {
                regime = Regime.ME;
                GameUI.Instance?.Toast("Ano novo: como você passou do limite do MEI, agora é Microempresa (Simples Nacional).", UIFactory.Alerta, 5f);
            }
        }
    }

    void NovoDia(int dia)
    {
        // Fecha os meses que terminaram
        int mesAtual = MesDoDia(dia);
        while (ultimoMesFechado < mesAtual - 1)
        {
            ultimoMesFechado++;
            FecharMes(ultimoMesFechado);
            GameUI.Instance?.Toast("Novo mês: chegaram as guias de impostos, folha e contas (aba Empresa do tablet).", UIFactory.Alerta, 4f);
            string noticia = Concorrencia.AtualizarMes(ultimoMesFechado);
            if (noticia != null) GameUI.Instance?.Toast(noticia, UIFactory.Destaque, 5f);
        }

        if (meProntaNoDia > 0 && dia >= meProntaNoDia)
        {
            meProntaNoDia = -1;
            regime = Regime.ME;
            GameUI.Instance?.Toast("<b>Sua Microempresa está aberta!</b> Simples Nacional, Anexo III. Pode contratar quantos funcionários quiser.", UIFactory.Sucesso, 5f);
        }

        ProcessarRecebimentos();

        foreach (var g in guias)
            if (!g.paga && g.vencimento == dia)
                GameUI.Instance?.Toast($"Vence hoje: {g.nome} ({UIFactory.Dinheiro(g.valor)}).", UIFactory.Alerta, 4f);
        guias.RemoveAll(g => g.paga && dia - g.vencimento > 60);
    }

    // ---------- Save ----------

    [System.Serializable]
    public class Dados
    {
        public Regime regime;
        public int meProntaNoDia = -1;
        public List<float> faturamentoMes = new List<float>();
        public List<Recebivel> recebiveis = new List<Recebivel>();
        public List<Guia> guias = new List<Guia>();
        public int ultimoMesFechado = -1;
        public int notasEmitidas;
    }

    public static Dados dadosSalvos;

    public Dados Exportar() => new Dados
    {
        regime = regime, meProntaNoDia = meProntaNoDia, faturamentoMes = new List<float>(faturamentoMes),
        recebiveis = new List<Recebivel>(recebiveis), guias = new List<Guia>(guias),
        ultimoMesFechado = ultimoMesFechado, notasEmitidas = notasEmitidas
    };

    public void Carregar(Dados d)
    {
        if (d == null) { NovoJogo(); return; }
        regime = d.regime; meProntaNoDia = d.meProntaNoDia;
        faturamentoMes = d.faturamentoMes ?? new List<float>();
        recebiveis = d.recebiveis ?? new List<Recebivel>();
        guias = d.guias ?? new List<Guia>();
        ultimoMesFechado = d.ultimoMesFechado; notasEmitidas = d.notasEmitidas;
    }

    public void NovoJogo()
    {
        regime = Regime.Autonomo; meProntaNoDia = -1; faturamentoMes.Clear();
        recebiveis.Clear(); guias.Clear(); ultimoMesFechado = -1; notasEmitidas = 0;
    }
}
