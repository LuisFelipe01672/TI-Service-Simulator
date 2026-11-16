using UnityEngine;

// Etapa 7: a pista de cada problema de software aparece na tela 3D do equipamento do chamado
// (monitor do computador ou tela do notebook): conta bloqueada, propagandas, erro do sistema contábil,
// e-mail preso na caixa de saída, janela de salvar PDF, programas abrindo na inicialização.
public partial class EquipamentoPecas
{
    TelaComputador telaSw;

    void AtualizarTelaSoftware(EstadoEquipamentos e)
    {
        if (telaSw == null && telaViva != null) telaSw = telaViva.GetComponentInChildren<TelaComputador>(true);
        if (telaSw == null) return;
        var ch = MissionManager.Instance != null ? MissionManager.Instance.chamadoAtivo : null;
        // Só a tela do equipamento do chamado mostra os sintomas (o estado é um só para o escritório)
        if (e == null || ch == null || ch.equipamento != eq.tipoEquipamento) { telaSw.Pista(null, null, null, Color.clear); return; }

        if (e.swContaBloqueada)
            telaSw.Pista("bloqueio", $"{e.swUsuario}",
                "A conta foi bloqueada.\nMuitas tentativas de senha incorreta.\n\n<size=80%>Tente mais tarde ou fale com o administrador.</size>",
                Color.clear, true);
        else if (e.swPopups || e.swPaginaTrocada)
            telaSw.Pista("virus", e.swPaginaTrocada ? "Navegador — ofertas-imperdiveis.com" : "", e.swPaginaTrocada ? "Página inicial: <b>buscador de ofertas</b>\nSUPER PROMOÇÕES PARA VOCÊ!!!" : "",
                new Color(0.75f, 0.2f, 0.2f), false, e.swPopups ? 4 : 0);
        else if (e.swAtualizacaoRuim)
            telaSw.Pista("atualizacao", "Sistema Contábil Ômega",
                "<color=#B23A48><b>Erro 0x80</b></color>\nComponente de banco de dados incompatível.\nContate o suporte técnico.\n\n[ OK ]",
                new Color(0.55f, 0.1f, 0.12f));
        else if (e.swEmailPresos > 0)
            telaSw.Pista("email" + e.swEmailPresos, "E-mail — Caixa de saída (" + e.swEmailPresos + ")",
                "<color=#B23A48>Não foi possível enviar.</color>\nO servidor de saída não respondeu.\n\n» Proposta_cliente.pdf\n» Boleto_novembro.pdf\n» Re: orçamento",
                new Color(0.15f, 0.35f, 0.65f));
        else if (e.swPadraoErrado)
            telaSw.Pista("pdf", "Salvar saída de impressão como",
                "Nome do arquivo: <b>Boleto.pdf</b>\nTipo: Documento PDF\n\n[ Salvar ]   [ Cancelar ]",
                new Color(0.25f, 0.3f, 0.38f));
        else if (e.InicializacaoCheia)
            telaSw.Pista("inicio", "Otimizador Turbo PC",
                $"Iniciando programas... ({e.ProgramasInicioDesnecessarios + 3} na inicialização)\n<color=#B23A48>Seu computador tem 1.284 problemas!</color>\n\n[ Corrigir agora - R$ 89,90 ]",
                new Color(0.1f, 0.5f, 0.3f));
        else
            telaSw.Pista(null, null, null, Color.clear);
    }
}
