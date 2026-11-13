using UnityEngine;

// Porta que abre e fecha com E (escritório e casa). A folha gira na dobradiça;
// o MontarCenario cria a "Dobradica" com a folha, as maçanetas e os espelhos dentro.
public class PortaAbrivel : Interactable
{
    public Transform dobradica;
    [Tooltip("Ângulo aberta (positivo abre para dentro da sala)")]
    public float anguloAberta = 95f;
    public bool aberta;

    float angulo;

    public override string TextoPrompt => aberta ? "[E] Fechar a porta" : "[E] Abrir a porta";

    public override void Interagir() => aberta = !aberta;

    protected override void Update()
    {
        if (dobradica == null) return;
        float alvo = aberta ? anguloAberta : 0f;
        if (Mathf.Approximately(angulo, alvo)) return;
        angulo = Mathf.MoveTowards(angulo, alvo, 240f * Time.deltaTime);
        dobradica.localRotation = Quaternion.Euler(0f, angulo, 0f);
    }
}
