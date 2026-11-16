using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Atalhos de teste da Etapa 5 (locação física: instalação na caixa e visita mensal no cliente).
// Menu "TI Service/Teste (Play)/Locação física". Só funcionam com o jogo rodando.
public static partial class AtalhosTeste
{
    const string MenuLoc = Menu + "Locação física/";

    static readonly (TipoEquipamento eq, string acao)[] passosInstalacao =
    {
        (TipoEquipamento.Impressora, "Olhar a caixa"),
        (TipoEquipamento.Impressora, "Abrir a caixa"),
        (TipoEquipamento.Impressora, "Tirar as fitas"),
        (TipoEquipamento.Impressora, "Levantar o scanner"),
        (TipoEquipamento.Impressora, "Pegar o toner inicial"),
        (TipoEquipamento.Impressora, "Chacoalhar o cartucho"),
        (TipoEquipamento.Impressora, "Puxar o lacre"),
        (TipoEquipamento.Impressora, "Encaixar o cartucho novo"),
        (TipoEquipamento.Impressora, "Baixar o scanner"),
        (TipoEquipamento.Impressora, "Abrir a gaveta de papel"),
        (TipoEquipamento.Impressora, "Colocar papel"),
        (TipoEquipamento.Impressora, "Fechar a gaveta"),
        (TipoEquipamento.Impressora, "Conectar o cabo da impressora"),
        (TipoEquipamento.Impressora, "Ligar a impressora"),
        (TipoEquipamento.Impressora, "Imprimir o relatório de configuração"),
        (TipoEquipamento.Impressora, "Configurar IP fixo"),
        (TipoEquipamento.Computador, "Adicionar a impressora pelo IP"),
        (TipoEquipamento.Computador, "Mandar imprimir uma página de teste pelo computador"),
    };

    static readonly (TipoEquipamento eq, string acao)[] passosVisita =
    {
        (TipoEquipamento.Impressora, "Ler o contador de páginas"),
        (TipoEquipamento.Impressora, "Levantar o scanner"),
        (TipoEquipamento.Impressora, "Retirar o cartucho"),
        (TipoEquipamento.Impressora, "Guardar este cartucho na mochila"),
        (TipoEquipamento.Impressora, "Troca preventiva da unidade de imagem"),
        (TipoEquipamento.Impressora, "Pegar um toner novo da mochila"),
        (TipoEquipamento.Impressora, "Chacoalhar o cartucho"),
        (TipoEquipamento.Impressora, "Puxar o lacre"),
        (TipoEquipamento.Impressora, "Encaixar o cartucho novo"),
        (TipoEquipamento.Impressora, "Baixar o scanner"),
        (TipoEquipamento.Impressora, "Deixar um toner reserva"),
        (TipoEquipamento.Impressora, "Imprimir página de teste"),
    };
    static int passoLocacao;

    static ContratoLocacao ContratoTesteLocacao(bool instalado)
    {
        var cm = ContratosManager.Instance;
        if (Jogador() == null || cm == null) return null;
        var c = cm.contratos.Find(x => x.ativo && x.Instalado == instalado && x.empresa == "Escritório Modelo Ltda");
        if (c != null) return c;
        if (instalado) { Debug.LogWarning("[Teste] Faça antes a instalação (atalhos 1 e 3)."); return null; }
        var tipo = CatalogoLocacao.Equipamentos[0];
        if (cm.EmEstoque(tipo.id).Count == 0)
            cm.patrimonio.Add(new EquipamentoPatrimonio { id = cm.proximoId++, tipoId = tipo.id, diaChegada = 0 });
        return cm.CriarContratoDireto("Escritório Modelo Ltda", tipo.id, 0, 12, tipo.planos[0].mensal, 1500, 0f);
    }

    static void EncherMochilaLocacao()
    {
        var m = Mochila.Instance;
        if (m == null) return;
        if (m.Quantidade(Item.Toner) < 3) m.Adicionar(Item.Toner, 3);
        if (m.Quantidade(Item.Cilindro) < 1) m.Adicionar(Item.Cilindro, 1);
        m.comVoce = true;
    }

    static void AceitarServico(ContratoLocacao c, ServicoLocacao tipo)
    {
        var mm = MissionManager.Instance;
        if (mm.chamadoAtivo != null) mm.AbandonarChamado();
        var ch = mm.ChamadoDeServico(c.id, tipo);
        if (ch == null) { Debug.LogWarning("[Teste] Chamado de " + tipo + " não apareceu na lista."); return; }
        if (EmpresaManager.Instance != null && !EmpresaManager.Instance.TemCnpj) ch.exigeNota = false;   // save de teste sem CNPJ
        mm.AceitarChamado(ch);
        if (mm.chamadoAtivo != ch) { Debug.LogWarning("[Teste] O chamado não foi aceito."); return; }
        passoLocacao = 0;
        IrAoEquipamento(TipoEquipamento.Impressora);
        Debug.Log($"[Teste] Chamado aceito: {ch.nomeCliente} — {ch.solucao} (sala {SalaCliente.Atual?.Nome}). Falta: {string.Join(" · ", mm.Estado.PendenciasLocacao())}");
    }

    [MenuItem(MenuLoc + "1. Contrato novo: chamado de instalação (impressora na caixa)", false, 600)]
    static void LocInstalacao()
    {
        var c = ContratoTesteLocacao(false);
        if (c == null) return;
        EncherMochilaLocacao();
        AceitarServico(c, ServicoLocacao.Instalacao);
    }

    static bool PassoLocacao((TipoEquipamento eq, string acao)[] passos)
    {
        var mm = MissionManager.Instance;
        var e = mm?.Estado;
        if (e == null) { Debug.LogWarning("[Teste] Aceite o chamado antes."); return false; }
        while (passoLocacao < passos.Length)
        {
            var p = passos[passoLocacao++];
            foreach (var a in AcoesFisicas.Para(p.eq, e))
                if (a.rotulo.StartsWith(p.acao))
                {
                    string r = a.executar();
                    if (!a.observacao) mm.RegistrarAcao(a.rotulo);
                    if (p.eq != TipoEquipamento.Computador) IrAoEquipamento(p.eq); else IrAoMonitor();
                    Debug.Log($"[Teste] Passo {passoLocacao}/{passos.Length}: {a.rotulo} → {r}");
                    return true;
                }
            Debug.Log($"[Teste] Passo {passoLocacao}/{passos.Length} pulado (não disponível agora): {p.acao}");
        }
        Debug.Log($"[Teste] Fim dos passos. Falta: {string.Join(" · ", e.PendenciasLocacao())} | aguardando assinatura: {mm.chamadoAtivo?.aguardandoAssinatura}");
        return false;
    }

    [MenuItem(MenuLoc + "2. Instalação: próximo passo", false, 601)]
    static void LocPassoInstalacao() => PassoLocacao(passosInstalacao);

    // Recolhe a embalagem do chão e o cliente assina a OS (como depois da prancheta)
    static void AssinarServico()
    {
        var mm = MissionManager.Instance;
        if (mm?.chamadoAtivo == null) return;
        if (!mm.chamadoAtivo.aguardandoAssinatura) { Debug.LogWarning($"[Teste] Ainda não resolvido. Falta: {string.Join(" · ", mm.Estado.PendenciasLocacao())}"); return; }
        Sujeira.LimparTudo();
        var os = mm.chamadoAtivo.os;
        mm.FecharOSAssinada();
        Debug.Log($"[Teste] OS {os?.numero} assinada: {os?.estrelas} estrela(s) · {os?.diagnostico}");
    }

    [MenuItem(MenuLoc + "3. Instalação inteira e assinar a OS", false, 602)]
    static void LocInstalacaoInteira()
    {
        if (MissionManager.Instance?.chamadoAtivo?.servicoLocacao != ServicoLocacao.Instalacao) LocInstalacao();
        while (PassoLocacao(passosInstalacao)) { }
        float saldoAntes = GameEconomy.Instance != null ? GameEconomy.Instance.saldo : 0f;
        AssinarServico();
        var c = ContratoTesteLocacao(true);
        if (c != null)
            Debug.Log($"[Teste] Contrato instalado no dia {c.diaInstalacao}, toner {c.suprimento * 100f:F0}%, satisfação {c.satisfacao:F0}, " +
                      $"saldo {saldoAntes:F2} → {(GameEconomy.Instance != null ? GameEconomy.Instance.saldo : 0f):F2}");
    }

    [MenuItem(MenuLoc + "4. Visita: agendar (toner baixo, cilindro no fim) e atender", false, 603)]
    static void LocVisita()
    {
        var c = ContratoTesteLocacao(true);
        if (c == null) return;
        EncherMochilaLocacao();
        c.suprimento = 0.12f;
        c.reserva = 0;
        c.paginasPeca = Mathf.RoundToInt(ContratosManager.VidaUnidadeImagem * 0.8f);
        c.paginasNoMes = c.franquia + 120;
        c.contadorTotal = Mathf.Max(c.contadorTotal, 1840);
        c.mesLeitura = -99;
        c.visitaAgendada = false;
        Debug.Log("[Teste] " + ContratosManager.Instance.AgendarVisita(c));
        AceitarServico(c, ServicoLocacao.VisitaMensal);
    }

    [MenuItem(MenuLoc + "5. Visita: próximo passo", false, 604)]
    static void LocPassoVisita() => PassoLocacao(passosVisita);

    [MenuItem(MenuLoc + "6. Visita inteira e assinar a OS", false, 605)]
    static void LocVisitaInteira()
    {
        if (MissionManager.Instance?.chamadoAtivo?.servicoLocacao != ServicoLocacao.VisitaMensal) LocVisita();
        while (PassoLocacao(passosVisita)) { }
        AssinarServico();
        var c = ContratoTesteLocacao(true);
        if (c != null)
            Debug.Log($"[Teste] Depois da visita: toner {c.suprimento * 100f:F0}%, reserva {c.reserva}, unidade de imagem {c.UsoPeca * 100f:F0}%, " +
                      $"leitura do mês {(c.mesLeitura == EmpresaManager.MesDoDia(DiaManager.Instance.dia) ? "feita" : "NÃO")}, visita agendada {c.visitaAgendada}, satisfação {c.satisfacao:F0}");
    }

    // Fotos: caixa no móvel, impressora saindo, fitas, embalagem no chão, painel com IP, toner reserva, unidade de imagem
    [MenuItem(MenuLoc + "7. Fotografar instalação e visita", false, 610)]
    static void LocFotos()
    {
        if (Jogador() == null) return;
        GameUI.Instance?.FecharModal();
        System.IO.Directory.CreateDirectory("Builds/Capturas/locacao");
        var passos = new List<(System.Action fazer, float espera, string arquivo)>
        {
            (() => LocInstalacao(), 2.5f, "1_impressora_na_caixa"),
            (() => { LocPassoInstalacao(); LocPassoInstalacao(); }, 0.6f, "2_saindo_da_caixa"),
            (() => { }, 1.6f, "3_fitas_e_embalagem"),
            (() => { for (int k = 0; k < 14; k++) LocPassoInstalacao(); }, 1.5f, "4_ip_fixo_no_visor"),
            (() => { LocPassoInstalacao(); LocPassoInstalacao(); }, 2.0f, "5_pagina_teste_pelo_pc"),
            (() => { AssinarServico(); LocVisita(); }, 2.5f, null),
            (() => { for (int k = 0; k < 4; k++) LocPassoVisita(); LocPassoVisita(); }, 0.9f, "6_troca_unidade_imagem"),
            (() => { while (PassoLocacao(passosVisita)) { } IrAoEquipamento(TipoEquipamento.Impressora); }, 1.6f, "7_toner_reserva_ao_lado"),
            (() => AssinarServico(), 0.5f, null),
        };
        int i = 0;
        double proximo = 0;
        bool capturar = false;
        string arquivo = null;
        void Passo()
        {
            if (!Application.isPlaying) { EditorApplication.update -= Passo; return; }
            if (EditorApplication.timeSinceStartup < proximo) return;
            if (capturar)
            {
                ScreenCapture.CaptureScreenshot($"Builds/Capturas/locacao/{arquivo}.png");
                Debug.Log($"[Teste] Foto: {arquivo}.png");
                capturar = false;
                proximo = EditorApplication.timeSinceStartup + 0.4;
                return;
            }
            if (i >= passos.Count)
            {
                EditorApplication.update -= Passo;
                Debug.Log("[Teste] Fotos da locação prontas em Builds/Capturas/locacao");
                return;
            }
            var p = passos[i++];
            GameUI.Instance?.FecharModal();
            p.fazer();
            arquivo = p.arquivo;
            capturar = arquivo != null;
            proximo = EditorApplication.timeSinceStartup + p.espera;
        }
        EditorApplication.update += Passo;
    }
}
