using System;
using System.Collections.Generic;

// Uma ação que o técnico faz com as mãos num equipamento
public class AcaoFisica
{
    public string rotulo;
    public float segurar;              // segundos segurando (0 = instantâneo)
    public Item? requer;               // ferramenta/peça necessária
    public Func<string> executar;      // devolve o que aconteceu
    public bool abreCrimpagem;         // abre o minigame de crimpar
    public bool abreComputador;        // abre o "Windows" do cliente
    public bool observacao;            // só olhar (não conta como mexer)
    public string peca;                // peça 3D do equipamento onde a ação acontece (mirar + E)
    public bool close = true;          // observação: a câmera faz um close na peça (CloseDePeca)
    public string pistaVisual;         // a pista está na peça (visor, LEDs): no close aparece só esta legenda, o texto vai para o registro

    public AcaoFisica(string rotulo, Func<string> executar, float segurar = 0f, Item? requer = null)
    {
        this.rotulo = rotulo; this.executar = executar; this.segurar = segurar; this.requer = requer;
        peca = AcoesFisicas.PecaAtual;
    }
}

// Lista das ações possíveis em cada equipamento, de acordo com o estado atual dele.
// A janela de manutenção (e, depois, as peças 3D) usa isso.
public static class AcoesFisicas
{
    // Peça das ações criadas a seguir (cada AcaoFisica guarda a peça atual ao ser criada).
    // Ids: Impressora: visor, traseira, tampa, cartucho, botao, energia
    //      Computador: tela, traseira, energia   ·   Roteador: luzes, etiqueta, fonte, reset
    public static string PecaAtual;

    public static List<AcaoFisica> Para(TipoEquipamento tipo, EstadoEquipamentos e)
    {
        var lista = new List<AcaoFisica>();
        PecaAtual = null;
        switch (tipo)
        {
            case TipoEquipamento.Impressora: Impressora(e, lista); break;
            case TipoEquipamento.Computador: Computador(e, lista); break;
            default: Roteador(e, lista); break;
        }
        return lista;
    }

    static MissionManager MM => MissionManager.Instance;

    // ---------- Impressora ----------

    static void Impressora(EstadoEquipamentos e, List<AcaoFisica> l)
    {
        PecaAtual = "visor";
        l.Add(new AcaoFisica("Olhar o visor", () => $"Visor: \"{e.VisorImpressora()}\"") { observacao = true, pistaVisual = "Leia o visor da impressora." });
        var locacao = ContratosManager.Instance != null ? ContratosManager.Instance.DoChamado(MM != null ? MM.chamadoAtivo : null) : null;
        if (e.impLigada && locacao != null)
            l.Add(new AcaoFisica("Ler o contador de páginas (menu Informações)", () =>
            {
                e.visorTemporario = $"Contador {locacao.contadorTotal:N0}";
                e.visorTemporarioAte = UnityEngine.Time.time + 6f;
                return ContratosManager.Instance.LerContadorNoVisor(locacao);
            }, 1.5f) { observacao = true });
        if (e.impLigada)
        {
            l.Add(new AcaoFisica("Imprimir o relatório de configuração de rede (menu do painel)", () =>
            {
                if (e.impTampaAberta || e.impTampaMalFechada || e.impSemPapel || e.impPapelAtolado || !e.impCartuchoPresente)
                    return "A impressora não consegue imprimir agora (veja o visor).";
                e.impRelatorioVisto = true;
                e.paginasImpressas++;
                e.ultimaPagina = ResultadoPagina.Relatorio;
                string ip = e.impIpMudou ? EstadoEquipamentos.IpNovoImpressora : e.ipImpressora;
                return $"Saiu o relatório: <b>IP {ip}</b> (obtido por DHCP), máscara 255.255.255.0, gateway 192.168.0.1." +
                       (e.impIpMudou ? $" <color=#F5B83F>O computador está configurado para imprimir em {e.ipImpressora}!</color>" : "");
            }, 2f) { observacao = true });
            if (e.impIpMudou && e.impRelatorioVisto)
                l.Add(new AcaoFisica($"Fixar o IP {e.ipImpressora} no painel (Rede > IP manual)", () =>
                {
                    e.impIpMudou = false;
                    return $"IP fixo {e.ipImpressora} gravado. A impressora reiniciou a rede e voltou no endereço que o computador conhece.";
                }, 3f));
        }
        PecaAtual = "traseira";
        l.Add(new AcaoFisica("Olhar atrás da impressora", () => DescreverCabo(e.impCabo, e.impLigada, "impressora") +
            (e.impCaboSolto ? " <color=#F5B83F>O cabo passa solto pelo chão, bem no caminho do armário que a limpeza arrasta.</color>" : "")) { observacao = true });
        // Causa raiz do cabo: sem prender, a próxima faxina solta de novo
        if (e.impCaboSolto && e.impCabo.conectado)
            l.Add(new AcaoFisica("Passar o cabo rente à parede e prender com abraçadeiras", () =>
            {
                e.impCaboSolto = false;
                MM?.CobrarPeca(5f, "Abraçadeiras e fixadores");
                return "Você tirou o cabo do caminho do armário, passou rente ao rodapé e prendeu com abraçadeiras a cada palmo.";
            }, 3f));
        AcoesCabo(e.impCabo, "da impressora", l);

        // Atolamento: a folha amassada sai pela tampa de trás
        if (e.impPapelAtolado)
        {
            PecaAtual = "traseira";
            l.Add(new AcaoFisica("Abrir a tampa traseira e puxar o papel atolado (devagar)", () =>
            {
                e.impPapelAtolado = false;
                Sujeira.Espalhar(MM != null ? MM.PosicaoEquipamentoAtual : UnityEngine.Vector3.zero, 1, "folha amassada");
                return "Você abriu a tampa de trás e puxou devagar, com as duas mãos, uma folha toda amassada. Saiu inteira.";
            }, 2.5f));
        }

        // Gaveta de papel
        PecaAtual = "gaveta";
        if (!e.impGavetaAberta)
            l.Add(new AcaoFisica("Abrir a gaveta de papel", () =>
            {
                e.impGavetaAberta = true;
                return e.impSemPapel ? "Gaveta aberta: está VAZIA, sem nenhuma folha." :
                       e.impPapelUmido ? "Gaveta aberta: tem papel A4, mas as folhas estão <color=#F5B83F>onduladas e frias, parecem úmidas</color>." :
                       "Gaveta aberta: tem papel A4 até a marca.";
            }));
        else
        {
            if (e.impSemPapel)
                l.Add(new AcaoFisica("Colocar papel (meia resma do cliente) e ajustar a guia", () =>
                {
                    e.impSemPapel = false;
                    return "Você ventilou as folhas, colocou na gaveta e encostou a guia no papel.";
                }, 2f));
            if (e.impPapelUmido && !e.impSemPapel)
                l.Add(new AcaoFisica("Trocar a resma úmida por papel seco (pacote lacrado do cliente)", () =>
                {
                    e.impPapelUmido = false;
                    return "Você tirou as folhas úmidas, abriu um pacote lacrado, ventilou o papel novo e colocou na gaveta. " +
                           "Avisou o cliente para guardar as resmas longe da janela e do ar-condicionado.";
                }, 2.5f));
            l.Add(new AcaoFisica("Fechar a gaveta (até o fim)", () => { e.impGavetaAberta = false; return "Gaveta fechada, encaixou certinho."; }));
        }

        // Tampa e toner: a parte de cima (scanner) levanta para acessar o cartucho
        PecaAtual = "tampa";
        if (e.impTampaMalFechada && !e.impTampaAberta)
            l.Add(new AcaoFisica("Pressionar a tampa dos dois lados até travar", () =>
            {
                e.impTampaMalFechada = false;
                return "Clique! Um dos lados não tinha travado. Agora fechou de verdade.";
            }, 1f));
        if (!e.impTampaAberta)
            l.Add(new AcaoFisica("Levantar o scanner (abrir a tampa)", () => { e.impTampaAberta = true; return "Você levantou o scanner. Dá para ver o cartucho de toner."; }));
        else
        {
            PecaAtual = "cartucho";
            if (e.impCartuchoPresente)
                l.Add(new AcaoFisica("Retirar o cartucho", () =>
                {
                    e.impCartuchoPresente = false;
                    e.cartuchoNaMao = true;
                    e.cartuchoNaMaoUsado = true;
                    e.cartuchoNaMaoToner = e.impToner;
                    e.cartuchoNaMaoComLacre = e.impCartuchoComLacre;
                    e.cartuchoNaMaoChacoalhado = false;
                    return "Você puxou o cartucho pela alça. Ele está na sua mão.";
                }, 1f));

            if (e.cartuchoNaMao)
            {
                l.Add(new AcaoFisica("Olhar o cartucho na mão", () =>
                {
                    string peso = e.cartuchoNaMaoToner < 0.1f ? "levinho: o toner ACABOU" : e.cartuchoNaMaoToner < 0.3f ? "meio leve: pouco toner" : "pesado: tem toner";
                    return $"Cartucho {(e.cartuchoNaMaoUsado ? "que estava na impressora" : "novo")}: {peso}." +
                           (e.cartuchoNaMaoComLacre ? " <color=#F5B83F>O lacre (fita) ainda está nele!</color>" : " Sem lacre.") +
                           (e.impCilindroRiscado && e.cartuchoNaMaoUsado ? " O cilindro verde tem um risco fino de ponta a ponta." : "");
                }) { observacao = true, close = false });
                if (!e.cartuchoNaMaoChacoalhado)
                    l.Add(new AcaoFisica("Chacoalhar o cartucho (de lado a lado)", () =>
                    {
                        e.cartuchoNaMaoChacoalhado = true;
                        // toner no fim: chacoalhar espalha o resto e rende mais algumas páginas
                        if (e.cartuchoNaMaoUsado && e.cartuchoNaMaoToner < 0.08f) e.cartuchoNaMaoToner = 0.08f;   // melhora um pouco, mas não resolve
                        return "Você chacoalhou de lado a lado para espalhar o toner por igual.";
                    }, 1.5f));
                if (e.cartuchoNaMaoComLacre)
                    l.Add(new AcaoFisica("Puxar o lacre (fita de ~70 cm)", () => { e.cartuchoNaMaoComLacre = false; return "Você puxou o lacre inteiro, em linha reta."; }, 2f));
                l.Add(new AcaoFisica(e.cartuchoNaMaoUsado ? "Recolocar o mesmo cartucho" : "Encaixar o cartucho novo", () =>
                {
                    e.cartuchoNaMao = false;
                    e.impCartuchoPresente = true;
                    e.impCartuchoComLacre = e.cartuchoNaMaoComLacre;
                    e.impToner = e.cartuchoNaMaoUsado ? e.cartuchoNaMaoToner : e.cartuchoNaMaoChacoalhado ? 1f : 0.85f;
                    return "Cartucho encaixado até o fim (clique).";
                }, 1f));
                if (e.cartuchoNaMaoUsado)
                    l.Add(new AcaoFisica("Guardar este cartucho na mochila (descarte)", () =>
                    {
                        e.cartuchoNaMao = false;
                        if (Mochila.Instance != null && !Mochila.Instance.Cabe(Item.TonerUsado, 1)) return "Não cabe na mochila (está cheia). Recoloque o cartucho ou libere espaço.";
                        Mochila.Instance?.Adicionar(Item.TonerUsado, 1);
                        return "Cartucho usado guardado num saco na mochila (logística reversa).";
                    }));
            }
            else if (!e.impCartuchoPresente && e.tonerDoCliente)
                l.Add(new AcaoFisica("Pegar o toner novo que o cliente deixou no armário", () =>
                {
                    e.tonerDoCliente = false;
                    e.cartuchoNaMao = true;
                    e.cartuchoNaMaoUsado = false;
                    e.cartuchoNaMaoToner = 1f;
                    e.cartuchoNaMaoComLacre = true;
                    e.cartuchoNaMaoChacoalhado = false;
                    return "Você pegou a caixa do toner que o cliente comprou e tirou o cartucho novo da embalagem (o cliente já pagou por ele).";
                }, 1f));
            else if (!e.impCartuchoPresente)
                l.Add(new AcaoFisica("Pegar um toner novo da mochila", () =>
                {
                    if (!Mochila.Instance.Usar(Item.Toner)) return "Você não tem toner.";
                    e.cartuchoNaMao = true;
                    e.cartuchoNaMaoUsado = false;
                    e.cartuchoNaMaoToner = 1f;
                    e.cartuchoNaMaoComLacre = true;
                    e.cartuchoNaMaoChacoalhado = false;
                    MM?.CobrarPeca(Mochila.Info(Item.Toner).preco, Mochila.Info(Item.Toner).nome);
                    return "Você tirou o cartucho novo da embalagem.";
                }, 0f, Item.Toner));

            // Cilindro riscado: troca a unidade de imagem (com o cartucho fora)
            if (e.impCilindroRiscado && !e.impCartuchoPresente)   // com o cartucho fora (pode estar na mão)
                l.Add(new AcaoFisica("Trocar o cilindro (unidade de imagem)", () =>
                {
                    if (!Mochila.Instance.Usar(Item.Cilindro)) return "Você não tem cilindro novo.";
                    e.impCilindroRiscado = false;
                    MM?.CobrarPeca(Mochila.Info(Item.Cilindro).preco, Mochila.Info(Item.Cilindro).nome);
                    ContratosManager.Instance?.PecaTrocada(ContratosManager.Instance.DoChamado(MM != null ? MM.chamadoAtivo : null));
                    return "Você soltou as travas, tirou o cilindro riscado e encaixou o novo, sem tocar na superfície verde.";
                }, 3f, Item.Cilindro));

            PecaAtual = "tampa";
            l.Add(new AcaoFisica("Baixar o scanner (fechar a tampa)", () =>
            {
                if (e.cartuchoNaMao) return "Encaixe ou guarde o cartucho que está na sua mão antes de fechar.";
                e.impTampaAberta = false;
                e.impTampaMalFechada = false;
                return "Tampa fechada.";
            }));
        }

        PecaAtual = "botao";
        l.Add(new AcaoFisica("Imprimir página de teste (botão da impressora)", () =>
        {
            string r = e.Imprimir(false, out bool boa);
            if (boa) MM?.Verificar();
            return r;
        }, 2f));
        PecaAtual = "energia";
        l.Add(new AcaoFisica(e.impLigada ? "Desligar a impressora" : "Ligar a impressora", () =>
        {
            e.impLigada = !e.impLigada;
            return e.impLigada ? "A impressora ligou e fez o barulhinho de aquecimento." : "Impressora desligada.";
        }));
    }

    // ---------- Computador ----------

    static void Computador(EstadoEquipamentos e, List<AcaoFisica> l)
    {
        PecaAtual = "tela";
        if (e.pcLigado) l.Add(new AcaoFisica("Sentar e usar o computador", () => "") { abreComputador = true, observacao = true });
        if (e.pcLigado)
        {
            l.Add(new AcaoFisica("Mandar imprimir uma página de teste pelo computador", () =>
            {
                string r = e.Imprimir(true, out bool boa);
                if (boa) MM?.Verificar();
                return r;
            }, 2f));
            if (e.impDriverErrado)
                l.Add(new AcaoFisica("Remover a impressora e reinstalar com o driver certo (pendrive)", () =>
                {
                    e.impDriverErrado = false;
                    MM?.ConsumirTempo(30f);
                    return "Você removeu a impressora, apagou o driver genérico errado e instalou o do modelo certo.";
                }, 3f, Item.PendriveDrivers));
        }
        PecaAtual = "traseira";
        l.Add(new AcaoFisica("Olhar atrás do gabinete", () => DescreverCabo(e.pcCabo, e.pcLigado, "computador")) { observacao = true });
        AcoesCabo(e.pcCabo, "do computador", l);
        PecaAtual = "energia";
        l.Add(new AcaoFisica(e.pcLigado ? "Desligar no botão (forçado)" : "Ligar o computador", () =>
        {
            if (e.pcLigado)
            {
                e.pcLigado = false;
                if (!e.clienteSalvouTrabalho) MM?.RegistrarErro("Desligou o PC no botão sem o cliente salvar: ele perdeu o trabalho!", 3f);
                return "O computador apagou na hora.";
            }
            if (e.pcGabineteAberto) return "Feche a tampa do gabinete antes de ligar.";
            if (e.pcFonteQueimada) return "Você apertou o botão e nada: nem luz, nem ventoinha. Completamente morto.";
            if (e.pcMemoriaRuim) return "Ligou, a ventoinha girou... e três bipes compridos. A tela continua preta.";
            if (e.pcCoolerFora) return "Não ligue sem o cooler: o processador queima em segundos.";
            if (e.pcSemSistema) return "Ligou, mas a tela mostra: \"Nenhum dispositivo de inicialização\". O disco novo está vazio.";
            e.pcLigado = true;
            e.pcSegundosQuente = 0f;
            e.ReiniciarPc();
            if (e.PcHardwareOk) MM?.Verificar();   // o Windows subiu: o cliente confere na hora
            return e.PcSuperaquecendo ? "O computador ligou, mas a ventoinha já está berrando e sai ar muito quente pelas grades."
                 : e.pcHdRuim ? "Ligou... e ficou minutos em \"Verificando o disco\". Dá para ouvir um clique-clique lá dentro."
                 : "O computador ligou e o Windows iniciou.";
        }, e.pcLigado ? 3f : 0f));

        // Gabinete: por dentro (Fase 4)
        PecaAtual = "gabinete";
        l.Add(new AcaoFisica("Olhar o gabinete (grades e ventoinha)", () =>
            e.pcPoeira > 0.7f ? "As grades estão entupidas de poeira e a ventoinha faz um barulhão. Sai ar muito quente."
            : e.pcPastaSeca ? "As grades estão limpas, mas a ventoinha gira no máximo e sai ar muito quente."
            : e.pcHdRuim ? "Encostando o ouvido: um clique-clique ritmado. É o HD tentando ler e não conseguindo."
            : e.pcFonteQueimada ? "Nenhuma luz acesa. Perto da fonte, atrás, tem um cheiro leve de queimado."
            : e.pcMemoriaRuim ? "A luz de energia acende ao ligar, a ventoinha gira, mas ele só apita."
            : "Gabinete normal: luz acesa, ventoinha baixinha.") { observacao = true });
        if (e.pcLigado)
        {
            l.Add(new AcaoFisica("Testar a saúde do disco (pendrive de diagnóstico)", () =>
                e.pcHdRuim ? "<color=#F2545B>Disco em estado CRÍTICO:</color> 1.243 setores realocados e subindo. Vai parar a qualquer momento."
                           : "Disco saudável: nenhum setor realocado.", 2f, Item.PendriveDrivers) { observacao = true, close = false });
            if (e.pcHdRuim && !e.pcBackupFeito)
                l.Add(new AcaoFisica("Copiar os arquivos do cliente para o seu notebook (backup)", () =>
                {
                    e.pcBackupFeito = true;
                    MM?.ConsumirTempo(40f);
                    return "Backup feito: documentos, planilhas e a pasta do sistema copiados e conferidos.";
                }, 4f));
        }
        if (e.pcSemSistema && !e.pcGabineteAberto)
            l.Add(new AcaoFisica("Instalar o sistema pelo pendrive e restaurar os arquivos", () =>
            {
                e.pcSemSistema = false;
                MM?.ConsumirTempo(60f);
                return e.pcBackupFeito ? "Windows instalado, drivers no lugar e os arquivos do cliente restaurados do backup."
                                       : "Windows instalado e drivers no lugar. (Não havia backup para restaurar.)";
            }, 5f, Item.PendriveDrivers));
        if (!e.pcGabineteAberto)
            l.Add(new AcaoFisica("Virar o gabinete e tirar a tampa lateral (2 parafusos)", () =>
            {
                if (e.pcLigado) return "Desligue o computador antes de abrir (e tire da tomada).";
                e.pcGabineteAberto = true;
                return "Você virou o gabinete, soltou os dois parafusos e deslizou a tampa lateral.";
            }, 1.5f));
        else
        {
            l.Add(new AcaoFisica("Olhar por dentro", () =>
            {
                string poeira = e.pcPoeira > 0.7f ? "Tem uma manta de poeira cobrindo tudo, e o cooler do processador está entupido."
                              : e.pcPoeira > 0.3f ? "Tem poeira acumulada, mas nada grave." : "Está limpo por dentro.";
                string fonte = e.pcFonteQueimada ? " A fonte tem um capacitor estufado e marca de queimado." : "";
                string mem = e.pcMemoriaRuim ? " Os pentes de memória parecem encaixados, mas um deles tem os contatos escurecidos." : "";
                string pasta = !e.pcCoolerFora ? (e.pcPastaSeca ? " O cooler está limpo e bem preso; só tirando para ver a pasta térmica." : "")
                             : e.pcPastaNova ? " O processador está com um grão de pasta nova no centro."
                             : e.pcPastaLimpa ? " O processador está limpo, brilhando, sem pasta."
                             : e.pcPastaSeca ? " <color=#F5B83F>A pasta térmica virou uma crosta seca e esfarelada.</color>" : " A pasta térmica está em bom estado.";
                return poeira + fonte + mem + pasta;
            }) { observacao = true });
            if (e.pcPoeira > 0.3f)
                l.Add(new AcaoFisica("Limpar com pincel e ar comprimido", () =>
                {
                    e.pcPoeira = 0.05f;
                    e.pcLimpezas++;
                    Sujeira.Espalhar(MM != null ? MM.PosicaoEquipamentoAtual : UnityEngine.Vector3.zero, 2, "poeira");
                    return "Você segurou a ventoinha para não girar, soprou o cooler e passou o pincel na placa. Saiu uma nuvem de poeira.";
                }, 3f, Item.ArComprimido));
            l.Add(new AcaoFisica("Tirar os pentes de memória e limpar os contatos", () =>
            {
                e.pcTrocasMemoria++;
                return e.pcMemoriaRuim ? "Você limpou os contatos com borracha e encaixou de novo. (Só testando para saber se resolveu.)"
                                       : "Contatos limpos e pentes encaixados de novo até o clique.";
            }, 2f));
            if (e.pcMemoriaRuim)
                l.Add(new AcaoFisica("Trocar o pente de memória com defeito", () =>
                {
                    if (!Mochila.Instance.Usar(Item.Memoria)) return "Você não tem pente de memória.";
                    e.pcMemoriaRuim = false;
                    e.pcTrocasMemoria++;
                    MM?.CobrarPeca(Mochila.Info(Item.Memoria).preco, Mochila.Info(Item.Memoria).nome);
                    return "Você abriu as travas, tirou o pente ruim e encaixou o novo até as duas travas fecharem.";
                }, 2f, Item.Memoria));
            if (e.pcFonteQueimada)
                l.Add(new AcaoFisica("Trocar a fonte", () =>
                {
                    if (!Mochila.Instance.Usar(Item.Fonte)) return "Você não tem fonte nova.";
                    e.pcFonteQueimada = false;
                    e.pcTrocasFonte++;
                    MM?.CobrarPeca(Mochila.Info(Item.Fonte).preco, Mochila.Info(Item.Fonte).nome);
                    return "Você soltou os 4 parafusos, desligou os conectores da placa e dos discos, e instalou a fonte nova.";
                }, 4f, Item.Fonte));
            // Cooler e pasta térmica
            if (!e.pcCoolerFora)
                l.Add(new AcaoFisica("Tirar o cooler do processador (soltar em X)", () =>
                {
                    e.pcCoolerFora = true;
                    e.pcPastaLimpa = e.pcPastaNova = false;
                    return e.pcPastaSeca ? "Você soltou os quatro parafusos em X e girou o cooler. A pasta velha é uma crosta seca."
                                         : "Você soltou os quatro parafusos em X e tirou o cooler. A pasta ainda está boa.";
                }, 2f));
            else
            {
                if (!e.pcPastaLimpa)
                    l.Add(new AcaoFisica("Limpar a pasta velha (álcool isopropílico e pano)", () =>
                    {
                        e.pcPastaLimpa = true;
                        e.pcPastaNova = false;
                        return "Você limpou o processador e a base do cooler até brilharem.";
                    }, 2f));
                else if (!e.pcPastaNova)
                    l.Add(new AcaoFisica("Aplicar pasta térmica nova (um grão no centro)", () =>
                    {
                        if (!Mochila.Instance.Usar(Item.PastaTermica)) return "Você não tem pasta térmica.";
                        e.pcPastaNova = true;
                        MM?.CobrarPeca(10f, "Pasta térmica");
                        return "Um grão de pasta do tamanho de uma ervilha, bem no centro do processador.";
                    }, 1.5f, Item.PastaTermica));
                l.Add(new AcaoFisica("Recolocar o cooler (apertar em X)", () =>
                {
                    e.pcCoolerFora = false;
                    if (e.pcPastaLimpa && !e.pcPastaNova)
                    {
                        e.pcPastaSeca = true;   // sem pasta nenhuma: esquenta ainda mais
                        return "Você recolocou o cooler SEM pasta térmica. Vai esquentar mais do que antes.";
                    }
                    if (e.pcPastaNova) e.pcPastaSeca = false;
                    return e.pcPastaNova ? "Cooler assentado na pasta nova e apertado em X, conector da ventoinha no lugar."
                                         : "Cooler de volta no lugar, com a mesma pasta de antes.";
                }, 2f));
            }
            // HD com setores ruins → SSD novo
            if (e.pcHdRuim)
                l.Add(new AcaoFisica("Trocar o HD por um SSD novo", () =>
                {
                    if (!Mochila.Instance.Usar(Item.SSD)) return "Você não tem SSD.";
                    e.pcHdRuim = false;
                    e.pcSemSistema = true;
                    e.pcTrocasHd++;
                    MM?.CobrarPeca(Mochila.Info(Item.SSD).preco, Mochila.Info(Item.SSD).nome);
                    if (!e.pcBackupFeito) MM?.RegistrarErro("Trocou o disco SEM fazer backup: o cliente perdeu todos os arquivos!", 6f);
                    return e.pcBackupFeito ? "HD velho fora, SSD novo parafusado e com os cabos de dados e energia ligados."
                                           : "SSD novo instalado... mas os arquivos do cliente ficaram no HD que morreu.";
                }, 3f, Item.SSD));
            l.Add(new AcaoFisica("Colocar a tampa e voltar o gabinete no lugar", () =>
            {
                if (e.pcCoolerFora) return "Recoloque o cooler do processador antes de fechar.";
                e.pcGabineteAberto = false;
                return "Tampa parafusada e gabinete de volta no lugar.";
            }, 1f));
        }
    }

    // ---------- Roteador ----------

    static void Roteador(EstadoEquipamentos e, List<AcaoFisica> l)
    {
        PecaAtual = "luzes";
        l.Add(new AcaoFisica("Olhar as luzes", () => e.LuzesRoteador()) { observacao = true });
        PecaAtual = "etiqueta";
        l.Add(new AcaoFisica("Ler a etiqueta embaixo do roteador", () =>
            $"Painel: http://192.168.0.1 · Usuário: admin · Senha: {EstadoEquipamentos.SenhaPadrao} · Wi-Fi de fábrica: ROTEADOR_5G_A1B2") { observacao = true });
        PecaAtual = "fonte";
        l.Add(new AcaoFisica(e.rotNaTomada ? "Tirar da tomada" : "Colocar de volta na tomada", () => e.TomadaRoteador()));
        PecaAtual = "reset";
        if (e.rotNaTomada)
            l.Add(new AcaoFisica("Segurar o botão de reset com o clipe (10 s)", () =>
            {
                bool precisava = MM != null && MM.chamadoAtivo != null && MM.chamadoAtivo.problema != null &&
                                 MM.chamadoAtivo.problema.solucao == "Resetar para padrão";
                e.ResetFabrica();
                if (!precisava) MM?.RegistrarErro("Você resetou o roteador sem precisar: a internet e o Wi-Fi de todo mundo caíram!", 3f);
                return "As luzes piscaram todas juntas: o roteador voltou ao padrão de fábrica. Configure de novo pelo painel (192.168.0.1).";
            }, 10f, Item.Clipe));
    }

    // ---------- Cabo de rede (impressora e computador) ----------

    static string DescreverCabo(CaboRede c, bool ligado, string onde)
    {
        string led = !ligado ? "apagado (equipamento desligado)" : c.Funciona ? "verde, piscando" : "apagado";
        string cabo = !c.conectado ? "O cabo de rede está FORA da porta!" :
                      c.defeito == DefeitoCabo.Conector ? "O cabo está na porta, mas o conector parece meio torto e a trava está quebrada." :
                      "O cabo está encaixado na porta.";
        return $"{cabo} LED da porta de rede do(a) {onde}: {led}.";
    }

    static void AcoesCabo(CaboRede c, string de, List<AcaoFisica> l)
    {
        l.Add(new AcaoFisica(c.conectado ? $"Desconectar o cabo {de}" : $"Conectar o cabo {de} (até dar o clique)", () =>
        {
            c.conectado = !c.conectado;
            return c.conectado ? (c.defeito == DefeitoCabo.Conector ? "Encaixou, mas sem o clique: o conector está ruim." : "Clique! Cabo encaixado.")
                               : "Cabo desconectado.";
        }));
        l.Add(new AcaoFisica($"Testar o cabo {de} com o testador", () =>
        {
            if (c.conectado) return "Desconecte o cabo primeiro para ligar no testador.";
            var est = MM?.Estado;
            if (est != null) { est.testesDeCabo++; est.ultimoTesteCabo = c.defeito; }
            return c.Testar();
        }, 2f, Item.TestadorCabo));
        if (!c.conectado)
        {
            l.Add(new AcaoFisica($"Refazer a ponta do cabo {de} (crimpar)", () => "") { abreCrimpagem = true, requer = Item.AlicateCrimpagem });
            l.Add(new AcaoFisica($"Medir a distância até a tomada de rede", () =>
                $"Da porta {de} até a tomada da parede dá uns {c.metrosNecessarios:0.#} m.") { observacao = true });
            l.Add(new AcaoFisica($"Passar um cabo novo da mochila", () =>
            {
                var m = Mochila.Instance;
                if (m == null || m.cabos.Count == 0) return "Você não tem nenhum cabo pronto na mochila.";
                var novo = m.PegarCabo(c.metrosNecessarios);
                if (novo == null)
                    return $"<color=#F2545B>Nenhum cabo seu alcança!</color> Precisa de {c.metrosNecessarios:0.#} m e você tem: {m.ListaCabos()}.";
                MM?.CobrarPeca(novo.metros * 3f + 4f, $"Cabo de rede {novo.metros:0.#} m + 2 conectores");   // cabo + 2 conectores
                Sujeira.Espalhar(MM != null ? MM.PosicaoEquipamentoAtual : UnityEngine.Vector3.zero, 1, "cabo velho");
                c.defeito = novo.Bom ? DefeitoCabo.Nenhum : DefeitoCabo.Conector;
                c.conectado = true;
                return $"Você passou um cabo de {novo.metros:0.#} m e conectou. Clique!" +
                       (novo.metros > c.metrosNecessarios * 2.5f ? " (sobrou bastante cabo enrolado)" : "");
            }, 4f));
        }
    }
}
