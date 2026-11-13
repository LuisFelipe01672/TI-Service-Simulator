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
    public string solucao;          // precisa bater com Equipamento.GetOpcoes()
    public string descricao;        // como um cliente normal descreve
    public string descricaoVaga;    // como um cliente confuso descreve
    public string[] respostas;      // uma por Pergunta (mesma ordem do enum)
    public Pergunta perguntaMentira;
    public string mentira;          // o que o Mentiroso responde nessa pergunta
    public string[] testes;         // um resultado por teste do equipamento (mesma ordem de CatalogoProblemas.Testes)

    public Problema(TipoEquipamento equipamento, string solucao, string descricao, string descricaoVaga,
                    string[] respostas, Pergunta perguntaMentira, string mentira, string[] testes)
    {
        this.equipamento = equipamento;
        this.solucao = solucao;
        this.descricao = descricao;
        this.descricaoVaga = descricaoVaga;
        this.respostas = respostas;
        this.perguntaMentira = perguntaMentira;
        this.mentira = mentira;
        this.testes = testes;
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

    // ---------------- Testes de cada equipamento ----------------

    static readonly string[] testesImpressora = {
        "Olhar o painel da impressora", "Abrir a fila de impressão", "Imprimir página de teste", "Conferir cabo e conexão" };
    static readonly string[] testesComputador = {
        "Olhar o ícone de rede", "Rodar ipconfig", "Abrir o Gerenciador de Dispositivos", "Abrir o Gerenciador de Tarefas" };
    static readonly string[] testesRoteador = {
        "Olhar as luzes do roteador", "Entrar no painel do roteador", "Testar ping para a internet", "Ver o log do roteador" };

    // Resultado quando o problema NÃO está nesse equipamento
    static readonly string[] normalImpressora = {
        "Visor mostra \"Pronta\". Nenhum erro.", "Fila vazia, nada travado.", "A página de teste sai perfeita.", "Cabo firme e conectado." };
    static readonly string[] normalComputador = {
        "Rede conectada, sem avisos.", "IP 192.168.0.23, o roteador responde.", "Todos os dispositivos funcionando.", "Uso de CPU e memória normal." };
    static readonly string[] normalRoteador = {
        "Todas as luzes verdes e estáveis.", "Configurações normais.", "Ping ok, 15 ms.", "Nenhum evento estranho no log." };

    public static string[] Testes(TipoEquipamento tipo) =>
        tipo == TipoEquipamento.Impressora ? testesImpressora :
        tipo == TipoEquipamento.Roteador   ? testesRoteador : testesComputador;

    public static string ResultadoNormal(TipoEquipamento tipo, int indice)
    {
        var lista = tipo == TipoEquipamento.Impressora ? normalImpressora :
                    tipo == TipoEquipamento.Roteador   ? normalRoteador : normalComputador;
        return lista[Mathf.Clamp(indice, 0, lista.Length - 1)];
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
            Pergunta.MensagemErro, "Mensagem? Não reparei em nenhuma.",
            new[] {
                "Visor mostra \"Pronta\". Nenhum erro na impressora.",
                "A fila nem abre: \"O spooler de impressão não está em execução\".",
                "Página de teste direto pela impressora sai perfeita.",
                "Cabo firme, luz de rede piscando normal." }),

        new Problema(TipoEquipamento.Impressora, "Verificar cabo",
            "Impressora aparece offline no sistema.",
            "A impressora sumiu do computador, acho.",
            new[] {
                "Desde ontem, depois que a equipe de limpeza passou aqui.",
                "A limpeza arrastou o armário onde ela fica.",
                "Só diz \"Impressora offline\".",
                "Ninguém consegue usar ela.",
                "Reiniciei o computador, continuou offline." },
            Pergunta.MexeramEmAlgo, "Ninguém mexeu em nada aqui, juro.",
            new[] {
                "Ícone de rede com um X no visor: \"Rede desconectada\".",
                "Documentos parados com status \"Offline\".",
                "A página de teste sai normal: a impressora em si funciona.",
                "O cabo de rede está meio solto atrás da impressora!" }),

        new Problema(TipoEquipamento.Impressora, "Verificar fila de impressão",
            "Um documento travou e nada mais imprime.",
            "Mandei imprimir, ficou lá parado... e agora nada sai.",
            new[] {
                "Desde que mandei um PDF de 300 páginas de manhã.",
                "Não mexi em nada, só mandei imprimir.",
                "No documento aparece \"Erro - imprimindo\".",
                "Todo mundo, porque a impressora é uma só.",
                "Não, achei melhor não mexer." },
            Pergunta.DesdeQuando, "Sei lá, parou do nada.",
            new[] {
                "Visor mostra \"Pronta\", sem erros.",
                "Um PDF de 300 páginas travado em \"Erro\", com 14 documentos esperando atrás dele.",
                "A página de teste sai normal.",
                "Cabo ok, bem conectado." }),

        new Problema(TipoEquipamento.Impressora, "Trocar toner",
            "A impressão está saindo clara e falhada.",
            "As folhas estão saindo meio... fantasma?",
            new[] {
                "Vem piorando faz umas duas semanas.",
                "Alguém sacudiu o toner pra ele \"render mais\".",
                "Às vezes pisca \"Toner baixo\", mas a gente ignora.",
                "Sai assim para todo mundo.",
                "Reiniciar não muda nada, sai clara igual." },
            Pergunta.MensagemErro, "Nunca apareceu mensagem nenhuma.",
            new[] {
                "Aviso no visor: \"Toner baixo - 3%\".",
                "Fila limpa, os trabalhos saem.",
                "A página de teste sai clarinha, cheia de faixas brancas.",
                "Cabo ok." }),
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
            Pergunta.MexeramEmAlgo, "Ninguém mudou nada de lugar.",
            new[] {
                "Ícone de rede com um X vermelho: \"Cabo desconectado\".",
                "Adaptador Ethernet: \"Mídia desconectada\".",
                "Placa de rede instalada e funcionando.",
                "Uso de CPU e memória normal." }),

        new Problema(TipoEquipamento.Computador, "Verificar IP",
            "Aparece \"conflito de IP\" e a rede não conecta.",
            "Aparece um aviso estranho de... IP? E não conecta.",
            new[] {
                "Desde que o estagiário \"arrumou\" a rede no meu PC.",
                "O estagiário colocou um número fixo aí.",
                "\"Outro dispositivo na rede está usando o seu endereço IP\".",
                "A impressora também começou a falhar.",
                "Reiniciei, voltou o mesmo aviso." },
            Pergunta.MexeramEmAlgo, "Ninguém mexeu nas configurações.",
            new[] {
                "Ícone com triângulo amarelo: \"Sem internet\".",
                "IP fixo 192.168.0.50... o mesmo endereço da impressora!",
                "Placa de rede funcionando.",
                "Nada fora do normal." }),

        new Problema(TipoEquipamento.Computador, "Reinstalar driver",
            "A placa de rede sumiu depois de uma atualização.",
            "O computador esqueceu que tem internet.",
            new[] {
                "Desde a atualização do Windows de ontem à noite.",
                "Só deixei atualizar, como o aviso pediu.",
                "Nem aparece o ícone de rede direito.",
                "Só o meu.",
                "Reiniciei umas três vezes." },
            Pergunta.DesdeQuando, "Faz tempo, nem lembro.",
            new[] {
                "Ícone de rede: \"Nenhum adaptador encontrado\".",
                "Nenhum adaptador Ethernet aparece na lista.",
                "Placa de rede com triângulo amarelo: \"Driver com problema (código 10)\".",
                "Tudo normal." }),

        new Problema(TipoEquipamento.Computador, "Reiniciar equipamento",
            "O computador está travando e lento.",
            "Ele está muito devagar, travando tudo.",
            new[] {
                "Está ligado há umas três semanas sem desligar.",
                "Não, só deixo ligado pra não perder as abas.",
                "Às vezes: \"O programa não está respondendo\".",
                "Só o meu.",
                "Reiniciar? Nunca, tenho medo de perder as coisas." },
            Pergunta.JaReiniciou, "Claro, reinicio todo dia.",
            new[] {
                "Rede conectada normalmente.",
                "IP ok, rede ok.",
                "Todos os dispositivos funcionando.",
                "Memória em 98% e o computador ligado há 21 dias sem reiniciar." }),
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
            Pergunta.MexeramEmAlgo, "Ninguém encostou nele!",
            new[] {
                "Luz de internet acesa, Wi-Fi aceso.",
                "A senha do painel foi trocada e ninguém sabe a nova.",
                "Ping falha em todos os computadores.",
                "\"Configuração alterada\" várias vezes ontem, com regras de bloqueio estranhas." }),

        new Problema(TipoEquipamento.Roteador, "Reiniciar roteador",
            "A internet cai para todo mundo a cada 10 minutos.",
            "A internet vai e volta, vai e volta...",
            new[] {
                "Desde a queda de energia de ontem.",
                "Não, ele voltou sozinho depois da queda de luz.",
                "Cai e volta sozinha em uns minutos.",
                "Todo mundo ao mesmo tempo.",
                "Não, ele voltou sozinho depois da queda e ficou assim." },
            Pergunta.JaReiniciou, "Já reiniciamos, claro.",
            new[] {
                "A luz de internet pisca laranja de vez em quando.",
                "O painel abre, as configurações parecem normais.",
                "O ping falha 1 a cada 10 vezes e depois volta.",
                "Desde a queda de energia: \"Falha na sessão\" repetida e memória cheia." }),

        new Problema(TipoEquipamento.Roteador, "Verificar configurações",
            "A rede Wi-Fi sumiu depois de mexerem no roteador.",
            "O Wi-Fi sumiu do celular de todo mundo.",
            new[] {
                "Desde ontem, quando trocaram a senha do Wi-Fi.",
                "O gerente entrou no painel para trocar a senha.",
                "A rede simplesmente não aparece na lista.",
                "Quem usa cabo está funcionando. Só o Wi-Fi sumiu.",
                "Reiniciamos, continua sem Wi-Fi." },
            Pergunta.MexeramEmAlgo, "Não mexemos em nada, o Wi-Fi sumiu sozinho.",
            new[] {
                "Luz de internet ok, mas a luz do Wi-Fi está apagada.",
                "Rede sem fio: DESATIVADA.",
                "Pelo cabo, o ping funciona normal.",
                "\"Wi-Fi desativado pelo administrador\" ontem às 17h." }),

        new Problema(TipoEquipamento.Roteador, "Verificar IP",
            "Os computadores pegam IP errado e não navegam.",
            "Os computadores conectam, mas não abre nada.",
            new[] {
                "Desde que trocaram o modem da operadora.",
                "O técnico da operadora instalou um modem novo.",
                "\"Rede não identificada\".",
                "Todo mundo que ligou o computador hoje.",
                "Reiniciamos os computadores, nada." },
            Pergunta.MexeramEmAlgo, "Nada mudou por aqui.",
            new[] {
                "Todas as luzes acesas normalmente.",
                "DHCP entregando IPs de outra faixa (10.0.0.x).",
                "Ping para a internet falha, e para o roteador também.",
                "\"Conflito de DHCP\": dois equipamentos entregando IP na mesma rede." }),
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
