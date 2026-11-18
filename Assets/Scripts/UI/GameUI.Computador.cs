using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Parte da GameUI: o computador do cliente, o TI OS (sistema fictício do jogo) com as ferramentas que um técnico usa:
// Prompt de Comando (ipconfig, ping, net start...), Configurações de rede, Serviços, Impressoras e fila,
// Gerenciador de Dispositivos, Gerenciador de Tarefas e o navegador (painel do roteador em 192.168.0.1).
public partial class GameUI
{
    enum App { Inicio, Prompt, Rede, Servicos, Impressoras, Dispositivos, Tarefas, Navegador, Email, Antivirus, Programas, Atualizacoes, SistemaContabil, Contas }

    RectTransform computador, pcConteudo, pcMenu;
    TextMeshProUGUI txtPcTitulo;
    App appAtual = App.Inicio;
    readonly List<string> terminal = new List<string>();
    string digitando = "";
    bool digitandoSenha;
    string senhaDigitada = "";
    bool logadoRoteador;
    string paginaNavegador = "";
    string avisoPc = "";
    bool pcReiniciando;
    const string PromptCmd = "C:\\Users\\cliente> ";

    // Etapa 10D: cara de sistema operacional — fundo em degradê, menu com ícone colorido por programa (o aberto em
    // destaque), janela do programa clara com sombra e barra de tarefas embaixo (rede e relógio)
    readonly Dictionary<App, Image> botoesApp = new Dictionary<App, Image>();
    TextMeshProUGUI txtBarraTarefas;
    static readonly Color CorBotaoApp = UIFactory.Hex("#16365C"), CorBotaoAppAtivo = UIFactory.Hex("#2F6FB3");

    void ConstruirComputador(RectTransform pai)
    {
        computador = JanelaModal(pai, "Computador", new Vector2(1500, 860));
        var fundo = computador.GetComponent<Image>();
        fundo.color = UIFactory.Hex("#0B1E36");
        UIFactory.Arredondar(fundo, 14f);
        UIFactory.Sombra(computador, 10f, 0.5f);
        var papel = UIFactory.Degrade(computador, "PapelDeParede", UIFactory.Hex("#1F5596"), UIFactory.Hex("#0B1E36"));
        UIFactory.Esticar(papel.rectTransform, 4);

        // Barra de título
        var barra = UIFactory.Painel(computador, "Titulo", UIFactory.Hex("#0A1628E6"));
        barra.anchorMin = new Vector2(0, 1); barra.anchorMax = new Vector2(1, 1); barra.pivot = new Vector2(0.5f, 1);
        barra.offsetMin = new Vector2(0, -56); barra.offsetMax = Vector2.zero;
        UIFactory.Horizontal(barra, 12, 10);
        txtPcTitulo = UIFactory.Texto(barra, "", 22, Color.white, FontStyles.Bold);
        txtPcTitulo.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
        UIFactory.Botao(barra, "Levantar (Esc)", FecharModal, UIFactory.Perigo, 200, 40, 18);

        // Barra de tarefas
        var tarefas = UIFactory.Painel(computador, "BarraTarefas", UIFactory.Hex("#081221F0"));
        tarefas.anchorMin = new Vector2(0, 0); tarefas.anchorMax = new Vector2(1, 0); tarefas.pivot = new Vector2(0.5f, 0);
        tarefas.offsetMin = new Vector2(0, 0); tarefas.offsetMax = new Vector2(0, 40);
        UIFactory.Horizontal(tarefas, 14, 6);
        var iniciar = UIFactory.Painel(tarefas, "Iniciar", UIFactory.Destaque);
        var leI = iniciar.gameObject.AddComponent<LayoutElement>(); leI.preferredWidth = 92; leI.preferredHeight = 28;
        var ti = UIFactory.Texto(iniciar, "TI OS", 15, Color.white, FontStyles.Bold, TextAlignmentOptions.Center);
        UIFactory.Esticar(ti.rectTransform);
        txtBarraTarefas = UIFactory.Texto(tarefas, "", 16, UIFactory.CorTexto, FontStyles.Normal, TextAlignmentOptions.Right);
        txtBarraTarefas.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
        txtBarraTarefas.margin = new Vector4(0, 0, 10, 0);

        // Menu Iniciar (lateral)
        pcMenu = UIFactory.Painel(computador, "Menu", UIFactory.Hex("#0A1628CC"));
        UIFactory.Arredondar(pcMenu.GetComponent<Image>(), 14f);
        pcMenu.anchorMin = new Vector2(0, 0); pcMenu.anchorMax = new Vector2(0, 1); pcMenu.pivot = new Vector2(0, 0.5f);
        pcMenu.offsetMin = new Vector2(10, 50); pcMenu.offsetMax = new Vector2(306, -66);
        UIFactory.Vertical(pcMenu, 5, 10);
        BotaoApp("Prompt de Comando", App.Prompt);
        BotaoApp("Configurações de rede", App.Rede);
        BotaoApp("Serviços", App.Servicos);
        BotaoApp("Impressoras e fila", App.Impressoras);
        BotaoApp("Gerenciador de Dispositivos", App.Dispositivos);
        BotaoApp("Gerenciador de Tarefas", App.Tarefas);
        BotaoApp("Navegador", App.Navegador);
        // Etapa 7: programas do dia a dia do cliente (problemas de software)
        BotaoApp("E-mail", App.Email);
        BotaoApp("Sistema Contábil Ômega", App.SistemaContabil);
        BotaoApp("Escudo Antivírus", App.Antivirus);
        BotaoApp("Programas instalados", App.Programas);
        BotaoApp("Atualizações do sistema", App.Atualizacoes);
        BotaoApp("Contas de usuário", App.Contas);
        UIFactory.Botao(pcMenu, "Reiniciar o computador", PedirReinicio, UIFactory.Hex("#7A2E35"), 276, 40, 16);

        // Janela do aplicativo
        var area = UIFactory.Painel(computador, "Area", UIFactory.Hex("#F4F6F9"));
        UIFactory.Arredondar(area.GetComponent<Image>(), 12f);
        UIFactory.Sombra(area, 6f, 0.45f);
        area.anchorMin = Vector2.zero; area.anchorMax = Vector2.one;
        area.offsetMin = new Vector2(318, 50); area.offsetMax = new Vector2(-12, -66);
        pcConteudo = UIFactory.Vazio(area, "Conteudo");
        UIFactory.Esticar(pcConteudo, 18);
        UIFactory.Vertical(pcConteudo, 10, 0);
    }

    // Botão do menu: quadradinho colorido com a sigla do programa + o nome
    void BotaoApp(string nome, App app)
    {
        var b = UIFactory.Botao(pcMenu, nome, () => { appAtual = app; digitandoSenha = false; MontarComputador(); }, CorBotaoApp, 276, 38, 16);
        var rotulo = b.GetComponentInChildren<TextMeshProUGUI>();
        rotulo.alignment = TextAlignmentOptions.Left;
        rotulo.fontStyle = FontStyles.Normal;
        rotulo.margin = new Vector4(48, 0, 6, 0);
        var icone = UIFactory.Painel(b.transform, "Icone", CorDoApp(app));
        UIFactory.Arredondar(icone.GetComponent<Image>(), 6f);
        UIFactory.Ancorar(icone, new Vector2(0f, 0.5f), new Vector2(8f, 0f), new Vector2(28f, 28f));
        var sigla = UIFactory.Texto(icone, Sigla(nome), 12, Color.white, FontStyles.Bold, TextAlignmentOptions.Center);
        UIFactory.Esticar(sigla.rectTransform);
        botoesApp[app] = b.GetComponent<Image>();
    }

    static string Sigla(string nome)
    {
        var p = nome.Split(' ');
        string s = p[0].Substring(0, 1);
        for (int i = 1; i < p.Length && s.Length < 2; i++) if (p[i].Length > 2) s += p[i].Substring(0, 1);
        if (s.Length < 2 && p[0].Length > 1) s += p[0].Substring(1, 1);
        return s.ToUpperInvariant();
    }

    static Color CorDoApp(App a)
    {
        switch (a)
        {
            case App.Prompt: return UIFactory.Hex("#2B2B2B");
            case App.Rede: return UIFactory.Hex("#1E88E5");
            case App.Servicos: return UIFactory.Hex("#6D4C41");
            case App.Impressoras: return UIFactory.Hex("#00897B");
            case App.Dispositivos: return UIFactory.Hex("#5E35B1");
            case App.Tarefas: return UIFactory.Hex("#43A047");
            case App.Navegador: return UIFactory.Hex("#F4511E");
            case App.Email: return UIFactory.Hex("#3949AB");
            case App.SistemaContabil: return UIFactory.Hex("#8E24AA");
            case App.Antivirus: return UIFactory.Hex("#C62828");
            case App.Programas: return UIFactory.Hex("#546E7A");
            case App.Atualizacoes: return UIFactory.Hex("#0277BD");
            case App.Contas: return UIFactory.Hex("#F9A825");
            default: return UIFactory.Destaque;
        }
    }

    public void AbrirComputador()
    {
        var e = MissionManager.Instance?.Estado;
        if (e == null) return;
        AbrirModal(Modal.Computador, computador);
        if (terminal.Count == 0) terminal.Add("TI OS [versão 11.2]  ·  digite \"ajuda\" para ver os comandos");
        avisoPc = "";
        MontarComputador();
    }

    // Chamado pelo Update da GameUI: teclado do Prompt e da senha do roteador
    void AtualizarComputador()
    {
        if (modal != Modal.Computador || pcReiniciando) return;
        bool terminalAtivo = appAtual == App.Prompt;
        if (!terminalAtivo && !digitandoSenha) return;

        string entrada = Input.inputString;
        if (string.IsNullOrEmpty(entrada)) return;
        foreach (char c in entrada)
        {
            string alvo = digitandoSenha ? senhaDigitada : digitando;
            if (c == '\b') { if (alvo.Length > 0) alvo = alvo.Substring(0, alvo.Length - 1); }
            else if (c == '\n' || c == '\r')
            {
                if (digitandoSenha) { EntrarNoRoteador(); return; }
                ExecutarComando(digitando.Trim());
                digitando = "";
                if (modal != Modal.Computador) return;
                continue;
            }
            else if (alvo.Length < 60 && c >= ' ') alvo += c;
            if (digitandoSenha) senhaDigitada = alvo; else digitando = alvo;
        }
        MontarComputador();
    }

    void MontarComputador()
    {
        var mm = MissionManager.Instance;
        var e = mm?.Estado;
        if (e == null) { FecharModal(); return; }
        txtPcTitulo.text = $"{mm.chamadoAtivo.EquipamentoTexto()} de {mm.chamadoAtivo.contato}  ·  {AppNome(appAtual)}" +
                           (e.PcNaInternet ? "   <color=#7CFFB0>• Internet</color>" : e.PcNaRede ? "   <color=#FFD54A>• Sem internet</color>" : "   <color=#FF8A8A>• Sem rede</color>");

        foreach (var kv in botoesApp) if (kv.Value != null) kv.Value.color = kv.Key == appAtual ? CorBotaoAppAtivo : CorBotaoApp;
        if (txtBarraTarefas != null)
            txtBarraTarefas.text = (e.PcNaInternet ? "<color=#7CFFB0>Internet</color>" : e.PcNaRede ? "<color=#FFD54A>Rede sem internet</color>" : "<color=#FF8A8A>Sem rede</color>") +
                                   $"     {mm.chamadoAtivo.contato}     {(DiaManager.Instance != null ? DiaManager.Instance.HoraTexto() : "")}";

        UIFactory.Limpar(pcConteudo);
        if (pcReiniciando) { TextoPc("Reiniciando...", 40, true); return; }
        if (!string.IsNullOrEmpty(avisoPc)) TextoPc($"<color=#B23A48>{avisoPc}</color>", 20);
        if (e.swContaBloqueada && !e.swAdminLogado) { MontarLogin(e); if (mm.chamadoAtivo == null) FecharModal(); return; }
        if (e.swPopups) TextoPc("<color=#B23A48><b>(!) PARABÉNS!!! Você ganhou um celular — clique aqui</b></color>   <size=80%>(janela de propaganda que não fecha)</size>", 19);

        switch (appAtual)
        {
            case App.Email: MontarEmail(e); break;
            case App.Antivirus: MontarAntivirus(e); break;
            case App.Programas: MontarProgramas(e); break;
            case App.Atualizacoes: MontarAtualizacoes(e); break;
            case App.SistemaContabil: MontarSistemaContabil(e); break;
            case App.Contas: MontarContas(e); break;
            case App.Prompt: MontarPrompt(); break;
            case App.Rede: MontarRede(e); break;
            case App.Servicos: MontarServicos(e); break;
            case App.Impressoras: MontarImpressoras(e); break;
            case App.Dispositivos: MontarDispositivos(e); break;
            case App.Tarefas: MontarTarefas(e); break;
            case App.Navegador: MontarNavegador(e); break;
            default:
                TextoPc("Área de trabalho", 30, true);
                TextoPc("Escolha um programa no menu à esquerda. No Prompt de Comando você pode digitar comandos como um técnico de verdade.", 22);
                break;
        }
        // Se algo conferido aqui resolveu o chamado, levanta do computador
        if (mm.chamadoAtivo == null) FecharModal();
    }

    static string AppNome(App a)
    {
        switch (a)
        {
            case App.Prompt: return "Prompt de Comando";
            case App.Rede: return "Configurações de rede";
            case App.Servicos: return "Serviços";
            case App.Impressoras: return "Impressoras e fila";
            case App.Dispositivos: return "Gerenciador de Dispositivos";
            case App.Tarefas: return "Gerenciador de Tarefas";
            case App.Navegador: return "Navegador";
            case App.Email: return "E-mail";
            case App.Antivirus: return "Escudo Antivírus";
            case App.Programas: return "Programas instalados";
            case App.Atualizacoes: return "Atualizações do sistema";
            case App.SistemaContabil: return "Sistema Contábil Ômega";
            case App.Contas: return "Contas de usuário";
            default: return "Área de trabalho";
        }
    }

    TextMeshProUGUI TextoPc(string texto, float tamanho = 21, bool negrito = false) =>
        UIFactory.Texto(pcConteudo, texto, tamanho, UIFactory.Hex("#1A2433"), negrito ? FontStyles.Bold : FontStyles.Normal);

    Button BotaoPc(RectTransform pai, string rotulo, System.Action acao, Color? cor = null, float largura = 260) =>
        UIFactory.Botao(pai, rotulo, () => { acao(); MissionManager.Instance?.RegistrarAcao("No computador: " + rotulo); if (modal == Modal.Computador) MontarComputador(); },
                        cor ?? UIFactory.Hex("#2D6CDF"), largura, 44, 18);

    RectTransform LinhaPc()
    {
        var l = UIFactory.Vazio(pcConteudo, "Linha");
        UIFactory.Horizontal(l, 10);
        return l;
    }

    // ---------- Prompt de Comando ----------

    void MontarPrompt()
    {
        var caixa = UIFactory.Painel(pcConteudo, "Terminal", UIFactory.Hex("#0C0C0C"));
        caixa.gameObject.AddComponent<LayoutElement>().preferredHeight = 700;
        UIFactory.Vertical(caixa, 0, 16);
        int inicio = Mathf.Max(0, terminal.Count - 22);
        var sb = new System.Text.StringBuilder();
        for (int i = inicio; i < terminal.Count; i++) sb.AppendLine(terminal[i]);
        sb.Append(PromptCmd + digitando + "_");
        UIFactory.Texto(caixa, sb.ToString(), 20, UIFactory.Hex("#D6D6D6"));
    }

    void Saida(string linha) => terminal.Add(linha);

    void ExecutarComando(string cmd)
    {
        var mm = MissionManager.Instance;
        var e = mm?.Estado;
        Saida(PromptCmd + cmd);
        if (e == null || cmd.Length == 0) return;
        string c = cmd.ToLowerInvariant();
        mm.RegistrarAcao("Prompt de comando: " + cmd);

        if (c == "ajuda" || c == "help")
        {
            Saida("  ipconfig            mostra o endereço IP");
            Saida("  ipconfig /release   libera o IP   ·   ipconfig /renew   pede um IP novo");
            Saida("  ipconfig /flushdns  limpa o cache de DNS");
            Saida("  ping <endereço>     testa a conexão (ex.: ping 8.8.8.8, ping 192.168.0.1)");
            Saida("  net start spooler   ·   net stop spooler   (serviço de impressão)");
            Saida("  shutdown /r         reinicia o computador   ·   cls   limpa a tela");
        }
        else if (c == "cls") terminal.Clear();
        else if (c == "ipconfig" || c == "ipconfig /all")
        {
            Saida("Adaptador Ethernet:");
            if (!e.PcRedeFisica) { Saida("   Estado da mídia. . . : mídia desconectada"); return; }
            Saida($"   Endereço IPv4. . . . : {e.PcIpMostrado}" + (e.pcIpFixo ? "   (manual)" : "   (DHCP)"));
            Saida("   Máscara de sub-rede  : 255.255.255.0");
            Saida($"   Gateway padrão . . . : {(e.pcIpErrado ? "10.0.0.1" : e.pcIpLiberado ? "" : "192.168.0.1")}");
            if (e.PcConflitoIp) Saida("   AVISO: outro dispositivo na rede está usando este endereço IP!");
        }
        else if (c == "ipconfig /release") Saida(e.LiberarIp());
        else if (c == "ipconfig /renew") Saida(e.RenovarIp());
        else if (c == "ipconfig /flushdns") Saida("Cache do DNS liberado com êxito.");
        else if (c.StartsWith("ping"))
        {
            string destino = c.Length > 5 ? c.Substring(5).Trim() : "8.8.8.8";
            if (destino == "buscador.com.br") destino = "142.250.79.46";
            Saida($"Disparando {destino} com 32 bytes de dados:");
            string r = e.Ping(destino);
            for (int i = 0; i < 4; i++) Saida(r);
            if (r.StartsWith("Resposta")) mm.Verificar();
        }
        else if (c == "net start spooler")
        {
            if (e.spoolerRodando) Saida("O serviço solicitado já foi iniciado.");
            else { e.spoolerRodando = true; Saida("O serviço de Spooler de Impressão foi iniciado com êxito."); }
        }
        else if (c == "net stop spooler")
        {
            e.spoolerRodando = false;
            Saida("O serviço de Spooler de Impressão foi parado com êxito.");
        }
        else if (c == "shutdown /r") { PedirReinicio(); }
        else if (c == "exit") FecharModal();
        else Saida($"'{cmd}' não é reconhecido como um comando interno. Digite \"ajuda\".");
    }

    // ---------- Configurações de rede ----------

    void MontarRede(EstadoEquipamentos e)
    {
        TextoPc("Ethernet", 28, true);
        TextoPc(e.PcRedeFisica ? (e.PcNaInternet ? "Conectado, com internet" : e.PcNaRede ? "Conectado, sem internet" : "Rede não identificada")
                               : "Cabo de rede desconectado / adaptador indisponível", 22);
        TextoPc($"Atribuição de IP: <b>{(e.pcIpFixo ? "Manual" : "Automático (DHCP)")}</b>   ·   Endereço IPv4: {e.PcIpMostrado}", 21);
        var l = LinhaPc();
        BotaoPc(l, "Mudar para Automático (DHCP)", () => avisoPc = e.DefinirIpAutomatico(true));
        BotaoPc(l, "Mudar para Manual", () => avisoPc = e.DefinirIpAutomatico(false), UIFactory.Hex("#6B7A90"));
    }

    // ---------- Serviços ----------

    void MontarServicos(EstadoEquipamentos e)
    {
        TextoPc("Serviços (services.msc)", 28, true);
        TextoPc($"Spooler de Impressão — <b>{(e.spoolerRodando ? "<color=#1E8E3E>Em execução</color>" : "<color=#B23A48>Parado</color>")}</b>", 22);
        var l = LinhaPc();
        BotaoPc(l, "Iniciar", () => { avisoPc = e.spoolerRodando ? "Já está em execução." : "Spooler de Impressão iniciado."; e.spoolerRodando = true; }, null, 160);
        BotaoPc(l, "Parar", () => { e.spoolerRodando = false; avisoPc = "Spooler de Impressão parado."; }, UIFactory.Hex("#6B7A90"), 160);
        BotaoPc(l, "Reiniciar", () => { e.spoolerRodando = true; avisoPc = "Spooler de Impressão reiniciado."; }, null, 160);
        TextoPc("Cliente DHCP — Em execução", 20);
        TextoPc("Atualizações do TI OS — Em execução", 20);
        TextoPc("Antivírus — Em execução", 20);
    }

    // ---------- Impressoras e fila ----------

    void MontarImpressoras(EstadoEquipamentos e)
    {
        if (e.pcSemImpressora)
        {
            TextoPc("Impressoras e scanners", 28, true);
            TextoPc("Nenhuma impressora instalada neste computador.", 22);
            BotaoPc(LinhaPc(), $"Adicionar impressora por endereço TCP/IP ({e.ipImpressora})", () =>
            {
                avisoPc = e.AdicionarImpressoraPeloIp(out bool ok);
                if (ok) { MissionManager.Instance?.ConsumirTempo(10f); MissionManager.Instance?.RegistrarAcao($"Adicionou a impressora pelo IP {e.ipImpressora}"); }
            }, null, 560);
            return;
        }
        TextoPc("Impressora do escritório (192.168.0.50)", 28, true);
        string status = !e.spoolerRodando ? "<color=#B23A48>Não disponível (spooler parado)</color>" :
                        !e.ImpressoraOnline || !e.PcNaRede ? "<color=#B23A48>Offline</color>" :
                        e.FilaTravada ? "<color=#C77700>Erro - imprimindo</color>" : "<color=#1E8E3E>Pronta</color>";
        TextoPc("Status: " + status, 22);
        // Etapa 10B: o driver aparece aqui (antes só dava para reinstalar mirando o monitor, e ninguém achava)
        TextoPc("Driver: " + (e.impDriverErrado
            ? "<color=#B23A48>Impressora PCL genérica</color>  <size=85%>(não é o driver do modelo: sai página com símbolos estranhos)</size>"
            : "Laser monocromática — driver do fabricante"), 21);
        if (e.impDriverErrado)
            BotaoPc(LinhaPc(), "Remover a impressora e reinstalar com o driver do modelo (pendrive de drivers)", () =>
            {
                var m = Mochila.Instance;
                if (m == null || !m.Tem(Item.PendriveDrivers)) { avisoPc = "Precisa do pendrive de drivers na mochila (Loja do tablet)."; return; }
                e.impDriverErrado = false;
                MissionManager.Instance?.ConsumirTempo(30f);
                MissionManager.Instance?.RegistrarAcao("Reinstalou a impressora com o driver certo (pendrive)");
                avisoPc = "Impressora removida, driver genérico apagado e o do modelo instalado pelo pendrive. Imprima uma página de teste.";
            }, null, 760);

        if (!e.spoolerRodando) { TextoPc("Não foi possível abrir a fila: o serviço Spooler de Impressão não está em execução.", 20); }
        else
        {
            TextoPc($"Fila de impressão ({e.fila.Count} documento(s)):", 21, true);
            for (int i = 0; i < Mathf.Min(e.fila.Count, 8); i++)
            {
                var t = e.fila[i];
                var l = LinhaPc();
                var nome = UIFactory.Texto(l, $"{t.nome}  ·  {t.paginas} pág.  ·  {(t.travado ? "<color=#B23A48>Erro - imprimindo</color>" : "Na fila")}", 19, UIFactory.Hex("#1A2433"));
                nome.gameObject.AddComponent<LayoutElement>().preferredWidth = 760;
                BotaoPc(l, "Cancelar", () => e.fila.Remove(t), UIFactory.Hex("#6B7A90"), 140);
            }
            if (e.fila.Count > 8) TextoPc($"... e mais {e.fila.Count - 8}", 19);
            if (e.fila.Count > 0) BotaoPc(LinhaPc(), "Cancelar todos os documentos", () => e.fila.Clear(), UIFactory.Hex("#B23A48"), 320);
        }

        MontarImpressoraPadrao(e);
        BotaoPc(LinhaPc(), "Imprimir página de teste pelo computador", () =>
        {
            avisoPc = e.Imprimir(true, out bool boa);
            if (boa) MissionManager.Instance?.Verificar();
        }, null, 420);
    }

    // ---------- Gerenciador de Dispositivos ----------

    void MontarDispositivos(EstadoEquipamentos e)
    {
        TextoPc("Gerenciador de Dispositivos", 28, true);
        TextoPc("» Adaptadores de rede", 22, true);
        if (!e.driverInstalado) TextoPc("     (nenhum adaptador de rede encontrado)", 20);
        else if (!e.driverOk) TextoPc("     <color=#C77700>(!)</color> Placa de rede Ethernet — <color=#B23A48>Este dispositivo não pode ser iniciado. (Código 10)</color>", 20);
        else TextoPc("     Placa de rede Ethernet — Este dispositivo está funcionando corretamente.", 20);
        TextoPc("» Processadores · Teclados · Monitores · Unidades de disco — sem problemas", 19);

        var l = LinhaPc();
        if (e.driverInstalado)
            BotaoPc(l, "Desinstalar a placa de rede", () => { e.driverInstalado = false; e.driverOk = false; avisoPc = "Dispositivo desinstalado. Reinicie ou procure alterações de hardware."; }, UIFactory.Hex("#B23A48"), 320);
        BotaoPc(l, "Procurar alterações de hardware", () => avisoPc = e.ProcurarHardware(), null, 340);
        bool pendrive = Mochila.Instance != null && Mochila.Instance.Tem(Item.PendriveDrivers);
        var b = BotaoPc(l, pendrive ? "Instalar driver do pendrive" : "Instalar driver do pendrive (não tem)", () =>
        {
            e.driverInstalado = true; e.driverOk = true;
            avisoPc = "Driver da placa de rede instalado pelo pendrive.";
        }, UIFactory.Hex("#2D8A5F"), 340);
        b.interactable = pendrive;
    }

    // ---------- Gerenciador de Tarefas ----------

    void MontarTarefas(EstadoEquipamentos e)
    {
        TextoPc("Gerenciador de Tarefas", 28, true);
        int mem = Mathf.RoundToInt(e.memoria * 100f);
        string cor = mem > 85 ? "#B23A48" : mem > 65 ? "#C77700" : "#1E8E3E";
        TextoPc($"Memória: <color={cor}><b>{mem}%</b></color>   ·   CPU: {(mem > 85 ? 78 : 12)}%   ·   Tempo ligado: <b>{e.diasLigado} dia(s)</b>", 22);
        TextoPc("Processos:", 21, true);
        if (mem > 85)
        {
            TextoPc("   Navegador (63 abas) ........ 5,1 GB", 20);
            TextoPc("   Planilha_gigante.xlsx ....... 1,9 GB  <color=#B23A48>(não está respondendo)</color>", 20);
            TextoPc("   Programa de chat ............ 0,9 GB", 20);
        }
        else
        {
            TextoPc("   Navegador (4 abas) ......... 0,8 GB", 20);
            TextoPc("   Programa de e-mail ......... 0,2 GB", 20);
        }
        MontarInicializacao(e);
        if (mem < 80) MissionManager.Instance?.Verificar();
    }

    // ---------- Navegador (painel do roteador) ----------

    void MontarNavegador(EstadoEquipamentos e)
    {
        var l = LinhaPc();
        BotaoPc(l, "192.168.0.1 (roteador)", () => { paginaNavegador = "roteador"; logadoRoteador = false; senhaDigitada = ""; }, UIFactory.Hex("#6B7A90"), 300);
        BotaoPc(l, "buscador.com.br", () => paginaNavegador = "busca", UIFactory.Hex("#6B7A90"), 200);
        if (e.wifiServico) BotaoPc(l, "192.168.0.2 (access point)", () => paginaNavegador = "ap", UIFactory.Hex("#6B7A90"), 300);

        if (e.swPaginaTrocada && paginaNavegador != "roteador")
        {
            TextoPc("<color=#B23A48><b>ofertas-imperdiveis.com</b></color> — SUPER PROMOÇÕES PARA VOCÊ!!! (a página inicial e a busca foram trocadas)", 24);
            BotaoPc(LinhaPc(), "Configurações do navegador » Restaurar as configurações padrão", () =>
            {
                avisoPc = e.RestaurarPaginaInicial();
                MissionManager.Instance?.Verificar();
            }, null, 620);
            return;
        }
        if (paginaNavegador == "busca")
        {
            if (e.PcNaInternet) { TextoPc("<b>Buscador</b>", 44); TextoPc("A página abriu normalmente.", 22); MissionManager.Instance?.Verificar(); }
            else TextoPc("Não é possível acessar esse site.  ERR_INTERNET_DISCONNECTED", 24);
            return;
        }
        if (paginaNavegador == "ap") { MontarPainelAp(e); return; }
        if (paginaNavegador != "roteador") { TextoPc("Escolha um endereço acima.", 22); return; }
        if (!e.PcNaRede || !e.RoteadorFuncionando) { TextoPc("Não é possível acessar 192.168.0.1 (o computador está sem rede ou o roteador está desligado).", 22); return; }

        if (!logadoRoteador)
        {
            TextoPc("Painel do roteador · Login", 28, true);
            TextoPc("Usuário: admin", 21);
            digitandoSenha = true;
            TextoPc($"Senha: {new string('*', senhaDigitada.Length)}_   <size=80%>(digite e aperte Enter)</size>", 22);
            BotaoPc(LinhaPc(), "Entrar", EntrarNoRoteador, null, 160);
            return;
        }

        digitandoSenha = false;
        TextoPc("Painel do roteador", 28, true);
        TextoPc($"Internet: {(e.InternetOk ? "<color=#1E8E3E>Conectada</color>" : "<color=#B23A48>Sem conexão</color>")}" +
                $"   ·   Configuração: {(e.rotConfigOk ? "ok" : "<color=#B23A48>não configurado (padrão de fábrica)</color>")}", 21);

        TextoPc($"<b>Wi-Fi</b>: {(e.wifiAtivo ? "Ativado" : "<color=#B23A48>DESATIVADO</color>")}  ·  Nome da rede: {e.wifiNome}", 21);
        var w = LinhaPc();
        BotaoPc(w, e.wifiAtivo ? "Desativar Wi-Fi" : "Ativar Wi-Fi", () => { e.wifiAtivo = !e.wifiAtivo; MissionManager.Instance?.Verificar(); }, null, 220);
        BotaoPc(w, "Assistente: configurar internet e Wi-Fi", () =>
        {
            e.ConfigurarRoteador(MissionManager.Instance?.chamadoAtivo?.nomeCliente?.Replace(" ", "_"));
            avisoPc = "Internet e Wi-Fi configurados.";
        }, null, 420);

        // Etapa 10: canal e rede de visitantes
        TextoPc($"<b>Canal do Wi-Fi</b>: {e.rotCanal}   <size=80%>(veja no celular, app Wi-Fi, em que canal estão as redes dos vizinhos)</size>", 21);
        var cn = LinhaPc();
        foreach (int canal in new[] { 1, 6, 11 })
        {
            int c = canal;
            BotaoPc(cn, $"Canal {c}", () =>
            {
                e.rotCanal = c;
                avisoPc = e.RotCanalLivre ? $"Wi-Fi no canal {c}: livre, nenhum vizinho aqui." : $"Wi-Fi no canal {c}: o mesmo de várias redes vizinhas.";
                MissionManager.Instance?.Verificar();
            }, c == e.rotCanal ? UIFactory.Destaque : UIFactory.Hex("#6B7A90"), 140);
        }
        TextoPc($"<b>Rede de visitantes</b>: {(e.rotRedeVisitantes ? $"{e.wifiNome}_Visitantes" + (e.rotVisitantesIsolada ? " (isolada da rede da empresa)" : " <color=#B23A48>(NÃO isolada)</color>") : "<color=#B23A48>não existe</color>")}" +
                $"   ·   Senha da empresa: {(e.rotSenhaVazada ? "<color=#B23A48>a mesma do quadro da recepção</color>" : "só com a equipe")}", 21);
        var vs = LinhaPc();
        if (!e.rotRedeVisitantes || !e.rotVisitantesIsolada)
            BotaoPc(vs, "Criar a rede de visitantes isolada", () =>
            {
                e.rotRedeVisitantes = e.rotVisitantesIsolada = true;
                avisoPc = "Rede de visitantes criada: só internet, sem acesso à impressora e aos computadores da empresa.";
                MissionManager.Instance?.Verificar();
            }, null, 380);
        if (e.rotSenhaVazada)
            BotaoPc(vs, "Trocar a senha da rede da empresa", () =>
            {
                e.rotSenhaVazada = false;
                avisoPc = "Senha da empresa trocada (forte, só com a equipe). A senha do quadro da recepção agora é a da rede de visitantes.";
                MissionManager.Instance?.Verificar();
            }, null, 360);

        TextoPc($"<b>Rede</b>: modem da operadora em modo {(e.modemEmBridge ? "Bridge (só repassa a internet)" : "<color=#B23A48>Roteador (também entrega IP!)</color>")}", 21);
        var r = LinhaPc();
        BotaoPc(r, "Colocar o modem em Bridge", () => { e.modemEmBridge = true; e.dhcpDuplo = false; avisoPc = "Modem em Bridge. Renove o IP dos computadores (ipconfig /renew)."; }, null, 340);

        TextoPc("<b>Sistema</b>", 21);
        var s = LinhaPc();
        BotaoPc(s, "Reiniciar o roteador", () =>
        {
            e.rotTravado = false;
            e.rotLigaEm = Time.time + EstadoEquipamentos.SegundosIniciandoRoteador;
            logadoRoteador = false;
            paginaNavegador = "";
            avisoPc = "O roteador está reiniciando (uns 15 segundos)...";
        }, UIFactory.Hex("#6B7A90"), 260);
        BotaoPc(s, "Restaurar padrão de fábrica", () =>
        {
            var mm = MissionManager.Instance;
            bool precisava = mm?.chamadoAtivo?.problema != null && mm.chamadoAtivo.problema.solucao == "Resetar para padrão";
            e.ResetFabrica();
            if (!precisava) mm?.RegistrarErro("Você restaurou o roteador sem precisar: a internet de todo mundo caiu!", 3f);
            logadoRoteador = false;
            paginaNavegador = "";
            avisoPc = "Roteador restaurado para o padrão de fábrica.";
        }, UIFactory.Hex("#B23A48"), 300);
    }

    // Etapa 10: painel do access point do projeto de Wi-Fi (192.168.0.2)
    void MontarPainelAp(EstadoEquipamentos e)
    {
        if (!e.PcNaRede) { TextoPc("Não é possível acessar 192.168.0.2 (o computador está sem rede).", 22); return; }
        if (!e.ApLigado)
        {
            TextoPc(e.ApIniciando ? "O access point está iniciando... (luz laranja piscando). Tente de novo em alguns segundos."
                                  : "192.168.0.2 não responde: o access point ainda não está instalado e ligado na porta PoE do switch.", 22);
            return;
        }
        string empresa = (MissionManager.Instance?.chamadoAtivo?.nomeCliente ?? "Empresa").Replace(" ", "_");
        TextoPc($"Painel do access point · {e.NomeAp}", 28, true);
        TextoPc($"<b>Rede da empresa</b>: {(e.apRedeEmpresa ? $"{empresa} (WPA2/WPA3, senha forte)" : "<color=#B23A48>não criada (rede aberta de fábrica no ar)</color>")}", 21);
        var r1 = LinhaPc();
        if (!e.apRedeEmpresa)
            BotaoPc(r1, "Criar a rede da empresa (WPA2/WPA3)", () =>
            {
                e.apRedeEmpresa = true;
                avisoPc = $"Rede \"{empresa}\" criada com WPA2/WPA3 e senha forte. A rede aberta de fábrica saiu do ar.";
                MissionManager.Instance?.Verificar();
            }, null, 380);

        TextoPc($"<b>Rede de visitantes</b>: {(e.apRedeVisitantes ? $"{empresa}_Visitantes · isolamento: {(e.apVisitantesIsolada ? "ligado (só internet)" : "<color=#B23A48>desligado (vê a rede da empresa!)</color>")}" : "não criada")}", 21);
        var r2 = LinhaPc();
        if (!e.apRedeVisitantes)
            BotaoPc(r2, "Criar a rede de visitantes", () =>
            {
                e.apRedeVisitantes = true;
                e.apVisitanteTestado = false;
                avisoPc = "Rede de visitantes criada. Atenção à opção de isolamento.";
            }, null, 300);
        else
            BotaoPc(r2, e.apVisitantesIsolada ? "Desligar o isolamento dos visitantes" : "Ligar o isolamento dos visitantes (só internet)", () =>
            {
                e.apVisitantesIsolada = !e.apVisitantesIsolada;
                e.apVisitanteTestado = false;
                avisoPc = e.apVisitantesIsolada ? "Visitantes isolados: só internet, sem ver a impressora nem os computadores." : "Isolamento desligado.";
                MissionManager.Instance?.Verificar();
            }, null, 460);

        TextoPc($"<b>Canal</b>: {e.apCanal}{(e.apCanal == 6 ? " (padrão de fábrica)" : "")}   <size=80%>(redes vizinhas: veja no celular, app Wi-Fi)</size>", 21);
        var cn = LinhaPc();
        foreach (int canal in new[] { 1, 6, 11 })
        {
            int c = canal;
            BotaoPc(cn, $"Canal {c}", () =>
            {
                e.apCanal = c;
                avisoPc = e.ApCanalLivre ? $"AP no canal {c}: livre." : $"AP no canal {c}: o mesmo das redes dos vizinhos (interferência).";
                MissionManager.Instance?.Verificar();
            }, c == e.apCanal ? UIFactory.Destaque : UIFactory.Hex("#6B7A90"), 140);
        }
        TextoPc(e.wifiAtivo ? "<color=#F5B83F>O Wi-Fi do roteador da operadora (192.168.0.1) ainda está ligado: duas redes brigando. Desligue lá.</color>"
                            : "Wi-Fi do roteador da operadora desligado: só o AP no ar.", 20);
    }

    void EntrarNoRoteador()
    {
        var e = MissionManager.Instance?.Estado;
        if (e == null) return;
        bool ok = !e.rotSenhaDesconhecida && senhaDigitada == EstadoEquipamentos.SenhaPadrao;
        avisoPc = ok ? "" : (e.rotSenhaDesconhecida ? "Senha incorreta. (Alguém trocou a senha... só resetando o roteador.)" : "Senha incorreta. (Olhe a etiqueta embaixo do roteador.)");
        logadoRoteador = ok;
        digitandoSenha = !ok;
        senhaDigitada = "";
        MontarComputador();
    }

    // ---------- Reiniciar ----------

    void PedirReinicio()
    {
        var mm = MissionManager.Instance;
        var e = mm?.Estado;
        if (e == null || pcReiniciando) return;
        if (!e.clienteSalvouTrabalho)
            mm.RegistrarErro($"Você reiniciou sem pedir para {mm.chamadoAtivo.contato} salvar: ele perdeu o que estava fazendo!", 3f);
        StartCoroutine(Reiniciar(e));
    }

    IEnumerator Reiniciar(EstadoEquipamentos e)
    {
        pcReiniciando = true;
        MontarComputador();
        yield return new WaitForSeconds(5f);
        e.ReiniciarPc();
        pcReiniciando = false;
        terminal.Clear();
        terminal.Add("TI OS [versão 11.2]  ·  digite \"ajuda\" para ver os comandos");
        avisoPc = "O computador reiniciou.";
        if (modal == Modal.Computador) MontarComputador();
        MissionManager.Instance?.Verificar();
    }
}
