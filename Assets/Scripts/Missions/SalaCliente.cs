using System.Collections.Generic;
using UnityEngine;

// Tipo de sala de cliente (Etapa 3): cada empresa vai para a sala que combina com o ramo dela.
public enum TipoSala { Contabilidade, Clinica, Loja }

// Uma sala de cliente montada pelo MontarCenario: onde o técnico chega, onde o contato espera, onde ficam
// a impressora, o roteador e o computador e o caminho que o contato faz até cada um.
// Os equipamentos são um conjunto só na cena: ao aceitar um chamado eles vão para os pontos da sala do cliente.
// Fica num objeto "Dados_Sala" dentro da sala (um script no objeto da sala tiraria a sala da luz pré-calculada).
public class SalaCliente : MonoBehaviour
{
    public TipoSala tipo;
    public TipoEndereco endereco = TipoEndereco.Contabilidade;   // prédio (Bairro.Predios)
    public Transform sala;          // objeto da sala (o centro do piso)
    public Transform chegada;       // onde o técnico entra (porta)
    public Transform recepcao;      // onde o contato espera o técnico
    public Transform ancoraImpressora, ancoraRoteador, ancoraComputador;
    // Pontos (no chão) que o contato percorre da recepção até o equipamento; o último é onde ele para, ao lado
    public Transform[] caminhoImpressora, caminhoRoteador, caminhoComputador;

    static readonly List<SalaCliente> todas = new List<SalaCliente>();
    public static IReadOnlyList<SalaCliente> Todas => todas;
    // Sala onde os equipamentos estão agora
    public static SalaCliente Atual { get; private set; }

    void OnEnable() { if (!todas.Contains(this)) todas.Add(this); luzes = null; }

    // Etapa 10E-3: dez salas de cliente na cidade. As lâmpadas (luz direta em tempo real; o rebote é pré-calculado e
    // fica) só acendem com a câmera a menos de DistanciaLuzes do centro da sala.
    public const float DistanciaLuzes = 45f;
    Light[] luzes;
    float proximaLuz;

    void Update()
    {
        if (Time.unscaledTime < proximaLuz) return;
        proximaLuz = Time.unscaledTime + 0.5f + (GetInstanceID() & 7) * 0.02f;
        var cam = Camera.main;
        if (cam == null || sala == null) return;
        luzes ??= sala.GetComponentsInChildren<Light>(true);
        bool perto = (cam.transform.position - Centro).sqrMagnitude < DistanciaLuzes * DistanciaLuzes;
        foreach (var l in luzes) if (l != null && l.enabled != perto) l.enabled = perto;
    }
    void OnDisable() { todas.Remove(this); if (Atual == this) Atual = null; }

    public Vector3 Centro => sala != null ? sala.position : transform.position;

    public string Nome => Bairro.EhCliente(endereco) ? Bairro.Predio(endereco).nome : endereco.ToString();

    public Transform Ancora(TipoEquipamento t)
    {
        switch (t)
        {
            case TipoEquipamento.Impressora: return ancoraImpressora;
            case TipoEquipamento.Roteador: case TipoEquipamento.Rack: case TipoEquipamento.AccessPoint: return ancoraRoteador;   // o rack fica na parede perto do roteador
            case TipoEquipamento.Computador: case TipoEquipamento.Notebook: return ancoraComputador;
            default: throw new System.ArgumentOutOfRangeException(nameof(t), $"Sala sem lugar para {t}");
        }
    }

    public Transform[] Caminho(TipoEquipamento t)
    {
        switch (t)
        {
            case TipoEquipamento.Impressora: return caminhoImpressora;
            case TipoEquipamento.Roteador: case TipoEquipamento.Rack: case TipoEquipamento.AccessPoint: return caminhoRoteador;
            case TipoEquipamento.Computador: case TipoEquipamento.Notebook: return caminhoComputador;
            default: throw new System.ArgumentOutOfRangeException(nameof(t), $"Sala sem caminho para {t}");
        }
    }

    // ---------- Qual sala é de qual cliente (Etapa 10E-3: um prédio fixo por empresa) ----------

    // Pelo ramo no nome da empresa (palavras de cada prédio em Bairro.Predios, os mais específicos primeiro);
    // nome sem pista fica sempre no mesmo prédio (soma dos caracteres)
    public static TipoEndereco EnderecoDoCliente(string nome)
    {
        var predios = Bairro.Predios;
        if (string.IsNullOrEmpty(nome)) return TipoEndereco.Contabilidade;
        string n = nome.ToLowerInvariant();
        foreach (var p in predios)
            foreach (var w in p.palavras)
                if (n.Contains(w)) return p.tipo;
        int soma = 0;
        foreach (char c in n) soma += c;
        return predios[soma % predios.Count].tipo;
    }

    // Tipo (layout de base) da sala do cliente
    public static TipoSala TipoDoCliente(string nome) => Bairro.Predio(EnderecoDoCliente(nome)).sala;

    public static SalaCliente Para(string nomeCliente)
    {
        var end = EnderecoDoCliente(nomeCliente);
        var tipo = Bairro.Predio(end).sala;
        SalaCliente doTipo = null, primeira = null;
        foreach (var s in todas)
        {
            if (s == null) continue;
            if (s.endereco == end) return s;
            if (doTipo == null && s.tipo == tipo) doTipo = s;
            if (primeira == null || s.endereco == TipoEndereco.Contabilidade) primeira = s;
        }
        return doTipo ?? primeira;   // cena sem esse prédio: uma sala do mesmo tipo, a de contabilidade ou a que houver
    }

    public static SalaCliente MaisProxima(Vector3 p)
    {
        SalaCliente melhor = null;
        float d = float.MaxValue;
        foreach (var s in todas)
        {
            if (s == null) continue;
            float x = (s.Centro - p).sqrMagnitude;
            if (x < d) { d = x; melhor = s; }
        }
        return melhor;
    }

    // ---------- Equipamentos ----------

    // Leva impressora, roteador e computador para os lugares desta sala (peças, cabos e LEDs vão junto: são filhos)
    public void ReceberEquipamentos()
    {
        NotebookCliente.Garantir();
        RackCliente.Garantir();
        AccessPointCliente.Garantir();
        foreach (var e in FindObjectsByType<Equipamento>())
        {
            var a = Ancora(e.tipoEquipamento);
            if (a == null) continue;
            if (e.tipoEquipamento == TipoEquipamento.Notebook) continue;   // vai depois, ao lado do monitor
            if (e.tipoEquipamento == TipoEquipamento.Rack) continue;       // vai depois, na parede perto do roteador
            if (e.tipoEquipamento == TipoEquipamento.AccessPoint) continue;   // vai depois, no centro da sala
            e.transform.SetPositionAndRotation(a.position, a.rotation);
        }
        Atual = this;
        Physics.SyncTransforms();
        NotebookCliente.Posicionar(this);
        RackCliente.Posicionar(this);
        AccessPointCliente.Posicionar(this);
        Debug.Log($"[Salas] Equipamentos na sala: {Nome} ({endereco}).");
    }
}
