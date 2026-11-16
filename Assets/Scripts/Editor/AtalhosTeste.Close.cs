using System.Reflection;
using UnityEditor;
using UnityEngine;

// Atalho de teste do close de câmera (Etapa 2.4): executa a primeira ação de "olhar" da peça mirada,
// pelo mesmo caminho do E (GameUI.ConcluirAcaoPeca, por reflexão: não abre a API do jogo só para teste).
static class AtalhosTesteClose
{
    const BindingFlags Privado = BindingFlags.NonPublic | BindingFlags.Instance;

    [MenuItem("TI Service/Teste (Play)/Close: olhar a peça mirada (E)", false, 141)]
    static void OlharPecaMirada()
    {
        if (!Application.isPlaying || GameUI.Instance == null) return;
        var p = PecaEquipamento.Mirada();
        var estado = MissionManager.Instance != null ? MissionManager.Instance.Estado : null;
        if (p == null || estado == null) { Debug.LogWarning("[Teste] Mire uma peça com um chamado ativo."); return; }
        foreach (var a in AcoesFisicas.Para(p.equipamento.tipoEquipamento, estado))
        {
            if (a.peca != p.id || !a.observacao || !a.close || a.abreComputador) continue;
            typeof(GameUI).GetField("pecaMirada", Privado).SetValue(GameUI.Instance, p);
            typeof(GameUI).GetMethod("ConcluirAcaoPeca", Privado).Invoke(GameUI.Instance, new object[] { a });
            Debug.Log($"[Teste] Close: '{a.rotulo}' em {p.nome} · ativo={CloseDePeca.Ativo} · legenda: {CloseDePeca.Legenda}");
            return;
        }
        Debug.LogWarning($"[Teste] {p.nome} não tem ação de olhar agora.");
    }

    [MenuItem("TI Service/Teste (Play)/Close: voltar", false, 142)]
    static void Voltar() { if (Application.isPlaying) CloseDePeca.Fechar(); }
}

static class AtalhosTesteClosePc
{
    [MenuItem("TI Service/Teste (Play)/Close: chamado de teste do PC com poeira", false, 143)]
    static void ChamadoPoeira()
    {
        var m = typeof(AtalhosTesteClose).Assembly.GetType("AtalhosTeste")
            ?.GetMethod("ChamadoTeste", BindingFlags.NonPublic | BindingFlags.Static);
        m?.Invoke(null, new object[] { "Limpar por dentro" });
    }
}
