using System.Collections.Generic;
using UnityEngine;

public class InfoProduto
{
    public int id;
    public string nome, descricao, efeito;
    public int meses;                 // meses de desenvolvimento até a primeira versão
    public float custoMensal;         // equipe de desenvolvimento (terceirizada) por mês
    public float manutencaoMensal;    // depois de lançado: correções, servidores, suporte
    public float reputacaoMinima;
}

[System.Serializable]
public class EstadoProduto
{
    public int id;
    public int status;        // 0 não começou, 1 desenvolvendo, 2 pausado, 3 lançado
    public int mesesFeitos;
    public int versao;        // versões lançadas (mais versões = menos bugs)
    public bool novaVersao;   // desenvolvendo a próxima versão
}

// Produtos próprios: quando a empresa cresce, ela pode desenvolver o próprio ERP e a própria plataforma de bot.
// Custa caro todo mês enquanto desenvolve (equipe de devs), mas depois a margem é muito maior que revender.
public static class ProdutosProprios
{
    public const int Erp = 0, Bot = 1;
    public const int MesesNovaVersao = 2;

    public static readonly InfoProduto[] Lista =
    {
        new InfoProduto { id = Erp, nome = "ERP próprio", meses = 8, custoMensal = 12000f, manutencaoMensal = 4000f, reputacaoMinima = 50f,
            descricao = "Sistema de gestão (financeiro, estoque, notas fiscais) vendido por usuário.",
            efeito = "Libera vender o ERP: R$ 89 por usuário/mês + implantação, custo de servidor baixíssimo." },
        new InfoProduto { id = Bot, nome = "Plataforma de bot própria", meses = 4, custoMensal = 6000f, manutencaoMensal = 1500f, reputacaoMinima = 35f,
            descricao = "Sua própria plataforma de atendimento no WhatsApp, sem depender de fornecedor.",
            efeito = "Vende bot sem parceria e o custo por bot cai de R$ 60 para R$ 15/mês." },
    };

    public static List<EstadoProduto> estados = new List<EstadoProduto>();
    public static int ultimoMes = -1;

    public static void Iniciar()
    {
        estados = new List<EstadoProduto>();
        foreach (var p in Lista) estados.Add(new EstadoProduto { id = p.id });
        ultimoMes = -1;
    }

    public static EstadoProduto Estado(int id)
    {
        if (estados == null) estados = new List<EstadoProduto>();
        var e = estados.Find(x => x.id == id);
        if (e == null) { e = new EstadoProduto { id = id }; estados.Add(e); }
        return e;
    }

    public static bool Lancado(int id) => Estado(id).status == 3;

    public static int MesesTotais(int id) => Mathf.CeilToInt(Lista[id].meses * FocoEmpresa.MultTempoDev);
    public static float CustoMensal(int id) => Mathf.Round(Lista[id].custoMensal * FocoEmpresa.MultCustoDev);

    // Chance de bug por cliente por dia: cai a cada versão
    public static float ChanceBug(int id) => 0.06f / Mathf.Max(1, Estado(id).versao);

    public static bool PodeComecar(int id, out string motivo)
    {
        motivo = null;
        var emp = EmpresaManager.Instance;
        if (emp != null && emp.regime != Regime.ME) { motivo = "Desenvolver software exige Microempresa."; return false; }
        float rep = ReputationManager.Instance != null ? ReputationManager.Instance.GetReputacao() : 100f;
        if (rep < Lista[id].reputacaoMinima) { motivo = $"Exige reputação {Lista[id].reputacaoMinima:0}."; return false; }
        return true;
    }

    public static string Comecar(int id)
    {
        var e = Estado(id);
        var p = Lista[id];
        if (e.status == 1) return "Já está em desenvolvimento.";
        if (e.status == 3 && !e.novaVersao)
        {
            e.novaVersao = true; e.mesesFeitos = 0; e.status = 1;
            return $"Começou a versão {e.versao + 1} do {p.nome}: {MesesNovaVersao} meses a {UIFactory.Dinheiro(CustoMensal(id))}/mês. Menos bugs para os clientes.";
        }
        if (e.status == 0 && !PodeComecar(id, out string motivo)) return motivo;
        e.status = 1;
        if (ultimoMes < 0) ultimoMes = EmpresaManager.MesDoDia(DiaManager.Instance != null ? DiaManager.Instance.dia : 1);
        return $"Equipe de desenvolvimento contratada para o {p.nome}: {UIFactory.Dinheiro(CustoMensal(id))}/mês por ~{MesesTotais(id) - e.mesesFeitos} meses. " +
               "A conta chega como guia todo mês.";
    }

    public static string Pausar(int id)
    {
        var e = Estado(id);
        if (e.status != 1) return "Não está em desenvolvimento.";
        e.status = e.novaVersao ? 3 : 2;
        if (e.novaVersao) { e.novaVersao = false; e.mesesFeitos = 0; return "Nova versão cancelada."; }
        return "Desenvolvimento pausado. O que já foi feito não se perde.";
    }

    // Uma vez por mês: cobra a equipe, avança o desenvolvimento e cobra a manutenção dos lançados
    public static void NovoDia(int dia)
    {
        int mes = EmpresaManager.MesDoDia(dia);
        if (ultimoMes < 0) { ultimoMes = mes; return; }
        while (ultimoMes < mes)
        {
            ultimoMes++;
            var inicio = EmpresaManager.PrimeiroDiaDoMes(ultimoMes);
            int venc = EmpresaManager.DiaDaData(inicio.AddDays(4));
            foreach (var p in Lista)
            {
                var e = Estado(p.id);
                if (e.status == 1)
                {
                    Cobrar($"Equipe de desenvolvimento: {p.nome}", CustoMensal(p.id), venc, ultimoMes - 1);
                    e.mesesFeitos++;
                    int total = e.novaVersao ? MesesNovaVersao : MesesTotais(p.id);
                    if (e.mesesFeitos >= total)
                    {
                        e.versao++; e.status = 3; e.novaVersao = false; e.mesesFeitos = 0;
                        GameUI.Instance?.Toast(e.versao == 1
                            ? $"<b>Lançamento!</b> O {p.nome} está pronto. {p.efeito}"
                            : $"Versão {e.versao} do {p.nome} lançada: menos bugs nos clientes.", UIFactory.Sucesso, 7f);
                        Conquistas.Desbloquear("produto-proprio");
                    }
                }
                if (e.status == 3) Cobrar($"Manutenção e servidores: {p.nome}", p.manutencaoMensal, venc, ultimoMes - 1);
            }
        }
    }

    static void Cobrar(string nome, float valor, int vencimento, int competencia)
    {
        if (EmpresaManager.Instance != null) EmpresaManager.Instance.AdicionarGuia(nome, valor, vencimento, competencia);
        else GameEconomy.Instance?.Gastar(valor);
    }

    [System.Serializable]
    public class Dados { public List<EstadoProduto> estados = new List<EstadoProduto>(); public int ultimoMes = -1; }

    public static Dados Exportar() => new Dados { estados = new List<EstadoProduto>(estados ?? new List<EstadoProduto>()), ultimoMes = ultimoMes };
    public static void Carregar(Dados d)
    {
        if (d == null || d.estados == null || d.estados.Count == 0) { Iniciar(); if (d != null) ultimoMes = d.ultimoMes; return; }
        estados = d.estados; ultimoMes = d.ultimoMes;
    }
}
