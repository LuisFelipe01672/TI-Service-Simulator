// Etapa 7: o que o técnico faz com as mãos no notebook do cliente.
// Peças: tela, teclado, energia, fundo (tampa de baixo, bateria e ventoinha), carregador, saida_ar, dobradica.
public static partial class AcoesFisicas
{
    internal static void Notebook(EstadoEquipamentos e, ListaAcoes l)
    {
        // ---------- Tela ----------
        l.peca = "tela";
        if (e.nbLigado && !e.nbTampaFundoAberta)
            l.Add(new AcaoFisica("Sentar e usar o notebook", () => "") { abreComputador = true, observacao = true });
        l.Add(new AcaoFisica("Abrir e fechar a tela devagar", () =>
            e.nbDobradicaQuebrada
                ? "A tela não para em pé: tomba para trás sozinha. No canto esquerdo a carcaça está trincada e a dobradiça range solta."
                : "A tela abre firme e para em qualquer ângulo. As dobradiças estão boas.") { observacao = true });

        // ---------- Teclado e botão ----------
        l.peca = "teclado";
        l.Add(new AcaoFisica("Olhar o teclado e o touchpad de perto", () =>
            e.nbBateriaEstufada ? "<color=#F5B83F>O touchpad está estufado</color> e as teclas do meio sobem: algo empurra o teclado por baixo."
            : e.nbTeclaFalhando ? "Entre as teclas tem farelo de biscoito. A tecla A afunda mole, sem o clique das outras."
            : "Teclado limpo, todas as teclas com o mesmo clique.") { observacao = true });
        if (e.nbLigado && !e.swContaBloqueada)
            l.Add(new AcaoFisica("Testar o teclado digitando no bloco de notas", () => e.TestarTeclado(), 2f) { observacao = true });
        if (!e.nbTeclaSemCapa)
            l.Add(new AcaoFisica("Tirar a capinha da tecla A (com a unha, de baixo para cima)", () =>
            {
                e.nbTeclaSemCapa = true;
                return e.nbTeclaFalhando ? "A capinha saiu: debaixo dela tem farelo e um pedacinho de biscoito travando o mecanismo."
                                         : "A capinha saiu inteira. O mecanismo está limpo.";
            }, 1.5f));
        else
        {
            if (e.nbTeclaFalhando)
                l.Add(new AcaoFisica("Soprar com ar comprimido e limpar com pincel", () =>
                {
                    e.nbTeclaFalhando = false;
                    Sujeira.Espalhar(MM != null ? MM.PosicaoEquipamentoAtual : UnityEngine.Vector3.zero, 1, "farelo");
                    return "Saiu uma chuva de farelo. O mecanismo da tecla voltou a mexer solto.";
                }, 2f, Item.ArComprimido));
            l.Add(new AcaoFisica("Encaixar a capinha da tecla de volta (clique)", () =>
            {
                e.nbTeclaSemCapa = false;
                MM?.Verificar();
                return "Clique! A capinha encaixou nas duas presilhas.";
            }, 1f));
        }

        l.peca = "energia";
        l.Add(new AcaoFisica(e.nbLigado ? "Desligar o notebook (desligar pelo sistema)" : "Ligar o notebook", () =>
        {
            if (e.nbLigado)
            {
                if (!e.clienteSalvouTrabalho) MM?.RegistrarErro("Desligou o notebook sem o cliente salvar: ele perdeu o trabalho!", 3f);
                e.nbLigado = false;
                return "O notebook desligou.";
            }
            if (e.nbTampaFundoAberta) return "Feche a tampa de baixo antes de ligar.";
            if (e.nbBateriaFora && !e.nbNaTomada) return "Nada acontece: está sem bateria e fora da tomada.";
            if (!e.NbCarregando && e.nbCarga < 0.03f) return "Nada acontece: a bateria está zerada e o carregador não carrega.";
            e.nbLigado = true;
            e.nbSegundosQuente = 0f;
            e.ReiniciarPc();
            if (e.NbHardwareOk) MM?.Verificar();
            return e.NbSuperaquecendo ? "Ligou, mas a ventoinha já berra e sai ar muito quente pela lateral."
                 : e.nbBateriaEstufada ? "Ligou. O touchpad continua estufado e às vezes clica sozinho."
                 : "O notebook ligou e o TI OS iniciou.";
        }));

        // ---------- Carregador ----------
        l.peca = "carregador";
        l.Add(new AcaoFisica("Olhar o carregador e o cabo", () =>
            $"Luz do carregador: {e.LedCarregador}. Ícone da bateria: {e.IconeBateria}." +
            (e.nbCarregadorRuim ? " <color=#F5B83F>O cabo está remendado com fita isolante perto do plugue, todo dobrado.</color>" : " O cabo está inteiro.")) { observacao = true });
        l.Add(new AcaoFisica("Medir a saída do carregador com o multímetro", () => e.MedirCarregador(), 2f, Item.Multimetro) { observacao = true });
        l.Add(new AcaoFisica(e.nbNaTomada ? "Tirar o carregador da tomada" : "Ligar o carregador na tomada", () =>
        {
            e.nbNaTomada = !e.nbNaTomada;
            if (e.nbNaTomada) MM?.Verificar();
            return e.nbNaTomada ? (e.nbCarregadorRuim ? "Carregador na tomada. A luz dele continua apagada." : "Carregador na tomada: a luz acendeu verde.") : "Carregador fora da tomada.";
        }));
        if (e.nbCarregadorRuim)
            l.Add(new AcaoFisica("Trocar pelo carregador universal novo (ponta certa, 19 V)", () =>
            {
                if (Mochila.Instance != null && !Mochila.Instance.Usar(Item.CarregadorNotebook)) return "Você não tem carregador de notebook.";
                e.nbCarregadorRuim = false;
                e.nbNaTomada = true;
                MM?.CobrarPeca(Mochila.Info(Item.CarregadorNotebook).preco, Mochila.Info(Item.CarregadorNotebook).nome);
                MM?.Verificar();
                return "Você escolheu a ponta do tamanho certo, conferiu 19 V no seletor e ligou: luz verde e o ícone mudou para \"carregando\"." + PecaVelhaNaMochila();
            }, 2f, Item.CarregadorNotebook));

        // ---------- Saída de ar ----------
        l.peca = "saida_ar";
        l.Add(new AcaoFisica("Pôr a mão na saída de ar e olhar a grade", () =>
            !e.nbLigado ? (e.nbPoeira > 0.7f ? "Desligado. Pela grade dá para ver uma manta cinza de poeira no radiador." : "Desligado. A grade está limpa.")
            : e.NbSuperaquecendo ? "<color=#F5B83F>Ar muito quente e fraco</color>, ventoinha no máximo. Pela grade aparece uma manta de poeira no radiador."
            : "Sai ar morno, ventoinha baixinha.") { observacao = true });

        // ---------- Fundo: tampa de baixo, bateria e ventoinha ----------
        l.peca = "fundo";
        l.Add(new AcaoFisica("Olhar o notebook de lado, na altura da mesa", () =>
            e.nbBateriaEstufada ? "<color=#F5B83F>A base está empenada</color>: o notebook balança na mesa e a tampa de baixo abriu uma fresta no meio."
                                : "A base está reta e apoia nos quatro pezinhos.") { observacao = true });
        if (!e.nbTampaFundoAberta)
            l.Add(new AcaoFisica("Virar e tirar os 10 parafusos da tampa de baixo", () =>
            {
                if (e.nbLigado) return "Desligue o notebook antes de abrir.";
                if (e.nbNaTomada) return "Tire o carregador da tomada antes de abrir.";
                e.nbTampaFundoAberta = true;
                return "Você fechou a tela, virou o notebook sobre um pano, soltou os 10 parafusos e destravou a tampa com a espátula." +
                       (e.nbBateriaEstufada ? " A tampa quase pulou sozinha: a bateria está inchada como um travesseiro." : "");
            }, 3f));
        else
        {
            l.Add(new AcaoFisica("Olhar por dentro", () =>
            {
                string bat = e.nbBateriaFora ? "O lugar da bateria está vazio." :
                             e.nbBateriaEstufada ? "<color=#F5B83F>A bateria está estufada</color>, as células abauladas empurrando o teclado." : "A bateria está lisa, sem estufar.";
                string po = e.nbPoeira > 0.7f ? " A ventoinha e o radiador estão entupidos de poeira." : e.nbPoeira > 0.3f ? " Tem um pouco de poeira na ventoinha." : " A ventoinha está limpa.";
                return bat + po;
            }) { observacao = true });
            if (!e.nbBateriaFora)
                l.Add(new AcaoFisica("Desconectar e retirar a bateria (sem furar nem dobrar)", () =>
                {
                    e.nbBateriaFora = true;
                    if (e.nbBateriaEstufada)
                    {
                        e.nbBateriaEstufada = false;
                        e.nbTrocasBateria++;
                        return "Você desligou o conector da placa primeiro, soltou os parafusos e levantou a bateria inchada sem apertar. " +
                               "Ela foi para um saco antichama." + PecaVelhaNaMochila();
                    }
                    e.nbBateriaNaMao = true;
                    return "Bateria desconectada e retirada. Ela está boa: dá para pôr de volta.";
                }, 2.5f));
            else if (e.nbBateriaNaMao)
                l.Add(new AcaoFisica("Recolocar a mesma bateria", () =>
                {
                    e.nbBateriaFora = false;
                    e.nbBateriaNaMao = false;
                    return "Bateria parafusada e conector da placa encaixado.";
                }, 2f));
            else
                l.Add(new AcaoFisica("Instalar a bateria nova e ligar o conector", () =>
                {
                    if (Mochila.Instance != null && !Mochila.Instance.Usar(Item.BateriaNotebook)) return "Você não tem bateria de notebook.";
                    e.nbBateriaFora = false;
                    e.nbCarga = 0.4f;
                    e.nbTrocasBateria++;
                    MM?.CobrarPeca(Mochila.Info(Item.BateriaNotebook).preco, Mochila.Info(Item.BateriaNotebook).nome);
                    return "Bateria nova no lugar, parafusada, conector encaixado até o fim.";
                }, 2.5f, Item.BateriaNotebook));
            if (e.nbPoeira > 0.3f)
                l.Add(new AcaoFisica("Limpar a ventoinha e o radiador (pincel e ar comprimido)", () =>
                {
                    e.nbPoeira = 0.05f;
                    e.nbLimpezas++;
                    Sujeira.Espalhar(MM != null ? MM.PosicaoEquipamentoAtual : UnityEngine.Vector3.zero, 2, "poeira");
                    return "Você travou a ventoinha com o dedo, soprou o radiador e tirou a manta de poeira com o pincel.";
                }, 3f, Item.ArComprimido));
            l.Add(new AcaoFisica("Encaixar a tampa de baixo e parafusar", () =>
            {
                if (e.nbBateriaNaMao) return "Recoloque a bateria antes de fechar.";
                e.nbTampaFundoAberta = false;
                return e.nbBateriaFora ? "Tampa fechada... SEM bateria. Só vai ligar na tomada." : "Tampa encaixada nas presilhas e os 10 parafusos de volta. Notebook desvirado.";
            }, 2.5f));
        }

        // ---------- Dobradiça ----------
        l.peca = "dobradica";
        if (!e.nbMolduraAberta)
        {
            l.Add(new AcaoFisica("Olhar a dobradiça de perto", () =>
                e.nbDobradicaQuebrada ? "<color=#F5B83F>A bucha de metal da dobradiça esquerda arrancou da carcaça</color>: a tela está presa só pela direita."
                                      : "As duas dobradiças estão firmes.") { observacao = true });
            l.Add(new AcaoFisica("Soltar a moldura da tela (espátula, devagar)", () =>
            {
                if (e.nbLigado) return "Desligue o notebook antes.";
                e.nbMolduraAberta = true;
                return "Você passou a espátula em volta da tela e soltou a moldura sem quebrar as presilhas. As dobradiças apareceram.";
            }, 2.5f));
        }
        else
        {
            if (e.nbDobradicaQuebrada)
                l.Add(new AcaoFisica("Trocar as dobradiças e colar a bucha (epóxi)", () =>
                {
                    if (Mochila.Instance != null && !Mochila.Instance.Usar(Item.DobradicaNotebook)) return "Você não tem dobradiças de notebook.";
                    e.nbDobradicaQuebrada = false;
                    e.nbTrocasDobradica++;
                    MM?.CobrarPeca(Mochila.Info(Item.DobradicaNotebook).preco + 8f, Mochila.Info(Item.DobradicaNotebook).nome + " + epóxi");
                    MM?.ConsumirTempo(20f);
                    return "Você colou a bucha arrancada com epóxi, esperou firmar e parafusou o par de dobradiças novas." + PecaVelhaNaMochila();
                }, 4f, Item.DobradicaNotebook));
            l.Add(new AcaoFisica("Encaixar a moldura da tela de volta", () =>
            {
                e.nbMolduraAberta = false;
                return "Moldura encaixada em volta da tela, clique por clique.";
            }, 2f));
        }
    }
}
