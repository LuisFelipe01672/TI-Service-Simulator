using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Atalho de teste da Etapa 9B (bairro andando, sem teleporte): o técnico sai da cama e anda de verdade, com o
// CharacterController (colide com muro, meio-fio, móveis e portas), da casa ao escritório e aos três clientes,
// abrindo as portas no caminho. No escritório aceita um chamado da contabilidade e confere se o GPS liga e desliga
// na chegada. Se travar em algum lugar, o log diz onde ("[Bairro] Travou"). Menu "TI Service/Teste (Play)/Bairro".
public static partial class AtalhosTeste
{
    const string MenuBairro = Menu + "Bairro/";

    struct Passo { public string nome; public Vector3 alvo; public System.Action aoChegar; }

    // FPS médio em 3 pontos (quarto, porta do escritório olhando a rua, esquina da Ipê) com o bairro novo ligado e
    // desligado (vizinhança, postes com fios, fachadas e muro): mede quanto a cidade custa na placa de vídeo
    [MenuItem(MenuBairro + "3. Medir FPS (com e sem o bairro)", false, 502)]
    static void MedirFps()
    {
        if (!Application.isPlaying) { Debug.LogWarning("[Bairro] Aperte Play antes."); return; }
        GameUI.Instance?.FecharModal();
        var pesados = new List<GameObject>();
        foreach (var nome in new[] { "Cenario/Vizinhanca", "Cenario/Rua/Postes", "Cenario/Muro" }) { var g = GameObject.Find(nome); if (g != null) pesados.Add(g); }
        foreach (var f in Object.FindObjectsByType<Transform>()) if (f.name == "Fachada") pesados.Add(f.gameObject);
        var pontos = new (string nome, Vector3 de, Vector3 para)[]
        {
            ("quarto", new Vector3(0.2f, 1f, -20.7f), new Vector3(-2f, 1f, -21.5f)),
            ("porta do escritório, olhando a rua", new Vector3(0f, 1f, -4.3f), new Vector3(40f, 2f, -6f)),
            ("esquina da Ipê, olhando o bairro", new Vector3(-21f, 1f, -14f), new Vector3(30f, 3f, -3f)),
        };
        int i = 0; bool comBairro = true; int quadros = 0; double t0 = 0; bool medindo = false;
        void Passo()
        {
            if (!Application.isPlaying) { EditorApplication.update -= Passo; return; }
            if (!medindo)
            {
                foreach (var g in pesados) g.SetActive(comBairro);
                Ir(pontos[i].de, pontos[i].para);
                t0 = EditorApplication.timeSinceStartup + 1.0;   // 1 s para assentar
                quadros = Time.frameCount; medindo = true;
                return;
            }
            double agora = EditorApplication.timeSinceStartup;
            if (agora < t0) { quadros = Time.frameCount; return; }
            if (agora - t0 < 3.0) return;
            float fps = (Time.frameCount - quadros) / (float)(agora - t0);
            Debug.Log($"[FPS] {pontos[i].nome} · {(comBairro ? "COM" : "SEM")} o bairro novo: {fps:0} FPS");
            medindo = false;
            if (comBairro) comBairro = false; else { comBairro = true; i++; }
            if (i >= pontos.Length)
            {
                foreach (var g in pesados) g.SetActive(true);
                Debug.Log("[FPS] Fim da medição.");
                EditorApplication.update -= Passo;
            }
        }
        EditorApplication.update += Passo;
        FocarJogo();
    }

    // Fotos da tela: o GPS no canto (indo do escritório à clínica) e o app Mapa aberto, em Builds/Capturas/bairro
    [MenuItem(MenuBairro + "2. Fotos do GPS e do mapa do celular", false, 501)]
    static void FotosGps()
    {
        if (!Application.isPlaying || GPS.Instance == null) { Debug.LogWarning("[Bairro] Aperte Play antes."); return; }
        System.IO.Directory.CreateDirectory("Builds/Capturas/bairro");
        GameUI.Instance?.FecharModal();
        GPS.Instance.Definir(Bairro.De(TipoEndereco.Clinica));
        Ir(new Vector3(-2f, 1f, -3.9f), new Vector3(-40f, 2f, -4.4f));
        double t0 = EditorApplication.timeSinceStartup;
        int etapa = 0;
        void Passo()
        {
            if (!Application.isPlaying) { EditorApplication.update -= Passo; return; }
            double t = EditorApplication.timeSinceStartup - t0;
            if (etapa == 0 && t > 1.5) { ScreenCapture.CaptureScreenshot("Builds/Capturas/bairro/gps_rua.png"); etapa = 1; }
            else if (etapa == 1 && t > 2.5) { GameUI.Instance?.AbrirMapa(); etapa = 2; }
            else if (etapa == 2 && t > 4.0) { ScreenCapture.CaptureScreenshot("Builds/Capturas/bairro/mapa_celular.png"); etapa = 3; }
            else if (etapa == 3 && t > 5.0)
            {
                GameUI.Instance?.FecharModal();
                Debug.Log("[Bairro] Fotos em Builds/Capturas/bairro (gps_rua, mapa_celular).");
                EditorApplication.update -= Passo;
            }
        }
        EditorApplication.update += Passo;
        FocarJogo();
    }

    // Etapa 10E-1: o GPS segue o caminho mais curto pelas ruas (aponta para o próximo cruzamento, não para o destino em
    // linha reta). Põe o técnico em 5 pontos com destinos diferentes, confere a direção no texto e tira foto de cada um.
    [MenuItem(MenuBairro + "4. GPS pelo caminho mais curto (direção e fotos)", false, 503)]
    static void GpsPeloCaminho()
    {
        if (!Application.isPlaying || GPS.Instance == null) { Debug.LogWarning("[Bairro] Aperte Play antes."); return; }
        GameUI.Instance?.FecharModal();
        System.IO.Directory.CreateDirectory("Builds/Capturas/bairro");
        var etapas = new List<Etapa>();
        int n = 0, certos = 0;
        void Ponto(string nome, TipoEndereco destino, Vector3 de, Vector3 olhar, string esperado)
        {
            int k = ++n;
            etapas.Add(E(nome, () => { GPS.Instance.Definir(Bairro.De(destino), false); Ir(de, olhar); }));
            etapas.Add(Esperar("GPS atualizar", 1.2f));
            etapas.Add(E("conferir " + nome, () =>
            {
                string t = System.Text.RegularExpressions.Regex.Replace(GPS.Instance.Texto, "<.*?>", "").Replace("\n", " | ");
                bool ok = t.Contains(esperado);
                if (ok) certos++;
                Debug.Log($"[GPS] {k}. {nome}: {(ok ? "OK" : "DIFERENTE")} (esperado '{esperado}') — {t}");
                ScreenCapture.CaptureScreenshot($"Builds/Capturas/bairro/gps_caminho_{k}.png");
            }));
            etapas.Add(Esperar("foto", 0.5f));
        }
        // em linha reta a coleta fica à esquerda (sudoeste); pelas ruas, primeiro segue até a Ipê
        Ponto("porta do escritório → coleta, olhando para o oeste", TipoEndereco.Coleta, new Vector3(0f, 1.1f, -4.4f), new Vector3(-30f, 1.5f, -4.4f), "siga em frente");
        Ponto("na esquina da Ipê → coleta, olhando para o oeste", TipoEndereco.Coleta, new Vector3(-24f, 1.1f, -9.5f), new Vector3(-40f, 1.5f, -9.5f), "vire à esquerda");
        Ponto("porta da clínica → fornecedor, olhando para o leste", TipoEndereco.Fornecedor, new Vector3(-42f, 1.1f, -4.4f), new Vector3(0f, 1.5f, -4.4f), "siga em frente");
        Ponto("na esquina da Jacarandás → fornecedor, olhando para o leste", TipoEndereco.Fornecedor, new Vector3(24f, 1.1f, -9.5f), new Vector3(40f, 1.5f, -9.5f), "vire à direita");
        Ponto("coleta → posto, olhando para o norte (pela Ipê)", TipoEndereco.Posto, new Vector3(-30.5f, 1.1f, -30f), new Vector3(-30.5f, 1.5f, 0f), "siga em frente");
        etapas.Add(E("resultado", () => { GPS.Instance.Desligar(); Debug.Log($"[GPS] Fim: {certos} de {n} direções certas. Fotos em Builds/Capturas/bairro (gps_caminho_*)."); }));
        Rodar("GPS pelas ruas", etapas);
    }

    [MenuItem(MenuBairro + "1. Andar da casa ao escritório e aos 3 clientes", false, 500)]
    static void AndarPeloBairro()
    {
        if (!Application.isPlaying) { Debug.LogWarning("[Bairro] Aperte Play antes."); return; }
        var j = Jogador();
        var cc = j != null ? j.GetComponent<CharacterController>() : null;
        if (cc == null) { Debug.LogWarning("[Bairro] Sem jogador."); return; }
        GameUI.Instance?.FecharModal();

        var casa = GameObject.Find("SpawnCasa");
        if (casa != null) Teleporte.MoverJogador(j.transform, casa.transform);   // começa ao lado da cama
        // Etapa 9C: o carro fica na vaga de casa (estacionado na frente de um cliente, ele bloqueia a travessia a pé)
        if (CarroTecnico.Instance != null && !CarroTecnico.NoCarro)
        {
            var vaga = Bairro.VagaDe(TipoEndereco.Casa);
            CarroTecnico.Instance.Posicionar(vaga.centro, vaga.giro);
        }

        var passos = new List<Passo>();
        void P(string nome, float x, float z, System.Action acao = null) => passos.Add(new Passo { nome = nome, alvo = new Vector3(x, 0, z), aoChegar = acao });
        // Casa → portão → faixa de pedestre → escritório
        P("frente da porta de casa (dentro)", 1.5f, -18.3f);
        P("porta de casa", 1.5f, -16.0f);
        P("portão", 1.5f, -14.6f);
        P("faixa de pedestre", 0.3f, -12.0f);
        P("calçada do escritório", 0.3f, -4.6f);
        P("porta do escritório", 0f, -2.6f);
        P("dentro do escritório", 0f, -1.0f, () =>
        {
            var mm = MissionManager.Instance;
            if (mm == null) return;
            if (mm.chamadoAtivo != null) mm.AbandonarChamado();
            if (mm.chamadosDisponiveis.Count == 0) mm.GerarChamados();
            var c = mm.chamadosDisponiveis[0];
            c.nomeCliente = "Contabilidade Alfa";
            c.contratoId = 0; c.tutorial = false; c.exigeNota = false;
            mm.AceitarChamado(c);   // sem teleporte: o GPS liga
            var g = GPS.Instance;
            Debug.Log($"[Bairro] Chamado aceito no escritório. GPS ativo: {g != null && g.Ativo} → {(g != null ? g.Destino.nome + " / " + g.Destino.Linha : "-")}");
        });
        P("saindo do escritório", 0f, -4.4f);
        P("calçada leste", 30f, -4.4f);
        P("porta da contabilidade", 42f, -4.2f, () => Debug.Log($"[Bairro] Na porta da contabilidade. GPS ainda ativo: {GPS.Instance != null && GPS.Instance.Ativo}"));
        P("dentro da contabilidade", 42f, -1.6f);
        P("saindo da contabilidade", 42f, -4.4f, () => GPS.Instance?.Definir(Bairro.De(TipoEndereco.Loja)));
        P("atravessando para a loja", 44f, -9f);
        P("porta da loja", 42f, -13.6f);
        P("dentro da loja", 42f, -16.6f);
        P("saindo da loja", 42f, -13.8f, () => GPS.Instance?.Definir(Bairro.De(TipoEndereco.Fornecedor)));
        P("esquina dos Jacarandás", 31f, -13.8f);
        P("fornecedor", 31f, -29f, () => GPS.Instance?.Definir(Bairro.De(TipoEndereco.Clinica)));
        P("volta pela Jacarandás", 31f, -13.8f);
        P("calçada sul, oeste", -20f, -13.8f);
        P("atravessa a Ipê", -33f, -13.8f);
        P("atravessa as Acácias", -40f, -9f);
        P("porta da clínica", -42f, -4.2f);
        P("dentro da clínica", -42f, -1.6f);
        P("saindo da clínica", -42f, -4.4f, () => GPS.Instance?.Definir(Bairro.De(TipoEndereco.Casa)));
        P("de volta: calçada norte", -10f, -3.8f);
        P("passa pelo orelhão", -2f, -3.8f);
        P("faixa de pedestre de volta", 0.3f, -6f);
        P("portão de casa", 1.5f, -14.6f);
        P("dentro de casa", 1.5f, -18.3f);

        int i = 0;
        float parado = 0f, melhor = float.MaxValue;
        double inicio = EditorApplication.timeSinceStartup, ultimo = inicio;
        Debug.Log($"[Bairro] Saindo de casa: {passos.Count} trechos.");
        void Atualizar()
        {
            if (!Application.isPlaying || j == null) { EditorApplication.update -= Atualizar; return; }
            double agora = EditorApplication.timeSinceStartup;
            float dt = Mathf.Min(0.05f, (float)(agora - ultimo));
            ultimo = agora;
            var p = passos[i];
            var pos = j.transform.position;
            var falta = new Vector3(p.alvo.x - pos.x, 0, p.alvo.z - pos.z);
            // Abre a porta fechada que estiver na frente
            foreach (var porta in Object.FindObjectsByType<PortaAbrivel>())
                if (!porta.aberta && (porta.transform.position - pos).sqrMagnitude < 2.5f * 2.5f) porta.Interagir();
            if (falta.magnitude < 0.35f)
            {
                Debug.Log($"[Bairro] OK {i + 1}/{passos.Count}: {p.nome} {pos:F1} ({agora - inicio:0} s)");
                p.aoChegar?.Invoke();
                i++;
                parado = 0f; melhor = float.MaxValue;
                if (i >= passos.Count)
                {
                    Debug.Log($"[Bairro] Fim do roteiro: andou da casa ao escritório e aos 3 clientes sem travar ({agora - inicio:0} s).");
                    EditorApplication.update -= Atualizar;
                }
                return;
            }
            var dir = falta.normalized;
            cc.Move(dir * 4.5f * dt + Vector3.down * 2f * dt);
            j.transform.rotation = Quaternion.LookRotation(dir);
            if (falta.magnitude < melhor - 0.05f) { melhor = falta.magnitude; parado = 0f; }
            else parado += dt;
            if (parado > 3f)
            {
                var bloqueio = Physics.Raycast(pos + Vector3.up * 0.2f, dir, out var h, 1.5f, ~0, QueryTriggerInteraction.Ignore) ? h.collider.name + " (" + h.collider.transform.parent?.name + ")" : "nada na frente";
                Debug.LogWarning($"[Bairro] Travou indo para '{p.nome}' em {pos:F2}; faltam {falta.magnitude:0.0} m. Na frente: {bloqueio}");
                EditorApplication.update -= Atualizar;
            }
        }
        EditorApplication.update += Atualizar;
        FocarJogo();
    }
}
