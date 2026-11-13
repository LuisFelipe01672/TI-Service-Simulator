using System.Collections.Generic;
using UnityEngine;

// Um fornecedor/distribuidor com quem a empresa pode fechar parceria (nomes fictícios).
public class ParceriaInfo
{
    public int id;
    public string nome, area, descricao;
    public Regime regimeMinimo;
    public float reputacaoMinima;
    public float adesao;            // taxa de adesão / treinamento / certificação
    public int diasAnalise;         // cadastro e análise de crédito
    public TipoServico[] libera = new TipoServico[0];
    public bool liberaFirewall, liberaInfra, liberaImpressoras, liberaBilhetagem;
    public string semParceria;      // o que acontece sem ela
}

[System.Serializable]
public class EstadoParceria
{
    public int id;
    public int status;               // 0 nenhuma, 1 em análise, 2 ativa
    public int ativaNoDia = -1;
    public float adesaoNegociada = -1f;
    public bool negociou;
}

// Parcerias com fornecedores: para vender um produto, a empresa precisa ser revenda autorizada.
// Cada parceria tem requisito (CNPJ, porte, reputação), taxa de adesão e análise de alguns dias.
// O nível (Registrado → Prata → Ouro) sobe com o volume vendido e dá desconto no custo.
public static class Parcerias
{
    public const int Nuvem = 0, Antivirus = 1, Backup = 2, Bot = 3, Hardware = 4, Firewall = 5, Infra = 6, Impressoras = 7, ControleImpressao = 8;

    public static readonly ParceriaInfo[] Lista =
    {
        new ParceriaInfo { id = Nuvem, nome = "NuvemBR Distribuidora", area = "E-mail e Office na nuvem",
            descricao = "Revenda de licenças de e-mail e Office por usuário.", regimeMinimo = Regime.MEI, adesao = 0f, diasAnalise = 3,
            libera = new[] { TipoServico.Email }, semParceria = "Não dá para vender e-mail/Office." },
        new ParceriaInfo { id = Antivirus, nome = "ProtegeMax", area = "Antivírus e monitoramento remoto",
            descricao = "Console de antivírus e monitoramento das máquinas dos clientes (suporte gerenciado).", regimeMinimo = Regime.MEI, adesao = 300f, diasAnalise = 2,
            libera = new[] { TipoServico.Suporte }, semParceria = "Não dá para vender suporte gerenciado." },
        new ParceriaInfo { id = Backup, nome = "CofreNuvem", area = "Backup em nuvem",
            descricao = "Armazenamento e agente de backup com painel de revenda.", regimeMinimo = Regime.ME, reputacaoMinima = 30f, adesao = 1000f, diasAnalise = 5,
            libera = new[] { TipoServico.Backup }, semParceria = "Não dá para vender backup." },
        new ParceriaInfo { id = Bot, nome = "ZapFlow", area = "Plataforma de bots de WhatsApp",
            descricao = "Plataforma white-label para montar bots de atendimento.", regimeMinimo = Regime.ME, reputacaoMinima = 35f, adesao = 1500f, diasAnalise = 5,
            libera = new[] { TipoServico.Bot }, semParceria = "Não dá para vender bot de atendimento." },
        new ParceriaInfo { id = Hardware, nome = "InfoAtacado", area = "Notebooks e mini PCs",
            descricao = "Distribuidor de computadores com preço de atacado e faturamento para CNPJ.", regimeMinimo = Regime.MEI, adesao = 0f, diasAnalise = 5,
            libera = new[] { TipoServico.Notebook, TipoServico.MiniPc }, semParceria = "Não dá para alugar notebooks e mini PCs." },
        new ParceriaInfo { id = Firewall, nome = "Muralha Security", area = "Firewalls",
            descricao = "Appliances de firewall com licença de proteção. Exige técnico certificado (curso incluído na adesão).", regimeMinimo = Regime.ME, reputacaoMinima = 40f, adesao = 2500f, diasAnalise = 7,
            liberaFirewall = true, semParceria = "Não dá para vender projetos de firewall." },
        new ParceriaInfo { id = Infra, nome = "Cabos & Conexões Atacado", area = "Material de rede",
            descricao = "Cabo, conectores, racks, switches e access points a preço de atacado, entrega em 1 dia.", regimeMinimo = Regime.MEI, adesao = 0f, diasAnalise = 1,
            liberaInfra = true, semParceria = "Você compra material no varejo: 30% mais caro e chega em 2 dias." },
        new ParceriaInfo { id = Impressoras, nome = "ImpressBR Distribuidora", area = "Impressoras para locação",
            descricao = "Compra direta de multifuncionais para outsourcing de impressão.", regimeMinimo = Regime.MEI, reputacaoMinima = 25f, adesao = 500f, diasAnalise = 4,
            liberaImpressoras = true, semParceria = "Você compra impressoras no varejo, sem desconto de nível." },
        new ParceriaInfo { id = ControleImpressao, nome = "PrintControl Software", area = "Controle de impressão (bilhetagem)",
            descricao = "Software que conta quem imprime o quê, cria cotas e lê o contador sozinho.", regimeMinimo = Regime.MEI, reputacaoMinima = 30f, adesao = 800f, diasAnalise = 3,
            liberaBilhetagem = true, semParceria = "Não dá para oferecer controle de impressão." },
    };

    public static List<EstadoParceria> estados = new List<EstadoParceria>();

    public static readonly string[] Niveis = { "Registrado", "Prata", "Ouro" };
    public static readonly float[] DescontoNivel = { 0f, 0.08f, 0.15f };
    public const float AcrescimoVarejo = 0.30f;

    public static void Iniciar()
    {
        estados = new List<EstadoParceria>();
        foreach (var p in Lista) estados.Add(new EstadoParceria { id = p.id });
    }

    static void Garantir()
    {
        if (estados == null) estados = new List<EstadoParceria>();
        foreach (var p in Lista)
            if (!estados.Exists(e => e.id == p.id)) estados.Add(new EstadoParceria { id = p.id });
    }

    public static EstadoParceria Estado(int id) { Garantir(); return estados.Find(e => e.id == id); }
    public static bool Ativa(int id) => Estado(id).status == 2;

    static int Hoje => DiaManager.Instance != null ? DiaManager.Instance.dia : 1;

    // ---------- Requisitos e liberação ----------

    public static bool AtendeRequisitos(ParceriaInfo p, out string motivo)
    {
        motivo = null;
        var emp = EmpresaManager.Instance;
        if (emp != null)
        {
            if (!emp.TemCnpj) { motivo = "Precisa de CNPJ."; return false; }
            if (p.regimeMinimo == Regime.ME && emp.regime != Regime.ME) { motivo = "Exige Microempresa (ME)."; return false; }
        }
        float rep = ReputationManager.Instance != null ? ReputationManager.Instance.GetReputacao() : 100f;
        if (rep < p.reputacaoMinima) { motivo = $"Exige reputação {p.reputacaoMinima:0} (você tem {rep:0})."; return false; }
        return true;
    }

    public static ParceriaInfo DoServico(TipoServico t)
    {
        foreach (var p in Lista) foreach (var s in p.libera) if (s == t) return p;
        return null;
    }

    public static bool LiberaServico(TipoServico t, out string motivo)
    {
        motivo = null;
        if (t == TipoServico.Erp)
        {
            if (ProdutosProprios.Lancado(ProdutosProprios.Erp)) return true;
            motivo = "O ERP é produto próprio: desenvolva primeiro (aba Negócios)."; return false;
        }
        if (t == TipoServico.Bot && ProdutosProprios.Lancado(ProdutosProprios.Bot)) return true;
        if (t == TipoServico.Firewall)
        {
            if (Ativa(Firewall)) return true;
            motivo = $"Precisa da parceria com a {Lista[Firewall].nome} (aba Negócios)."; return false;
        }
        var p = DoServico(t);
        if (p == null || Ativa(p.id)) return true;
        motivo = $"Precisa da parceria com a {p.nome} (aba Negócios).";
        return false;
    }

    public static float Adesao(ParceriaInfo p)
    {
        var e = Estado(p.id);
        return e.adesaoNegociada >= 0f ? e.adesaoNegociada : p.adesao;
    }

    // Tenta negociar a taxa de adesão (uma vez por fornecedor). Reputação ajuda.
    public static string Negociar(int id)
    {
        var p = Lista[id];
        var e = Estado(id);
        if (e.status != 0) return "A parceria já foi assinada.";
        if (p.adesao <= 0f) return "Essa parceria não tem taxa de adesão.";
        if (e.negociou) return "Você já negociou com esse fornecedor. O preço agora é esse.";
        e.negociou = true;
        float rep = ReputationManager.Instance != null ? ReputationManager.Instance.GetReputacao() : 30f;
        float chance = 0.3f + rep / 200f;
        if (Random.value < chance)
        {
            float desconto = Random.Range(0.25f, 0.6f);
            e.adesaoNegociada = Mathf.Round(p.adesao * (1f - desconto));
            return $"O gerente de canais da {p.nome} topou: adesão por {UIFactory.Dinheiro(e.adesaoNegociada)} (era {UIFactory.Dinheiro(p.adesao)}).";
        }
        e.adesaoNegociada = p.adesao;
        return $"A {p.nome} não deu desconto: \"política de canais\". Adesão continua {UIFactory.Dinheiro(p.adesao)}.";
    }

    public static string Solicitar(int id)
    {
        var p = Lista[id];
        var e = Estado(id);
        if (e.status == 2) return "Parceria já ativa.";
        if (e.status == 1) return "Cadastro já está em análise.";
        if (!AtendeRequisitos(p, out string motivo)) return motivo;
        float taxa = Adesao(p);
        var eco = GameEconomy.Instance;
        if (taxa > 0f && eco != null && eco.saldo < taxa) return $"Saldo insuficiente para a adesão: {UIFactory.Dinheiro(taxa)}.";
        if (taxa > 0f) eco?.Gastar(taxa, LivroCaixa.Taxas);
        e.status = 1;
        e.ativaNoDia = Hoje + p.diasAnalise;
        return $"Cadastro enviado para a {p.nome}" + (taxa > 0f ? $" (adesão {UIFactory.Dinheiro(taxa)})" : "") +
               $". Análise de crédito e documentos: pronto em {DiaManager.DataDoDia(e.ativaNoDia):dd/MM}.";
    }

    // Chamado todo dia (pelo ProjetosManager): ativa as parcerias aprovadas
    public static void NovoDia(int dia)
    {
        Garantir();
        foreach (var e in estados)
        {
            if (e.status != 1 || dia < e.ativaNoDia) continue;
            e.status = 2;
            var p = Lista[e.id];
            GameUI.Instance?.Toast($"<b>Parceria aprovada:</b> agora você é revenda da {p.nome} ({p.area}).", UIFactory.Sucesso, 5f);
            Conquistas.Desbloquear("parceria");
        }
    }

    // ---------- Nível e desconto ----------

    // Volume: quantos contratos/equipamentos/projetos ativos dependem desse fornecedor
    public static int Volume(int id)
    {
        var p = Lista[id];
        int v = 0;
        var sm = ServicosManager.Instance;
        if (sm != null)
            foreach (var c in sm.contratos)
            {
                if (!c.ativo) continue;
                foreach (var s in p.libera) if (s == c.tipo) v++;
                if (p.liberaFirewall && c.tipo == TipoServico.Firewall) v++;
            }
        if (p.liberaImpressoras && ContratosManager.Instance != null) v += ContratosManager.Instance.patrimonio.Count;
        if (p.liberaBilhetagem && ContratosManager.Instance != null) v += ContratosManager.Instance.contratos.FindAll(c => c.ativo && c.bilhetagem).Count;
        if (p.liberaInfra && ProjetosManager.Instance != null) v += ProjetosManager.Instance.concluidos;
        return v;
    }

    public static int Nivel(int id)
    {
        if (!Ativa(id)) return -1;
        int v = Volume(id);
        return v >= 10 ? 2 : v >= 4 ? 1 : 0;
    }

    public static string NomeNivel(int id) { int n = Nivel(id); return n < 0 ? "—" : Niveis[n]; }

    // Desconto no custo pago ao fornecedor (0 sem parceria)
    public static float Desconto(int id) { int n = Nivel(id); return n < 0 ? 0f : DescontoNivel[n]; }

    public static float DescontoServico(TipoServico t)
    {
        if (t == TipoServico.Firewall) return Desconto(Firewall);
        var p = DoServico(t);
        return p == null ? 0f : Desconto(p.id);
    }

    // Multiplicador do custo de material de infraestrutura: varejo sem parceria, desconto de nível com ela
    public static float MultMaterialInfra => Ativa(Infra) ? 1f - Desconto(Infra) : 1f + AcrescimoVarejo;

    public static int MaiorNivel()
    {
        int m = -1;
        foreach (var p in Lista) m = Mathf.Max(m, Nivel(p.id));
        return m;
    }

    // ---------- Save ----------

    [System.Serializable]
    public class Dados { public List<EstadoParceria> estados = new List<EstadoParceria>(); }

    public static Dados Exportar() { Garantir(); return new Dados { estados = new List<EstadoParceria>(estados) }; }

    public static void Carregar(Dados d)
    {
        if (d == null || d.estados == null) { Iniciar(); return; }
        estados = d.estados;
        Garantir();
    }
}
