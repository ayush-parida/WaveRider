using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using SgLib;

public enum GameState
{
    Prepare,
    Playing,
    Paused,
    PreGameOver,
    GameOver
}


public class GameManager : MonoBehaviour
{
    public static event System.Action<GameState, GameState> GameStateChanged = delegate { };

    public GameState GameState
    {
        get
        {
            return _gameState;
        }
        private set
        {
            if (value != _gameState)
            {
                GameState oldState = _gameState;
                _gameState = value;

                GameStateChanged(_gameState, oldState);
            }
        }
    }

    private GameState _gameState = GameState.Prepare;

    public static int GameCount
    { 
        get { return _gameCount; } 
        private set { _gameCount = value; } 
    }

    private static int _gameCount = 0;

    [Header("Gameplay Preferences")]
    public UIManager uIManager;
    public GameObject player;
    public GameObject firstPath;
    public GameObject scoreCircle;
    public GameObject gold;
    public GameObject scoreParticle;
    public ParticleSystem playerDie;
    public ParticleSystem hitGold;
    public AnimationClip scoreCircleScaleSmaller;
    public AnimationClip scoreParticleScale;
    public GameObject[] pathObjects;
    [HideInInspector]
    public int combo;

    [Header("Gameplay Config")]
    public int initialPath;
    //How many path is created when the game started
    public int totalPathOnScene;
    //Max path number when the game run
    [Range(0f, 1f)]
    public float scoreCircleFrequency;
    //Probability to create score circle
    [Range(0f, 1f)]
    public float goldFrequency;
    //Probability to create gold
    public Color[] pathColors;

    private List<GameObject> listPath = new List<GameObject>();
    private Vector3 generatePathPosition;
    private Color color;
    private float xSize;
    private int pathIndex = 0;
    private int colorIndex;

    void OnEnable()
    {
        PlayerController.PlayerDied += PlayerController_PlayerDied;
    }

    void OnDisable()
    {
        PlayerController.PlayerDied -= PlayerController_PlayerDied;
    }

    void Start()
    {
        ScoreManager.Instance.Reset();
       
        //Create path position
        xSize = firstPath.transform.GetChild(0).GetComponent<Renderer>().bounds.size.x;
        generatePathPosition = firstPath.transform.position + Vector3.right * xSize;
        firstPath.transform.parent = transform;
        listPath.Add(firstPath);
        colorIndex = Random.Range(0, pathColors.Length);
        color = CurrentColor();

        //Generate some paths when the game started
        for (int i = 0; i < initialPath; i++)
        {
            GeneratePath();
        }

        StartCoroutine(CreateAndDestroyPath());
        SetColor(firstPath, color);

        // Play background music
        SoundManager.Instance.PlayMusic(SoundManager.Instance.background);
    }

    void PlayerController_PlayerDied()
    {
        SoundManager.Instance.StopMusic();
        GameOver();
    }

    public void StartGame()
    {
        GameState = GameState.Playing;
    }

    public void GameOver()
    {
        GameState = GameState.GameOver;

        // Stop background music

    }

    public void RestartGame(float delay = 0)
    {
        StartCoroutine(CRRestartGame(delay));
    }

    IEnumerator CRRestartGame(float delay = 0)
    {
        yield return new WaitForSeconds(delay);
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    IEnumerator CreateAndDestroyPath()
    {
        while (GameState != GameState.GameOver)
        {
            if (transform.childCount < totalPathOnScene)
            {
                GeneratePath();
            }
            CheckAndDestroyPath();
            CheckAndResetCombo();
            yield return new WaitForSeconds(0.5f);
        }
    }

    //Random one path from path array
    GameObject GetRandomPath()
    {
        return pathObjects[Random.Range(0, pathObjects.Length)];
    }

    void GeneratePath()
    {
        //Create path
        GameObject thePath = Instantiate(GetRandomPath(), generatePathPosition, Quaternion.identity) as GameObject;
        thePath.transform.parent = transform;
        listPath.Add(thePath);
        generatePathPosition = thePath.transform.position + Vector3.right * xSize;
        color = CurrentColor();
        SetColor(thePath, color);
        //Find all child has tag "Point" of the path and create score torus and gold
        for (int i = 0; i < thePath.transform.childCount; i++)
        {
            GameObject childObject = thePath.transform.GetChild(i).gameObject;
            if (childObject.CompareTag("Point")) //This is the point to create score circle and gold
            {
                GameObject point = thePath.transform.GetChild(i).gameObject;
        
                if (Random.value <= 0.5f) //Create score circle
                {
                    if (Random.value <= scoreCircleFrequency)
                    {
                        GameObject score = Instantiate(scoreCircle, point.transform.position, Quaternion.Euler(0, 90, 0)) as GameObject;
                        score.transform.parent = thePath.transform;      
                    }
                }
                else //Create gold
                {
                    if (Random.value <= scoreCircleFrequency)
                    {
                        GameObject goldObject = Instantiate(gold, point.transform.position, Quaternion.Euler(0, 90, 0)) as GameObject;
                        goldObject.transform.parent = point.transform;
                    }
                }
            }
        }
             
    }

    //Check if the path lagged behind then destroy it
    void CheckAndDestroyPath()
    {
        if (listPath[pathIndex].transform.position.x < Camera.main.transform.position.x - 20f)
        {
            Destroy(listPath[pathIndex]);
            pathIndex++;
        }
    }

    //Check and reset combo
    void CheckAndResetCombo()
    {
        GameObject[] scores = GameObject.FindGameObjectsWithTag("Score");

        foreach (GameObject score in scores)
        {
            if (score.transform.position.x < player.transform.position.x - 1 && score.transform.localScale.x > 1)
            {
                combo = 0;
                break;
            }
        }
    }

    // Path color
    Color CurrentColor()
    {
        return pathColors[colorIndex];
    }

    //Set color for the path
    void SetColor(GameObject thePath, Color color)
    {
        for (int i = 0; i < thePath.transform.childCount; i++)
        {
            GameObject childOb = thePath.transform.GetChild(i).gameObject;

            if (childOb.GetComponent<Renderer>() != null)
            {
                Renderer renderer = childOb.GetComponent<Renderer>();
                Material[] materials = renderer.materials;
                foreach (Material mat in materials)
                {
                    if (mat.name.Split(' ')[0].Equals("MainMaterial"))
                    {
                        mat.SetColor("_Color", color);
                    }
                }
            }
        }
    }
}
