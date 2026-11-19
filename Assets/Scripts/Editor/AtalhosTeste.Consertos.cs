using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Atalhos de teste da Etapa 10B (consertos pedidos pelo colega testador): fotos do roteador e da mesa do computador
// nas três salas de cliente e da bancada do escritório. Menu "TI Service/Teste (Play)/Consertos".
public static partial class AtalhosTeste
{
    const string MenuConsertos = Menu + "Consertos/";

    static void ChamadoNaSala(string cliente, TipoEquipamento t, string solucao)
    {
        var mm = MissionManager.Instance;
        if (mm == null) return;
        if (mm.chamadoAtivo != null) mm.AbandonarChamado();
        if (mm.chamadosDisponiveis.Count == 0) mm.GerarChamados();
        var p = Achar(t, solucao);
        if (p == null || mm.chamadosDisponiveis.Count == 0) return;
        var c = mm.chamadosDisponiveis[0];
        PrepararChamado(c, p);
        c.nomeCliente = cliente;
        GameUI.Instance?.FecharModal();
        AceitarComTeleporte(mm, c);
        Debug.Log($"[Teste] {cliente}: chamado de {t} na sala {SalaCliente.Atual?.Nome}");
    }

    static void VisaoGeral(TipoEquipamento t)
    {
        foreach (var e in Object.FindObjectsByType<Equipamento>())
        {
            if (e.tipoEquipamento != t) continue;
            Vector3 alvo = e.transform.position;
            Vector3 dir = -e.transform.forward; dir.y = 0f; dir.Normalize();
            Ir(new Vector3(alvo.x, 1.1f, alvo.z) + dir * 2.4f, alvo);
            return;
        }
    }

    static void SequenciaFotos(string pasta, List<(System.Action fazer, float espera, string arquivo)> passos)
    {
        System.IO.Directory.CreateDirectory($"Builds/Capturas/{pasta}");
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
                ScreenCapture.CaptureScreenshot($"Builds/Capturas/{pasta}/{arquivo}.png");
                Debug.Log($"[Teste] Foto: {arquivo}.png");
                capturar = false;
                proximo = EditorApplication.timeSinceStartup + 0.4;
                return;
            }
            if (i >= passos.Count) { EditorApplication.update -= Passo; Debug.Log($"[Teste] Fotos prontas em Builds/Capturas/{pasta}"); return; }
            var p = passos[i++];
            GameUI.Instance?.FecharModal();
            p.fazer();
            arquivo = p.arquivo;
            capturar = arquivo != null;
            proximo = EditorApplication.timeSinceStartup + p.espera;
        }
        EditorApplication.update += Passo;
    }

    [MenuItem(MenuConsertos + "1. Fotos: roteador e mesa do computador nas 10 salas", false, 1200)]
    static void FotosSalasConsertos()
    {
        if (Jogador() == null) return;
        var passos = new List<(System.Action, float, string)>();
        foreach (var cliente in clientesRede)
        {
            string c = cliente, n = SalaCliente.EnderecoDoCliente(cliente).ToString().ToLowerInvariant();
            passos.Add((() => ChamadoNaSala(c, TipoEquipamento.Roteador, "Reiniciar roteador"), 2.5f, null));
            passos.Add((() => VisaoGeral(TipoEquipamento.Roteador), 1.2f, $"{n}_roteador_longe"));
            passos.Add((() => IrPerto(TipoEquipamento.Roteador, 0.9f), 1.0f, $"{n}_roteador_perto"));
            passos.Add((() => VisaoGeral(TipoEquipamento.Computador), 1.2f, $"{n}_mesa_computador"));
        }
        SequenciaFotos("consertos", passos);
    }

    // Chama um método privado da GameUI (abrir aba do tablet, app do computador) só para as fotos
    static void ChamarGameUI(string metodo, params object[] args)
    {
        var ui = GameUI.Instance;
        if (ui == null) return;
        var m = typeof(GameUI).GetMethod(metodo, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public,
            null, System.Array.ConvertAll(args, a => a.GetType()), null);
        m?.Invoke(ui, args);
    }

    static void CampoGameUI(string campo, object valor)
    {
        var f = typeof(GameUI).GetField(campo, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        if (f == null) return;
        f.SetValue(GameUI.Instance, f.FieldType.IsEnum ? System.Enum.Parse(f.FieldType, valor.ToString()) : valor);
    }

    [MenuItem(MenuConsertos + "3. Fotos dos consertos (notebook, driver, Negócios, bancadas)", false, 1202)]
    static void FotosConsertos()
    {
        if (Jogador() == null) return;
        var passos = new List<(System.Action, float, string)>
        {
            (() => ChamadoNaSala("Clínica Bem Viver", TipoEquipamento.Roteador, "Reiniciar roteador"), 2.5f, null),
            (() => IrPerto(TipoEquipamento.Computador, 1.3f), 1.2f, "depois_clinica_mesa_sem_notebook"),
            (() => ChamadoNaSala("Clínica Bem Viver", TipoEquipamento.Notebook, CatalogoProblemas.TrocarDobradica), 2.5f, null),
            (() => IrPerto(TipoEquipamento.Computador, 1.3f), 1.2f, "depois_clinica_chamado_de_notebook"),
            (() => { TesteDriver(); }, 2.5f, null),
            (() => { GameUI.Instance?.AbrirComputador(); CampoGameUI("appAtual", "Impressoras"); ChamarGameUI("MontarComputador"); }, 1.2f, "depois_driver_na_tela"),
            (() => { CampoGameUI("abasLiberadas", 0xFFFF); GameUI.Instance?.AbrirTablet(); CampoGameUI("secaoNegocios", null); ChamarGameUI("TrocarAba", 6); }, 1.2f, "depois_negocios_menu"),
            (() => { GameUI.Instance?.AbrirTablet(); CampoGameUI("secaoNegocios", "projetos"); ChamarGameUI("TrocarAba", 6); ChamarGameUI("AtualizarTablet"); }, 1.2f, "depois_negocios_projetos"),
            (() => { var sala = GameObject.Find("Cenario/Escritorio")?.transform; if (sala != null) Ir(sala.TransformPoint(new Vector3(-1.2f, 1.1f, 0.1f)), sala.TransformPoint(new Vector3(2.7f, 0.6f, 0.1f))); }, 1.2f, "depois_duas_bancadas"),
        };
        SequenciaFotos("consertos", passos);
    }

    [MenuItem(MenuConsertos + "2. Foto: bancada do escritório", false, 1201)]
    static void FotoBancadaConsertos()
    {
        if (Jogador() == null) return;
        var b = Object.FindAnyObjectByType<Bancada>();
        var passos = new List<(System.Action, float, string)>();
        foreach (var bancada in Object.FindObjectsByType<Bancada>())
        {
            var bb = bancada;
            passos.Add((() => { var t = bb.transform; Ir(t.position + t.forward * 1.6f + Vector3.up * 1.1f, t.position + Vector3.up * 0.8f); }, 1.2f, $"bancada_{bb.name}"));
        }
        if (b != null)
        {
            var t = b.transform;
            passos.Add((() => Ir(t.position + t.forward * 2.8f + t.right * 1.5f + Vector3.up * 1.1f, t.position + t.right * 1.2f), 1.2f, "parede_das_bancadas"));
        }
        SequenciaFotos("consertos", passos);
    }
}
