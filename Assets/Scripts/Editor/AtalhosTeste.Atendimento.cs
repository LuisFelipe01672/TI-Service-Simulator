using UnityEditor;
using UnityEngine;

// Atalhos de teste da Etapa 1 (fechar o atendimento): OS, assinatura, estrelas, SLA e retrabalho.
// Menu "TI Service/Teste (Play)/Atendimento". Só funcionam com o jogo rodando.
public static partial class AtalhosTeste
{
    const string MenuAt = Menu + "Atendimento/";

    static ClienteNPC ClienteAtual() => Object.FindAnyObjectByType<ClienteNPC>();

    // Conserta o sintoma do chamado ativo (equipamento novo), mas mantém as causas raiz: serve para testar o retrabalho
    [MenuItem(MenuAt + "1. Resolver só o sintoma e conferir", false, 200)]
    static void ResolverSintoma()
    {
        var mm = MissionManager.Instance;
        if (!Application.isPlaying || mm?.chamadoAtivo == null) { Debug.LogWarning("[Teste] Aceite um chamado antes."); return; }
        var velho = mm.Estado;
        var novo = new EstadoEquipamentos { impPapelUmido = velho.impPapelUmido, impCaboSolto = velho.impCaboSolto };
        mm.chamadoAtivo.estado = novo;
        mm.RegistrarAcao("(teste) Consertou o sintoma");
        Debug.Log($"[Teste] Sintoma resolvido. Verificar → {mm.Verificar()} | aguardando assinatura: {mm.chamadoAtivo?.aguardandoAssinatura} " +
                  $"| causa pendente: {CatalogoProblemas.CausaRaizPendente(mm.chamadoAtivo?.problema, mm.Estado) ?? "nenhuma"}");
    }

    [MenuItem(MenuAt + "2. Resolver tudo (sintoma e causa) e conferir", false, 201)]
    static void ResolverTudo()
    {
        var mm = MissionManager.Instance;
        if (!Application.isPlaying || mm?.chamadoAtivo == null) { Debug.LogWarning("[Teste] Aceite um chamado antes."); return; }
        mm.chamadoAtivo.estado = new EstadoEquipamentos();
        mm.RegistrarAcao("(teste) Consertou tudo");
        Debug.Log($"[Teste] Tudo resolvido. Verificar → {mm.Verificar()}");
    }

    [MenuItem(MenuAt + "3. Deixar o gabinete aberto e sujeira no chão", false, 202)]
    static void Bagunca()
    {
        var mm = MissionManager.Instance;
        if (mm?.Estado == null) { Debug.LogWarning("[Teste] Aceite um chamado antes."); return; }
        mm.Estado.pcGabineteAberto = true;
        Sujeira.Espalhar(mm.PosicaoEquipamentoAtual, 2, "poeira");
        LogChecklist();
    }

    [MenuItem(MenuAt + "4. Abrir a OS (janela do cliente)", false, 203)]
    static void AbrirOS()
    {
        var npc = ClienteAtual();
        if (npc == null || GameUI.Instance == null) { Debug.LogWarning("[Teste] Sem cliente na cena."); return; }
        if (!npc.chamado.aguardandoAssinatura) { Debug.LogWarning("[Teste] O defeito ainda não foi resolvido (use o atalho 1 ou 2)."); return; }
        GameUI.Instance.AbrirOS(npc);
        LogChecklist();
    }

    [MenuItem(MenuAt + "5. Pedir a assinatura (animação da prancheta)", false, 204)]
    static void PedirAssinatura()
    {
        var npc = ClienteAtual();
        if (npc == null || !npc.chamado.aguardandoAssinatura) { Debug.LogWarning("[Teste] Nada para assinar."); return; }
        GameUI.Instance?.FecharModal();
        // Olha para o cliente para ver a prancheta
        Ir(npc.transform.position + npc.transform.forward * 1.3f + Vector3.up * 1.1f, npc.transform.position + Vector3.up * 1.05f);
        npc.Assinar(() => MissionManager.Instance?.FecharOSAssinada());
        Debug.Log("[Teste] Cliente assinando a OS...");
    }

    [MenuItem(MenuAt + "6. Ver a última OS no log", false, 205)]
    static void LogUltimaOS()
    {
        var mm = MissionManager.Instance;
        if (mm == null) return;
        var os = mm.chamadoAtivo?.os ?? (mm.HistoricoOS.Count > 0 ? mm.HistoricoOS[0] : null);
        if (os == null) { Debug.Log("[Teste] Nenhuma OS ainda."); return; }
        Debug.Log($"[Teste] OS {os.numero:0000} | {os.cliente} ({os.contato}) | aberta {os.abertura} | {os.prioridade} | relato \"{os.relato}\" | " +
                  $"diagnóstico: {os.diagnostico} | ações: {string.Join(" / ", os.acoes)} | peças: {string.Join(" / ", os.pecas)} (R$ {os.valorPecas:F0}) | " +
                  $"tempo {OrdemServico.HorasTexto(os.minutosTrabalho)} | no prazo: {os.noPrazo} | assinada: {os.assinada} | {os.estrelas} estrela(s): \"{os.comentario}\" | " +
                  $"causa pendente: {(string.IsNullOrEmpty(os.causaPendente) ? "nenhuma" : os.causaPendente)} | média {mm.MediaEstrelas:0.0} ({mm.Avaliacoes})");
    }

    [MenuItem(MenuAt + "7. Adiantar o relógio 1 hora (SLA)", false, 206)]
    static void AdiantarHora()
    {
        var d = DiaManager.Instance;
        if (d == null) return;
        d.minutoAtual += 60f;
        var mm = MissionManager.Instance;
        Debug.Log($"[Teste] Agora {d.HoraTexto()}." + (mm?.chamadoAtivo != null
            ? $" SLA {SLA.Texto(mm.chamadoAtivo.prioridade)} até {SLA.HoraDoPrazo(mm.chamadoAtivo, SLA.Agora())}, resta {mm.TempoRestante:F0} min." : ""));
    }

    [MenuItem(MenuAt + "8. Trazer os retornos para hoje", false, 207)]
    static void RetornosHoje()
    {
        var mm = MissionManager.Instance;
        var d = DiaManager.Instance;
        if (mm == null || d == null) return;
        foreach (var r in mm.Retornos) r.diaRetorno = Mathf.Min(r.diaRetorno, d.dia);
        mm.GerarChamados();
        Debug.Log($"[Teste] {mm.Retornos.Count} retorno(s): " + string.Join(" / ", mm.Retornos.ConvertAll(r => $"{r.cliente} dia {r.diaRetorno} ({r.motivo})")) +
                  $" | chamados em garantia abertos: {mm.chamadosDisponiveis.FindAll(c => c.retrabalho).Count}");
    }

    [MenuItem(MenuAt + "9. Aceitar o retorno em garantia", false, 208)]
    static void AceitarRetorno()
    {
        var mm = MissionManager.Instance;
        if (mm == null) return;
        var ch = mm.chamadosDisponiveis.Find(c => c.retrabalho);
        if (ch == null) { Debug.LogWarning("[Teste] Nenhum retorno aberto (use o atalho 8)."); return; }
        if (mm.chamadoAtivo != null) mm.AbandonarChamado();
        ch.exigeNota = false;
        mm.AceitarChamado(ch);
        Debug.Log($"[Teste] Retorno aceito: {ch.nomeCliente} — {ch.descricaoProblema} | SLA {SLA.Texto(ch.prioridade)} | causa: {CatalogoProblemas.CausaRaizPendente(ch.problema, mm.Estado)}");
    }

    [MenuItem(MenuAt + "Impressora: abrir/fechar a gaveta de papel", false, 220)]
    static void Gaveta() { var e = MissionManager.Instance?.Estado; AcaoImpressora(e != null && e.impGavetaAberta ? "Fechar a gaveta" : "Abrir a gaveta"); }
    [MenuItem(MenuAt + "Impressora: trocar a resma úmida (causa raiz)", false, 221)]
    static void TrocarResma() => AcaoImpressora("Trocar a resma úmida");
    [MenuItem(MenuAt + "Impressora: olhar atrás (cabo)", false, 222)]
    static void OlharAtras() => AcaoImpressora("Olhar atrás da impressora");
    [MenuItem(MenuAt + "Impressora: prender o cabo na parede (causa raiz)", false, 223)]
    static void PrenderCabo() => AcaoImpressora("Passar o cabo rente à parede");

    [MenuItem(MenuAt + "Tablet: abrir (ver abas liberadas)", false, 230)]
    static void AbrirTablet() { if (Application.isPlaying) GameUI.Instance?.AbrirTablet(); }

    [MenuItem(MenuAt + "Primeiro dia: aceitar o chamado guiado", false, 231)]
    static void AceitarGuiado()
    {
        var mm = MissionManager.Instance;
        var ch = mm?.chamadosDisponiveis.Find(c => c.tutorial);
        if (ch == null) { Debug.LogWarning("[Teste] Sem chamado guiado (só aparece em jogo novo, antes do primeiro chamado)."); return; }
        if (mm.chamadoAtivo != null) mm.AbandonarChamado();
        mm.AceitarChamado(ch);
        Debug.Log("[Teste] Guia: " + TutorialPrimeiroDia.Passo(ch));
    }

    [MenuItem(MenuAt + "Primeiro dia: ver o passo do guia", false, 232)]
    static void PassoGuia() => Debug.Log("[Teste] Guia: " + TutorialPrimeiroDia.Passo(MissionManager.Instance?.chamadoAtivo));

    static void LogChecklist()
    {
        var mm = MissionManager.Instance;
        if (mm == null) return;
        foreach (var i in mm.ChecklistAtual()) Debug.Log($"[Teste] Checklist: {(i.ok ? "OK" : "FALTA")} — {i.texto}");
    }
}
