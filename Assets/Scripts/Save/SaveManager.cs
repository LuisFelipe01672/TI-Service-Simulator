using System;
using System.Collections.Generic;
using System.IO;
using NaughtyAttributes;
using UnityEngine;

// Salva e carrega o progresso do jogador em JSON (Application.persistentDataPath/save.json).
// Salva sozinho quando: um chamado é concluído, alguém é contratado, uma fase é desbloqueada e ao sair do jogo.
[Serializable]
public class SaveData
{
    public int versao = 2;
    public int dia = 1;
    public float saldo;
    public float reputacao;
    public int fase;
    public int chamadosConcluidos;
    public List<Funcionario> equipe = new List<Funcionario>();
    public string salvoEm;
}

[DefaultExecutionOrder(-100)] // carrega antes do MissionManager gerar os chamados
public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance;

    // O menu principal define isso: true = "Continuar", false = "Novo jogo"
    public static bool CarregarAoIniciar = true;

    public static string CaminhoSave => Path.Combine(Application.persistentDataPath, "save.json");
    public static bool ExisteSave => File.Exists(CaminhoSave);

    [ShowNonSerializedField] private int chamadosConcluidos;

    EventBinding<ChamadoConcluidoEvent> bindChamado;
    EventBinding<FuncionarioContratadoEvent> bindContratado;
    EventBinding<FaseDesbloqueadaEvent> bindFase;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(this); return; }
    }

    void OnEnable()
    {
        bindChamado = new EventBinding<ChamadoConcluidoEvent>(_ => { chamadosConcluidos++; Salvar(); });
        bindContratado = new EventBinding<FuncionarioContratadoEvent>(Salvar);
        bindFase = new EventBinding<FaseDesbloqueadaEvent>(Salvar);
        EventBus<ChamadoConcluidoEvent>.Register(bindChamado);
        EventBus<FuncionarioContratadoEvent>.Register(bindContratado);
        EventBus<FaseDesbloqueadaEvent>.Register(bindFase);
    }

    void OnDisable()
    {
        EventBus<ChamadoConcluidoEvent>.Deregister(bindChamado);
        EventBus<FuncionarioContratadoEvent>.Deregister(bindContratado);
        EventBus<FaseDesbloqueadaEvent>.Deregister(bindFase);
    }

    void Start()
    {
        if (CarregarAoIniciar) Carregar();
        else DiaManager.DiaSalvo = 0;   // jogo novo começa no dia 1
    }

    void OnApplicationQuit() => Salvar();

    [Button("Salvar agora")]
    public void Salvar()
    {
        var data = new SaveData
        {
            saldo = GameEconomy.Instance != null ? GameEconomy.Instance.saldo : 0f,
            reputacao = ReputationManager.Instance != null ? ReputationManager.Instance.GetReputacao() : 50f,
            fase = ReputationManager.Instance != null ? ReputationManager.Instance.FaseAtual : 1,
            chamadosConcluidos = chamadosConcluidos,
            dia = DiaManager.Instance != null ? DiaManager.Instance.dia : Mathf.Max(1, DiaManager.DiaSalvo),
            equipe = FuncionarioManager.Instance != null
                     ? new List<Funcionario>(FuncionarioManager.Instance.equipe)
                     : new List<Funcionario>(),
            salvoEm = DateTime.Now.ToString("dd/MM/yyyy HH:mm")
        };

        try
        {
            File.WriteAllText(CaminhoSave, JsonUtility.ToJson(data, true));
            Debug.Log($"[Save] Jogo salvo em {CaminhoSave}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[Save] Falha ao salvar: {e.Message}");
        }
    }

    [Button("Carregar")]
    void BotaoCarregar() => Carregar();

    public bool Carregar()
    {
        if (!ExisteSave) return false;

        SaveData data;
        try
        {
            data = JsonUtility.FromJson<SaveData>(File.ReadAllText(CaminhoSave));
        }
        catch (Exception e)
        {
            Debug.LogError($"[Save] Save corrompido, começando do zero: {e.Message}");
            return false;
        }
        if (data == null) return false;

        if (GameEconomy.Instance != null) GameEconomy.Instance.DefinirSaldo(data.saldo);
        if (ReputationManager.Instance != null) ReputationManager.Instance.DefinirEstado(data.reputacao, data.fase);
        if (FuncionarioManager.Instance != null)
        {
            FuncionarioManager.Instance.equipe = data.equipe ?? new List<Funcionario>();
            foreach (var f in FuncionarioManager.Instance.equipe) f.ocupado = false;
        }
        chamadosConcluidos = data.chamadosConcluidos;
        DiaManager.DiaSalvo = Mathf.Max(1, data.dia);
        if (DiaManager.Instance != null) DiaManager.Instance.dia = DiaManager.DiaSalvo;

        Debug.Log($"[Save] Jogo carregado (salvo em {data.salvoEm}): R${data.saldo:F2}, reputação {data.reputacao:F0}, fase {data.fase}, {data.equipe?.Count ?? 0} funcionário(s)");
        return true;
    }

    [Button("Apagar save")]
    void BotaoApagar() => ApagarSave();

    public static void ApagarSave()
    {
        if (ExisteSave) File.Delete(CaminhoSave);
        Debug.Log("[Save] Save apagado.");
    }
}
