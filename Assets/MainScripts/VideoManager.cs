using TMPro;
using UnityEngine;
using YG;
using System.Linq;

public class VideoManager : MonoBehaviour
{
    public static VideoManager Instance { get; private set; }

    public int TotalVideos { get; private set; }
    public int OpenedVideos { get; private set; }

    [SerializeField] TextMeshProUGUI Total;
    [SerializeField] TextMeshProUGUI Opened;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
          
        }
        else
        {
            Destroy(gameObject);
        }
    
        Total.text = TotalVideos.ToString();
     
    }

    public void InitializeVideoManager(int totalVideos)
    {
        TotalVideos = totalVideos;

        // Обновляем количество открытых видео
        UpdateOpenedVideosCount();

        Total.text = TotalVideos.ToString();
    }

    public void SaveVideoState(string videoId, bool isSold)
    {
        for (int i = 0; i < YandexGame.savesData.VideoName.Length; i++)
        {
            if (YandexGame.savesData.VideoName[i] == videoId)
            {
                // Если видео найдено, обновляем состояние
                YandexGame.savesData.VideoBool[i] = isSold;
                UpdateOpenedVideosCount();
                YandexGame.SaveProgress();
                return;
            }
            else if (string.IsNullOrEmpty(YandexGame.savesData.VideoName[i]))
            {
                // Если найден первый пустой слот, добавляем новое видео
                YandexGame.savesData.VideoName[i] = videoId;
                YandexGame.savesData.VideoBool[i] = isSold;
                UpdateOpenedVideosCount();
                YandexGame.SaveProgress();
                return;
            }
        }

        Debug.LogWarning("Нет места для сохранения нового видео!");
    }

    public bool LoadVideoState(string videoId)
    {
        for (int i = 0; i < YandexGame.savesData.VideoName.Length; i++)
        {
            if (YandexGame.savesData.VideoName[i] == videoId)
            {
                return YandexGame.savesData.VideoBool[i];
            }
        }

        return false; // Если идентификатор не найден, возвращаем "не продано"
    }


    public void InitializeVideoState(LoadVideoButtonClick videoButton, string videoId)
    {
        bool isSold = LoadVideoState(videoId); // Проверяем сохранённое состояние
        videoButton.isSold = isSold;          // Применяем его к кнопке
        //videoButton.UpdateLockState();        // Обновляем визуальное состояние
    }

    private void UpdateOpenedVideosCount()
    {
        OpenedVideos = 0;

        for (int i = 0; i < YandexGame.savesData.VideoBool.Length; i++)
        {
            if (YandexGame.savesData.VideoBool[i])
            {
                OpenedVideos++;
            }
        }

        Opened.text = OpenedVideos.ToString();
    }
}
