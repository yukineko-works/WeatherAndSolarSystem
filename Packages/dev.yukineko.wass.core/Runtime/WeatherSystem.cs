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

    public class WeatherSystem : UdonSharpBehaviour
    {
        [SerializeField] private uint _randomSeed = 0;
        [SerializeField] private int _weatherChangeSpanMinutes = 60;
        [SerializeField] private int[] _monthlySunnyPercentage = { 75, 65, 55, 50, 45, 30, 35, 50, 40, 45, 60, 75 };
        [SerializeField] private int _skyboxSunnyCloudCoverage = 40;
        [SerializeField] private int _skyboxCloudyCloudCoverage = 80;
        [SerializeField] private int _skyboxRainyCloudCoverage = 100;

        [SerializeField] private GameObject _weatherAssetContainerSunny;
        [SerializeField] private GameObject _weatherAssetContainerCloudy;
        [SerializeField] private GameObject _weatherAssetContainerRainy;
        [SerializeField] private GameObject _weatherAssetContainerSnowy;

        private bool _isInitialized = false;
        private UdonBehaviour[] _weatherUpdateCallbacks = new UdonBehaviour[0];

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

            if (_weatherAssetContainerSunny == null || _weatherAssetContainerCloudy == null || _weatherAssetContainerRainy == null || _weatherAssetContainerSnowy == null)
            {
                Debug.LogWarning("設定されていない天候アセットのコンテナがあります");
            }

            _isInitialized = true;
            UpdateWeather();
        }

        public void UpdateWeather()
        {
            var currentTime = DateTimeOffset.UtcNow;
            var x = currentTime.ToUnixTimeSeconds();
            var y = 60 * _weatherChangeSpanMinutes;
            var elapsedSeconds = x - y * (x / y);
            // var elapsedSeconds = currentTime.ToUnixTimeSeconds() % (60 * _weatherChangeSpanMinutes); // U#だとlong型に対して剰余演算子が使えない
            var weather = GetWeather(currentTime.AddSeconds(-elapsedSeconds));

            if (_weatherAssetContainerSunny != null) _weatherAssetContainerSunny.SetActive(weather == Weather.Sunny);
            if (_weatherAssetContainerCloudy != null) _weatherAssetContainerCloudy.SetActive(weather == Weather.Cloudy);
            if (_weatherAssetContainerRainy != null) _weatherAssetContainerRainy.SetActive(weather == Weather.Rainy);
            if (_weatherAssetContainerSnowy != null) _weatherAssetContainerSnowy.SetActive(weather == Weather.Snowy);

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

        private Weather GetWeather(DateTimeOffset time)
        {
            var month = time.Month;
            var r = Rand(time, _randomSeed);
            var p = Mathf.Clamp01(_monthlySunnyPercentage[month - 1] / 100f);

            if (r >= p)
            {
                // 50%の確率で曇り
                if (Rand(time, _randomSeed + 256) < 0.5f) return Weather.Cloudy;

                // 1~2月は確定で雪
                if (month <= 2) return Weather.Snowy;

                if (month == 12 || month == 3)
                {
                    var mp = time.Day / DateTime.DaysInMonth(time.Year, time.Month);
                    var sr = Rand(time, _randomSeed + 512);

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
        /// 時間ベースで乱数生成を行う
        /// </summary>
        /// <param name="time">乱数生成に使用する日時</param>
        /// <param name="randomSeed">シード値 (未指定の場合は `_randomSeed` が使用される)</param>
        /// <returns></returns>
        private float Rand(DateTimeOffset time, uint randomSeed)
        {
            return Rand(time.ToUnixTimeSeconds(), randomSeed);
        }

        /// <summary>
        /// 第1引数の値を元に乱数生成を行う
        /// </summary>
        /// <param name="unix">乱数生成に使用する数値</param>
        /// <param name="randomSeed">シード値 (未指定の場合は `_randomSeed` が使用される)</param>
        /// <returns></returns>
        private float Rand(long unix, uint randomSeed)
        {
            var mlunix = (uint)(unix / 60 & 0xFFFF);
            var seed = randomSeed & 0xFFFF;
            var combinedSeed = (mlunix << 16) | seed;

            var x = combinedSeed;
            x ^= x << 13;
            x ^= x >> 17;
            x ^= x << 5;

            return (x & 0xFFFF) / 65535f;
        }

        /// <summary>
        /// 天候更新時に呼び出されるUdonBehaviourを登録する.
        /// 天候更新時には登録されたUdonBehaviourの `WeatherUpdated` メソッドが呼び出される.
        /// </summary>
        /// <param name="callback">呼び出すUdonBehaviour</param>
        public void RegisterWeatherUpdateCallback(UdonBehaviour callback)
        {
            if (callback == null) return;

            var length = _weatherUpdateCallbacks.Length;
            var list = new UdonBehaviour[length + 1];
            Array.Copy(_weatherUpdateCallbacks, list, length);
            list[length] = callback;
            _weatherUpdateCallbacks = list;
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