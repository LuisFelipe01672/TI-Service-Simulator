using UnityEngine;

public class Interactable : MonoBehaviour
{
    public string mensagemInteracao = "Pressione E para interagir";
    public float distanciaInteracao = 2.5f;

    private Transform jogador;

    void Start()
    {
        jogador = GameObject.FindWithTag("Player").transform;
    }

    protected virtual void Update()
    {
        if (jogador == null) return;

        float distancia = Vector3.Distance(transform.position, jogador.position);

        if (distancia <= distanciaInteracao)
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                Interagir();
            }
        }
    }

    public virtual void Interagir()
    {
        Debug.Log($"[Interactable] Interagindo com {gameObject.name}");
    }
}