using System.Collections.Generic;
using UnityEngine;

public class InfoConquista
{
    public string id, nome, descricao;
    public InfoConquista(string id, string nome, string descricao) { this.id = id; this.nome = nome; this.descricao = descricao; }
}

// Conquistas: marcos da carreira, do primeiro chamado ao império de TI.
// Algumas são verificadas todo dia (BancoManager chama Verificar); outras são desbloqueadas na hora pelo sistema que as causa.
public static class Conquistas
{
    public static readonly InfoConquista[] Lista =
    {
        new InfoConquista("primeiro-chamado", "Primeiro chamado", "Resolveu o primeiro chamado."),
        new InfoConquista("certeiro", "Olho clínico", "10 diagnósticos certeiros (sem errar e com conversa)."),
        new InfoConquista("cem-chamados", "Calejado", "100 chamados resolvidos."),
        new InfoConquista("cnpj", "CNPJ na mão", "Abriu o MEI."),
        new InfoConquista("me", "Gente grande", "Virou Microempresa."),
        new InfoConquista("sede", "Endereço comercial", "Alugou a primeira sala."),
        new InfoConquista("equipe", "Não estou mais sozinho", "Contratou o primeiro funcionário."),
        new InfoConquista("equipe-5", "Time completo", "5 funcionários na equipe."),
        new InfoConquista("parceria", "Revenda autorizada", "Fechou a primeira parceria com fornecedor."),
        new InfoConquista("ouro", "Parceiro Ouro", "Chegou ao nível Ouro com um fornecedor."),
        new InfoConquista("contrato", "Receita que se repete", "Fechou o primeiro contrato mensal."),
        new InfoConquista("mrr-5k", "Dorme tranquilo", "R$ 5 mil por mês em contratos."),
        new InfoConquista("mrr-20k", "Máquina de recorrência", "R$ 20 mil por mês em contratos."),
        new InfoConquista("projeto", "Mãos na massa", "Entregou o primeiro projeto de infraestrutura."),
        new InfoConquista("firewall", "Muralha", "Instalou o primeiro firewall."),
        new InfoConquista("heroi", "Herói do ransomware", "Salvou um cliente de um ataque com o backup testado."),
        new InfoConquista("faturou-100k", "Seis dígitos", "R$ 100 mil faturados no total."),
        new InfoConquista("emprestimo-quitado", "Nome limpo", "Pagou um empréstimo até a última parcela."),
    };

    public static List<string> desbloqueadas = new List<string>();
    public static int chamados, certeiros;

    public static bool Tem(string id) => desbloqueadas.Contains(id);

    public static void Desbloquear(string id)
    {
        if (desbloqueadas == null) desbloqueadas = new List<string>();
        if (Tem(id)) return;
        var info = System.Array.Find(Lista, c => c.id == id);
        if (info == null) return;
        desbloqueadas.Add(id);
        GameUI.Instance?.Toast($"<b>Conquista desbloqueada: {info.nome}</b>\n{info.descricao}", UIFactory.Hex("#C9A227"), 5f);
        Debug.Log($"[Conquistas] {info.nome}");
    }

    public static void RegistrarChamado(bool certeiro)
    {
        chamados++;
        if (certeiro) certeiros++;
        if (chamados >= 1) Desbloquear("primeiro-chamado");
        if (certeiros >= 10) Desbloquear("certeiro");
        if (chamados >= 100) Desbloquear("cem-chamados");
    }

    // Verificação diária das conquistas de estado
    public static void Verificar()
    {
        var emp = EmpresaManager.Instance;
        if (emp != null)
        {
            if (emp.TemCnpj) Desbloquear("cnpj");
            if (emp.regime == Regime.ME) Desbloquear("me");
            float total = 0f;
            foreach (var f in emp.faturamentoMes) total += f;
            if (total >= 100000f) Desbloquear("faturou-100k");
        }
        if (SedeManager.Instance != null && SedeManager.Instance.imovel > 0) Desbloquear("sede");
        int equipe = FuncionarioManager.Instance != null ? FuncionarioManager.Instance.equipe.Count : 0;
        if (equipe >= 1) Desbloquear("equipe");
        if (equipe >= 5) Desbloquear("equipe-5");
        if (Parcerias.MaiorNivel() >= 2) Desbloquear("ouro");

        float mrr = Mrr();
        if (mrr > 0f) Desbloquear("contrato");
        if (mrr >= 5000f) Desbloquear("mrr-5k");
        if (mrr >= 20000f) Desbloquear("mrr-20k");
    }

    // Receita mensal recorrente: serviços + locação de impressoras
    public static float Mrr()
    {
        float t = ServicosManager.Instance != null ? ServicosManager.Instance.Mrr : 0f;
        if (ContratosManager.Instance != null)
            foreach (var c in ContratosManager.Instance.contratos) if (c.ativo && c.Instalado) t += c.mensal;
        return t;
    }

    [System.Serializable]
    public class Dados
    {
        public List<string> desbloqueadas = new List<string>();
        public int chamados, certeiros;
    }

    public static Dados Exportar() => new Dados { desbloqueadas = new List<string>(desbloqueadas ?? new List<string>()), chamados = chamados, certeiros = certeiros };

    public static void Carregar(Dados d)
    {
        if (d == null) { Iniciar(); return; }
        desbloqueadas = d.desbloqueadas ?? new List<string>();
        chamados = d.chamados; certeiros = d.certeiros;
    }

    public static void Iniciar() { desbloqueadas = new List<string>(); chamados = certeiros = 0; }
}
