using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Atalhos de teste da Etapa 4 (ferramentas de diagnóstico, saúde das peças e check-up com laudo).
// Menu "TI Service/Teste (Play)/Diagnóstico". Só funcionam com o jogo rodando.
public static partial class AtalhosTeste
{
    const string MenuDiag = Menu + "Diagnóstico/";

    [MenuItem(MenuDiag + "0. Ganhar as quatro ferramentas e baterias", false, 500)]
    static void GanharFerramentas()
    {
        var m = Mochila.Instance;
        if (!Application.isPlaying || m == null) { Debug.LogWarning("[Teste] Aperte Play antes."); return; }
        foreach (var it in new[] { Item.PlacaPOST, Item.TestadorFonte, Item.Multimetro, Item.PendriveDiagnostico, Item.PendriveDrivers })
            if (!m.Tem(it)) m.Adicionar(it, 1);
        if (m.Quantidade(Item.BateriaCMOS) == 0) m.Adicionar(Item.BateriaCMOS, 5);
        if (m.Quantidade(Item.Fonte) == 0) m.Adicionar(Item.Fonte, 1);
        if (m.Quantidade(Item.Memoria) == 0) m.Adicionar(Item.Memoria, 1);
        m.comVoce = true;
        Debug.Log($"[Teste] Ferramentas na mochila. Peso {m.PesoAtual:0.00}/{Mochila.CapacidadeKg} kg. Check-up liberado: {CatalogoProblemas.PodeFazerCheckup(m)}");
    }

    [MenuItem(MenuDiag + "1. Chamado: liga sem vídeo e sem apito (placa POST)", false, 501)]
    static void ChamadoPost() => ChamadoDiag(CatalogoProblemas.EncaixarMemoria);
    [MenuItem(MenuDiag + "2. Chamado: reinicia sozinho (fonte fraca)", false, 502)]
    static void ChamadoFonteFraca() => ChamadoDiag(CatalogoProblemas.TrocarFonteFraca);
    [MenuItem(MenuDiag + "3. Chamado: tela azul (memória com erro)", false, 503)]
    static void ChamadoMemoria() => ChamadoDiag(CatalogoProblemas.TrocarMemoriaComErro);
    [MenuItem(MenuDiag + "4. Chamado: perde a data (bateria da placa-mãe)", false, 504)]
    static void ChamadoBateria() => ChamadoDiag(CatalogoProblemas.TrocarBateria);
    [MenuItem(MenuDiag + "5. Chamado: check-up completo (laudo)", false, 505)]
    static void ChamadoCheckup() => ChamadoDiag(CatalogoProblemas.Checkup);

    static void ChamadoDiag(string solucao)
    {
        GanharFerramentas();
        ChamadoTeste(solucao);
        var e = MissionManager.Instance?.Estado;
        if (e != null)
            Debug.Log($"[Teste] Saúde: memória {e.SaudeMemoriaPct}% · fonte {e.SaudeFontePct}% · disco {e.SaudeDiscoPct}% · cooler {e.SaudeCoolerPct}% · " +
                      $"bateria {e.SaudeBateriaPct}% · idade {e.idadePcAnos} anos | 12 V {e.Tensao12V:0.00} · bateria {e.TensaoBateria:0.00} V · POST {e.CodigoPost()}");
    }

    // Prepara o computador para a ferramenta (desligado e aberto, ou ligado e fechado) e executa a ação
    static string AcaoPc(string inicio, bool aberto)
    {
        var mm = MissionManager.Instance;
        var e = mm?.Estado;
        if (e == null) { Debug.LogWarning("[Teste] Aceite um chamado de computador antes."); return null; }
        if (aberto) { e.pcLigado = false; e.pcGabineteAberto = true; e.pcCoolerFora = false; }
        else
        {
            e.pcGabineteAberto = false;
            if (!e.pcLigado && e.PcDaVideo && !e.pcSemSistema && !inicio.StartsWith("Ligar o computador")) e.pcLigado = true;
        }
        foreach (var a in AcoesFisicas.Para(TipoEquipamento.Computador, e))
            if (a.rotulo.StartsWith(inicio))
            {
                string r = a.executar();
                if (!a.observacao) mm.RegistrarAcao(a.rotulo);
                Debug.Log($"[Teste] {a.rotulo} → {r}");
                return r;
            }
        Debug.LogWarning("[Teste] Ação não disponível agora: " + inicio);
        return null;
    }

    [MenuItem(MenuDiag + "6. Usar a placa POST", false, 510)]
    static void UsarPost() { IrAoEquipamento(TipoEquipamento.Computador); AcaoPc("Espetar a placa POST", true); }
    [MenuItem(MenuDiag + "7. Usar o testador de fonte", false, 511)]
    static void UsarTestadorFonte() { IrAoEquipamento(TipoEquipamento.Computador); AcaoPc("Ligar o testador de fonte", true); }
    [MenuItem(MenuDiag + "8. Multímetro na bateria da placa-mãe", false, 512)]
    static void UsarMultimetroBateria() { IrAoEquipamento(TipoEquipamento.Computador); AcaoPc("Medir a bateria da placa-mãe", true); }
    [MenuItem(MenuDiag + "8b. Multímetro: continuidade do fusível", false, 513)]
    static void UsarMultimetroFusivel() { IrAoEquipamento(TipoEquipamento.Computador); AcaoPc("Testar a continuidade do fusível", true); }
    [MenuItem(MenuDiag + "9. Pendrive: teste de memória", false, 514)]
    static void UsarPendriveMemoria() { IrAoMonitor(); AcaoPc("Dar boot pelo pendrive: teste de memória", false); }
    [MenuItem(MenuDiag + "9b. Pendrive: disco e sensores", false, 515)]
    static void UsarPendriveDisco() { IrAoMonitor(); AcaoPc("Dar boot pelo pendrive: disco e sensores", false); }

    // Faz todas as medições do check-up, fecha o gabinete, liga e abre a OS com o laudo
    [MenuItem(MenuDiag + "10. Fazer o laudo inteiro e abrir a OS", false, 520)]
    static void LaudoInteiro()
    {
        var e = MissionManager.Instance?.Estado;
        if (e == null) { Debug.LogWarning("[Teste] Aceite o chamado de check-up antes (atalho 5)."); return; }
        e.clienteSalvouTrabalho = true;
        AcaoPc("Dar boot pelo pendrive: teste de memória", false);
        AcaoPc("Dar boot pelo pendrive: disco e sensores", false);
        AcaoPc("Olhar por dentro", true);
        AcaoPc("Ligar o testador de fonte", true);
        AcaoPc("Medir a bateria da placa-mãe", true);
        AcaoPc("Colocar a tampa", true);
        AcaoPc("Ligar o computador", false);
        var mm = MissionManager.Instance;
        Debug.Log($"[Teste] Laudo completo: {e.LaudoCompleto} (falta: {e.FaltaNoLaudo()}) | aguardando assinatura: {mm.chamadoAtivo?.aguardandoAssinatura}\n" +
                  string.Join("\n", e.medicoes) + "\nRecomendações: " + string.Join(" / ", e.Recomendacoes()));
        var npc = ClienteAtual();
        if (npc != null && mm.chamadoAtivo != null && mm.chamadoAtivo.aguardandoAssinatura) GameUI.Instance?.AbrirOS(npc);
    }

    static void IrAoMonitor()
    {
        foreach (var eq in Object.FindObjectsByType<Equipamento>())
        {
            if (eq.tipoEquipamento != TipoEquipamento.Computador) continue;
            var tela = eq.GetComponentInChildren<TelaComputador>(true);
            if (tela == null) { IrAoEquipamento(TipoEquipamento.Computador); return; }
            Vector3 c = tela.transform.position;
            Vector3 fora = tela.transform.forward; fora.y = 0f; fora.Normalize();
            Ir(new Vector3(c.x, 1.15f, c.z) + fora * 0.75f, c);
            return;
        }
    }

    // Uma foto de cada ferramenta em uso e da tela do pendrive, em Builds/Capturas/diagnostico/
    [MenuItem(MenuDiag + "11. Fotografar as ferramentas em uso", false, 530)]
    static void FotografarFerramentas()
    {
        if (Jogador() == null) return;
        GameUI.Instance?.FecharModal();
        System.IO.Directory.CreateDirectory("Builds/Capturas/diagnostico");
        var passos = new List<(System.Action fazer, float espera, string arquivo)>
        {
            (() => ChamadoPost(), 2.5f, null),
            (() => UsarPost(), 2.9f, "1_placa_post_55"),
            (() => ChamadoFonteFraca(), 2.5f, null),
            (() => UsarTestadorFonte(), 2.6f, "2_testador_fonte_fraca"),
            (() => ChamadoBateria(), 2.5f, null),
            (() => UsarMultimetroBateria(), 2.6f, "3_multimetro_bateria"),
            (() => ChamadoMemoria(), 2.5f, null),
            (() => UsarPendriveMemoria(), 1.0f, "4a_pendrive_na_mao"),
            (() => { }, 6.5f, "4b_pendrive_teste_memoria"),
            (() => ChamadoCheckup(), 2.5f, null),
            (() => UsarPendriveDisco(), 7.5f, "5_pendrive_disco_sensores"),
            (() => LaudoInteiro(), 1.5f, "6_os_com_laudo"),
        };
        int i = 0;
        double proximo = 0;
        bool capturar = false;
        string arquivo = null;
        void Passo()
        {
            if (!Application.isPlaying) { EditorApplication.update -= Passo; return; }
            if (EditorApplication.timeSinceStartup < proximo) return;
            if (capturar)
            {
                ScreenCapture.CaptureScreenshot($"Builds/Capturas/diagnostico/{arquivo}.png");
                Debug.Log($"[Teste] Foto: {arquivo}.png");
                capturar = false;
                proximo = EditorApplication.timeSinceStartup + 0.4;
                return;
            }
            if (i >= passos.Count)
            {
                EditorApplication.update -= Passo;
                Debug.Log("[Teste] Fotos das ferramentas prontas em Builds/Capturas/diagnostico");
                return;
            }
            var p = passos[i++];
            GameUI.Instance?.FecharModal();
            p.fazer();
            arquivo = p.arquivo;
            capturar = arquivo != null;
            proximo = EditorApplication.timeSinceStartup + p.espera;
        }
        EditorApplication.update += Passo;
    }
}
