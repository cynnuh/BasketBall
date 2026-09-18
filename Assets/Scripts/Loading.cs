using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class Loading : MonoBehaviour
{
    [SerializeField]
    private Slider slider;

    [SerializeField]
    private float waitSeconds = 1f;

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (waitSeconds > 0f)
        {
            waitSeconds -= Time.deltaTime;
        }
        else
        {
            StartCoroutine(LoadNewScene());
        }
    }

    private IEnumerator LoadNewScene()
    {
       AsyncOperation oper = SceneManager.LoadSceneAsync("BasketBall");
       while(!oper.isDone)
        {
            slider.value = oper.progress / 0.9f;
            yield return null;
        }
    }
}
