using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float velocidade = 5f;
    [Tooltip("Multiplica a velocidade segurando Shift")]
    public float multiplicadorCorrida = 1.6f;
    public float sensibilidadeMouse = 2f;
    public float gravidade = -20f;

    private CharacterController controller;
    private Camera cam;
    private float rotacaoVertical = 0f;
    private float velocidadeVertical = 0f;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        cam = GetComponentInChildren<Camera>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // Parado enquanto o tablet, o diagnóstico ou a pausa estiverem abertos
        if (GameUI.Instance != null && GameUI.Instance.PainelAberto) return;

        MoverJogador();
        RotacionarCamera();
    }

    void MoverJogador()
    {
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        Vector3 movimento = transform.right * x + transform.forward * z;
        if (movimento.sqrMagnitude > 1f) movimento.Normalize();
        float vel = Input.GetKey(KeyCode.LeftShift) ? velocidade * multiplicadorCorrida : velocidade;

        // Gravidade: mantém o jogador no chão (degraus, calçadas, rampas)
        if (controller.isGrounded && velocidadeVertical < 0f) velocidadeVertical = -2f;
        velocidadeVertical += gravidade * Time.deltaTime;

        controller.Move((movimento * vel + Vector3.up * velocidadeVertical) * Time.deltaTime);

        // Passos: um som a cada ~0,6 m andando no chão (correndo, passos mais longos)
        if (controller.isGrounded && movimento.sqrMagnitude > 0.01f)
        {
            distanciaPasso += movimento.magnitude * vel * Time.deltaTime;
            float passo = vel > velocidade ? 0.8f : 0.6f;
            if (distanciaPasso >= passo)
            {
                distanciaPasso = 0f;
                SomManager.TocarEm("passo", transform.position + Vector3.down * 0.8f, 0.35f, Random.Range(0.85f, 1.15f));
            }
        }
    }

    float distanciaPasso;

    // Vira o corpo e a câmera para olhar um ponto (usado pelos atalhos de teste e cenas)
    public void OlharPara(Vector3 ponto)
    {
        if (cam == null) cam = GetComponentInChildren<Camera>();
        Vector3 d = ponto - cam.transform.position;
        Vector3 plano = new Vector3(d.x, 0f, d.z);
        if (plano.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.LookRotation(plano);
        rotacaoVertical = Mathf.Clamp(-Mathf.Atan2(d.y, plano.magnitude) * Mathf.Rad2Deg, -80f, 80f);
        cam.transform.localRotation = Quaternion.Euler(rotacaoVertical, 0f, 0f);
    }

    void RotacionarCamera()
    {
        // Sensibilidade e eixo Y vêm das configurações do jogador
        float sens = Configuracoes.Sensibilidade;
        float mouseX = Input.GetAxis("Mouse X") * sens;
        float mouseY = Input.GetAxis("Mouse Y") * sens * (Configuracoes.InverterY ? -1f : 1f);

        rotacaoVertical -= mouseY;
        rotacaoVertical = Mathf.Clamp(rotacaoVertical, -80f, 80f);

        cam.transform.localRotation = Quaternion.Euler(rotacaoVertical, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);
    }
}