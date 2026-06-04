using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float velocidade = 5f;
    public float sensibilidadeMouse = 2f;

    private CharacterController controller;
    private Camera cam;
    private float rotacaoVertical = 0f;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        cam = GetComponentInChildren<Camera>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        MoverJogador();
        RotacionarCamera();
    }

    void MoverJogador()
    {
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        Vector3 movimento = transform.right * x + transform.forward * z;
        controller.Move(movimento * velocidade * Time.deltaTime);
    }

    void RotacionarCamera()
    {
        float mouseX = Input.GetAxis("Mouse X") * sensibilidadeMouse;
        float mouseY = Input.GetAxis("Mouse Y") * sensibilidadeMouse;

        rotacaoVertical -= mouseY;
        rotacaoVertical = Mathf.Clamp(rotacaoVertical, -80f, 80f);

        cam.transform.localRotation = Quaternion.Euler(rotacaoVertical, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);
    }
}