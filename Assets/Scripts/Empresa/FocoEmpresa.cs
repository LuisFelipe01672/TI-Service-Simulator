using UnityEngine;

public enum AreaNegocio { Nenhuma, Impressao, Gerenciados, Infraestrutura, Software }

// Foco (ramo) da empresa: escolher uma área dá vantagem nela e deixa as outras um pouco de lado.
// O primeiro foco é de graça; trocar custa um "rebranding" e só pode a cada 30 dias.
public static class FocoEmpresa
{
    public const float CustoTroca = 2000f;
    public const int DiasEntreTrocas = 30;
    public const float BonusProposta = 0.08f;
    public const float MultLeadsFoco = 1.5f, MultLeadsOutras = 0.85f;

    public static AreaNegocio atual = AreaNegocio.Nenhuma;
    public static int diaEscolha = -999;

    public static readonly string[] Nomes = { "Sem foco", "Outsourcing de impressão", "Serviços gerenciados", "Infraestrutura e redes", "Software" };
    public static readonly string[] Efeitos =
    {
        "Atende de tudo um pouco, sem vantagem em nada.",
        "50% mais contatos de locação, propostas de impressora 8% mais fortes e tinta 5% mais barata.",
        "50% mais contatos de suporte/e-mail/backup/bot, propostas 8% mais fortes e alertas resolvidos 25% mais rápido.",
        "50% mais pedidos de projeto, orçamentos 8% mais fortes e projetos com 15% menos horas.",
        "Desenvolver produtos próprios custa 30% menos e leva 25% menos tempo; propostas de ERP e bot 8% mais fortes.",
    };

    static int Hoje => DiaManager.Instance != null ? DiaManager.Instance.dia : 1;

    public static string Escolher(AreaNegocio area)
    {
        if (area == atual) return "Esse já é o seu foco.";
        var emp = EmpresaManager.Instance;
        if (emp != null && !emp.TemCnpj) return "Escolher um ramo é para empresas: abra o CNPJ primeiro.";
        bool primeira = atual == AreaNegocio.Nenhuma && diaEscolha < 0;
        if (!primeira && Hoje - diaEscolha < DiasEntreTrocas) return $"Você mudou de foco há pouco. Pode trocar de novo em {DiasEntreTrocas - (Hoje - diaEscolha)} dia(s).";
        if (!primeira)
        {
            var eco = GameEconomy.Instance;
            if (eco != null && eco.saldo < CustoTroca) return $"Mudar de foco custa {UIFactory.Dinheiro(CustoTroca)} (site, material, divulgação).";
            eco?.Gastar(CustoTroca, LivroCaixa.Marketing);
        }
        atual = area; diaEscolha = Hoje;
        return $"Foco da empresa: <b>{Nomes[(int)area]}</b>. {Efeitos[(int)area]}";
    }

    public static float MultLeads(AreaNegocio area) => atual == AreaNegocio.Nenhuma ? 1f : atual == area ? MultLeadsFoco : MultLeadsOutras;
    public static float Vantagem(AreaNegocio area) => atual != AreaNegocio.Nenhuma && atual == area ? BonusProposta : 0f;

    public static AreaNegocio AreaDoServico(TipoServico t) =>
        t == TipoServico.Erp || t == TipoServico.Bot ? AreaNegocio.Software
        : t == TipoServico.Firewall ? AreaNegocio.Infraestrutura
        : t == TipoServico.Notebook || t == TipoServico.MiniPc ? AreaNegocio.Impressao
        : AreaNegocio.Gerenciados;

    public static float MultSuprimento => atual == AreaNegocio.Impressao ? 0.95f : 1f;
    public static float MultMinutosAlerta => atual == AreaNegocio.Gerenciados ? 0.75f : 1f;
    public static float MultHorasProjeto => atual == AreaNegocio.Infraestrutura ? 0.85f : 1f;
    public static float MultCustoDev => atual == AreaNegocio.Software ? 0.7f : 1f;
    public static float MultTempoDev => atual == AreaNegocio.Software ? 0.75f : 1f;

    [System.Serializable]
    public class Dados { public int area; public int diaEscolha = -999; }

    public static Dados Exportar() => new Dados { area = (int)atual, diaEscolha = diaEscolha };
    public static void Carregar(Dados d) { if (d == null) { Iniciar(); return; } atual = (AreaNegocio)d.area; diaEscolha = d.diaEscolha; }
    public static void Iniciar() { atual = AreaNegocio.Nenhuma; diaEscolha = -999; }
}
