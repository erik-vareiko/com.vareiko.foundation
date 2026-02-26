using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Vareiko.Foundation.Consent;
using Vareiko.Foundation.Time;
using UnityEngine;
using Zenject;

namespace Vareiko.Foundation.Analytics
{
    public sealed class AnalyticsService : IAnalyticsService
    {
        private readonly AnalyticsConfig _config;
        private readonly IConsentService _consentService;
        private readonly IFoundationTimeProvider _time;
        private readonly SignalBus _signalBus;
        private readonly List<AnalyticsEventModel> _buffer;
        private readonly Dictionary<string, string> _sessionProperties = new Dictionary<string, string>();

        private string _userId;
        private readonly int _maxBufferedEvents;

        [Inject]
        public AnalyticsService(
            IFoundationTimeProvider timeProvider,
            [InjectOptional] IConsentService consentService = null,
            [InjectOptional] AnalyticsConfig config = null,
            [InjectOptional] SignalBus signalBus = null)
        {
            _time = timeProvider;
            _consentService = consentService;
            _config = config;
            _signalBus = signalBus;
            _maxBufferedEvents = _config != null ? Mathf.Max(32, _config.MaxBufferedEvents) : 2048;
            _buffer = new List<AnalyticsEventModel>(_maxBufferedEvents);
        }

        public bool Enabled => _config == null || _config.Enabled;

        public void SetUserId(string userId)
        {
            _userId = userId ?? string.Empty;
        }

        public void SetSessionProperty(string key, string value)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return;
            }

            _sessionProperties[key] = value ?? string.Empty;
        }

        public void TrackEvent(string eventName, IReadOnlyDictionary<string, string> properties = null)
        {
            if (!Enabled || string.IsNullOrWhiteSpace(eventName))
            {
                return;
            }

            if (!HasTrackingConsent())
            {
                _signalBus?.Fire(new AnalyticsEventDroppedSignal(eventName, "Consent is not granted."));
                return;
            }

            if (_buffer.Count >= _maxBufferedEvents)
            {
                _buffer.RemoveAt(0);
            }

            AnalyticsEventModel model = new AnalyticsEventModel
            {
                EventName = eventName,
                TimeFromStartup = _time.Time,
                UserId = _userId,
                Properties = new Dictionary<string, string>(_sessionProperties)
            };

            if (properties != null)
            {
                foreach (KeyValuePair<string, string> pair in properties)
                {
                    model.Properties[pair.Key] = pair.Value;
                }
            }

            _buffer.Add(model);
            if (_config != null && _config.VerboseLogging)
            {
                Debug.Log($"[Analytics] {eventName}");
            }

            _signalBus?.Fire(new AnalyticsEventTrackedSignal(eventName));
        }

        public UniTask FlushAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return UniTask.CompletedTask;
        }

        private bool HasTrackingConsent()
        {
            bool requireConsent = _config == null || _config.RequireConsent;
            if (!requireConsent)
            {
                return true;
            }

            if (_consentService == null)
            {
                return false;
            }

            return _consentService.IsConsentCollected && _consentService.HasConsent(ConsentScope.Analytics);
        }
    }
}
