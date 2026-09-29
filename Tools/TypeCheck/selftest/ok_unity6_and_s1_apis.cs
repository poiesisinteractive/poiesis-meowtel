// EXPECT: OK
// CONFIG: Android,Editor
// Unity 6 members injected by UnityRefPatcher + stubs S1 will rely on must type-check.
using System.Threading.Tasks;
using TMPro;
using Unity.Services.Analytics;
using Unity.Services.LevelPlay;
using UnityEngine;
using UnityEngine.UI;

namespace Meowtel.HarnessSelfTest
{
    public class OkUnity6 : MonoBehaviour
    {
        [SerializeField] private TMP_Text _label;
        [SerializeField] private Button _button;
        private LevelPlayRewardedAd _ad;

        private async Awaitable Start()
        {
            var token = destroyCancellationToken;
            await Awaitable.WaitForSecondsAsync(0.5f, token);
            var rb = GetComponent<Rigidbody2D>();
            rb.linearVelocity = Vector2.zero;
            int levels = QualitySettings.count;
            _label.text = levels.ToString();
            _label.textWrappingMode = TextWrappingModes.Normal;
            _button.onClick.AddListener(() => _ad.ShowAd("X2Adoption"));

            AnalyticsService.Instance.StartDataCollection();
            var e = new CustomEvent("tutorial_step") { { "step_index", 3 }, { "step_id", "intro" }, { "ratio", 0.5f }, { "done", true } };
            e["level"] = 5;
            AnalyticsService.Instance.RecordEvent(e);
            AnalyticsService.Instance.RecordEvent("tutorial_complete");
            AnalyticsService.Instance.Flush();

            _ad = new LevelPlayRewardedAd("unit");
            _ad.OnAdDisplayed += info => { };
            _ad.OnAdRewarded += (info, reward) => Debug.Log(reward.Name + reward.Amount);
            _ad.OnAdDisplayFailed += (info, error) => Debug.Log(error.ErrorMessage);
            if (_ad.IsAdReady()) _ad.ShowAd();
            await Task.Yield();
        }
    }
}
