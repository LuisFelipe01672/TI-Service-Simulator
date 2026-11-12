using UnityEngine;

// Ponto de interação que leva o jogador para outro lugar (ex.: porta do cliente → escritório).
public class Teleporte : Interactable
{
    public Transform destino;
    public string rotulo = "Voltar ao escritório";

    public override string TextoPrompt => "[E] " + rotulo;

    public override void Interagir()
    {
        if (destino == null) return;
        var jogador = GameObject.FindWithTag("Player");
        if (jogador != null) MoverJogador(jogador.transform, destino);
    }

    // Move o jogador respeitando o CharacterController (que ignora mudanças diretas de posição)
    public static void MoverJogador(Transform jogador, Transform alvo)
    {
        var cc = jogador.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;
        jogador.position = alvo.position;
        jogador.rotation = Quaternion.Euler(0f, alvo.eulerAngles.y, 0f);
        if (cc != null) cc.enabled = true;
    }
}
