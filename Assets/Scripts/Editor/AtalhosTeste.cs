using UnityEditor;
using UnityEngine;

// Atalhos para testar no Play: levam o jogador a um ponto exato e miram num alvo.
// Menu "TI Service/Teste (Play)". Só funcionam com o jogo rodando.
public static class AtalhosTeste
{
    const string Menu = "TI Service/Teste (Play)/";

    [MenuItem(Menu + "Escritório: porta", false, 100)]
    static void PortaEscritorio() => Ir(new Vector3(0f, 1.1f, -1.9f), new Vector3(0f, 1.1f, -3f));

    [MenuItem(Menu + "Rua: calçada olhando a casa", false, 101)]
    static void RuaCasa() => Ir(new Vector3(0f, 1.25f, -4.6f), new Vector3(0f, 1.2f, -19f));

    [MenuItem(Menu + "Rua: meio da rua olhando o escritório", false, 102)]
    static void RuaEscritorio() => Ir(new Vector3(-3f, 1.1f, -11f), new Vector3(0f, 1.5f, 0f));

    [MenuItem(Menu + "Casa: porta (lado de fora)", false, 103)]
    static void PortaCasa() => Ir(new Vector3(1.5f, 1.25f, -15.4f), new Vector3(1.5f, 1.1f, -16.6f));

    [MenuItem(Menu + "Casa: dentro, olhando a cama", false, 104)]
    static void Cama()
    {
        var cama = Object.FindAnyObjectByType<Cama>();
        if (cama == null) { Debug.LogWarning("[Teste] Sem cama na cena (rode o menu 3)."); return; }
        Ir(cama.transform.position + new Vector3(1.6f, 1.1f, 0.3f), cama.transform.position + Vector3.up * 0.4f);
    }

    [MenuItem(Menu + "Casa: sentar e usar o PC", false, 106)]
    static void UsarPc()
    {
        var pc = Object.FindAnyObjectByType<ComputadorCasa>();
        if (pc == null || pc.poseSentado == null) { Debug.LogWarning("[Teste] Sem PC de casa (rode o menu 3)."); return; }
        var p = pc.poseSentado.position;
        Ir(new Vector3(p.x, p.y - 0.2f, p.z - 0.5f), pc.transform.position);
        pc.Interagir();
    }

    [MenuItem(Menu + "Casa: cafeteira (tomar café)", false, 110)]
    static void TomarCafe()
    {
        var c = Object.FindAnyObjectByType<Cafeteira>();
        if (c == null) { Debug.LogWarning("[Teste] Sem cafeteira (rode o menu 3)."); return; }
        Ir(c.transform.position + new Vector3(-1.2f, 1.1f, 0f), c.transform.position + Vector3.up * 0.2f);
        c.Interagir();
        Debug.Log($"[Teste] Energia agora: {EnergiaTecnico.Instance?.energia:0}");
    }

    [MenuItem(Menu + "Remoto: tentar todos os chamados abertos (log)", false, 113)]
    static void TestarRemoto()
    {
        var mm = MissionManager.Instance;
        if (!Application.isPlaying || mm == null) return;
        foreach (var c in new System.Collections.Generic.List<Chamado>(mm.chamadosDisponiveis))
        {
            float antes = DiaManager.Instance != null ? DiaManager.Instance.minutoAtual : 0f;
            var r = mm.AtenderRemoto(c, out string msg);
            float gasto = (DiaManager.Instance != null ? DiaManager.Instance.minutoAtual : 0f) - antes;
            Debug.Log($"[Teste] Remoto {c.nomeCliente} ({c.problema?.equipamento}: {c.problema?.solucao}) → {r} em {gasto:0} min — {msg}");
        }
    }

    [MenuItem(Menu + "Casa: comer (geladeira)", false, 115)]
    static void Comer()
    {
        var g = Object.FindAnyObjectByType<Geladeira>();
        if (g == null) { Debug.LogWarning("[Teste] Sem geladeira (rode o menu 3)."); return; }
        Ir(g.transform.position + new Vector3(-1.3f, 1.1f, 0f), g.transform.position + Vector3.up);
        Debug.Log($"[Teste] {g.TextoPrompt}");
        g.Interagir();
        Debug.Log($"[Teste] Energia agora: {EnergiaTecnico.Instance?.energia:0}, saldo {GameEconomy.Instance?.saldo:0}");
    }

    [MenuItem(Menu + "Encomendas: comprar cabo 305 m + conectores e chegar agora", false, 116)]
    static void EncomendasChegar()
    {
        var m = Mochila.Instance;
        if (!Application.isPlaying || m == null) return;
        GameEconomy.Instance?.ReceberPagamento(600f);
        Debug.Log("[Teste] " + m.Comprar(Item.CaixaCabo, true));
        Debug.Log("[Teste] " + m.Comprar(Item.ConectorRJ45, true));
        foreach (var e in m.entregas) e.minuto = 0f;   // chegam já
    }

    [MenuItem(Menu + "Encomendas: testar MegaShop com defeito e atraso", false, 119)]
    static void EncomendaProblema()
    {
        var m = Mochila.Instance;
        if (!Application.isPlaying || m == null) return;
        GameEconomy.Instance?.ReceberPagamento(300f);
        foreach (var f in Mercado.Fornecedores)
            Debug.Log($"[Teste] {f.nome}: conectores {UIFactory.Dinheiro(Mercado.Preco(Mochila.Info(Item.ConectorRJ45), System.Array.IndexOf(Mercado.Fornecedores, f)))} ({Mercado.VariacaoTexto(Item.ConectorRJ45)})");
        Debug.Log("[Teste] " + m.Comprar(Item.ConectorRJ45, false, 2));
        Debug.Log("[Teste] " + m.Comprar(Item.Toner, false, 2));
        var d = DiaManager.Instance;
        var e1 = m.entregas[m.entregas.Count - 2];   // conectores: chega agora, com defeito
        e1.defeito = true; e1.errado = false; e1.dia = d.dia; e1.minuto = 0f; e1.diaPrevisto = -1;
        var e2 = m.entregas[m.entregas.Count - 1];   // toner: atrasou (o aviso sai agora)
        e2.diaPrevisto = d.dia; e2.minuto = 0f; e2.dia = d.dia + 1;
    }

    [MenuItem(Menu + "Encomendas: listar no log", false, 120)]
    static void EncomendasListar()
    {
        var m = Mochila.Instance;
        if (m == null) return;
        foreach (var e in m.entregas)
            Debug.Log($"[Teste] Entrega {e.item}: dia {e.dia} min {e.minuto:0} | {Mercado.Fornecedores[e.fornecedor].nome} | naPorta {e.naPorta} defeito {e.defeito} errado {e.errado} previsto {e.diaPrevisto} reposição {e.reposicao}");
        Debug.Log($"[Teste] Dia atual {DiaManager.Instance?.dia} — conectores na mochila: {m.Quantidade(Item.ConectorRJ45)}");
    }

    [MenuItem(Menu + "Encomendas: olhar as caixas na porta", false, 118)]
    static void EncomendasOlhar()
    {
        var caixas = Object.FindObjectsByType<EncomendaFisica>();
        if (caixas.Length == 0) { Debug.LogWarning("[Teste] Nenhuma encomenda na porta."); return; }
        Ir(caixas[0].transform.position + caixas[0].transform.forward * -1.3f + Vector3.up * 0.9f, caixas[0].transform.position + Vector3.up * 0.2f);
    }

    [MenuItem(Menu + "Encomendas: abrir todas as da porta", false, 117)]
    static void EncomendasAbrir()
    {
        if (!Application.isPlaying) return;
        var caixas = Object.FindObjectsByType<EncomendaFisica>();
        if (caixas.Length > 0) Ir(caixas[0].transform.position + new Vector3(0f, 1.1f, 1.4f), caixas[0].transform.position);
        foreach (var c in caixas) if (c.enabled) c.Interagir();
        Debug.Log($"[Teste] {caixas.Length} encomenda(s) aberta(s). Cabo na caixa: {Mochila.Instance?.MetrosNaCaixa} m");
    }

    [MenuItem(Menu + "Celular: abrir", false, 114)]
    static void AbrirCelular()
    {
        if (!Application.isPlaying || GameUI.Instance == null) return;
        GameUI.Instance.AbrirCelular();
        FocarJogo();
    }

    // Diagnóstico do [E]: para na frente da mesa e olha a mochila, depois o PC, e diz quem receberia o E
    [MenuItem(Menu + "Diagnóstico: mochila x PC (quem recebe o E)", false, 121)]
    static void DiagE()
    {
        var m = Object.FindAnyObjectByType<MochilaFisica>();
        var pc = Object.FindAnyObjectByType<ComputadorCasa>();
        if (m == null || pc == null) return;
        Vector3 pos = pc.transform.position + new Vector3(0.5f, 0.3f, -1.3f);
        Ir(pos, m.PontoMira);
        var cam = Camera.main;
        foreach (var it in new Interactable[] { m, pc })
            Debug.Log($"[Teste] {it.GetType().Name}: ativo {it.isActiveAndEnabled} perto {it.JogadorPerto(out float dd)} d {dd:0.00}/{it.distanciaInteracao} ângulo {Vector3.Angle(cam.transform.forward, it.PontoMira - cam.transform.position):0} mira {it.PontoMira} cam {cam.transform.position}");
        Debug.Log("[Teste] Olhando a mochila: " + Interactable.MaisProximoEmAlcance()?.TextoPrompt);
        Jogador().OlharPara(pc.PontoMira);
        Debug.Log("[Teste] Olhando o PC: " + Interactable.MaisProximoEmAlcance()?.TextoPrompt);
        var porta = Object.FindAnyObjectByType<Teleporte>();
        if (porta != null) { Ir(porta.transform.position + new Vector3(0.3f, 0f, 1.0f), porta.transform.position + new Vector3(3f, 0f, 3f)); Debug.Log("[Teste] Perto da porta do cliente, olhando para o lado: " + (Interactable.MaisProximoEmAlcance()?.TextoPrompt ?? "(nada)")); Jogador().OlharPara(porta.transform.position); Debug.Log("[Teste] Olhando a porta: " + Interactable.MaisProximoEmAlcance()?.TextoPrompt); }
    }

    [MenuItem(Menu + "Estoque: olhar a estante do escritório", false, 122)]
    static void OlharEstoque()
    {
        var e = Object.FindAnyObjectByType<EstoqueVisual>();
        if (e == null) { Debug.LogWarning("[Teste] Sem estoque (rode o menu 3)."); return; }
        Ir(e.transform.position + new Vector3(1.9f, 1.1f, 0f), e.transform.position + Vector3.up * 0.9f);
    }

    [MenuItem(Menu + "Estoque: descartar toner usado", false, 123)]
    static void Descartar()
    {
        var d = Object.FindAnyObjectByType<DescarteEletronico>();
        if (d == null) return;
        Debug.Log("[Teste] " + d.TextoPrompt);
        d.Interagir();
        Debug.Log($"[Teste] Toner usado restante: {Mochila.Instance?.Quantidade(Item.TonerUsado)}, saldo {GameEconomy.Instance?.saldo:0}");
    }

    [MenuItem(Menu + "Usados: comprar lote e processar tudo (log)", false, 124)]
    static void TestarUsados()
    {
        if (!Application.isPlaying) return;
        GameEconomy.Instance?.ReceberPagamento(1500f);
        bool tem = EquipamentosUsados.LoteDisponivel(out int n, out float preco, out string origem);
        Debug.Log($"[Teste] Dia {DiaManager.Instance?.dia}: lote disponível {tem} — {n} itens, {preco:0}, {origem}");
        if (tem) Debug.Log("[Teste] " + EquipamentosUsados.ComprarLote());
        foreach (var e in new System.Collections.Generic.List<EquipUsado>(EquipamentosUsados.Estoque))
        {
            Debug.Log("[Teste] " + EquipamentosUsados.Testar(e));
            if (e.condicao == CondicaoUsado.DefeitoLeve) Debug.Log("[Teste] " + EquipamentosUsados.Recuperar(e));
            if (e.condicao == CondicaoUsado.Sucata) { Debug.Log("[Teste] " + EquipamentosUsados.Descartar(e)); continue; }
            if (e.temDados) Debug.Log("[Teste] " + EquipamentosUsados.ApagarDados(e));
            Debug.Log("[Teste] " + EquipamentosUsados.Vender(e));
        }
        Debug.Log($"[Teste] Saldo final {GameEconomy.Instance?.saldo:0}, usados restantes {EquipamentosUsados.Estoque.Count}");
    }

    [MenuItem(Menu + "Inventário: abrir", false, 125)]
    static void AbrirInv() { if (Application.isPlaying) { GameUI.Instance?.AbrirInventario(); FocarJogo(); } }

    [MenuItem(Menu + "Inventário: testar limite (log)", false, 126)]
    static void TestarLimite()
    {
        var m = Mochila.Instance;
        if (!Application.isPlaying || m == null) return;
        Debug.Log($"[Teste] Peso {m.PesoAtual:0.00}/{Mochila.CapacidadeKg} kg, livres {m.KgLivres:0.00}");
        int n = 0; while (m.Cabe(Item.Toner, 1) && n < 30) { m.Adicionar(Item.Toner, 1); n++; }
        Debug.Log($"[Teste] Couberam {n} toners; peso {m.PesoAtual:0.00} kg; cabe mais um? {m.Cabe(Item.Toner, 1)}");
        Debug.Log($"[Teste] Guardar 3 toners na estante: {m.Guardar(Item.Toner, 3)} → mochila {m.Quantidade(Item.Toner)}, estante {m.NoEstoque(Item.Toner)}, peso {m.PesoAtual:0.00}");
        Debug.Log($"[Teste] Pegar 1 de volta: {m.Pegar(Item.Toner, 1)} → mochila {m.Quantidade(Item.Toner)}, estante {m.NoEstoque(Item.Toner)}");
    }

    [MenuItem(Menu + "Casa: pegar a mochila", false, 112)]
    static void PegarMochila()
    {
        var m = Object.FindAnyObjectByType<MochilaFisica>();
        if (m == null) { Debug.LogWarning("[Teste] Sem mochila física (rode o menu 3)."); return; }
        Ir(m.transform.position + new Vector3(0.9f, 1.1f, -0.9f), m.transform.position);
        m.Interagir();
        Debug.Log($"[Teste] Mochila com você: {Mochila.Instance?.comVoce}");
    }

    [MenuItem(Menu + "Energia: cansar (-50)", false, 111)]
    static void Cansar()
    {
        if (!Application.isPlaying || EnergiaTecnico.Instance == null) return;
        EnergiaTecnico.Instance.Gastar(50f);
        Debug.Log($"[Teste] Energia agora: {EnergiaTecnico.Instance.energia:0}");
    }

    [MenuItem(Menu + "Fechar o painel aberto (tablet/PC)", false, 109)]
    static void FecharPainel()
    {
        if (!Application.isPlaying || GameUI.Instance == null) return;
        GameUI.Instance.FecharModal();
        FocarJogo();
    }

    [MenuItem(Menu + "Casa: interruptor da sala (apagar/acender)", false, 107)]
    static void InterruptorSala()
    {
        foreach (var i in Object.FindObjectsByType<Interruptor>())
            if (i.name == "Interruptor_Sala") { Ir(i.transform.position + i.transform.forward * 1.2f, i.transform.position); i.Interagir(); return; }
        Debug.LogWarning("[Teste] Sem interruptor (rode o menu 3).");
    }

    [MenuItem(Menu + "Casa: relógio de parede", false, 108)]
    static void RelogioParede()
    {
        var r = Object.FindAnyObjectByType<RelogioAnalogico>();
        if (r == null) { Debug.LogWarning("[Teste] Sem relógio (rode o menu 3)."); return; }
        Ir(r.transform.position + r.frente.normalized * 1.3f + Vector3.down * 0.6f, r.transform.position + Vector3.up * 0.15f);
    }

    [MenuItem(Menu + "Cliente: impressora", false, 120)]
    static void Impressora() => IrAoEquipamento(TipoEquipamento.Impressora);

    [MenuItem(Menu + "Cliente: impressora — abrir/fechar a tampa (teste)", false, 124)]
    static void TampaImpressora()
    {
        if (!Application.isPlaying) return;
        EquipamentoPecas.testeTampaAberta = !EquipamentoPecas.testeTampaAberta;
        Debug.Log("[Teste] Tampa da impressora " + (EquipamentoPecas.testeTampaAberta ? "aberta" : "fechada"));
    }

    [MenuItem(Menu + "Cliente: impressora — olhar o visor de perto", false, 125)]
    static void VisorImpressora()
    {
        var lcd = GameObject.Find("LCD");
        if (lcd == null) { Debug.LogWarning("[Teste] Sem visor (impressora realista)."); return; }
        var eq = lcd.GetComponentInParent<Equipamento>();
        Vector3 frente = eq != null ? -eq.transform.Find("Visual").forward : Vector3.left;
        Ir(lcd.transform.position + frente * 0.45f, lcd.transform.position);
    }

    // Aceita um chamado de teste com um problema escolhido do catálogo (pela solução)
    static void ChamadoTeste(string solucao)
    {
        var mm = MissionManager.Instance;
        if (!Application.isPlaying || mm == null) return;
        if (mm.chamadoAtivo != null) mm.AbandonarChamado();
        Problema p = null;
        foreach (var lista in new[] { CatalogoProblemas.Faceis, CatalogoProblemas.Medios, CatalogoProblemas.Dificeis })
            foreach (var x in lista) if (x.solucao == solucao && (p == null || x.equipamento == TipoEquipamento.Impressora)) p = x;
        if (p == null || mm.chamadosDisponiveis.Count == 0) { Debug.LogWarning("[Teste] Sem problema/chamado para " + solucao); return; }
        foreach (var c in new System.Collections.Generic.List<Chamado>(mm.chamadosDisponiveis))
        {
            c.exigeNota = false;   // teste: qualquer chamado serve
        c.problema = p;
        c.equipamento = p.equipamento;
        c.descricaoProblema = p.descricao;
            mm.AceitarChamado(c);
            if (mm.chamadoAtivo != null) break;
        }
        Debug.Log($"[Teste] Chamado de teste aceito: {solucao}. Visor: {mm.Estado?.VisorImpressora()}");
        IrAoEquipamento(p.equipamento);
    }

    [MenuItem(Menu + "Impressora: chamado de teste — papel atolado", false, 126)]
    static void TestePapelAtolado() => ChamadoTeste("Retirar papel atolado");
    [MenuItem(Menu + "Impressora: chamado de teste — sem papel", false, 127)]
    static void TesteSemPapel() => ChamadoTeste("Colocar papel");
    [MenuItem(Menu + "Impressora: chamado de teste — cilindro riscado", false, 128)]
    static void TesteCilindro() => ChamadoTeste("Trocar cilindro");
    [MenuItem(Menu + "Impressora: chamado de teste — toner", false, 129)]
    static void TesteToner() => ChamadoTeste("Trocar toner");

    // Executa uma ação física da impressora pelo nome (começo do rótulo), como se o jogador tivesse feito
    static void AcaoImpressora(string inicio)
    {
        var mm = MissionManager.Instance;
        if (mm?.Estado == null) { Debug.LogWarning("[Teste] Aceite um chamado de impressora antes."); return; }
        foreach (var a in AcoesFisicas.Para(mm.chamadoAtivo.equipamento, mm.Estado))
            if (a.rotulo.StartsWith(inicio)) { string r = a.executar(); Debug.Log($"[Teste] {a.rotulo} → {r} | Visor: {mm.Estado?.VisorImpressora() ?? "(chamado concluído)"}"); return; }
        Debug.LogWarning("[Teste] Ação não disponível agora: " + inicio);
    }

    [MenuItem(Menu + "Impressora: imprimir página de teste", false, 130)]
    static void AcaoImprimir() => AcaoImpressora("Imprimir página de teste");
    [MenuItem(Menu + "Impressora: levantar/baixar scanner", false, 131)]
    static void AcaoTampa()
    {
        var e = MissionManager.Instance?.Estado;
        AcaoImpressora(e != null && e.impTampaAberta ? "Baixar o scanner" : "Levantar o scanner");
    }
    [MenuItem(Menu + "Impressora: retirar o cartucho", false, 132)]
    static void AcaoRetirar() => AcaoImpressora("Retirar o cartucho");
    [MenuItem(Menu + "Impressora: chacoalhar o cartucho", false, 133)]
    static void AcaoChacoalhar() => AcaoImpressora("Chacoalhar");
    [MenuItem(Menu + "Impressora: recolocar o cartucho", false, 134)]
    static void AcaoRecolocar() => AcaoImpressora("Recolocar o mesmo cartucho");
    [MenuItem(Menu + "Impressora: desconectar/conectar o cabo de rede", false, 135)]
    static void AcaoCabo()
    {
        var e = MissionManager.Instance?.Estado;
        AcaoImpressora(e != null && e.impCabo.conectado ? "Desconectar o cabo" : "Conectar o cabo");
    }
    [MenuItem(Menu + "Impressora: trocar o cilindro", false, 137)]
    static void AcaoCilindro() => AcaoImpressora("Trocar o cilindro");
    [MenuItem(Menu + "Impressora: puxar o papel atolado", false, 136)]
    static void AcaoAtolado() => AcaoImpressora("Abrir a tampa traseira e puxar");

    [MenuItem(Menu + "PC: chamado de teste — poeira", false, 140)]
    static void TestePoeira() => ChamadoTeste("Limpar por dentro");
    [MenuItem(Menu + "PC: chamado de teste — memória", false, 141)]
    static void TesteMemoria() => ChamadoTeste("Trocar memória");
    [MenuItem(Menu + "PC: chamado de teste — fonte", false, 142)]
    static void TesteFonte() => ChamadoTeste("Trocar fonte");
    [MenuItem(Menu + "PC: ligar ou desligar", false, 143)]
    static void PcLigar() { var e = MissionManager.Instance?.Estado; AcaoImpressora(e != null && e.pcLigado ? "Desligar no botão" : "Ligar o computador"); }
    [MenuItem(Menu + "PC: abrir o gabinete", false, 144)]
    static void PcAbrir() => AcaoImpressora("Virar o gabinete");
    [MenuItem(Menu + "PC: olhar por dentro", false, 145)]
    static void PcOlhar() => AcaoImpressora("Olhar por dentro");
    [MenuItem(Menu + "PC: limpar a poeira", false, 146)]
    static void PcLimpar() => AcaoImpressora("Limpar com pincel");
    [MenuItem(Menu + "PC: trocar a memória", false, 147)]
    static void PcMemoria() => AcaoImpressora("Trocar o pente");
    [MenuItem(Menu + "PC: trocar a fonte", false, 148)]
    static void PcFonte() => AcaoImpressora("Trocar a fonte");
    [MenuItem(Menu + "PC: chamado de teste — pasta térmica", false, 150)]
    static void TestePasta() => ChamadoTeste("Trocar pasta térmica");
    [MenuItem(Menu + "PC: chamado de teste — HD", false, 151)]
    static void TesteHd() => ChamadoTeste("Trocar HD");
    [MenuItem(Menu + "PC: tirar o cooler", false, 152)]
    static void PcCooler() => AcaoImpressora("Tirar o cooler");
    [MenuItem(Menu + "PC: limpar a pasta velha", false, 153)]
    static void PcLimparPasta() => AcaoImpressora("Limpar a pasta velha");
    [MenuItem(Menu + "PC: aplicar pasta nova", false, 154)]
    static void PcPasta() => AcaoImpressora("Aplicar pasta térmica");
    [MenuItem(Menu + "PC: recolocar o cooler", false, 155)]
    static void PcRecolocar() => AcaoImpressora("Recolocar o cooler");
    [MenuItem(Menu + "PC: testar o disco", false, 156)]
    static void PcDisco() => AcaoImpressora("Testar a saúde do disco");
    [MenuItem(Menu + "PC: fazer backup", false, 157)]
    static void PcBackup() => AcaoImpressora("Copiar os arquivos");
    [MenuItem(Menu + "PC: trocar o HD", false, 158)]
    static void PcHd() => AcaoImpressora("Trocar o HD");
    [MenuItem(Menu + "PC: instalar o sistema", false, 159)]
    static void PcInstalar() => AcaoImpressora("Instalar o sistema");
    [MenuItem(Menu + "PC: fechar o gabinete", false, 149)]
    static void PcFechar() => AcaoImpressora("Colocar a tampa");

    [MenuItem(Menu + "Cliente: roteador", false, 121)]
    static void Roteador() => IrAoEquipamento(TipoEquipamento.Roteador);

    [MenuItem(Menu + "Cliente: computador", false, 122)]
    static void Computador() => IrAoEquipamento(TipoEquipamento.Computador);

    [MenuItem(Menu + "Cliente: olhar o cliente (aceite um chamado antes)", false, 123)]
    static void OlharCliente()
    {
        var npc = Object.FindAnyObjectByType<ClienteNPC>();
        if (npc == null) { Debug.LogWarning("[Teste] Nenhum cliente na cena: aceite um chamado."); return; }
        var t = npc.transform;
        Ir(t.position + t.forward * 1.8f + Vector3.up * 1.1f, t.position + Vector3.up * 1.2f);
    }

    [MenuItem(Menu + "Mirar: próxima peça do equipamento", false, 140)]
    static void ProximaPeca()
    {
        var jogador = Jogador();
        if (jogador == null) return;
        Equipamento eq = null;
        float menor = float.MaxValue;
        foreach (var e in Object.FindObjectsByType<Equipamento>())
        {
            float d = Vector3.Distance(e.transform.position, jogador.transform.position);
            if (d < menor) { menor = d; eq = e; }
        }
        if (eq == null) return;

        var pecas = eq.GetComponentsInChildren<PecaEquipamento>();
        if (pecas.Length == 0) return;
        indicePeca = (indicePeca + 1) % pecas.Length;
        var p = pecas[indicePeca];
        var bc = p.GetComponent<BoxCollider>();
        Vector3 alvo = bc.bounds.center;
        if (p.id == "traseira" || p.id == "fonte" || p.id == "reset") alvo.y = bc.bounds.max.y - 0.03f;   // por cima
        jogador.OlharPara(alvo);
        FocarJogo();

        // Diagnóstico: o que o raio da mira atinge até a peça
        var cam = jogador.GetComponentInChildren<Camera>().transform;
        var hits = Physics.RaycastAll(cam.position, cam.forward, 3f, ~0, QueryTriggerInteraction.Collide);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        var nomes = new System.Text.StringBuilder();
        foreach (var h in hits) nomes.Append($"{h.collider.name}[{h.collider.GetType().Name},{h.collider.tag}]({h.distance:0.00}{(h.collider.isTrigger ? ",gatilho" : "")}) ");
        var m = PecaEquipamento.Mirada();
        nomes.Append($" → Mirada(): {(m != null ? m.id : "null")}");
        Debug.Log($"[Teste] Mirando: {p.nome} ({p.id}) do(a) {eq.Nome} · centro {alvo} · olho {cam.position} · raio: {nomes}");
    }

    static int indicePeca = -1;

    // ---------- Forçar eventos raros (por reflexão: não abre a API do jogo só para teste) ----------

    const System.Reflection.BindingFlags Privado =
        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public |
        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static;

    [MenuItem(Menu + "Gerar pedido de projeto", false, 180)]
    static void GerarProjeto()
    {
        if (Jogador() == null || ProjetosManager.Instance == null) return;
        typeof(ProjetosManager).GetMethod("NovoContato", Privado)?.Invoke(ProjetosManager.Instance, null);
    }

    [MenuItem(Menu + "Gerar edital de licitação", false, 181)]
    static void GerarEdital()
    {
        if (Jogador() == null) return;
        typeof(Licitacoes).GetMethod("NovoEdital", Privado)?.Invoke(null, null);
    }

    [MenuItem(Menu + "Gerar contato de locação de impressora", false, 182)]
    static void GerarLeadImpressora()
    {
        if (Jogador() == null || ContratosManager.Instance == null) return;
        typeof(ContratosManager).GetMethod("GerarLead", Privado)?.Invoke(ContratosManager.Instance, null);
    }

    [MenuItem(Menu + "Encher a mochila (ferramentas e peças)", false, 183)]
    static void EncherMochila()
    {
        if (Jogador() == null || Mochila.Instance == null) return;
        foreach (Item item in System.Enum.GetValues(typeof(Item)))
            Mochila.Instance.Adicionar(item, item == Item.CaixaCabo ? 305 : 10);
        Debug.Log("[Teste] Mochila cheia.");
    }

    [MenuItem(Menu + "Escritório: bancada (fazer cabo)", false, 105)]
    static void Bancada()
    {
        var b = Object.FindAnyObjectByType<Bancada>();
        if (b == null) return;
        Ir(b.transform.position + b.transform.forward * 1.1f + Vector3.up * 1.1f, b.transform.position + Vector3.up * 0.9f);
    }

    [MenuItem(Menu + "Pular 1 dia", false, 160)]
    static void Pular1() => PularDias(1);

    [MenuItem(Menu + "Pular 10 dias", false, 161)]
    static void Pular10() => PularDias(10);

    // Encerra e começa dias seguidos (como dormir), rodando a virada de cada manager
    static void PularDias(int n)
    {
        if (Jogador() == null || DiaManager.Instance == null) return;
        var mm = MissionManager.Instance;
        bool faliu = false;
        for (int i = 0; i < n && !faliu; i++)
        {
            if (mm != null && mm.chamadoAtivo != null) mm.AbandonarChamado();
            DiaManager.Instance.EncerrarDia(true);
            DiaManager.Instance.ComecarNovoDia();
            faliu = BancoManager.Instance != null && BancoManager.Instance.faliu;
        }
        if (!faliu) GameUI.Instance?.FecharModal();   // na falência, deixa a tela de game over aberta
        Debug.Log($"[Teste] Pulou {n} dia(s): agora é o dia {DiaManager.Instance.dia}");
    }

    static void IrAoEquipamento(TipoEquipamento tipo)
    {
        foreach (var e in Object.FindObjectsByType<Equipamento>())
        {
            if (e.tipoEquipamento != tipo) continue;
            var sala = GameObject.Find("Cenario/Cliente");
            Vector3 centro = e.GetComponent<Collider>() != null ? e.GetComponent<Collider>().bounds.center : e.transform.position;
            Vector3 dir = sala != null ? sala.transform.position - e.transform.position : -e.transform.forward;
            dir.y = 0f;
            dir.Normalize();
            Vector3 pos = new Vector3(centro.x, 1.1f, centro.z) + dir * 0.95f;
            indicePeca = -1;
            Ir(pos, centro);
            return;
        }
        Debug.LogWarning("[Teste] Equipamento não encontrado: " + tipo);
    }

    static PlayerController Jogador()
    {
        if (!Application.isPlaying) { Debug.LogWarning("[Teste] Aperte Play antes."); return null; }
        var go = GameObject.FindWithTag("Player");
        return go != null ? go.GetComponent<PlayerController>() : null;
    }

    static void Ir(Vector3 posicao, Vector3 olharPara)
    {
        var jogador = Jogador();
        if (jogador == null) return;
        var alvo = new GameObject("tmp").transform;
        alvo.position = posicao;
        Teleporte.MoverJogador(jogador.transform, alvo);
        Object.Destroy(alvo.gameObject);
        jogador.OlharPara(olharPara);
        FocarJogo();
    }

    // Devolve o teclado para a aba Game (sem clicar nela, o que giraria a câmera)
    static void FocarJogo()
    {
        var tipo = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
        if (tipo != null) EditorWindow.FocusWindowIfItsOpen(tipo);
    }
}
