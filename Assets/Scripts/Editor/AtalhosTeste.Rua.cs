using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Atalhos de teste da Etapa 9D (vida na rua): trânsito, pedestres e a rua de dia e de noite. Cada um roda sozinho no
// Play e escreve o resultado no log ("[Rua] ..."): batidas entre carros, carro fora do asfalto (atravessando prédio),
// pedestre fora da calçada/faixa ou dentro de carro, e a menor distância entre os carros e o técnico.
// Menu "TI Service/Teste (Play)/Rua".
public static partial class AtalhosTeste
{
    const string MenuRua = Menu + "Rua/";
    const string PastaFotosRua = "Builds/Capturas/rua";

    // ---------- Fiscal: confere a rua a cada quadro enquanto o teste roda ----------

    class FiscalRua
    {
        public int quadros, batidas, foraAsfalto, pedForaCalcada, pedDentroCarro, encostouJogador;
        public float menorFolgaJogador = float.MaxValue;
        public readonly HashSet<string> pares = new HashSet<string>();
        public readonly HashSet<int> presos = new HashSet<int>();
        public readonly Dictionary<string, int> motivos = new Dictionary<string, int>();
        List<Rect> asfalto, calcada;

        public void Quadro()
        {
            var rua = VidaNaRua.Instance;
            if (rua == null) return;
            if (asfalto == null)
            {
                asfalto = Bairro.Pedacos().Where(p => p.tipo == TipoPiso.Asfalto).Select(p => p.area).ToList();
                calcada = Bairro.Pedacos().Where(p => p.tipo == TipoPiso.Calcada).Select(p => p.area).ToList();
            }
            quadros++;
            bool Em(List<Rect> l, Vector3 p) => l.Any(r => r.Contains(new Vector2(p.x, p.z)));
            var cs = rua.carros;
            foreach (var c in cs)
            {
                var t = c.transform;
                Vector3 lado = t.right * RotasTransito.MeiaLargura, fr = t.forward * (RotasTransito.MeioComprimento - 0.2f);
                foreach (var q in new[] { t.position, t.position + fr + lado, t.position + fr - lado, t.position - fr + lado, t.position - fr - lado })
                    if (!Em(asfalto, q)) { foraAsfalto++; break; }
                if (c.tempoBloqueado > 25f && presos.Add(c.id))
                    Debug.LogWarning($"[Rua] Carro preso há {c.tempoBloqueado:0} s: {Desc(c)} · na frente: {(c.bloqueador != null ? Desc(c.bloqueador) : "-")}");
                string m = string.IsNullOrEmpty(c.motivoParada) ? "andando" : c.motivoParada;
                motivos[m] = motivos.TryGetValue(m, out int n) ? n + 1 : 1;
            }
            for (int i = 0; i < cs.Count; i++)
                for (int j = i + 1; j < cs.Count; j++)
                    if (Encostam(cs[i].transform, cs[j].transform, 0.8f, 0.8f))
                    {
                        batidas++;
                        if (pares.Add(cs[i].id + "x" + cs[j].id))
                            Debug.LogWarning($"[Rua] Carros encostando: {Desc(cs[i])} × {Desc(cs[j])}");
                    }
            foreach (var p in rua.pedestres)
            {
                Vector3 pp = p.transform.position;
                bool naFaixa = Enumerable.Range(0, rua.Malha.faixas.Count).Any(f => rua.NaFaixa(f, pp, 0.3f));
                bool rebaixo = pp.x > Bairro.JacarandasX + Bairro.MeiaPista && pp.z > Bairro.RebaixoZ0 && pp.z < Bairro.RebaixoZ1;
                if (!naFaixa && !rebaixo && !Em(calcada, pp)) pedForaCalcada++;
                foreach (var c in cs) if (DentroDoCarro(c.transform, pp, 0.2f)) { pedDentroCarro++; break; }
            }
            var j0 = GameObject.FindWithTag("Player");
            if (j0 != null && !CarroTecnico.NoCarro)
                foreach (var c in cs)
                {
                    float folga = DistanciaAoCarro(c.transform, j0.transform.position) - 0.35f;
                    menorFolgaJogador = Mathf.Min(menorFolgaJogador, folga);
                    if (folga < 0.05f) encostouJogador++;
                }
        }

        static string Desc(CarroNPC c) => $"#{c.id} {c.caminho.de}→{c.caminho.para} s={c.s:0.0}/{c.caminho.Comprimento:0} v={c.v:0.0} lat={c.lateral:0.00} motivo='{c.motivoParada}' pos={c.transform.position} bloq={c.tempoBloqueado:0.0}";

        static float DistanciaAoCarro(Transform c, Vector3 p)
        {
            Vector3 l = c.InverseTransformPoint(p);
            float dx = Mathf.Max(Mathf.Abs(l.x) - RotasTransito.MeiaLargura, 0f), dz = Mathf.Max(Mathf.Abs(l.z) - RotasTransito.MeioComprimento, 0f);
            return Mathf.Sqrt(dx * dx + dz * dz);
        }
        static bool DentroDoCarro(Transform c, Vector3 p, float margem) => DistanciaAoCarro(c, p) < margem;
        static bool Encostam(Transform a, Transform b, float ra, float rb)
        {
            for (int i = -1; i <= 1; i++)
                for (int k = -1; k <= 1; k++)
                {
                    Vector3 pa = a.position + a.forward * (1.25f * i), pb = b.position + b.forward * (1.25f * k);
                    pa.y = pb.y = 0f;
                    if ((pa - pb).sqrMagnitude < (ra + rb - 0.1f) * (ra + rb - 0.1f)) return true;
                }
            return false;
        }

        public string Resumo()
        {
            var rua = VidaNaRua.Instance;
            string m = string.Join(", ", motivos.OrderByDescending(x => x.Value).Select(x => $"{x.Key} {x.Value * 100f / Mathf.Max(1, motivos.Values.Sum()):0}%"));
            return $"{quadros} quadros · carros agora {rua?.carros.Count} · pedestres {rua?.pedestres.Count} · batidas {batidas} ({pares.Count} pares) · " +
                   $"carro fora do asfalto {foraAsfalto} · pedestre fora da calçada {pedForaCalcada} · pedestre dentro de carro {pedDentroCarro} · " +
                   $"encostou no técnico {encostouJogador} · menor folga até o técnico {(menorFolgaJogador == float.MaxValue ? "-" : menorFolgaJogador.ToString("0.00") + " m")} · situação dos carros: {m}";
        }
    }

    static FiscalRua fiscal;
    static void ComecarFiscal() { fiscal = new FiscalRua(); EditorApplication.update -= QuadroFiscal; EditorApplication.update += QuadroFiscal; }
    static void QuadroFiscal() { if (!Application.isPlaying) { EditorApplication.update -= QuadroFiscal; return; } fiscal?.Quadro(); }
    static void FimFiscal(string titulo) { EditorApplication.update -= QuadroFiscal; Debug.Log($"[Rua] {titulo}: {fiscal?.Resumo()}"); }

    static void Hora(float h)
    {
        var d = DiaManager.Instance;
        if (d != null) d.minutoAtual = h * 60f;
    }

    static Etapa FotoRua(string nome) => E("foto " + nome, () =>
    {
        System.IO.Directory.CreateDirectory(PastaFotosRua);
        ScreenCapture.CaptureScreenshot($"{PastaFotosRua}/{nome}.png");
    });

    static bool ProntoRua() { if (!Application.isPlaying || VidaNaRua.Instance == null) { Debug.LogWarning("[Rua] Aperte Play antes (e monte o cenário com o molde do carro)."); return false; } GameUI.Instance?.FecharModal(); return true; }

    // ---------- Atalhos ----------

    [MenuItem(MenuRua + "1. Observar o trânsito por 90 s (cheio, horário comercial)", false, 700)]
    static void ObservarTransito()
    {
        if (!ProntoRua()) return;
        Rodar("Trânsito cheio", new List<Etapa>
        {
            E("11h, 10 carros e 14 pedestres", () => { Hora(11f); VidaNaRua.CarrosFixos = 10; VidaNaRua.PedestresFixos = 14; VidaNaRua.Instance.Repovoar(); }),
            E("ir para a esquina da Jacarandás", () => Ir(new Vector3(20.5f, 1.1f, -15.5f), new Vector3(26f, 2.5f, -8f))),
            E("começar a conferir", ComecarFiscal),
            Esperar("trânsito rodando (sinal)", 25f), FotoRua("01_cruzamento_jacarandas"),
            E("ir para a porta do escritório", () => Ir(new Vector3(-1.5f, 1.1f, -4.3f), new Vector3(20f, 1.5f, -9f))),
            Esperar("trânsito rodando (escritório)", 25f), FotoRua("02_porta_escritorio"),
            E("ir para a esquina da Ipê (PARE)", () => Ir(new Vector3(-20.5f, 1.1f, -15.5f), new Vector3(-26f, 2f, 5f))),
            Esperar("trânsito rodando (Ipê)", 30f), FotoRua("03_pare_ipe"),
            E("resultado", () => { FimFiscal("Trânsito cheio"); VidaNaRua.CarrosFixos = null; VidaNaRua.PedestresFixos = null; }),
        });
    }

    [MenuItem(MenuRua + "2. Ficar parado no meio da rua (os carros param)", false, 701)]
    static void ParadoNaRua()
    {
        if (!ProntoRua()) return;
        Rodar("Técnico no meio da rua", new List<Etapa>
        {
            E("11h, 10 carros", () => { Hora(11f); VidaNaRua.CarrosFixos = 10; VidaNaRua.Instance.Repovoar(); }),
            // na mão de quem vai para o oeste, entre o escritório e a Jacarandás, olhando os carros que vêm
            E("ficar na faixa dos carros", () => Ir(new Vector3(12f, 1.1f, Bairro.AcaciasCentroZ + RotasTransito.DeslocFaixa), new Vector3(40f, 1.2f, Bairro.AcaciasCentroZ + 1.6f))),
            E("começar a conferir", ComecarFiscal),
            Esperar("carros chegando", 30f), FotoRua("04_tecnico_na_rua"),
            Esperar("mais um pouco", 15f),
            E("resultado", () => { FimFiscal("Técnico no meio da rua"); VidaNaRua.CarrosFixos = null; }),
        });
    }

    [MenuItem(MenuRua + "3. Carro do técnico estacionado (os carros desviam)", false, 702)]
    static void CarroEstacionado()
    {
        if (!ProntoRua() || CarroTecnico.Instance == null) return;
        var v = Bairro.VagaDe(TipoEndereco.Escritorio);
        int passaram = 0;
        var antes = new Dictionary<int, float>();
        void Contar()
        {
            if (!Application.isPlaying || VidaNaRua.Instance == null) { EditorApplication.update -= Contar; return; }
            foreach (var c in VidaNaRua.Instance.carros)
            {
                float x = c.transform.position.x;
                if (c.transform.forward.x > -0.5f) continue;   // só quem vai para o oeste (a mão da vaga)
                if (x > v.centro.x + 3f) antes[c.id] = 1f;
                else if (x <= v.centro.x - 3f && antes.TryGetValue(c.id, out float viu) && viu > 0f) { passaram++; antes[c.id] = 0f; }
            }
        }
        Rodar("Carro estacionado", new List<Etapa>
        {
            E("carro do técnico na vaga do escritório", () => { CarroTecnico.Instance.Posicionar(v.centro, v.giro); Hora(11f); VidaNaRua.CarrosFixos = 10; VidaNaRua.Instance.Repovoar(); }),
            E("olhar da calçada", () => Ir(new Vector3(-3f, 1.1f, -4.4f), v.centro + new Vector3(12f, 0.5f, -1f))),
            E("começar a conferir", () => { ComecarFiscal(); EditorApplication.update += Contar; }),
            Esperar("carros passando", 35f), FotoRua("05_desvio_carro_estacionado"),
            Esperar("mais carros", 35f),
            E("resultado", () => { EditorApplication.update -= Contar; FimFiscal("Carro estacionado"); Debug.Log($"[Rua] Passaram pelo carro estacionado: {passaram}"); VidaNaRua.CarrosFixos = null; }),
        });
    }

    [MenuItem(MenuRua + "4. Rua de dia e de noite (fotos: postes, janelas, faróis)", false, 703)]
    static void DiaENoite()
    {
        if (!ProntoRua()) return;
        var vistas = new (string nome, Vector3 de, Vector3 para)[]
        {
            ("acacias_leste", new Vector3(-3f, 1.7f, -4.4f), new Vector3(40f, 2.5f, -8f)),
            ("cruzamento_jacarandas", new Vector3(19f, 1.7f, -15.6f), new Vector3(30f, 3f, -5f)),
            ("ipe_hospital", new Vector3(-21f, 1.7f, -4f), new Vector3(-30f, 5f, 18f)),
        };
        var etapas = new List<Etapa>();
        foreach (var (h, rotulo) in new[] { (11f, "dia"), (18.2f, "anoitecer"), (20.5f, "noite") })
        {
            etapas.Add(E($"{rotulo}: {h:0.0}h", () => { Hora(h); VidaNaRua.CarrosFixos = 8; VidaNaRua.PedestresFixos = 10; VidaNaRua.Instance.Repovoar(); }));
            foreach (var (nome, de, para) in vistas)
            {
                etapas.Add(E("ir " + nome, () => Ir(de - Vector3.up * 0.6f, para)));
                etapas.Add(Esperar("assentar", 4f));
                etapas.Add(FotoRua($"{rotulo}_{nome}"));
            }
        }
        etapas.Add(E("fim", () => { Hora(11f); VidaNaRua.CarrosFixos = null; VidaNaRua.PedestresFixos = null; Debug.Log($"[Rua] Fotos em {PastaFotosRua}"); }));
        Rodar("Dia e noite", etapas);
    }

    // FPS na rua com e sem o movimento (carros e pedestres), de dia e de noite, em 3 pontos
    [MenuItem(MenuRua + "5. Medir FPS (com e sem movimento, dia e noite)", false, 704)]
    static void FpsRua()
    {
        if (!ProntoRua()) return;
        var pontos = new (string nome, Vector3 de, Vector3 para)[]
        {
            ("porta do escritório, olhando a rua", new Vector3(0f, 1f, -4.3f), new Vector3(40f, 2f, -6f)),
            ("esquina da Ipê, olhando o bairro", new Vector3(-21f, 1f, -14f), new Vector3(30f, 3f, -3f)),
            ("cruzamento da Jacarandás", new Vector3(19f, 1f, -15.6f), new Vector3(30f, 3f, -5f)),
        };
        var casos = new List<(float hora, bool movimento)> { (11f, false), (11f, true), (20.5f, false), (20.5f, true) };
        var etapas = new List<Etapa>();
        foreach (var p in pontos)
            foreach (var cs in casos)
            {
                int q0 = 0; double t0 = 0;
                etapas.Add(E($"{p.nome} · {(cs.movimento ? "com" : "sem")} movimento · {cs.hora:0.#}h", () =>
                {
                    Hora(cs.hora);
                    VidaNaRua.Desligada = !cs.movimento;
                    VidaNaRua.CarrosFixos = 9; VidaNaRua.PedestresFixos = 12;
                    if (cs.movimento) VidaNaRua.Instance.Repovoar();
                    Ir(p.de, p.para);
                }));
                etapas.Add(Esperar("assentar", 2f));
                etapas.Add(E("medir", () => { q0 = Time.frameCount; t0 = EditorApplication.timeSinceStartup; }, () => EditorApplication.timeSinceStartup - t0 >= 4.0, 10f));
                etapas.Add(E("anotar", () => Debug.Log($"[FPS] {p.nome} · {(cs.movimento ? "COM" : "SEM")} movimento · {cs.hora:0.#}h: {(Time.frameCount - q0) / (EditorApplication.timeSinceStartup - t0):0} FPS (carros {VidaNaRua.Instance.carros.Count}, pedestres {VidaNaRua.Instance.pedestres.Count})")));
            }
        etapas.Add(E("fim", () => { VidaNaRua.Desligada = false; VidaNaRua.CarrosFixos = null; VidaNaRua.PedestresFixos = null; Hora(11f); Debug.Log("[FPS] Fim da medição da rua."); }));
        Rodar("FPS da rua", etapas);
    }
}
