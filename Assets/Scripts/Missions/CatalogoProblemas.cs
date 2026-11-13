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
}

public static class CatalogoProblemas
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

    static string Chave(Problema p) => p.equipamento + ":" + p.solucao;

    // Estraga os equipamentos do cliente de acordo com o problema (com variações sorteadas)
    public static void Quebrar(Problema p, EstadoEquipamentos e)
    {
        // Distâncias até a tomada de rede variam de cliente para cliente
        e.impCabo.metrosNecessarios = Mathf.Round(Random.Range(1f, 6f) * 2f) / 2f;
        e.pcCabo.metrosNecessarios = Mathf.Round(Random.Range(1f, 4f) * 2f) / 2f;
        e.diasLigado = Random.Range(1, 4);
        e.fila.Clear();

        switch (Chave(p))
        {
            case "Impressora:Reiniciar spooler":
                e.spoolerRodando = false;
                break;
            case "Impressora:Verificar cabo":
                SortearDefeitoCabo(e.impCabo);
                break;
            case "Impressora:Verificar fila de impressão":
                e.fila.Add(new TrabalhoImpressao("Relatorio_anual_FINAL_v3.pdf", 300, true));
                int n = Random.Range(4, 15);
                for (int i = 0; i < n; i++)
                    e.fila.Add(new TrabalhoImpressao(new[] { "Boleto.pdf", "Contrato.docx", "Planilha.xlsx", "Foto.jpg", "Recibo.pdf" }[Random.Range(0, 5)], Random.Range(1, 6)));
                break;
            case "Impressora:Trocar toner":
                e.impToner = Random.Range(0.01f, 0.06f);
                break;
            case "Computador:Verificar cabo de rede":
                SortearDefeitoCabo(e.pcCabo);
                break;
            case "Computador:Verificar IP":
                e.pcIpFixo = true;
                e.pcIp = e.ipImpressora;
                break;
            case "Computador:Reinstalar driver":
                e.driverOk = false;
                break;
            case "Computador:Reiniciar equipamento":
                e.memoria = Random.Range(0.94f, 0.99f);
                e.diasLigado = Random.Range(15, 40);
                break;
            case "Roteador:Resetar para padrão":
                e.rotConfigOk = false;
                e.rotSenhaDesconhecida = true;
                break;
            case "Roteador:Reiniciar roteador":
                e.rotTravado = true;
                break;
            case "Roteador:Verificar configurações":
                e.wifiAtivo = false;
                break;
            case "Roteador:Verificar IP":
                e.dhcpDuplo = true;
                e.modemEmBridge = false;
                e.pcIpErrado = true;
                break;
        }
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
        switch (p.equipamento)
        {
            case TipoEquipamento.Impressora:
                return e.impLigada && !e.impTampaAberta && e.impCartuchoPresente && !e.impCartuchoComLacre && e.impToner >= 0.1f &&
                       e.spoolerRodando && !e.FilaTravada && e.ImpressoraOnline && e.PcNaRede;
            case TipoEquipamento.Computador:
                return e.PcNaInternet && e.memoria < 0.8f;
            default:
                return e.PcNaInternet && e.wifiAtivo && e.rotConfigOk && !e.rotTravado && !e.dhcpDuplo;
        }
    }

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
            Pergunta.MensagemErro, "Mensagem? Não reparei em nenhuma."),

        new Problema(TipoEquipamento.Impressora, "Verificar cabo",
            "Impressora aparece offline no sistema.",
            "A impressora sumiu do computador, acho.",
            new[] {
                "Desde ontem, depois que a equipe de limpeza passou aqui.",
                "A limpeza arrastou o armário onde ela fica.",
                "Só diz \"Impressora offline\".",
                "Ninguém consegue usar ela.",
                "Reiniciei o computador, continuou offline." },
            Pergunta.MexeramEmAlgo, "Ninguém mexeu em nada aqui, juro."),

        new Problema(TipoEquipamento.Impressora, "Verificar fila de impressão",
            "Um documento travou e nada mais imprime.",
            "Mandei imprimir, ficou lá parado... e agora nada sai.",
            new[] {
                "Desde que mandei um PDF de 300 páginas de manhã.",
                "Não mexi em nada, só mandei imprimir.",
                "No documento aparece \"Erro - imprimindo\".",
                "Todo mundo, porque a impressora é uma só.",
                "Não, achei melhor não mexer." },
            Pergunta.DesdeQuando, "Sei lá, parou do nada."),

        new Problema(TipoEquipamento.Impressora, "Trocar toner",
            "A impressão está saindo clara e falhada.",
            "As folhas estão saindo meio... fantasma?",
            new[] {
                "Vem piorando faz umas duas semanas.",
                "Alguém sacudiu o toner pra ele \"render mais\".",
                "Às vezes pisca \"Toner baixo\", mas a gente ignora.",
                "Sai assim para todo mundo.",
                "Reiniciar não muda nada, sai clara igual." },
            Pergunta.MensagemErro, "Nunca apareceu mensagem nenhuma."),
    };

    public static readonly Problema[] Medios = {
        new Problema(TipoEquipamento.Computador, "Verificar cabo de rede",
            "Computador sem acesso à rede.",
            "A internet do meu computador acabou.",
            new[] {
                "Desde que trocaram minha mesa de lugar.",
                "Mudaram minha mesa ontem à tarde.",
                "\"Cabo de rede desconectado\", no cantinho da tela.",
                "Só eu. O pessoal do lado está navegando.",
                "Reiniciei duas vezes." },
            Pergunta.MexeramEmAlgo, "Ninguém mudou nada de lugar."),

        new Problema(TipoEquipamento.Computador, "Verificar IP",
            "Aparece \"conflito de IP\" e a rede não conecta.",
            "Aparece um aviso estranho de... IP? E não conecta.",
            new[] {
                "Desde que o estagiário \"arrumou\" a rede no meu PC.",
                "O estagiário colocou um número fixo aí.",
                "\"Outro dispositivo na rede está usando o seu endereço IP\".",
                "A impressora também começou a falhar.",
                "Reiniciei, voltou o mesmo aviso." },
            Pergunta.MexeramEmAlgo, "Ninguém mexeu nas configurações."),

        new Problema(TipoEquipamento.Computador, "Reinstalar driver",
            "A placa de rede sumiu depois de uma atualização.",
            "O computador esqueceu que tem internet.",
            new[] {
                "Desde a atualização do Windows de ontem à noite.",
                "Só deixei atualizar, como o aviso pediu.",
                "Nem aparece o ícone de rede direito.",
                "Só o meu.",
                "Reiniciei umas três vezes." },
            Pergunta.DesdeQuando, "Faz tempo, nem lembro."),

        new Problema(TipoEquipamento.Computador, "Reiniciar equipamento",
            "O computador está travando e lento.",
            "Ele está muito devagar, travando tudo.",
            new[] {
                "Está ligado há umas três semanas sem desligar.",
                "Não, só deixo ligado pra não perder as abas.",
                "Às vezes: \"O programa não está respondendo\".",
                "Só o meu.",
                "Reiniciar? Nunca, tenho medo de perder as coisas." },
            Pergunta.JaReiniciou, "Claro, reinicio todo dia."),
    };

    public static readonly Problema[] Dificeis = {
        new Problema(TipoEquipamento.Roteador, "Resetar para padrão",
            "Roteador desconfigurado, ninguém acessa a internet.",
            "A internet do escritório inteiro morreu.",
            new[] {
                "Desde que o sobrinho do chefe \"deu uma olhada\" no roteador.",
                "Ele trocou um monte de coisa e não lembra o quê.",
                "O navegador diz \"Sem conexão com a internet\".",
                "O escritório inteiro.",
                "Já tiramos da tomada, não voltou." },
            Pergunta.MexeramEmAlgo, "Ninguém encostou nele!"),

        new Problema(TipoEquipamento.Roteador, "Reiniciar roteador",
            "A internet cai para todo mundo a cada 10 minutos.",
            "A internet vai e volta, vai e volta...",
            new[] {
                "Desde a queda de energia de ontem.",
                "Não, ele voltou sozinho depois da queda de luz.",
                "Cai e volta sozinha em uns minutos.",
                "Todo mundo ao mesmo tempo.",
                "Não, ele voltou sozinho depois da queda e ficou assim." },
            Pergunta.JaReiniciou, "Já reiniciamos, claro."),

        new Problema(TipoEquipamento.Roteador, "Verificar configurações",
            "A rede Wi-Fi sumiu depois de mexerem no roteador.",
            "O Wi-Fi sumiu do celular de todo mundo.",
            new[] {
                "Desde ontem, quando trocaram a senha do Wi-Fi.",
                "O gerente entrou no painel para trocar a senha.",
                "A rede simplesmente não aparece na lista.",
                "Quem usa cabo está funcionando. Só o Wi-Fi sumiu.",
                "Reiniciamos, continua sem Wi-Fi." },
            Pergunta.MexeramEmAlgo, "Não mexemos em nada, o Wi-Fi sumiu sozinho."),

        new Problema(TipoEquipamento.Roteador, "Verificar IP",
            "Os computadores pegam IP errado e não navegam.",
            "Os computadores conectam, mas não abre nada.",
            new[] {
                "Desde que trocaram o modem da operadora.",
                "O técnico da operadora instalou um modem novo.",
                "\"Rede não identificada\".",
                "Todo mundo que ligou o computador hoje.",
                "Reiniciamos os computadores, nada." },
            Pergunta.MexeramEmAlgo, "Nada mudou por aqui."),
    };

    public static Problema Sortear(DificuldadeChamado dif)
    {
        var lista = dif == DificuldadeChamado.Facil ? Faceis : dif == DificuldadeChamado.Medio ? Medios : Dificeis;
        return lista[Random.Range(0, lista.Length)];
    }

    // ---------------- Clientes ----------------

    static readonly string[] contatos = {
        "Sr. Carlos", "Dona Márcia", "Sr. Roberto", "Fernanda", "Seu Jorge", "Patrícia",
        "Dr. Henrique", "Cláudia", "Marcos", "Dona Lúcia", "Renato", "Simone" };

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
