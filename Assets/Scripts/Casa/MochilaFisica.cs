using PrimeTween;
using UnityEngine;

// A mochila de verdade, no home office. Cada dia começa com ela em casa: E pega (some do chão e vai com você).
// Sem ela, no cliente, as ferramentas e peças não estão à mão (Mochila.EsquecidaLonge).
public class MochilaFisica : Interactable
{
    Vector3 escalaOriginal;
    bool avisouEsquecida;

    EventBinding<DiaIniciadoEvent> bDia;

    public override string TextoPrompt => "[E] Pegar a mochila (ferramentas e peças)";

    // O aviso de "novo dia" fica registrado mesmo com o script desligado (mochila nas costas)
    void Awake()
    {
        distanciaInteracao = 1.7f;
        escalaOriginal = transform.localScale;
        bDia = new EventBinding<DiaIniciadoEvent>(_ => Devolver());
        EventBus<DiaIniciadoEvent>.Register(bDia);
        // Aceitou um chamado sem a mochila: lembrete (antes de chegar no cliente de mãos vazias)
        bChamado = new EventBinding<ChamadoAceitoEvent>(_ =>
        {
            if (Mochila.Instance != null && !Mochila.Instance.comVoce)
                GameUI.Instance?.Toast("Lembrete: a mochila com as ferramentas ficou no home office.", UIFactory.Alerta, 4f);
        });
        EventBus<ChamadoAceitoEvent>.Register(bChamado);
    }

    EventBinding<ChamadoAceitoEvent> bChamado;

    void OnDestroy()
    {
        EventBus<DiaIniciadoEvent>.Deregister(bDia);
        EventBus<ChamadoAceitoEvent>.Deregister(bChamado);
    }

    void Start()
    {
        if (Mochila.Instance != null) Mochila.Instance.comVoce = false;   // o dia começa com a mochila em casa
    }

    public override void Interagir()
    {
        if (Mochila.Instance == null) return;
        Mochila.Instance.comVoce = true;
        avisouEsquecida = false;
        SomManager.TocarEm("mexer", transform.position, 0.6f, 0.9f);
        GameUI.Instance?.Toast("Mochila nas costas: ferramentas e peças vão com você.", UIFactory.Sucesso, 2.5f);
        Tween.Scale(transform, Vector3.zero, 0.25f, Ease.InBack).OnComplete(this, m => m.Esconder());
    }

    void Esconder()
    {
        foreach (var r in GetComponentsInChildren<Renderer>()) r.enabled = false;
        foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = false;
        transform.localScale = escalaOriginal;
        enabled = false;   // some do aviso "[E]"
    }

    // Dormiu: a mochila volta para o lugar (de manhã você pega de novo)
    void Devolver()
    {
        if (Mochila.Instance != null) Mochila.Instance.comVoce = false;
        foreach (var r in GetComponentsInChildren<Renderer>(true)) r.enabled = true;
        foreach (var c in GetComponentsInChildren<Collider>(true)) c.enabled = true;
        transform.localScale = escalaOriginal;
        enabled = true;
    }

    protected override void Update()
    {
        // Avisa uma vez ao chegar no cliente sem a mochila
        if (!avisouEsquecida && Mochila.Instance != null && Mochila.Instance.EsquecidaLonge)
        {
            avisouEsquecida = true;
            GameUI.Instance?.Toast("Você esqueceu a mochila em casa! Sem ferramentas e peças aqui.", UIFactory.Perigo, 5f);
        }
    }
}
