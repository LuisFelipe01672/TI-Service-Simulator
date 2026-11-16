using UnityEngine;

// Etapa 7: programas do TI OS do cliente para os problemas de software — tela de login bloqueada, contas de usuário,
// e-mail, antivírus, programas instalados, atualizações, sistema contábil, inicialização e impressora padrão.
// A regra fica em EstadoEquipamentos.Software.cs; aqui só a tela e os cliques.
public partial class GameUI
{
    static MissionManager MMpc => MissionManager.Instance;

    // ---------- Login bloqueado ----------

    void MontarLogin(EstadoEquipamentos e)
    {
        TextoPc($"<b>{e.swUsuario}</b>", 34, true);
        TextoPc("<color=#B23A48>A conta foi bloqueada. Muitas tentativas de senha incorreta.</color>", 24);
        TextoPc("Tente mais tarde ou fale com o administrador.", 20);
        TextoPc("Outro usuário: <b>Administrador</b> (conta local do técnico; a senha está na ficha do cliente)", 20);
        BotaoPc(LinhaPc(), "Entrar como Administrador", () => avisoPc = e.EntrarComoAdministrador(), null, 360);
    }

    // ---------- Contas de usuário ----------

    void MontarContas(EstadoEquipamentos e)
    {
        TextoPc("Contas de usuário", 28, true);
        TextoPc($"<b>{e.swUsuario}</b> — {(e.swContaBloqueada ? "<color=#B23A48>Bloqueada</color>" : "<color=#1E8E3E>Ativa</color>")}" +
                (e.swSenhaRedefinida ? "   ·   senha redefinida (trocar no próximo acesso)" : ""), 22);
        TextoPc("<b>Administrador</b> — conta local do técnico", 20);
        if (!e.swContaBloqueada) { TextoPc("Nenhuma conta com problema.", 20); return; }
        var l = LinhaPc();
        BotaoPc(l, "Redefinir a senha (o cliente digita)", () => avisoPc = e.RedefinirSenha(), null, 400);
        BotaoPc(l, "Desbloquear a conta e sair", () =>
        {
            avisoPc = e.DesbloquearConta();
            if (!e.swContaBloqueada) { appAtual = App.Inicio; MMpc?.Verificar(); }
        }, UIFactory.Hex("#2D8A5F"), 340);
    }

    // ---------- E-mail ----------

    void MontarEmail(EstadoEquipamentos e)
    {
        TextoPc("E-mail", 28, true);
        TextoPc($"Caixa de entrada (12)   ·   Caixa de saída <b>({e.swEmailPresos})</b>" +
                (e.swEmailPresos > 0 ? "   <color=#B23A48>Não foi possível enviar</color>" : ""), 22);
        for (int i = 0; i < Mathf.Min(e.swEmailPresos, 4); i++)
            TextoPc($"   » {new[] { "Proposta_cliente.pdf", "Boleto_novembro.pdf", "Re: orçamento", "Nota fiscal 1532", "Fotos da obra", "Contrato assinado" }[i]}  —  <color=#B23A48>na caixa de saída</color>", 19);
        TextoPc("<b>Configurações da conta</b>   ·   Recebimento: servidor do provedor, porta 995 (seguro)", 21);
        TextoPc($"Servidor de saída: smtp.provedor.com.br   ·   Porta: <b>{e.swEmailPorta}</b>   ·   " +
                $"Meu servidor de saída requer autenticação: <b>{(e.swEmailAutenticacao ? "Sim" : "Não")}</b>", 20);
        var l = LinhaPc();
        BotaoPc(l, e.swEmailPorta == 587 ? "Porta 25 (sem criptografia)" : "Porta 587 (envio seguro)",
            () => { e.swEmailPorta = e.swEmailPorta == 587 ? 25 : 587; avisoPc = $"Porta do servidor de saída: {e.swEmailPorta}."; }, UIFactory.Hex("#6B7A90"), 320);
        BotaoPc(l, e.swEmailAutenticacao ? "Desmarcar autenticação" : "Marcar \"requer autenticação\"",
            () => { e.swEmailAutenticacao = !e.swEmailAutenticacao; avisoPc = "Configuração da conta salva."; }, UIFactory.Hex("#6B7A90"), 340);
        BotaoPc(l, "Enviar e receber", () =>
        {
            avisoPc = e.EnviarEReceber();
            if (e.swEmailPresos == 0) MMpc?.Verificar();
        }, null, 220);
    }

    // ---------- Antivírus ----------

    void MontarAntivirus(EstadoEquipamentos e)
    {
        TextoPc("Escudo Antivírus", 28, true);
        TextoPc(e.swAmeacas ? "<color=#B23A48>Proteção em tempo real: ameaças ativas</color>" : "<color=#1E8E3E>Você está protegido.</color>", 22);
        var l = LinhaPc();
        BotaoPc(l, "Verificação completa (demora)", () =>
        {
            MMpc?.ConsumirTempo(25f);
            avisoPc = e.VarreduraCompleta();
        }, null, 340);
        if (e.swVarreduraFeita && e.swAmeacas)
            BotaoPc(l, "Colocar as ameaças em quarentena", () => avisoPc = e.Quarentena(), UIFactory.Hex("#B23A48"), 380);
        if (!e.swAmeacas && e.swPrecisaReiniciar) TextoPc("Reinicie o computador para terminar a limpeza.", 20);
    }

    // ---------- Programas instalados ----------

    void MontarProgramas(EstadoEquipamentos e)
    {
        TextoPc("Programas instalados", 28, true);
        foreach (var nome in new[] { "Sistema Contábil Ômega 8.4", "Escudo Antivírus", "Leitor de PDF", "Pacote de escritório" })
            TextoPc("   " + nome, 20);
        if (e.swProgramaPropaganda)
        {
            var l = LinhaPc();
            var t = UIFactory.Texto(l, "   <color=#B23A48>Barra de Ofertas Turbo</color>  ·  instalado há 6 dias  ·  editor desconhecido", 20, UIFactory.Hex("#1A2433"));
            t.gameObject.AddComponent<UnityEngine.UI.LayoutElement>().preferredWidth = 760;
            BotaoPc(l, "Desinstalar", () => { avisoPc = e.DesinstalarPropaganda(); MMpc?.Verificar(); }, UIFactory.Hex("#B23A48"), 180);
        }
        if (e.ProgramasInicioDesnecessarios > 0)
            TextoPc($"   ... e mais {e.ProgramasInicioDesnecessarios} programinhas grátis (veja o Gerenciador de Tarefas » Inicialização).", 19);
    }

    // ---------- Atualizações ----------

    void MontarAtualizacoes(EstadoEquipamentos e)
    {
        TextoPc("Atualizações do sistema", 28, true);
        TextoPc(e.swAtualizacaoVaiVoltar ? "Atualizações automáticas: <b>ligadas</b> (instala ao desligar)" : "Atualizações: <b>pausadas</b>", 21);
        TextoPc("<b>Histórico de atualizações</b>", 21);
        if (e.swAtualizacaoRuim) TextoPc($"   Atualização de qualidade {EstadoEquipamentos.CodigoAtualizacao} — instalada ontem, 22:47", 20);
        TextoPc("   Definições do antivírus — instalada hoje", 20);
        TextoPc("   Atualização de segurança TIOS-2603 — instalada há 3 semanas", 20);
        var l = LinhaPc();
        if (e.swAtualizacaoRuim)
            BotaoPc(l, $"Desinstalar a {EstadoEquipamentos.CodigoAtualizacao}", () => avisoPc = e.DesinstalarAtualizacao(), UIFactory.Hex("#B23A48"), 340);
        if (e.swAtualizacaoVaiVoltar)
            BotaoPc(l, "Ocultar esta atualização e pausar", () => avisoPc = e.PausarAtualizacao(), null, 360);
        if (e.swPrecisaReiniciar) TextoPc("<color=#C77700>Reinício pendente para concluir.</color>", 20);
    }

    // ---------- Sistema contábil (é onde o cliente vê o problema da atualização) ----------

    void MontarSistemaContabil(EstadoEquipamentos e)
    {
        TextoPc("Sistema Contábil Ômega", 28, true);
        if (!e.SistemaContabilAbre)
        {
            TextoPc(e.swAtualizacaoRuim
                ? "<color=#B23A48><b>Erro 0x80</b> — componente de banco de dados incompatível. Contate o suporte técnico.</color>"
                : "<color=#C77700>O sistema pede para reiniciar o computador antes de abrir.</color>", 22);
            TextoPc("Registro do sistema: \"o componente de dados foi substituído em ontem, 22:47\".", 19);
            return;
        }
        TextoPc("Abriu! Lançamentos de novembro · Notas fiscais · Folha de pagamento", 22);
        TextoPc("O cliente conferiu e as notas de ontem estão todas lá.", 20);
        MMpc?.Verificar();
    }

    // ---------- Gerenciador de Tarefas » Inicialização ----------

    void MontarInicializacao(EstadoEquipamentos e)
    {
        TextoPc($"<b>Inicialização</b> ({e.swInicializacao.Count} programas; {e.swInicializacao.FindAll(p => p.ativo).Count} habilitados)" +
                (e.InicializacaoCheia ? "   <color=#B23A48>tempo até ficar usável: 9 min</color>" : "   tempo até ficar usável: 40 s"), 21);
        for (int i = 0; i < e.swInicializacao.Count; i++)
        {
            var p = e.swInicializacao[i];
            var l = LinhaPc();
            var t = UIFactory.Texto(l, $"{p.nome}  ·  impacto {p.impacto}  ·  {(p.ativo ? "Habilitado" : "<color=#6B7A90>Desabilitado</color>")}",
                                    18, UIFactory.Hex("#1A2433"));
            t.gameObject.AddComponent<UnityEngine.UI.LayoutElement>().preferredWidth = 760;
            BotaoPc(l, p.ativo ? "Desabilitar" : "Habilitar", () =>
            {
                avisoPc = e.AlternarInicio(p);
                if (p.essencial && !p.ativo) MMpc?.RegistrarErro($"Você desabilitou \"{p.nome}\" na inicialização: isso o cliente precisa!", 1f);
            }, p.ativo ? UIFactory.Hex("#6B7A90") : null, 170);
        }
        if (e.swPrecisaReiniciar) TextoPc("<color=#C77700>Reinicie para valer.</color>", 19);
    }

    // ---------- Impressoras » padrão ----------

    void MontarImpressoraPadrao(EstadoEquipamentos e)
    {
        TextoPc($"Impressora padrão: <b>{(e.swPadraoErrado ? "Imprimir em arquivo PDF" : "Impressora do escritório")}</b>" +
                $"   ·   Deixar o sistema gerenciar a padrão: <b>{(e.swGerenciarPadraoAuto ? "Ligado" : "Desligado")}</b>", 20);
        var l = LinhaPc();
        if (e.swPadraoErrado)
            BotaoPc(l, "Definir \"Impressora do escritório\" como padrão", () => avisoPc = e.DefinirPadraoEscritorio(), null, 480);
        BotaoPc(l, e.swGerenciarPadraoAuto ? "Desligar \"gerenciar a padrão\"" : "Ligar \"gerenciar a padrão\"",
            () => avisoPc = e.AlternarGerenciarPadrao(), UIFactory.Hex("#6B7A90"), 340);
    }
}
