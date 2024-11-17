
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace yukineko.WeatherAndSolarSystem
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class DayCycleSystem : UdonSharpBehaviour
    {
        [SerializeField] private float _latitude = 35.68f;
        [SerializeField] private float _longitude = 139.75f;
        [SerializeField] private int _timezone = 9;
        [SerializeField] private float _fixedAngle = 0f;
        [SerializeField] private int _updateFrequency = 1;
        [SerializeField] private Light _sunLight;
        [SerializeField] private Light _moonLight;

        [SerializeField] private float _standardMinHeightAngle = 0f;
        [SerializeField] private float _standardMaxHeightAngle = 10f;

        [SerializeField] private bool _sunIntensityControlEnabled = true;
        [SerializeField] private float _sunIntensityMin = 0f;
        [SerializeField] private float _sunIntensityMax = 1f;
        [SerializeField] private bool _sunTemperatureControlEnabled = true;
        [SerializeField] private float _sunTemperatureMin = 2000f;
        [SerializeField] private float _sunTemperatureMax = 6500f;

        [SerializeField] private bool _moonIntensityControlEnabled = true;
        [SerializeField] private float _moonIntensityMin = 0f;
        [SerializeField] private float _moonIntensityMax = 0.5f;
        [SerializeField] private bool _moonTemperatureControlEnabled = true;
        [SerializeField] private float _moonTemperatureMin = 6500f;
        [SerializeField] private float _moonTemperatureMax = 15000f;

        [SerializeField] private bool _debugMode = false;
        [SerializeField] private float _debugTimeValue = 0f;

        private readonly int _shaderIdUdonLightRotation = VRCShader.PropertyToID("_UdonLightRotation");
        private readonly int _shaderIdUdonLocalLightRotation = VRCShader.PropertyToID("_UdonLocalLightRotation");
        private readonly int _shaderIdUdonMoonDir = VRCShader.PropertyToID("_UdonMoonDir");
        private readonly int _shaderIdUdonMoonSpaceMatrix = VRCShader.PropertyToID("_UdonMoonSpaceMatrix");

        private float _delta = 0;
        private float _e = 0;
        private float _phi = 0;
        private float _t = 0;
        private bool _isInitialized = false;
        private bool _isUsingPhysicalSkybox = false;

        public int Timezone => _timezone;
        public float SunriseTime => (-_t + 180f) / 15f - (_longitude - 135f) / 15f - _e;
        public float SunsetTime => (_t + 180f) / 15f - (_longitude - 135f) / 15f - _e;
        public float MoonPhase {
            get {
                var lunarCycle = 29.53059;
                var date = DateTime.UtcNow;
                var year = date.Year;
                var month = date.Month;
                var day = date.Day;

                if (month <= 2)
                {
                    year--;
                    month += 12;
                }

                var x = year / 100;
                var currentJulianDate = Math.Floor(365.25 * (year + 4716)) + Math.Floor(30.6001 * (month + 1)) + day + (2 - x + x / 4) - 1524.5;
                var moonAge = (currentJulianDate - 2451550.1) % lunarCycle;
                if (moonAge < 0) moonAge += lunarCycle;
                return (float)(moonAge / lunarCycle);
            }
        }

        private void Start()
        {
            if (_sunLight == null)
            {
                Debug.LogError("SunLightが設定されていません");
                return;
            }

            Initialize();
        }

        private void Initialize()
        {
            if (_isInitialized) return;
            var angle = 2f * Math.PI / (DateTime.IsLeapYear(DateTime.UtcNow.Year) ? 366 : 365) * (DateTime.UtcNow.DayOfYear + 0.5f);
            _delta = (float)((0.33281f - (22.984f * Math.Cos(angle)) - (0.3499f * Math.Cos(2 * angle)) - (0.1398f * Math.Cos(3 * angle)) + (3.7872f * Math.Sin(angle)) + (0.0325f * Math.Sin(2 * angle)) + (0.07187f * Math.Sin(3 * angle))) * Mathf.Deg2Rad);
            _e = (float)((0.0072f * Math.Cos(angle)) - (0.0528f * Math.Cos(2 * angle)) - (0.0012f * Math.Cos(3 * angle)) - (0.1229f * Math.Sin(angle)) - (0.1565f * Math.Sin(2 * angle)) - (0.0041f * Math.Sin(3 * angle)));
            _phi = _latitude * Mathf.Deg2Rad;
            _t = (float)(Math.Acos(-Math.Tan(_delta) * Math.Tan(_phi)) * Mathf.Rad2Deg);
            _isInitialized = true;
            _isUsingPhysicalSkybox = RenderSettings.skybox != null && RenderSettings.skybox.shader.name == "CaminoVR/Skybox";

            if (_isUsingPhysicalSkybox)
            {
                RenderSettings.skybox.SetFloat("_MoonPhase", MoonPhase);
            }

            UpdateLightAngle();
        }

        public void UpdateLightAngle()
        {
            var current = DateTime.UtcNow.AddHours(_timezone);
            var T = current.Hour + (current.Minute / 60f) + (current.Second / 3600f) + (current.Millisecond / 3600000f) + ((_longitude - 135f) / 15f) + _e + _debugTimeValue;
            var t = ((15f * T) - 180f) * Mathf.Deg2Rad;
            var h = Math.Asin((Math.Sin(_phi) * Math.Sin(_delta)) + (Math.Cos(_phi) * Math.Cos(_delta) * Math.Cos(t)));
            var sinA = Math.Cos(_delta) * Math.Sin(t) / Math.Cos(h);
            var cosA = ((Math.Sin(h) * Math.Sin(_phi)) - Math.Sin(_delta)) / Math.Cos(h) / Math.Cos(_phi);
            var A = (float)(((Math.Atan2(sinA, cosA) + Math.PI) * Mathf.Rad2Deg) + _fixedAngle);
            var height = (float)(h * Mathf.Rad2Deg);
            var moonA = A + 180f;
            var moonHeight = height * -1f;

            _sunLight.transform.rotation = Quaternion.Euler(height, A, 0f);
            var moonLightRotation = Quaternion.Euler(moonHeight, moonA, 0f);

            if (_sunIntensityControlEnabled)
            {
                _sunLight.intensity = Mathf.Lerp(_sunIntensityMin, _sunIntensityMax, Mathf.InverseLerp(_standardMinHeightAngle, _standardMaxHeightAngle, height));
            }

            if (_sunTemperatureControlEnabled)
            {
                _sunLight.colorTemperature = Mathf.Lerp(_sunTemperatureMin, _sunTemperatureMax, Mathf.InverseLerp(_standardMinHeightAngle, _standardMaxHeightAngle, height));
            }

            if (_moonLight != null)
            {
                _moonLight.transform.rotation = moonLightRotation;

                if (_moonIntensityControlEnabled)
                {
                    _moonLight.intensity = Mathf.Lerp(_moonIntensityMin, _moonIntensityMax, Mathf.InverseLerp(_standardMinHeightAngle, _standardMaxHeightAngle, moonHeight));
                }

                if (_moonTemperatureControlEnabled)
                {
                    _moonLight.colorTemperature = Mathf.Lerp(_moonTemperatureMin, _moonTemperatureMax, Mathf.InverseLerp(_standardMinHeightAngle, _standardMaxHeightAngle, moonHeight));
                }
            }

            if (_isUsingPhysicalSkybox)
            {
                VRCShader.SetGlobalFloat(_shaderIdUdonLightRotation, A * Mathf.Deg2Rad);
                VRCShader.SetGlobalFloat(_shaderIdUdonLocalLightRotation, height);
                VRCShader.SetGlobalVector(_shaderIdUdonMoonDir, -(moonLightRotation * Vector3.forward));
                VRCShader.SetGlobalMatrix(_shaderIdUdonMoonSpaceMatrix, new Matrix4x4(-(moonLightRotation * Vector3.forward), -(moonLightRotation * Vector3.up), -(moonLightRotation * Vector3.right), Vector4.zero).transpose);
            }

#if UNITY_EDITOR
            if (_debugMode)
            {
                SendCustomEventDelayedFrames(nameof(UpdateLightAngle), 1);
            }
            else
            {
                SendCustomEventDelayedSeconds(nameof(UpdateLightAngle), _updateFrequency);
            }
#else
            SendCustomEventDelayedSeconds(nameof(UpdateLightAngle), _updateFrequency);
#endif
        }
    }
}