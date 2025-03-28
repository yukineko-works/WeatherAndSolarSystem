using System;
using UdonSharp;
using UnityEngine;
using VRC.Udon;

namespace yukineko.WeatherAndSolarSystem
{
    public enum Weather
    {
        Sunny = 0,
        Cloudy = 1,
        Rainy = 2,
        Snowy = 3
    }

    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class WeatherSystem : UdonSharpBehaviour
    {
        [SerializeField] private uint _randomSeed = 0;
        [SerializeField] private int _weatherChangeSpanMinutes = 60;
        [SerializeField] private int[] _monthlySunnyPercentage = { 75, 65, 55, 50, 45, 30, 35, 50, 40, 45, 60, 75 };
        [SerializeField] private int _skyboxSunnyCloudCoverage = 40;
        [SerializeField] private int _skyboxCloudyCloudCoverage = 80;
        [SerializeField] private int _skyboxRainyCloudCoverage = 100;

        [SerializeField] private GameObject[] _weatherAssetContainerSunny = new GameObject[0];
        [SerializeField] private GameObject[] _weatherAssetContainerCloudy = new GameObject[0];
        [SerializeField] private GameObject[] _weatherAssetContainerRainy = new GameObject[0];
        [SerializeField] private GameObject[] _weatherAssetContainerSnowy = new GameObject[0];

        private bool _isInitialized = false;
        private UdonSharpBehaviour[] _weatherUpdateCallbacks = new UdonSharpBehaviour[0];

        public int WeatherChangeSpanMinutes => _weatherChangeSpanMinutes;

        private void Start()
        {
            Initialize();
        }

        private void Initialize()
        {
            if (_isInitialized) return;
            if (_monthlySunnyPercentage.Length != 12)
            {
                Debug.LogError("_monthlySunnyPercentageの長さが12ではありません");
                return;
            }

            _isInitialized = true;
            UpdateWeather();
        }

        public int GetElapsedSeconds(DateTimeOffset currentTime = default)
        {
            if (currentTime == default) currentTime = DateTimeOffset.UtcNow;
            var x = currentTime.ToUnixTimeSeconds();
            var y = 60 * _weatherChangeSpanMinutes;
            return (int)(x - y * (x / y));
            // var elapsedSeconds = currentTime.ToUnixTimeSeconds() % (60 * _weatherChangeSpanMinutes); // U#だとlong型に対して剰余演算子が使えない
        }

        public void UpdateWeather()
        {
            var currentTime = DateTimeOffset.UtcNow;
            var elapsedSeconds = GetElapsedSeconds(currentTime);
            var weather = GetWeather(currentTime.AddSeconds(-elapsedSeconds));

            foreach (var obj in _weatherAssetContainerSunny)
            {
                if (obj == null) continue;
                obj.SetActive(weather == Weather.Sunny);
            }

            foreach (var obj in _weatherAssetContainerCloudy)
            {
                if (obj == null) continue;
                obj.SetActive(weather == Weather.Cloudy);
            }

            foreach (var obj in _weatherAssetContainerRainy)
            {
                if (obj == null) continue;
                obj.SetActive(weather == Weather.Rainy);
            }

            foreach (var obj in _weatherAssetContainerSnowy)
            {
                if (obj == null) continue;
                obj.SetActive(weather == Weather.Snowy);
            }

            switch(weather)
            {
                case Weather.Sunny:
                    SetCloudCoverage(_skyboxSunnyCloudCoverage / 100f);
                    break;
                case Weather.Cloudy:
                    SetCloudCoverage(_skyboxCloudyCloudCoverage / 100f);
                    break;
                case Weather.Rainy:
                case Weather.Snowy:
                    SetCloudCoverage(_skyboxRainyCloudCoverage / 100f);
                    break;
            }

            foreach (var callback in _weatherUpdateCallbacks)
            {
                if (callback == null) continue;
                callback.SendCustomEvent("WeatherUpdated");
            }

            SendCustomEventDelayedSeconds(nameof(UpdateWeather), 60 * _weatherChangeSpanMinutes - elapsedSeconds + 1);
        }

        public Weather GetWeather(DateTimeOffset time)
        {
            var month = time.Month;
            var r = Rand(time, _randomSeed);
            var p = Mathf.Clamp01(_monthlySunnyPercentage[month - 1] / 100f);

            if (r >= p)
            {
                // 50%の確率で曇り
                if (Rand(time, _randomSeed + 1024) < 0.5f) return Weather.Cloudy;

                // 1~2月は確定で雪
                if (month <= 2) return Weather.Snowy;

                if (month == 12 || month == 3)
                {
                    var mp = time.Day / DateTime.DaysInMonth(time.Year, time.Month);
                    var sr = Rand(time, _randomSeed + 2048);

                    // 12月なら月末に向かうにつれて雪が降る確率が上がる
                    // 3月なら月末に向かうにつれて雪が降る確率が下がる
                    if ((month == 12 && sr < mp) || (month == 3 && sr > mp)) return Weather.Snowy;
                }

                // それ以外は雨
                return Weather.Rainy;
            }

            return Weather.Sunny;
        }

        /// <summary>
        /// 時間ベースで簡易乱数生成を行う.
        /// 精度は分単位.
        /// </summary>
        /// <param name="time">乱数生成に使用する日時</param>
        /// <param name="seed">シード値</param>
        /// <returns></returns>
        private double Rand(DateTimeOffset time, uint seed)
        {
            return Rand((uint)(time.ToUnixTimeSeconds() / 60), seed);
        }

        /// <summary>
        /// 第1引数の値を元に簡易乱数生成を行う
        /// </summary>
        /// <param name="rand">乱数生成に使用する数値</param>
        /// <param name="seed">シード値</param>
        /// <returns></returns>
        private double Rand(uint rand, uint seed)
        {
            rand ^= 0x9e3779b9;
            rand += seed + (rand << 6) + (rand >> 2);
            rand ^= rand << 13;
            rand ^= rand >> 17;
            rand ^= rand << 5;

            return (double)rand / uint.MaxValue;
        }

        /// <summary>
        /// 天候更新時に呼び出されるUdonSharpBehaviourを登録する.
        /// 天候更新時には登録されたUdonSharpBehaviourの `WeatherUpdated` メソッドが呼び出される.
        /// </summary>
        /// <param name="callback">呼び出すUdonSharpBehaviour</param>
        /// <param name="callImmediately">登録時に初期化が完了していた場合に、即時 `WeatherUpdated` メソッドを呼び出す</param>
        public void RegisterWeatherUpdateCallback(UdonSharpBehaviour callback, bool callImmediately = true)
        {
            if (callback == null) return;

            var length = _weatherUpdateCallbacks.Length;
            var list = new UdonSharpBehaviour[length + 1];
            Array.Copy(_weatherUpdateCallbacks, list, length);
            list[length] = callback;
            _weatherUpdateCallbacks = list;

            if (_isInitialized && callImmediately) callback.SendCustomEvent("WeatherUpdated");
        }

        private void SetCloudCoverage(float value)
        {
            var skybox = RenderSettings.skybox;
            switch (skybox.shader.name)
            {
                case "CaminoVR/Skybox":
                    skybox.SetFloat("_CloudCoverage", value);
                    break;
                case "Typhon/SkyBox1.1":
                    skybox.SetFloat("_CloudDensity", value);
                    break;
            }
        }
    }
}