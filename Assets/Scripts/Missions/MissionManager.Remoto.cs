using System.Collections.Generic;
using UnityEngine;

// Atendimento remoto (Fase 1A): do PC de casa, o técnico liga para o cliente, conecta pelo programa de acesso
// remoto e resolve o que é de software. Defeito físico (cabo, toner, roteador fora) exige ir ao local.
public partial class MissionManager
{
    public const float FatorPagamentoRemoto = 0.75f;   // sem deslocamento: cobra menos
    public const float MinutosAtendimentoRemoto = 30f;
    public const float ChanceClienteAusente = 0.2f;

    public enum ResultadoRemoto { Resolvido, ClienteAusente, SemInternet, PrecisaVisita, Bloqueado }

    // Dá para aplicar de longe? (marcado no catálogo com .Remoto(); o resto precisa das mãos no equipamento)
    public static bool SolucaoRemota(Problema p) => p != null && p.remoto;

    // Problema que derruba a internet do cliente (.SemInternet() no catálogo): nem dá para conectar
    static bool SemInternetNoCliente(Problema p) => p != null && p.derrubaInternet;

    // Tenta resolver um chamado aberto à distância. Sempre gasta tempo do dia (a ligação e a tentativa).
    public ResultadoRemoto AtenderRemoto(Chamado c, out string mensagem)
    {
        mensagem = "";
        if (chamadoAtivo != null) { mensagem = "Termine o chamado em atendimento antes."; return ResultadoRemoto.Bloqueado; }
        var empresa = EmpresaManager.Instance;
        if (empresa != null && !empresa.PodeAtender(c, out string motivo)) { mensagem = motivo; return ResultadoRemoto.Bloqueado; }

        var dia = DiaManager.Instance;
        void Gastar(float minutos) { if (dia != null) dia.minutoAtual += minutos; }

        if (Random.value < ChanceClienteAusente)
        {
            Gastar(10f);
            mensagem = $"{c.contato} não está no computador para aceitar a conexão. Tente de novo mais tarde.";
            return ResultadoRemoto.ClienteAusente;
        }
        if (SemInternetNoCliente(c.problema))
        {
            Gastar(15f);
            c.anotacoes.Add("Remoto falhou: o cliente está sem internet (problema no roteador).");
            mensagem = $"Não conectou: {c.nomeCliente} está sem internet. Problema no roteador — precisa ir lá.";
            return ResultadoRemoto.SemInternet;
        }
        if (!SolucaoRemota(c.problema))
        {
            Gastar(25f);
            string porque = c.problema?.motivoPresencial;
            if (porque != null)
            {
                c.anotacoes.Add("Remoto não deu: " + porque);
                mensagem = $"Não deu para atender {c.nomeCliente} de longe: {porque} Precisa ir ao local.";
                return ResultadoRemoto.PrecisaVisita;
            }
            c.anotacoes.Add("Remoto: conectou, mas o defeito é físico (cabo, toner ou peça). Precisa de visita.");
            mensagem = $"Conectou no {c.EquipamentoTexto().ToLowerInvariant()} de {c.nomeCliente}, mas o defeito é físico (cabo, toner ou peça). Precisa ir ao local.";
            return ResultadoRemoto.PrecisaVisita;
        }

        // Resolvido de longe: conclui como um atendimento normal, cobrando a tarifa de acesso remoto
        Gastar(MinutosAtendimentoRemoto);
        chamadoAtivo = c;
        chamadosDisponiveis.Remove(c);
        c.estado = new EstadoEquipamentos();
        c.aceitoEm = SLA.Agora() - MinutosAtendimentoRemoto;
        c.os = NovaOS(c);
        c.os.diagnostico = $"Resolvido por acesso remoto: {c.problema?.solucao.ToLowerInvariant()}.";
        c.os.RegistrarAcao("Ligou para o cliente e conectou pelo programa de acesso remoto");
        c.os.RegistrarAcao(c.problema?.comoRemoto ?? $"Acesso remoto: {c.problema?.solucao}");
        float original = c.recompensa;
        c.recompensa = Mathf.Round(original * FatorPagamentoRemoto);
        c.anotacoes.Add($"Resolvido por acesso remoto: {c.problema?.solucao}.");
        mensagem = $"Resolvido por acesso remoto em {c.nomeCliente}: {c.problema?.solucao.ToLowerInvariant()}." +
                   (c.contratoId > 0 ? "" : $" Tarifa remota: {UIFactory.Dinheiro(c.recompensa)}.");
        ConcluirChamado();
        return ResultadoRemoto.Resolvido;
    }
}
