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

    // Cria um personagem animado como filho de 'pai' (ou null se não houver modelos configurados)
    public static Animator Instanciar(Transform pai, int indice)
    {
        var banco = Carregar();
        if (banco == null) return null;
        var modelo = banco.modelos[Mathf.Abs(indice) % banco.modelos.Length];
        if (modelo == null) return null;
        var go = Instantiate(modelo, pai, false);
        go.name = "Personagem";
        var anim = go.GetComponent<Animator>();
        if (anim == null) anim = go.AddComponent<Animator>();
        anim.runtimeAnimatorController = banco.controlador;
        anim.applyRootMotion = false;
        anim.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
        return anim;
    }
}
