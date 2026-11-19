using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Atalhos de teste da Etapa 8 (rede física: organizar rack, projeto de cabeamento e reset do roteador com o clipe).
// Menu "TI Service/Teste (Play)/Rede física". Só funcionam com o jogo rodando.
public static partial class AtalhosTeste
{
    const string MenuRede = Menu + "Rede física/";

    // Um cliente por prédio (Etapa 10E-3: as dez salas); o atalho 8 passa para o próximo
    static readonly string[] clientesRede = { "Escritório Modelo Ltda", "Clínica Bem Viver", "Loja do Bairro",
        "Advocacia Mendes", "Imobiliária Lar Novo", "Escola Aprender", "Laboratório Vida", "Clínica Veterinária Patas",
        "Farmácia Popular do Bairro", "Ótica Bom Olhar" };
    static int salaRede;

    static void EncherMochilaRede()
    {
        var m = Mochila.Instance;
        if (m == null) return;
        foreach (var it in new[] { Item.Etiquetadora, Item.PunchDown, Item.AlicateCrimpagem, Item.TestadorCabo, Item.Clipe })
            if (m.Quantidade(it) <= 0) m.Adicionar(it, 1);
        if (m.Quantidade(Item.ConectorRJ45) < 10) m.Adicionar(Item.ConectorRJ45, 10);
        m.comVoce = true;
    }

    // Projeto já em execução (material entregue) para o cliente de teste
    static Projeto ProjetoTesteRede(TipoProjeto tipo, int tamanho)
    {
        var pm = ProjetosManager.Instance;
        if (Jogador() == null || pm == null) return null;
        string cliente = clientesRede[salaRede % clientesRede.Length];
        var p = pm.projetos.Find(x => x.tipo == tipo && x.empresa == cliente && x.etapa == 4);
        if (p != null) return p;
        p = pm.CriarProjetoContratado(tipo, cliente, tamanho, 4000f);
        p.etapa = 4;
        return p;
    }

    static void VisitaRede(TipoProjeto tipo, int tamanho)
    {
        var mm = MissionManager.Instance;
        if (mm == null) return;
        var p = ProjetoTesteRede(tipo, tamanho);
        if (p == null) return;
        EncherMochilaRede();
        GameUI.Instance?.FecharModal();
        if (mm.chamadoAtivo != null && mm.chamadoAtivo.projetoId != p.id) mm.AbandonarChamado();
        string r = mm.chamadoAtivo != null ? "já estava na visita" : ProjetosManager.Instance.IrExecutar(p);
        IrAoRack();
        Debug.Log($"[Teste] {r} | sala {SalaCliente.Atual?.Nome} | {ProjetosManager.Andamento(p)} | falta: {string.Join(" · ", Pendencias())}");
    }

    static List<string> Pendencias()
    {
        var mm = MissionManager.Instance;
        var e = mm?.Estado;
        if (e == null) return new List<string>();
        return mm.chamadoAtivo.solucao == CatalogoProblemas.OrganizarRack ? e.PendenciasRack() : e.PendenciasPontos();
    }

    static void IrAoRack()
    {
        var t = RackCliente.Transform;
        if (t == null) { Debug.LogWarning("[Teste] Rack não encontrado."); return; }
        Vector3 centro = t.TransformPoint(new Vector3(0f, RackCliente.Base + RackCliente.Altura / 2f - 0.05f, -RackCliente.Profundidade / 2f));
        Vector3 dir = -t.forward; dir.y = 0f; dir.Normalize();
        indicePeca = -1;
        Ir(new Vector3(centro.x, 1.1f, centro.z) + dir * 1.0f, centro);
    }

    static void IrAoPonto(int i)
    {
        var t = RackCliente.Transform;
        if (t == null) return;
        Vector3 p = RackCliente.PosicaoPonto(i);
        Vector3 dir = -t.forward; dir.y = 0f; dir.Normalize();
        Ir(new Vector3(p.x, 1.1f, p.z) + dir * 0.85f, p);
    }

    static bool Executar(string inicio, int ponto = -1)
    {
        var mm = MissionManager.Instance;
        var e = mm?.Estado;
        if (e == null) return false;
        foreach (var a in AcoesFisicas.Para(TipoEquipamento.Rack, e))
        {
            if (!a.rotulo.StartsWith(inicio)) continue;
            string r;
            if (a.aoCrimpar != null) { a.aoCrimpar(true); mm.RegistrarAcao(a.rotulo); r = "(crimpagem certa, sem o minigame)"; }
            else { r = a.executar(); if (!a.observacao) mm.RegistrarAcao(a.rotulo); }
            if (ponto >= 0 && (a.peca ?? "").StartsWith("ponto")) IrAoPonto(ponto); else IrAoRack();
            Debug.Log($"[Teste] {a.rotulo} → {r}");
            return true;
        }
        Debug.LogWarning($"[Teste] Ação não disponível agora: {inicio}");
        return false;
    }

    // Próximo passo do rack bagunçado, na ordem de um técnico caprichoso
    static bool PassoRack()
    {
        var e = MissionManager.Instance?.Estado;
        if (e == null || !e.rackServico) { Debug.LogWarning("[Teste] Comece a visita do rack (atalho 1)."); return false; }
        if (!e.rackPortaAberta && !e.rackFotoDepois && e.PendenciasRack().Count > 2) return Executar("Abrir a porta");
        if (!e.rackFotoAntes && !e.rackMexeu) return Executar("Tirar a foto do antes");
        if (!e.rackMapaAnotado && !e.rackMexeu) return Executar("Anotar o mapa");
        if (e.rackCordoes.Exists(c => !c.Curto)) return Executar("Desconectar os cordões velhos");
        if (e.ProximaPortaSemCordao() > 0) return Executar("Ligar cordão de 0,3 m");
        if (!e.rackNoOrganizador) return Executar("Passar os cordões pelo organizador");
        if (!e.rackVelcro) return Executar("Prender o feixe");
        if (e.ProximaPortaSemEtiqueta() > 0) return Executar("Etiquetar a porta");
        if (!e.rackLinksConferidos) return Executar("Olhar as luzes de link");
        if (e.rackPortaAberta) return Executar("Fechar a porta");
        if (!e.rackFotoDepois) return Executar("Tirar a foto do depois");
        Debug.Log($"[Teste] Rack pronto. Aguardando assinatura: {MissionManager.Instance.chamadoAtivo?.aguardandoAssinatura}");
        return false;
    }

    // Próximo passo do cabeamento: ponto por ponto, depois fecha a porta do rack
    static bool PassoPontos()
    {
        var e = MissionManager.Instance?.Estado;
        if (e == null || e.pontos.Count == 0) { Debug.LogWarning("[Teste] Comece a visita de cabeamento (atalho 4)."); return false; }
        for (int i = 0; i < e.pontos.Count; i++)
        {
            var p = e.pontos[i];
            if (p.Pronto) continue;
            if (!p.canaleta) return Executar($"Fixar a caixa do {p.Nome}", i);
            if (!p.caboPassado) return Executar($"Passar o cabo Cat6 do {p.Nome}", i);
            if (!p.crimpado) return Executar($"Crimpar a ponta do {p.Nome}", i);
            if (!p.noPatch)
            {
                if (!e.rackPortaAberta) return Executar("Abrir a porta");
                return Executar($"Inserir o ponto {p.Nome}");
            }
            if (!p.Ok) return Executar($"Testar o {p.Nome}", i);
            return Executar($"Etiquetar o {p.Nome}", i);
        }
        if (e.rackPortaAberta) return Executar("Fechar a porta");
        Debug.Log($"[Teste] Pontos prontos. Aguardando assinatura: {MissionManager.Instance.chamadoAtivo?.aguardandoAssinatura}");
        return false;
    }

    // Mostra a OS da visita (com as fotos do antes e do depois), como o técnico mostra ao contato
    static void AbrirOSDaVisita()
    {
        var mm = MissionManager.Instance;
        foreach (var npc in Object.FindObjectsByType<ClienteNPC>())
            if (npc.chamado != null && npc.chamado == mm?.chamadoAtivo) { GameUI.Instance?.AbrirOS(npc); return; }
        Debug.LogWarning("[Teste] Contato do chamado não encontrado para mostrar a OS.");
    }

    static void AssinarRede()
    {
        var mm = MissionManager.Instance;
        if (mm?.chamadoAtivo == null) return;
        if (!mm.chamadoAtivo.aguardandoAssinatura) { Debug.LogWarning($"[Teste] Ainda não resolvido. Falta: {string.Join(" · ", Pendencias())}"); return; }
        Sujeira.LimparTudo();
        var os = mm.chamadoAtivo.os;
        int id = mm.chamadoAtivo.projetoId;
        mm.FecharOSAssinada();
        var p = ProjetosManager.Instance?.projetos.Find(x => x.id == id);
        Debug.Log($"[Teste] OS {os?.numero} assinada: {os?.estrelas} estrela(s), fotos: {os?.fotos} · {os?.diagnostico} | projeto: " +
                  (p == null ? "?" : p.etapa == 5 ? "ENTREGUE" : ProjetosManager.Andamento(p)));
    }

    [MenuItem(MenuRede + "1. Projeto de rack: visita (rack bagunçado)", false, 700)]
    static void RedeRack() => VisitaRede(TipoProjeto.Rack, 1);

    [MenuItem(MenuRede + "2. Rack: próximo passo", false, 701)]
    static void RedePassoRack() => PassoRack();

    [MenuItem(MenuRede + "3. Rack inteiro e assinar a OS", false, 702)]
    static void RedeRackInteiro()
    {
        if (MissionManager.Instance?.chamadoAtivo?.solucao != CatalogoProblemas.OrganizarRack) RedeRack();
        for (int i = 0; i < 60 && PassoRack(); i++) { }
        AssinarRede();
    }

    [MenuItem(MenuRede + "4. Projeto de cabeamento: visita (6 pontos)", false, 703)]
    static void RedeCabeamento() => VisitaRede(TipoProjeto.Cabeamento, 6);

    [MenuItem(MenuRede + "5. Cabeamento: próximo passo", false, 704)]
    static void RedePassoPontos() => PassoPontos();

    [MenuItem(MenuRede + "6. Cabeamento: visita inteira e assinar a OS", false, 705)]
    static void RedeCabeamentoInteiro()
    {
        if (MissionManager.Instance?.chamadoAtivo?.solucao != CatalogoProblemas.PassarPontos) RedeCabeamento();
        for (int i = 0; i < 80 && PassoPontos(); i++) { }
        AssinarRede();
    }

    [MenuItem(MenuRede + "7. Reset do roteador com o clipe (10 s)", false, 706)]
    static void RedeReset()
    {
        ChamadoDe(TipoEquipamento.Roteador, "Resetar para padrão");
        var mm = MissionManager.Instance;
        if (mm?.Estado == null) return;
        IrPerto(TipoEquipamento.Roteador, 0.7f);
        EquipamentoPecas.testeClipeAte = Time.time + 10f;
        double fim = EditorApplication.timeSinceStartup + 10.2;
        void Esperar()
        {
            if (!Application.isPlaying) { EditorApplication.update -= Esperar; return; }
            if (EditorApplication.timeSinceStartup < fim) return;
            EditorApplication.update -= Esperar;
            foreach (var a in AcoesFisicas.Para(TipoEquipamento.Roteador, mm.Estado))
                if (a.animacao == "clipe") { Debug.Log("[Teste] Reset: " + a.executar()); break; }
        }
        EditorApplication.update += Esperar;
        Debug.Log("[Teste] Segurando o clipe no furo de reset por 10 s...");
    }

    [MenuItem(MenuRede + "8. Trocar a sala dos próximos testes (passa pelas 10 salas)", false, 707)]
    static void RedeTrocarSala()
    {
        salaRede++;
        string c = clientesRede[salaRede % clientesRede.Length];
        Debug.Log($"[Teste] Próximas visitas de rede no cliente: {c} (prédio {Bairro.Predio(SalaCliente.EnderecoDoCliente(c)).nome}).");
    }

    // Fotos: rack bagunçado, organizando, organizado; pontos sendo passados; clipe no reset
    [MenuItem(MenuRede + "9. Fotografar rack, cabeamento e reset", false, 710)]
    static void RedeFotos()
    {
        if (Jogador() == null) return;
        GameUI.Instance?.FecharModal();
        System.IO.Directory.CreateDirectory("Builds/Capturas/rede");
        var passos = new List<(System.Action fazer, float espera, string arquivo)>
        {
            (() => RedeRack(), 2.5f, "1_rack_baguncado"),
            (() => { for (int k = 0; k < 4; k++) PassoRack(); }, 1.2f, "2_cordoes_desconectados"),
            (() => { for (int k = 0; k < 8; k++) PassoRack(); }, 1.2f, "3_cordoes_curtos"),
            (() => { for (int k = 0; k < 10; k++) PassoRack(); }, 1.2f, "4_organizado_etiquetado"),
            (() => { for (int k = 0; k < 4; k++) PassoRack(); }, 1.5f, "5_porta_fechada"),
            (() => AbrirOSDaVisita(), 1.0f, "5b_os_com_fotos"),
            (() => { AssinarRede(); RedeCabeamento(); }, 2.5f, null),
            (() => { IrAoPonto(0); }, 1.0f, "6_marca_de_lapis"),
            (() => { PassoPontos(); PassoPontos(); }, 1.6f, "7_canaleta_e_cabo"),
            (() => { for (int k = 0; k < 8; k++) PassoPontos(); IrAoRack(); }, 1.2f, "8_cabos_no_patch"),
            (() => { for (int k = 0; k < 40 && PassoPontos(); k++) { } IrAoPonto(0); }, 1.2f, "9_tomada_etiquetada"),
            (() => { var t = RackCliente.Transform; if (t != null) Ir(t.position - t.forward * 2.6f + Vector3.up * 1.1f, t.position + Vector3.up * 1.2f); }, 1.2f, "10_parede_inteira"),
            (() => { AssinarRede(); RedeReset(); }, 4.0f, "11_clipe_no_reset"),
            (() => IrPerto(TipoEquipamento.Roteador, 0.45f), 1.0f, "11b_clipe_de_perto"),
            (() => { }, 6.5f, "12_luzes_piscando"),
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
                ScreenCapture.CaptureScreenshot($"Builds/Capturas/rede/{arquivo}.png");
                Debug.Log($"[Teste] Foto: {arquivo}.png");
                capturar = false;
                proximo = EditorApplication.timeSinceStartup + 0.4;
                return;
            }
            if (i >= passos.Count)
            {
                EditorApplication.update -= Passo;
                Debug.Log("[Teste] Fotos da rede prontas em Builds/Capturas/rede");
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
