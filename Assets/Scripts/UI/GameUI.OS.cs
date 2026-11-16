using System.Collections;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Parte da GameUI: ordem de serviço (OS) na hora de fechar o atendimento.
// O técnico mostra a OS ao cliente, ele confere o checklist e assina a prancheta (animação na ClienteNPC).
// Depois aparece o cartão com as estrelas e o comentário dele.
public partial class GameUI
{
    RectTransform janelaOS, osConteudo;
    ClienteNPC npcOS;

    void ConstruirOS(RectTransform pai)
    {
        janelaOS = JanelaModal(pai, "OrdemServico", new Vector2(900, 0));
        UIFactory.Vertical(janelaOS, 10, 30);
        UIFactory.AjustarAoConteudo(janelaOS);
        osConteudo = UIFactory.Vazio(janelaOS, "Conteudo");
        UIFactory.Vertical(osConteudo, 8);
    }

    // Chamado pela ClienteNPC quando o defeito já foi resolvido
    public void AbrirOS(ClienteNPC npc)
    {
        if (npc == null || npc.chamado == null || npc.chamado.os == null) return;
        npcOS = npc;
        AbrirModal(Modal.OS, janelaOS);
        MontarOS();
    }

    void MontarOS()
    {
        var mm = MissionManager.Instance;
        var ch = npcOS != null ? npcOS.chamado : null;
        if (mm == null || ch == null || ch.os == null) { FecharModal(); return; }
        var os = ch.os;
        UIFactory.Limpar(osConteudo);

        UIFactory.Texto(osConteudo, $"ORDEM DE SERVIÇO  Nº {os.numero:0000}" + (os.retrabalho ? "   <color=#F2545B><size=70%>RETORNO EM GARANTIA</size></color>" : ""),
            30, UIFactory.CorTexto, FontStyles.Bold);

        // A "folha" da OS
        var folha = UIFactory.Painel(osConteudo, "Folha", UIFactory.Hex("#EEF0EA"));
        UIFactory.Vertical(folha, 4, 22);
        var tinta = UIFactory.Hex("#1A2433");
        var cinza = UIFactory.Hex("#5A6472");
        void Linha(string t, float tam = 19) => UIFactory.Texto(folha, t, tam, tinta);
        void Titulo(string t) => UIFactory.Texto(folha, t, 15, cinza, FontStyles.Bold);

        Linha($"<b>Cliente:</b> {os.cliente}  ·  <b>Contato:</b> {os.contato}");
        Linha($"<b>Aberta:</b> {os.abertura}  ·  <b>Prioridade:</b> {os.prioridade}  ·  <b>Equipamento:</b> {os.equipamento}", 17);
        Titulo("RELATO DO CLIENTE");
        Linha($"\"{os.relato}\"");
        Titulo("DIAGNÓSTICO");
        Linha(string.IsNullOrEmpty(os.diagnostico) ? "—" : os.diagnostico);
        Titulo("SERVIÇOS EXECUTADOS");
        int inicio = Mathf.Max(0, os.acoes.Count - 8);
        if (inicio > 0) Linha($"<size=85%>(+{inicio} registro(s) anterior(es))</size>", 16);
        for (int i = inicio; i < os.acoes.Count; i++) Linha("• " + os.acoes[i], 17);
        if (os.acoes.Count == 0) Linha("—");
        if (os.laudo != null && os.laudo.Count > 0)
        {
            Titulo("LAUDO TÉCNICO (medições)");
            foreach (var m in os.laudo) Linha("• " + m, 16);
            if (os.recomendacoes != null && os.recomendacoes.Count > 0)
                foreach (var r in os.recomendacoes) Linha("<color=#B3262E>Recomendação:</color> " + r, 16);
            else Linha("<color=#1E7A4C>Sem peças no fim da vida.</color>", 16);
        }
        Titulo("PEÇAS E MATERIAIS");
        if (os.pecas.Count == 0) Linha("Nenhuma.", 17);
        foreach (var p in os.pecas) Linha("• " + p, 17);

        float agora = SLA.Agora();
        float trabalho = agora - ch.aceitoEm;
        float restante = SLA.Restante(ch, agora);
        string valor = ch.retrabalho ? "garantia (sem cobrança do serviço)" : ch.contratoId > 0 ? "incluso no contrato"
                     : UIFactory.Dinheiro(ch.recompensa);
        Linha($"<b>Tempo de trabalho:</b> {OrdemServico.HorasTexto(trabalho)}  ·  <b>SLA:</b> " +
              (restante >= 0 ? "<color=#1E7A4C>no prazo</color>" : $"<color=#B3262E>estourado há {OrdemServico.HorasTexto(-restante)}</color>"), 17);
        Linha($"<b>Serviço:</b> {valor}  ·  <b>Peças:</b> {UIFactory.Dinheiro(os.valorPecas)}", 17);

        // Checklist que o cliente confere
        UIFactory.Texto(osConteudo, "CHECKLIST (o cliente confere antes de assinar)", 17, UIFactory.Destaque, FontStyles.Bold);
        var lista = mm.ChecklistAtual();
        foreach (var item in lista)
            UIFactory.Texto(osConteudo, item.ok ? $"<color=#3CCB7F>[OK]</color>  {item.texto}" : $"<color=#F2545B>[FALTA]</color>  {item.texto}", 20, UIFactory.CorTexto);
        int falhas = AvaliacaoCliente.Falhas(lista);
        UIFactory.Texto(osConteudo, falhas == 0
            ? "Tudo certo. Peça para o cliente conferir e assinar."
            : $"<color=#F5B83F>{falhas} item(ns) faltando: cada um custa uma estrela.</color> Dá para voltar e arrumar antes.", 18, UIFactory.CorSuave);

        var linha = UIFactory.Vazio(osConteudo, "Botoes");
        UIFactory.Horizontal(linha, 12);
        UIFactory.Botao(linha, "Pedir a assinatura", PedirAssinatura, UIFactory.Sucesso, 300, 52, 22);
        UIFactory.Botao(linha, "Voltar e arrumar", FecharModal, UIFactory.CartaoClaro, 260, 52, 22);
        LayoutRebuilder.MarkLayoutForRebuild(janelaOS);
    }

    void PedirAssinatura()
    {
        var npc = npcOS;
        FecharModal();
        if (npc == null) return;
        npc.Assinar(() => MissionManager.Instance?.FecharOSAssinada());
    }

    // ---------- Cartão da avaliação (estrelas e comentário) ----------

    static Sprite spriteEstrela;

    // Estrela de 5 pontas desenhada por código (a fonte não tem o caractere da estrela)
    static Sprite SpriteEstrela()
    {
        if (spriteEstrela != null) return spriteEstrela;
        const int n = 64;
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var pts = new Vector2[10];
        for (int i = 0; i < 10; i++)
        {
            float ang = Mathf.PI / 2f + i * Mathf.PI / 5f;
            float r = (i % 2 == 0 ? 0.48f : 0.2f) * n;
            pts[i] = new Vector2(n / 2f + Mathf.Cos(ang) * r, n / 2f + Mathf.Sin(ang) * r - 2f);
        }
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                // 4 amostras por pixel: borda suave
                float cob = 0f;
                for (int s = 0; s < 4; s++)
                    if (DentroPoligono(new Vector2(x + 0.25f + 0.5f * (s % 2), y + 0.25f + 0.5f * (s / 2)), pts)) cob += 0.25f;
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, cob));
            }
        tex.Apply();
        spriteEstrela = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f));
        return spriteEstrela;
    }

    static bool DentroPoligono(Vector2 p, Vector2[] v)
    {
        bool dentro = false;
        for (int i = 0, j = v.Length - 1; i < v.Length; j = i++)
            if ((v[i].y > p.y) != (v[j].y > p.y) && p.x < (v[j].x - v[i].x) * (p.y - v[i].y) / (v[j].y - v[i].y) + v[i].x)
                dentro = !dentro;
        return dentro;
    }

    // Linha de 5 estrelas (cheias até "estrelas")
    public static RectTransform LinhaEstrelas(Transform pai, int estrelas, float tamanho = 34)
    {
        var linha = UIFactory.Vazio(pai, "Estrelas");
        UIFactory.Horizontal(linha, 4);
        for (int i = 0; i < 5; i++)
        {
            var go = new GameObject("Estrela", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            go.transform.SetParent(linha, false);
            var img = go.GetComponent<Image>();
            img.sprite = SpriteEstrela();
            img.color = i < estrelas ? UIFactory.Hex("#FFD54A") : UIFactory.Hex("#3A4352");
            var le = go.GetComponent<LayoutElement>();
            le.preferredWidth = le.preferredHeight = tamanho;
        }
        return linha;
    }

    RectTransform cartaoAvaliacao;

    void MostrarAvaliacao(ChamadoConcluidoEvent e)
    {
        if (cartaoAvaliacao != null) Destroy(cartaoAvaliacao.gameObject);
        var raiz = (RectTransform)areaToasts.parent;
        var c = UIFactory.Painel(raiz, "Avaliacao", UIFactory.Fundo);
        // Canto de baixo à direita: em cima ficam os objetivos e o contador de FPS
        UIFactory.Ancorar(c, new Vector2(1f, 0f), new Vector2(-24, 24), new Vector2(460, 0));
        c.pivot = new Vector2(1f, 0f);
        UIFactory.Vertical(c, 8, 20);
        UIFactory.AjustarAoConteudo(c);
        c.SetSiblingIndex(areaToasts.GetSiblingIndex());   // abaixo das notificações e janelas

        var os = e.chamado != null ? e.chamado.os : null;
        UIFactory.Texto(c, $"OS {(os != null ? os.numero.ToString("0000") : "")} assinada  ·  {e.chamado?.nomeCliente}", 18, UIFactory.CorSuave, FontStyles.Bold);
        LinhaEstrelas(c, e.estrelas);
        UIFactory.Texto(c, $"\"{e.comentario}\"  <color=#9AA6B8>— {e.chamado?.contato}</color>", 20, UIFactory.CorTexto, FontStyles.Italic);
        var mm = MissionManager.Instance;
        if (mm != null && mm.Avaliacoes > 0)
            UIFactory.Texto(c, $"Média dos clientes: {mm.MediaEstrelas:0.0} de 5 ({mm.Avaliacoes} avaliação(ões))", 16, UIFactory.CorSuave);

        cartaoAvaliacao = c;
        var grupo = c.gameObject.AddComponent<CanvasGroup>();
        grupo.alpha = 0f;
        Sequence.Create()
            .Chain(Tween.Custom(0f, 1f, 0.3f, v => { if (grupo != null) grupo.alpha = v; }))
            .ChainDelay(7f)
            .Chain(Tween.Custom(1f, 0f, 0.6f, v => { if (grupo != null) grupo.alpha = v; }))
            .ChainCallback(() => { if (c != null) Destroy(c.gameObject); });
        if (e.estrelas >= 4) SomManager.Tocar("conquista", 0.5f);
    }

    // Resumo das avaliações na aba Chamados do tablet
    void MontarResumoAtendimento()
    {
        var mm = MissionManager.Instance;
        if (mm == null || (mm.Avaliacoes == 0 && mm.Retornos.Count == 0)) return;
        Secao("ATENDIMENTO");
        var c = Cartao();
        var topo = UIFactory.Vazio(c, "Media");
        UIFactory.Horizontal(topo, 12);
        if (mm.Avaliacoes > 0)
        {
            LinhaEstrelas(topo, Mathf.RoundToInt(mm.MediaEstrelas), 24);
            UIFactory.Texto(topo, $"{mm.MediaEstrelas:0.0} de 5  ·  {mm.Avaliacoes} OS avaliada(s)", 19, UIFactory.CorTexto);
        }
        int pendentes = mm.Retornos.FindAll(r => !r.avisado).Count;
        int abertos = mm.Retornos.Count - pendentes;
        if (abertos > 0)
            UIFactory.Texto(c, $"<color=#F2545B>{abertos} retorno(s) em garantia esperando você.</color>", 18, UIFactory.CorTexto);
        for (int i = 0; i < Mathf.Min(3, mm.HistoricoOS.Count); i++)
        {
            var os = mm.HistoricoOS[i];
            UIFactory.Texto(c, $"OS {os.numero:0000} · {os.cliente} · <color=#FFD54A>{os.estrelas}/5</color> · " +
                               $"{OrdemServico.HorasTexto(os.minutosTrabalho)}{(os.noPrazo ? "" : " <color=#F2545B>(fora do SLA)</color>")}" +
                               $"\n<size=85%><i>\"{os.comentario}\"</i></size>", 17, UIFactory.CorSuave);
        }
    }
}
