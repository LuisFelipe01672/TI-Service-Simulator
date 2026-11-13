using UnityEngine;

// Lista dos personagens 3D (Mixamo) e do controlador de animação usados pelos clientes e funcionários.
// É criado pelo menu "TI Service/5. Configurar personagens" em Resources/Personagens/BancoPersonagens.
public class BancoPersonagens : ScriptableObject
{
    [Tooltip("Modelos com esqueleto humanoide (o primeiro é o de roupa mais formal, usado pelo VIP)")]
    public GameObject[] modelos;
    public RuntimeAnimatorController controlador;

    // Nomes dos estados no controlador
    public const string Parado = "Parado";
    public const string Andando = "Andando";
    public const string Digitando = "Digitando";
    public const string ConversandoSentado = "ConversandoSentado";
    public const string ConversandoEmPe = "ConversandoEmPe";

    static BancoPersonagens carregado;
    static bool tentou;

    public static BancoPersonagens Carregar()
    {
        if (!tentou) { carregado = Resources.Load<BancoPersonagens>("Personagens/BancoPersonagens"); tentou = true; }
        return carregado != null && carregado.modelos != null && carregado.modelos.Length > 0 && carregado.controlador != null ? carregado : null;
    }

    // Cada FBX do Mixamo pode vir numa escala diferente (o Remy veio enorme):
    // mede a altura do modelo e ajusta para 'altura' metros, com os pés no chão
    static void NormalizarAltura(Transform t, float altura)
    {
        var rs = t.GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) return;
        var b = rs[0].bounds;
        for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
        if (b.size.y < 0.01f) return;
        t.localScale *= altura / b.size.y;
        // pés no chão do 'pai'
        b = rs[0].bounds;
        for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
        float pe = t.parent != null ? t.parent.position.y : 0f;
        t.position += Vector3.up * (pe - b.min.y);
    }

    // Cria um personagem animado como filho de 'pai' (ou null se não houver modelos configurados)
    public static Animator Instanciar(Transform pai, int indice)
    {
        var banco = Carregar();
        if (banco == null) return null;
        var modelo = banco.modelos[Mathf.Abs(indice) % banco.modelos.Length];
        if (modelo == null) return null;
        var go = Instantiate(modelo, pai, false);
        go.name = "Personagem";
        NormalizarAltura(go.transform, 1.75f);
        var anim = go.GetComponent<Animator>();
        if (anim == null) anim = go.AddComponent<Animator>();
        anim.runtimeAnimatorController = banco.controlador;
        anim.applyRootMotion = false;
        anim.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
        return anim;
    }
}
