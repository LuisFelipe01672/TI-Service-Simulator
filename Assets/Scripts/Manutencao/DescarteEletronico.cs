using UnityEngine;

// Caixa de logística reversa no escritório (Fase 2): os toners usados que você trouxe dos clientes vão aqui.
// A recicladora paga um valor pequeno por cartucho vazio e o descarte correto conta um pouco para a reputação.
public class DescarteEletronico : Interactable
{
    public float valorPorCartucho = 8f;
    public float valorPorPeca = 2f;   // peças velhas dos consertos (Etapa 6)
    static readonly Vector3 Tam = new Vector3(0.5f, 0.55f, 0.4f);

    public override string TextoPrompt
    {
        get
        {
            var mao = UsadosFisicos.NaMao;
            if (mao != null)
                return mao.condicao == CondicaoUsado.Sucata && mao.testado ? $"[E] Descartar a sucata: {mao.nome} (lixo eletrônico)"
                                                                           : $"Lixo eletrônico: {mao.nome} {(mao.testado ? "ainda tem valor" : "não foi testado")}";
            int n = Contar(Item.TonerUsado), p = Contar(Item.PecaVelha);
            if (n + p == 0) return "Caixa de logística reversa (nada para descartar)";
            var partes = new System.Collections.Generic.List<string>();
            if (n > 0) partes.Add($"{n} toner(es) usado(s)");
            if (p > 0) partes.Add($"{p} peça(s) velha(s)");
            return $"[E] Descartar {string.Join(" e ", partes)} (logística reversa)";
        }
    }

    void Awake() => distanciaInteracao = 1.6f;

    void Start()
    {
        EncomendaFisica.CriarCaixa(transform, Tam);
        EncomendaFisica.Etiqueta(transform, Tam, "LOGÍSTICA REVERSA\ntoner e eletrônicos", true);
    }

    static int Contar(Item i) => Mochila.Instance != null ? Mochila.Instance.Quantidade(i) + Mochila.Instance.NoEstoque(i) : 0;

    public override void Interagir()
    {
        // Etapa 6: sucata de equipamento usado na mão
        if (UsadosFisicos.NaMao != null && UsadosFisicos.Instance != null)
        {
            var mao = UsadosFisicos.NaMao;
            string r = UsadosFisicos.Instance.DescartarNaMao();
            bool foi = !EquipamentosUsados.Estoque.Contains(mao);
            if (foi) SomManager.TocarEm("mexer", transform.position, 0.9f, 0.6f);
            GameUI.Instance?.Toast(r, foi ? UIFactory.Sucesso : UIFactory.Alerta, 4f);
            return;
        }
        var m = Mochila.Instance;
        if (m == null) return;
        int n = Retirar(m, Item.TonerUsado), p = Retirar(m, Item.PecaVelha);
        if (n + p <= 0) return;
        float valor = n * valorPorCartucho + p * valorPorPeca;
        GameEconomy.Instance?.ReceberPagamento(valor, LivroCaixa.OutrasReceitas);
        ReputationManager.Instance?.AlterarReputacao(Mathf.Min(1f, (n + p) * 0.25f));
        SomManager.TocarEm("mexer", transform.position, 0.7f, 0.8f);
        string oQue = n > 0 && p > 0 ? $"{n} toner(es) e {p} peça(s) velha(s)" : n > 0 ? $"{n} toner(es) usado(s)" : $"{p} peça(s) velha(s)";
        GameUI.Instance?.Toast($"{oQue} para a recicladora: +{UIFactory.Dinheiro(valor)} e descarte correto.", UIFactory.Sucesso, 4f);
    }

    static int Retirar(Mochila m, Item item)
    {
        int naMochila = m.Quantidade(item);
        int n = naMochila + m.TirarDoEstoque(item);
        for (int i = 0; i < naMochila; i++) m.Usar(item);
        return n;
    }
}
