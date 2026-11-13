using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Menu principal montado por código com a mesma identidade visual do jogo.
public class MenuPrincipal : MonoBehaviour
{
    const string CenaJogo = "SampleScene";

    void Start()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Construir();
    }

    void Construir()
    {
        var canvas = UIFactory.CriarCanvas("Canvas_Menu");
        var raiz = (RectTransform)canvas.transform;

        var fundo = UIFactory.Painel(raiz, "Fundo", new Color(0.05f, 0.07f, 0.1f, 0.78f));
        UIFactory.Esticar(fundo);
        var grupo = fundo.gameObject.AddComponent<CanvasGroup>();

        var coluna = UIFactory.Vazio(fundo, "Coluna");
        UIFactory.Ancorar(coluna, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900, 0));
        coluna.pivot = new Vector2(0.5f, 0.5f);
        UIFactory.Vertical(coluna, 18, 0, TextAnchor.UpperCenter);
        coluna.GetComponent<VerticalLayoutGroup>().childControlWidth = false;
        UIFactory.AjustarAoConteudo(coluna);

        var titulo = UIFactory.Texto(coluna, "TI Service Simulator", 84, UIFactory.CorTexto, FontStyles.Bold, TextAlignmentOptions.Center);
        titulo.rectTransform.sizeDelta = new Vector2(900, 110);
        var sub = UIFactory.Texto(coluna, "Do técnico solo à maior empresa de TI da cidade", 28, UIFactory.CorSuave,
            FontStyles.Normal, TextAlignmentOptions.Center);
        sub.rectTransform.sizeDelta = new Vector2(900, 50);

        var espaco = UIFactory.Vazio(coluna, "Espaco");
        espaco.gameObject.AddComponent<LayoutElement>().preferredHeight = 30;

        if (SaveManager.ExisteSave)
        {
            UIFactory.Botao(coluna, "Continuar", () =>
            {
                SaveManager.CarregarAoIniciar = true;
                SceneManager.LoadScene(CenaJogo);
            }, UIFactory.Destaque, 380, 64, 28);
        }

        UIFactory.Botao(coluna, SaveManager.ExisteSave ? "Novo jogo" : "Jogar", () =>
        {
            SaveManager.ApagarSave();
            SaveManager.CarregarAoIniciar = false;
            SceneManager.LoadScene(CenaJogo);
        }, SaveManager.ExisteSave ? UIFactory.CartaoClaro : UIFactory.Destaque, 380, 64, 28);

        // Configurações: a coluna do menu some e a janela aparece no lugar
        var config = UIFactory.Painel(fundo, "Configuracoes", UIFactory.Fundo);
        UIFactory.Ancorar(config, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(820, 0));
        config.pivot = new Vector2(0.5f, 0.5f);
        UIFactory.Vertical(config, 10, 32);
        UIFactory.AjustarAoConteudo(config);
        JanelaConfiguracoes.Montar(config, () => { config.gameObject.SetActive(false); coluna.gameObject.SetActive(true); });
        config.gameObject.SetActive(false);

        UIFactory.Botao(coluna, "Configurações", () => { coluna.gameObject.SetActive(false); config.gameObject.SetActive(true); },
            UIFactory.CartaoClaro, 380, 64, 28);
        UIFactory.Botao(coluna, "Sair", Application.Quit, UIFactory.CartaoClaro, 380, 64, 28);

        var dica = UIFactory.Texto(fundo, "WASD andar  ·  Mouse olhar  ·  E interagir  ·  Tab tablet  ·  Esc pausa",
            20, UIFactory.CorSuave, FontStyles.Normal, TextAlignmentOptions.Center);
        UIFactory.Ancorar(dica.rectTransform, new Vector2(0.5f, 0f), new Vector2(0, 40), new Vector2(1400, 40));
        dica.rectTransform.pivot = new Vector2(0.5f, 0f);

        grupo.alpha = 0f;
        Tween.Custom(startValue: 0f, endValue: 1f, duration: 0.6f,
            onValueChange: v => { if (grupo != null) grupo.alpha = v; }, ease: Ease.OutCubic);
        Tween.Custom(startValue: -40f, endValue: 0f, duration: 0.7f,
            onValueChange: v => { if (coluna != null) coluna.anchoredPosition = new Vector2(0, v); }, ease: Ease.OutCubic);
    }
}
