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

    void OnEnable() { if (!todas.Contains(this)) todas.Add(this); }
    void OnDisable() { todas.Remove(this); if (Atual == this) Atual = null; }

    public Vector3 Centro => sala != null ? sala.position : transform.position;

    public string Nome =>
        tipo == TipoSala.Clinica ? "Clínica" : tipo == TipoSala.Loja ? "Loja" : "Escritório de contabilidade";

    public Transform Ancora(TipoEquipamento t)
    {
        switch (t)
        {
            case TipoEquipamento.Impressora: return ancoraImpressora;
            case TipoEquipamento.Roteador: return ancoraRoteador;
            case TipoEquipamento.Computador: case TipoEquipamento.Notebook: return ancoraComputador;
            default: throw new System.ArgumentOutOfRangeException(nameof(t), $"Sala sem lugar para {t}");
        }
    }

    public Transform[] Caminho(TipoEquipamento t)
    {
        switch (t)
        {
            case TipoEquipamento.Impressora: return caminhoImpressora;
            case TipoEquipamento.Roteador: return caminhoRoteador;
            case TipoEquipamento.Computador: case TipoEquipamento.Notebook: return caminhoComputador;
            default: throw new System.ArgumentOutOfRangeException(nameof(t), $"Sala sem caminho para {t}");
        }
    }

    // ---------- Qual sala é de qual cliente ----------

    static readonly string[] palavrasClinica =
        { "clínica", "clinica", "hospital", "odonto", "laborat", "consult", "veterin", "dra.", "dr.", "saúde" };
    static readonly string[] palavrasLoja =
        { "loja", "farmácia", "farmacia", "ótica", "otica", "autopeças", "gráfica", "grafica", "papelaria", "mercado",
          "padaria", "pet shop", "materiais", "magazine" };
    static readonly string[] palavrasEscritorio =
        { "contabil", "escritório", "escritorio", "advoca", "banco", "construtora", "imobili", "multinacional",
          "indústria", "industria", "despachante", "agência", "agencia", "escola", "colégio", "transportadora", "hotel" };

    // Pelo ramo no nome da empresa; nome sem pista fica sempre na mesma sala (soma dos caracteres)
    public static TipoSala TipoDoCliente(string nome)
    {
        if (string.IsNullOrEmpty(nome)) return TipoSala.Contabilidade;
        string n = nome.ToLowerInvariant();
        foreach (var p in palavrasClinica) if (n.Contains(p)) return TipoSala.Clinica;
        foreach (var p in palavrasLoja) if (n.Contains(p)) return TipoSala.Loja;
        foreach (var p in palavrasEscritorio) if (n.Contains(p)) return TipoSala.Contabilidade;
        int soma = 0;
        foreach (char c in n) soma += c;
        return (TipoSala)(soma % 3);
    }

    public static SalaCliente Para(string nomeCliente)
    {
        var tipo = TipoDoCliente(nomeCliente);
        SalaCliente primeira = null;
        foreach (var s in todas)
        {
            if (s == null) continue;
            if (s.tipo == tipo) return s;
            if (primeira == null || s.tipo == TipoSala.Contabilidade) primeira = s;
        }
        return primeira;   // cena sem a sala desse tipo: a de contabilidade (ou a que houver)
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
        foreach (var e in FindObjectsByType<Equipamento>())
        {
            var a = Ancora(e.tipoEquipamento);
            if (a == null) continue;
            if (e.tipoEquipamento == TipoEquipamento.Notebook) continue;   // vai depois, ao lado do monitor
            e.transform.SetPositionAndRotation(a.position, a.rotation);
        }
        Atual = this;
        Physics.SyncTransforms();
        NotebookCliente.Posicionar(this);
        Debug.Log($"[Salas] Equipamentos na sala: {Nome}.");
    }
}
