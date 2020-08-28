using UnityEngine;
using System.Collections;
using SgLib;

public class PlayerController : MonoBehaviour
{
    public static event System.Action PlayerDied;

    [Header("Gameplay References")]
    public GameManager gameManager;
    public CameraController cameraController;
    [Header("Gameplay Config")]
    public float speed = 10;
    public float speedFactor = 40;
    public float speedLimited = 25;
    public TrailRenderer trailRenderer;

    private Vector3 upVector;
    private Vector3 rightVector;
    private Vector3 downVector;
    private Vector3 totalVector;
    private float upSpeed;
    private bool hittedScore;
    private bool hittedScoreCenter;

    // Use this for initialization
    void Start()
    {
        //Change the character to the selected one
        GameObject currentCharacter = CharacterManager.Instance.characters[CharacterManager.Instance.CurrentCharacterIndex];
        Mesh charMesh = currentCharacter.GetComponent<MeshFilter>().sharedMesh;
        Material charMaterial = currentCharacter.GetComponent<Renderer>().sharedMaterial;
        GetComponent<MeshFilter>().mesh = charMesh;
        GetComponent<MeshRenderer>().material = charMaterial;

        // Set trail color
        trailRenderer.material.SetColor("_TintColor", GetComponent<Renderer>().material.color);

        upSpeed = 0;
    }
    // Update is called once per frame
    void Update()
    {
        if (gameManager.GameState == GameState.Playing) //Start moving
        {
            if (Input.GetMouseButton(0)) //Increase upspeed to make player flying up
            {
                if (upSpeed < speedLimited)
                {
                    upSpeed += speedFactor * Time.deltaTime;
                }
                else
                {
                    upSpeed = speedLimited;
                }
            }
            else
            {
                if (upSpeed > 0) //Decrease upspeed to make player falling down
                {
                    upSpeed -= speedFactor * Time.deltaTime;
                }
                else
                {
                    upSpeed = 0;
                }
            }

            upVector = Vector3.up * upSpeed;
            rightVector = Vector3.right * speed;
            downVector = Vector3.down * speed;

            totalVector = upVector + downVector + rightVector; //Moving direction

            transform.position += totalVector * Time.deltaTime;

            //Caculate rotate angle and rotated the player
            float rotateAngle = Vector3.Angle(Vector3.right, totalVector); 
            if (totalVector.y < 0)
            {
                rotateAngle = -rotateAngle;
            }
            transform.rotation = Quaternion.Euler(0, 0, rotateAngle);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Gold")) //Hit gold
        {
            CoinManager.Instance.AddCoins(1);
            SoundManager.Instance.PlaySound(SoundManager.Instance.coin);

            ParticleSystem particle = Instantiate(gameManager.hitGold, other.transform.position, Quaternion.identity) as ParticleSystem;
            particle.Play();
            Destroy(particle.gameObject, 1f);
            Destroy(other.gameObject);
        }
        else if (other.CompareTag("Score") && !hittedScoreCenter) //Hit score
        {
            hittedScore = true;
            ScoreManager.Instance.AddScore(1);
            SoundManager.Instance.PlaySound(SoundManager.Instance.score);
            gameManager.combo = 0;

            other.GetComponent<Animator>().Play(gameManager.scoreCircleScaleSmaller.name);
            Destroy(other.gameObject, gameManager.scoreCircleScaleSmaller.length);
        }
        else if (other.CompareTag("ScoreCenter") && !hittedScore)
        {
            GameObject theParent = other.transform.parent.gameObject;

            hittedScoreCenter = true;
            gameManager.combo++;
            ScoreManager.Instance.AddScore(1 + gameManager.combo);
            SoundManager.Instance.PlaySound(SoundManager.Instance.bigScore);

            GameObject scoreParticle = Instantiate(gameManager.scoreParticle, theParent.transform.position, Quaternion.Euler(0, 90f, 0)) as GameObject;
            theParent.GetComponent<Animator>().Play(gameManager.scoreCircleScaleSmaller.name);
            float destroyTime = (gameManager.scoreCircleScaleSmaller.length + (gameManager.combo / 10f) >= gameManager.scoreParticleScale.length) ? gameManager.scoreParticleScale.length : (gameManager.scoreCircleScaleSmaller.length + (gameManager.combo / 10f));
            Destroy(scoreParticle, destroyTime);
            Destroy(theParent, gameManager.scoreCircleScaleSmaller.length);
        }
        else if (other.CompareTag("Obstacle"))//Hit obstacle 
        {
            Die();
            StartCoroutine(DelayDestroy());
        }
    }
    
    IEnumerator DelayDestroy()
    {
        yield return new WaitForSeconds(0.1f);

        ParticleSystem particle = Instantiate(gameManager.playerDie, transform.position, Quaternion.identity) as ParticleSystem;
        var main = particle.main;
        main.startColor = GetComponent<Renderer>().material.color;
        particle.Play();
        Destroy(particle.gameObject, 2f);
        gameObject.SetActive(false);

        cameraController.originalPos = cameraController.gameObject.transform.position;
        cameraController.ShakeCamera();

        SoundManager.Instance.PlaySound(SoundManager.Instance.gameOver);
    }
    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Score") || other.CompareTag("ScoreCenter"))
        {
            hittedScore = false;
            hittedScoreCenter = false;
        }
    }

    void Die()
    {
        if (PlayerDied != null)
        {
            // Fire event
            PlayerDied();
        }
    }
}
