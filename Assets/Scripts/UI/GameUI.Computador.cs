using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Parte da GameUI: o computador do cliente, um "Windows" simplificado com as ferramentas que um técnico usa:
// Prompt de Comando (ipconfig, ping, net start...), Configurações de rede, Serviços, Impressoras e fila,
// Gerenciador de Dispositivos, Gerenciador de Tarefas e o navegador (painel do roteador em 192.168.0.1).
public partial class GameUI
{
    enum App { Inicio, Prompt, Rede, Servicos, Impressoras, Dispositivos, Tarefas, Navegador }

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

    void ConstruirComputador(RectTransform pai)
    {
        computador = JanelaModal(pai, "Computador", new Vector2(1500, 860));
        var fundo = computador.GetComponent<Image>();
        fundo.color = UIFactory.Hex("#0F2A4A");

        // Barra de título
        var barra = UIFactory.Painel(computador, "Titulo", UIFactory.Hex("#1B3A5E"));
        barra.anchorMin = new Vector2(0, 1); barra.anchorMax = new Vector2(1, 1); barra.pivot = new Vector2(0.5f, 1);
        barra.offsetMin = new Vector2(0, -56); barra.offsetMax = Vector2.zero;
        UIFactory.Horizontal(barra, 12, 10);
        txtPcTitulo = UIFactory.Texto(barra, "", 22, Color.white, FontStyles.Bold);
        txtPcTitulo.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
        UIFactory.Botao(barra, "Levantar (Esc)", FecharModal, UIFactory.Perigo, 200, 40, 18);

        // Menu Iniciar (lateral)
        pcMenu = UIFactory.Painel(computador, "Menu", UIFactory.Hex("#132F52"));
        pcMenu.anchorMin = new Vector2(0, 0); pcMenu.anchorMax = new Vector2(0, 1); pcMenu.pivot = new Vector2(0, 0.5f);
        pcMenu.offsetMin = new Vector2(0, 0); pcMenu.offsetMax = new Vector2(300, -56);
        UIFactory.Vertical(pcMenu, 6, 12);
        BotaoApp("Prompt de Comando", App.Prompt);
        BotaoApp("Configurações de rede", App.Rede);
        BotaoApp("Serviços", App.Servicos);
        BotaoApp("Impressoras e fila", App.Impressoras);
        BotaoApp("Gerenciador de Dispositivos", App.Dispositivos);
        BotaoApp("Gerenciador de Tarefas", App.Tarefas);
        BotaoApp("Navegador", App.Navegador);
        UIFactory.Botao(pcMenu, "Reiniciar o computador", PedirReinicio, UIFactory.Hex("#7A2E35"), 276, 46, 18);

        // Área do aplicativo
        var area = UIFactory.Painel(computador, "Area", UIFactory.Hex("#E9EDF2"));
        area.anchorMin = Vector2.zero; area.anchorMax = Vector2.one;
        area.offsetMin = new Vector2(312, 12); area.offsetMax = new Vector2(-12, -68);
        pcConteudo = UIFactory.Vazio(area, "Conteudo");
        UIFactory.Esticar(pcConteudo, 18);
        UIFactory.Vertical(pcConteudo, 10, 0);
    }

    void BotaoApp(string nome, App app)
    {
        UIFactory.Botao(pcMenu, nome, () => { appAtual = app; digitandoSenha = false; MontarComputador(); }, UIFactory.Hex("#1F4A7A"), 276, 46, 18);
    }

    public void AbrirComputador()
    {
        var e = MissionManager.Instance?.Estado;
        if (e == null) return;
        AbrirModal(Modal.Computador, computador);
        if (terminal.Count == 0) terminal.Add("Microsoft Windows [versão 10.0]  ·  digite \"ajuda\" para ver os comandos");
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
        txtPcTitulo.text = $"Computador de {mm.chamadoAtivo.contato}  ·  {AppNome(appAtual)}" +
                           (e.PcNaInternet ? "   <color=#7CFFB0>• Internet</color>" : e.PcNaRede ? "   <color=#FFD54A>• Sem internet</color>" : "   <color=#FF8A8A>• Sem rede</color>");

        UIFactory.Limpar(pcConteudo);
        if (pcReiniciando) { TextoPc("Reiniciando...", 40, true); return; }
        if (!string.IsNullOrEmpty(avisoPc)) TextoPc($"<color=#B23A48>{avisoPc}</color>", 20);

        switch (appAtual)
        {
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
            default: return "Área de trabalho";
        }
    }

    TextMeshProUGUI TextoPc(string texto, float tamanho = 21, bool negrito = false) =>
        UIFactory.Texto(pcConteudo, texto, tamanho, UIFactory.Hex("#1A2433"), negrito ? FontStyles.Bold : FontStyles.Normal);

    Button BotaoPc(RectTransform pai, string rotulo, System.Action acao, Color? cor = null, float largura = 260) =>
        UIFactory.Botao(pai, rotulo, () => { acao(); MissionManager.Instance?.RegistrarAcao(); if (modal == Modal.Computador) MontarComputador(); },
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
        mm.RegistrarAcao();

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
            if (destino == "google.com") destino = "142.250.79.46";
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
        TextoPc("Windows Update — Em execução", 20);
        TextoPc("Antivírus — Em execução", 20);
    }

    // ---------- Impressoras e fila ----------

    void MontarImpressoras(EstadoEquipamentos e)
    {
        TextoPc("Impressora do escritório (192.168.0.50)", 28, true);
        string status = !e.spoolerRodando ? "<color=#B23A48>Não disponível (spooler parado)</color>" :
                        !e.ImpressoraOnline || !e.PcNaRede ? "<color=#B23A48>Offline</color>" :
                        e.FilaTravada ? "<color=#C77700>Erro - imprimindo</color>" : "<color=#1E8E3E>Pronta</color>";
        TextoPc("Status: " + status, 22);

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
        if (mem < 80) MissionManager.Instance?.Verificar();
    }

    // ---------- Navegador (painel do roteador) ----------

    void MontarNavegador(EstadoEquipamentos e)
    {
        var l = LinhaPc();
        BotaoPc(l, "192.168.0.1 (roteador)", () => { paginaNavegador = "roteador"; logadoRoteador = false; senhaDigitada = ""; }, UIFactory.Hex("#6B7A90"), 300);
        BotaoPc(l, "google.com", () => paginaNavegador = "google", UIFactory.Hex("#6B7A90"), 200);

        if (paginaNavegador == "google")
        {
            if (e.PcNaInternet) { TextoPc("<b>Google</b>", 44); TextoPc("A página abriu normalmente.", 22); MissionManager.Instance?.Verificar(); }
            else TextoPc("Não é possível acessar esse site.  ERR_INTERNET_DISCONNECTED", 24);
            return;
        }
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
        BotaoPc(w, e.wifiAtivo ? "Desativar Wi-Fi" : "Ativar Wi-Fi", () => e.wifiAtivo = !e.wifiAtivo, null, 220);
        BotaoPc(w, "Assistente: configurar internet e Wi-Fi", () =>
        {
            e.ConfigurarRoteador(MissionManager.Instance?.chamadoAtivo?.nomeCliente?.Replace(" ", "_"));
            avisoPc = "Internet e Wi-Fi configurados.";
        }, null, 420);

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
        terminal.Add("Microsoft Windows [versão 10.0]  ·  digite \"ajuda\" para ver os comandos");
        avisoPc = "O computador reiniciou.";
        if (modal == Modal.Computador) MontarComputador();
        MissionManager.Instance?.Verificar();
    }
}
