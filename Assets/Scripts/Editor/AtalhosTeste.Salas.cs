using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Atalhos de teste da Etapa 3 (três clientes com sala própria): chamado em cada sala, seguir o contato até o
// equipamento e fotos das salas. Menu "TI Service/Teste (Play)/Salas". Só funcionam com o jogo rodando.
public static partial class AtalhosTeste
{
    const string MenuSalas = Menu + "Salas/";

    [MenuItem(MenuSalas + "1. Chamado na contabilidade (computador)", false, 400)]
    static void ChamadoContabilidade() => ChamadoNaSala("Contabilidade Alfa", TipoEquipamento.Computador);
    [MenuItem(MenuSalas + "2. Chamado na clínica (impressora)", false, 401)]
    static void ChamadoClinica() => ChamadoNaSala("Clínica Gama", TipoEquipamento.Impressora);
    [MenuItem(MenuSalas + "3. Chamado na loja (roteador)", false, 402)]
    static void ChamadoLoja() => ChamadoNaSala("Papelaria Kappa", TipoEquipamento.Roteador);
    [MenuItem(MenuSalas + "3b. Chamado na loja (computador do caixa)", false, 403)]
    static void ChamadoLojaPc() => ChamadoNaSala("Papelaria Kappa", TipoEquipamento.Computador);
    [MenuItem(MenuSalas + "3c. Chamado na clínica (roteador)", false, 404)]
    static void ChamadoClinicaRot() => ChamadoNaSala("Clínica Gama", TipoEquipamento.Roteador);

    // Aceita um chamado do cliente 'nome' com um problema do equipamento pedido. O técnico chega na porta da sala
    // (para testar o contato levando até o equipamento: atalho 4).
    static void ChamadoNaSala(string nome, TipoEquipamento equipamento)
    {
        var mm = MissionManager.Instance;
        if (!Application.isPlaying || mm == null) { Debug.LogWarning("[Teste] Aperte Play antes."); return; }
        if (mm.chamadoAtivo != null) mm.AbandonarChamado();
        if (mm.chamadosDisponiveis.Count == 0) mm.GerarChamados();
        Problema p = null;
        foreach (var lista in new[] { CatalogoProblemas.Faceis, CatalogoProblemas.Medios, CatalogoProblemas.Dificeis })
            foreach (var x in lista) if (p == null && x.equipamento == equipamento) p = x;
        if (p == null) { Debug.LogWarning("[Teste] Sem problema para " + equipamento); return; }
        var c = mm.chamadosDisponiveis[0];
        c.nomeCliente = nome;
        c.exigeNota = false;
        c.tutorial = false;
        c.contratoId = 0;
        c.problema = p;
        c.equipamento = p.equipamento;
        c.solucao = p.solucao;
        c.descricaoProblema = p.descricao;
        if (c.personalidade == Personalidade.Confuso) c.personalidade = Personalidade.Tranquilo;
        GameUI.Instance?.FecharModal();
        mm.AceitarChamado(c);
        var sala = SalaCliente.Atual;
        var eq = EquipamentoDoTipo(equipamento);
        Debug.Log($"[Teste] Chamado de {nome} aceito ({p.solucao}) → sala {(sala != null ? sala.Nome : "?")} | " +
                  $"{equipamento} em {(eq != null ? eq.transform.position.ToString("F2") : "?")} | contato {c.contato}");
        FocarJogo();
    }

    static Equipamento EquipamentoDoTipo(TipoEquipamento t)
    {
        foreach (var e in Object.FindObjectsByType<Equipamento>()) if (e.tipoEquipamento == t) return e;
        return null;
    }

    // Leva o técnico para perto do contato e vai junto atrás dele (como o jogador seguindo) até ele parar
    [MenuItem(MenuSalas + "4. Seguir o contato até o equipamento", false, 410)]
    static void SeguirContato()
    {
        var npc = ClienteAtual();
        var jogador = Jogador();
        if (npc == null || jogador == null) { Debug.LogWarning("[Teste] Aceite um chamado antes (atalhos 1 a 3)."); return; }
        Ir(npc.transform.position + npc.transform.forward * 1.6f + Vector3.up * 1f, npc.transform.position + Vector3.up * 1.5f);
        double fim = EditorApplication.timeSinceStartup + 25;
        Vector3 ultimo = npc.transform.position;
        double parado = EditorApplication.timeSinceStartup;
        void Passo()
        {
            if (!Application.isPlaying || npc == null || EditorApplication.timeSinceStartup > fim)
            {
                EditorApplication.update -= Passo;
                return;
            }
            var j = Jogador();
            Vector3 atras = npc.transform.position - npc.transform.forward * 1.3f;
            if ((j.transform.position - atras).magnitude > 1.5f && (npc.transform.position - ultimo).magnitude > 0.01f)
            {
                var alvo = new GameObject("tmp").transform;
                alvo.position = new Vector3(atras.x, j.transform.position.y, atras.z);
                Teleporte.MoverJogador(j.transform, alvo);
                Object.Destroy(alvo.gameObject);
                j.OlharPara(npc.transform.position + Vector3.up * 1.4f);
            }
            if ((npc.transform.position - ultimo).magnitude > 0.01f) parado = EditorApplication.timeSinceStartup;
            ultimo = npc.transform.position;
            // Parou de andar há 2 s: chegou. O técnico olha para o equipamento.
            if (EditorApplication.timeSinceStartup - parado > 2.0)
            {
                EditorApplication.update -= Passo;
                var eq = EquipamentoDoTipo(npc.chamado.equipamento);
                if (eq != null)
                {
                    var b = eq.GetComponent<Collider>() != null ? eq.GetComponent<Collider>().bounds.center : eq.transform.position;
                    j.OlharPara(b);
                    Debug.Log($"[Teste] Contato parou a {Vector3.Distance(npc.transform.position, eq.transform.position):0.00} m do(a) {eq.Nome}; " +
                              $"técnico a {Vector3.Distance(j.transform.position, eq.transform.position):0.00} m.");
                }
            }
        }
        EditorApplication.update += Passo;
    }

    // Uma foto de cada sala de cliente (do canto da porta, olhando o fundo) e uma de cada equipamento no lugar,
    // em Builds/Capturas/salas/
    [MenuItem(MenuSalas + "5. Fotografar as três salas", false, 420)]
    static void FotografarSalas()
    {
        if (Jogador() == null) return;
        GameUI.Instance?.FecharModal();
        System.IO.Directory.CreateDirectory("Builds/Capturas/salas");
        var fotos = new List<(System.Action ir, string arquivo)>();
        foreach (var s in SalaCliente.Todas)
        {
            var sala = s;
            string nome = sala.tipo.ToString().ToLowerInvariant();
            // Visão geral: perto da porta, de lado, olhando a diagonal da sala
            fotos.Add((() =>
            {
                sala.ReceberEquipamentos();
                Vector3 c = sala.Centro;
                Ir(sala.sala.TransformPoint(new Vector3(-3.9f, 1.15f, -3.3f)), sala.sala.TransformPoint(new Vector3(2.5f, 0.8f, 2.8f)));
            }, $"{nome}_1_geral_a"));
            fotos.Add((() => Ir(sala.sala.TransformPoint(new Vector3(3.9f, 1.15f, -3.3f)), sala.sala.TransformPoint(new Vector3(-2.5f, 0.8f, 2.8f))), $"{nome}_2_geral_b"));
            fotos.Add((() => Ir(sala.sala.TransformPoint(new Vector3(0f, 1.15f, 3.3f)), sala.sala.TransformPoint(new Vector3(0f, 0.9f, -3.8f))), $"{nome}_3_geral_c"));
            foreach (TipoEquipamento t in System.Enum.GetValues(typeof(TipoEquipamento)))
            {
                var tipo = t;
                fotos.Add((() => IrAoEquipamento(tipo), $"{nome}_eq_{tipo.ToString().ToLowerInvariant()}"));
            }
        }
        // Ir → esperar a câmera e as sondas assentarem → capturar (a captura sai no fim do quadro: só depois mover)
        int i = 0, espera = 0;
        bool capturou = true;
        void Passo()
        {
            if (!Application.isPlaying) { EditorApplication.update -= Passo; return; }
            if (espera > 0) { espera--; return; }
            if (!capturou)
            {
                ScreenCapture.CaptureScreenshot($"Builds/Capturas/salas/{fotos[i - 1].arquivo}.png");
                capturou = true;
                espera = 6;
                return;
            }
            if (i >= fotos.Count)
            {
                EditorApplication.update -= Passo;
                Debug.Log($"[Teste] {fotos.Count} fotos das salas em Builds/Capturas/salas.");
                return;
            }
            fotos[i].ir();
            i++;
            capturou = false;
            espera = 40;
        }
        EditorApplication.update += Passo;
    }
    // Banheiro da casa: duas fotos (da porta e do canto do box) em Builds/Capturas/salas/
    [MenuItem(MenuSalas + "6. Fotografar o banheiro da casa", false, 430)]
    static void FotografarBanheiro()
    {
        if (Jogador() == null) return;
        var b = GameObject.Find("Cenario/Banheiro");
        if (b == null) { Debug.LogWarning("[Teste] Sem banheiro (rode o menu 3)."); return; }
        GameUI.Instance?.FecharModal();
        System.IO.Directory.CreateDirectory("Builds/Capturas/salas");
        var poses = new (Vector3 de, Vector3 para, string arquivo)[]
        {
            (new Vector3(0f, 1.05f, 0.75f), new Vector3(0f, 0.6f, -0.85f), "banheiro_1_porta"),
            (new Vector3(1.05f, 1.05f, 0.6f), new Vector3(-1.2f, 0.8f, -0.4f), "banheiro_2_box"),
            (new Vector3(-0.2f, 1.05f, 0.65f), new Vector3(1.3f, 1.4f, -0.3f), "banheiro_3_chuveiro"),
            (new Vector3(0.1f, 1.0f, -0.4f), new Vector3(-1.3f, 1.1f, 0.4f), "banheiro_4_pia"),
        };
        int i = 0, espera = 0;
        bool capturou = true;
        void Passo()
        {
            if (!Application.isPlaying) { EditorApplication.update -= Passo; return; }
            if (espera > 0) { espera--; return; }
            if (!capturou)
            {
                ScreenCapture.CaptureScreenshot($"Builds/Capturas/salas/{poses[i - 1].arquivo}.png");
                capturou = true; espera = 6; return;
            }
            if (i >= poses.Length) { EditorApplication.update -= Passo; Debug.Log("[Teste] Fotos do banheiro em Builds/Capturas/salas."); return; }
            Ir(b.transform.TransformPoint(poses[i].de), b.transform.TransformPoint(poses[i].para));
            i++; capturou = false; espera = 40;
        }
        EditorApplication.update += Passo;
    }
}
