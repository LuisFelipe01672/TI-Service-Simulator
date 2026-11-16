using UnityEditor;
using UnityEngine;

// Atalhos de teste da Etapa 7 (notebook e problemas de software). Menu "TI Service/Teste (Play)/Notebook e software".
// Só funcionam com o jogo rodando. Fotos em Builds/Capturas/notebook.
public static partial class AtalhosTeste
{
    const string MenuNb = Menu + "Notebook e software/";

    [MenuItem(MenuNb + "Notebook: bateria estufada", false, 700)]
    static void NbBateria() => ChamadoDe(TipoEquipamento.Notebook, CatalogoProblemas.TrocarBateriaEstufada);
    [MenuItem(MenuNb + "Notebook: tecla falhando", false, 701)]
    static void NbTecla() => ChamadoDe(TipoEquipamento.Notebook, CatalogoProblemas.LimparTecla);
    [MenuItem(MenuNb + "Notebook: carregador", false, 702)]
    static void NbCarregador() => ChamadoDe(TipoEquipamento.Notebook, CatalogoProblemas.TrocarCarregador);
    [MenuItem(MenuNb + "Notebook: superaquecimento", false, 703)]
    static void NbVentoinha() => ChamadoDe(TipoEquipamento.Notebook, CatalogoProblemas.LimparVentoinha);
    [MenuItem(MenuNb + "Notebook: dobradiça", false, 704)]
    static void NbDobradica() => ChamadoDe(TipoEquipamento.Notebook, CatalogoProblemas.TrocarDobradica);
    [MenuItem(MenuNb + "Notebook: vírus e propaganda", false, 705)]
    static void NbVirus() => ChamadoDe(TipoEquipamento.Notebook, CatalogoProblemas.RemoverVirus);

    [MenuItem(MenuNb + "Computador: senha bloqueada", false, 720)]
    static void SwSenha() => ChamadoDe(TipoEquipamento.Computador, CatalogoProblemas.DesbloquearConta);
    [MenuItem(MenuNb + "Computador: e-mail não envia", false, 721)]
    static void SwEmail() => ChamadoDe(TipoEquipamento.Computador, CatalogoProblemas.CorrigirEmail);
    [MenuItem(MenuNb + "Computador: vírus e propaganda", false, 722)]
    static void SwVirus() => ChamadoDe(TipoEquipamento.Computador, CatalogoProblemas.RemoverVirus);
    [MenuItem(MenuNb + "Computador: lentidão (inicialização cheia)", false, 723)]
    static void SwInicio() => ChamadoDe(TipoEquipamento.Computador, CatalogoProblemas.LimparInicializacao);
    [MenuItem(MenuNb + "Computador: atualização quebrou o sistema contábil", false, 724)]
    static void SwAtualizacao() => ChamadoDe(TipoEquipamento.Computador, CatalogoProblemas.DesfazerAtualizacao);
    [MenuItem(MenuNb + "Computador: impressora padrão errada", false, 725)]
    static void SwPadrao() => ChamadoDe(TipoEquipamento.Computador, CatalogoProblemas.DefinirPadrao);

    [MenuItem(MenuNb + "Abrir a tela do cliente (TI OS)", false, 740)]
    static void AbrirTelaCliente()
    {
        if (!Application.isPlaying || GameUI.Instance == null) return;
        GameUI.Instance.AbrirComputador();
    }

    [MenuItem(MenuNb + "Ir ao notebook", false, 741)]
    static void IrAoNotebook() => IrPerto(TipoEquipamento.Notebook, 0.6f);

    [MenuItem(MenuNb + "Ir ao monitor do computador", false, 742)]
    static void IrAoMonitorPc() => IrPerto(TipoEquipamento.Computador, 0.75f);

    [MenuItem(MenuNb + "Foto (Builds/Capturas/notebook)", false, 743)]
    static void FotoNb()
    {
        if (!Application.isPlaying) return;
        var mm = MissionManager.Instance;
        string nome = mm?.chamadoAtivo?.problema != null ? mm.chamadoAtivo.problema.solucao : "sem_chamado";
        string pasta = System.IO.Path.Combine(Application.dataPath, "../Builds/Capturas/notebook");
        System.IO.Directory.CreateDirectory(pasta);
        string arq = System.IO.Path.Combine(pasta, $"{System.DateTime.Now:HHmmss}_{nome.Replace(' ', '_')}.png");
        ScreenCapture.CaptureScreenshot(arq);
        Debug.Log("[Teste] Foto: " + arq);
    }

    // Atende de casa (acesso remoto) um chamado de software sem ir ao cliente: mostra o resultado no log
    [MenuItem(MenuNb + "Remoto: tentar todos os problemas novos (log)", false, 760)]
    static void RemotoTodos()
    {
        var mm = MissionManager.Instance;
        if (!Application.isPlaying || mm == null) return;
        if (mm.chamadoAtivo != null) mm.AbandonarChamado();
        foreach (var (t, s) in new[] {
            (TipoEquipamento.Computador, CatalogoProblemas.DefinirPadrao), (TipoEquipamento.Computador, CatalogoProblemas.DesbloquearConta),
            (TipoEquipamento.Computador, CatalogoProblemas.CorrigirEmail), (TipoEquipamento.Computador, CatalogoProblemas.LimparInicializacao),
            (TipoEquipamento.Computador, CatalogoProblemas.RemoverVirus), (TipoEquipamento.Computador, CatalogoProblemas.DesfazerAtualizacao),
            (TipoEquipamento.Notebook, CatalogoProblemas.RemoverVirus), (TipoEquipamento.Notebook, CatalogoProblemas.TrocarCarregador) })
        {
            if (mm.chamadosDisponiveis.Count == 0) mm.GerarChamados();
            var c = mm.chamadosDisponiveis[0];
            PrepararChamado(c, Achar(t, s));
            // tenta até o cliente atender (20% de chance de estar ausente)
            MissionManager.ResultadoRemoto r = MissionManager.ResultadoRemoto.ClienteAusente;
            string msg = "";
            for (int i = 0; i < 8 && r == MissionManager.ResultadoRemoto.ClienteAusente; i++) r = mm.AtenderRemoto(c, out msg);
            Debug.Log($"[Teste] Remoto {t}/{s}: {r} — {msg}");
            if (r != MissionManager.ResultadoRemoto.Resolvido) mm.chamadosDisponiveis.Remove(c);
        }
    }

    // Passos com as mãos, pela mesma lista de ações do jogo (log com o resultado de cada um)
    [MenuItem(MenuNb + "Passo: desligar, tirar da tomada e abrir por baixo", false, 750)]
    static void PassoAbrirPorBaixo() => Passos(TipoEquipamento.Notebook, "Desligar o notebook", "Tirar o carregador da tomada", "Virar e tirar os 10 parafusos");
    [MenuItem(MenuNb + "Passo: trocar a bateria e fechar", false, 751)]
    static void PassoTrocarBateria() => Passos(TipoEquipamento.Notebook, "Desconectar e retirar a bateria", "Instalar a bateria nova", "Limpar a ventoinha",
                                               "Encaixar a tampa de baixo", "Ligar o carregador na tomada", "Ligar o notebook");
    [MenuItem(MenuNb + "Passo: soltar a moldura e trocar a dobradiça", false, 752)]
    static void PassoDobradica() => Passos(TipoEquipamento.Notebook, "Desligar o notebook", "Soltar a moldura", "Trocar as dobradiças", "Encaixar a moldura", "Ligar o notebook");
    [MenuItem(MenuNb + "Passo: tirar a capinha da tecla A", false, 753)]
    static void PassoTecla() => Passos(TipoEquipamento.Notebook, "Testar o teclado", "Tirar a capinha da tecla A");

    static void Passos(TipoEquipamento t, params string[] inicios)
    {
        var mm = MissionManager.Instance;
        if (!Application.isPlaying || mm?.Estado == null) { Debug.LogWarning("[Teste] Sem chamado ativo."); return; }
        foreach (var inicio in inicios)
        {
            AcaoFisica achada = null;
            foreach (var a in AcoesFisicas.Para(t, mm.Estado)) if (a.rotulo.StartsWith(inicio)) { achada = a; break; }
            if (achada == null) { Debug.Log($"[Teste] Passo \"{inicio}\": não disponível agora"); continue; }
            string r = achada.executar != null ? achada.executar() : "";
            mm.RegistrarAcao(achada.rotulo);
            Debug.Log($"[Teste] Passo \"{achada.rotulo}\": {r}");
        }
        Debug.Log($"[Teste] Resolvido? {CatalogoProblemas.Resolvido(mm.chamadoAtivo.problema, mm.Estado)}");
    }

    static Problema Achar(TipoEquipamento t, string solucao)
    {
        foreach (var lista in new[] { CatalogoProblemas.Faceis, CatalogoProblemas.Medios, CatalogoProblemas.Dificeis })
            foreach (var x in lista) if (x.equipamento == t && x.solucao == solucao) return x;
        return null;
    }

    static void PrepararChamado(Chamado c, Problema p)
    {
        c.exigeNota = false;
        c.tutorial = false;
        c.contratoId = 0;
        c.problema = p;
        c.equipamento = p.equipamento;
        c.solucao = p.solucao;
        c.descricaoProblema = p.descricao;
        c.tipo = RegistroEquipamentos.De(p.equipamento).TipoChamado;
        if (c.personalidade == Personalidade.Confuso) c.personalidade = Personalidade.Tranquilo;
    }

    // Aceita um chamado com o problema escolhido e leva o técnico até o equipamento
    static void ChamadoDe(TipoEquipamento t, string solucao)
    {
        var mm = MissionManager.Instance;
        if (!Application.isPlaying || mm == null) { Debug.LogWarning("[Teste] Aperte Play antes."); return; }
        if (mm.chamadoAtivo != null) mm.AbandonarChamado();
        if (mm.chamadosDisponiveis.Count == 0) mm.GerarChamados();
        var p = Achar(t, solucao);
        if (p == null || mm.chamadosDisponiveis.Count == 0) { Debug.LogWarning($"[Teste] Sem problema/chamado para {t}: {solucao}"); return; }
        var c = mm.chamadosDisponiveis[0];
        PrepararChamado(c, p);
        GameUI.Instance?.FecharModal();
        mm.AceitarChamado(c);
        Debug.Log($"[Teste] Chamado aceito: {t} — {solucao} | sala {SalaCliente.Atual?.Nome} | resolvido? {CatalogoProblemas.Resolvido(p, mm.Estado)}");
        IrPerto(t, t == TipoEquipamento.Notebook ? 0.6f : 0.95f);
    }

    static void IrPerto(TipoEquipamento tipo, float distancia)
    {
        foreach (var e in Object.FindObjectsByType<Equipamento>())
        {
            if (e.tipoEquipamento != tipo) continue;
            Vector3 centro = e.GetComponent<Collider>() != null ? e.GetComponent<Collider>().bounds.center : e.transform.position;
            Vector3 dir = -e.transform.forward;
            dir.y = 0f;
            dir.Normalize();
            indicePeca = -1;
            Ir(new Vector3(centro.x, 1.1f, centro.z) + dir * distancia, centro + Vector3.up * 0.08f);
            return;
        }
        Debug.LogWarning("[Teste] Equipamento não encontrado: " + tipo);
    }
}
