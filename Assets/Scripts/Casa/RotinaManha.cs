using UnityEngine;

// Rotina da manhã (Fase 1A): acordou em casa → café → ver os chamados (tablet, celular ou PC) → pegar a mochila
// → sair de casa. Não obriga nada: só lembra o próximo passo com um aviso curto e comemora quando sai pronto.
// O bônus de energia do café da manhã fica na Cafeteira. Começa a valer a partir do 2º dia (o 1º tem o tutorial).
public class RotinaManha : MonoBehaviour
{
    public static RotinaManha Instance;

    bool ativa, cafe, chamados, mochila;
    float proximoLembrete;
    EventBinding<DiaIniciadoEvent> bDia;

    // Interior da casa (MontarCenario.CentroCasa, 8 x 6 m)
    static readonly Vector3 CentroCasa = new Vector3(0f, 0f, -19.5f);
    const float MeiaLargura = 4f, MeiaProfundidade = 3f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (Instance != null || GameObject.Find("SpawnCasa") == null) return;
        Instance = new GameObject("RotinaManha").AddComponent<RotinaManha>();
    }

    void Awake()
    {
        bDia = new EventBinding<DiaIniciadoEvent>(_ => Comecar());
        EventBus<DiaIniciadoEvent>.Register(bDia);
        Cafeteira.CafeTomado += AoTomarCafe;
    }

    void OnDestroy()
    {
        EventBus<DiaIniciadoEvent>.Deregister(bDia);
        Cafeteira.CafeTomado -= AoTomarCafe;
        if (Instance == this) Instance = null;
    }

    void Comecar()
    {
        ativa = true;
        cafe = chamados = mochila = false;
        proximoLembrete = Time.time + 25f;
        GameUI.Instance?.Toast("Bom dia! Rotina: um café, ver os chamados, pegar a mochila e sair.", UIFactory.Destaque, 5f);
        Debug.Log("[Rotina] Começou a rotina da manhã.");
    }

    void AoTomarCafe() { if (ativa && !cafe) { cafe = true; Passo("café tomado"); } }

    int Feitos => (cafe ? 1 : 0) + (chamados ? 1 : 0) + (mochila ? 1 : 0);

    void Passo(string qual)
    {
        GameUI.Instance?.Toast($"Rotina da manhã {Feitos}/3 • {qual}", UIFactory.Sucesso, 2.2f);
        Debug.Log($"[Rotina] {Feitos}/3: {qual}");
        proximoLembrete = Time.time + 40f;
    }

    void Update()
    {
        if (!ativa) return;
        var d = DiaManager.Instance;
        if (d != null && d.Hora >= 11f) { ativa = false; return; }   // a manhã passou

        var ui = GameUI.Instance;
        if (!chamados && ui != null && ui.VendoChamados) { chamados = true; Passo("chamados vistos"); }
        if (!mochila && Mochila.Instance != null && Mochila.Instance.comVoce) { mochila = true; Passo("mochila nas costas"); }

        var jogador = GameObject.FindWithTag("Player");
        if (jogador != null)
        {
            Vector3 p = jogador.transform.position - CentroCasa;
            if (Mathf.Abs(p.x) > MeiaLargura + 0.3f || Mathf.Abs(p.z) > MeiaProfundidade + 0.3f)
            {
                ativa = false;
                if (Feitos == 3) ui?.Toast("Saiu de casa pronto para o dia.", UIFactory.Sucesso, 3f);
                return;
            }
        }

        // Um lembrete do próximo passo de vez em quando (nunca com painel aberto)
        if (Time.time >= proximoLembrete && ui != null && !ui.PainelAberto)
        {
            proximoLembrete = Time.time + 40f;
            string falta = !cafe ? "um café na cozinha dá energia para a manhã"
                         : !chamados ? "veja os chamados do dia (Tab, celular ou o PC)"
                         : !mochila ? "a mochila com as ferramentas está no home office"
                         : "tudo pronto: pode sair";
            ui.Toast("Rotina da manhã: " + falta + ".", UIFactory.CorSuave, 3f);
        }
    }
}
