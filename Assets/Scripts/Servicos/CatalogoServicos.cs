using UnityEngine;

// Serviços vendidos por mês (receita recorrente). Nomes fictícios.
// Estrutura e preços inspirados numa tabela real de uma empresa de TI (arredondados);
// custos são estimativas (o que a empresa paga ao fornecedor) para dar margem realista.
public enum TipoServico { Suporte, Email, Backup, Bot, Notebook, MiniPc }

public class ServicoInfo
{
    public TipoServico tipo;
    public string nome, unidade, descricao;
    public bool exigeEquipamento;        // notebook/mini PC: precisa comprar e preparar cada unidade
    public float custoEquipamento;       // preço de compra de cada unidade
    public bool porPrazo;                // preço depende do prazo (12/24/36 meses)
    public string requisito;             // texto mostrado quando bloqueado

    public ServicoInfo(TipoServico tipo, string nome, string unidade, string descricao)
    { this.tipo = tipo; this.nome = nome; this.unidade = unidade; this.descricao = descricao; }
}

public static class CatalogoServicos
{
    public const float HoraTecnica = 125f;
    public const float Deslocamento = 50f;

    public static readonly ServicoInfo[] Servicos =
    {
        new ServicoInfo(TipoServico.Suporte, "Suporte gerenciado", "máquina",
            "Antivírus + monitoramento remoto em cada máquina. Alertas chegam na sua central antes do cliente perceber."),
        new ServicoInfo(TipoServico.Email, "E-mail e Office na nuvem", "conta",
            "Contas de e-mail e Office por usuário. Você cria usuários, reseta senhas e cuida das caixas."),
        new ServicoInfo(TipoServico.Backup, "Backup gerenciado", "GB",
            "Cópia diária para a nuvem. Falhas aparecem na central; teste de restauração todo mês. Salva o cliente num ataque de ransomware."),
        new ServicoInfo(TipoServico.Bot, "Bot de atendimento no WhatsApp", "bot",
            "Atendimento automático com menus. Até 5 usuários, 2 números de WhatsApp e 2 contas de Instagram."),
        new ServicoInfo(TipoServico.Notebook, "Locação de notebook", "notebook",
            "Notebook preparado na bancada (formatado, imagem padrão, antivírus, Office) e entregue no cliente.")
            { exigeEquipamento = true, custoEquipamento = 3200f, porPrazo = true },
        new ServicoInfo(TipoServico.MiniPc, "Locação de mini PC", "mini PC",
            "Mini PC com Windows Pro preparado na bancada. Combos com antivírus e Office.")
            { exigeEquipamento = true, custoEquipamento = 1800f, porPrazo = true },
    };

    public static ServicoInfo Info(TipoServico t)
    {
        foreach (var s in Servicos) if (s.tipo == t) return s;
        return Servicos[0];
    }

    // ---------- Preço por unidade ao cliente ----------

    public static float PrecoUnidade(TipoServico t, int unidades, int meses)
    {
        switch (t)
        {
            case TipoServico.Suporte: return 39.20f;                                    // antivírus 15,60 + monitoramento 23,60
            case TipoServico.Email: return unidades <= 5 ? 36f : unidades <= 20 ? 32f : 28f;
            case TipoServico.Notebook: return meses >= 36 ? 160f : meses >= 24 ? 170f : 280f;
            case TipoServico.MiniPc: return meses >= 36 ? 175f : meses >= 24 ? 195f : 215f;   // combo com antivírus
            default: return 0f;
        }
    }

    // Mensal do contrato inteiro
    public static float Mensal(TipoServico t, int unidades, int meses)
    {
        switch (t)
        {
            case TipoServico.Backup: return PrecoBackup(unidades);
            case TipoServico.Bot: return 199f;
            default: return PrecoUnidade(t, unidades, meses) * unidades;
        }
    }

    // Implantação (cobrada uma vez)
    public static float Implantacao(TipoServico t, int unidades)
    {
        switch (t)
        {
            case TipoServico.Suporte: return HoraTecnica * Mathf.CeilToInt(unidades / 4f);
            case TipoServico.Email: return (unidades <= 5 ? 190f : unidades <= 20 ? 170f : 150f) * unidades;
            case TipoServico.Backup: return HoraTecnica * 2f;
            case TipoServico.Bot: return 800f;
            default: return 150f;
        }
    }

    // Faixas de backup por volume (GB): preço por GB cai com o volume
    static readonly int[] faixasGB = { 50, 100, 200, 250, 300, 400, 500, 700, 1000 };
    static readonly float[] precosGB = { 200f, 250f, 350f, 400f, 450f, 532f, 640f, 889f, 1200f };

    public static int FaixaBackup(int gb)
    {
        for (int i = 0; i < faixasGB.Length; i++) if (gb <= faixasGB[i]) return faixasGB[i];
        return faixasGB[faixasGB.Length - 1];
    }

    public static float PrecoBackup(int gb)
    {
        for (int i = 0; i < faixasGB.Length; i++) if (gb <= faixasGB[i]) return precosGB[i];
        return precosGB[precosGB.Length - 1] * gb / 1000f;
    }

    // ---------- Custo mensal para a empresa (estimativas) ----------

    public static float CustoMensal(TipoServico t, int unidades)
    {
        switch (t)
        {
            case TipoServico.Suporte: return unidades * 39.20f / CatalogoLocacao.MarkupPecas;   // licenças compradas do fornecedor
            case TipoServico.Email: return unidades * 27f;                                      // licença no parceiro de nuvem (aprox.)
            case TipoServico.Backup: return FaixaBackup(unidades) * 0.15f + 25f;                // armazenamento + licença do agente
            case TipoServico.Bot: return 60f;                                                   // plataforma (mensagens à parte)
            default: return 20f * unidades;                                                     // seguro/manutenção do equipamento
        }
    }

    public static readonly string[] Prazos = { "12 meses", "24 meses", "36 meses" };

    public static string SortearEmpresa()
    {
        string[] nomes =
        {
            "Escritório Alves & Filhos", "Clínica Odonto Sorriso", "Transportadora Veloz", "Colégio Novo Saber",
            "Restaurante Sabor da Casa", "Academia Corpo Ativo", "Cartório 2º Ofício", "Indústria Metalfix",
            "Loja Moda Nova", "Escritório de Engenharia Prumo", "Consultório Dra. Helena", "Hotel Recanto"
        };
        return nomes[Random.Range(0, nomes.Length)];
    }
}
