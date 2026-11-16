using System.Collections.Generic;

// Um programa que abre junto com o sistema (Gerenciador de Tarefas > Inicialização)
[System.Serializable]
public class ProgramaInicio
{
    public string nome;
    public string impacto;      // "Alto", "Médio", "Baixo"
    public bool essencial;      // antivírus, áudio, o próprio sistema contábil: desativar faz falta
    public bool ativo = true;
    public ProgramaInicio(string nome, string impacto, bool essencial) { this.nome = nome; this.impacto = impacto; this.essencial = essencial; }
}

// Etapa 7: o sistema (TI OS) do computador e do notebook do cliente — conta, e-mail, vírus, inicialização,
// atualização e impressora padrão. Tudo começa certo; cada problema de software estraga uma parte.
// A tela do cliente (GameUI.Computador) mostra e conserta; o monitor 3D mostra a pista (EquipamentoPecas.Software).
public partial class EstadoEquipamentos
{
    // ---------- Conta do usuário ----------
    public bool swContaBloqueada;             // errou a senha 5 vezes: tela de login bloqueada
    public bool swAdminLogado;                // o técnico entrou com a conta de administrador
    public bool swSenhaRedefinida;            // o cliente já criou a senha nova
    public string swUsuario = "recepcao";

    // ---------- E-mail ----------
    public int swEmailPresos;                 // mensagens paradas na caixa de saída
    public int swEmailPorta = 587;            // porta do servidor de saída (25 é bloqueada pelo provedor)
    public bool swEmailAutenticacao = true;   // "meu servidor de saída requer autenticação"
    public bool EmailConfigCerta => swEmailPorta == 587 && swEmailAutenticacao;

    // ---------- Vírus / propaganda ----------
    public bool swAmeacas;                    // o antivírus ainda não colocou as ameaças em quarentena
    public bool swVarreduraFeita;
    public bool swProgramaPropaganda;         // "Barra de Ofertas Turbo" instalada junto com um programa grátis
    public bool swPaginaTrocada;              // página inicial do navegador sequestrada
    public bool swPopups => swAmeacas || swProgramaPropaganda;

    // ---------- Inicialização ----------
    public List<ProgramaInicio> swInicializacao = InicializacaoPadrao();
    public bool swPrecisaReiniciar;           // mudança que só vale depois de reiniciar

    // ---------- Atualização que quebrou o sistema contábil ----------
    public bool swAtualizacaoRuim;            // a atualização TIOS-2611 trocou o componente de banco de dados
    public bool swAtualizacaoVaiVoltar;       // sem pausar/ocultar, ela reinstala sozinha à noite
    public const string CodigoAtualizacao = "TIOS-2611";

    // ---------- Impressora padrão ----------
    public bool swPadraoErrado;               // a padrão virou "Imprimir em arquivo PDF"
    public bool swGerenciarPadraoAuto;        // o sistema troca a padrão sozinho (a última usada)

    public static List<ProgramaInicio> InicializacaoPadrao() => new List<ProgramaInicio>
    {
        new ProgramaInicio("Escudo Antivírus", "Médio", true),
        new ProgramaInicio("Driver de áudio", "Baixo", true),
        new ProgramaInicio("Sistema Contábil Ômega (serviço)", "Médio", true),
    };

    // Lixo que vai se acumulando: cada programa grátis instalado põe mais um na inicialização
    public static readonly string[] ProgramasInuteis =
    {
        "Atualizador de impressora antiga", "Chat de vídeo", "Nuvem de fotos", "Assistente de jogos", "Otimizador Turbo PC",
        "Barra do navegador", "Leitor de PDF (atualizador)", "Mensageiro", "Toolbar de cupons", "Música online",
        "Sincronizador de celular", "Agenda do fabricante",
    };

    public int ProgramasInicioDesnecessarios => swInicializacao.FindAll(p => p.ativo && !p.essencial).Count;
    public bool InicializacaoCheia => ProgramasInicioDesnecessarios > 2;
    public bool SistemaContabilAbre => !swAtualizacaoRuim && !swPrecisaReiniciar;

    public bool SoftwareOk =>
        !swContaBloqueada && swEmailPresos == 0 && !swAmeacas && !swProgramaPropaganda && !swPaginaTrocada &&
        !InicializacaoCheia && !swPrecisaReiniciar && !swAtualizacaoRuim && !swPadraoErrado;

    // ---------- Ações na tela ----------

    public string EntrarComoAdministrador()
    {
        swAdminLogado = true;
        return "Você entrou com a conta de administrador local (senha do cadastro do cliente na sua ficha).";
    }

    public string DesbloquearConta()
    {
        if (!swAdminLogado) return "Só o administrador desbloqueia contas.";
        if (!swSenhaRedefinida) return $"Conta \"{swUsuario}\" desbloqueada, mas o cliente ainda não sabe a senha. Redefina a senha antes.";
        swContaBloqueada = false;
        swAdminLogado = false;
        return $"Conta \"{swUsuario}\" desbloqueada. O cliente entrou com a senha nova e a área de trabalho abriu.";
    }

    public string RedefinirSenha()
    {
        if (!swAdminLogado) return "Só o administrador redefine senhas.";
        swSenhaRedefinida = true;
        return "O cliente digitou a senha nova duas vezes (você virou de costas) e marcou \"pedir para trocar no próximo acesso\". " +
               "Você recomendou não anotar em post-it no monitor.";
    }

    public string EnviarEReceber()
    {
        if (swEmailPresos == 0) return "Enviar/receber: nada na caixa de saída. 2 mensagens novas recebidas.";
        if (!PcNaInternetOuNotebook) return "Erro: sem conexão com o servidor.";
        if (swEmailPorta != 587) return $"Erro ao enviar {swEmailPresos} mensagem(ns): o servidor de saída não respondeu na porta {swEmailPorta} (bloqueada pelo provedor).";
        if (!swEmailAutenticacao) return $"Erro ao enviar {swEmailPresos} mensagem(ns): 530 - o servidor de saída exige autenticação.";
        int n = swEmailPresos;
        swEmailPresos = 0;
        return $"{n} mensagem(ns) enviada(s)! A caixa de saída esvaziou.";
    }

    // O notebook está no Wi-Fi do escritório: depende só do roteador
    public bool PcNaInternetOuNotebook => PcNaInternet || InternetOk;

    public string VarreduraCompleta()
    {
        swVarreduraFeita = true;
        if (!swAmeacas && !swProgramaPropaganda) return "Verificação completa: nenhuma ameaça encontrada.";
        return swAmeacas
            ? "Verificação completa: <color=#B23A48>3 ameaças encontradas</color> — Adware.OfertaPop, Sequestrador.PaginaInicial e Trojan.Minerador (na pasta Temp)."
            : "Verificação completa: as ameaças já estão em quarentena. Mas um programa indesejado continua instalado (Barra de Ofertas Turbo).";
    }

    public string Quarentena()
    {
        if (!swVarreduraFeita) return "Rode a verificação completa antes.";
        if (!swAmeacas) return "Nada para colocar em quarentena.";
        swAmeacas = false;
        swPrecisaReiniciar = true;
        return "3 ameaças em quarentena. Reinicie o computador para terminar a limpeza.";
    }

    public string DesinstalarPropaganda()
    {
        if (!swProgramaPropaganda) return "Programa não encontrado.";
        swProgramaPropaganda = false;
        return "\"Barra de Ofertas Turbo\" desinstalada. (Veio escondida no instalador de um conversor de PDF grátis.)";
    }

    public string RestaurarPaginaInicial()
    {
        swPaginaTrocada = false;
        return "Página inicial e mecanismo de busca do navegador de volta ao padrão.";
    }

    public string AlternarInicio(ProgramaInicio p)
    {
        p.ativo = !p.ativo;
        swPrecisaReiniciar = true;
        return $"{p.nome}: {(p.ativo ? "habilitado" : "desabilitado")} na inicialização. Vale a partir da próxima vez que o sistema ligar.";
    }

    public string DesinstalarAtualizacao()
    {
        if (!swAtualizacaoRuim) return $"A atualização {CodigoAtualizacao} não está instalada.";
        swAtualizacaoRuim = false;
        swPrecisaReiniciar = true;
        return $"Atualização {CodigoAtualizacao} desinstalada. Reinicie para concluir.";
    }

    public string PausarAtualizacao()
    {
        swAtualizacaoVaiVoltar = false;
        return $"A {CodigoAtualizacao} ficou oculta e as atualizações pausadas por 5 semanas (até sair a correção do fabricante do sistema contábil).";
    }

    public string DefinirPadraoEscritorio()
    {
        swPadraoErrado = false;
        return "\"Impressora do escritório\" agora é a impressora padrão.";
    }

    public string AlternarGerenciarPadrao()
    {
        swGerenciarPadraoAuto = !swGerenciarPadraoAuto;
        return swGerenciarPadraoAuto ? "O sistema vai trocar a padrão sozinho (a última usada)." : "Desligado: a padrão só muda se alguém escolher.";
    }

    // Reiniciou o sistema: o que esperava reinício passa a valer; com as atualizações sem pausar, a ruim volta
    void ReiniciarSoftware()
    {
        swPrecisaReiniciar = false;
        swAdminLogado = false;
    }
}
