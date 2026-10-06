using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;

public class BuildExpiryManager : MonoBehaviour
{
    public enum ExpiryDuration
    {
        FiveSeconds,
        OneDay,
        SevenDays,
        TwentyOneDays
    }

    [Serializable]
    public class StartDateTime
    {
        [Header("Date")]
        [Range(2020, 2100)]
        public int year = 2026;

        [Range(1, 12)]
        public int month = 7;

        [Range(1, 31)]
        public int day = 23;

        [Header("Time - IST")]
        [Range(0, 23)]
        public int hour = 0;

        [Range(0, 59)]
        public int minute = 0;

        [Range(0, 59)]
        public int second = 0;
    }


    [Header("Build Expiry Configuration")]

    [Tooltip("All dates and times are treated as Indian Standard Time (IST).")]
    public StartDateTime startDateTime;


    [Tooltip("Select the validity duration of the build.")]
    public ExpiryDuration expiryDuration =
        ExpiryDuration.SevenDays;


    [Header("Online Time Verification")]

    [Tooltip("Replace this with your own secure HTTPS API in production.")]
    public string dateApiUrl =
        "https://worldtimeapi.org/api/timezone/Etc/UTC";


    [Tooltip("The build will verify the current time after this interval.")]
    public float checkInterval = 30f;


    [Header("Expiry UI")]

    public GameObject expiredPanel;

    public TMP_Text expiryMessage;


    [Header("Expiry Message")]

    [TextArea(4, 8)]
    public string expiredMessage =
        "Demo Access Expired\n\n" +
        "This demonstration build is no longer available for use.\n\n" +
        "Please contact the project administrator for an updated build " +
        "or to request continued access.";


    private DateTime expiryDateIST;

    private bool isExpired = false;


    private void Awake()
    {
        // Hide expiry panel initially
        if (expiredPanel != null)
        {
            expiredPanel.SetActive(false);
        }


        // Calculate expiry date
        CalculateExpiryDate();


        // Check system time immediately
        CheckSystemDate();


        // Check online time
        StartCoroutine(CheckOnlineTime());


        // Start periodic checking
        StartCoroutine(PeriodicExpiryCheck());
    }


    // =====================================================
    // CALCULATE EXPIRY DATE
    // =====================================================

    private void CalculateExpiryDate()
    {
        try
        {
            // Create IST start date
            DateTime startDateIST =
                new DateTime(
                    startDateTime.year,
                    startDateTime.month,
                    startDateTime.day,
                    startDateTime.hour,
                    startDateTime.minute,
                    startDateTime.second,
                    DateTimeKind.Unspecified
                );


            // Calculate expiry
            switch (expiryDuration)
            {
                case ExpiryDuration.FiveSeconds:

                    expiryDateIST =
                        startDateIST.AddSeconds(5);

                    break;


                case ExpiryDuration.OneDay:

                    expiryDateIST =
                        startDateIST.AddDays(1);

                    break;


                case ExpiryDuration.SevenDays:

                    expiryDateIST =
                        startDateIST.AddDays(7);

                    break;


                case ExpiryDuration.TwentyOneDays:

                    expiryDateIST =
                        startDateIST.AddDays(21);

                    break;
            }


            Debug.Log(
                "Build Start Date & Time (IST): " +
                startDateIST
            );


            Debug.Log(
                "Build Expiry Date & Time (IST): " +
                expiryDateIST
            );
        }
        catch (Exception exception)
        {
            Debug.LogError(
                "Invalid Start Date/Time: " +
                exception.Message
            );
        }
    }


    // =====================================================
    // GET SYSTEM IST TIME
    // =====================================================

    private DateTime GetSystemISTTime()
    {
        // Get system time
        DateTime localTime =
            DateTime.Now;


        // Convert system time to UTC
        DateTime utcTime =
            localTime.ToUniversalTime();


        // Convert UTC to IST
        DateTime istTime =
            utcTime
            .AddHours(5)
            .AddMinutes(30);


        return istTime;
    }


    // =====================================================
    // SYSTEM TIME CHECK
    // =====================================================

    private void CheckSystemDate()
    {
        DateTime currentIST =
            GetSystemISTTime();


        Debug.Log(
            "Current System Time (IST): " +
            currentIST
        );


        Debug.Log(
            "Build Expiry Time (IST): " +
            expiryDateIST
        );


        if (currentIST >=
            expiryDateIST)
        {
            ShowExpiredPanel();
        }
    }


    // =====================================================
    // ONLINE TIME CHECK
    // =====================================================

    private IEnumerator CheckOnlineTime()
    {
        using (
            UnityWebRequest request =
            UnityWebRequest.Get(
                dateApiUrl
            )
        )
        {
            request.timeout = 10;


            yield return request.SendWebRequest();


            if (
                request.result ==
                UnityWebRequest.Result.Success
            )
            {
                string json =
                    request.downloadHandler.text;


                WorldTimeData data =
                    JsonUtility.FromJson<WorldTimeData>(
                        json
                    );


                if (data != null)
                {
                    DateTime onlineUTC;


                    if (DateTime.TryParse(
                        data.utc_datetime,
                        out onlineUTC))
                    {
                        // Convert UTC to IST
                        DateTime onlineIST =
                            onlineUTC
                            .ToUniversalTime()
                            .AddHours(5)
                            .AddMinutes(30);


                        Debug.Log(
                            "Online Time (IST): " +
                            onlineIST
                        );


                        CheckOnlineExpiry(
                            onlineIST
                        );
                    }
                }
            }
            else
            {
                Debug.LogWarning(
                    "Online time verification failed: " +
                    request.error
                );
            }
        }
    }


    // =====================================================
    // ONLINE EXPIRY CHECK
    // =====================================================

    private void CheckOnlineExpiry(
        DateTime onlineIST)
    {
        if (onlineIST >=
            expiryDateIST)
        {
            ShowExpiredPanel();
        }
    }


    // =====================================================
    // PERIODIC CHECK
    // =====================================================

    private IEnumerator PeriodicExpiryCheck()
    {
        while (!isExpired)
        {
            // Wait 30 seconds
            yield return new WaitForSeconds(
                checkInterval
            );


            // Check system time
            CheckSystemDate();


            // Check online time
            yield return CheckOnlineTime();
        }
    }


    // =====================================================
    // SHOW EXPIRY PANEL
    // =====================================================

    private void ShowExpiredPanel()
    {
        if (isExpired)
            return;


        isExpired = true;


        Debug.Log(
            "BUILD EXPIRED"
        );


        if (expiredPanel != null)
        {
            expiredPanel.SetActive(true);
        }


        if (expiryMessage != null)
        {
            expiryMessage.text =
                expiredMessage;
        }


        // Stop application interaction
        Time.timeScale = 0f;
    }


    // =====================================================
    // API DATA
    // =====================================================

    [Serializable]
    private class WorldTimeData
    {
        public string utc_datetime;
    }
    public void CloseApplication()
    {
#if UNITY_EDITOR
        // Stop Play Mode in Unity Editor
        UnityEditor.EditorApplication.isPlaying = false;
#else
    // Exit the application on Android / Windows build
    Application.Quit();
#endif
    }
}