using TMPro;
using UnityEngine;
using UnityEngine.UI;

// GPS do celular (Etapa 9B): leva o técnico a pé até um endereço do bairro (Cidade/Bairro.cs), sem teleporte.
// - Ao aceitar um chamado o destino vira o endereço do cliente; no app Mapa do celular dá para escolher outro.
// - No canto inferior esquerdo: para onde ir, a rua, a distância e a direção ("vire à direita", "siga em frente").
// - No endereço, uma coluna de luz verde marca a porta (dá para ver por cima dos telhados).
// - Chegando a 3 m da porta: "Você chegou" e o GPS desliga.
public class GPS : MonoBehaviour
{
    public static GPS Instance { get; private set; }

    public bool Ativo { get; private set; }
    public Endereco Destino { get; private set; }

    Transform jogador, marcador;
    RectTransform painel;
    TextMeshProUGUI texto;
    float proximaAtualizacao;
    const float DistanciaChegada = 3f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Criar()
    {
        if (Instance != null || GameObject.Find("Cenario/Rua") == null) return;   // cena sem o bairro
        Instance = new GameObject("GPS").AddComponent<GPS>();
    }

    void Awake()
    {
        Instance = this;
        var canvas = UIFactory.CriarCanvas("GPS_Canvas", 5);
        canvas.transform.SetParent(transform, false);
        painel = UIFactory.Painel(canvas.transform, "GPS", new Color(0.06f, 0.08f, 0.1f, 0.82f));
        UIFactory.Ancorar(painel, new Vector2(0f, 0f), new Vector2(24f, 24f), new Vector2(600f, 124f));
        texto = UIFactory.Texto(painel, "", 24, UIFactory.CorTexto);
        UIFactory.Esticar(texto.rectTransform, 12);
        texto.raycastTarget = false;
        painel.GetComponent<Image>().raycastTarget = false;
        painel.gameObject.SetActive(false);
        CriarMarcador();
    }

    void OnDestroy() { if (Instance == this) Instance = null; }

    // Coluna de luz fina e alta sobre a porta do destino (sem colisão, sem sombra)
    void CriarMarcador()
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = "GPS_Marcador";
        Destroy(go.GetComponent<Collider>());
        go.transform.SetParent(transform, false);
        go.transform.localScale = new Vector3(0.35f, 15f, 0.35f);
        var r = go.GetComponent<Renderer>();
        var shader = Shader.Find("Sprites/Default");
        if (shader != null) r.sharedMaterial = new Material(shader) { color = new Color(0.25f, 1f, 0.55f, 0.35f) };
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
        marcador = go.transform;
        go.SetActive(false);
    }

    public void Definir(Endereco e, bool avisar = true)
    {
        Destino = e;
        Ativo = true;
        marcador.position = new Vector3(e.porta.x, 15f, e.porta.z);
        marcador.gameObject.SetActive(true);
        painel.gameObject.SetActive(true);
        proximaAtualizacao = 0f;
        if (avisar)
            GameUI.Instance?.Toast($"GPS: <b>{e.nome}</b> — {e.Linha}", UIFactory.Destaque, 4f);
        Debug.Log($"[GPS] Destino: {e.nome} ({e.Linha}) em {e.porta}");
    }

    public void Desligar()
    {
        Ativo = false;
        if (marcador != null) marcador.gameObject.SetActive(false);
        if (painel != null) painel.gameObject.SetActive(false);
    }

    // Distância e direção do jogador até o destino (para o app Mapa e os testes)
    public float Distancia(Vector3 de) => Vector2.Distance(new Vector2(de.x, de.z), new Vector2(Destino.porta.x, Destino.porta.z));

    // "siga em frente", "vire à direita"... pelo ângulo entre o olhar e o destino
    public static string Direcao(Vector3 olhar, Vector3 de, Vector3 para)
    {
        var a = new Vector2(olhar.x, olhar.z);
        var b = new Vector2(para.x - de.x, para.z - de.z);
        if (a.sqrMagnitude < 0.0001f || b.sqrMagnitude < 0.0001f) return "você está aqui";
        float ang = Vector2.SignedAngle(a, b);   // positivo = à esquerda
        float abs = Mathf.Abs(ang);
        if (abs < 25f) return "siga em frente";
        if (abs > 150f) return "dê meia-volta";
        string lado = ang > 0 ? "esquerda" : "direita";
        return abs < 70f ? $"siga à {lado}" : $"vire à {lado}";
    }

    void Update()
    {
        if (!Ativo) return;
        if (jogador == null)
        {
            var j = GameObject.FindWithTag("Player");
            if (j == null) return;
            jogador = j.transform;
        }
        float d = Distancia(jogador.position);
        if (d < DistanciaChegada)
        {
            GameUI.Instance?.Toast($"Você chegou: <b>{Destino.nome}</b>", UIFactory.Sucesso, 3f);
            SomManager.Tocar("clique", 0.6f);
            Debug.Log($"[GPS] Chegou em {Destino.nome}.");
            Desligar();
            return;
        }
        if (Time.unscaledTime < proximaAtualizacao) return;
        proximaAtualizacao = Time.unscaledTime + 0.25f;

        var cam = Camera.main != null ? Camera.main.transform : jogador;
        string dir = Direcao(cam.forward, jogador.position, Destino.porta);
        string onde = Bairro.RuaMaisProxima(jogador.position);
        texto.text = $"<color=#3CCB7F><b>GPS</b></color>  {Destino.nome}\n" +
                     $"<size=85%>{Destino.Linha}</size>\n" +
                     $"<b>{d:0} m</b> · {dir}  <color=#9AA6B8><size=80%>(você está na {onde})</size></color>";
    }
}
