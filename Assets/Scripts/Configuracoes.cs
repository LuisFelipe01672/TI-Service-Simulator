using System;
using UnityEngine;

// Preferências do jogador (salvas no PC com PlayerPrefs, separadas do save do jogo).
public static class Configuracoes
{
    public static float VolumeGeral { get; private set; } = 0.8f;
    public static float VolumeEfeitos { get; private set; } = 1f;
    public static float VolumeAmbiente { get; private set; } = 0.7f;
    public static float Sensibilidade { get; private set; } = 2f;
    public static bool InverterY { get; private set; }
    public static bool TelaCheia { get; private set; } = true;
    public static bool Debug { get; private set; } = true;   // contador de FPS e console de debug

    public const float SensMin = 0.3f, SensMax = 6f;

    // Limite de FPS: -1 = V-Sync (acompanha o monitor), 0 = sem limite, outro = FPS máximo
    public static readonly int[] OpcoesFps = { -1, 30, 60, 90, 120, 144, 165, 240, 0 };
    public static int LimiteFps { get; private set; } = -1;
    public static string TextoFps(int v) => v < 0 ? "V-Sync" : v == 0 ? "Sem limite" : v + " FPS";
    public static void DefinirLimiteFps(int v) { LimiteFps = v; Salvar(); }

    public static event Action Mudou;

    static bool carregado;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AoIniciar()
    {
        Carregar();
        Aplicar();
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += (_, __) => Aplicar();
    }

    public static void Carregar()
    {
        if (carregado) return;
        carregado = true;
        VolumeGeral = PlayerPrefs.GetFloat("cfg_volume", VolumeGeral);
        VolumeEfeitos = PlayerPrefs.GetFloat("cfg_efeitos", VolumeEfeitos);
        VolumeAmbiente = PlayerPrefs.GetFloat("cfg_ambiente", VolumeAmbiente);
        Sensibilidade = PlayerPrefs.GetFloat("cfg_sens", Sensibilidade);
        InverterY = PlayerPrefs.GetInt("cfg_invy", 0) == 1;
        TelaCheia = PlayerPrefs.GetInt("cfg_telacheia", Screen.fullScreen ? 1 : 0) == 1;
        Debug = PlayerPrefs.GetInt("cfg_debug", 1) == 1;
        LimiteFps = PlayerPrefs.GetInt("cfg_fps", LimiteFps);
    }

    public static void DefinirVolumeGeral(float v) { VolumeGeral = Mathf.Clamp01(v); Salvar(); }
    public static void DefinirVolumeEfeitos(float v) { VolumeEfeitos = Mathf.Clamp01(v); Salvar(); }
    public static void DefinirVolumeAmbiente(float v) { VolumeAmbiente = Mathf.Clamp01(v); Salvar(); }
    public static void DefinirSensibilidade(float v) { Sensibilidade = Mathf.Clamp(v, SensMin, SensMax); Salvar(); }
    public static void DefinirInverterY(bool v) { InverterY = v; Salvar(); }
    public static void DefinirTelaCheia(bool v) { TelaCheia = v; Salvar(); }
    public static void DefinirDebug(bool v) { Debug = v; Salvar(); }

    static void Salvar()
    {
        PlayerPrefs.SetFloat("cfg_volume", VolumeGeral);
        PlayerPrefs.SetFloat("cfg_efeitos", VolumeEfeitos);
        PlayerPrefs.SetFloat("cfg_ambiente", VolumeAmbiente);
        PlayerPrefs.SetFloat("cfg_sens", Sensibilidade);
        PlayerPrefs.SetInt("cfg_invy", InverterY ? 1 : 0);
        PlayerPrefs.SetInt("cfg_telacheia", TelaCheia ? 1 : 0);
        PlayerPrefs.SetInt("cfg_debug", Debug ? 1 : 0);
        PlayerPrefs.SetInt("cfg_fps", LimiteFps);
        PlayerPrefs.Save();
        Aplicar();
    }

    public static void Aplicar()
    {
        AudioListener.volume = VolumeGeral;

        // FPS: com V-Sync ligado o targetFrameRate é ignorado, então um exclui o outro
        if (LimiteFps < 0) { QualitySettings.vSyncCount = 1; Application.targetFrameRate = -1; }
        else { QualitySettings.vSyncCount = 0; Application.targetFrameRate = LimiteFps == 0 ? -1 : LimiteFps; }
        // No editor a "tela cheia" não se aplica (a aba Game controla o tamanho)
        if (!Application.isEditor && Screen.fullScreen != TelaCheia)
        {
            if (TelaCheia) Screen.SetResolution(Display.main.systemWidth, Display.main.systemHeight, FullScreenMode.FullScreenWindow);
            else Screen.SetResolution(1600, 900, FullScreenMode.Windowed);
        }
        // Ferramentas de debug (contador de FPS etc.), quando existirem na cena
        // (guarda a referência: depois de desligado, o GameObject.Find não acha mais o objeto)
        for (int i = 0; i < nomesDebug.Length; i++)
        {
            if (objsDebug[i] == null) objsDebug[i] = GameObject.Find(nomesDebug[i]);
            if (objsDebug[i] != null && objsDebug[i].activeSelf != Debug) objsDebug[i].SetActive(Debug);
        }
        Mudou?.Invoke();
    }

    // Console de debug e contador de FPS (Graphy)
    static readonly string[] nomesDebug = { "IngameDebugConsole", "[Graphy]" };
    static readonly GameObject[] objsDebug = new GameObject[2];
}
