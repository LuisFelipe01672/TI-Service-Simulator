using TMPro;
using UnityEngine;

// Etapa 6: monitor de bancada (ligado no equipamento que está sendo testado). Mostra a pista do teste
// ("sem sinal", a área de trabalho do antigo dono com os arquivos dele) e a tela de progresso do apagamento
// seguro dos dados (LGPD). Frente em -Z, base em y = 0.
public class TelaBancada : MonoBehaviour
{
    public enum Modo { Desligada, SemSinal, Desktop, Progresso, Texto }

    const float L = 0.4f, A = 0.24f;
    Renderer fundo;
    Transform icones, barraFundo, barra;
    TextMeshPro texto;
    static Material mPreto, mDesligada, mAzul, mEscura, mBarra, mBarraFundo;
    static readonly Color[] CoresIcones = { new Color(0.2f, 0.65f, 0.3f), new Color(0.25f, 0.45f, 0.9f), new Color(0.95f, 0.75f, 0.2f), new Color(0.85f, 0.3f, 0.3f) };
    Material[] mIcones;
    float progIni, progDur;
    string progTitulo;
    Modo modo;

    public static TelaBancada Criar(Transform pai, Vector3 posLocal, Quaternion rotLocal)
    {
        var go = new GameObject("MonitorBancada");
        go.transform.SetParent(pai, false);
        go.transform.localPosition = posLocal;
        go.transform.localRotation = rotLocal;
        var t = go.AddComponent<TelaBancada>();
        t.Montar();
        return t;
    }

    static Material Mat(Color cor, bool emite)
    {
        var m = new Material(Shader.Find("Standard")) { color = cor };
        m.SetFloat("_Glossiness", emite ? 0.7f : 0.4f);
        if (emite) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", cor * 0.9f); }
        return m;
    }

    GameObject Cubo(Transform pai, string nome, Vector3 pos, Vector3 tam, Material mat, PrimitiveType tipo = PrimitiveType.Cube)
    {
        var c = GameObject.CreatePrimitive(tipo);
        c.name = nome;
        Destroy(c.GetComponent<Collider>());
        c.transform.SetParent(pai, false);
        c.transform.localPosition = pos;
        c.transform.localScale = tam;
        c.GetComponent<Renderer>().sharedMaterial = mat;
        return c;
    }

    void Montar()
    {
        if (mPreto == null)
        {
            mPreto = Mat(new Color(0.05f, 0.05f, 0.055f), false);
            mDesligada = Mat(new Color(0.015f, 0.017f, 0.02f), false);
            mAzul = Mat(new Color(0.08f, 0.32f, 0.62f), true);
            mEscura = Mat(new Color(0.05f, 0.06f, 0.08f), true);
            mBarra = Mat(new Color(0.25f, 0.85f, 0.45f), true);
            mBarraFundo = Mat(new Color(0.18f, 0.2f, 0.24f), true);
        }
        mIcones = new Material[CoresIcones.Length];
        for (int i = 0; i < CoresIcones.Length; i++) mIcones[i] = Mat(CoresIcones[i], true);

        var corpo = transform;
        var b = Cubo(corpo, "Base", new Vector3(0f, 0.004f, 0.02f), new Vector3(0.18f, 0.004f, 0.13f), mPreto, PrimitiveType.Cylinder);
        b.transform.localScale = new Vector3(0.18f, 0.004f, 0.13f);
        Cubo(corpo, "Haste", new Vector3(0f, 0.08f, 0.03f), new Vector3(0.04f, 0.15f, 0.02f), mPreto);
        Cubo(corpo, "Painel", new Vector3(0f, 0.25f, 0f), new Vector3(L + 0.025f, A + 0.03f, 0.03f), mPreto);
        fundo = Cubo(corpo, "Tela", new Vector3(0f, 0.255f, -0.0155f), new Vector3(L, A, 0.001f), mDesligada).GetComponent<Renderer>();

        icones = new GameObject("Icones").transform;
        icones.SetParent(corpo, false);
        icones.localPosition = new Vector3(-L / 2 + 0.03f, 0.255f + A / 2 - 0.035f, -0.0168f);
        for (int i = 0; i < 4; i++)
            Cubo(icones, "Icone" + i, new Vector3(0f, -i * 0.045f, 0f), new Vector3(0.022f, 0.022f, 0.001f), mIcones[i]);

        barraFundo = Cubo(corpo, "BarraFundo", new Vector3(0f, 0.2f, -0.0168f), new Vector3(L * 0.8f, 0.018f, 0.001f), mBarraFundo).transform;
        barra = Cubo(corpo, "Barra", new Vector3(-L * 0.4f, 0.2f, -0.0172f), new Vector3(0.001f, 0.014f, 0.001f), mBarra).transform;

        var go = new GameObject("Texto", typeof(RectTransform));
        go.transform.SetParent(corpo, false);
        go.transform.localPosition = new Vector3(0f, 0.255f, -0.0175f);
        texto = go.AddComponent<TextMeshPro>();
        texto.rectTransform.sizeDelta = new Vector2(L - 0.03f, A - 0.03f);
        texto.fontSize = 0.22f;
        texto.enableAutoSizing = true;
        texto.fontSizeMin = 0.08f;
        texto.fontSizeMax = 0.22f;
        texto.alignment = TextAlignmentOptions.Center;
        texto.color = Color.white;
        Mostrar(Modo.Desligada);
    }

    public void Mostrar(Modo m, string msg = "")
    {
        modo = m;
        fundo.sharedMaterial = m == Modo.Desligada ? mDesligada : m == Modo.Desktop ? mAzul : mEscura;
        icones.gameObject.SetActive(false);
        barra.gameObject.SetActive(m == Modo.Progresso);
        barraFundo.gameObject.SetActive(m == Modo.Progresso);
        texto.alignment = TextAlignmentOptions.Center;
        texto.text = m == Modo.SemSinal ? "<color=#9AA6B8>SEM SINAL</color>\n<size=55%>Verifique o cabo de vídeo</size>" : m == Modo.Texto ? msg : "";
    }

    // Área de trabalho: com dados do antigo dono (arquivos na tela) ou limpa (sistema reinstalado)
    public void Desktop(bool comDados)
    {
        Mostrar(Modo.Desktop);
        icones.gameObject.SetActive(comDados);
        texto.alignment = TextAlignmentOptions.TopLeft;
        texto.text = comDados
            ? "<size=60%><indent=10%>Folha de pagamento 2025.xlsx\n\n<indent=10%>Clientes com CPF.xlsx\n\n<indent=10%>Fotos da festa\n\n<indent=10%>Senhas do banco.txt</size>"
            : "<size=60%>\n\n\n<align=center>Sistema reinstalado — sem arquivos do antigo dono</align></size>";
    }

    public void Progresso(string titulo, float segundos)
    {
        Mostrar(Modo.Progresso);
        progTitulo = titulo;
        progIni = Time.time;
        progDur = Mathf.Max(0.5f, segundos);
    }

    void Update()
    {
        if (modo != Modo.Progresso) return;
        float f = Mathf.Clamp01((Time.time - progIni) / progDur);
        float w = L * 0.8f * f;
        barra.localScale = new Vector3(Mathf.Max(0.001f, w), 0.014f, 0.001f);
        barra.localPosition = new Vector3(-L * 0.4f + w / 2f, 0.2f, -0.0172f);
        texto.text = f < 1f ? $"{progTitulo}\n<size=80%>{f * 100f:0}%</size>\n\n"
                            : $"{progTitulo}\n<color=#3CCB7F>Concluído: nada recuperável</color>\n\n";
        if (f >= 1f && modo == Modo.Progresso && progDur > 0f)
        {
            progDur = 0f;
            SomManager.TocarEm("sucesso", transform.position, 0.4f);
            modo = Modo.Texto;
        }
    }
}
