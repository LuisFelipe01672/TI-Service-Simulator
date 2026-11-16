using UnityEngine;

// Estado do notebook do cliente (campos nb*): bateria, carregador, teclado, dobradiça e ventilação.
// O sistema dele (senha, e-mail, vírus...) usa os mesmos campos de software do computador (EstadoEquipamentos.Software.cs).
public partial class EstadoEquipamentos
{
    // ---------- Notebook ----------
    public bool nbLigado = true;
    public bool nbNaTomada = true;            // carregador ligado no notebook e na tomada
    public float nbCarga = 0.8f;              // carga da bateria (0 a 1)
    public bool nbBateriaEstufada;            // a bateria inchou: base empenada, touchpad levantado
    public bool nbTampaFundoAberta;           // tampa de baixo fora (10 parafusos): notebook de cabeça para baixo
    public bool nbBateriaFora;                // bateria desconectada e retirada
    public bool nbBateriaNaMao;               // a bateria que saiu (boa) espera para voltar
    public bool nbCarregadorRuim;             // cabo do carregador rompido perto do plugue: não carrega
    public bool nbTeclaFalhando;              // migalha debaixo da tecla A: a letra não sai
    public bool nbTeclaSemCapa;               // capinha da tecla tirada (o visual mostra o buraco)
    public bool nbDobradicaQuebrada;          // dobradiça da tela quebrou a bucha: a tela tomba
    public bool nbMolduraAberta;              // moldura da tela solta para chegar na dobradiça
    public float nbPoeira = 0.1f;             // ventoinha e radiador; acima de 0,7 superaquece
    public int nbTrocasBateria, nbLimpezas, nbTrocasDobradica, nbTestesTeclado, nbMedicoesCarregador;
    public string nbUltimoTesteTeclado = "";
    public float nbSegundosQuente;
    public const float SegundosNotebookDesligarPorCalor = 30f;

    public bool NbSuperaquecendo => nbPoeira > 0.7f;
    public bool NbCarregando => nbNaTomada && !nbCarregadorRuim && !nbBateriaFora;
    public bool NbAberto => nbTampaFundoAberta || nbMolduraAberta;
    public bool NbHardwareOk => !nbBateriaEstufada && !nbCarregadorRuim && !nbTeclaFalhando && !nbTeclaSemCapa &&
                                !nbDobradicaQuebrada && !NbSuperaquecendo && !NbAberto && !nbBateriaFora;

    // O que o técnico digita no bloco de notas para testar o teclado
    public string TestarTeclado()
    {
        nbTestesTeclado++;
        const string frase = "a casa da fazenda tem uma janela azul";
        nbUltimoTesteTeclado = nbTeclaFalhando ? frase.Replace("a", "") : frase;
        return nbTeclaFalhando
            ? $"Você digitou \"{frase}\" e saiu: \"{nbUltimoTesteTeclado}\". <color=#F5B83F>A tecla A não responde</color> (às vezes sai só apertando com força)."
            : $"Você digitou \"{frase}\" e saiu certinho. Todas as teclas respondem.";
    }

    // Luz do carregador e a saída medida com o multímetro
    public string LedCarregador => !nbNaTomada ? "apagada (fora da tomada)" : nbCarregadorRuim ? "apagada" : "verde";
    public string MedirCarregador()
    {
        nbMedicoesCarregador++;
        if (!nbNaTomada) return "Multímetro na ponta do carregador: 0,0 V. (Ele está fora da tomada.)";
        return nbCarregadorRuim
            ? "Multímetro na ponta do carregador: 0,0 V. Mexendo no cabo perto do plugue, o número pula para 19 V e volta a zero: <color=#F5B83F>fio rompido por dentro</color>."
            : "Multímetro na ponta do carregador: 19,5 V firmes. O carregador está bom.";
    }

    public string IconeBateria =>
        nbBateriaFora ? "sem bateria" :
        NbCarregando ? $"{Mathf.RoundToInt(nbCarga * 100)}% (carregando)" :
        nbNaTomada ? $"{Mathf.RoundToInt(nbCarga * 100)}% (conectado, não está carregando)" : $"{Mathf.RoundToInt(nbCarga * 100)}% (na bateria)";
}
