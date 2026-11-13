using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Janela de configurações (volume, mouse, tela). Usada no menu principal e na pausa do jogo.
public static class JanelaConfiguracoes
{
    // Monta o conteúdo dentro de 'janela' (que já deve ter um VerticalLayoutGroup).
    // 'aoVoltar' é o botão de baixo (volta para o menu/pausa).
    public static void Montar(RectTransform janela, Action aoVoltar)
    {
        UIFactory.Texto(janela, "Configurações", 40, UIFactory.CorTexto, FontStyles.Bold, TextAlignmentOptions.Center);

        Secao(janela, "SOM");
        LinhaBarra(janela, "Volume geral", 0f, 1f, Configuracoes.VolumeGeral, Configuracoes.DefinirVolumeGeral, v => $"{v * 100:0}%");
        LinhaBarra(janela, "Efeitos", 0f, 1f, Configuracoes.VolumeEfeitos, v => { Configuracoes.DefinirVolumeEfeitos(v); SomManager.Tocar("clique"); }, v => $"{v * 100:0}%");
        LinhaBarra(janela, "Ambiente (rua, escritório)", 0f, 1f, Configuracoes.VolumeAmbiente, Configuracoes.DefinirVolumeAmbiente, v => $"{v * 100:0}%");

        Secao(janela, "CONTROLES");
        LinhaBarra(janela, "Sensibilidade do mouse", Configuracoes.SensMin, Configuracoes.SensMax, Configuracoes.Sensibilidade, Configuracoes.DefinirSensibilidade, v => $"{v:0.0}");
        LinhaLiga(janela, "Inverter o eixo Y", () => Configuracoes.InverterY, Configuracoes.DefinirInverterY);

        Secao(janela, "TELA");
        LinhaLiga(janela, "Tela cheia", () => Configuracoes.TelaCheia, Configuracoes.DefinirTelaCheia);
        LinhaLiga(janela, "Contador de FPS e console de debug", () => Configuracoes.Debug, Configuracoes.DefinirDebug);

        var espaco = UIFactory.Vazio(janela, "Espaco");
        espaco.gameObject.AddComponent<LayoutElement>().preferredHeight = 6;
        UIFactory.Botao(janela, "Voltar", aoVoltar, UIFactory.Destaque, 300, 54);
    }

    static void Secao(RectTransform pai, string titulo) =>
        UIFactory.Texto(pai, titulo, 17, UIFactory.CorSuave, FontStyles.Bold);

    static RectTransform Linha(RectTransform pai, string rotulo)
    {
        var linha = UIFactory.Painel(pai, "Linha", UIFactory.Cartao);
        var h = UIFactory.Horizontal(linha, 16, 0);
        h.padding = new RectOffset(18, 18, 8, 8);
        var t = UIFactory.Texto(linha, rotulo, 21, UIFactory.CorTexto);
        t.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
        return linha;
    }

    static void LinhaBarra(RectTransform pai, string rotulo, float min, float max, float valor, Action<float> definir, Func<float, string> formato)
    {
        var linha = Linha(pai, rotulo);
        TextMeshProUGUI txt = null;
        UIFactory.Barra(linha, min, max, valor, v => { definir(v); if (txt != null) txt.text = formato(v); }, 320);
        txt = UIFactory.Texto(linha, formato(valor), 20, UIFactory.CorSuave, FontStyles.Bold, TextAlignmentOptions.Right);
        txt.gameObject.AddComponent<LayoutElement>().preferredWidth = 70;
    }

    static void LinhaLiga(RectTransform pai, string rotulo, Func<bool> ler, Action<bool> definir)
    {
        var linha = Linha(pai, rotulo);
        Button b = null;
        b = UIFactory.Botao(linha, ler() ? "Ligado" : "Desligado", () =>
        {
            definir(!ler());
            b.GetComponentInChildren<TextMeshProUGUI>().text = ler() ? "Ligado" : "Desligado";
            b.GetComponent<Image>().color = ler() ? UIFactory.Sucesso : UIFactory.CartaoClaro;
        }, ler() ? UIFactory.Sucesso : UIFactory.CartaoClaro, 160, 44, 19);
    }
}
