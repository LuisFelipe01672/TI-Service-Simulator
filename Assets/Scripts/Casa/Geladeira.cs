using PrimeTween;
using UnityEngine;

// Geladeira da cozinha: comer recupera mais energia que o café, mas custa a comida (mercado) e tem intervalo
// de algumas horas (ninguém almoça de hora em hora). A porta abre um pouco na animação, se existir.
public class Geladeira : Interactable
{
    public float energiaPorRefeicao = 30f;
    public float custo = 12f;
    public float intervaloHoras = 3f;
    public int maximoPorDia = 3;
    public Transform porta;   // opcional: gira ao comer

    int refeicoesHoje, diaContado = -1;
    float ultima = -999f;   // hora do jogo

    public override string TextoPrompt
    {
        get
        {
            AtualizarDia();
            var d = DiaManager.Instance;
            if (refeicoesHoje >= maximoPorDia) return "Já comeu o suficiente hoje";
            if (d != null && d.Hora - ultima < intervaloHoras) return "Ainda está de barriga cheia";
            string refeicao = d != null && d.Hora < 10.5f ? "Tomar café da manhã" : d != null && d.Hora < 15f ? "Almoçar" : "Fazer um lanche";
            return $"[E] {refeicao} (+{energiaPorRefeicao:0} de energia, {UIFactory.Dinheiro(custo)})";
        }
    }

    void Awake() => distanciaInteracao = 1.7f;

    void AtualizarDia()
    {
        int dia = DiaManager.Instance != null ? DiaManager.Instance.dia : 0;
        if (dia != diaContado) { diaContado = dia; refeicoesHoje = 0; ultima = -999f; }
    }

    public override void Interagir()
    {
        AtualizarDia();
        var d = DiaManager.Instance;
        if (refeicoesHoje >= maximoPorDia || (d != null && d.Hora - ultima < intervaloHoras)) return;
        if (GameEconomy.Instance != null && GameEconomy.Instance.saldo < custo)
        {
            GameUI.Instance?.Toast("Sem dinheiro nem para a comida...", UIFactory.Perigo, 3f);
            return;
        }
        GameEconomy.Instance?.Gastar(custo, LivroCaixa.Alimentacao);
        refeicoesHoje++;
        ultima = d != null ? d.Hora : 0f;
        if (d != null) d.minutoAtual += 20f;   // preparar e comer leva um tempinho
        EnergiaTecnico.Instance?.Recuperar(energiaPorRefeicao);
        SomManager.TocarEm("mexer", transform.position, 0.7f, 0.85f);
        GameUI.Instance?.Toast($"Refeição feita: +{energiaPorRefeicao:0} de energia (-{UIFactory.Dinheiro(custo)}).", UIFactory.Sucesso, 3f);
        if (porta != null)
        {
            var fechada = porta.localRotation;
            Sequence.Create()
                .Chain(Tween.LocalRotation(porta, fechada * Quaternion.Euler(0, -70f, 0), 0.35f, Ease.OutCubic))
                .ChainDelay(0.8f)
                .Chain(Tween.LocalRotation(porta, fechada, 0.3f, Ease.InCubic));
        }
    }
}
