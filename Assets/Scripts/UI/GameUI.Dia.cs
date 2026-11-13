using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Parte da GameUI: relógio no HUD, aviso de fim de expediente e tela de resumo do dia.
public partial class GameUI
{
    TextMeshProUGUI txtRelogio;
    RectTransform resumo, resumoLinhas;
    TextMeshProUGUI txtResumoTitulo, txtResumoSub;
    Button botaoDormir;
    TextMeshProUGUI txtBotaoDormir;
    CanvasGroup telaPreta;

    EventBinding<ExpedienteEncerradoEvent> bExpediente;
    EventBinding<DiaEncerradoEvent> bDiaFim;
    EventBinding<DiaIniciadoEvent> bDiaInicio;

    void ConstruirRelogio(RectTransform hudPai)
    {
        txtRelogio = UIFactory.Texto(hudPai, "", 22, UIFactory.CorTexto, FontStyles.Bold);
    }

    void ConstruirResumo(RectTransform pai, RectTransform raiz)
    {
        resumo = JanelaModal(pai, "ResumoDia", new Vector2(640, 0));
        UIFactory.Vertical(resumo, 12, 36);
        UIFactory.AjustarAoConteudo(resumo);

        txtResumoTitulo = UIFactory.Texto(resumo, "", 38, UIFactory.CorTexto, FontStyles.Bold);
        txtResumoSub = UIFactory.Texto(resumo, "", 20, UIFactory.CorSuave);
        resumoLinhas = UIFactory.Vazio(resumo, "Linhas");
        UIFactory.Vertical(resumoLinhas, 6);
        botaoDormir = UIFactory.Botao(resumo, "Dormir", Dormir, UIFactory.Destaque, 560, 58);
        txtBotaoDormir = botaoDormir.GetComponentInChildren<TextMeshProUGUI>();

        // Tela preta para a transição entre os dias (fica por cima de tudo)
        var preta = UIFactory.Painel(raiz, "TelaPreta", Color.black);
        UIFactory.Esticar(preta);
        telaPreta = preta.gameObject.AddComponent<CanvasGroup>();
        telaPreta.alpha = 0f;
        telaPreta.blocksRaycasts = false;
    }

    void RegistrarEventosDia()
    {
        bExpediente = new EventBinding<ExpedienteEncerradoEvent>(_ =>
            Toast("<b>18h: fim do expediente!</b>\nNovos chamados só amanhã. Saia pela porta do escritório para encerrar o dia.",
                  UIFactory.Alerta, 5f));
        bDiaFim = new EventBinding<DiaEncerradoEvent>(e => MostrarResumo(e.resumo));
        bDiaInicio = new EventBinding<DiaIniciadoEvent>(e =>
        {
            var data = DiaManager.DataDoDia(e.dia);
            bool fds = data.DayOfWeek == System.DayOfWeek.Saturday || data.DayOfWeek == System.DayOfWeek.Sunday;
            Toast($"<b>{data:dd/MM/yyyy}</b>" + (fds ? " — fim de semana: poucos clientes ligam hoje." : " — bom trabalho!"), UIFactory.Destaque, 3f);
        });
        EventBus<ExpedienteEncerradoEvent>.Register(bExpediente);
        EventBus<DiaEncerradoEvent>.Register(bDiaFim);
        EventBus<DiaIniciadoEvent>.Register(bDiaInicio);
        bFalencia = new EventBinding<FalenciaEvent>(MostrarFalencia);
        EventBus<FalenciaEvent>.Register(bFalencia);
    }

    void RemoverEventosDia()
    {
        EventBus<ExpedienteEncerradoEvent>.Deregister(bExpediente);
        EventBus<DiaEncerradoEvent>.Deregister(bDiaFim);
        EventBus<DiaIniciadoEvent>.Deregister(bDiaInicio);
        EventBus<FalenciaEvent>.Deregister(bFalencia);
    }

    void AtualizarRelogio()
    {
        var d = DiaManager.Instance;
        if (txtRelogio == null) return;
        if (d == null) { txtRelogio.text = ""; return; }

        string cor = d.ForaDoExpediente ? "#F5B83F" : "#E8ECF2";
        txtRelogio.text = $"{d.DiaSemana}, {d.Data:dd/MM}   <color={cor}>{d.HoraTexto()}</color>" +
                          (d.ForaDoExpediente ? "  <size=75%><color=#F5B83F>fora do expediente</color></size>" : "");
    }

    void MostrarResumo(ResumoDia r)
    {
        AbrirModal(Modal.Resumo, resumo);

        var data = DiaManager.DataDoDia(r.dia);
        txtResumoTitulo.text = $"Fim do dia  <size=60%><color=#9AA6B8>{data:dd/MM/yyyy}</color></size>";
        txtResumoSub.text = r.dormiuNoEscritorio
            ? $"<color=#F2545B>Você ficou até {r.horaSaida} e acabou dormindo no escritório (-2 reputação).</color>"
            : $"Você foi embora às {r.horaSaida}.";

        UIFactory.Limpar(resumoLinhas);
        Linha("Chamados resolvidos por você", r.chamados.ToString(), UIFactory.CorTexto);
        if (r.chamadosEquipe > 0) Linha("Chamados resolvidos pela equipe", r.chamadosEquipe.ToString(), UIFactory.CorTexto);
        if (r.erros > 0) Linha("Chamados com erro", r.erros.ToString(), UIFactory.Perigo);
        Linha("Dinheiro recebido", "+" + UIFactory.Dinheiro(r.ganhos), UIFactory.Sucesso);
        if (r.compras > 0f) Linha("Compras (ferramentas e peças)", "-" + UIFactory.Dinheiro(r.compras), UIFactory.Perigo);
        if (r.salarios > 0f) Linha("Salários da equipe", "-" + UIFactory.Dinheiro(r.salarios), UIFactory.Perigo);
        Linha("Reputação no dia", $"{(r.deltaReputacao >= 0 ? "+" : "")}{r.deltaReputacao:F0}",
              r.deltaReputacao >= 0 ? UIFactory.Sucesso : UIFactory.Perigo);
        Linha("Saldo", UIFactory.Dinheiro(r.saldoFinal), r.saldoFinal >= 0 ? UIFactory.CorTexto : UIFactory.Perigo);

        txtBotaoDormir.text = $"Dormir (amanhã é {DiaManager.DataDoDia(r.dia + 1):dd/MM})";
        botaoDormir.interactable = true;
    }

    void Linha(string rotulo, string valor, Color corValor)
    {
        var l = UIFactory.Painel(resumoLinhas, "Linha", UIFactory.Cartao);
        UIFactory.Horizontal(l, 10, 14);
        var t = UIFactory.Texto(l, rotulo, 21, UIFactory.CorSuave);
        t.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
        var v = UIFactory.Texto(l, valor, 22, corValor, FontStyles.Bold, TextAlignmentOptions.Right);
        v.gameObject.AddComponent<LayoutElement>().preferredWidth = 200;
    }

    // Escurece a tela, começa o dia seguinte e clareia de novo
    void Dormir()
    {
        botaoDormir.interactable = false;
        telaPreta.blocksRaycasts = true;
        Tween.Custom(startValue: 0f, endValue: 1f, duration: 0.7f,
                onValueChange: v => { if (telaPreta != null) telaPreta.alpha = v; }, useUnscaledTime: true)
            .OnComplete(() =>
            {
                FecharModal();
                DiaManager.Instance?.ComecarNovoDia();
                Tween.Custom(startValue: 1f, endValue: 0f, duration: 1.2f, startDelay: 0.4f,
                        onValueChange: v => { if (telaPreta != null) telaPreta.alpha = v; }, useUnscaledTime: true)
                    .OnComplete(() => { if (telaPreta != null) telaPreta.blocksRaycasts = false; });
            });
    }
}
