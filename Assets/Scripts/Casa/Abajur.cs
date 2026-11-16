using UnityEngine;

// Abajur do criado-mudo: E acende e apaga (luz quente fraca, a cúpula de tecido brilha por dentro).
// Luz só em tempo real e sem sombra: não entra no cálculo de luz da casa e quase não pesa.
public class Abajur : Interactable
{
    public Light luz;
    public Renderer cupula;
    public Material cupulaAcesa, cupulaApagada;
    public bool aceso;

    public override string TextoPrompt => aceso ? "[E] Apagar o abajur" : "[E] Acender o abajur";

    void Awake()
    {
        distanciaInteracao = 1.5f;
        Aplicar();
    }

    public override void Interagir()
    {
        aceso = !aceso;
        SomManager.TocarEm("clique", transform.position, 0.6f, 1.6f);
        Aplicar();
    }

    void Aplicar()
    {
        if (luz != null) luz.enabled = aceso;
        if (cupula != null) cupula.sharedMaterial = aceso ? cupulaAcesa : cupulaApagada;
    }
}
