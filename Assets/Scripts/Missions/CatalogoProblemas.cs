using UnityEngine;

// Jeito de ser do cliente: muda prazo, pagamento, reputação e o que ele fala na conversa.
public enum Personalidade
{
    Tranquilo,   // normal
    Apressado,   // prazo curto, perde a paciência com muitas perguntas
    Confuso,     // explica mal: precisa conversar para saber qual equipamento olhar
    Mentiroso,   // esconde o que fez (uma das respostas é falsa)
    VIP          // paga o dobro, mas erro custa o dobro
}

// Perguntas que o técnico pode fazer ao cliente
public enum Pergunta
{
    DesdeQuando,
    MexeramEmAlgo,
    MensagemErro,
    OutrosAfetados,
    JaReiniciou
}

// Um problema possível: onde está, o que resolve, o que o cliente responde e o que cada teste mostra.
public class Problema
{
    public TipoEquipamento equipamento;
    public string solucao;          // identifica o defeito (ver CatalogoProblemas.Quebrar)
    public string descricao;        // como um cliente normal descreve
    public string descricaoVaga;    // como um cliente confuso descreve
    public string[] respostas;      // uma por Pergunta (mesma ordem do enum)
    public Pergunta perguntaMentira;
    public string mentira;          // o que o Mentiroso responde nessa pergunta

    public Problema(TipoEquipamento equipamento, string solucao, string descricao, string descricaoVaga,
                    string[] respostas, Pergunta perguntaMentira, string mentira)
    {
        this.equipamento = equipamento;
        this.solucao = solucao;
        this.descricao = descricao;
        this.descricaoVaga = descricaoVaga;
        this.respostas = respostas;
        this.perguntaMentira = perguntaMentira;
        this.mentira = mentira;
    }

    public string Resposta(Pergunta p, bool mentir) =>
        mentir && p == perguntaMentira ? mentira : respostas[(int)p];

    // ---------- O defeito na simulação (cada problema carrega o seu) ----------
    public System.Action<EstadoEquipamentos> quebrar;          // estraga o estado do cliente (CatalogoProblemas.Quebrar)
    public System.Func<EstadoEquipamentos, bool> concluido;    // condição a mais para dar por resolvido (além do equipamento funcionar)
    public System.Func<EstadoEquipamentos, string> causaRaiz;  // como o cliente reclama se a causa ficou (null = tratada)
    public Item? ferramentaIdeal;                              // ferramenta que deixa o diagnóstico bem mais fácil
    public bool remoto;                                        // dá para resolver por acesso remoto
    public bool derrubaInternet;                               // o cliente fica sem internet: nem o acesso remoto conecta
    public string comoRemoto;                                  // o que o técnico faz de longe (vai na OS)

    public Problema Quebra(System.Action<EstadoEquipamentos> q) { quebrar = q; return this; }
    public Problema Conclui(System.Func<EstadoEquipamentos, bool> c) { concluido = c; return this; }
    public Problema Causa(System.Func<EstadoEquipamentos, string> c) { causaRaiz = c; return this; }
    public Problema Ferramenta(Item i) { ferramentaIdeal = i; return this; }
    public Problema Remoto(string como = null) { remoto = true; comoRemoto = como; return this; }
    public Problema SemInternet() { derrubaInternet = true; return this; }
    public string motivoPresencial;                            // por que não dá de longe (o técnico ouve isso no remoto)
    public Problema SoPresencial(string motivo) { motivoPresencial = motivo; return this; }
}

public static partial class CatalogoProblemas
{
    // ---------------- Perguntas ----------------

    public static string TextoPergunta(Pergunta p)
    {
        switch (p)
        {
            case Pergunta.DesdeQuando:    return "Desde quando isso acontece?";
            case Pergunta.MexeramEmAlgo:  return "Alguém mexeu em alguma coisa?";
            case Pergunta.MensagemErro:   return "Aparece alguma mensagem de erro?";
            case Pergunta.OutrosAfetados: return "Acontece com mais alguém?";
            default:                      return "Já tentou reiniciar?";
        }
    }

    public static string RotuloPergunta(Pergunta p)
    {
        switch (p)
        {
            case Pergunta.DesdeQuando:    return "Desde quando";
            case Pergunta.MexeramEmAlgo:  return "Alguém mexeu?";
            case Pergunta.MensagemErro:   return "Mensagem";
            case Pergunta.OutrosAfetados: return "Quem é afetado";
            default:                      return "Reiniciou?";
        }
    }

    // ---------------- Defeitos na simulação ----------------

    // Soluções da Etapa 4 (ferramentas de diagnóstico)
    public const string EncaixarMemoria = "Encaixar a memória";
    public const string TrocarFonteFraca = "Trocar fonte fraca";
    public const string TrocarMemoriaComErro = "Trocar memória com erro";
    public const string TrocarBateria = "Trocar bateria da placa-mãe";
    public const string Checkup = "Check-up completo";
    // Etapa 7: software e notebook
    public const string DefinirPadrao = "Definir a impressora padrão";
    public const string DesbloquearConta = "Desbloquear a conta";
    public const string CorrigirEmail = "Corrigir o servidor de saída do e-mail";
    public const string LimparInicializacao = "Limpar a inicialização";
    public const string RemoverVirus = "Remover vírus e propaganda";
    public const string DesfazerAtualizacao = "Desfazer a atualização";
    public const string LimparTecla = "Limpar a tecla";
    public const string TrocarCarregador = "Trocar o carregador";
    public const string LimparVentoinha = "Limpar a ventoinha do notebook";
    public const string TrocarBateriaEstufada = "Trocar a bateria estufada";
    public const string TrocarDobradica = "Trocar a dobradiça";
    public static bool EhCheckup(Problema p) => p != null && p.solucao == Checkup;

    // Etapa 5: serviços da locação feitos no local. Ficam FORA das listas sorteáveis (só o ContratosManager cria).
    public const string InstalarLocacao = "Instalar a impressora alugada";
    public const string VisitarLocacao = "Visita mensal da locação";
    public static bool EhServicoLocacao(Problema p) => p != null && (p.solucao == InstalarLocacao || p.solucao == VisitarLocacao);

    public static readonly Problema InstalacaoLocacao = new Problema(TipoEquipamento.Impressora, InstalarLocacao,
        "Chegou a impressora do contrato: precisa instalar e deixar imprimindo no computador.",
        "Chegou a impressora nova, está na caixa ainda.",
        new[] {
            "Assinamos o contrato com vocês esta semana.",
            "Ninguém abriu a caixa, deixamos para o técnico.",
            "Nenhuma, ela nem foi ligada ainda.",
            "Todo mundo vai imprimir nela pelo computador da recepção.",
            "Ainda não ligamos nada." },
        Pergunta.MexeramEmAlgo, "Ninguém encostou na caixa.")
        .Conclui(e => e.InstalacaoCompleta)
        .Quebra(e =>
        {
            e.instalacaoEmAndamento = true;
            e.impNaCaixa = true;
            e.impComFitas = true;
            e.impCartuchoPresente = false;
            e.tonerDaCaixa = true;
            e.impToner = 0f;
            e.impSemPapel = true;
            e.impCabo.conectado = false;
            e.impCabo.defeito = DefeitoCabo.Nenhum;
            e.impLigada = false;
            e.impIpPendente = true;
            e.pcSemImpressora = true;
            e.impTestePeloPc = false;
        });

    public static readonly Problema VisitaLocacao = new Problema(TipoEquipamento.Impressora, VisitarLocacao,
        "Visita mensal da impressora alugada: leitura do contador e suprimentos.",
        "É a visita do mês da impressora, né?",
        new[] {
            "A última visita foi no mês passado.",
            "Só usamos normalmente.",
            "Às vezes aparece aviso de toner no visor.",
            "Todo mundo imprime nela.",
            "Não precisou reiniciar." },
        Pergunta.MensagemErro, "Nunca aparece nada no visor.")
        .Conclui(e => e.VisitaCompleta)
        .Quebra(e =>
        {
            // O ContratosManager acerta o toner e a unidade de imagem pelo contrato (PrepararVisita)
            e.PrepararVisita(e.impToner, 0f);
        });

    // Problemas que ficam bem mais fáceis com uma ferramenta (o card mostra a dica quando o técnico não a tem)
    public static Item? FerramentaIdeal(Problema p) => p?.ferramentaIdeal;

    // O check-up só é oferecido a quem tem as ferramentas para fazer o laudo inteiro
    public static bool PodeFazerCheckup(Mochila m) =>
        m != null && m.Quantidade(Item.PendriveDiagnostico) > 0 && m.Quantidade(Item.Multimetro) > 0;

    // Estraga os equipamentos do cliente de acordo com o problema (com variações sorteadas)
    public static void Quebrar(Problema p, EstadoEquipamentos e)
    {
        // Distâncias até a tomada de rede variam de cliente para cliente
        e.impCabo.metrosNecessarios = Mathf.Round(Random.Range(1f, 6f) * 2f) / 2f;
        e.pcCabo.metrosNecessarios = Mathf.Round(Random.Range(1f, 4f) * 2f) / 2f;
        e.diasLigado = Random.Range(1, 4);
        e.fila.Clear();
        // Desgaste das peças pela idade do computador (Etapa 4); o defeito abaixo derruba a peça culpada
        e.SortearDesgaste(Random.Range(1, 9));

        if (p.quebrar == null)
            throw new System.InvalidOperationException($"Problema \"{p.equipamento}: {p.solucao}\" sem defeito no catálogo (falta .Quebra).");
        p.quebrar(e);
    }

    // Cabo com defeito: às vezes só está solto, às vezes o conector está ruim, às vezes rompeu
    static void SortearDefeitoCabo(CaboRede c)
    {
        float r = Random.value;
        if (r < 0.4f) { c.conectado = false; c.defeito = DefeitoCabo.Nenhum; }
        else if (r < 0.75f) { c.conectado = true; c.defeito = DefeitoCabo.Conector; }
        else { c.conectado = true; c.defeito = DefeitoCabo.Rompido; }
    }

    // O problema que o cliente reclamou foi resolvido? (o jogador confere imprimindo, pingando etc.)
    public static bool Resolvido(Problema p, EstadoEquipamentos e)
    {
        if (p.concluido != null && !p.concluido(e)) return false;
        return RegistroEquipamentos.De(p.equipamento).Funcionando(e);
    }

    // A causa do problema ficou? (o sintoma sumiu, mas volta em 1–3 dias: o cliente chama de novo, em garantia)
    // Devolve como o cliente vai descrever a volta, ou null se a causa foi tratada.
    public static string CausaRaizPendente(Problema p, EstadoEquipamentos e) =>
        p == null || e == null || p.causaRaiz == null ? null : p.causaRaiz(e);

    // ---------------- Problemas ----------------
    // Ordem das respostas: DesdeQuando, MexeramEmAlgo, MensagemErro, OutrosAfetados, JaReiniciou

    public static readonly Problema[] Faceis = {
        new Problema(TipoEquipamento.Impressora, "Reiniciar spooler",
            "Não consigo imprimir nada.",
            "O negócio de imprimir não vai, sabe?",
            new[] {
                "Desde que o computador voltou de uma atualização hoje cedo.",
                "Não, ninguém encostou na impressora.",
                "Aparece \"O serviço de impressão não está em execução\".",
                "Ninguém consegue mandar impressão daqui.",
                "Desliguei e liguei a impressora, não mudou nada." },
            Pergunta.MensagemErro, "Mensagem? Não reparei em nenhuma.")
            .Remoto()
            .Quebra(e => { e.spoolerRodando = false; }),

        new Problema(TipoEquipamento.Impressora, "Verificar cabo",
            "Impressora aparece offline no sistema.",
            "A impressora sumiu do computador, acho.",
            new[] {
                "Desde ontem, depois que a equipe de limpeza passou aqui.",
                "A limpeza arrastou o armário onde ela fica.",
                "Só diz \"Impressora offline\".",
                "Ninguém consegue usar ela.",
                "Reiniciei o computador, continuou offline." },
            Pergunta.MexeramEmAlgo, "Ninguém mexeu em nada aqui, juro.")
            .Causa(e => e.impCaboSolto ? "A impressora sumiu de novo: arrastaram o armário e o cabo solto soltou outra vez" : null)
            .Quebra(e =>
            {
                SortearDefeitoCabo(e.impCabo);
                e.impCaboSolto = true;   // a causa: o cabo passa solto atrás do armário
            }),

        new Problema(TipoEquipamento.Impressora, "Verificar fila de impressão",
            "Um documento travou e nada mais imprime.",
            "Mandei imprimir, ficou lá parado... e agora nada sai.",
            new[] {
                "Desde que mandei um PDF de 300 páginas de manhã.",
                "Não mexi em nada, só mandei imprimir.",
                "No documento aparece \"Erro - imprimindo\".",
                "Todo mundo, porque a impressora é uma só.",
                "Não, achei melhor não mexer." },
            Pergunta.DesdeQuando, "Sei lá, parou do nada.")
            .Remoto()
            .Quebra(e =>
            {
                e.fila.Add(new TrabalhoImpressao("Relatorio_anual_FINAL_v3.pdf", 300, true));
                int n = Random.Range(4, 15);
                for (int i = 0; i < n; i++)
                    e.fila.Add(new TrabalhoImpressao(new[] { "Boleto.pdf", "Contrato.docx", "Planilha.xlsx", "Foto.jpg", "Recibo.pdf" }[Random.Range(0, 5)], Random.Range(1, 6)));
            }),

        new Problema(TipoEquipamento.Impressora, "Fechar a tampa",
            "A impressora parou de imprimir do nada.",
            "Ela tá ligada, mas não imprime mais nada.",
            new[] {
                "Desde que trocamos o papel hoje de manhã.",
                "Só abrimos para ver se tinha papel preso e fechamos.",
                "No visor aparece \"Feche a tampa\", mas ela está fechada!",
                "Ninguém consegue imprimir.",
                "Desliguei e liguei, a mensagem continua." },
            Pergunta.MexeramEmAlgo, "Ninguém encostou nela.")
            .Quebra(e => { e.impTampaMalFechada = true; }),

        new Problema(TipoEquipamento.Impressora, "Retirar papel atolado",
            "A impressora puxa a folha e trava no meio.",
            "Ela come o papel e para, faz um barulho feio.",
            new[] {
                "Desde hoje de manhã, quando imprimi umas etiquetas.",
                "Coloquei umas folhas que estavam meio úmidas, só isso.",
                "Aparece \"Atolamento de papel\" no visorzinho.",
                "Ninguém consegue imprimir, a impressora é uma só.",
                "Desliguei e liguei, ela volta a reclamar do papel." },
            Pergunta.MexeramEmAlgo, "Não mexi em nada, ela que é ruim.")
            .Causa(e => e.impPapelUmido ? "A impressora voltou a atolar: o papel úmido continuou na gaveta" : null)
            .Quebra(e =>
            {
                e.impPapelAtolado = true;
                e.impPapelUmido = true;  // a causa: a resma úmida continua na gaveta
            }),

        new Problema(TipoEquipamento.Impressora, "Colocar papel",
            "A impressora não imprime e fica piscando.",
            "Ela fica com uma luz piscando e não sai nada.",
            new[] {
                "Desde depois do almoço, imprimimos muita nota fiscal hoje.",
                "Não, só usamos bastante.",
                "No visor diz alguma coisa de \"bandeja\".",
                "Todo mundo, a impressora é a mesma.",
                "Reiniciei duas vezes e continua igual." },
            Pergunta.MensagemErro, "Não tem mensagem, só pisca.")
            .Quebra(e => { e.impSemPapel = true; }),

        new Problema(TipoEquipamento.Impressora, "Trocar toner",
            "A impressão está saindo clara e falhada.",
            "As folhas estão saindo meio... fantasma?",
            new[] {
                "Vem piorando faz umas duas semanas.",
                "Alguém sacudiu o toner pra ele \"render mais\".",
                "Às vezes pisca \"Toner baixo\", mas a gente ignora.",
                "Sai assim para todo mundo.",
                "Reiniciar não muda nada, sai clara igual." },
            Pergunta.MensagemErro, "Nunca apareceu mensagem nenhuma.")
            .Quebra(e => { e.impToner = Random.Range(0.01f, 0.06f); }),
    };

    public static readonly Problema[] Medios = {
        new Problema(TipoEquipamento.Computador, EncaixarMemoria,
            "O computador liga, mas não aparece imagem nenhuma.",
            "A luz acende, o ventilador gira, mas a tela fica preta.",
            new[] {
                "Desde hoje cedo. Ontem estava normal.",
                "A moça da limpeza arrastou a mesa ontem e o gabinete caiu de lado.",
                "Não aparece nada e nem apita, fica só a tela preta.",
                "Só esse.",
                "Desliguei da tomada e liguei de novo, igual." },
            Pergunta.MexeramEmAlgo, "Ninguém encostou nele.")
            .Ferramenta(Item.PlacaPOST)
            .Quebra(e =>
            {
                e.pcMemoriaSolta = true;
                e.pcLigado = false;
            }),

        new Problema(TipoEquipamento.Computador, TrocarBateria,
            "O computador perde a data toda vez que fica desligado.",
            "Ele liga pedindo pra apertar F1 e o relógio volta para 2009.",
            new[] {
                "Faz um mês. Toda segunda-feira, depois do fim de semana desligado.",
                "Nada. O computador é antigo, deve ter uns oito anos.",
                "\"CMOS checksum error\" e pede F1. Depois o sistema da contabilidade reclama do certificado.",
                "Só esse; os outros ficam ligados direto.",
                "Reiniciar não muda, o problema é quando desliga da tomada." },
            Pergunta.MensagemErro, "Não aparece mensagem, ele só fica com a hora errada.")
            .Ferramenta(Item.Multimetro)
            .Quebra(e =>
            {
                e.pcBateriaFraca = true;
                e.saudeBateria = 0.05f;
            }),

        new Problema(TipoEquipamento.Computador, Checkup,
            "Quero um check-up completo do computador antes que ele dê problema.",
            "Queria que você desse uma olhada geral no computador, ele é velhinho.",
            new[] {
                "Não tem problema nenhum agora; é prevenção, ele já tem uns anos.",
                "Nunca abriram. Só uso, todo dia.",
                "Nenhuma mensagem. Só às vezes acho ele mais barulhento.",
                "É o computador do caixa; se ele parar, a empresa para.",
                "Desligo todo dia no fim do expediente." },
            Pergunta.MexeramEmAlgo, "Já trocaram tudo dele ano passado.")
            .Ferramenta(Item.PendriveDiagnostico)
            .Quebra(e =>
            {
                e.checkup = true;
                e.SortearDesgaste(Random.Range(4, 9));   // quem pede check-up costuma ter um computador mais velho
                // Uma peça perto do fim (é o que dá valor ao laudo)
                switch (Random.Range(0, 4))
                {
                    case 0: e.saudeDisco = Random.Range(0.25f, 0.38f); break;
                    case 1: e.saudeBateria = Random.Range(0.3f, 0.38f); break;
                    case 2: e.saudeCooler = Random.Range(0.3f, 0.38f); break;
                    default: e.pcPoeira = Random.Range(0.4f, 0.65f); break;
                }
            }),

        new Problema(TipoEquipamento.Computador, "Limpar por dentro",
            "O computador desliga sozinho no meio do trabalho.",
            "Ele apaga do nada, e faz um barulhão de ventilador.",
            new[] {
                "Faz umas semanas, e piora nos dias quentes.",
                "Ninguém nunca abriu esse computador, tem uns seis anos.",
                "Não aparece nada, a tela só apaga de uma vez.",
                "Só esse, os outros são mais novos.",
                "Depois que esfria ele liga de novo, e daqui a pouco apaga." },
            Pergunta.MexeramEmAlgo, "Mandei limpar mês passado, está novinho por dentro.")
            .Quebra(e => { e.pcPoeira = Random.Range(0.85f, 1f); }),

        new Problema(TipoEquipamento.Computador, "Trocar memória",
            "O computador liga, apita e não aparece nada na tela.",
            "Ele liga, faz uns apitos e a tela fica preta.",
            new[] {
                "Desde ontem, depois de uma queda de energia.",
                "Só mudamos ele de mesa na semana passada.",
                "Tela preta. São três apitos compridos toda vez que liga.",
                "Só esse computador.",
                "Liguei e desliguei umas dez vezes, sempre os apitos." },
            Pergunta.MensagemErro, "Não tem apito nenhum, só não liga.")
            .Ferramenta(Item.PlacaPOST)
            .Quebra(e =>
            {
                e.pcMemoriaRuim = true;
                e.saudeMemoria = 0f;
                e.pcLigado = false;
            }),

        new Problema(TipoEquipamento.Computador, "Trocar fonte",
            "O computador não liga de jeito nenhum.",
            "Morreu. Aperto o botão e nada acontece.",
            new[] {
                "Desde o temporal de ontem à noite.",
                "Não mexemos, ele estava ligado quando caiu um raio perto.",
                "Nada: nem luz, nem barulho. Senti um cheiro de queimado.",
                "Só esse; o roteador e a impressora estão normais.",
                "Troquei de tomada e nada." },
            Pergunta.MensagemErro, "Não senti cheiro nem nada, só parou.")
            .Ferramenta(Item.TestadorFonte)
            .Quebra(e =>
            {
                e.pcFonteQueimada = true;
                e.saudeFonte = 0f;
                e.pcLigado = false;
            }),

        new Problema(TipoEquipamento.Impressora, "Fixar o IP",
            "A impressora está ligada, mas aparece offline no computador.",
            "Ela tá com a luzinha acesa, mas o computador diz que não acha.",
            new[] {
                "Desde ontem, quando faltou luz e voltou.",
                "Não mexemos em nada; só reiniciaram o roteador depois da queda de luz.",
                "No computador diz \"Impressora offline\". No visor dela diz \"Pronta\".",
                "Nenhum computador imprime, mas a cópia direto nela funciona.",
                "Reiniciei a impressora e o computador, igual." },
            Pergunta.MexeramEmAlgo, "Ninguém mexeu em roteador nenhum.")
            .Quebra(e => { e.impIpMudou = true; }),

        new Problema(TipoEquipamento.Impressora, "Reinstalar driver da impressora",
            "A impressora imprime um monte de símbolos estranhos.",
            "Sai um monte de folha com uns rabiscos, gastou a resma toda.",
            new[] {
                "Desde que o computador fez uma atualização grande.",
                "O sobrinho do chefe \"instalou a impressora de novo\" na semana passada.",
                "Não dá erro: imprime, só que sai tudo embaralhado.",
                "Só do computador da recepção; a cópia direto na impressora sai certa.",
                "Reiniciei tudo e continua saindo lixo." },
            Pergunta.MexeramEmAlgo, "Ninguém instalou nada.")
            .Quebra(e => { e.impDriverErrado = true; }),

        new Problema(TipoEquipamento.Impressora, "Trocar cilindro",
            "Todas as folhas saem com uma risca preta.",
            "Tem um risco estranho em tudo que imprime.",
            new[] {
                "Faz uns dias, foi aparecendo e ficou.",
                "Trocamos o toner semana passada e a risca continuou.",
                "Não aparece mensagem, só a risca na folha.",
                "Sai assim para todo mundo, sempre no mesmo lugar da folha.",
                "Desliguei, chacoalhei o toner, nada resolve." },
            Pergunta.MexeramEmAlgo, "Não mexemos em nada.")
            .Quebra(e =>
            {
                e.impCilindroRiscado = true;
                e.impToner = Random.Range(0.4f, 0.9f);   // toner está bom: a listra é do cilindro
            }),

        new Problema(TipoEquipamento.Computador, "Verificar cabo de rede",
            "Computador sem acesso à rede.",
            "A internet do meu computador acabou.",
            new[] {
                "Desde que trocaram minha mesa de lugar.",
                "Mudaram minha mesa ontem à tarde.",
                "\"Cabo de rede desconectado\", no cantinho da tela.",
                "Só eu. O pessoal do lado está navegando.",
                "Reiniciei duas vezes." },
            Pergunta.MexeramEmAlgo, "Ninguém mudou nada de lugar.")
            .Quebra(e => { SortearDefeitoCabo(e.pcCabo); }),

        new Problema(TipoEquipamento.Computador, "Verificar IP",
            "Aparece \"conflito de IP\" e a rede não conecta.",
            "Aparece um aviso estranho de... IP? E não conecta.",
            new[] {
                "Desde que o estagiário \"arrumou\" a rede no meu PC.",
                "O estagiário colocou um número fixo aí.",
                "\"Outro dispositivo na rede está usando o seu endereço IP\".",
                "A impressora também começou a falhar.",
                "Reiniciei, voltou o mesmo aviso." },
            Pergunta.MexeramEmAlgo, "Ninguém mexeu nas configurações.")
            .Remoto()
            .Quebra(e =>
            {
                e.pcIpFixo = true;
                e.pcIp = e.ipImpressora;
            }),

        new Problema(TipoEquipamento.Computador, "Reinstalar driver",
            "A placa de rede sumiu depois de uma atualização.",
            "O computador esqueceu que tem internet.",
            new[] {
                "Desde a atualização do sistema de ontem à noite.",
                "Só deixei atualizar, como o aviso pediu.",
                "Nem aparece o ícone de rede direito.",
                "Só o meu.",
                "Reiniciei umas três vezes." },
            Pergunta.DesdeQuando, "Faz tempo, nem lembro.")
            .Remoto()
            .Quebra(e => { e.driverOk = false; }),

        new Problema(TipoEquipamento.Computador, "Reiniciar equipamento",
            "O computador está travando e lento.",
            "Ele está muito devagar, travando tudo.",
            new[] {
                "Está ligado há umas três semanas sem desligar.",
                "Não, só deixo ligado pra não perder as abas.",
                "Às vezes: \"O programa não está respondendo\".",
                "Só o meu.",
                "Reiniciar? Nunca, tenho medo de perder as coisas." },
            Pergunta.JaReiniciou, "Claro, reinicio todo dia.")
            .Remoto()
            .Quebra(e =>
            {
                e.memoria = Random.Range(0.94f, 0.99f);
                e.diasLigado = Random.Range(15, 40);
            }),

        // ---------- Etapa 7: software (tela do cliente) ----------
        new Problema(TipoEquipamento.Computador, DefinirPadrao,
            "Mando imprimir e abre uma janela pedindo para salvar um arquivo.",
            "A impressora não imprime, ela quer salvar o papel no computador?",
            new[] {
                "Desde ontem, depois que eu salvei um boleto em PDF.",
                "Só usei a opção de imprimir em PDF uma vez, para mandar um boleto por e-mail.",
                "Aparece \"Salvar saída de impressão como\" e pede um nome.",
                "Só no meu computador; dos outros sai normal.",
                "Reiniciei e continua pedindo para salvar." },
            Pergunta.MexeramEmAlgo, "Não mexi em nada, só mandei imprimir.")
            .Remoto("Pelo acesso remoto: definiu a impressora do escritório como padrão e desligou a troca automática da padrão.")
            .Causa(e => e.swGerenciarPadraoAuto ? "A impressão voltou a virar PDF: o sistema trocou a impressora padrão sozinho de novo" : null)
            .Quebra(e => { e.swPadraoErrado = true; e.swGerenciarPadraoAuto = true; }),

        new Problema(TipoEquipamento.Computador, DesbloquearConta,
            "Errei a senha umas vezes e agora o computador não deixa entrar.",
            "O computador fala que eu tô bloqueada. Eu fiz alguma coisa errada?",
            new[] {
                "Desde hoje cedo. Voltei de férias e não lembrava a senha.",
                "A senha estava num papelzinho colado no monitor, mas a faxina jogou fora.",
                "\"A conta foi bloqueada. Muitas tentativas de senha incorreta.\"",
                "Só a minha conta; o pessoal entra normal.",
                "Reiniciei, mas volta na mesma tela de senha." },
            Pergunta.MexeramEmAlgo, "Não perdi papel nenhum, a senha que mudou sozinha.")
            .SoPresencial("Com a conta bloqueada o cliente nem entra no sistema para aceitar o acesso remoto.")
            .Quebra(e => { e.swContaBloqueada = true; e.swSenhaRedefinida = false; e.swAdminLogado = false; }),

        new Problema(TipoEquipamento.Computador, CorrigirEmail,
            "Meus e-mails não saem: ficam todos parados na caixa de saída.",
            "Eu mando os e-mails e eles não chegam em ninguém.",
            new[] {
                "Desde segunda, quando o provedor mudou alguma coisa.",
                "Chegou um aviso do provedor sobre \"segurança no envio\", mas eu não entendi.",
                "\"O servidor de saída não respondeu\" e o e-mail fica na caixa de saída.",
                "Receber eu recebo normal. Só não sai nada.",
                "Fechei e abri o e-mail várias vezes, reiniciei também." },
            Pergunta.MensagemErro, "Não aparece erro nenhum, só não chega.")
            .Remoto("Pelo acesso remoto: mudou o servidor de saída para a porta 587 com autenticação e enviou as mensagens presas.")
            .Quebra(e => { e.swEmailPresos = Random.Range(2, 7); e.swEmailPorta = 25; e.swEmailAutenticacao = Random.value < 0.5f; }),

        new Problema(TipoEquipamento.Computador, LimparInicializacao,
            "O computador demora uma eternidade para ficar usável depois que liga.",
            "Ele liga, mas fica um tempão rodando a bolinha antes de deixar eu fazer qualquer coisa.",
            new[] {
                "Foi piorando de uns meses para cá.",
                "Só fui instalando uns programinhas grátis que o pessoal indica.",
                "Não dá erro; aparece um monte de janelinha abrindo sozinha quando liga.",
                "Só o meu, os outros ligam rapidinho.",
                "Eu reinicio e piora: tem que esperar tudo abrir de novo." },
            Pergunta.MexeramEmAlgo, "Nunca instalei nada.")
            .Remoto("Pelo acesso remoto: desativou os programas desnecessários na inicialização e reiniciou.")
            .Quebra(e =>
            {
                int n = Random.Range(8, 12);
                for (int i = 0; i < n; i++)
                    e.swInicializacao.Add(new ProgramaInicio(EstadoEquipamentos.ProgramasInuteis[i], i % 3 == 0 ? "Alto" : "Médio", false));
            }),

        // ---------- Etapa 7: notebook ----------
        new Problema(TipoEquipamento.Notebook, LimparTecla,
            "Uma tecla do notebook parou de funcionar.",
            "O notebook come as letras, as palavras saem faltando pedaço.",
            new[] {
                "Desde ontem, depois do lanche da tarde.",
                "Eu como na mesa às vezes... caiu um farelinho de biscoito, mas eu assoprei.",
                "Não dá mensagem, a letra A simplesmente não sai.",
                "É o meu notebook, só eu uso.",
                "Reiniciei e continua sem a letra A." },
            Pergunta.MexeramEmAlgo, "Nunca comi perto do notebook.")
            .Quebra(e => { e.nbTeclaFalhando = true; }),

        new Problema(TipoEquipamento.Notebook, TrocarCarregador,
            "O notebook não carrega mais: só funciona até a bateria acabar.",
            "O notebook tá ligado na tomada mas a bateria só desce.",
            new[] {
                "Faz uns três dias. Antes já falhava se mexesse no fio.",
                "O cabo do carregador estava descascando, meu marido passou fita isolante.",
                "No ícone da bateria diz \"conectado, não está carregando\".",
                "Só nesse notebook; o carregador é dele mesmo.",
                "Reiniciei e troquei de tomada, nada." },
            Pergunta.MexeramEmAlgo, "Ninguém mexeu no carregador, está novinho.")
            .Ferramenta(Item.Multimetro)
            .Quebra(e => { e.nbCarregadorRuim = true; e.nbCarga = Random.Range(0.06f, 0.2f); }),

        new Problema(TipoEquipamento.Notebook, LimparVentoinha,
            "O notebook esquenta muito e desliga sozinho.",
            "O notebook fica pelando embaixo e apaga do nada.",
            new[] {
                "Faz umas semanas, piora à tarde.",
                "Eu uso ele em cima da cama às vezes, no cobertor.",
                "Não aparece nada, a tela só apaga.",
                "É só esse notebook.",
                "Depois que esfria ele liga, e daqui a pouco desliga de novo." },
            Pergunta.MexeramEmAlgo, "Uso sempre na mesa, certinho.")
            .Quebra(e => { e.nbPoeira = Random.Range(0.85f, 1f); }),
    };

    public static readonly Problema[] Dificeis = {
        new Problema(TipoEquipamento.Computador, TrocarFonteFraca,
            "O computador reinicia sozinho quando abro o sistema.",
            "Ele apaga e liga de novo do nada, principalmente quando estou trabalhando.",
            new[] {
                "Começou faz uns dez dias, e está cada vez mais frequente.",
                "Colocamos uma placa de vídeo nova mês passado para o outro monitor.",
                "Não aparece nada, a tela apaga e ele já volta ligando.",
                "Só esse. Parado, ele fica ligado o dia inteiro; é só trabalhar que reinicia.",
                "Reinicia sozinho, não precisa nem eu reiniciar." },
            Pergunta.MexeramEmAlgo, "Ninguém mexeu em nada nesse computador.")
            .Ferramenta(Item.TestadorFonte)
            .Quebra(e =>
            {
                e.pcFonteFraca = true;
                e.saudeFonte = 0.12f;
            }),

        new Problema(TipoEquipamento.Computador, TrocarMemoriaComErro,
            "O computador dá tela azul de vez em quando.",
            "Do nada aparece uma tela azul com umas letras e ele reinicia.",
            new[] {
                "Umas três semanas. Às vezes uma vez por dia, às vezes nenhuma.",
                "Nada, só atualizações do sistema mesmo.",
                "Tela azul. Já apareceu MEMORY_MANAGEMENT e outra vez um nome diferente.",
                "Só esse. E os arquivos às vezes abrem corrompidos.",
                "Reinicio e volta a funcionar... até a próxima." },
            Pergunta.MensagemErro, "Não aparece mensagem nenhuma, ele só trava.")
            .Ferramenta(Item.PendriveDiagnostico)
            .Quebra(e =>
            {
                e.pcMemoriaIntermitente = true;
                e.saudeMemoria = 0.2f;
            }),

        new Problema(TipoEquipamento.Computador, "Trocar pasta térmica",
            "O computador esquenta muito e desliga sozinho.",
            "Ele fica pelando e apaga, mesmo depois da limpeza.",
            new[] {
                "Faz uns meses, foi piorando aos poucos.",
                "Outro técnico limpou por dentro mês passado e não adiantou.",
                "Não aparece mensagem, ele simplesmente apaga.",
                "Só esse; é o mais antigo, tem uns oito anos.",
                "Liga de novo depois de uns minutos, e apaga outra vez." },
            Pergunta.MexeramEmAlgo, "Ninguém nunca abriu esse computador.")
            .Quebra(e =>
            {
                e.pcPastaSeca = true;
                e.pcPoeira = Random.Range(0.05f, 0.25f);   // está limpo: não é poeira
            }),

        new Problema(TipoEquipamento.Computador, "Trocar HD",
            "O computador está muito lento e travando.",
            "Demora uns dez minutos pra ligar e fica travando tudo.",
            new[] {
                "Faz umas duas semanas, e cada dia pior.",
                "Não mexemos; ele caiu da mesa uma vez, mas faz tempo.",
                "Às vezes aparece \"Verificando o disco\" quando liga.",
                "Só esse computador. E dá para ouvir um clique-clique vindo dele.",
                "Reiniciar não ajuda, demora o mesmo tanto." },
            Pergunta.MexeramEmAlgo, "Nunca aconteceu nada com ele.")
            .Ferramenta(Item.PendriveDiagnostico)
            .Quebra(e =>
            {
                e.pcHdRuim = true;
                e.saudeDisco = 0.07f;
            }),

        new Problema(TipoEquipamento.Roteador, "Resetar para padrão",
            "Roteador desconfigurado, ninguém acessa a internet.",
            "A internet do escritório inteiro morreu.",
            new[] {
                "Desde que o sobrinho do chefe \"deu uma olhada\" no roteador.",
                "Ele trocou um monte de coisa e não lembra o quê.",
                "O navegador diz \"Sem conexão com a internet\".",
                "O escritório inteiro.",
                "Já tiramos da tomada, não voltou." },
            Pergunta.MexeramEmAlgo, "Ninguém encostou nele!")
            .SemInternet()
            .Quebra(e =>
            {
                e.rotConfigOk = false;
                e.rotSenhaDesconhecida = true;
            }),

        new Problema(TipoEquipamento.Roteador, "Reiniciar roteador",
            "A internet cai para todo mundo a cada 10 minutos.",
            "A internet vai e volta, vai e volta...",
            new[] {
                "Desde a queda de energia de ontem.",
                "Não, ele voltou sozinho depois da queda de luz.",
                "Cai e volta sozinha em uns minutos.",
                "Todo mundo ao mesmo tempo.",
                "Não, ele voltou sozinho depois da queda e ficou assim." },
            Pergunta.JaReiniciou, "Já reiniciamos, claro.")
            .SemInternet()
            .Quebra(e => { e.rotTravado = true; }),

        new Problema(TipoEquipamento.Roteador, "Verificar configurações",
            "A rede Wi-Fi sumiu depois de mexerem no roteador.",
            "O Wi-Fi sumiu do celular de todo mundo.",
            new[] {
                "Desde ontem, quando trocaram a senha do Wi-Fi.",
                "O gerente entrou no painel para trocar a senha.",
                "A rede simplesmente não aparece na lista.",
                "Quem usa cabo está funcionando. Só o Wi-Fi sumiu.",
                "Reiniciamos, continua sem Wi-Fi." },
            Pergunta.MexeramEmAlgo, "Não mexemos em nada, o Wi-Fi sumiu sozinho.")
            .Remoto()
            .Quebra(e => { e.wifiAtivo = false; }),

        new Problema(TipoEquipamento.Roteador, "Verificar IP",
            "Os computadores pegam IP errado e não navegam.",
            "Os computadores conectam, mas não abre nada.",
            new[] {
                "Desde que trocaram o modem da operadora.",
                "O técnico da operadora instalou um modem novo.",
                "\"Rede não identificada\".",
                "Todo mundo que ligou o computador hoje.",
                "Reiniciamos os computadores, nada." },
            Pergunta.MexeramEmAlgo, "Nada mudou por aqui.")
            .SemInternet()
            .Quebra(e =>
            {
                e.dhcpDuplo = true;
                e.modemEmBridge = false;
                e.pcIpErrado = true;
            }),

        // ---------- Etapa 7: software (tela do cliente) ----------
        new Problema(TipoEquipamento.Computador, RemoverVirus,
            "Aparecem propagandas sem parar e o navegador abre um site estranho.",
            "O computador está cheio de anúncio pulando na tela, acho que pegou vírus.",
            new[] {
                "Desde que baixei um conversor de PDF grátis na semana passada.",
                "Baixei um programinha para juntar PDF, cliquei em \"avançar\" em tudo.",
                "\"PARABÉNS! Você ganhou um celular\" e outras janelas pulando.",
                "Só no meu; e agora o computador está bem mais lento.",
                "Reiniciei e as propagandas voltam sozinhas." },
            Pergunta.MexeramEmAlgo, "Não baixei nada, apareceu do nada.")
            .Remoto("Pelo acesso remoto: varredura completa, ameaças em quarentena, programa de propaganda desinstalado e página inicial restaurada.")
            .Quebra(e => { e.swAmeacas = true; e.swProgramaPropaganda = true; e.swPaginaTrocada = true; e.swVarreduraFeita = false; e.memoria = 0.7f; }),

        new Problema(TipoEquipamento.Computador, DesfazerAtualizacao,
            "Depois de uma atualização, o sistema da contabilidade não abre mais.",
            "O programa das notas não abre, dá um erro esquisito de banco de dados.",
            new[] {
                "Desde hoje cedo. O computador ficou atualizando ontem à noite.",
                "Ninguém mexeu, ele atualizou sozinho na hora de desligar.",
                "\"Erro 0x80 — componente de banco de dados incompatível. Contate o suporte.\"",
                "Os outros dois computadores ainda não atualizaram e estão funcionando.",
                "Reiniciei três vezes, o erro continua." },
            Pergunta.DesdeQuando, "Faz uns dias, não sei dizer.")
            .Remoto("Pelo acesso remoto: desinstalou a atualização TIOS-2611, ocultou ela e pausou as atualizações até a correção do fabricante.")
            .Causa(e => e.swAtualizacaoVaiVoltar ? "A atualização reinstalou sozinha de madrugada e o sistema contábil parou de novo" : null)
            .Quebra(e => { e.swAtualizacaoRuim = true; e.swAtualizacaoVaiVoltar = true; }),

        // ---------- Etapa 7: notebook ----------
        new Problema(TipoEquipamento.Notebook, TrocarBateriaEstufada,
            "O touchpad do notebook está estufado e ele balança na mesa.",
            "O notebook está meio inchado embaixo, o mouse dele está levantando.",
            new[] {
                "Começou faz umas duas semanas, foi aumentando.",
                "Ele fica na tomada o dia inteiro, nunca tiro.",
                "Não dá mensagem; às vezes o touchpad clica sozinho.",
                "É o notebook da gerente.",
                "Reiniciei, mas o estufado continua." },
            Pergunta.MexeramEmAlgo, "Ninguém deixou ele na tomada, eu sempre tiro.")
            .Quebra(e => { e.nbBateriaEstufada = true; e.nbCarga = Random.Range(0.1f, 0.3f); }),

        new Problema(TipoEquipamento.Notebook, TrocarDobradica,
            "A tela do notebook está bamba e não para em pé.",
            "A tampa do notebook está caindo para trás, vai quebrar.",
            new[] {
                "Desde que ele caiu da mesa, faz uns dez dias.",
                "Ele caiu aberto, pela tela. Fechei com força para ver se encaixava.",
                "Não dá mensagem; faz um estalo quando abro.",
                "É o notebook que a gente leva para as visitas.",
                "Não adianta reiniciar, o problema é a tampa." },
            Pergunta.MexeramEmAlgo, "Ele não caiu, é defeito de fábrica.")
            .Quebra(e => { e.nbDobradicaQuebrada = true; }),

        new Problema(TipoEquipamento.Notebook, RemoverVirus,
            "O notebook está cheio de propaganda e o navegador abre sozinho num site de ofertas.",
            "O notebook fica abrindo anúncio, deve ser vírus.",
            new[] {
                "Desde que instalei um programa para baixar vídeos.",
                "Meu filho instalou um programa de vídeos nele no fim de semana.",
                "Pula uma janela \"Seu notebook está lento! Clique aqui\".",
                "Só nesse notebook.",
                "Reiniciei, as janelas voltam." },
            Pergunta.MexeramEmAlgo, "Ninguém instalou nada nele.")
            .Remoto("Pelo acesso remoto: varredura completa, ameaças em quarentena, programa de propaganda desinstalado e página inicial restaurada.")
            .Quebra(e => { e.swAmeacas = true; e.swProgramaPropaganda = true; e.swPaginaTrocada = true; e.swVarreduraFeita = false; }),
    };

    public static Problema Sortear(DificuldadeChamado dif)
    {
        var lista = dif == DificuldadeChamado.Facil ? Faceis : dif == DificuldadeChamado.Medio ? Medios : Dificeis;
        return lista[Random.Range(0, lista.Length)];
    }

    // Sorteia um problema que o técnico consegue atender: o check-up só aparece para quem tem as ferramentas
    public static Problema SortearDisponivel(DificuldadeChamado dif, bool podeCheckup)
    {
        for (int i = 0; i < 12; i++)
        {
            var p = Sortear(dif);
            if (!EhCheckup(p) || podeCheckup) return p;
        }
        var lista = dif == DificuldadeChamado.Facil ? Faceis : dif == DificuldadeChamado.Medio ? Medios : Dificeis;
        foreach (var p in lista) if (!EhCheckup(p)) return p;
        return lista[0];
    }

    // ---------------- Clientes ----------------

    static readonly string[] contatos = {
        "Sr. Carlos", "Dona Márcia", "Sr. Roberto", "Fernanda", "Seu Jorge", "Patrícia",
        "Dr. Henrique", "Cláudia", "Marcos", "Dona Lúcia", "Renato", "Simone" };

    // Acha um problema do catálogo pelo equipamento e pela solução (retornos salvos no save)
    public static Problema Buscar(TipoEquipamento equipamento, string solucao)
    {
        foreach (var lista in new[] { Faceis, Medios, Dificeis })
            foreach (var p in lista) if (p.equipamento == equipamento && p.solucao == solucao) return p;
        foreach (var p in new[] { InstalacaoLocacao, VisitaLocacao, OrganizacaoRack, CabeamentoPontos })
            if (p.equipamento == equipamento && p.solucao == solucao) return p;
        return null;
    }

    public static string SortearContato() => contatos[Random.Range(0, contatos.Length)];

    // Fase 1: tranquilos, apressados e confusos. Fase 2: entram os mentirosos. Fase 3: VIPs.
    public static Personalidade SortearPersonalidade(int fase)
    {
        float r = Random.value;
        if (fase <= 1)
            return r < 0.5f ? Personalidade.Tranquilo : r < 0.75f ? Personalidade.Apressado : Personalidade.Confuso;
        if (fase == 2)
            return r < 0.35f ? Personalidade.Tranquilo : r < 0.55f ? Personalidade.Apressado :
                   r < 0.75f ? Personalidade.Confuso : Personalidade.Mentiroso;
        return r < 0.25f ? Personalidade.Tranquilo : r < 0.45f ? Personalidade.Apressado :
               r < 0.6f ? Personalidade.Confuso : r < 0.8f ? Personalidade.Mentiroso : Personalidade.VIP;
    }

    public static string Saudacao(Personalidade p, string problemaDito)
    {
        switch (p)
        {
            case Personalidade.Apressado: return $"Finalmente! Tenho reunião daqui a pouco. {problemaDito} Resolve rápido, por favor.";
            case Personalidade.Confuso:   return $"Oi... então, não sei explicar direito. {problemaDito}";
            case Personalidade.Mentiroso: return $"Olha, já vou avisando que eu não fiz nada, tá? {problemaDito}";
            case Personalidade.VIP:       return $"Sou o diretor aqui e espero um atendimento à altura. {problemaDito}";
            default:                      return $"Bom dia! Obrigado por vir. {problemaDito}";
        }
    }

    public static string Impaciencia() =>
        new[] { "Dá pra ser mais rápido?", "Mais pergunta? Eu tô com pressa!", "Isso vai demorar muito?" }[Random.Range(0, 3)];
}
