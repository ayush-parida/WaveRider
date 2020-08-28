using UnityEngine;
using System.Collections;

public class CameraController : MonoBehaviour
{
    public PlayerController playerController;
    public GameManager gameManager;
    public GameObject player;

    // How long the camera shaking.
    public float shakeDuration = 0.1f;
    // Amplitude of the shake. A larger value shakes the camera harder.
    public float shakeAmount = 0.2f;
    public float decreaseFactor = 0.3f;
    [HideInInspector]
    public Vector3 originalPos;

    private float currentShakeDuration;
    private float currentDistance;
	
    // Update is called once per frame
    void Update()
    {
        if (gameManager.GameState == GameState.Playing)
        {
            transform.position += new Vector3(playerController.speed * Time.deltaTime, 0f, 0f);
        }
    }

    public void ShakeCamera()
    {
        StartCoroutine(Shake());
    }

    IEnumerator Shake()
    {
        originalPos = transform.position;
        currentShakeDuration = shakeDuration;
        while (currentShakeDuration > 0)
        {
            transform.position = originalPos + Random.insideUnitSphere * shakeAmount;
            currentShakeDuration -= Time.deltaTime * decreaseFactor;
            yield return null;
        }
        transform.position = originalPos;
    }
}
