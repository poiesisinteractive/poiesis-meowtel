// PRE-STUB of com.unity.services.analytics 6.x (manifest: 6.3.0) - namespace Unity.Services.Analytics.
// Not used by the baseline game code: added so that sprint S1 (B-5, S1.3) can be type-checked.
// Every member here is an API-BELIEF taken from the Analytics 6.x documentation, not verified against the package source.
using System;
using System.Collections;
using System.Collections.Generic;

namespace Unity.Services.Analytics
{
    // API-BELIEF (Analytics 6.x docs)
    public static class AnalyticsService
    {
        public static IAnalyticsService Instance => throw null; // API-BELIEF (Analytics 6.x docs)
    }

    // API-BELIEF (Analytics 6.x docs)
    public interface IAnalyticsService
    {
        void StartDataCollection();                 // API-BELIEF (Analytics 6.x docs)
        void StopDataCollection();                  // API-BELIEF (Analytics 6.x docs)
        void RequestDataDeletion();                 // API-BELIEF (Analytics 6.x docs)
        void RecordEvent(Event e);                  // API-BELIEF (Analytics 6.x docs)
        void RecordEvent(string eventName);         // API-BELIEF (Analytics 6.x docs)
        void Flush();                               // API-BELIEF (Analytics 6.x docs)
        string GetAnalyticsUserID();                // API-BELIEF (Analytics 6.x docs) // GUESS
        string SessionID { get; }                   // API-BELIEF (Analytics 6.x docs) // GUESS
    }

    // API-BELIEF (Analytics 6.x docs): base class of all recordable events; subclass it for strongly typed events.
    public abstract class Event
    {
        protected Event(string name) { }            // API-BELIEF (Analytics 6.x docs)
        protected void SetParameter(string name, string value) { }  // API-BELIEF (Analytics 6.x docs)
        protected void SetParameter(string name, int value) { }     // API-BELIEF (Analytics 6.x docs)
        protected void SetParameter(string name, long value) { }    // API-BELIEF (Analytics 6.x docs)
        protected void SetParameter(string name, float value) { }   // API-BELIEF (Analytics 6.x docs)
        protected void SetParameter(string name, double value) { }  // API-BELIEF (Analytics 6.x docs)
        protected void SetParameter(string name, bool value) { }    // API-BELIEF (Analytics 6.x docs)
        protected void SetParameter(string name, DateTime value) { } // API-BELIEF (Analytics 6.x docs) // GUESS
        public void Reset() { }                                      // API-BELIEF (Analytics 6.x docs) // GUESS
    }

    // API-BELIEF (Analytics 6.x docs): usage
    //   var e = new CustomEvent("tutorial_step") { { "step_index", 3 }, { "step_id", "intro" } };
    //   e["level"] = 5;
    //   AnalyticsService.Instance.RecordEvent(e);
    public class CustomEvent : Event, IEnumerable
    {
        public CustomEvent(string name) : base(name) { }             // API-BELIEF (Analytics 6.x docs)
        public void Add(string key, string value) { }                // API-BELIEF (Analytics 6.x docs)
        public void Add(string key, int value) { }                   // API-BELIEF (Analytics 6.x docs)
        public void Add(string key, long value) { }                  // API-BELIEF (Analytics 6.x docs)
        public void Add(string key, float value) { }                 // API-BELIEF (Analytics 6.x docs)
        public void Add(string key, double value) { }                // API-BELIEF (Analytics 6.x docs)
        public void Add(string key, bool value) { }                  // API-BELIEF (Analytics 6.x docs)
        public object this[string key] { set { } }                   // API-BELIEF (Analytics 6.x docs) // GUESS: setter-only, object-typed
        IEnumerator IEnumerable.GetEnumerator() => throw null;       // API-BELIEF (Analytics 6.x docs): needed for collection initializers
    }
}
