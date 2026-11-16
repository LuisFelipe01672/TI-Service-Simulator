using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Parte da GameUI: peças 3D dos equipamentos. Mire numa peça (tampa, cartucho, cabo atrás...),
// escolha a ação com a roda do mouse e aperte E (ou segure E nas ações demoradas).
// As ações são as mesmas da janela de manutenção (AcoesFisicas), filtradas pela peça.
public partial class GameUI
{
    RectTransform miraRt, painelPeca, barraPecaFundo, barraPeca;
    Image miraImg;
    TextMeshProUGUI txtPecaTitulo, txtPecaAcoes, txtPecaResultado;
    RectTransform painelClose;
    TextMeshProUGUI txtClose;

    PecaEquipamento pecaMirada;
    readonly List<AcaoFisica> acoesPeca = new List<AcaoFisica>();
    int indicePeca;
    AcaoFisica acaoSegurandoPeca;
    float progressoPeca;
    string resultadoPeca;
    float resultadoAte;

    void ConstruirPainelPeca(RectTransform raiz, RectTransform mira)
    {
        miraRt = mira;
        miraImg = mira.GetComponent<Image>();

        // À direita da mira, sem cobrir o que está sendo mirado
        painelPeca = UIFactory.Painel(raiz, "PainelPeca", UIFactory.Fundo);
        painelPeca.anchorMin = painelPeca.anchorMax = new Vector2(0.5f, 0.5f);
        painelPeca.pivot = new Vector2(0f, 0.5f);
        painelPeca.anchoredPosition = new Vector2(60f, -20f);
        painelPeca.sizeDelta = new Vector2(520f, 0f);
        UIFactory.Vertical(painelPeca, 6, 18);
        UIFactory.AjustarAoConteudo(painelPeca);

        txtPecaTitulo = UIFactory.Texto(painelPeca, "", 22, UIFactory.Destaque, FontStyles.Bold);
        txtPecaAcoes = UIFactory.Texto(painelPeca, "", 20, UIFactory.CorTexto);
        barraPecaFundo = UIFactory.Painel(painelPeca, "BarraFundo", UIFactory.CartaoClaro);
        barraPecaFundo.gameObject.AddComponent<LayoutElement>().preferredHeight = 10;
        barraPeca = UIFactory.Painel(barraPecaFundo, "Barra", UIFactory.Alerta);
        barraPeca.anchorMin = Vector2.zero;
        barraPeca.anchorMax = new Vector2(0f, 1f);
        barraPeca.offsetMin = barraPeca.offsetMax = Vector2.zero;
        txtPecaResultado = UIFactory.Texto(painelPeca, "", 19, UIFactory.CorSuave);
        painelPeca.gameObject.SetActive(false);

        // Legenda do close de câmera (embaixo, no meio): a pista está na peça, o texto é curto
        painelClose = UIFactory.Painel(raiz, "LegendaClose", UIFactory.Fundo);
        UIFactory.Ancorar(painelClose, new Vector2(0.5f, 0f), new Vector2(0, 70), new Vector2(900, 0));
        painelClose.pivot = new Vector2(0.5f, 0f);
        UIFactory.Vertical(painelClose, 4, 14);
        UIFactory.AjustarAoConteudo(painelClose);
        txtClose = UIFactory.Texto(painelClose, "", 21, UIFactory.CorTexto, FontStyles.Normal, TextAlignmentOptions.Center);
        painelClose.gameObject.SetActive(false);
    }

    // Chamado pelo Update. Devolve true se o jogador está mirando uma peça (aí o E é dela).
    bool AtualizarPecas()
    {
        var p = modal == Modal.Nenhum ? PecaEquipamento.Mirada() : null;
        if (p != pecaMirada)
        {
            pecaMirada = p;
            acaoSegurandoPeca = null;
            indicePeca = 0;
        }

        bool mirando = p != null;
        if (painelPeca.gameObject.activeSelf != mirando) painelPeca.gameObject.SetActive(mirando);
        miraImg.color = mirando ? UIFactory.Destaque : new Color(1, 1, 1, 0.8f);
        miraRt.sizeDelta = mirando ? new Vector2(10, 10) : new Vector2(6, 6);
        if (!mirando) return false;

        var mm = MissionManager.Instance;
        var estado = mm != null ? mm.Estado : null;
        txtPecaTitulo.text = $"{p.nome}  <size=75%><color=#9AA6B8>{p.Titulo}</color></size>";

        acoesPeca.Clear();
        if (p.acoesProprias != null) acoesPeca.AddRange(p.acoesProprias());   // usado na bancada (Etapa 6)
        else if (estado != null && p.equipamento != null)
            foreach (var a in AcoesFisicas.Para(p.equipamento.tipoEquipamento, estado))
                if (a.peca == p.id) acoesPeca.Add(a);

        // Roda do mouse escolhe a ação
        if (acoesPeca.Count > 0)
        {
            float roda = Input.mouseScrollDelta.y;
            if (acaoSegurandoPeca == null && roda != 0f)
                indicePeca += roda < 0f ? 1 : -1;
            indicePeca = (indicePeca % acoesPeca.Count + acoesPeca.Count) % acoesPeca.Count;
        }

        MontarTextoPeca(estado == null && p.acoesProprias == null);
        TratarTeclaPeca();
        return true;
    }

    void MontarTextoPeca(bool semChamado)
    {
        var sb = new System.Text.StringBuilder();
        var mochila = Mochila.Instance;
        if (semChamado)
            sb.Append("<color=#9AA6B8>Nenhum chamado em atendimento. Aceite um chamado no tablet (Tab).</color>");
        else if (acoesPeca.Count == 0)
            sb.Append("<color=#9AA6B8>Nada para fazer aqui agora.</color>");
        else
        {
            for (int i = 0; i < acoesPeca.Count; i++)
            {
                var a = acoesPeca[i];
                bool sel = i == indicePeca;
                bool temItem = a.requer == null || (mochila != null && mochila.Tem(a.requer.Value));
                string extra = !temItem ? $"  <size=80%><color=#F2545B>precisa: {Mochila.Info(a.requer.Value).nome}</color></size>"
                             : a.segurar > 0f ? $"  <size=80%><color=#9AA6B8>(segure {a.segurar:0.#}s)</color></size>" : "";
                sb.Append(sel ? $"<color=#3FA9F5>»</color> <b>{a.rotulo}</b>{extra}" : $"<color=#9AA6B8>   {a.rotulo}</color>{extra}");
                if (i < acoesPeca.Count - 1) sb.Append('\n');
            }
            var atual = acoesPeca[indicePeca];
            sb.Append("\n<size=80%><color=#9AA6B8>");
            sb.Append(atual.segurar > 0f ? "[segure E]" : "[E]");
            if (acoesPeca.Count > 1) sb.Append("  ·  roda do mouse: escolher");
            sb.Append("</color></size>");
        }
        txtPecaAcoes.text = sb.ToString();

        bool segurando = acaoSegurandoPeca != null;
        barraPecaFundo.gameObject.SetActive(segurando);
        if (segurando) barraPeca.anchorMax = new Vector2(Mathf.Clamp01(progressoPeca / acaoSegurandoPeca.segurar), 1f);

        bool mostrarResultado = !string.IsNullOrEmpty(resultadoPeca) && Time.time < resultadoAte;
        if (txtPecaResultado.gameObject.activeSelf != mostrarResultado) txtPecaResultado.gameObject.SetActive(mostrarResultado);
        if (mostrarResultado) txtPecaResultado.text = resultadoPeca;
    }

    void TratarTeclaPeca()
    {
        if (acaoSegurandoPeca != null)
        {
            if (!Input.GetKey(KeyCode.E)) { acaoSegurandoPeca = null; return; }   // soltou antes: cancela
            progressoPeca += Time.deltaTime * EnergiaTecnico.Ritmo;   // cansado demora mais
            if (progressoPeca >= acaoSegurandoPeca.segurar)
            {
                var a = acaoSegurandoPeca;
                acaoSegurandoPeca = null;
                ConcluirAcaoPeca(a);
            }
            return;
        }

        if (!Input.GetKeyDown(KeyCode.E) || acoesPeca.Count == 0) return;
        var acao = acoesPeca[indicePeca];
        var mochila = Mochila.Instance;
        if (acao.requer != null && (mochila == null || !mochila.Tem(acao.requer.Value)))
        {
            MostrarResultadoPeca($"<color=#F2545B>Você precisa de: {Mochila.Info(acao.requer.Value).nome}.</color> Compre na Loja do tablet.");
            return;
        }

        if (pecaMirada.equipamento != null)
        {
            if (equipamentoAtual != pecaMirada.equipamento) logManutencao.Clear();
            equipamentoAtual = pecaMirada.equipamento;
        }
        if (acao.abreComputador) { AbrirComputador(); return; }
        if (acao.abreCrimpagem) { CrimparNoCliente(true); return; }
        if (acao.segurar > 0f) { acaoSegurandoPeca = acao; progressoPeca = 0f; return; }
        ConcluirAcaoPeca(acao);
    }

    void ConcluirAcaoPeca(AcaoFisica a)
    {
        if (pecaMirada != null) SomManager.SomDaAcao(a.rotulo, pecaMirada.transform.position);
        string r = a.executar();
        bool doChamado = pecaMirada == null || pecaMirada.equipamento != null;
        if (!a.observacao && doChamado) MissionManager.Instance?.RegistrarAcao(a.rotulo);
        if (doChamado) Registrar(r);
        bool fazClose = a.observacao && a.close && !a.abreComputador && pecaMirada != null && !string.IsNullOrEmpty(r);
        if (fazClose) CloseDePeca.Abrir(pecaMirada, a.pistaVisual ?? r);
        if (!fazClose || a.pistaVisual == null) MostrarResultadoPeca(r);   // pista visual: o texto fica só no registro (Tab)
        // A ação pode ter fechado o chamado (página de teste ok, por exemplo): a notificação já avisa
        if (doChamado && (MissionManager.Instance == null || MissionManager.Instance.chamadoAtivo == null)) resultadoPeca = null;
    }

    void MostrarResultadoPeca(string texto)
    {
        if (string.IsNullOrEmpty(texto)) return;
        resultadoPeca = texto;
        resultadoAte = Time.time + 6f;
    }
}

public partial class GameUI
{
    // Durante o close de câmera: só a legenda; o painel da peça e o aviso de interação somem
    void AtualizarLegendaClose(bool ativo)
    {
        if (painelClose == null) return;
        if (painelClose.gameObject.activeSelf != ativo) painelClose.gameObject.SetActive(ativo);
        if (!ativo) return;
        if (painelPeca.gameObject.activeSelf) painelPeca.gameObject.SetActive(false);
        txtClose.text = $"{CloseDePeca.Legenda}\n<size=75%><color=#9AA6B8>[E] ou mexa o mouse para voltar</color></size>";
    }
}
