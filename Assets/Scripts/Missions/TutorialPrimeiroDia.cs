// Primeiro dia do jogo novo: um chamado guiado de toner. O card do chamado mostra o próximo passo
// conforme o que o técnico já fez (lê o estado do equipamento, sem roteiro fixo: dá para errar e corrigir).
public static class TutorialPrimeiroDia
{
    public const string Solucao = "Trocar toner";

    public static string Passo(Chamado ch)
    {
        if (ch == null) return null;
        var e = ch.estado;
        if (ch.aguardandoAssinatura)
            return $"Funcionou! Recolha o que caiu no chão, depois fale com {ch.contato} (E) para conferir o checklist e assinar a OS.";
        if (e == null) return "Aceite o chamado no tablet (Tab).";
        if (!ch.conversou)
            return $"Fale com {ch.contato}: mire nele(a), aperte E e pergunte se aparece alguma mensagem.";
        if (e.cartuchoNaMao && e.cartuchoNaMaoUsado)
            return e.cartuchoNaMaoToner < 0.1f
                ? "O cartucho está levinho: o toner acabou. Guarde-o na mochila (descarte) para liberar a impressora."
                : "Olhe o cartucho na mão: pesado ou leve?";
        if (e.cartuchoNaMao && e.cartuchoNaMaoComLacre)
            return "Antes de encaixar, puxe o lacre (a fita comprida). Sem isso a folha sai em branco.";
        if (e.cartuchoNaMao)
            return "Encaixe o cartucho novo na impressora até dar o clique.";
        if (!e.impCartuchoPresente)
            return e.tonerDoCliente ? "Pegue o toner novo que o cliente deixou no armário (na tampa aberta da impressora)."
                                    : "Coloque um toner novo (da mochila ou da loja).";
        if (e.impToner < 0.1f)
        {
            if (e.ultimaPagina == ResultadoPagina.Nenhuma)
                return "Vá até a impressora, olhe o visor e imprima uma página de teste (botão da impressora).";
            if (!e.impTampaAberta)
                return "A folha saiu clara e falhada: levante o scanner (tampa de cima) para chegar no toner.";
            return "Retire o cartucho de toner e olhe se está leve.";
        }
        if (e.impTampaAberta) return "Baixe o scanner (feche a tampa de cima).";
        return "Imprima uma página de teste para conferir se ficou boa.";
    }
}
