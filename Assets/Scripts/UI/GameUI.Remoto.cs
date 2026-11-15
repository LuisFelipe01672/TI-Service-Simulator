using System.Collections;
using UnityEngine;

// Sessão de acesso remoto vista do PC: liga para o cliente, conecta e tenta resolver (MissionManager.AtenderRemoto)
public partial class GameUI
{
    bool sessaoRemotaEmAndamento;

    IEnumerator SessaoRemota(Chamado c)
    {
        var mm = MissionManager.Instance;
        if (mm == null || sessaoRemotaEmAndamento) yield break;
        sessaoRemotaEmAndamento = true;
        AtualizarTablet();

        SomManager.Tocar("bip", 0.5f);
        Toast($"Ligando para {c.contato} ({c.nomeCliente}) e passando o código do acesso remoto...", UIFactory.Destaque, 2.5f);
        yield return new WaitForSecondsRealtime(2.2f);
        Toast("Conectando ao computador do cliente...", UIFactory.Destaque, 2f);
        yield return new WaitForSecondsRealtime(1.8f);

        var r = mm.AtenderRemoto(c, out string msg);
        var cor = r == MissionManager.ResultadoRemoto.Resolvido ? UIFactory.Sucesso
                : r == MissionManager.ResultadoRemoto.ClienteAusente ? UIFactory.Alerta : UIFactory.Perigo;
        Toast(msg, cor, 5f);
        if (r == MissionManager.ResultadoRemoto.Resolvido) SomManager.Tocar("sucesso", 0.7f);
        else SomManager.Tocar("erro", 0.5f);

        sessaoRemotaEmAndamento = false;
        AtualizarTablet();
    }
}
