using System.Collections.Generic;
using UnityEngine;

public class Interactable : MonoBehaviour
{
    // Todos os interagíveis da cena, para a GameUI achar o mais próximo e mostrar o aviso "[E] ..."
    public static readonly List<Interactable> Todos = new List<Interactable>();

    public string mensagemInteracao = "Pressione E para interagir";
    public float distanciaInteracao = 2.5f;

    private Transform jogador;

    public virtual string TextoPrompt => "[E] " + mensagemInteracao;

    protected virtual void OnEnable() => Todos.Add(this);
    protected virtual void OnDisable() => Todos.Remove(this);

    void Start()
    {
        var p = GameObject.FindWithTag("Player");
        if (p != null) jogador = p.transform;
    }

    public bool JogadorPerto(out float distancia)
    {
        distancia = float.MaxValue;
        // procura o jogador aqui também: uma subclasse com Start próprio esconde o Start desta classe
        if (jogador == null) { var p = GameObject.FindWithTag("Player"); if (p != null) jogador = p.transform; }
        if (jogador == null) return false;
        distancia = Vector3.Distance(transform.position, jogador.position);
        return distancia <= distanciaInteracao;
    }

    // Portas e saídas só respondem quando o jogador olha para elas (evita sair do cliente sem querer)
    protected virtual bool PrecisaMirar => false;

    // Centro de verdade do objeto (colisor ou malha), e não o pivô — a mochila tem o pivô no chão, por exemplo
    Renderer[] renderers;
    Collider[] colisores;
    public Vector3 PontoMira
    {
        get
        {
            colisores ??= GetComponentsInChildren<Collider>();
            renderers ??= GetComponentsInChildren<Renderer>();
            foreach (var c in colisores) if (c != null && c.enabled) return c.bounds.center;
            foreach (var r in renderers) if (r != null && r.enabled) return r.bounds.center;
            return transform.position;
        }
    }

    const float AnguloMaximo = 60f;          // fora disso o objeto não está "na sua frente"
    const float AnguloMaximoMirar = 20f;     // portas: precisa olhar para elas
    const float GrausPorMetro = 12f;         // desempate: um pouco mais perto vale um pouco de ângulo

    // Quem recebe o E: entre os interagíveis ao alcance, o que está mais no centro da visão (e perto).
    // A GameUI usa isso para o aviso "[E] ..." e para decidir quem reage, assim só um objeto reage.
    public static Interactable MaisProximoEmAlcance()
    {
        var cam = Camera.main;
        Interactable melhor = null;
        float melhorNota = float.MaxValue;
        foreach (var it in Todos)
        {
            if (it == null || !it.isActiveAndEnabled || !it.JogadorPerto(out float d)) continue;
            float nota = d;
            if (cam != null)
            {
                Vector3 alvo = it.PontoMira - cam.transform.position;
                float angulo = Vector3.Angle(cam.transform.forward, alvo);
                if (angulo > (it.PrecisaMirar ? AnguloMaximoMirar : AnguloMaximo)) continue;
                nota = angulo + d * GrausPorMetro;
            }
            if (nota < melhorNota) { melhorNota = nota; melhor = it; }
        }
        return melhor;
    }

    protected virtual void Update() { }

    public virtual void Interagir()
    {
        Debug.Log($"[Interactable] Interagindo com {gameObject.name}");
    }
}
