using PrimeTween;
using UnityEngine;

// PC do jogador em casa: E senta na cadeira (a câmera desliza até a frente do monitor) e abre o sistema
// (chamados, loja, banco — a mesma interface do tablet). Ao fechar, a câmera volta para o jogador em pé.
public class ComputadorCasa : Interactable
{
    [Tooltip("Ponto dos olhos de quem está sentado, olhando para o monitor")]
    public Transform poseSentado;

    Camera cam;
    PlayerController controle;
    Vector3 posLocalOriginal;
    Quaternion rotLocalOriginal;
    enum Estado { Livre, Sentando, Usando, Levantando }
    Estado estado = Estado.Livre;

    public override string TextoPrompt => "[E] Sentar e usar o computador";

    void Awake() => distanciaInteracao = 1.9f;

    public override void Interagir()
    {
        if (estado != Estado.Livre || GameUI.Instance == null || poseSentado == null) return;
        var jogador = GameObject.FindWithTag("Player");
        if (jogador == null) return;
        cam = jogador.GetComponentInChildren<Camera>();
        controle = jogador.GetComponent<PlayerController>();
        if (cam == null) return;

        estado = Estado.Sentando;
        if (controle != null) controle.enabled = false;   // o mouse não gira a câmera enquanto senta
        posLocalOriginal = cam.transform.localPosition;
        rotLocalOriginal = cam.transform.localRotation;
        SomManager.TocarEm("mexer", transform.position, 0.5f, 0.8f);
        Sequence.Create()
            .Group(Tween.Position(cam.transform, poseSentado.position, 0.55f, Ease.InOutSine))
            .Group(Tween.Rotation(cam.transform, poseSentado.rotation, 0.55f, Ease.InOutSine))
            .ChainCallback(() =>
            {
                estado = Estado.Usando;
                if (controle != null) controle.enabled = true;   // parado de qualquer jeito: o painel está aberto
                GameUI.Instance?.AbrirTablet("COMPUTADOR · TI OS");
            });
    }

    protected override void Update()
    {
        // Fechou o sistema (Tab/Esc): levanta da cadeira
        if (estado != Estado.Usando || GameUI.Instance == null || GameUI.Instance.PainelAberto) return;
        estado = Estado.Levantando;
        if (controle != null) controle.enabled = false;
        Sequence.Create()
            .Group(Tween.LocalPosition(cam.transform, posLocalOriginal, 0.45f, Ease.InOutSine))
            .Group(Tween.LocalRotation(cam.transform, rotLocalOriginal, 0.45f, Ease.InOutSine))
            .ChainCallback(() =>
            {
                estado = Estado.Livre;
                if (controle != null) controle.enabled = true;
            });
    }
}
