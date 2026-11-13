using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Parte da GameUI: janela de conversa com o cliente.
// O jogador escolhe perguntas; cada resposta vira uma pista anotada no chamado.
public partial class GameUI
{
    RectTransform conversa;
    TextMeshProUGUI txtConvTitulo, txtConvFala;
    RectTransform convPerguntas, convAnotacoes;
    ClienteNPC npcConversa;
    string falaAtual;

    void ConstruirConversa(RectTransform pai)
    {
        conversa = JanelaModal(pai, "Conversa", new Vector2(980, 0));
        UIFactory.Vertical(conversa, 14, 32);
        UIFactory.AjustarAoConteudo(conversa);

        txtConvTitulo = UIFactory.Texto(conversa, "", 32, UIFactory.CorTexto, FontStyles.Bold);

        // Balão com a fala do cliente
        var balao = UIFactory.Painel(conversa, "Fala", UIFactory.CartaoClaro);
        UIFactory.Vertical(balao, 0, 22);
        txtConvFala = UIFactory.Texto(balao, "", 24, UIFactory.CorTexto, FontStyles.Italic);

        UIFactory.Texto(conversa, "PERGUNTAR", 18, UIFactory.Destaque, FontStyles.Bold);
        convPerguntas = UIFactory.Vazio(conversa, "Perguntas");
        UIFactory.Vertical(convPerguntas, 8);

        UIFactory.Texto(conversa, "ANOTAÇÕES", 18, UIFactory.CorSuave, FontStyles.Bold);
        convAnotacoes = UIFactory.Vazio(conversa, "Anotacoes");
        UIFactory.Vertical(convAnotacoes, 4);

        UIFactory.Botao(conversa, "Encerrar conversa", FecharModal, UIFactory.CartaoClaro, 260, 50);
    }

    public void AbrirConversa(ClienteNPC npc)
    {
        if (npc == null || npc.chamado == null) return;
        var ch = npc.chamado;
        npcConversa = npc;
        AbrirModal(Modal.Conversa, conversa);

        // Primeira vez: o cliente se apresenta e conta o problema do jeito dele
        falaAtual = ch.perguntasFeitas.Count == 0
            ? CatalogoProblemas.Saudacao(ch.personalidade, ch.DescricaoVisivel)
            : "Mais alguma pergunta?";
        MontarConversa();
    }

    void MontarConversa()
    {
        var npc = npcConversa;
        if (npc == null || npc.chamado == null) { FecharModal(); return; }
        var ch = npc.chamado;
        var mm = MissionManager.Instance;

        txtConvTitulo.text = $"{ch.contato}  <size=60%><color=#9AA6B8>{ch.nomeCliente}</color>   " +
                             $"<color={ch.CorPersonalidade()}>{ch.PersonalidadeTexto().ToUpper()}</color></size>";
        txtConvFala.text = $"\"{falaAtual}\"";

        UIFactory.Limpar(convPerguntas);
        int restantes = 0;
        foreach (Pergunta p in System.Enum.GetValues(typeof(Pergunta)))
        {
            if (ch.perguntasFeitas.Contains(p)) continue;
            restantes++;
            var pergunta = p;
            UIFactory.Botao(convPerguntas, CatalogoProblemas.TextoPergunta(pergunta), () =>
            {
                if (MissionManager.Instance == null) return;
                falaAtual = MissionManager.Instance.Perguntar(pergunta);
                MontarConversa();
            }, UIFactory.CartaoClaro, 900, 48, 21);
        }
        if (restantes == 0)
            UIFactory.Texto(convPerguntas, "Você já perguntou tudo. Agora é com você: vá ao equipamento e teste.", 20, UIFactory.CorSuave);
        else
            UIFactory.Texto(convPerguntas, $"<size=85%>Cada pergunta gasta {(mm != null ? mm.custoPergunta : 5f):F0}s do prazo." +
                (ch.personalidade == Personalidade.Apressado ? " Este cliente está com pressa: não exagere." : "") + "</size>",
                18, UIFactory.CorSuave);

        UIFactory.Limpar(convAnotacoes);
        if (ch.anotacoes.Count == 0)
            UIFactory.Texto(convAnotacoes, "Nada anotado ainda.", 19, UIFactory.CorSuave);
        // Só as últimas, para a janela não passar da tela (todas ficam no tablet)
        int inicio = Mathf.Max(0, ch.anotacoes.Count - 5);
        if (inicio > 0)
            UIFactory.Texto(convAnotacoes, $"<size=85%>+{inicio} anotação(ões) antiga(s) no tablet</size>", 18, UIFactory.CorSuave);
        for (int i = inicio; i < ch.anotacoes.Count; i++)
            UIFactory.Texto(convAnotacoes, "• " + ch.anotacoes[i], 19, UIFactory.CorTexto);

        LayoutRebuilder.MarkLayoutForRebuild(conversa);
    }
}
