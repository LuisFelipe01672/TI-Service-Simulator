using System.Collections;
using TMPro;
using UnityEngine;

// Bomba do posto de combustível (Etapa 9C), na Rua dos Jacarandás, 340. Pare o carro ao lado da bomba, desça e
// mire a bomba: E completa o tanque (o frentista coloca o bico, o visor conta litros e reais, a bomba zune).
// O valor sai do caixa da empresa no livro-caixa ("Combustível e deslocamento"). Sem carro por perto (pane seca),
// E compra um galão de 5 L para levar a pé até o carro.
public class PostoCombustivel : Interactable
{
    public const float PrecoLitro = 6.19f;     // gasolina comum (valor fictício, perto da média)
    public const float LitrosGalao = 5f;
    public const float DistanciaBomba = 3.6f;  // carro a até 3,6 m da bomba

    public TextMeshPro visor;
    public Transform bico;
    bool abastecendo;
    AudioSource som;

    protected override bool PrecisaMirar => true;

    CarroTecnico CarroPerto
    {
        get
        {
            var c = CarroTecnico.Instance;
            if (c == null || CarroTecnico.NoCarro) return null;
            var a = c.transform.position; var b = Bairro.VagaPosto;
            return Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z)) < DistanciaBomba ? c : null;
        }
    }

    public override string TextoPrompt
    {
        get
        {
            if (abastecendo || CarroTecnico.NoCarro) return "";
            var c = CarroPerto;
            if (c != null)
            {
                float falta = c.FaltaParaEncher;
                if (falta < 0.5f) return "Tanque cheio";
                return $"[E] Completar o tanque: {falta:0.0} L · {UIFactory.Dinheiro(Mathf.Round(falta * PrecoLitro * 100f) / 100f)}  <size=80%><color=#9AA6B8>(R$ {PrecoLitro:0.00}/L)</color></size>";
            }
            if (CargaFisica.MaosOcupadas) return "Bomba de gasolina (mãos ocupadas)";
            return $"[E] Comprar um galão com {LitrosGalao:0} L ({UIFactory.Dinheiro(LitrosGalao * PrecoLitro + 25f)} com o galão)  <size=80%><color=#9AA6B8>Pare o carro aqui do lado para abastecer</color></size>";
        }
    }

    public override void Interagir()
    {
        if (abastecendo || CarroTecnico.NoCarro) return;
        var c = CarroPerto;
        if (c != null) { if (c.FaltaParaEncher >= 0.5f) StartCoroutine(Abastecer(c)); return; }
        if (CargaFisica.MaosOcupadas) return;
        float preco = LitrosGalao * PrecoLitro + 25f;   // o galão de plástico também se paga
        if (!Pagar(preco, "Galão de 5 L")) return;
        var g = CargaFisica.CriarGalao();
        g.transform.position = transform.position + transform.forward * 0.6f;
        CargaFisica.Pegar(g);
        Debug.Log($"[Posto] Galão de {LitrosGalao} L comprado por {preco:0.00}.");
    }

    bool Pagar(float valor, string oque)
    {
        var eco = GameEconomy.Instance;
        if (eco != null && eco.saldo < valor)
        {
            GameUI.Instance?.Toast($"Saldo insuficiente para {oque} ({UIFactory.Dinheiro(valor)}).", UIFactory.Perigo, 3.5f);
            return false;
        }
        eco?.Gastar(valor, LivroCaixa.Combustivel);
        return true;
    }

    IEnumerator Abastecer(CarroTecnico c)
    {
        var eco = GameEconomy.Instance;
        float litros = c.FaltaParaEncher;
        if (eco != null) litros = Mathf.Min(litros, Mathf.Floor(eco.saldo / PrecoLitro));
        if (litros < 1f) { GameUI.Instance?.Toast("Saldo insuficiente para abastecer.", UIFactory.Perigo, 3f); yield break; }
        abastecendo = true;
        if (som == null)
        {
            som = gameObject.AddComponent<AudioSource>();
            som.clip = SomManager.ClipDe("bomba");
            som.loop = true; som.spatialBlend = 1f; som.minDistance = 1.5f; som.maxDistance = 15f;
        }
        som.volume = 0.7f * Configuracoes.VolumeEfeitos;
        som.Play();
        Vector3 bicoPos = bico != null ? bico.localPosition : Vector3.zero;
        if (bico != null) PrimeTween.Tween.LocalPosition(bico, bicoPos + new Vector3(0f, -0.1f, 0.25f), 0.4f);
        // A bomba enche a uns 0,4 L por segundo de jogo acelerado (não demora de verdade)
        float dur = Mathf.Clamp(litros * 0.12f, 1.5f, 6f), t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float l = litros * Mathf.Clamp01(t / dur);
            if (visor != null) visor.text = $"R$ {l * PrecoLitro:0.00}\n{l:0.00} L";
            yield return null;
        }
        som.Stop();
        if (bico != null) PrimeTween.Tween.LocalPosition(bico, bicoPos, 0.4f);
        float valor = Mathf.Round(litros * PrecoLitro * 100f) / 100f;
        eco?.Gastar(valor, LivroCaixa.Combustivel);
        c.Abastecer(litros);
        SomManager.TocarEm("clique", transform.position, 0.6f);
        GameUI.Instance?.Toast($"Abastecido: <b>{litros:0.0} L</b> por {UIFactory.Dinheiro(valor)} (no livro-caixa: combustível). Tanque com {c.litros:0} L.", UIFactory.Sucesso, 4.5f);
        Debug.Log($"[Posto] Abasteceu {litros:0.0} L por R$ {valor:0.00}. Tanque {c.litros:0.0} L.");
        abastecendo = false;
    }
}
