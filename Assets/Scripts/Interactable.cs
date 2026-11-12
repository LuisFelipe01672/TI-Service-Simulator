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
        if (jogador == null) return false;
        distancia = Vector3.Distance(transform.position, jogador.position);
        return distancia <= distanciaInteracao;
    }

    // O interagível mais próximo do jogador dentro do alcance (ou null).
    // A GameUI usa isso para o aviso "[E] ..." e para decidir quem recebe o E,
    // assim só um objeto reage mesmo quando vários estão perto.
    public static Interactable MaisProximoEmAlcance()
    {
        Interactable melhor = null;
        float menor = float.MaxValue;
        foreach (var it in Todos)
        {
            if (it != null && it.isActiveAndEnabled && it.JogadorPerto(out float d) && d < menor)
            {
                menor = d;
                melhor = it;
            }
        }
        return melhor;
    }

    protected virtual void Update() { }

    public virtual void Interagir()
    {
        Debug.Log($"[Interactable] Interagindo com {gameObject.name}");
    }
}
