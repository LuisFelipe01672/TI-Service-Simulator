using UnityEngine;

// Etapa 6: modelos 3D reaproveitados do projeto (Sketchfab, CC-BY: ver CREDITOS.md) para os equipamentos usados
// do lote. Criado pelo menu "TI Service/Usados: ligar modelos 3D" em Resources/Usados/ModelosUsados.
// Sem ele (ou sem um modelo), o equipamento é montado com primitivas (UsadoVisual.cs).
public class ModelosUsados : ScriptableObject
{
    public GameObject impressora, roteador, switchRede;
    public GameObject notebook;   // Etapa 7: notebook do cliente (EquipamentoPecas.Notebook)

    static ModelosUsados carregado;
    static bool tentou;

    public static ModelosUsados Carregar()
    {
        if (!tentou) { carregado = Resources.Load<ModelosUsados>("Usados/ModelosUsados"); tentou = true; }
        return carregado;
    }
}
