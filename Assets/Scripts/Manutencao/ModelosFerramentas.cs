using UnityEngine;

// Modelos 3D das ferramentas de diagnóstico que aparecem na mão do técnico (Sketchfab, CC-BY: ver CREDITOS.md).
// Criado pelo menu "TI Service/Ferramentas: ligar modelos 3D" em Resources/Ferramentas/ModelosFerramentas.
// Sem ele (ou sem um modelo), a ferramenta é montada com primitivas (EquipamentoPecas.Ferramentas.cs).
public class ModelosFerramentas : ScriptableObject
{
    public GameObject multimetro;
    public GameObject pendrive;

    static ModelosFerramentas carregado;
    static bool tentou;

    public static ModelosFerramentas Carregar()
    {
        if (!tentou) { carregado = Resources.Load<ModelosFerramentas>("Ferramentas/ModelosFerramentas"); tentou = true; }
        return carregado;
    }

    // Instancia o modelo dentro de 'pai', na escala real: o maior lado vira 'comprimento' metros e o centro fica na origem.
    // 'giro' leva o modelo para a posição da mão (frente para a câmera = -Z do pai).
    public static Transform Instanciar(GameObject modelo, Transform pai, float comprimento, Quaternion giro)
    {
        if (modelo == null) return null;
        var go = Object.Instantiate(modelo, pai, false);
        go.name = modelo.name;
        foreach (var c in go.GetComponentsInChildren<Collider>(true)) Object.Destroy(c);
        var t = go.transform;
        t.localRotation = giro;
        t.localPosition = Vector3.zero;
        t.localScale = Vector3.one;
        var b = Limites(t, pai);
        float maior = Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z));
        if (maior > 0f) t.localScale = Vector3.one * (comprimento / maior);
        b = Limites(t, pai);
        t.localPosition -= b.center;
        return t;
    }

    // Limites dos renderers no espaço local de 'pai'
    public static Bounds Limites(Transform t, Transform pai)
    {
        bool tem = false;
        var b = new Bounds();
        foreach (var r in t.GetComponentsInChildren<Renderer>(true))
        {
            var mf = r.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null) continue;
            var mb = mf.sharedMesh.bounds;
            for (int i = 0; i < 8; i++)
            {
                var canto = mb.center + Vector3.Scale(mb.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                var p = pai.InverseTransformPoint(r.transform.TransformPoint(canto));
                if (!tem) { b = new Bounds(p, Vector3.zero); tem = true; } else b.Encapsulate(p);
            }
        }
        return b;
    }
}
