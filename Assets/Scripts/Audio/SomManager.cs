using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Sons do jogo: efeitos (2D e no mundo 3D) e ambiente em loop (dentro das salas / na rua).
// Cada som é procurado em Resources/Sons/<nome>; se não existir, é gerado por código (SonsGerados).
// Assim dá para trocar qualquer som por um arquivo de verdade sem mexer no código.
public class SomManager : MonoBehaviour
{
    static SomManager instancia;

    readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
    readonly Dictionary<string, float> ultimoToque = new Dictionary<string, float>();
    readonly List<AudioSource> fontes2D = new List<AudioSource>();
    AudioSource ambienteDentro, ambienteRua;
    readonly List<Bounds> salas = new List<Bounds>();
    float dentroSuave = 1f;

    // ---------- Criação automática (vale para o menu e para o jogo) ----------

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (instancia != null) return;
        var go = new GameObject("SomManager");
        DontDestroyOnLoad(go);
        instancia = go.AddComponent<SomManager>();
    }

    void Awake()
    {
        for (int i = 0; i < 8; i++)
        {
            var s = gameObject.AddComponent<AudioSource>();
            s.playOnAwake = false;
            fontes2D.Add(s);
        }
        ambienteDentro = Loop("ambiente_dentro");
        ambienteRua = Loop("ambiente_rua");
        SceneManager.sceneLoaded += (_, __) => AcharSalas();
        AcharSalas();
    }

    AudioSource Loop(string nome)
    {
        var s = gameObject.AddComponent<AudioSource>();
        s.clip = Clip(nome);
        s.loop = true;
        s.volume = 0f;
        s.Play();
        return s;
    }

    // Salas = objetos do cenário com "Piso" (escritório, cliente, casa). Fora delas é rua.
    void AcharSalas()
    {
        salas.Clear();
        var cenario = GameObject.Find("Cenario");
        if (cenario == null) return;
        foreach (Transform sala in cenario.transform)
        {
            var piso = sala.Find("Piso");
            var r = piso != null ? piso.GetComponent<Renderer>() : null;
            if (r != null) { var b = r.bounds; b.Expand(new Vector3(0f, 8f, 0f)); salas.Add(b); }
        }
    }

    void Update()
    {
        // Ambiente: só no jogo (no menu fica mudo)
        var jogador = GameObject.FindWithTag("Player");
        float alvoDentro = 1f, haJogo = 0f;
        if (jogador != null)
        {
            haJogo = 1f;
            alvoDentro = 0f;
            foreach (var b in salas) if (b.Contains(jogador.transform.position)) { alvoDentro = 1f; break; }
            if (salas.Count == 0) alvoDentro = 1f;
        }
        dentroSuave = Mathf.MoveTowards(dentroSuave, alvoDentro, Time.unscaledDeltaTime * 1.5f);
        float amb = Configuracoes.VolumeAmbiente * haJogo;
        bool noite = DiaManager.Instance != null && DiaManager.Instance.EhNoite;
        ambienteDentro.volume = amb * 0.35f * dentroSuave;
        ambienteRua.volume = amb * (noite ? 0.35f : 0.6f) * (1f - dentroSuave);
    }

    // ---------- API ----------

    // Som "na cabeça" (interface, avisos)
    public static void Tocar(string nome, float volume = 1f, float pitch = 1f)
    {
        if (instancia == null || !instancia.PodeTocar(nome)) return;
        var clip = instancia.Clip(nome);
        if (clip == null) return;
        var fonte = instancia.FonteLivre();
        fonte.pitch = pitch;
        fonte.PlayOneShot(clip, volume * Configuracoes.VolumeEfeitos);
    }

    // Som num ponto do mundo (porta, impressora, passos): mais baixo de longe
    public static void TocarEm(string nome, Vector3 posicao, float volume = 1f, float pitch = 1f)
    {
        if (instancia == null || !instancia.PodeTocar(nome)) return;
        var clip = instancia.Clip(nome);
        if (clip == null) return;
        var go = new GameObject("Som_" + nome);
        go.transform.position = posicao;
        var s = go.AddComponent<AudioSource>();
        s.clip = clip;
        s.spatialBlend = 1f;
        s.rolloffMode = AudioRolloffMode.Linear;
        s.minDistance = 1.5f;
        s.maxDistance = 18f;
        s.pitch = pitch;
        s.volume = volume * Configuracoes.VolumeEfeitos;
        s.Play();
        Destroy(go, clip.length / Mathf.Max(0.1f, pitch) + 0.1f);
    }

    // Som da ação feita no equipamento (impressora imprimindo, bip de ligar, clique de cabo...)
    public static void SomDaAcao(string rotulo, Vector3 posicao)
    {
        string r = rotulo.ToLowerInvariant();
        if (r.Contains("imprimir")) TocarEm("impressora", posicao, 0.9f);
        else if (r.Contains("ligar") || r.Contains("tomada") || r.Contains("reset")) TocarEm("bip", posicao, 0.7f);
        else if (r.Contains("olhar") || r.Contains("ler ") || r.Contains("medir")) { }
        else TocarEm("mexer", posicao, 0.8f, Random.Range(0.9f, 1.1f));
    }

    // Evita o mesmo som várias vezes no mesmo instante (ex.: muitos avisos juntos)
    bool PodeTocar(string nome)
    {
        float agora = Time.unscaledTime;
        if (ultimoToque.TryGetValue(nome, out float t) && agora - t < 0.06f) return false;
        ultimoToque[nome] = agora;
        return true;
    }

    AudioSource FonteLivre()
    {
        foreach (var f in fontes2D) if (!f.isPlaying) return f;
        return fontes2D[0];
    }

    AudioClip Clip(string nome)
    {
        if (clips.TryGetValue(nome, out var c)) return c;
        c = Resources.Load<AudioClip>("Sons/" + nome);
        if (c == null) c = SonsGerados.Gerar(nome);
        clips[nome] = c;
        return c;
    }
}
