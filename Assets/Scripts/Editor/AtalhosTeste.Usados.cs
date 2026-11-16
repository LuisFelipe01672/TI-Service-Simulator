using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Atalhos de teste da Etapa 6 (bancada e usados como objetos). Menu "TI Service/Teste (Play)/Usados na bancada".
// Só funcionam com o jogo rodando.
public static partial class AtalhosTeste
{
    const string MenuUs = Menu + "Usados na bancada/";

    // Ordem de trabalho na bancada (o primeiro rótulo disponível é feito)
    static readonly string[] OrdemBancada =
    {
        "Ligar na tomada", "Espetar a placa POST", "Trocar a peça ruim", "Desmontar e aproveitar", "Limpar", "Apagar",
        "Colar etiqueta", "Colocar na frota", "Fotografar e anunciar",
    };

    static void PrepararMochilaUsados()
    {
        var m = Mochila.Instance;
        if (m == null) return;
        m.comVoce = true;
        foreach (var it in new[] { Item.PlacaPOST, Item.TestadorFonte })
            if (m.Quantidade(it) < 1) m.Adicionar(it, 1);
        foreach (var it in new[] { Item.Memoria, Item.Cilindro })
            if (m.Quantidade(it) < 2) m.Adicionar(it, 2);
        GameEconomy.Instance?.ReceberPagamento(1500f);
    }

    [MenuItem(MenuUs + "1. Lote de teste (um de cada) e ir até as caixas", false, 700)]
    static void UsLote()
    {
        if (Jogador() == null) return;
        PrepararMochilaUsados();
        var lote = new (TipoUsado t, string nome, float v, bool d, CondicaoUsado c, float s)[]
        {
            (TipoUsado.Computador, "Computador de mesa usado", 420f, true, CondicaoUsado.DefeitoLeve, 0.1f),
            (TipoUsado.Impressora, "Impressora laser usada", 380f, true, CondicaoUsado.DefeitoLeve, 0f),
            (TipoUsado.Notebook, "Notebook usado", 650f, true, CondicaoUsado.Bom, 0f),
            (TipoUsado.Monitor, "Monitor 19\" usado", 160f, false, CondicaoUsado.DefeitoLeve, 0f),
            (TipoUsado.Roteador, "Roteador usado", 90f, false, CondicaoUsado.Bom, 0f),
            (TipoUsado.Switch, "Switch 8 portas usado", 110f, false, CondicaoUsado.Sucata, 0f),
            (TipoUsado.Computador, "Computador de mesa usado", 420f, true, CondicaoUsado.Sucata, 0f),
        };
        foreach (var l in lote) EquipamentosUsados.Adicionar(EquipamentosUsados.Novo(l.t, l.nome, l.v, l.d, l.c, l.s));
        UsadosFisicos.Instance?.Sincronizar();
        Debug.Log($"[Teste] Lote de teste: {lote.Length} caixas no escritório (total {EquipamentosUsados.Estoque.Count}).");
        var sala = GameObject.Find("Cenario/Escritorio")?.transform;
        if (sala != null) Ir(sala.TransformPoint(new Vector3(1.0f, 1.1f, -0.6f)), sala.TransformPoint(new Vector3(1.6f, 0.2f, -2.4f)));
    }

    [MenuItem(MenuUs + "2. Ir para a bancada", false, 701)]
    static void UsIrBancada()
    {
        var b = UsadosFisicos.Instance != null ? UsadosFisicos.Instance.BancadaAtual : null;
        if (b == null) { Debug.LogWarning("[Teste] Bancada não encontrada."); return; }
        Ir(b.transform.TransformPoint(new Vector3(0.12f, 1.1f, 0.75f)), b.transform.TransformPoint(UsadosFisicos.PosNaBancada + Vector3.up * 0.12f));
    }

    static EquipUsado NaBancada() => EquipamentosUsados.Estoque.FirstOrDefault(e => e.local == LocalUsado.Bancada);

    // Abre a caixa (se precisar), pega e põe na bancada o próximo que ainda não foi testado
    [MenuItem(MenuUs + "3. Próximo usado para a bancada", false, 702)]
    static void UsProximo()
    {
        var u = UsadosFisicos.Instance;
        if (Jogador() == null || u == null) return;
        if (UsadosFisicos.NaMao != null) u.Largar();
        var atual = NaBancada();
        if (atual != null) { u.Pegar(atual); u.Largar(); }
        var e = EquipamentosUsados.Estoque.FirstOrDefault(x => !x.testado && x.local != LocalUsado.Bancada)
             ?? EquipamentosUsados.Estoque.FirstOrDefault(x => x != atual && !x.anunciado);
        if (e == null) { Debug.Log("[Teste] Nenhum usado para levar à bancada."); return; }
        var obj = u.Objeto(e);
        if (e.local == LocalUsado.Caixa && obj != null) obj.Interagir();
        u.Pegar(e);
        Debug.Log("[Teste] " + u.ColocarNaBancada());
        UsIrBancada();
    }

    static bool PassoBancada()
    {
        var e = NaBancada();
        if (e == null) return false;
        var acoes = AcoesBancada.Para(e);
        foreach (var inicio in OrdemBancada)
        {
            var a = acoes.FirstOrDefault(x => x.rotulo.StartsWith(inicio));
            if (a == null) continue;
            if (inicio == "Colocar na frota" && !EquipamentosUsados.PodeIrParaFrota(e, out _)) continue;
            if (a.requer != null && Mochila.Instance != null && !Mochila.Instance.Tem(a.requer.Value)) continue;
            Debug.Log($"[Teste] {e.nome}: {a.rotulo} → {a.executar()}");
            return true;
        }
        if (e.condicao == CondicaoUsado.Sucata && e.testado)
        {
            var u = UsadosFisicos.Instance;
            u.Pegar(e);
            Debug.Log("[Teste] " + u.DescartarNaMao());
            return true;
        }
        return false;
    }

    [MenuItem(MenuUs + "4. Próxima ação na bancada", false, 703)]
    static void UsPasso()
    {
        if (Jogador() == null) return;
        if (!PassoBancada()) Debug.Log("[Teste] Nada mais a fazer com o equipamento da bancada (use o 3 para o próximo).");
        FocarJogo();
    }

    // Todos os usados: bancada → ações até o destino; depois o dia vira e os anúncios são vendidos
    [MenuItem(MenuUs + "5. Ciclo inteiro: todos os usados até o destino (log)", false, 704)]
    static void UsCiclo()
    {
        var u = UsadosFisicos.Instance;
        if (Jogador() == null || u == null) return;
        PrepararMochilaUsados();
        float saldo = GameEconomy.Instance != null ? GameEconomy.Instance.saldo : 0f;
        int frota = ContratosManager.Instance != null ? ContratosManager.Instance.patrimonio.Count : 0;
        int velhas = Mochila.Instance != null ? Mochila.Instance.Quantidade(Item.PecaVelha) : 0;
        for (int guarda = 0; guarda < 20; guarda++)
        {
            var e = EquipamentosUsados.Estoque.FirstOrDefault(x => !x.anunciado);
            if (e == null) break;
            if (UsadosFisicos.NaMao != null) u.Largar();
            var atual = NaBancada();
            if (atual != null && atual != e) { u.Pegar(atual); u.Largar(); }
            if (e.local != LocalUsado.Bancada)
            {
                if (e.local == LocalUsado.Caixa) u.Objeto(e)?.Interagir();
                u.Pegar(e);
                u.ColocarNaBancada();
            }
            int passos = 0;
            while (EquipamentosUsados.Estoque.Contains(e) && !e.anunciado && passos++ < 15 && PassoBancada()) { }
        }
        var anunciados = EquipamentosUsados.Estoque.Where(x => x.anunciado).ToList();
        foreach (var e in anunciados) if (e.local == LocalUsado.Bancada) { u.Pegar(e); u.Largar(); }
        foreach (var m in EquipamentosUsados.VenderAnunciados()) Debug.Log("[Teste] Comprador: " + m);
        Debug.Log($"[Teste] Ciclo dos usados: saldo {saldo:0} → {GameEconomy.Instance?.saldo:0}, patrimônio {frota} → {ContratosManager.Instance?.patrimonio.Count}, " +
                  $"peças velhas na mochila {velhas} → {Mochila.Instance?.Quantidade(Item.PecaVelha)}, usados restantes {EquipamentosUsados.Estoque.Count}");
        var d = Object.FindAnyObjectByType<DescarteEletronico>();
        if (d != null && Mochila.Instance != null && Mochila.Instance.Quantidade(Item.PecaVelha) > 0)
        {
            Debug.Log("[Teste] " + d.TextoPrompt);
            d.Interagir();
            Debug.Log($"[Teste] Peças velhas depois do lixo eletrônico: {Mochila.Instance.Quantidade(Item.PecaVelha)}");
        }
    }

    // Fotos: caixas do lote, caixa aberta, equipamento na mão, na bancada, teste, troca, apagamento, etiqueta, anúncio
    [MenuItem(MenuUs + "6. Fotografar o ciclo", false, 705)]
    static void UsFotos()
    {
        if (Jogador() == null) return;
        GameUI.Instance?.FecharModal();
        System.IO.Directory.CreateDirectory("Builds/Capturas/usados");
        var u = UsadosFisicos.Instance;
        EquipUsado pc = null, imp = null;
        void Bancada(EquipUsado e) { if (UsadosFisicos.NaMao != null) u.Largar(); var a = NaBancada(); if (a != null && a != e) { u.Pegar(a); u.Largar(); } if (e.local == LocalUsado.Caixa) u.Objeto(e)?.Interagir(); u.Pegar(e); u.ColocarNaBancada(); UsIrBancada(); }
        string Fazer(EquipUsado e, string inicio) { var a = AcoesBancada.Para(e).FirstOrDefault(x => x.rotulo.StartsWith(inicio)); return a != null ? a.executar() : $"(sem ação {inicio})"; }
        var passos = new List<(System.Action fazer, float espera, string arquivo)>
        {
            (() => { UsLote(); pc = EquipamentosUsados.Estoque[EquipamentosUsados.Estoque.Count - 7]; imp = EquipamentosUsados.Estoque[EquipamentosUsados.Estoque.Count - 6]; }, 1.5f, "1_caixas_do_lote"),
            (() => { u.Objeto(pc)?.Interagir(); u.Objeto(imp)?.Interagir(); }, 1.6f, "2_caixas_abertas"),
            (() => { u.Pegar(pc); }, 1.0f, "3_na_mao"),
            (() => { u.ColocarNaBancada(); UsIrBancada(); }, 1.2f, "4_na_bancada"),
            (() => Debug.Log("[Teste] " + Fazer(pc, "Ligar na tomada")), 2.4f, "5_teste_sem_video"),
            (() => Debug.Log("[Teste] " + Fazer(pc, "Espetar a placa POST")), 2.6f, "6_placa_post_55"),
            (() => Debug.Log("[Teste] " + Fazer(pc, "Trocar a peça ruim")), 0.9f, "7_memoria_saindo"),
            (() => { }, 3.0f, "8_ligou_com_dados"),
            (() => Debug.Log("[Teste] " + Fazer(pc, "Limpar")), 0.6f, "9_limpando"),
            (() => Debug.Log("[Teste] " + Fazer(pc, "Apagar os dados")), 4.0f, "10_apagando_dados"),
            (() => { Debug.Log("[Teste] " + Fazer(pc, "Colar etiqueta")); Debug.Log("[Teste] " + Fazer(pc, "Fotografar e anunciar")); }, 1.2f, "11_etiqueta_e_anuncio"),
            (() => { Bancada(imp); Debug.Log("[Teste] " + Fazer(imp, "Ligar na tomada")); }, 3.2f, "12_impressora_listra"),
            (() => { Fazer(imp, "Trocar a peça ruim"); Fazer(imp, "Apagar"); Fazer(imp, "Colar etiqueta"); }, 4.0f, "13_impressora_recuperada"),
            (() => { var suc = EquipamentosUsados.Estoque.LastOrDefault(x => x.condicao == CondicaoUsado.Sucata && x.tipo == TipoUsado.Computador); if (suc != null) { Bancada(suc); Fazer(suc, "Ligar na tomada"); } }, 1.4f, "14_sucata_fumaca"),
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
                ScreenCapture.CaptureScreenshot($"Builds/Capturas/usados/{arquivo}.png");
                Debug.Log($"[Teste] Foto: {arquivo}.png");
                capturar = false;
                proximo = EditorApplication.timeSinceStartup + 0.4;
                return;
            }
            if (i >= passos.Count)
            {
                EditorApplication.update -= Passo;
                Debug.Log("[Teste] Fotos dos usados prontas em Builds/Capturas/usados");
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
