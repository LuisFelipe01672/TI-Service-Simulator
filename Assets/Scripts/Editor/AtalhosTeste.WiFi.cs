using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Atalhos de teste da Etapa 10 (Wi-Fi corporativo: visita do projeto com o AP no teto, medição/mapa de calor,
// defeitos de Wi-Fi do roteador). Menu "TI Service/Teste (Play)/Wi-Fi". Só funcionam com o jogo rodando.
public static partial class AtalhosTeste
{
    const string MenuWiFi = Menu + "Wi-Fi/";

    // Lugar do AP nos passos automáticos (coordenadas da sala): no meio ou num canto
    static Vector2 alvoAp = new Vector2(0.3f, 0.2f);

    static bool ExecutarEm(TipoEquipamento t, string inicio)
    {
        var mm = MissionManager.Instance;
        var e = mm?.Estado;
        if (e == null) return false;
        foreach (var a in AcoesFisicas.Para(t, e))
        {
            if (!a.rotulo.StartsWith(inicio)) continue;
            string r;
            if (a.aoCrimpar != null) { a.aoCrimpar(true); mm.RegistrarAcao(a.rotulo); r = "(crimpagem certa, sem o minigame)"; }
            else { r = a.executar(); if (!a.observacao) mm.RegistrarAcao(a.rotulo); }
            Debug.Log($"[Teste] {a.rotulo} → {r}");
            return true;
        }
        Debug.LogWarning($"[Teste] Ação não disponível agora: {inicio}");
        return false;
    }

    // Embaixo do ponto do AP, olhando para o forro
    static void IrEmbaixoDoAp(Vector2 local)
    {
        var teto = AccessPointCliente.Mundo(local.x, AccessPointCliente.AlturaForro, local.y);
        var pe = AccessPointCliente.Mundo(local.x - 0.35f, 1.1f, local.y - 0.35f);
        indicePeca = -1;
        Ir(pe, teto);
    }

    static void EscadaEmbaixo(Vector2 local)
    {
        var esc = AccessPointCliente.Escada();
        if (esc == null) { Debug.LogWarning("[Teste] Escada não encontrada (o carro está na cena?)."); return; }
        if (CargaFisica.NaMao == esc) CargaFisica.Soltar();
        if (esc.noPortaMalas) PortaMalas.Instance?.Tirar(esc);
        var chao = AccessPointCliente.Mundo(local.x + 0.35f, 0f, local.y + 0.2f);
        var t = AccessPointCliente.Transform;
        CargaFisica.Pousar(esc, chao, Quaternion.Euler(0f, (t != null ? t.eulerAngles.y : 0f) + 90f, 0f));
        esc.transform.position = chao;   // sem esperar a animação
        Debug.Log("[Teste] Escada aberta embaixo do ponto do AP.");
    }

    // Próximo passo da visita do Wi-Fi
    static bool PassoWiFi()
    {
        var mm = MissionManager.Instance;
        var e = mm?.Estado;
        if (e == null || !e.wifiServico) { Debug.LogWarning("[Teste] Comece a visita do Wi-Fi (atalho 1)."); return false; }
        if (SurveyWiFi.Medindo) return false;
        if (!e.surveyAntes && !e.apInstalado) { Debug.Log("[Teste] Medição antes: " + SurveyWiFi.Garantir().MedirTudo()); return true; }
        if (!e.apMarcado) { IrEmbaixoDoAp(alvoAp); return ExecutarEm(TipoEquipamento.AccessPoint, "Marcar aqui"); }
        if (!e.apSuporte)
        {
            Vector2 l = new Vector2(AccessPointCliente.Local(e.apPos).x, AccessPointCliente.Local(e.apPos).z);
            EscadaEmbaixo(l);
            IrEmbaixoDoAp(l);
            return ExecutarEm(TipoEquipamento.AccessPoint, "Subir na escada");
        }
        if (!e.apCabo) return ExecutarEm(TipoEquipamento.AccessPoint, "Fixar a canaleta");
        if (!e.apInstalado)
        {
            if (!e.apCrimpado || (e.apTestado && !e.apPontaBoa)) return ExecutarEm(TipoEquipamento.AccessPoint, e.apCrimpado ? "Refazer a ponta" : "Crimpar a ponta");
            if (!e.apTestado) return ExecutarEm(TipoEquipamento.AccessPoint, "Testar o cabo do AP");
            return ExecutarEm(TipoEquipamento.AccessPoint, "Ligar o cabo e encaixar");
        }
        if (!e.apNoSwitch)
        {
            IrAoRack();
            if (!e.rackPortaAberta) return ExecutarEm(TipoEquipamento.Rack, "Abrir a porta");
            return ExecutarEm(TipoEquipamento.Rack, "Ligar o cabo do");
        }
        if (e.ApIniciando) { e.apLigaEm = Time.time; Debug.Log("[Teste] AP terminou de iniciar (pulando os 20 s)."); return true; }
        if (!e.apRedeEmpresa || !e.apRedeVisitantes || !e.apVisitantesIsolada || !e.ApCanalLivre)
        {
            e.apRedeEmpresa = e.apRedeVisitantes = e.apVisitantesIsolada = true;
            e.apCanal = e.canalVizinhos == 1 ? 11 : 1;
            mm.RegistrarAcao("Painel do AP: rede da empresa, visitantes isolados e canal livre");
            Debug.Log($"[Teste] Painel do AP (pelo atalho): redes criadas, visitantes isolados, canal {e.apCanal} (vizinhos no {e.canalVizinhos}).");
            return true;
        }
        if (e.wifiAtivo) { e.wifiAtivo = false; mm.RegistrarAcao("Wi-Fi do roteador da operadora desligado"); Debug.Log("[Teste] Wi-Fi do roteador desligado."); return true; }
        if (!e.apVisitanteTestado) { Debug.Log("[Teste] Visitante: " + e.TestarComoVisitante()); mm.Verificar(); return true; }
        if (!e.surveyDepois) { Debug.Log("[Teste] Medição depois: " + SurveyWiFi.Garantir().MedirTudo()); return true; }
        if (e.piorDepois < EstadoEquipamentos.MetaDbm)
        {
            Debug.LogWarning($"[Teste] Sinal fraco depois ({e.piorDepois:0} dBm): tirando o AP para pôr no meio.");
            alvoAp = new Vector2(0.3f, 0.2f);
            return ExecutarEm(TipoEquipamento.AccessPoint, "Tirar o");
        }
        if (e.rackPortaAberta) { IrAoRack(); return ExecutarEm(TipoEquipamento.Rack, "Fechar a porta"); }
        mm.Verificar();
        Debug.Log($"[Teste] Wi-Fi pronto. Aguardando assinatura: {mm.chamadoAtivo?.aguardandoAssinatura}");
        return false;
    }

    static void LogWiFi()
    {
        var e = MissionManager.Instance?.Estado;
        if (e == null) return;
        Debug.Log($"[Teste] Wi-Fi: antes {e.piorAntes:0} dBm ({e.fracasAntes} fracos) · depois {(e.surveyDepois ? $"{e.piorDepois:0} dBm ({e.fracasDepois} fracos)" : "-")} · " +
                  $"AP {(e.apInstalado ? "no teto" : "-")} em {AccessPointCliente.Local(e.apPos)} · falta: {string.Join(" · ", e.PendenciasWiFi())}");
    }

    [MenuItem(MenuWiFi + "1. Projeto de Wi-Fi: visita (2 access points)", false, 1100)]
    static void WiFiVisita()
    {
        alvoAp = new Vector2(0.3f, 0.2f);
        VisitaRede(TipoProjeto.WiFi, 2);
        LogWiFi();
    }

    [MenuItem(MenuWiFi + "2. Medir a sala agora (sem andar)", false, 1101)]
    static void WiFiMedir()
    {
        Debug.Log("[Teste] " + SurveyWiFi.Garantir().MedirTudo());
        LogWiFi();
    }

    [MenuItem(MenuWiFi + "3. Wi-Fi: próximo passo", false, 1102)]
    static void WiFiPasso() { PassoWiFi(); LogWiFi(); }

    [MenuItem(MenuWiFi + "4. Visita inteira (AP no meio) e assinar a OS", false, 1103)]
    static void WiFiInteira()
    {
        if (MissionManager.Instance?.chamadoAtivo?.solucao != CatalogoProblemas.InstalarAccessPoint) WiFiVisita();
        alvoAp = new Vector2(0.3f, 0.2f);
        for (int i = 0; i < 40 && PassoWiFi(); i++) { }
        LogWiFi();
        AssinarRede();
    }

    [MenuItem(MenuWiFi + "5. Visita com o AP num canto (sinal fraco depois)", false, 1104)]
    static void WiFiCanto()
    {
        if (MissionManager.Instance?.chamadoAtivo?.solucao != CatalogoProblemas.InstalarAccessPoint) WiFiVisita();
        alvoAp = new Vector2(4.3f, 3.3f);
        var e = MissionManager.Instance?.Estado;
        for (int i = 0; i < 40 && e != null && !(e.surveyDepois) && PassoWiFi(); i++) { }
        LogWiFi();
    }

    [MenuItem(MenuWiFi + "6. Chamado: Wi-Fi lento (canal congestionado)", false, 1105)]
    static void WiFiCanal()
    {
        ChamadoDe(TipoEquipamento.Roteador, CatalogoProblemas.TrocarCanalWiFi);
        var e = MissionManager.Instance?.Estado;
        if (e != null) Debug.Log($"[Teste] Roteador no canal {e.rotCanal}, vizinhos no {e.canalVizinhos}. Redes: {string.Join(" | ", e.RedesVisiveis(-60f, "Cliente"))}");
    }

    [MenuItem(MenuWiFi + "7. Chamado: visitantes na rede da empresa", false, 1106)]
    static void WiFiVisitantes()
    {
        ChamadoDe(TipoEquipamento.Roteador, CatalogoProblemas.CriarRedeVisitantes);
        var e = MissionManager.Instance?.Estado;
        if (e != null) Debug.Log("[Teste] Visitante: " + e.TestarComoVisitante());
    }

    [MenuItem(MenuWiFi + "8. Fotografar a visita do Wi-Fi", false, 1110)]
    static void WiFiFotos()
    {
        if (Jogador() == null) return;
        GameUI.Instance?.FecharModal();
        System.IO.Directory.CreateDirectory("Builds/Capturas/wifi");
        Vector3 Sala(float x, float y, float z) => AccessPointCliente.Mundo(x, y, z);
        var passos = new List<(System.Action fazer, float espera, string arquivo)>
        {
            (() => { if (MissionManager.Instance?.chamadoAtivo != null) MissionManager.Instance.AbandonarChamado(); WiFiVisita(); }, 2.5f, null),
            (() => { WiFiMedir(); Ir(Sala(-4.3f, 1.1f, -3.3f), Sala(0.5f, 0f, 0.8f)); }, 1.2f, "1_mapa_de_calor_antes"),
            (() => { GameUI.Instance?.AbrirAppWiFi(); }, 1.0f, "2_celular_app_wifi"),
            (() => { PassoWiFi(); EscadaEmbaixo(alvoAp); IrEmbaixoDoAp(alvoAp); }, 1.2f, "3_marca_e_escada"),
            (() => { PassoWiFi(); PassoWiFi(); IrEmbaixoDoAp(alvoAp); }, 1.2f, "4_suporte_e_canaleta"),
            (() => { var t = RackCliente.Transform; if (t != null) Ir(t.position - t.forward * 3.2f + Vector3.up * 1.1f, t.position + Vector3.up * 2.3f); }, 1.2f, "5_canaleta_ate_o_rack"),
            (() => { for (int k = 0; k < 6; k++) PassoWiFi(); IrEmbaixoDoAp(alvoAp); }, 1.0f, "6_ap_iniciando"),
            (() => { for (int k = 0; k < 12 && PassoWiFi(); k++) { } Ir(Sala(-4.3f, 1.1f, -3.3f), Sala(0.5f, 0f, 0.8f)); }, 1.2f, "7_mapa_de_calor_depois"),
            (() => { IrEmbaixoDoAp(alvoAp); }, 1.0f, "8_ap_no_ar"),
            (() => AbrirOSDaVisita(), 1.2f, "9_os_com_mapas"),
            (() => { AssinarRede(); WiFiVisita(); }, 2.5f, null),
            (() => { WiFiCanto(); Ir(Sala(-4.3f, 1.1f, -3.3f), Sala(2f, 0f, 1.5f)); }, 1.5f, "10_ap_no_canto_mapa_depois"),
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
                ScreenCapture.CaptureScreenshot($"Builds/Capturas/wifi/{arquivo}.png");
                Debug.Log($"[Teste] Foto: {arquivo}.png");
                capturar = false;
                proximo = EditorApplication.timeSinceStartup + 0.4;
                return;
            }
            if (i >= passos.Count)
            {
                EditorApplication.update -= Passo;
                Debug.Log("[Teste] Fotos do Wi-Fi prontas em Builds/Capturas/wifi");
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
