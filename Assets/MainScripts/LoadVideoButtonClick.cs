using UnityEngine;
using Zenject;
using TMPro;
using UnityEngine.Events;
using YG;
using Cysharp.Threading.Tasks.Triggers;

public class LoadVideoButtonClick : MonoBehaviour
{
    [SerializeField] int Price;
    [SerializeField] GameObject Video;
    [SerializeField] GameObject Lock;
    [SerializeField] TMP_Text text;
    public UnityEvent OnPlay;
    public bool isSold;

    [Inject] Scores _scores;
    [Inject] Reward _rew;
    [Inject] VideoManager manager;

    private void OnEnable()
    {
        YandexGame.GetDataEvent += GetLoad;
        text.text = Price.ToString();
    }

    private void OnDisable()
    {
        YandexGame.GetDataEvent -= GetLoad;
    }


    public void GetLoad()
    {
        // Загружаем состояние видео из VideoManager
        manager.InitializeVideoState(this, gameObject.name);

        // Обновляем состояние замка только если видео действительно куплено
        Lock.SetActive(!isSold);
    }

    private void Update()
    {
        GetLoad();
    }

    private void Start()
    {
        if (YandexGame.SDKEnabled)
        {
            GetLoad();
        }

        // Передаем общее количество видео в VideoManager (можно установить вручную или подсчитать в сцене).
        manager.InitializeVideoManager(FindObjectsOfType<LoadVideoButtonClick>().Length);
    }

    public void OpenVideo()
    {
        if (_scores.GetScores() >= Price || isSold)
        {
            if(!isSold)
                _scores.AddScores(-Price);

            Video.SetActive(true);
            isSold = true;
            Lock.SetActive(false);
            OnPlay?.Invoke();
            manager.SaveVideoState(gameObject.name, isSold);
            YandexGame.savesData.money = _scores.GetScores();
            YandexGame.SaveProgress();

            GetLoad();
        }
        else
        {
            _rew.gameObject.SetActive(true);
            _rew.SetCurrentVideoId(gameObject.name, this); // Передаем идентификатор видео в Reward
        }
    }
}
