using PKR.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PKR
{
    /// <summary>
    /// Watches frame times on the phone and picks how much to draw (Settings > Graphics: AUTO / HIGH / BATTERY SAVER).
    /// Every 30 s it writes one "[PKR Perf]" line to the device log (frame rate, 95th percentile frame, missed frames,
    /// tier, thermal state), which is how frame rates are measured on a real iPhone. SHOW FPS draws a small readout.
    /// </summary>
    public class PerfMonitor : MonoBehaviour
    {
        public static PerfMonitor Instance { get; private set; }

        /// <summary>The current tier (High when no monitor runs, e.g. in tests that skip bootstrap).</summary>
        public static PerfTier Tier => Instance != null ? Instance.Governor.Tier : PerfTier.High;

        public PerfGovernor Governor { get; } = new PerfGovernor();
        public float LogEvery = 30f;
        public int LinesLogged { get; private set; }

        float _logT, _deviceT, _labelT, _settingsT;
        int _appliedFps;
        string _label = "";
        bool _show;
        GUIStyle _style;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
            ReadSettings();
            ReadDevice();
            ApplyFrameRate();
        }

        void OnDestroy() { if (Instance == this) Instance = null; }

        void ReadSettings()
        {
            var s = Services.Settings != null ? Services.Settings.Data : null;
            var mode = s != null ? (GraphicsMode)s.graphicsMode : GraphicsMode.Auto;
            if (mode != Governor.Mode) Governor.SetMode(mode);
            _show = s != null && s.showFps;
        }

        void ReadDevice()
        {
            Governor.SetThermal(DeviceIdiom.Thermal);
            Governor.LowPower = DeviceIdiom.LowPowerMode;
        }

        void ApplyFrameRate()
        {
            int fps = Governor.TargetFps;
            if (fps == _appliedFps) return;
            _appliedFps = fps;
            Application.targetFrameRate = fps;
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            var before = Governor.Tier;
            Governor.Frame(dt);
            _settingsT -= dt;
            if (_settingsT <= 0f) { _settingsT = 1f; ReadSettings(); }
            _deviceT -= dt;
            if (_deviceT <= 0f) { _deviceT = 3f; ReadDevice(); }
            ApplyFrameRate();
            if (Governor.Tier != before)
                Debug.Log($"[PKR Perf] tier {before} -> {Governor.Tier} (thermal {Governor.Thermal}) {Governor.Stats.Summary(Governor.Budget)}");
            _logT += dt;
            if (_logT >= LogEvery) { _logT = 0f; Log(); }
            if (_show)
            {
                _labelT -= dt;
                if (_labelT <= 0f)
                {
                    _labelT = 0.5f;
                    _label = $"{Governor.Stats.AverageFps:0} FPS  p95 {Governor.Stats.Percentile(0.95f) * 1000f:0}ms  {Governor.Tier}" +
                             (Governor.Thermal >= ThermalState.Serious ? "  HOT" : "");
                }
            }
        }

        public void Log()
        {
            LinesLogged++;
            Debug.Log($"[PKR Perf] scene {SceneManager.GetActiveScene().name}  mode {Governor.Mode}  tier {Governor.Tier}  target {Governor.TargetFps}  " +
                      $"thermal {Governor.Thermal}  lowpower {Governor.LowPower}  {Governor.Stats.Summary(Governor.Budget)}");
        }

        void OnGUI()
        {
            if (!_show) return;
            if (_style == null)
            {
                _style = new GUIStyle(GUI.skin.label) { fontSize = Mathf.Max(18, Screen.height / 45), fontStyle = FontStyle.Bold };
                _style.normal.textColor = new Color(0.6f, 1f, 0.6f);
            }
            var sa = Screen.safeArea;
            GUI.Label(new Rect(sa.x + 12f, Screen.height - sa.yMax + 8f, 600f, 40f), _label, _style);
        }
    }
}
