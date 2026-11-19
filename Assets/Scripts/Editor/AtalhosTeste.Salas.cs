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

    // ---------- Etapa 10E-3: sete clientes novos com interior ----------
    // Um nome por prédio novo (cada um cai sempre no mesmo prédio: SalaCliente.EnderecoDoCliente)
    static readonly string[] clientesNovos = { "Advocacia Lambda", "Imobiliária Pi", "Escola Rô", "Laboratório Tau",
                                               "Clínica Veterinária Ípsilon", "Farmácia Ômicron", "Ótica Sigma" };
    static int clienteNovo;
    static string ClienteNovo => clientesNovos[clienteNovo % clientesNovos.Length];

    [MenuItem(MenuSalas + "7. Cliente novo: chamado de impressora", false, 440)]
    static void ChamadoNovoImpressora() => ChamadoNaSala(ClienteNovo, TipoEquipamento.Impressora);
    [MenuItem(MenuSalas + "8. Cliente novo: chamado de roteador", false, 441)]
    static void ChamadoNovoRoteador() => ChamadoNaSala(ClienteNovo, TipoEquipamento.Roteador);
    [MenuItem(MenuSalas + "9. Cliente novo: chamado de computador", false, 442)]
    static void ChamadoNovoComputador() => ChamadoNaSala(ClienteNovo, TipoEquipamento.Computador);
    [MenuItem(MenuSalas + "10. Passar para o próximo cliente novo", false, 443)]
    static void ProximoClienteNovo()
    {
        clienteNovo++;
        var p = Bairro.Predio(SalaCliente.EnderecoDoCliente(ClienteNovo));
        Debug.Log($"[Teste] Cliente novo: {ClienteNovo} → {p.nome} ({p.rua}, {p.numero}).");
    }

    // Impressora (papel atolado) e roteador (reiniciar na tomada) resolvidos de ponta a ponta no cliente novo atual:
    // aceita, faz as ações físicas, confere o defeito resolvido e assina a OS. Tudo no log ("[Teste] Ponta a ponta").
    [MenuItem(MenuSalas + "13. Cliente novo: impressora e roteador de ponta a ponta", false, 446)]
    static void PontaAPontaClienteNovo()
    {
        if (Jogador() == null) return;
        string cliente = ClienteNovo;
        var passos = new List<(System.Action, float, string)>();
        string nome = SalaCliente.EnderecoDoCliente(cliente).ToString().ToLowerInvariant();
        void Resolver(TipoEquipamento t, string solucao, string foto, params string[] acoes)
        {
            passos.Add((() => { ChamadoNaSala(cliente, t, solucao); IrPerto(t, 0.95f); }, 2.5f, null));
            for (int i = 0; i < acoes.Length; i++)
            {
                string a = acoes[i];
                // tirou da tomada: espera os 10 s antes de religar (como manda o roteiro do roteador)
                // e, religado, espera ele iniciar (as luzes voltam) antes de conferir
                float espera = a.StartsWith("Tirar da tomada") ? EstadoEquipamentos.SegundosParaReiniciarRoteador + 1f
                             : a.StartsWith("Colocar de volta") ? EstadoEquipamentos.SegundosIniciandoRoteador + 1f : 1.2f;
                passos.Add((() => ExecutarEm(t, a), espera, i == 0 ? $"{nome}_{foto}" : null));
            }
            passos.Add((() =>
            {
                var mm = MissionManager.Instance;
                var ch = mm?.chamadoAtivo;
                bool resolvido = ch != null && CatalogoProblemas.Resolvido(ch.problema, ch.estado);
                bool ok = mm != null && mm.Verificar();
                var os = ch?.os;
                if (ok) { Sujeira.LimparTudo(); mm.FecharOSAssinada(); }
                Debug.Log($"[Teste] Ponta a ponta {cliente} ({SalaCliente.Atual?.Nome}) — {t} / {solucao}: resolvido {resolvido}, OS {os?.numero} " +
                          (ok ? $"assinada ({os?.estrelas} estrela(s))" : "NÃO fechou"));
            }, 1.5f, null));
        }
        Resolver(TipoEquipamento.Impressora, "Retirar papel atolado", "imp_atolada",
            "Abrir a tampa traseira e puxar o papel atolado", "Imprimir página de teste");
        Resolver(TipoEquipamento.Roteador, "Reiniciar roteador", "rot_reiniciar",
            "Tirar da tomada", "Colocar de volta na tomada");
        SequenciaFotos("salas", passos);
    }

    // Leva o técnico à calçada na frente do cliente novo atual, olhando a fachada (como se tivesse chegado pelo GPS)
    [MenuItem(MenuSalas + "11. Ir à porta do cliente novo", false, 444)]
    static void IrAoClienteNovo()
    {
        if (Jogador() == null) return;
        var p = Bairro.Predio(SalaCliente.EnderecoDoCliente(ClienteNovo));
        IrAPorta(p);
        Debug.Log($"[Teste] Na porta de {p.nome} ({p.rua}, {p.numero}) em {p.PontoGPS}.");
        FocarJogo();
    }

    static void IrAPorta(PredioCliente p)
    {
        Vector3 fora = (p.PontoGPS - p.Porta).normalized;
        fora.y = 0f;
        Ir(p.PontoGPS + fora * 3.5f + Vector3.up * 0.1f, p.Porta + Vector3.up * 2.2f);
    }

    // Uma foto da fachada de cada prédio de cliente (da calçada da frente), em Builds/Capturas/salas/
    [MenuItem(MenuSalas + "12. Fotografar a fachada de cada cliente", false, 445)]
    static void FotografarFachadas()
    {
        if (Jogador() == null) return;
        GameUI.Instance?.FecharModal();
        System.IO.Directory.CreateDirectory("Builds/Capturas/salas");
        var fotos = new List<(System.Action ir, string arquivo)>();
        foreach (var p in Bairro.Predios)
        {
            var pr = p;
            fotos.Add((() => IrAPorta(pr), $"fachada_{pr.tipo.ToString().ToLowerInvariant()}"));
        }
        Fotografar(fotos, "fachadas");
    }

    // Ir → esperar a câmera e as sondas assentarem → capturar (a captura sai no fim do quadro: só depois mover)
    static void Fotografar(List<(System.Action ir, string arquivo)> fotos, string oQue)
    {
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
                Debug.Log($"[Teste] {fotos.Count} fotos ({oQue}) em Builds/Capturas/salas.");
                return;
            }
            fotos[i].ir();
            i++;
            capturou = false;
            espera = 40;
        }
        EditorApplication.update += Passo;
    }

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
        AceitarComTeleporte(mm, c);
        var sala = SalaCliente.Atual;
        var eq = EquipamentoDoTipo(equipamento);
        Debug.Log($"[Teste] Chamado de {nome} aceito ({p.solucao}) → sala {(sala != null ? sala.Nome : "?")} | " +
                  $"{equipamento} em {(eq != null ? eq.transform.position.ToString("F2") : "?")} | contato {c.contato}");
        FocarJogo();
    }

    // Atalho de teste: aceita o chamado e leva o técnico direto à porta do cliente (no jogo ele vai andando, com GPS)
    static void AceitarComTeleporte(MissionManager mm, Chamado c)
    {
        MissionManager.TeleporteDeTeste = true;
        try { mm.AceitarChamado(c); }
        finally { MissionManager.TeleporteDeTeste = false; }
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
    [MenuItem(MenuSalas + "5. Fotografar todas as salas de cliente", false, 420)]
    static void FotografarSalas()
    {
        if (Jogador() == null) return;
        GameUI.Instance?.FecharModal();
        System.IO.Directory.CreateDirectory("Builds/Capturas/salas");
        var fotos = new List<(System.Action ir, string arquivo)>();
        foreach (var s in SalaCliente.Todas)
        {
            var sala = s;
            string nome = sala.endereco.ToString().ToLowerInvariant();
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
