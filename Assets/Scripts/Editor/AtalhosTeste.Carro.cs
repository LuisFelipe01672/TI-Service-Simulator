using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Atalhos de teste da Etapa 9C (carro do técnico), no menu "TI Service/Teste (Play)/Carro". Usam um piloto
// automático (CarroTecnico.PilotoAutomatico) que segue pontos no meio da faixa: a física, o GPS, o combustível e o
// hodômetro são os de verdade. O log conta cada etapa ("[Carro] ...") e as fotos vão para Builds/Capturas/carro.
public static partial class AtalhosTeste
{
    const string MenuCarro = Menu + "Carro/";
    const string PastaFotosCarro = "Builds/Capturas/carro";

    // ---------- Roteiro de etapas (cada uma começa, espera uma condição e passa para a próxima) ----------

    class Etapa
    {
        public string nome;
        public System.Action inicio;
        public System.Func<bool> pronto;
        public float limite = 60f;
    }

    static void Rodar(string titulo, List<Etapa> etapas)
    {
        int i = -1;
        double inicioEtapa = 0, inicioTudo = EditorApplication.timeSinceStartup;
        void Passo()
        {
            if (!Application.isPlaying) { CarroTecnico.PilotoAutomatico = null; EditorApplication.update -= Passo; return; }
            double agora = EditorApplication.timeSinceStartup;
            if (i >= 0)
            {
                var e = etapas[i];
                bool ok = false;
                try { ok = e.pronto == null || e.pronto(); }
                catch (System.Exception ex) { Debug.LogError($"[Carro] Erro em '{e.nome}': {ex}"); ok = true; }
                if (!ok)
                {
                    if (agora - inicioEtapa > e.limite)
                    {
                        Debug.LogWarning($"[Carro] {titulo}: a etapa '{e.nome}' passou de {e.limite:0} s. Parei aqui.");
                        CarroTecnico.PilotoAutomatico = null;
                        EditorApplication.update -= Passo;
                    }
                    return;
                }
                Debug.Log($"[Carro] OK {i + 1}/{etapas.Count}: {e.nome} ({agora - inicioTudo:0} s)");
            }
            i++;
            if (i >= etapas.Count)
            {
                Debug.Log($"[Carro] {titulo}: fim ({agora - inicioTudo:0} s).");
                EditorApplication.update -= Passo;
                return;
            }
            inicioEtapa = agora;
            try { etapas[i].inicio?.Invoke(); }
            catch (System.Exception ex) { Debug.LogError($"[Carro] Erro ao começar '{etapas[i].nome}': {ex}"); }
        }
        Debug.Log($"[Carro] {titulo}: {etapas.Count} etapas.");
        EditorApplication.update += Passo;
        FocarJogo();
    }

    static Etapa E(string nome, System.Action inicio, System.Func<bool> pronto = null, float limite = 60f) =>
        new Etapa { nome = nome, inicio = inicio, pronto = pronto, limite = limite };

    static Etapa Esperar(string nome, float segundos)
    {
        double fim = 0;
        return E(nome, () => fim = EditorApplication.timeSinceStartup + segundos, () => EditorApplication.timeSinceStartup >= fim, segundos + 5f);
    }

    static Etapa Foto(string nome) => E("foto " + nome, () =>
    {
        System.IO.Directory.CreateDirectory(PastaFotosCarro);
        ScreenCapture.CaptureScreenshot($"{PastaFotosCarro}/{nome}.png");
    });

    // ---------- Piloto automático ----------

    static List<Vector3> rota;
    static int rotaI;
    static float rotaVel;
    static bool chegou;
    static float semAvanco, melhorDist;

    static void Dirigir(List<Vector3> pontos, float kmh = 25f)
    {
        rota = pontos; rotaI = 0; rotaVel = kmh / 3.6f; chegou = false; semAvanco = 0f; melhorDist = float.MaxValue;
        CarroTecnico.PilotoAutomatico = Pilotar;
    }

    static (float, float, float, bool) Pilotar(CarroTecnico c)
    {
        if (rota == null || chegou) return (0f, 1f, 0f, true);
        Vector3 p = c.transform.position;
        float D(Vector3 a) => Vector2.Distance(new Vector2(p.x, p.z), new Vector2(a.x, a.z));
        while (rotaI < rota.Count - 1 && D(rota[rotaI]) < 3.2f) { rotaI++; melhorDist = float.MaxValue; }
        bool ultimo = rotaI == rota.Count - 1;
        Vector3 alvo = rota[rotaI];
        float d = D(alvo);
        Vector3 para = alvo - p; para.y = 0f;
        float ang = Vector3.SignedAngle(Vector3.ProjectOnPlane(c.transform.forward, Vector3.up), para, Vector3.up);
        float giro = Mathf.Clamp(ang / 28f, -1f, 1f);
        float vAlvo = rotaVel * Mathf.Lerp(1f, 0.35f, Mathf.Abs(ang) / 70f);
        if (ultimo) vAlvo = Mathf.Min(vAlvo, d * 0.7f);
        float v = c.Velocidade;
        float acel = v < vAlvo - 0.2f ? Mathf.Clamp01((vAlvo - v) * 0.6f + 0.25f) : 0f;
        float freio = v > vAlvo + 0.6f ? Mathf.Clamp01((v - vAlvo) * 0.35f) : 0f;
        bool passou = ultimo && Vector3.Dot(c.transform.forward, para) < 0f;
        if (ultimo && (d < 0.5f || passou)) { acel = 0f; freio = 1f; giro = 0f; if (Mathf.Abs(v) < 0.1f) chegou = true; }
        // travado (bateu em algo): avisa uma vez
        if (d < melhorDist - 0.05f) { melhorDist = d; semAvanco = 0f; } else semAvanco += Time.deltaTime;
        if (semAvanco > 8f && !chegou) { Debug.LogWarning($"[Carro] Piloto sem avançar em {p:F1} indo para {alvo:F1} (ponto {rotaI + 1}/{rota.Count})."); semAvanco = -1000f; }
        return (acel, freio, giro, chegou);
    }

    // Pontos no meio da faixa da direita (mão brasileira)
    static float FaixaSulAcacias => Bairro.AcaciasCentroZ - Bairro.MeiaPista / 2f;     // indo para o leste
    static float FaixaNorteAcacias => Bairro.AcaciasCentroZ + Bairro.MeiaPista / 2f;   // indo para o oeste
    static float FaixaNorteJacarandas => Bairro.JacarandasX + Bairro.MeiaPista / 2f;   // indo para o norte

    static List<Vector3> RotaCasaLoja()
    {
        var loja = Bairro.VagaDe(TipoEndereco.Loja).centro;
        float z = FaixaSulAcacias;
        return new List<Vector3> { new Vector3(14f, 0, z), new Vector3(30f, 0, z), new Vector3(loja.x - 6f, 0, z), new Vector3(loja.x - 3f, 0, loja.z), loja };
    }

    static List<Vector3> RotaCasaPosto()
    {
        float z = FaixaSulAcacias, x = FaixaNorteJacarandas;
        var vaga = Bairro.VagaPosto;
        return new List<Vector3>
        {
            new Vector3(13f, 0, z), new Vector3(21f, 0, z), new Vector3(25.5f, 0, z + 1.2f), new Vector3(x, 0, Bairro.AcaciasAsfaltoZ0 + 1f),
            new Vector3(x, 0, 20f), new Vector3(x, 0, vaga.z - 4.5f), new Vector3(x + 2.5f, 0, vaga.z - 1f), new Vector3(vaga.x - 5f, 0, vaga.z),
            vaga,
        };
    }

    // ---------- Ajudantes ----------

    static CarroTecnico Carro() => CarroTecnico.Instance;

    static void IrParaPortaDoCarro()
    {
        var c = Carro();
        if (c == null) return;
        Ir(c.pontoSaida.position + Vector3.up * 1f - c.transform.right * 0.2f, c.porta.position + Vector3.down * 0.2f);
    }

    static Etapa Entrar() => E("entrar no carro", () => { IrParaPortaDoCarro(); Carro().Entrar(); }, () => CarroTecnico.NoCarro && !Carro().Animando, 10f);
    static Etapa Descer() => E("descer do carro", () => { CarroTecnico.PilotoAutomatico = null; Carro().Sair(); }, () => !CarroTecnico.NoCarro && !Carro().Animando, 10f);

    // Anda a pé com o CharacterController (como o atalho do bairro)
    static Etapa Andar(string nome, params Vector3[] pontos)
    {
        int k = 0;
        return E(nome, () => k = 0, () =>
        {
            var j = Jogador();
            var cc = j != null ? j.GetComponent<CharacterController>() : null;
            if (cc == null || !cc.enabled) return false;
            foreach (var porta in Object.FindObjectsByType<PortaAbrivel>())
                if (!porta.aberta && (porta.transform.position - j.transform.position).sqrMagnitude < 2.5f * 2.5f) porta.Interagir();
            var falta = pontos[k] - j.transform.position; falta.y = 0f;
            if (falta.magnitude < 0.35f) { k++; return k >= pontos.Length; }
            cc.Move(falta.normalized * 4f * Mathf.Min(0.05f, Time.deltaTime) + Vector3.down * 0.1f);
            j.transform.rotation = Quaternion.LookRotation(falta.normalized);
            return false;
        }, 40f);
    }

    static string EstadoCarro()
    {
        var c = Carro();
        var os = MissionManager.Instance?.chamadoAtivo?.os;
        return c == null ? "sem carro" : $"carro em {c.transform.position:F1} giro {c.transform.eulerAngles.y:0}°, {c.litros:0.00} L, {c.kmTotal:0.0} km" +
            (os != null ? $" | OS {os.numero}: {os.kmRodados:0.0} km, {os.minutosDeslocamento:0} min, + R$ {os.ValorDeslocamento:0}" : "");
    }

    // ---------- Menus ----------

    [MenuItem(MenuCarro + "0. Diagnóstico do carro (log)", false, 599)]
    static void DiagnosticoCarro()
    {
        var pm = PortaMalas.Instance;
        Debug.Log($"[Carro] Diagnóstico: carro {(Carro() != null)}, porta-malas {(pm != null)}, escada {(pm != null && pm.escada != null)}, maleta {(pm != null && pm.maleta != null)}. " + EstadoCarro());
        foreach (var c in Object.FindObjectsByType<CargaFisica>(FindObjectsInactive.Include))
            Debug.Log($"[Carro]   carga {c.name} ativa {c.gameObject.activeInHierarchy} pai {c.transform.parent?.name} pos {c.transform.position:F2} noPortaMalas {c.noPortaMalas}");
        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
            if (t.name.StartsWith("Carga_")) Debug.Log($"[Carro]   objeto {t.name} pai {t.parent?.name} ativo {t.gameObject.activeInHierarchy}");
    }

    [MenuItem(MenuCarro + "1. Ir até a porta do carro", false, 600)]
    static void IrAteOCarro()
    {
        if (!Application.isPlaying || Carro() == null) { Debug.LogWarning("[Carro] Aperte Play antes (cena com o carro)."); return; }
        GameUI.Instance?.FecharModal();
        IrParaPortaDoCarro();
        Debug.Log("[Carro] Na porta do carro: " + EstadoCarro());
    }

    [MenuItem(MenuCarro + "2. Chamado na loja: dirigir pelo GPS, estacionar, descer e entrar", false, 601)]
    static void ChamadoDeCarro()
    {
        if (!Application.isPlaying || Carro() == null || MissionManager.Instance == null) { Debug.LogWarning("[Carro] Aperte Play antes."); return; }
        GameUI.Instance?.FecharModal();
        var mm = MissionManager.Instance;
        var c = Carro();
        var porta = Bairro.De(TipoEndereco.Loja).porta;
        Rodar("chamado na loja de carro", new List<Etapa>
        {
            E("carro na vaga de casa e chamado da papelaria aceito (sem teleporte: GPS)", () =>
            {
                var v = Bairro.VagaDe(TipoEndereco.Casa);
                c.Posicionar(v.centro, v.giro);
                if (mm.chamadoAtivo != null) mm.AbandonarChamado();
                if (mm.chamadosDisponiveis.Count == 0) mm.GerarChamados();
                var ch = mm.chamadosDisponiveis.First(x => x.contratoId <= 0 && !x.retrabalho && x.servicoLocacao == ServicoLocacao.Nenhum && x.projetoId <= 0);
                ch.nomeCliente = "Papelaria Central"; ch.tutorial = false; ch.exigeNota = false;
                mm.AceitarChamado(ch);
                Debug.Log($"[Carro] GPS: {GPS.Instance?.Destino.nome} → vaga {GPS.Instance?.VagaDestino.centro}. " + EstadoCarro());
            }),
            Entrar(),
            Foto("1_painel_saindo_de_casa"),
            E("dirigir até a vaga da loja", () => Dirigir(RotaCasaLoja()), () => chegou, 60f),
            Foto("2_estacionado_na_loja"),
            E("conferir a vaga", () =>
            {
                var vaga = Bairro.VagaDe(TipoEndereco.Loja);
                var p = c.transform.position;
                Debug.Log($"[Carro] Estacionado a {Vector2.Distance(new Vector2(p.x, p.z), new Vector2(vaga.centro.x, vaga.centro.z)):0.0} m do meio da vaga, " +
                          $"giro {c.transform.eulerAngles.y:0}° (vaga {vaga.giro:0}°). " + EstadoCarro());
            }),
            Descer(),
            Andar("contornar a frente do carro até a calçada e a porta da loja",
                c.transform.position + c.transform.forward * 2.6f - c.transform.right * 1.2f,
                c.transform.position + c.transform.forward * 2.6f + c.transform.right * 1.6f,
                new Vector3(porta.x, 0f, porta.z), new Vector3(porta.x, 0f, porta.z - 2.4f)),
            E("dentro da loja", () =>
            {
                Debug.Log($"[Carro] Dentro da loja. GPS ativo: {GPS.Instance != null && GPS.Instance.Ativo} (desliga ao chegar). " + EstadoCarro());
                Jogador()?.OlharPara(porta + new Vector3(0f, 0.2f, -4f));
            }),
            Esperar("olhar a loja", 1f),
            Foto("3_dentro_da_loja"),
        });
    }

    [MenuItem(MenuCarro + "3. Ir ao posto e abastecer (tanque quase vazio)", false, 602)]
    static void IrAoPosto()
    {
        if (!Application.isPlaying || Carro() == null) { Debug.LogWarning("[Carro] Aperte Play antes."); return; }
        GameUI.Instance?.FecharModal();
        var c = Carro();
        float saldoAntes = 0f, litrosAntes = 0f;
        Rodar("posto", new List<Etapa>
        {
            E("carro em casa com 6 L", () =>
            {
                var v = Bairro.VagaDe(TipoEndereco.Casa);
                c.Posicionar(v.centro, v.giro);
                c.litros = 6f;
                if (GameEconomy.Instance != null && GameEconomy.Instance.saldo < 400f) GameEconomy.Instance.ReceberPagamento(500f);
                GPS.Instance?.Definir(Bairro.De(TipoEndereco.Posto));
            }),
            Entrar(),
            E("dirigir até a bomba", () => Dirigir(RotaCasaPosto(), 22f), () => chegou, 70f),
            Foto("4_chegando_no_posto"),
            Descer(),
            E("mirar a bomba e completar o tanque", () =>
            {
                var bomba = Object.FindAnyObjectByType<PostoCombustivel>();
                Ir(Bairro.VagaPosto + new Vector3(-0.6f, 1f, 0.9f), bomba.transform.position + Vector3.up * 1.1f);
                saldoAntes = GameEconomy.Instance != null ? GameEconomy.Instance.saldo : 0f;
                litrosAntes = c.litros;
                Debug.Log($"[Carro] Aviso na bomba: {bomba.TextoPrompt}");
                bomba.Interagir();
            }, () => c.litros > litrosAntes + 1f && Object.FindAnyObjectByType<PostoCombustivel>().TextoPrompt == "Tanque cheio", 20f),
            Foto("5_abastecido"),
            E("conferir o caixa", () =>
            {
                float gasto = saldoAntes - (GameEconomy.Instance != null ? GameEconomy.Instance.saldo : 0f);
                float noLivro = LivroCaixa.lancamentos.Where(l => l.categoria == LivroCaixa.Combustivel).Sum(l => l.valor);
                Debug.Log($"[Carro] Abasteceu {c.litros - litrosAntes:0.0} L, pagou R$ {gasto:0.00}; livro-caixa (combustível) R$ {noLivro:0.00}. " + EstadoCarro());
            }),
        });
    }

    [MenuItem(MenuCarro + "4. Levar uma encomenda no porta-malas", false, 603)]
    static void EncomendaNoPortaMalas()
    {
        if (!Application.isPlaying || Carro() == null || Mochila.Instance == null) { Debug.LogWarning("[Carro] Aperte Play antes."); return; }
        GameUI.Instance?.FecharModal();
        var c = Carro();
        var m = Mochila.Instance;
        var pm = PortaMalas.Instance;
        Entrega entrega = null;
        Rodar("encomenda no porta-malas", new List<Etapa>
        {
            E("encomenda na porta de casa e o técnico na frente dela", () =>
            {
                var v = Bairro.VagaDe(TipoEndereco.Casa);
                c.Posicionar(v.centro, v.giro);
                entrega = new Entrega { item = Item.ConectorRJ45, quantidade = 50, dia = 1, naPorta = true };
                m.entregas.Add(entrega);
                EncomendaFisica.Sincronizar(m.entregas);
                var ponto = GameObject.Find("PontoEntrega").transform;
                Ir(ponto.position + ponto.forward * 1.2f + Vector3.up, ponto.position);
            }),
            Esperar("caixa aparecer", 0.8f),
            E("[G] pegar a caixa", () =>
            {
                var enc = Object.FindObjectsByType<EncomendaFisica>().First(x => x.entrega == entrega);
                Debug.Log($"[Carro] Aviso da caixa: {enc.TextoPrompt}");
                enc.PegarCaixa();
            }, () => CargaFisica.NaMao != null && CargaFisica.NaMao.entrega == entrega, 5f),
            E("ir até a traseira do carro e abrir o porta-malas", () =>
            {
                Ir(c.transform.position - c.transform.forward * 3.1f + Vector3.up, pm.transform.position);
                Debug.Log($"[Carro] Aviso na traseira: {pm.TextoPrompt}");
                c.AbrirTampa(true);
            }),
            Esperar("tampa subir", 1.2f),
            Foto("6_porta_malas_aberto_com_caixa_na_mao"),
            E("guardar a caixa", () => { Debug.Log($"[Carro] Aviso: {pm.TextoPrompt}"); pm.Interagir(); }, () => CargaFisica.NaMao == null && entrega.local == 1, 5f),
            Esperar("caixa assentar", 0.8f),
            Foto("7_caixa_no_porta_malas"),
            E("fechar e conferir o save do porta-malas", () =>
            {
                c.AbrirTampa(false);
                var salvo = CarroTecnico.Exportar();
                Debug.Log($"[Carro] Porta-malas: {pm.Livres} vaga(s) livre(s); no save: {string.Join(", ", salvo.carga.Select(x => x.tipo + "#" + x.id))}.");
            }),
            Esperar("tampa fechar", 0.8f),
            E("abrir de novo, tirar a caixa e largar no chão (vira encomenda de novo)", () =>
            {
                c.AbrirTampa(true);
                var vol = pm.Volumes().First(x => x.entrega == entrega);
                vol.Interagir();
            }, () => CargaFisica.NaMao != null, 5f),
            E("largar", () => CargaFisica.Largar(), () => CargaFisica.NaMao == null && Object.FindObjectsByType<EncomendaFisica>().Any(x => x.entrega == entrega), 5f),
            E("fim", () => { c.AbrirTampa(false); Debug.Log($"[Carro] Encomenda largada no chão (local {entrega.local}), abre com E. " + EstadoCarro()); }),
        });
    }

    [MenuItem(MenuCarro + "5. Fotos do carro (por fora, porta aberta, escada e maleta)", false, 604)]
    static void FotosCarro()
    {
        if (!Application.isPlaying || Carro() == null) { Debug.LogWarning("[Carro] Aperte Play antes."); return; }
        GameUI.Instance?.FecharModal();
        var c = Carro();
        var pm = PortaMalas.Instance;
        Rodar("fotos do carro", new List<Etapa>
        {
            E("lado do motorista", () => Ir(c.transform.position - c.transform.right * 5f + c.transform.forward * 1.5f + Vector3.up * 0.7f, c.transform.position + Vector3.up * 0.7f)),
            Esperar("assentar", 1f), Foto("8_lado_motorista"),
            E("traseira com a tampa aberta", () => { c.AbrirTampa(true); Ir(c.transform.position - c.transform.forward * 4f - c.transform.right * 1.2f + Vector3.up * 0.9f, c.transform.position - c.transform.forward * 1.5f + Vector3.up * 0.7f); }),
            Esperar("tampa", 1.3f), Foto("9_traseira_porta_malas"),
            E("escada no chão (aberta)", () =>
            {
                pm.escada.Interagir();
                CargaFisica.Largar();
            }),
            Esperar("escada abrir", 1f), Foto("10_escada_aberta"),
            E("escada de volta", () => { pm.escada.Interagir(); }, () => CargaFisica.NaMao == pm.escada, 3f),
            E("guardar", () => pm.Interagir(), () => CargaFisica.NaMao == null, 3f),
            E("porta aberta", () => { c.AbrirTampa(false); Ir(c.transform.position - c.transform.right * 3.5f + c.transform.forward * 3f + Vector3.up * 0.8f, c.porta.position); PrimeTween.Tween.LocalRotation(c.porta, Quaternion.Euler(c.portaAberta), 0.4f); }),
            Esperar("porta", 1f), Foto("11_porta_aberta"),
            E("fecha a porta", () => PrimeTween.Tween.LocalRotation(c.porta, Quaternion.identity, 0.3f)),
            E("posto", () => Ir(Bairro.VagaPosto + new Vector3(-9f, 1.2f, -6f), Bairro.BombaPosto + Vector3.up * 2f)),
            Esperar("posto", 1f), Foto("12_posto"),
        });
    }

    [MenuItem(MenuCarro + "6. Medir FPS dirigindo pela Rua das Acácias", false, 605)]
    static void FpsDirigindo()
    {
        if (!Application.isPlaying || Carro() == null) { Debug.LogWarning("[Carro] Aperte Play antes."); return; }
        GameUI.Instance?.FecharModal();
        var c = Carro();
        int q0 = 0; double t0 = 0;
        Rodar("FPS dirigindo", new List<Etapa>
        {
            E("carro em casa", () => { var v = Bairro.VagaDe(TipoEndereco.Casa); c.Posicionar(v.centro, v.giro); }),
            Entrar(),
            E("dirigir até a loja medindo", () => { q0 = Time.frameCount; t0 = EditorApplication.timeSinceStartup; Dirigir(RotaCasaLoja(), 35f); }, () => chegou, 60f),
            E("resultado", () => Debug.Log($"[FPS] Dirigindo de casa até a loja: {(Time.frameCount - q0) / (EditorApplication.timeSinceStartup - t0):0} FPS (editor). " + EstadoCarro())),
            Descer(),
        });
    }
}
