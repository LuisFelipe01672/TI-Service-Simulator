using UnityEngine;

// Caixa de logística reversa no escritório (Fase 2): os toners usados que você trouxe dos clientes vão aqui.
// A recicladora paga um valor pequeno por cartucho vazio e o descarte correto conta um pouco para a reputação.
public class DescarteEletronico : Interactable
{
    public float valorPorCartucho = 8f;
    static readonly Vector3 Tam = new Vector3(0.5f, 0.55f, 0.4f);

    public override string TextoPrompt
    {
        get
        {
            int n = Mochila.Instance != null ? Mochila.Instance.Quantidade(Item.TonerUsado) + Mochila.Instance.NoEstoque(Item.TonerUsado) : 0;
            return n > 0 ? $"[E] Descartar {n} toner(es) usado(s) (logística reversa)" : "Caixa de logística reversa (sem toner usado para descartar)";
        }
    }

    void Awake() => distanciaInteracao = 1.6f;

    void Start()
    {
        EncomendaFisica.CriarCaixa(transform, Tam);
        EncomendaFisica.Etiqueta(transform, Tam, "LOGÍSTICA REVERSA\ntoner e eletrônicos", true);
    }

    public override void Interagir()
    {
        var m = Mochila.Instance;
        if (m == null) return;
        int naMochila = m.Quantidade(Item.TonerUsado);
        int n = naMochila + m.TirarDoEstoque(Item.TonerUsado);
        if (n <= 0) return;
        for (int i = 0; i < naMochila; i++) m.Usar(Item.TonerUsado);
        float valor = n * valorPorCartucho;
        GameEconomy.Instance?.ReceberPagamento(valor, LivroCaixa.OutrasReceitas);
        ReputationManager.Instance?.AlterarReputacao(Mathf.Min(1f, n * 0.25f));
        SomManager.TocarEm("mexer", transform.position, 0.7f, 0.8f);
        GameUI.Instance?.Toast($"{n} toner(es) usado(s) para a recicladora: +{UIFactory.Dinheiro(valor)} e descarte correto.", UIFactory.Sucesso, 4f);
    }
}
